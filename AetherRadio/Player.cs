using System.Diagnostics;
using AetherRadio.Core;
using Dalamud.Plugin.Services;

namespace AetherRadio;

public sealed class Player(Configuration config, Catalog catalog, BgmPlayback? engine, IClientState client, IPluginLog log)
{
    private readonly Stopwatch clock = new();
    public PlaybackQueue Queue { get; } = new() { Shuffle = config.Shuffle, Repeat = config.Repeat };
    public Track? Current { get; private set; }
    public bool IsPlaying => engine?.Current != null;
    public bool Available => engine != null;
    public string? Error { get; set; }
    public double Elapsed => clock.Elapsed.TotalSeconds;
    public int AdvanceSeconds => config.UseTrackDuration ? Current?.DurationSeconds ?? config.TrackSeconds : config.TrackSeconds;

    public void Start(Track track, IEnumerable<ushort> queue)
    {
        if (!client.IsLoggedIn) { Error = "ログイン後に再生できます。"; return; }
        Queue.Start(queue.Where(catalog.Tracks.ContainsKey), track.Id);
        Play(track.Id);
    }
    private void Play(ushort id)
    {
        try
        {
            if (engine == null) throw new InvalidOperationException("BGM APIが利用できません。");
            if (!catalog.Tracks.TryGetValue(id, out var track)) throw new InvalidOperationException("この曲は現在のゲームデータに存在しません。");
            engine.Locked = config.LockBgm;
            engine.Play(id);
            Current = track;
            clock.Restart();
            Error = null;
        }
        catch (Exception e)
        {
            Error = e.Message;
            log.Error(e, "BGM playback failed");
            Stop();
        }
    }
    public void Resume() { if (Current != null && client.IsLoggedIn) Play(Current.Id); }
    public void Next(bool automatic = false)
    {
        if (Queue.Next(automatic) is { } id) Play(id); else Stop();
    }
    public void Previous() { if (Queue.Previous() is { } id) Play(id); }
    public void Stop()
    {
        clock.Stop();
        try { engine?.Stop(); }
        catch (Exception e) { Error = e.Message; log.Error(e, "Restoring game BGM failed"); }
    }
    public void Update()
    {
        try { UpdatePlayback(); }
        catch (Exception e) { Error = e.Message; log.Error(e, "BGM update failed"); Stop(); }
    }
    private void UpdatePlayback()
    {
        if (!IsPlaying) { clock.Stop(); return; }
        if (!client.IsLoggedIn) { Stop(); return; }
        engine!.Locked = config.LockBgm;
        engine.ValidateOwner();
        if (IsPlaying && config.AutoAdvance && config.Repeat != RepeatMode.One && Elapsed >= AdvanceSeconds) Next(true);
    }
}
