namespace AetherRadio.Core;

/// <summary>Snapshots on the caller thread, one background writer, monotonic 60-second cadence.</summary>
public sealed class DeferredSettings<T> : IDisposable
{
    private readonly object gate = new();
    private readonly AtomicSettingsFile<T> file;
    private readonly Func<T> snapshot;
    private readonly Func<long> milliseconds;
    private readonly Action<Exception> report;
    private Task? writer;
    private byte[]? baseline, pending;
    private long revision, savedRevision, pendingRevision, nextCheck;
    private int failures;
    private bool disposed;
    public Exception? LastError { get; private set; }
    public bool Stopped => failures >= 3;
    public bool BackupCleanupFailed => file.CleanupError != null;
    public bool Recovered { get; }
    public bool HasPendingChanges => revision != savedRevision;
    public int SerializationCount { get; private set; }

    public DeferredSettings(AtomicSettingsFile<T> file, Func<T> snapshot, byte[]? baseline, bool recovered,
        Action<Exception> report, Func<long>? milliseconds = null)
    {
        this.file = file; this.snapshot = snapshot; this.baseline = baseline; this.report = report;
        this.milliseconds = milliseconds ?? (() => Environment.TickCount64);
        Recovered = recovered; revision = baseline == null ? 1 : 0;
        nextCheck = this.milliseconds() + 60_000;
    }
    public void MarkDirty() { lock (gate) { if (!disposed) revision++; } }
    public void Tick()
    {
        lock (gate)
        {
            if (disposed) return;
            if (writer?.IsCompleted == true) Complete();
            if (milliseconds() < nextCheck) return;
            nextCheck = milliseconds() + 60_000;
            if (writer == null && !Stopped) Start();
        }
    }
    private void Start()
    {
        if (!HasPendingChanges) return;
        try
        {
            SerializationCount++;
            var capturedRevision = revision;
            var bytes = file.Encode(snapshot());
            if (baseline != null && bytes.AsSpan().SequenceEqual(baseline)) { savedRevision = capturedRevision; return; }
            pending = bytes; pendingRevision = capturedRevision;
            writer = Task.Run(() => file.Write(bytes));
        }
        catch (Exception e) { Failed(e); }
    }
    private void Failed(Exception e)
    {
        LastError = e; failures++; nextCheck = milliseconds() + 60_000;
        if (failures == 1 || failures == 3) report(e);
    }
    private void Complete()
    {
        var task = writer!; writer = null;
        try { task.GetAwaiter().GetResult(); baseline = pending; savedRevision = pendingRevision; failures = 0; LastError = null; }
        catch (Exception e) { Failed(e); }
        finally { pending = null; }
    }
    private void FlushCore()
    {
        if (writer != null) Complete(); // Writer never waits for the caller/UI thread.
        if (Stopped || (LastError != null && milliseconds() < nextCheck)) return;
        Start();
        if (writer != null) Complete();
    }
    public void Flush() { lock (gate) { if (!disposed) FlushCore(); } }
    public void Retry()
    {
        lock (gate)
        {
            if (disposed) return;
            if (writer != null) Complete();
            failures = 0; LastError = null; FlushCore();
            if (!HasPendingChanges && !Stopped) file.CleanupBackups();
            nextCheck = milliseconds() + 60_000;
        }
    }
    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            try { FlushCore(); } finally { disposed = true; }
        }
    }
}
