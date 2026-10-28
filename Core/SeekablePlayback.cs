namespace AetherRadio.Core;

public interface ILocalTrack : IDisposable
{
    double Position { get; }
    double Duration { get; }
    bool HasEnded => Position >= Duration;
    bool Loop { get; set; }
    int VolumePercent { get; set; }
    void Start();
    void Seek(double seconds);
    void Validate();
}

// Only decoding runs on the worker. Native hooks/output ownership stay on the caller thread.
// One worker and one replaceable pending request bound rapid-click memory and IO.
public sealed class SeekablePlayback(IBgmPlayback native, Action<bool> silentOverride,
    Func<ushort, ILocalTrack?> load) : IBgmPlayback
{
    private readonly object gate = new();
    private Task? worker;
    private (ushort Id, long Generation)? pending;
    private (long Generation, ILocalTrack? Track, Exception? Error)? ready;
    private long generation;
    private bool disposed, loading;
    private ILocalTrack? audio;
    public ushort? Current { get; private set; }
    public bool Locked { get; set; } = true;
    public int VolumePercent { get; set; } = 100;
    public bool Loop { get; set; } = true;
    public bool IsLoading => loading;
    public bool CanSeek => audio != null;
    public bool HasEnded => audio?.HasEnded == true;
    public double PositionSeconds => audio?.Position ?? 0;
    public double DurationSeconds => audio?.Duration ?? 0;

    public void Play(ushort id)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        Stop();
        Current = id;
        loading = true;
        lock (gate)
        {
            pending = (id, ++generation);
            worker ??= Task.Run(LoadWorker);
        }
    }
    private void LoadWorker()
    {
        while (true)
        {
            (ushort Id, long Generation) request;
            lock (gate)
            {
                if (disposed || pending == null) { worker = null; return; }
                request = pending.Value; pending = null;
            }
            ILocalTrack? result = null;
            Exception? error = null;
            try { result = load(request.Id); } catch (Exception e) { error = e; }
            lock (gate)
            {
                if (!disposed && generation == request.Generation)
                { ready = (request.Generation, result, error); result = null; }
            }
            result?.Dispose();
        }
    }
    public void ValidateOwner()
    {
        if (disposed || Current == null) return;
        (long Generation, ILocalTrack? Track, Exception? Error)? completed;
        lock (gate) { completed = ready; ready = null; }
        if (completed is { } item)
        {
            if (item.Generation != generation) { item.Track?.Dispose(); return; }
            loading = false;
            if (item.Error != null) throw new PlaybackException("AudioLoadFailed", item.Error);
            audio = item.Track;
            silentOverride(audio != null);
            native.Locked = Locked;
            native.VolumePercent = audio != null ? 0 : VolumePercent;
            native.Play(Current.Value);
            if (audio != null)
            {
                audio.Loop = Loop; audio.VolumePercent = VolumePercent;
                audio.Start();
            }
        }
        if (loading) return;
        native.Locked = Locked;
        native.VolumePercent = audio != null ? 0 : VolumePercent;
        native.ValidateOwner();
        if (native.Current == null) { Stop(); return; }
        if (audio != null)
        {
            audio.Loop = Loop; audio.VolumePercent = VolumePercent;
            audio.Validate();
        }
    }
    public void Seek(double seconds)
    {
        if (disposed || audio == null || !double.IsFinite(seconds)) return;
        audio.Seek(Math.Clamp(seconds, 0, audio.Duration));
    }
    private void Clear()
    {
        ILocalTrack? abandoned;
        lock (gate)
        {
            generation++; pending = null;
            abandoned = ready?.Track; ready = null;
        }
        Current = null; loading = false;
        var previous = audio; audio = null;
        try { previous?.Dispose(); } finally { abandoned?.Dispose(); }
    }
    public void Stop() { if (disposed) return; try { Clear(); } finally { native.Stop(); } }
    public void ResetSession() { if (disposed) return; try { Clear(); } finally { native.ResetSession(); } }
    public void Dispose()
    {
        if (disposed) return;
        Task? running;
        lock (gate) { disposed = true; running = worker; }
        try { Clear(); }
        finally
        {
            try { running?.GetAwaiter().GetResult(); }
            finally { native.Dispose(); }
        }
    }
}
