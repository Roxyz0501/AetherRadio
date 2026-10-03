using AetherRadio;
using AetherRadio.Core;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.Sheets;

var passed = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException("FAIL: " + name);
    passed++; Console.WriteLine("PASS: " + name);
}

var q = new PlaybackQueue(new Random(42));
Check(q.Next() == null && q.Previous() == null, "Empty queue is safe");
q.Start([1, 2, 2, 3], 1);
Check(q.Count == 3 && q.Next() == 2 && q.Next() == 3, "Ordered queue deduplicates and advances");
q.Repeat = RepeatMode.Off;
Check(q.Next(true) == null, "Repeat off stops at queue end");
Check(q.Next() == 1, "Explicit next wraps independently of automatic stop");
Check(q.Previous() == 3, "Previous follows playback history");
q.Repeat = RepeatMode.One;
Check(q.Next(true) == 3 && q.Next() == 1, "Repeat one only retains automatic transitions");
q.Start([], 7);
q.Shuffle = true;
Check(q.Next() == 7, "Single-item shuffle terminates safely");
q.Repeat = RepeatMode.Off;
Check(q.Next(true) == null, "Single-item shuffle stops with repeat off");
q.Repeat = RepeatMode.All;
q.Start(Enumerable.Range(1, 30).Select(x => (ushort)x), 1);
var cycle = new List<ushort> { 1 };
for (var i = 0; i < 29; i++) cycle.Add(q.Next(true)!.Value);
Check(cycle.Distinct().Count() == 30, "Shuffle visits all tracks exactly once in a cycle");
var last = q.Current;
Check(q.Next(true) != last, "Shuffle does not repeat at cycle boundary");
q.Repeat = RepeatMode.Off;
q.Start([10, 20, 30], 10);
Check(q.Next(true) != 10 && q.Next(true) != 10 && q.Next(true) == null, "Shuffle repeat off exhausts the bag");
q.Start([5, 6], 8);
Check(q.Count == 3 && q.Current == 8, "Selected track is retained when filter changes");

if (args.Length > 0)
{
    using var game = new GameData(args[0], new LuminaOptions { DefaultExcelLanguage = Language.Japanese, CacheFileResources = false });
    var catalogue = new Catalog(new OfflineData(game), (e, label) => Console.WriteLine($"WARNING {label}: {e.Message}"));
    catalogue.Load();
    var expected = (game.GetExcelSheet<BGM>() ?? throw new InvalidOperationException("Missing BGM sheet")).Count(x => x.RowId is > 0 and <= ushort.MaxValue && !string.IsNullOrWhiteSpace(x.File.ToString()) && game.FileExists(x.File.ToString()));
    Check(catalogue.Tracks.Count == expected, "Every installed nonempty BGM row is selectable");
    Check(catalogue.Tracks.Values.All(x => x.Locations.Count > 0), "Every track has a category, including Other");
    Check(catalogue.Warnings.Count == 0, "All catalogue enrichment sheets loaded");
    Check(catalogue.Tracks.Values.Any(x => x.Locations.Any(l => l.Category == "フィールド")), "Field BGM mapping is populated");
    Check(catalogue.Tracks.Values.Any(x => x.Locations.Any(l => l.Category == "コンテンツ")), "Duty BGM mapping is populated");
    var named = catalogue.Tracks.Values.Count(x => !x.Title.StartsWith("BGM "));
    Check(named > 500, "Bundled metadata resolves Japanese song titles");
    var other = catalogue.Tracks.Values.Count(x => x.Locations.All(l => l.Category == "その他"));
    Console.WriteLine($"CATALOG: {catalogue.Tracks.Count} tracks, {named} named, {other} other, {catalogue.MissingFiles} missing files");
    foreach (var exp in catalogue.Tracks.Values.SelectMany(t => t.Locations.Select(l => (t.Id, l.ExpansionId, l.Expansion))).Distinct().GroupBy(x => (x.ExpansionId, x.Expansion)).OrderBy(x => x.Key.ExpansionId))
        Console.WriteLine($"  {exp.Key.Expansion}: {exp.Count()}");
}
Console.WriteLine($"{passed} checks passed.");

sealed class OfflineData(GameData game) : ITrackData
{
    public ExcelSheet<T> GetExcelSheet<T>(Language? language = null) where T : struct, IExcelRow<T> => game.GetExcelSheet<T>(language) ?? throw new InvalidOperationException($"Missing sheet: {typeof(T).Name}");
    public bool FileExists(string path) => game.FileExists(path);
}
