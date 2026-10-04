using System.Reflection;
using AetherRadio;
using AetherRadio.Core;
using Dalamud.Plugin.Services;
using Newtonsoft.Json;

static class SessionTests
{
    public static void Run(Action<bool, string> check)
    {
        var client = DispatchProxy.Create<IClientState, SessionService>();
        var state = (SessionService)(object)client;
        var log = DispatchProxy.Create<IPluginLog, SessionService>();
        var catalog = new Catalog(null!, (_, _) => { });
        var first = new Track(1, "test/first", "First");
        catalog.Tracks.Add(1, first);
        catalog.Tracks.Add(2, new Track(2, "test/second", "Second"));
        var config = new Configuration
        {
            VolumePercent = 40, Shuffle = true, Repeat = RepeatMode.One,
            MiniPosition = new(20, 30), Favorites = [1],
            Playlists = [new Playlist { Name = "Saved", Tracks = [1, 2] }],
        };
        var saved = JsonConvert.SerializeObject(config);
        var mixer = new TestMusicVolume();
        var engines = new List<SessionEngine>();
        var failCreation = false;
        var attempts = 0;
        using var player = new Player(config, catalog, null, client, log, () =>
        {
            attempts++;
            if (failCreation) throw new InvalidOperationException("Not initialized yet");
            var engine = new SessionEngine(mixer);
            engines.Add(engine);
            return engine;
        });
        player.Start(first, [1, 2]); player.Next(); player.Previous(); player.Resume();
        check(attempts == 0 && player.Current == null && player.Queue.Count == 0,
            "All playback entry points reject a logged-out session");
        state.LoggedIn = true; player.OnLogin();
        for (var i = 0; i < 100; i++) player.Update();
        check(attempts == 0 && player.Error == null, "Login does not autoplay or initialize native hooks each frame");
        player.Start(first, [1, 2]); player.Next();
        check(player.IsPlaying && mixer.Values[0] < 0.8f && mixer.Values[2] == 0, "Session playback applies BGM volume and suppresses orchestrion audio");
        state.LoggedIn = false; player.OnLogout(0, 0);
        check(!player.IsPlaying && player.Current == null && player.Elapsed == 0 && player.Error == null &&
              player.Queue.Count == 0 && player.Queue.Next() == null && player.Queue.Previous() == null,
            "Logout clears active selection, timer, queue, history and error");
        check(engines[0].Resets == 1 && engines[0].Disposals == 1 && engines[0].Stops == 0 &&
              Math.Abs(mixer.Values[0] - 0.8f) < 0.00001f && Math.Abs(mixer.Values[1] - 0.6f) < 0.00001f && Math.Abs(mixer.Values[2] - 0.7f) < 0.00001f,
            "Logout releases hooks and BGM volume without replaying old content requests");
        check(JsonConvert.SerializeObject(config) == saved && player.Queue.Shuffle && player.Queue.Repeat == RepeatMode.One,
            "Logout preserves playlists, favorites, volume, placement and playback preferences");
        player.OnLogout(0, 0); player.Update();
        check(engines[0].Disposals == 1, "Repeated logout notifications are harmless");
        state.LoggedIn = true; player.OnLogin(); player.Update();
        check(!player.IsPlaying && player.Current == null && attempts == 1, "Next login starts stopped with no previous selection");
        player.Start(first, [1, 2]);
        check(player.IsPlaying && engines.Count == 2 && engines[1].Plays == 1, "Next login creates a fresh working playback engine");
        player.Stop();
        check(player.Current == first && player.Queue.Count == 2 && engines[1].Stops == 1 && Math.Abs(mixer.Values[2] - 0.7f) < 0.00001f,
            "Ordinary stop still retains the selected list for manual resume");
        state.LoggedIn = false; player.Update();
        check(player.Current == null && player.Queue.Count == 0 && engines[1].Disposals == 1,
            "Polling detects a missed logout even when playback was already stopped");
        state.LoggedIn = true; player.Update(); player.Start(first, [1, 2]);
        player.OnLogout(1, 0); // Some hosts signal before IsLoggedIn changes.
        player.Update(); player.Start(first, [1, 2]); player.Next(); player.Previous(); player.Resume();
        check(engines.Count == 3 && !player.IsPlaying && player.Queue.Count == 0,
            "An early logout event cannot reopen playback while the login flag is still true");
        state.LoggedIn = false; player.Update();
        state.LoggedIn = true; player.OnLogin(); failCreation = true;
        player.Start(first, [1, 2]);
        var failedAttempts = attempts;
        for (var i = 0; i < 100; i++) player.Update();
        check(player.Error != null && !player.IsPlaying && attempts == failedAttempts,
            "Initialization failure remains retryable without per-frame retries");
        failCreation = false; player.Start(first, [1, 2]);
        check(player.IsPlaying && player.Error == null, "Manual retry recovers from a temporarily unavailable engine");
        engines[^1].FailReset = true;
        state.LoggedIn = false; player.OnLogout(0, 0);
        check(player.Current == null && player.Queue.Count == 0 && player.Error == null && engines[^1].Disposals == 1,
            "Cleanup failure still disposes the old engine and clears session state");
        state.LoggedIn = true; player.OnLogin(); player.Start(first, [1, 2]);
        check(player.IsPlaying, "A cleanup failure does not poison the next login");
        state.LoggedIn = false; player.Next();
        check(!player.IsPlaying && player.Queue.Count == 0, "A command detects logout before the next framework update");
        state.LoggedIn = true; player.OnLogin();
        player.Start(first, [1, 2]); player.Stop(); engines[^1].Ready = false;
        player.Resume();
        check(!player.IsPlaying && player.Error != null, "Playback waits for native readiness without retaining an active override");
        engines[^1].Ready = true; player.Resume();
        check(player.IsPlaying && player.Error == null, "Playback can retry when native initialization finishes");
        player.Dispose();
        check(!player.IsPlaying && engines[^1].Disposals == 1 && Math.Abs(mixer.Values[2] - 0.7f) < 0.00001f, "Plugin unload disposes active playback and restores orchestrion volume");
    }
}

public class SessionService : DispatchProxy
{
    public bool LoggedIn;
    protected override object? Invoke(MethodInfo? method, object?[]? args) =>
        method?.Name == "get_IsLoggedIn" ? LoggedIn :
        method?.ReturnType == typeof(void) ? null :
        method?.ReturnType.IsValueType == true ? Activator.CreateInstance(method.ReturnType) : null;
}

sealed class SessionEngine(TestMusicVolume mixer) : IBgmPlayback
{
    private readonly BgmVolumeSession volume = new(mixer);
    public ushort? Current { get; private set; }
    public bool Locked { get; set; }
    public int VolumePercent { get; set; }
    public bool Ready = true, FailReset;
    public int Plays, Stops, Resets, Disposals;
    public void Play(ushort id)
    {
        if (!Ready) throw new InvalidOperationException("BGM engine not ready");
        Plays++; Current = id; volume.Apply(VolumePercent);
    }
    public void ValidateOwner() { }
    public void Stop() { Stops++; Current = null; volume.Restore(); }
    public void ResetSession()
    {
        Resets++; Current = null; volume.Restore();
        if (FailReset) throw new InvalidOperationException("Cleanup failed");
    }
    public void Dispose() { Disposals++; Current = null; volume.Restore(); }
}
