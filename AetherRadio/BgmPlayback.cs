using Dalamud.Hooking;
using System.Text;
using InteropGenerator.Runtime;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Sound;
using AetherRadio.Core;

namespace AetherRadio;

// Hooks terminate in BGMSystem and local PlayBGMSound; volume uses music buses.
// No networking, actions,
// chat commands, event packets or server-bound game functions are used.
public sealed unsafe class BgmPlayback : IBgmPlayback
{
    private readonly Hook<BGMSystem.Delegates.SetBGM> setHook;
    private readonly Hook<BGMSystem.Delegates.ResetBGM> resetHook;
    private readonly Dictionary<uint, Request> pending = [];
    private Hook<SoundManager.Delegates.PlayBGMSound>? pathHook;
    private readonly Catalog catalog;
    private PlaybackRouter? router;
    private PlaybackRoute? route;
    private long replacementStarted;
    private bool replacementObserved;
    private readonly object gate = new();
    private nint owner;
    private int ownCallDepth;
    private readonly Func<bool> isLoggedIn;
    private bool disposed;
    private readonly BgmVolumeSession volume = new(new NativeMusicVolume());
    public int VolumePercent { get; set; } = 100;
    public ushort? Current { get; private set; }
    public bool Locked { get; set; } = true;
    private readonly record struct Request(ushort Id, uint Scene, byte A3 = 0, bool Fade = false,
        uint Out = 0, uint In = 0, uint Start = 0, byte A8 = 0, byte A9 = 0, float Volume = 1, bool Reset = false);

    public BgmPlayback(IGameInteropProvider interop, Func<bool> isLoggedIn, Catalog catalog)
    {
        this.isLoggedIn = isLoggedIn;
        this.catalog = catalog;
        var setAddress = (nint)BGMSystem.MemberFunctionPointers.SetBGM;
        var resetAddress = (nint)BGMSystem.MemberFunctionPointers.ResetBGM;
        if (setAddress == 0 || resetAddress == 0 || BGMSystem.StaticAddressPointers.ppInstance == null)
            throw new PlaybackException("ApiUnavailable");
        setHook = interop.HookFromAddress<BGMSystem.Delegates.SetBGM>(setAddress, OnSet);
        try { resetHook = interop.HookFromAddress<BGMSystem.Delegates.ResetBGM>(resetAddress, OnReset); }
        catch { setHook.Dispose(); throw; }
        try
        {
            var address = (nint)SoundManager.MemberFunctionPointers.PlayBGMSound;
            if (address != 0) pathHook = interop.HookFromAddress<SoundManager.Delegates.PlayBGMSound>(address, OnPlayBgmSound);
        }
        catch { resetHook.Dispose(); setHook.Dispose(); throw; }

    }

