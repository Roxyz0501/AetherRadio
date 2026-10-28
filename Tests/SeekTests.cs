using AetherRadio;
using AetherRadio.Core;
using Lumina;
using Lumina.Data.Files;
using System.Reflection;
using Dalamud.Plugin.Services;

static class SeekTests
{
    public static void Run(Action<bool, string> check)
    {
        var client = DispatchProxy.Create<IClientState, SessionService>();
        ((SessionService)(object)client).LoggedIn = true;
        var log = DispatchProxy.Create<IPluginLog, SessionService>();
        var catalog = new Catalog(null!, (_, _) => { });
        var first = new Track(1, "test/first", "First"); catalog.Tracks.Add(1, first);
        catalog.Tracks.Add(2, new Track(2, "test/second", "Second"));
        var ending = new EndPlayback();
        using (var player = new Player(new Configuration { AutoAdvance = true, UseTrackDuration = false, TrackSeconds = 180, Repeat = RepeatMode.All }, catalog, ending, client, log))
        {
            player.Start(first, [1, 2]); ending.Finished = true; player.Update();
            check(player.Current?.Id == 2, "Actual audio end advances even when configured timer exceeds duration");
        }
        using (var asyncPlayer = new Player(new Configuration(), catalog,
            new SeekablePlayback(new SeekNative(), _ => { }, id => id == 1 ? throw new InvalidDataException() : new FakeAudio()), client, log))
        {
            asyncPlayer.Start(first, [1, 2]);
            check(asyncPlayer.Recent.Count == 0, "Loading selections do not enter successful playback history");
            var deadline = Environment.TickCount64 + 5000;
            while (asyncPlayer.IsLoading && Environment.TickCount64 < deadline) { asyncPlayer.Update(); Thread.Sleep(1); }
            check(!asyncPlayer.IsPlaying && asyncPlayer.Recent.Count == 0 && asyncPlayer.Error != null, "Failed asynchronous decode restores playback without recording success");
            asyncPlayer.Start(catalog.Tracks[2], [2]);
            deadline = Environment.TickCount64 + 5000;
            while (asyncPlayer.IsLoading && Environment.TickCount64 < deadline) { asyncPlayer.Update(); Thread.Sleep(1); }
            check(asyncPlayer.CanSeek && asyncPlayer.Recent.SequenceEqual(new ushort[] { 2 }), "Successful asynchronous start enters recent history once");
        }
        var native = new SeekNative();
        var loaded = new List<FakeAudio>();
        using var blocked = new ManualResetEventSlim();
        using var entered = new ManualResetEventSlim();
        var calls = 0; var silent = false;
        using var engine = new SeekablePlayback(native, value => silent = value, id =>
        {
            Interlocked.Increment(ref calls);
            if (id == 1) { entered.Set(); blocked.Wait(); }
            if (id == 99) return null;
            if (id == 98) throw new InvalidDataException();
            var track = new FakeAudio(); lock (loaded) loaded.Add(track); return track;
        });
        engine.Play(1); check(entered.Wait(5000), "Async load starts independently of frame thread");
        for (ushort id = 2; id <= 50; id++) engine.Play(id);
        check(calls == 1 && engine.IsLoading && !engine.CanSeek, "Rapid selection keeps one active read and one latest request");
        blocked.Set(); Pump(engine);
        check(calls == 2 && engine.Current == 50 && loaded[0].Disposed && loaded[1].Started,
            "Stale load is disposed; only latest selection starts");
        check(silent && native.VolumePercent == 0 && engine.DurationSeconds == 100, "Local audio mutes only controlled game BGM and exposes duration");
        engine.Seek(40); check(engine.PositionSeconds == 40, "Seeking updates audio position");
        engine.Seek(double.NaN); check(engine.PositionSeconds == 40, "Nonfinite seek is ignored");
        engine.Seek(300); check(engine.PositionSeconds == 100, "Seek clamps to track duration");
        engine.VolumePercent = 180; engine.Loop = false; engine.ValidateOwner();
        check(loaded[1].VolumePercent == 180 && !loaded[1].Loop && native.VolumePercent == 0, "Local gain and repeat changes do not unmute game BGM");
        native.Stop(); engine.ValidateOwner();
        check(engine.Current == null && loaded[1].Disposed, "Game takeover disposes local output");
        engine.Play(99); Pump(engine);
        check(!silent && !engine.CanSeek && native.Current == 99, "Unsupported format uses native playback with disabled seek");
        engine.Play(98);
        var failed = false;
        try { Pump(engine); } catch (PlaybackException e) { failed = e.Key == "AudioLoadFailed"; }
        engine.Stop(); check(failed && engine.Current == null, "Corrupt audio reports failure and can stop safely");
        blocked.Reset(); entered.Reset(); engine.Play(1); entered.Wait(5000); engine.ResetSession(); blocked.Set();
        engine.Dispose();
        check(loaded.All(x => x.Disposed) && native.Current == null, "Logout/unload joins pending work and disposes every decoded track");
    }
    private static void Pump(SeekablePlayback engine)
    {
        var deadline = Environment.TickCount64 + 5000;
        do { engine.ValidateOwner(); if (!engine.IsLoading) return; Thread.Sleep(1); } while (Environment.TickCount64 < deadline);
        throw new TimeoutException("Audio worker did not finish");
    }
    public static void Installed(GameData game, Action<bool, string> check)
    {
        foreach (var path in new[] { "music/ffxiv/BGM_Town_Rest_01.scd", "music/ffxiv/Orchestrion/BGM_ORCH_907.scd", "music/ffxiv/Orchestrion/BGM_ORCH_908.scd" })
        {
            var file = game.GetFile<ScdFile>(path)!;
            using var track = LocalMusic.Decode(file);
            check(track != null && track.Duration > 20, "Installed song decodes in memory: " + path);
            var samples = new float[4096];
            track!.Loop = false; track.Seek(20); var n = track.Read(samples, 0, samples.Length);
            check(n == samples.Length && samples.Any(x => Math.Abs(x) > 0.00001), "Seeked song produces nonzero PCM without a device");
            var expected = samples.ToArray(); track.Seek(20); track.Read(samples, 0, samples.Length);
            check(samples.SequenceEqual(expected), "Repeated seek is sample deterministic");
            track.Seek(track.Duration - 0.01); var tail = track.Read(samples, 0, samples.Length);
            check(tail < samples.Length && track.Read(samples, 0, samples.Length) == 0, "Nonlooping decoder reaches end");
            track.Loop = true; track.Seek(track.Duration - 0.01);
            check(track.Read(samples, 0, samples.Length) == samples.Length, "Loop crosses EOF without stopping");
            track.Seek(-1); check(track.Position == 0, "Decoder clamps negative seek");
            Console.WriteLine($"AUDIO: {path}; compressed={file.Data.Length}; duration={track.Duration:F3}s; channels={track.WaveFormat.Channels}");
        }
    }
    private sealed class FakeAudio : ILocalTrack
    {
        public double Position { get; private set; }
        public double Duration => 100;
        public bool Loop { get; set; }
        public int VolumePercent { get; set; }
        public bool Disposed, Started;
        public void Start() => Started = true;
        public void Seek(double seconds) => Position = seconds;
        public void Validate() { }
        public void Dispose() => Disposed = true;
    }
    private sealed class EndPlayback : IBgmPlayback
    {
        public ushort? Current { get; private set; }
        public bool Locked { get; set; }
        public int VolumePercent { get; set; }
        public bool Finished;
        public bool CanSeek => true;
        public bool HasEnded => Finished;
        public double DurationSeconds => 100;
        public double PositionSeconds => Finished ? 100 : 0;
        public void Play(ushort id) { Current = id; Finished = false; }
        public void Stop() => Current = null;
        public void ResetSession() => Stop();
        public void ValidateOwner() { }
        public void Dispose() => Stop();
    }
    private sealed class SeekNative : IBgmPlayback
    {
        public ushort? Current { get; private set; }
        public bool Locked { get; set; }
        public int VolumePercent { get; set; }
        public void Play(ushort id) => Current = id;
        public void Stop() => Current = null;
        public void ResetSession() => Stop();
        public void ValidateOwner() { }
        public void Dispose() => Stop();
    }
}
