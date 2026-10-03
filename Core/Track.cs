namespace AetherRadio.Core;

public sealed record Location(uint ExpansionId, string Expansion, string Category, string Name, bool Inferred = false);

public sealed class Track(ushort id, string path, string title)
{
    public ushort Id { get; } = id;
    public string Path { get; } = path;
    public string Title { get; } = title;
    public List<Location> Locations { get; } = [];
    public string MetadataSearch { get; init; } = "";
    public int? DurationSeconds { get; init; }
    public string SearchText => $"{Title} {Id} {Path} {MetadataSearch} {string.Join(' ', Locations.Select(x => x.Name))}";
}

public enum RepeatMode { Off, All, One }

public sealed class Playlist
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "マイリスト";
    public List<ushort> Tracks { get; set; } = [];
}
