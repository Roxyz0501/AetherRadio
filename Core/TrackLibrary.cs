namespace AetherRadio.Core;

/// <summary>Immutable display indexes built once from the loaded local catalogue.</summary>
public sealed class TrackLibrary
{
    public Track[] Tracks { get; }
    public Location[] Expansions { get; }
    private readonly Dictionary<string, uint> earliest;
    private readonly Location[] allLocations;
    private readonly Location[] originalLocations;

    public TrackLibrary(IEnumerable<Track> tracks)
    {
        Tracks = tracks.OrderBy(t => t.Title).ThenBy(t => t.Id).ToArray();
        earliest = Tracks.GroupBy(t => t.Path, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key,
            g => g.SelectMany(t => t.Locations).Select(l => l.ExpansionId).DefaultIfEmpty(uint.MaxValue).Min(), StringComparer.OrdinalIgnoreCase);
        allLocations = Tracks.SelectMany(t => t.Locations).Distinct().ToArray();
        originalLocations = Tracks.SelectMany(t => t.Locations.Where(l => l.ExpansionId == uint.MaxValue || l.ExpansionId == earliest[t.Path])).Distinct().ToArray();
        Expansions = allLocations.GroupBy(l => l.ExpansionId).OrderBy(g => g.Key).Select(g => g.First()).ToArray();
    }

    public Location[] Locations(bool includeReused) => includeReused ? allLocations : originalLocations;

    public bool Matches(Track track, uint? expansion, string? genre, string? area, bool includeReused)
    {
        // All tracks, unclassified tracks, and personal lists remain accessible.
        if (!includeReused && expansion is { } selected && selected != uint.MaxValue && earliest[track.Path] < selected) return false;
        return track.Locations.Any(l => (expansion == null || l.ExpansionId == expansion)
            && (genre == null || l.Category == genre) && (area == null || l.Name == area));
    }
}
