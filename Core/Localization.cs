using System.Globalization;
using System.Text.Json;

namespace AetherRadio.Core;

public static class Languages
{
    public static readonly string[] Codes = ["ja", "en", "de", "fr", "ko", "zh-Hans", "zh-Hant"];
    public static readonly string[] Names = ["日本語", "English", "Deutsch", "Français", "한국어", "简体中文", "繁體中文"];

    public static string? Normalize(string? value)
    {
        var parts = (value ?? "").Trim().Replace('_', '-').ToLowerInvariant().Split('-');
        if (parts[0] is "ja" or "en" or "de" or "fr" or "ko") return parts[0];
        if (parts[0] != "zh") return null;
        if (parts.Contains("hans")) return "zh-Hans";
        if (parts.Contains("hant")) return "zh-Hant";
        if (parts.Any(p => p is "cn" or "sg")) return "zh-Hans";
        if (parts.Any(p => p is "tw" or "hk" or "mo")) return "zh-Hant";
        return null;
    }

    public static string Resolve(string? saved, params Func<string?>[] sources)
    {
        if (Normalize(saved) is { } existing) return existing;
        foreach (var source in sources)
        {
            try { if (Normalize(source()) is { } language) return language; }
            catch { /* A missing public language source must not prevent startup. */ }
        }
        return "en";
    }
}

public sealed class Localization(Func<string?> language)
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Resources = Load();
    private string? lastInput;
    private string current = "en";
    private CultureInfo culture = CultureInfo.GetCultureInfo("en");
    public string Language
    {
        get
        {
            var input = language();
            if (input != lastInput)
            {
                lastInput = input;
                current = Languages.Normalize(input) ?? "en";
                culture = CultureInfo.GetCultureInfo(current);
            }
            return current;
        }
    }
    public CultureInfo Culture { get { _ = Language; return culture; } }
    public string this[string key] => Get(Language, key);
    public string Format(string key, params object[] args) => string.Format(Culture, this[key], args);
    public string Label(string key) => this[key] + "###" + key;
    public static string Get(string language, string key) =>
        (Resources.TryGetValue(language, out var values) && values.TryGetValue(key, out var text) ? text : null)
        ?? Resources["en"].GetValueOrDefault(key) ?? $"[{key}]";

    public string Title(Track track)
    {
        if (track.HasKnownTitle) return track.Title;
        var place = track.Locations.FirstOrDefault(l => l.ExpansionId != uint.MaxValue);
        return place == null ? Format("UnknownTitle", track.Id) : Format("UnknownTitleAt", place.Name, track.Id);
    }
    public string Expansion(Location location) => location.ExpansionId == uint.MaxValue ? this["Other"]
        : location.Expansion.StartsWith("拡張 ", StringComparison.Ordinal) ? Format("ExpansionNumber", location.ExpansionId) : location.Expansion;
    public string Area(Location location) => location.ExpansionId == uint.MaxValue ? this["Unclassified"] : location.Name;
    public string Genre(string name) => this[name switch
    {
        "討滅" => "Trials", "レイド" => "Raids", "ダンジョン" => "Dungeons", "フィールド" => "Fields",
        "ディープダンジョン" => "DeepDungeons", "PvP" => "Pvp", "その他のコンテンツ" => "OtherDuties", _ => "Other",
    }];

    public static ushort[] GlyphRanges(string code)
    {
        var chars = (string.Join("", Resources[code].Values) + string.Join("", Languages.Names) + "0123456789＋↑↓×…%·").Where(c => !char.IsControl(c)).Distinct().Order().ToArray();
        var result = new List<ushort>();
        foreach (var c in chars)
        {
            if (result.Count > 0 && c == result[^1] + 1) result[^1] = c;
            else { result.Add(c); result.Add(c); }
        }
        result.Add(0);
        return result.ToArray();
    }

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Load() => Languages.Codes.ToDictionary(code => code, code =>
    {
        using var stream = typeof(Localization).Assembly.GetManifestResourceStream($"AetherRadio.Locales.{code}.json")
            ?? throw new InvalidOperationException($"Missing locale resource: {code}");
        return (IReadOnlyDictionary<string, string>)JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!;
    });
}

public sealed class PlaybackException(string key) : InvalidOperationException(key)
{
    public string Key { get; } = key;
}
