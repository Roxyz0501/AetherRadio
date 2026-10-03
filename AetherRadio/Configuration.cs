using Dalamud.Configuration;
using AetherRadio.Core;
using System.Numerics;

namespace AetherRadio;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public bool ShowMini { get; set; } = true;
    public bool PinMini { get; set; }
    public int Corner { get; set; } = 2;
    public Vector2? MiniPosition { get; set; }
    public float Opacity { get; set; } = 0.94f;
    public int VolumePercent { get; set; } = 100;
    public bool LockBgm { get; set; } = true;
    public bool Shuffle { get; set; }
    public RepeatMode Repeat { get; set; } = RepeatMode.All;
    public bool AutoAdvance { get; set; } = true;
    public bool UseTrackDuration { get; set; } = true;
    public int TrackSeconds { get; set; } = 180;
    public HashSet<ushort> Favorites { get; set; } = [];
    public List<Playlist> Playlists { get; set; } = [];
}
