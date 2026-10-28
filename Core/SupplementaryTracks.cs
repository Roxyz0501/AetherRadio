namespace AetherRadio.Core;

public static class SupplementaryTracks
{
    // Stable plugin-only IDs. These must never be passed to BGMSystem.SetBGM.
    public const int IdBase = 0x8000;
    public static bool IsMusicPath(string path) => path.StartsWith("music/", StringComparison.OrdinalIgnoreCase)
        && path.EndsWith(".scd", StringComparison.OrdinalIgnoreCase) && path.Length < 260
        && !path.Contains("..", StringComparison.Ordinal) && !path.Contains('\\') && !path.Contains('\0');

    public static void Add(Dictionary<ushort, Track> tracks, IEnumerable<(uint Id, string Path, string Title)> songs,
        Func<string, bool> exists, CancellationToken cancellation = default)
    {
        var paths = tracks.Values.Select(t => t.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var song in songs.OrderBy(s => s.Id))
        {
            cancellation.ThrowIfCancellationRequested();
            if (song.Id == 0 || song.Id >= IdBase || !IsMusicPath(song.Path) || paths.Contains(song.Path)) continue;
            var id = (ushort)(IdBase + song.Id);
            if (tracks.ContainsKey(id) || !exists(song.Path)) continue;
            var known = !string.IsNullOrWhiteSpace(song.Title);
            tracks.Add(id, new Track(id, song.Path, known ? song.Title : "曲名未登録") {
                OrchestrionId = song.Id, HasKnownTitle = known,
                MetadataSearch = song.Title.Replace("達", "たち", StringComparison.Ordinal),
            });
            paths.Add(song.Path);
        }
    }
}

public sealed record PlaybackRoute(ushort NativeId, string? ExpectedPath = null, string? ReplacementPath = null);

// Reuse the game's BGM lifecycle, replacing only the selected carrier's local path.
public sealed class PlaybackRouter(IEnumerable<Track> tracks)
{
    public static bool IsCarrier(Track track)
    {
        var name = track.Path[(track.Path.LastIndexOf('/') + 1)..];
        return track.OrchestrionId == null && track.Id > 1 && SupplementaryTracks.IsMusicPath(track.Path)
            && !name.Equals("BGM_Null.scd", StringComparison.OrdinalIgnoreCase)
            && !name.StartsWith("BGM_System_", StringComparison.OrdinalIgnoreCase);
    }
    private readonly Track[] carriers = tracks.Where(IsCarrier).OrderBy(t => t.Id).ToArray();
    public PlaybackRoute Resolve(Track track, ushort currentNative, IEnumerable<ushort>? activeNativeIds = null)
    {
        if (track.OrchestrionId == null) return new(track.Id);
        if (!SupplementaryTracks.IsMusicPath(track.Path)) throw new PlaybackException("TrackUnavailable");
        var unavailable = activeNativeIds?.ToHashSet() ?? [];
        unavailable.Add(currentNative);
        var carrier = carriers.FirstOrDefault(t => !unavailable.Contains(t.Id)) ?? throw new PlaybackException("EngineNotReady");
        return new(carrier.Id, carrier.Path, track.Path);
    }
    public static bool ShouldReplace(PlaybackRoute? route, ushort sceneId, string requestedPath) =>
        route?.ReplacementPath != null && route.NativeId == sceneId &&
        string.Equals(route.ExpectedPath, requestedPath, StringComparison.OrdinalIgnoreCase);
}
