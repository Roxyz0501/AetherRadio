namespace AetherRadio.Core;

public enum MusicChannel { Normal, TimeStretched, Orchestrion }

// Local music buses only; this cannot address SE, voice, environment or master.
public interface IMusicVolume
{
    nint Owner { get; }
    float Read(nint owner, MusicChannel channel);
    void Write(nint owner, MusicChannel channel, float value);
}

public sealed class BgmVolumeSession(IMusicVolume output)
{
    public const int MaxPercent = 200;
    private static readonly MusicChannel[] Channels = [MusicChannel.Normal, MusicChannel.TimeStretched, MusicChannel.Orchestrion];
    private sealed class Level(float baseline)
    {
        public float Baseline = baseline;
        public float Applied = baseline;
    }
    private readonly Dictionary<MusicChannel, Level> levels = [];
    private nint owner;

    public void Apply(int percent)
    {
        var currentOwner = output.Owner;
        if (owner != currentOwner) { levels.Clear(); owner = currentOwner; }
        if (owner == 0) return;
        var gain = Math.Clamp(percent, 0, MaxPercent) / 100f;
        foreach (var channel in Channels)
        {
            var current = output.Read(owner, channel);
            if (!float.IsFinite(current) || current < 0 || current > 1) continue;
            if (!levels.TryGetValue(channel, out var level)) levels[channel] = level = new Level(current);
            // Honor a later game-side volume change instead of restoring a stale value.
            if (!Same(current, level.Applied)) level.Baseline = current;
            // The game's orchestrion plays outside BGMSystem scene priority.
            // Keep it running silently so Stop can reveal its current track.
            var target = channel == MusicChannel.Orchestrion ? 0 : Math.Min(1, level.Baseline * gain);
            if (!Same(current, target)) output.Write(owner, channel, target);
            level.Applied = target;
        }
    }

    public void Restore()
    {
        try
        {
            if (owner == 0 || output.Owner != owner) return;
            List<Exception>? errors = null;
            foreach (var (channel, level) in levels)
            {
                try
                {
                    if (!Same(level.Applied, level.Baseline) && Same(output.Read(owner, channel), level.Applied))
                        output.Write(owner, channel, level.Baseline);
                }
                catch (Exception e) { (errors ??= []).Add(e); }
            }
            if (errors != null) throw new AggregateException("Restoring music volume failed.", errors);
        }
        finally { levels.Clear(); owner = 0; }
    }

    private static bool Same(float a, float b) => Math.Abs(a - b) < 0.00001f;
}
