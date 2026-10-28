using AetherRadio.Core;
using AetherRadio;

static class SupplementaryTests
{
    public static void Run(Action<bool, string> check)
    {
        var tracks = new Dictionary<ushort, Track> { [1] = new(1, "music/main.scd", "Main"), [2] = new(2, "music/second.scd", "Second") };
        var checks = 0;
        SupplementaryTracks.Add(tracks, [(857u, "music/orch/907.scd", "闘う者達"), (858u, "music/orch/908.scd", "更に闘う者達"),
            (859u, "music/orch/missing.scd", "Missing"), (860u, "music/main.scd", "Duplicate"), (861u, "MUSIC/ORCH/907.SCD", "Duplicate"),
            (862u, "sound/effect.scd", "Not music"), (863u, "music/../bad.scd", "Traversal"), (40000u, "music/overflow.scd", "Overflow")],
            p => { checks++; return !p.Contains("missing"); });
        check(tracks.Count == 4 && checks == 3, "Supplement: unique installed music paths only, with bounded file checks");
        var song = tracks[(ushort)(SupplementaryTracks.IdBase + 857)];
        check(song.Title == "闘う者達" && song.MetadataSearch.Contains("闘う者たち") && song.OrchestrionId == 857, "Supplement: official title and hiragana search alias are retained");
        var persistedId = song.Id;
        var saved = ConfigurationCodec.Decode(ConfigurationCodec.Encode(new Configuration { Favorites = [persistedId], Playlists = [new() { Tracks = [persistedId] }] }));
        check(saved.Favorites.Contains(persistedId) && saved.Playlists[0].Tracks[0] == persistedId, "Supplement: favorites and playlists round-trip stable IDs");

        SupplementaryTracks.Add(tracks, [(857u, song.Path, song.Title)], _ => throw new Exception("Unexpected repeated lookup"));
        check(tracks.Count == 4 && tracks[persistedId] == song, "Supplement: repeated collection preserves stable playlist IDs");
        var router = new PlaybackRouter(tracks.Values);
        var route = router.Resolve(song, 1);
        check(route.NativeId == 2 && route.ReplacementPath == song.Path && route.NativeId != song.Id, "Routing: plugin-only song ID never reaches SetBGM");
        var next = router.Resolve(tracks[(ushort)(SupplementaryTracks.IdBase + 858)], route.NativeId);
        check(next.NativeId == 1 && next.ReplacementPath != route.ReplacementPath, "Routing: consecutive supplementary songs alternate carrier IDs");
        check(PlaybackRouter.ShouldReplace(route, 2, "MUSIC/SECOND.SCD") && !PlaybackRouter.ShouldReplace(route, 1, "music/second.scd") &&
            !PlaybackRouter.ShouldReplace(route, 2, "music/other.scd") && !PlaybackRouter.ShouldReplace(null, 2, "music/second.scd"),
            "Routing: replacement matches only the selected scene and carrier path");
        check(router.Resolve(tracks[1], 2) == new PlaybackRoute(1), "Routing: ordinary songs retain their original native IDs");
        var collision = new Dictionary<ushort, Track> { [persistedId] = new(persistedId, "music/real.scd", "Native") };
        SupplementaryTracks.Add(collision, [(857u, song.Path, song.Title)], _ => true);
        check(collision[persistedId].OrchestrionId == null && collision.Count == 1, "Supplement: native ID collisions never overwrite an existing song");
        using var cancel = new CancellationTokenSource(); cancel.Cancel(); var cancelled = false;
        try { SupplementaryTracks.Add(new(), [(1u, "music/a.scd", "A")], _ => throw new Exception("Unexpected lookup"), cancel.Token); }
        catch (OperationCanceledException) { cancelled = true; }
        check(cancelled, "Supplement: cancellation stops before filesystem reads");
    }
}
