using System.Diagnostics;
using AetherRadio.Core;
using Dalamud.Plugin.Services;

namespace AetherRadio;

public sealed class Player(Configuration config, Catalog catalog, IBgmPlayback? initialEngine,
    IClientState client, IPluginLog log, Func<IBgmPlayback>? engineFactory = null) : IDisposable
{
    private IBgmPlayback? engine = initialEngine;
    private bool disposed;
    private bool sessionActive = client.IsLoggedIn;
    private bool observedLogin = client.IsLoggedIn;
    private readonly Stopwatch clock = new();
    public PlaybackQueue Queue { get; } = new() { Shuffle = config.Shuffle, Repeat = config.Repeat };
    public Track? Current { get; private set; }
    private readonly List<ushort> recent = [];
    public IReadOnlyList<ushort> Recent => recent;
    public int RecentRevision { get; private set; }
    private void Remember(ushort id)
    {
        if (recent.Count > 0 && recent[0] == id) return;
        recent.Remove(id); recent.Insert(0, id);
        if (recent.Count > 30) recent.RemoveAt(30);
        RecentRevision++;
    }
    private void ClearRecent()
    {
        if (recent.Count == 0) return;
        recent.Clear(); RecentRevision++;
    }
    public bool IsPlaying => engine?.Current != null;
    public bool Available => !disposed && (engine != null || engineFactory != null);
    private readonly Localization text = new(() => config.Language);
    private string? errorKey;
    public string? Error { get => errorKey == null ? null : text[errorKey]; set => errorKey = value; }
    public double Elapsed => clock.Elapsed.TotalSeconds;
    public int AdvanceSeconds => config.UseTrackDuration ? Current?.DurationSeconds ?? config.TrackSeconds : config.TrackSeconds;

    public void Start(Track track, IEnumerable<ushort> queue)
    {
        if (!CanPlay()) return;
        Queue.Start(queue.Where(catalog.Tracks.ContainsKey), track.Id);
        Play(track.Id);
    }
    private void Play(ushort id)
    {
        if (!CanPlay()) return;
        try
        {
            // Create on demand, including after logout or a failed initialization.
            engine ??= engineFactory?.Invoke();
            if (engine == null) throw new PlaybackException("ApiUnavailable");
            if (!catalog.Tracks.TryGetValue(id, out var track)) throw new PlaybackException("TrackUnavailable");
            engine.Locked = config.LockBgm;
            engine.VolumePercent = config.VolumePercent;
            engine.Play(id);
            Current = track;
            Remember(id);
            clock.Restart();
            Error = null;
        }
        catch (Exception e)
        {
            Error = e is PlaybackException known ? known.Key : "PlaybackError";
            log.Error(e, "BGM playback failed");
            Stop();
        }
    }
    public void Resume() { if (CanPlay() && Current != null) Play(Current.Id); }
    public void Next(bool automatic = false)
    {
        if (!CanPlay()) return;
        if (Queue.Next(automatic) is { } id) Play(id); else Stop();
    }
    public void Previous() { if (CanPlay() && Queue.Previous() is { } id) Play(id); }
    public void Stop()
    {
        if (disposed) return;
        SynchronizeSession();
        clock.Stop();
        try { engine?.Stop(); }
        catch (Exception e) { Error = "RestoreError"; log.Error(e, "Restoring game BGM failed"); }
    }
    public void Update()
    {
        if (disposed) return;
        try { UpdatePlayback(); }
        catch (Exception e) { Error = e is PlaybackException known ? known.Key : "PlaybackError"; log.Error(e, "BGM update failed"); Stop(); }
    }
    private void UpdatePlayback()
    {
        SynchronizeSession();
        if (!IsPlaying) { clock.Stop(); return; }
        if (!sessionActive) { ResetSession(); return; }
        engine!.Locked = config.LockBgm;
        engine.VolumePercent = config.VolumePercent;
        engine.ValidateOwner();
        if (IsPlaying && config.AutoAdvance && config.Repeat != RepeatMode.One && Elapsed >= AdvanceSeconds) Next(true);
    }

    public void OnLogin()
    {
        if (disposed) return;
        if (!sessionActive) ResetSession();
        sessionActive = true;
        observedLogin = client.IsLoggedIn;
    }

    public void OnLogout(int type, int code)
    {
        if (disposed) return;
        sessionActive = false;
        observedLogin = client.IsLoggedIn;
        ResetSession();
    }

    private void SynchronizeSession()
    {
        // Also cover a state change missed by an event subscription. Keeping the
        // observation separate prevents an early logout event from reopening a session.
        var loggedIn = client.IsLoggedIn;
        if (loggedIn == observedLogin) return;
        observedLogin = loggedIn;
        if (loggedIn) OnLogin(); else OnLogout(0, 0);
    }

    private bool CanPlay()
    {
        if (disposed) return false;
        SynchronizeSession();
        if (sessionActive && client.IsLoggedIn) return true;
        Error = "LoginRequired";
        return false;
    }

    private void ResetSession()
    {
        clock.Reset();
        Current = null;
        Queue.Clear();
        ClearRecent();
        Error = null;
        var previous = engine;
        engine = null;
        if (previous == null) return;
        try
        {
            try { previous.ResetSession(); }
            finally { previous.Dispose(); }
        }
        catch (Exception e) { log.Error(e, "BGM session cleanup failed"); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { engine?.Dispose(); }
        finally { engine = null; clock.Reset(); Current = null; Queue.Clear(); ClearRecent(); }
    }
}
