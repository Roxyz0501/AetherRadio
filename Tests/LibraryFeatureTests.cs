using System.Reflection;
using AetherRadio;
using AetherRadio.Core;
using Dalamud.Plugin.Services;
using Newtonsoft.Json;

static class LibraryFeatureTests
{
    public static void Run(Action<bool, string> check)
    {
        foreach (var (type, ui, roulette, parties, expected) in new (string, string, bool, int, string)[] {
            ("レイド", "ノーマルレイド", false, 1, "ノーマルレイド"),
            ("レイド", "高難易度レイド", false, 1, "ノーマルレイド"),
            ("レイド", "アライアンスレイド", false, 3, "アライアンス"),
            ("レイド", "", true, 3, "アライアンス"),
            ("レイド", "", false, 6, "特殊コンテンツ"),
            ("絶レイド", "", false, 1, "特殊コンテンツ"),
            ("滅アライアンスレイド", "アライアンス", true, 3, "特殊コンテンツ"),
            ("禁断の地エウレカ", "", false, 1, "特殊コンテンツ"),
            ("セイブ・ザ・クイーン", "", false, 1, "特殊コンテンツ"),
            ("蜃気楼の島 クレセントアイル", "", false, 6, "特殊コンテンツ"),
            ("特殊ダンジョン探索", "", false, 1, "特殊コンテンツ"),
            ("ディープダンジョン", "", false, 1, "ディープダンジョン"),
            ("ダンジョン", "", false, 1, "ダンジョン"),
            ("討伐・討滅戦", "", false, 1, "討滅"),
            ("PvP", "", false, 1, "PvP"),
        }) check(DutyGenre.Classify(type, ui, roulette, parties) == expected, $"Duty genre: {type}/{ui}/{parties}");
        var reused = new Track(1, "music/shared.scd", "Reused");
        reused.Locations.AddRange([new(1, "One", "フィールド", "First"), new(3, "Three", "ノーマルレイド", "Later")]);
        var alias = new Track(2, "MUSIC/SHARED.SCD", "Alias"); alias.Locations.Add(new(3, "Three", "特殊コンテンツ", "Alias area"));
        var arrangement = new Track(3, "music/arrangement.scd", "Reused"); arrangement.Locations.Add(new(3, "Three", "アライアンス", "New arrangement"));
        var other = new Track(4, "music/other.scd", "Other"); other.Locations.Add(new(uint.MaxValue, "Other", "その他", "Unknown"));
        var index = new TrackLibrary([reused, alias, arrangement, other]);
        check(index.Matches(reused, 3, null, null, true) && !index.Matches(reused, 3, null, null, false), "Reuse toggle hides later expansion associations");
        check(index.Matches(reused, 1, "フィールド", "First", false) && !index.Matches(reused, 1, "ノーマルレイド", null, false), "Earliest expansion and related genre filters remain accurate");
        check(!index.Matches(alias, 3, null, null, false), "Duplicate audio paths across BGM IDs share earliest expansion");
        check(index.Matches(arrangement, 3, null, null, false), "A different arrangement with the same title remains visible");
        check(index.Matches(other, uint.MaxValue, null, null, false) && index.Matches(alias, null, null, null, false), "Other and All retain unclassified and reused tracks");
        check(!index.Locations(false).Any(l => l.Name == "Later" || l.Name == "Alias area") && index.Locations(true).Length == 5, "Genre and duty choices follow the duplicate toggle");
        check(ReferenceEquals(index.Locations(false), index.Locations(false)), "Unchanged filter options reuse the immutable index");
        var old = JsonConvert.DeserializeObject<Configuration>("{\"Version\":2}")!;
        check(old.ShowReusedTracks, "Existing configurations keep reused tracks visible");
        old.ShowReusedTracks = false;
        check(!JsonConvert.DeserializeObject<Configuration>(JsonConvert.SerializeObject(old))!.ShowReusedTracks, "Duplicate visibility preference survives reload");

        var client = DispatchProxy.Create<IClientState, SessionService>(); ((SessionService)(object)client).LoggedIn = true;
        var catalog = new Catalog(null!, (_, _) => { });
        for (ushort i = 1; i <= 35; i++) catalog.Tracks.Add(i, new Track(i, $"music/{i}", $"Track {i}"));
        var engine = new SessionEngine(new TestMusicVolume()); var config = new Configuration();
        var saved = JsonConvert.SerializeObject(config);
        using var player = new Player(config, catalog, engine, client, DispatchProxy.Create<IPluginLog, SessionService>());
        foreach (var track in catalog.Tracks.Values) player.Start(track, [track.Id]);
        check(player.Recent.Count == 30 && player.Recent[0] == 35 && player.Recent[^1] == 6, "Recent history caps at 30 and evicts oldest tracks");
        player.Start(catalog.Tracks[8], [8]);
        check(player.Recent[0] == 8 && player.Recent.Count == 30 && player.Recent.Distinct().Count() == 30, "Replaying moves a track to the front without duplication");
        var revision = player.RecentRevision; player.Resume();
        check(player.RecentRevision == revision, "Resuming the newest track does not invalidate the history cache");
        engine.Ready = false; player.Start(catalog.Tracks[1], [1]);
        check(player.RecentRevision == revision && !player.Recent.Contains((ushort)1), "Failed playback is not recorded");
        player.Stop();
        check(player.Recent.Count == 30 && JsonConvert.SerializeObject(config) == saved, "Stopping retains session history without changing saved configuration");
        player.OnLogout(0, 0);
        check(player.Recent.Count == 0 && player.RecentRevision > revision, "Logout clears history and invalidates the recent view");
    }
}
