using AetherRadio.Core;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Data;

namespace AetherRadio;

public interface ITrackData
{
    ExcelSheet<T> GetExcelSheet<T>(Language? language = null) where T : struct, IExcelRow<T>;
    bool FileExists(string path);
}

public sealed class Catalog(ITrackData data, System.Action<Exception, string> log)
{
    public Dictionary<ushort, Track> Tracks { get; } = [];
    public int MissingFiles { get; private set; }
    public List<string> Warnings { get; } = [];

    public void Load()
    {
        var metadata = MetadataStore.Load();
        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Enrich("曲名", () => {
            var paths = data.GetExcelSheet<OrchestrionPath>();
            foreach (var song in data.GetExcelSheet<Orchestrion>())
            {
                var path = paths.GetRowOrDefault(song.RowId)?.File.ToString();
                var title = song.Name.ToString();
                if (!string.IsNullOrWhiteSpace(path) && !string.IsNullOrWhiteSpace(title)) names.TryAdd(path, title);
            }
        });
        foreach (var bgm in data.GetExcelSheet<BGM>())
        {
            var path = bgm.File.ToString();
            if (bgm.RowId is 0 or > ushort.MaxValue || string.IsNullOrWhiteSpace(path)) continue;
            if (!data.FileExists(path)) { MissingFiles++; continue; }
            var extra = metadata.GetValueOrDefault((ushort)bgm.RowId);
            var title = extra?.Title ?? names.GetValueOrDefault(path);
            Tracks.Add((ushort)bgm.RowId, new Track((ushort)bgm.RowId, path, title ?? "曲名未登録") { HasKnownTitle = title != null, MetadataSearch = extra?.SearchTerms ?? "", DurationSeconds = extra?.Seconds });
        }
        Enrich("フィールド分類", () => {
            foreach (var territory in data.GetExcelSheet<TerritoryType>())
            {
                var duty = territory.ContentFinderCondition.ValueNullable;
                var dutyName = duty?.Name.ToString();
                var areaName = territory.PlaceName.ValueNullable?.Name.ToString();
                var isDuty = !string.IsNullOrWhiteSpace(dutyName);
                var name = isDuty ? dutyName! : areaName;
                if (string.IsNullOrWhiteSpace(name)) continue;
                var loc = new Location(territory.ExVersion.RowId, Expansion(territory.ExVersion.RowId), isDuty ? Genre(duty!.Value) : "フィールド", name);
                Resolve(territory.BGM, loc, new HashSet<(Type?, uint)>());
            }
        });
        Enrich("コンテンツ分類", () => {
            foreach (var instance in data.GetExcelSheet<InstanceContent>())
            {
                if (instance.ContentFinderCondition.ValueNullable is not { } duty) continue;
                var name = duty.Name.ToString();
                if (string.IsNullOrWhiteSpace(name)) continue;
                var loc = new Location(duty.RequiredExVersion.RowId, Expansion(duty.RequiredExVersion.RowId), Genre(duty), name);
                Add(instance.BGM.RowId, loc);
                Add(instance.WinBGM.RowId, loc);
            }
        });
        Enrich("曲名・場所の補助分類", () => {
            var englishTerritories = data.GetExcelSheet<TerritoryType>(Language.English);
            var englishDuties = data.GetExcelSheet<ContentFinderCondition>(Language.English);
            foreach (var territory in data.GetExcelSheet<TerritoryType>())
            {
                var duty = territory.ContentFinderCondition.ValueNullable;
                var isDuty = duty is { } d && !string.IsNullOrWhiteSpace(d.Name.ToString());
                var name = isDuty ? duty!.Value.Name.ToString() : territory.PlaceName.ValueNullable?.Name.ToString();
                var english = isDuty ? englishDuties.GetRowOrDefault(duty!.Value.RowId)?.Name.ToString()
                    : englishTerritories.GetRowOrDefault(territory.RowId)?.PlaceName.ValueNullable?.Name.ToString();
                if (string.IsNullOrWhiteSpace(name)) continue;
                var loc = new Location(territory.ExVersion.RowId, Expansion(territory.ExVersion.RowId), isDuty ? Genre(duty!.Value) : "フィールド", name, true);
                foreach (var track in Tracks.Values)
                {
                    var japaneseMatch = name.Length >= 4 && track.Title.Contains(name, StringComparison.OrdinalIgnoreCase);
                    var englishMatch = english is { Length: >= 5 } && ContainsName(track.MetadataSearch, english);
                    if ((japaneseMatch || englishMatch) && !track.Locations.Any(x => x.ExpansionId == loc.ExpansionId && x.Category == loc.Category && x.Name == loc.Name))
                        track.Locations.Add(loc);
                }
            }
        });
        foreach (var track in Tracks.Values.Where(t => t.Locations.Count == 0))
            track.Locations.Add(new Location(uint.MaxValue, "その他", "その他", "未分類のBGM"));
        foreach (var track in Tracks.Values.Where(t => !t.HasKnownTitle))
        {
            var location = track.Locations.FirstOrDefault(l => l.ExpansionId != uint.MaxValue);
            track.Title = location == null ? $"曲名未登録（{track.Id:D4}）" : $"{location.Name}（曲名未登録・{track.Id:D4}）";
        }
    }

    private static string Genre(ContentFinderCondition duty)
    {
        var name = duty.ContentType.ValueNullable?.Name.ToString();
        if (string.IsNullOrWhiteSpace(name)) return "その他";
        if (name.Contains("討伐") || name.Contains("討滅")) return "討滅";
        if (name.Contains("レイド")) return "レイド";
        if (name.Contains("ダンジョン")) return name.Contains("ディープ") ? "ディープダンジョン" : "ダンジョン";
        return name == "PvP" ? "PvP" : "その他のコンテンツ";
    }

    private string Expansion(uint id) => data.GetExcelSheet<ExVersion>().GetRowOrDefault(id)?.Name.ToString() is { Length: > 0 } name ? name : $"拡張 {id}";
    private static bool ContainsName(string text, string name)
    {
        var position = 0;
        while ((position = text.IndexOf(name, position, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            var end = position + name.Length;
            if ((position == 0 || !char.IsLetterOrDigit(text[position - 1])) && (end == text.Length || !char.IsLetterOrDigit(text[end]))) return true;
            position++;
        }
        return false;
    }
    private void Add(uint id, Location location)
    {
        if (id <= ushort.MaxValue && Tracks.TryGetValue((ushort)id, out var track) && !track.Locations.Contains(location)) track.Locations.Add(location);
    }
    private void Resolve(RowRef row, Location loc, HashSet<(Type?, uint)> visited)
    {
        if (row.RowId == 0 || !visited.Add((row.RowType, row.RowId))) return;
        if (row.Is<BGM>()) Add(row.RowId, loc);
        else if (row.TryGetValue<BGMSituation>(out var situation))
        {
            Add(situation.DaytimeID.RowId, loc); Add(situation.NightID.RowId, loc);
            Add(situation.BattleID.RowId, loc); Add(situation.DaybreakID.RowId, loc); Add(situation.TwilightID.RowId, loc);
        }
        else if (row.TryGetValueSubrow<BGMSwitch>(out var switches))
            foreach (var entry in switches) Resolve(entry.BGM, loc, visited);
    }
    private void Enrich(string label, System.Action action)
    {
        try { action(); }
        catch (Exception e) { Warnings.Add($"{label}を一部取得できません。未分類の曲は「その他」で検索できます。"); log(e, label); }
    }
}
