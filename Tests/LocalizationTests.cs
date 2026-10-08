using System.Reflection;
using System.Text.RegularExpressions;
using AetherRadio;
using AetherRadio.Core;
using Dalamud.Plugin.Services;
using Newtonsoft.Json;

static class LocalizationTests
{
    public static void Run(Action<bool, string> check)
    {
        foreach (var (input, expected) in new (string, string?)[]
        {
            ("ja-JP", "ja"), ("EN_us", "en"), ("en-GB", "en"), ("de-DE", "de"), ("fr-fr", "fr"),
            ("ko_KR", "ko"), ("zh-CN", "zh-Hans"), ("zh-SG", "zh-Hans"), ("zh-TW", "zh-Hant"),
            ("zh-HK", "zh-Hant"), ("zh-MO", "zh-Hant"), ("ZH_hant_CN", "zh-Hant"), ("zh-Hans-TW", "zh-Hans"),
            ("zh", null), ("auto", null), ("xx", null), ("", null),
        }) check(Languages.Normalize(input) == expected, $"Language normalization: {input}");
        var calls = 0;
        check(Languages.Resolve("fr", () => { calls++; return "ja"; }) == "fr" && calls == 0, "Saved language prevents source lookup");
        check(Languages.Resolve(null, () => "ja", () => "en") == "ja", "Game language takes priority over Dalamud");
        check(Languages.Resolve("Auto", () => "zh", () => "zh-Hant") == "zh-Hant", "Ambiguous game language falls through to supported UI language");
        check(Languages.Resolve("invalid", () => throw new Exception(), () => "ko") == "ko", "Unavailable source falls through safely");
        check(Languages.Resolve(null, () => null, () => "xx") == "en", "All missing or unsupported sources fall back to English");
        check(Languages.Resolve(null, () => null, () => null, () => "de") == "de", "An explicitly available last source is used before English");

        foreach (var language in Languages.Codes)
        {
            var config = JsonConvert.DeserializeObject<Configuration>("""{"Version":2,"VolumePercent":170,"Repeat":0,"Favorites":[12],"Playlists":[{"Name":"私の 음악","Tracks":[12,34]}]}""")!;
            check(config.Language == null && config.InitializeLanguage(() => language), $"Unset config resolves and requests persistence: {language}");
            var roundtrip = JsonConvert.DeserializeObject<Configuration>(JsonConvert.SerializeObject(config))!;
            check(!roundtrip.InitializeLanguage(() => throw new Exception()) && roundtrip.Language == language && roundtrip.VolumePercent == 170 && roundtrip.Repeat == RepeatMode.Off && roundtrip.Favorites.Contains(12) && roundtrip.Playlists[0].Name == "私の 음악", $"Reload keeps selected language and user data: {language}");
            var catalog = new Catalog(null!, (_, _) => { });
            using var player = new Player(roundtrip, catalog, null, DispatchProxy.Create<IClientState, TestServices>(), DispatchProxy.Create<IPluginLog, TestServices>());
            player.OnLogin(); player.OnLogout(0, 0); player.OnLogin();
            check(roundtrip.Language == language && !roundtrip.InitializeLanguage(() => "en"), $"Character/session changes retain language: {language}");
            var text = new Localization(() => roundtrip.Language);
            player.Error = "LoginRequired";
            check(player.Error == text["LoginRequired"], $"Player errors use current language: {language}");
            roundtrip.Language = language == "en" ? "ja" : "en";
            check(player.Error == text["LoginRequired"], $"Existing errors follow language switching: {language}");
        }
        foreach (var saved in new[] { "ja", "en", "ja-JP", "en-US" })
        {
            var old = JsonConvert.DeserializeObject<Configuration>($$"""{"Version":1,"Language":"{{saved}}","Repeat":1} """)!;
            old.Upgrade(); old.InitializeLanguage(() => "ko");
            check(old.Language == Languages.Normalize(saved) && old.Repeat == RepeatMode.One, $"Old repeat migration preserves existing language: {saved}");
        }
        foreach (var saved in new[] { "Auto", "invalid", "zh", "" })
        {
            var old = new Configuration { Language = saved };
            check(old.InitializeLanguage(() => "de") && !old.InitializeLanguage(() => "ko") && old.Language == "de", $"Invalid/Auto config resolves only once: {saved}");
        }
        var keys = Localization.Resources["en"].Keys.Order().ToArray();
        static string[] Slots(string value) => Regex.Matches(value, @"\{\d+(?:[^{}]*)\}").Select(m => m.Value).Order().ToArray();
        foreach (var language in Languages.Codes)
        {
            var values = Localization.Resources[language];
            check(values.Keys.Order().SequenceEqual(keys) && values.Values.All(v => !string.IsNullOrWhiteSpace(v)), $"Complete nonempty translation keys: {language} ({keys.Length})");
            check(values.All(p => Slots(p.Value).SequenceEqual(Slots(Localization.Resources["en"][p.Key]))), $"Matching formatting slots: {language}");
            var text = new Localization(() => language);
            foreach (var key in keys) _ = text.Format(key, 1234, 5678);
            check(text.Format("UnknownTitle", 946).Contains("0946") && text.Format("UnknownTitleAt", "User place", 946).Contains("User place"), $"Formatted titles retain IDs and game names: {language}");
            var ranges = Localization.GlyphRanges(language);
            check(ranges.Length % 2 == 1 && ranges[^1] == 0 && ranges.Chunk(2).SkipLast(1).All(pair => pair[0] <= pair[1]), $"Valid terminated font glyph ranges: {language}");
            var unknown = new Track(946, "music/BGM_EX4.sc d", "old fallback") { HasKnownTitle = false };
            check(text.Title(unknown) == text.Format("UnknownTitle", 946) && !text.Title(unknown).Contains("BGM_EX"), $"Unknown track titles are localized without internal paths: {language}");
        }
        check(Localization.Get("missing-locale", "Play") == Localization.Resources["en"]["Play"] && Localization.Get("en", "missing-key") == "[missing-key]", "Missing resource lookup safely falls back to English then diagnostic key");
        check((int)RepeatMode.Off == 0 && (int)RepeatMode.All == 1 && (int)RepeatMode.One == 2, "Persisted enum numbers remain unchanged");
    }
}

public class TestServices : DispatchProxy
{
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod!.ReturnType.IsValueType ? Activator.CreateInstance(targetMethod.ReturnType) : null;
}