    public void Play(ushort id)
    {
        if (id == 0) throw new ArgumentOutOfRangeException(nameof(id));
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (!isLoggedIn()) throw new PlaybackException("LoginRequired");
            var system = BGMSystem.Instance();
            if (!IsReady(system) || SoundManager.Instance() == null)
                throw new PlaybackException("EngineNotReady");
            if (!catalog.Tracks.TryGetValue(id, out var track)) throw new PlaybackException("TrackUnavailable");
            var activeIds = new HashSet<ushort>();
            for (var i = 0; i < system->NumScenes; i++)
            {
                var scene = system->Scenes[i];
                activeIds.Add(scene.BgmId); activeIds.Add(scene.PlayingBgmId); activeIds.Add(scene.PreviousBgmId);
            }
            foreach (var request in pending.Values) activeIds.Add(request.Id);
            var nextRoute = (router ??= new PlaybackRouter(catalog.Tracks.Values)).Resolve(track, system->Scenes[0].BgmId, activeIds);
            if (nextRoute.ReplacementPath != null && pathHook == null) throw new PlaybackException("ApiUnavailable");
            if (Current != null && owner != (nint)system) Release(ReleaseMode.Discard);
            if (Current == null)
            {
                owner = (nint)system;
                pending.Clear();
                {
                    var scene = system->Scenes[0];
                    pending[0] = new Request(scene.BgmId, 0, Fade: scene.EnableCustomFade,
                        Out: scene.FadeOutTime, In: scene.FadeInTime, Start: scene.FadeInStartTime, Volume: scene.InitialVolume, Reset: scene.BgmId == 0);
                }
                setHook.Enable();
                resetHook.Enable();
            }
            if (route?.ReplacementPath != null)
            {
                // Invalidate the previous scene before reusing a carrier as a real song.
                ownCallDepth++;
                try { resetHook.Original(system, 0); }
                finally { ownCallDepth--; }
            }
            route = nextRoute;
            replacementObserved = false;
            replacementStarted = Environment.TickCount64;
            if (route.ReplacementPath != null) pathHook!.Enable(); else pathHook?.Disable();
            Current = id;
            volume.Apply(VolumePercent);
            // Scene 0 has highest priority. Other scenes continue tracking normal
            // game state, so stopping immediately reveals the current territory/duty.
            ApplySelection(route.NativeId);
        }
    }

    private void OnSet(ushort id, uint scene, byte a3, bool fade, uint fadeOut, uint fadeIn, uint start, byte a8, byte a9, float volume)
    {
        lock (gate)
        {
            if (ownCallDepth == 0 && isLoggedIn() && Current != null && owner == (nint)BGMSystem.Instance())
            {
                if (!Locked) Stop();
                else
                {
                    if (pending.ContainsKey(scene)) pending[scene] = new Request(id, scene, a3, fade, fadeOut, fadeIn, start, a8, a9, volume);
                    else setHook.Original(id, scene, a3, fade, fadeOut, fadeIn, start, a8, a9, volume);
                    return;
                }
            }
            setHook.Original(id, scene, a3, fade, fadeOut, fadeIn, start, a8, a9, volume);
        }
    }

    private void OnReset(BGMSystem* system, uint scene)
    {
        lock (gate)
        {
            if (ownCallDepth == 0 && isLoggedIn() && Current != null && owner == (nint)system)
            {
                if (!Locked) Stop();
                else if (pending.ContainsKey(scene)) { pending[scene] = new Request(0, scene, Reset: true); return; }
            }
            resetHook.Original(system, scene);
        }
    }

    public void ValidateOwner()
    {
        lock (gate)
        {
            var system = BGMSystem.Instance();
            if (!isLoggedIn()) { ResetSession(); return; }
            if (Current != null && (owner != (nint)system || !IsReady(system))) { Release(ReleaseMode.Discard); return; }
            if (Current != null && route is { } selectedRoute && IsReady(system) && system->Scenes[0].BgmId != selectedRoute.NativeId)
            {
                if (!Locked) { Stop(); return; }
                // Catch direct local engine changes which bypass SetBGM/ResetBGM.
                var scene = system->Scenes[0];
                pending[0] = new Request(scene.BgmId, 0, Fade: scene.EnableCustomFade,
                    Out: scene.FadeOutTime, In: scene.FadeInTime, Start: scene.FadeInStartTime,
                    Volume: scene.InitialVolume, Reset: scene.BgmId == 0);
                ApplySelection(selectedRoute.NativeId);
            }
            if (route?.ReplacementPath != null && !replacementObserved && Environment.TickCount64 - replacementStarted >= 8_000)
                throw new PlaybackException("SupplementPlaybackFailed");
            if (Current != null) volume.Apply(VolumePercent);
        }
    }

    private SoundData* OnPlayBgmSound(SoundManager* manager, CStringPointer path)
    {
        lock (gate)
        {
            var system = BGMSystem.Instance();
            if (!disposed && Current != null && isLoggedIn() && manager == SoundManager.Instance() && owner == (nint)system && IsReady(system) &&
                PlaybackRouter.ShouldReplace(route, system->Scenes[0].BgmId, path.ToString()))
            {
                var bytes = Encoding.UTF8.GetBytes(route!.ReplacementPath! + "\0");
                fixed (byte* replacement = bytes)
                {
                    var sound = pathHook!.Original(manager, replacement);
                    replacementObserved = sound != null;
                    return sound;
                }
            }
            return pathHook!.Original(manager, path);
        }
    }

    private void ApplySelection(ushort id)
    {
        // Native SetBGM can internally invoke ResetBGM. Those nested calls are
        // our own playback operation, not a new request from game content.
        ownCallDepth++;
        try { setHook.Original(id, 0, 0, true, 300, 400, 0, 0, 0, 1); }
        finally { ownCallDepth--; }
    }

    private static bool IsReady(BGMSystem* system) => system != null &&
        system->NumScenes is > 0 and <= 32 && system->Scenes.LongCount == system->NumScenes;

    public void Stop() => Release(isLoggedIn() ? ReleaseMode.Restore : ReleaseMode.ResetOwned);
    public void ResetSession() => Release(ReleaseMode.ResetOwned);

    private enum ReleaseMode { Restore, Discard, ResetOwned }

    private void Release(ReleaseMode mode)
    {
        lock (gate)
        {
            if (disposed) return;
            var selected = Current == null ? (ushort?)null : route?.NativeId;
            var previousOwner = owner;
            var wasReplacement = route?.ReplacementPath != null;
            var requests = mode == ReleaseMode.Restore ? pending.Values.OrderBy(x => x.Scene).ToArray() : [];
            // Clear ownership before native cleanup; failures must not leave a
            // stale lock, restoration request or session for the next login.
            Current = null;
            route = null;
            pending.Clear();
            owner = 0;
            List<Exception>? errors = null;
            void Attempt(Action action)
            {
                try { action(); }
                catch (Exception e) { (errors ??= []).Add(e); }
            }
            if (pathHook != null) Attempt(pathHook.Disable);
            Attempt(setHook.Disable);
            Attempt(resetHook.Disable);
            Attempt(volume.Restore);
            Attempt(() =>
            {
                if (selected == null || mode == ReleaseMode.Discard) return;
                var system = BGMSystem.Instance();
                if (!IsReady(system) || previousOwner != (nint)system) return;
                if (mode == ReleaseMode.ResetOwned)
                {
                    // Never replay old content/title requests across a login.
                    // Release scene 0 only while it still contains our selection.
                    if (system->Scenes[0].BgmId == selected.Value) resetHook.Original(system, 0);
                }
                else
                {
                    if (wasReplacement && system->Scenes[0].BgmId == selected.Value) resetHook.Original(system, 0);
                    foreach (var r in requests)
                    {
                        if (r.Scene >= system->NumScenes) continue;
                        if (r.Reset) resetHook.Original(system, r.Scene);
                        else setHook.Original(r.Id, r.Scene, r.A3, r.Fade, r.Out, r.In, r.Start, r.A8, r.A9, r.Volume);
                    }
                }
            });
            if (errors != null) throw new AggregateException("BGM cleanup failed.", errors);
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            try { Stop(); }
            finally
            {
                disposed = true;
                try { pathHook?.Dispose(); }
                finally { try { resetHook.Dispose(); } finally { setHook.Dispose(); } }
            }
        }
    }
}
