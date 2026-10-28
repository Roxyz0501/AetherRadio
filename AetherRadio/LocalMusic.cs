using AetherRadio.Core;
using Dalamud.Plugin.Services;
using Lumina.Data.Files;
using Lumina.Data.Parsing.Scd;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NVorbis;

namespace AetherRadio;

public static class LocalMusic
{
    public const int MaxCompressedBytes = 64 * 1024 * 1024;
    public static ILocalTrack? Load(IDataManager data, Track track)
    {
        var file = data.GetFile<ScdFile>(track.Path) ?? throw new PlaybackException("TrackUnavailable");
        return Decode(file);
    }
    // Never export game data. Only the selected compressed stream is retained.
    public static LocalVorbisTrack? Decode(ScdFile file)
    {
        if (file.Data.Length > MaxCompressedBytes || file.AudioDataCount != 1) return null;
        var audio = file.GetAudio(0);
        if (audio.AudioBasicDesc.Format != AudioFormat.OggVorbis) return null;
        if (audio.AudioData.Length > MaxCompressedBytes) return null;
        return new LocalVorbisTrack(audio.AudioData, audio.MarkerChunkHeader?.SampleLoopStartPos ?? 0,
            audio.MarkerChunkHeader?.SampleLoopEndPos ?? 0);
    }
}

public sealed class LocalVorbisTrack : ILocalTrack, ISampleProvider
{
    private readonly object gate = new();
    private readonly VorbisReader reader;
    private readonly long loopStart, loopEnd;
    private WaveOutEvent? output;
    private Exception? outputError;
    private bool disposed;
    private volatile bool reachedEnd;
    private volatile bool loop = true;
    private volatile int volumePercent = 100;
    public WaveFormat WaveFormat { get; }
    public double Duration { get; }
    public bool HasEnded => reachedEnd && output?.PlaybackState == PlaybackState.Stopped;
    public double Position { get { lock (gate) return reader.TimePosition.TotalSeconds; } }
    public bool Loop { get => loop; set => loop = value; }
    public int VolumePercent { get => volumePercent; set => volumePercent = Math.Clamp(value, 0, BgmVolumeSession.MaxPercent); }

    public LocalVorbisTrack(byte[] ogg, long start, long end)
    {
        var stream = new MemoryStream(ogg, false);
        try { reader = new VorbisReader(stream, true); }
        catch { stream.Dispose(); throw; }
        try
        {
            if (reader.Channels is < 1 or > 2 || reader.SampleRate is < 8000 or > 192000 || reader.TotalSamples <= 0)
                throw new InvalidDataException("Unsupported audio layout");
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(reader.SampleRate, reader.Channels);
            Duration = reader.TotalTime.TotalSeconds;
            loopStart = start >= 0 && start < reader.TotalSamples ? start : 0;
            loopEnd = end > loopStart && end <= reader.TotalSamples ? end : reader.TotalSamples;
        }
        catch { reader.Dispose(); throw; }
    }
    public void Start()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        outputError = null;
        var device = new WaveOutEvent { DesiredLatency = 100, NumberOfBuffers = 2, DeviceNumber = -1 };
        try
        {
            device.Init(new SampleToWaveProvider16(this));
            device.PlaybackStopped += (_, e) => { if (ReferenceEquals(output, device) && e.Exception != null) Interlocked.Exchange(ref outputError, e.Exception); };
            output = device;
            device.Play();
        }
        catch { output = null; device.Dispose(); throw; }
    }
    public void Seek(double seconds)
    {
        if (!double.IsFinite(seconds)) return;
        var playing = output != null;
        // Flush queued samples before moving the decoder. Never wait while holding its lock.
        CloseOutput();
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            var target = (long)(Math.Clamp(seconds, 0, Duration) * reader.SampleRate);
            SeekSamples(Math.Min(target, reader.TotalSamples - 1));
            reachedEnd = false;
        }
        if (playing) Start();
    }
    private readonly float[] seekScratch = new float[8192];
    private void SeekSamples(long target)
    {
        try { reader.SamplePosition = target; return; }
        catch (InvalidDataException) when (target > 0)
        {
            // Some SCD Ogg endings have trimmed final granules. Seek to an earlier
            // valid page, then decode/discard a bounded tail to reach the exact sample.
            for (var seconds = 2; seconds <= 16; seconds *= 2)
            {
                try { reader.SamplePosition = Math.Max(0, target - (long)reader.SampleRate * seconds); }
                catch (InvalidDataException) { continue; }
                while (reader.SamplePosition < target)
                {
                    var count = (int)Math.Min(seekScratch.Length, (target - reader.SamplePosition) * reader.Channels);
                    if (reader.ReadSamples(seekScratch, 0, count) == 0) break;
                }
                return;
            }
            throw;
        }
    }
    public int Read(float[] buffer, int offset, int count)
    {
        lock (gate)
        {
            if (disposed) return 0;
            var written = 0;
            while (written < count)
            {
                var boundary = loop && reader.SamplePosition < loopEnd ? loopEnd : reader.TotalSamples;
                var available = (int)Math.Min(count - written, (boundary - reader.SamplePosition) * reader.Channels);
                var read = available > 0 ? reader.ReadSamples(buffer, offset + written, available) : 0;
                written += read;
                if (read == 0 || reader.SamplePosition >= boundary)
                {
                    if (!loop) { reachedEnd = true; break; }
                    reader.SamplePosition = loopStart;
                    if (read == 0 && available > 0) break; // corrupt/truncated stream: no endless callback
                }
            }
            var gain = volumePercent / 100f;
            for (var i = offset; i < offset + written; i++) buffer[i] = Math.Clamp(buffer[i] * gain, -1f, 1f);
            return written;
        }
    }
    public void Validate()
    {
        if (Volatile.Read(ref outputError) is { } error) throw new PlaybackException("AudioOutputFailed", error);
    }
    private void CloseOutput()
    {
        var previous = output; output = null;
        if (previous != null) { try { previous.Stop(); } finally { previous.Dispose(); } }
    }
    public void Dispose()
    {
        if (disposed) return;
        try { CloseOutput(); }
        finally { lock (gate) { disposed = true; reader.Dispose(); } }
    }
}
