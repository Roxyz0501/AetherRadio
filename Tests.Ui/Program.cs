using System.Reflection;
using System.Runtime.InteropServices;
using System.Numerics;
using AetherRadio;
using AetherRadio.Core;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Game.Command;
using Dalamud.Bindings.ImGui;
using Dalamud.Plugin.Services;
using Lumina;
using Lumina.Data;
using Lumina.Excel;

unsafe partial class Program
{
    static void Main(string[] args)
    {
        var output = Path.GetFullPath(args[1]); Directory.CreateDirectory(output);
        NativeLibrary.SetDllImportResolver(typeof(ImGui).Assembly, (name, _, _) => name.Contains("cimgui") ? NativeLibrary.Load(Path.Combine(AppContext.BaseDirectory, "cimgui.dll")) : 0);
        using var game = new GameData(args[0], new LuminaOptions { DefaultExcelLanguage = Language.Japanese });
        var data = new Data(game);
        var catalog = new Catalog(data, (e, s) => throw new Exception(s, e)); catalog.Load();
        var initialReads = (data.SheetReads, data.FileChecks);
        var config = new Configuration { Corner = 4, Language = "ja" };
        var client = DispatchProxy.Create<IClientState, Dummy>();
        ((Dummy)(object)client).LoggedIn = true;
        var engine = new UiPlayback();
        var player = new Player(config, catalog, engine, client, DispatchProxy.Create<IPluginLog, Dummy>(), () => engine);
        var saves = 0; var changes = 0;
        var ui = new RadioUi(config, catalog, player, () => saves++) { CatalogReady = true, LanguageChanged = () => changes++ };
        ImGui.CreateContext(); var io = ImGui.GetIO();
        io.IniFilename = null; io.DisplaySize = new Vector2(1440, 900); io.DeltaTime = 1f / 60;
        io.ConfigInputTrickleEventQueue = false; io.ConfigWindowsMoveFromTitleBarOnly = true;
        var fonts = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        var font = io.Fonts.AddFontFromFileTTF(Path.Combine(fonts, "meiryo.ttc"), 16, default, io.Fonts.GetGlyphRangesJapanese());
        var pinned = new List<GCHandle>();
        foreach (var (code, file) in new[] { ("en", "segoeui.ttf"), ("de", "segoeui.ttf"), ("fr", "segoeui.ttf"), ("ko", "malgun.ttf"), ("zh-Hans", "msyh.ttc"), ("zh-Hant", "msjh.ttc"), ("ja", "meiryo.ttc") })
        {
            var pin = GCHandle.Alloc(Localization.GlyphRanges(code), GCHandleType.Pinned); pinned.Add(pin);
            var raw = new SafeFontConfig { SizePx = 16, MergeFont = font }.Raw;
            io.Fonts.AddFontFromFileTTF(Path.Combine(fonts, file), 16, raw, (ushort*)pin.AddrOfPinnedObject());
        }
        io.Fonts.Build();
        foreach (var pin in pinned) pin.Free();
        foreach (var code in Languages.Codes)
        {
            var missing = (string.Join("", Localization.Resources[code].Values) + string.Join("", Languages.Names)).Where(c => !char.IsControl(c)).Distinct().Where(c => font.FindGlyphNoFallback(c) == null).ToArray();
            Check(missing.Length == 0, $"Glyph coverage: {code}; missing={new string(missing)}");
        }
        var textures = new Dictionary<ulong, (nint, int, int)>();
        for (int t = 0; t < io.Fonts.Textures.Size; t++)
        {
            byte* atlas; int w = 0, h = 0, bpp = 0;
            io.Fonts.GetTexDataAsRGBA32(t, &atlas, &w, &h, &bpp);
            io.Fonts.SetTexID(t, new ImTextureID((ulong)t + 1)); textures[(ulong)t + 1] = ((nint)atlas, w, h);
        }
        void Frame() { ImGui.NewFrame(); ui.Draw(); ImGui.Render(); }
        void Click(float x, float y)
        {
            io.AddMousePosEvent(x, y); Frame(); io.AddMouseButtonEvent(0, true); Frame(); io.AddMouseButtonEvent(0, false); Frame();
            for (var i = 0; i < 3; i++) Frame();
        }
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var search = typeof(RadioUi).GetField("search", flags)!;
        var newList = typeof(RadioUi).GetField("newList", flags)!;
        var standard = typeof(RadioUi).Assembly.GetType("Roxyz0501.Dalamud.CommonUi.StandardPluginTabs", true)!;
        var standardLanguages = (IReadOnlyList<(string Code, string SelfName)>)standard.GetProperty("Languages")!.GetValue(null)!;
        Check(standardLanguages.Select(l => l.Code).SequenceEqual(Languages.Codes) && standardLanguages.Select(l => l.SelfName).SequenceEqual(Languages.Names), "Canonical language order and native names match stored configuration codes");
        void LanguageTab()
        {
            var text = new Localization(() => config.Language);
            var x = 238 + ImGui.CalcTextSize(text["Library"]).X + ImGui.CalcTextSize(text["Settings"]).X + 55;
            Click(x, 188);
        }
        (Vector2 Min, Vector2 Max) BoundsForColor(Vector4 color)
        {
            var target = ImGui.ColorConvertFloat4ToU32(color);
            var min = new Vector2(float.MaxValue); var max = new Vector2(float.MinValue);
            var draw = ImGui.GetDrawData();
            for (var n = 0; n < draw.CmdListsCount; n++)
            {
                var vertices = new ImDrawListPtr(draw.CmdLists[n]).VtxBuffer;
                for (var v = 0; v < vertices.Size; v++)
                    if (vertices[v].Col == target) { min = Vector2.Min(min, vertices[v].Pos); max = Vector2.Max(max, vertices[v].Pos); }
            }
            if (min.X == float.MaxValue) throw new Exception("Expected shared Support color was not drawn");
            return (min, max);
        }
        void CheckTabOrder(string language)
        {
            var bars = ImGui.GetCurrentContext().TabBars.Buf;
            for (var n = 0; n < bars.Size; n++)
            {
                var bar = new ImGuiTabBarPtr(bars.Data + n);
                if (bar.Tabs.Size != 4) continue;
                var names = Enumerable.Range(0, 4).Select(i => Marshal.PtrToStringUTF8((nint)(bar.TabsNames.Buf.Data + bar.Tabs[i].NameOffset))!).ToArray();
                if (!names[0].EndsWith("###Library", StringComparison.Ordinal)) continue;
                var expected = new[] { "Library", "Settings", "LanguageSettings", "Support" };
                Check(names.Select(s => s[(s.IndexOf("###", StringComparison.Ordinal) + 3)..]).SequenceEqual(expected)
                    && Enumerable.Range(1, 3).All(i => bar.Tabs[i].Offset >= bar.Tabs[i - 1].Offset + bar.Tabs[i - 1].Width),
                    $"Main tab order is Library, Settings, Language settings, Support: {language}");
                Check(bar.SelectedTabId == bar.Tabs[1].ID, $"Installer settings opens Settings directly: {language}");
                return;
            }
            throw new Exception("Main tab bar was not found");
        }
        foreach (var code in Languages.Codes)
        {
            config.Language = code; ui.OpenSettings(); for (var i = 0; i < 3; i++) Frame();
            CheckTabOrder(code);
            Render(ImGui.GetDrawData(), textures, Path.Combine(output, code + "-settings.bmp"));
            search.SetValue(ui, "水車"); newList.SetValue(ui, "私の 음악");
            // Main window tab positions derive from the current font metrics.
            Click(250, 188); Frame();
            Check(search.GetValue(ui)?.ToString() == "水車" && newList.GetValue(ui)?.ToString() == "私の 음악", $"Input survives language/tab switching: {code}");
            Render(ImGui.GetDrawData(), textures, Path.Combine(output, code + "-library.bmp"));
            LanguageTab();
            Render(ImGui.GetDrawData(), textures, Path.Combine(output, code + "-language.bmp"));
            // Locate the actual gold tab geometry; translated label widths vary.
            var tab = BoundsForColor(new Vector4(0.55f, 0.34f, 0.05f, 1));
            Click((tab.Min.X + tab.Max.X) / 2, (tab.Min.Y + tab.Max.Y) / 2);
            io.AddMousePosEvent(200, 100); Frame();
            var button = BoundsForColor(new Vector4(0.78f, 0.39f, 0.05f, 1));
            Console.WriteLine($"SUPPORT: {code}; min={button.Min}; max={button.Max}");
            Check(button.Min.Y > 200 && button.Max.X - button.Min.X >= 298.9f && Math.Abs(button.Max.Y - button.Min.Y - 46) <= 1.1f,
                $"Support tab opens with canonical button dimensions: {code}");
            Render(ImGui.GetDrawData(), textures, Path.Combine(output, code + "-support.bmp"));
        }
        config.Language = "ja"; ui.OpenSettings(); for (var i = 0; i < 3; i++) Frame();
        LanguageTab();
        // Exercise the real combo rather than just setting the language property.
        var before = saves;
        for (var i = 1; i <= 7; i++)
        {
            Click(420, 218);
            Render(ImGui.GetDrawData(), textures, Path.Combine(output, "language-dropdown.bmp"));
            Click(275, 254 + 25 * (i % 7));
            Check(config.Language == Languages.Codes[i % 7] && saves == before + i && changes == i, $"Language combo switches and saves: {Languages.Codes[i % 7]}");
            Check(search.GetValue(ui)?.ToString() == "水車" && newList.GetValue(ui)?.ToString() == "私の 음악", "Pending input retained after combo change");
        }
        Click(420, 218); Click(275, 254);
        Check(saves == before + 7 && changes == 7, "Reselecting the saved language does not write configuration");
        for (var i = 0; i < 120; i++) Frame();
        Check(saves == before + 7, "Unchanged language frames do not write configuration");
        Check((data.SheetReads, data.FileChecks) == initialReads, "Tab navigation and language changes perform zero catalogue reads");
        Console.WriteLine($"DATA: initial sheet requests={initialReads.SheetReads}; file checks={initialReads.FileChecks}; subsequent reads=0; unchanged saves=0");
        Render(ImGui.GetDrawData(), textures, Path.Combine(output, "language-changed.bmp"));
        var manager = DispatchProxy.Create<ICommandManager, CommandStub>();
        var stub = (CommandStub)(object)manager;
        using (var binding = new PluginCommands(manager, ui.Open, player.Stop, new Localization(() => config.Language)))
        {
            config.Language = "de"; binding.RefreshLanguage();
            Check(stub.Handlers[PluginCommands.Main].HelpMessage == Localization.Get("de", "CommandHelp"), "Command help follows selected language");
            config.Language = "ko"; binding.RefreshLanguage();
            Check(stub.Handlers[PluginCommands.Main].HelpMessage == Localization.Get("ko", "CommandHelp"), "Command help can switch repeatedly");
        }
        Check(stub.Handlers.Count == 0, "Localized command handlers are cleaned up");
        // Exercise the new catalogue views without native audio hooks.
        config.Language = "ja"; search.SetValue(ui, "");
        Click(250, 188); Frame();
        var selected = catalog.Tracks.Values.First(t => t.Locations.Select(l => l.ExpansionId).Distinct().Count() > 1 && t.Locations.All(l => l.ExpansionId != uint.MaxValue));
        player.Start(selected, [selected.Id]);
        Click(300, 455);
        var visibleField = typeof(RadioUi).GetField("visible", flags)!;
        Check(((List<Track>)visibleField.GetValue(ui)!).Single().Id == selected.Id, "Recent navigation displays the played song");
        var recentView = visibleField.GetValue(ui); var unchangedSaves = saves;
        for (var i = 0; i < 120; i++) Frame();
        Check(ReferenceEquals(recentView, visibleField.GetValue(ui)) && saves == unchangedSaves, "Unchanged recent view reuses filtered rows without saving");
        player.OnLogout(0, 0); Frame();
        Check(((List<Track>)visibleField.GetValue(ui)!).Count == 0, "Logout refreshes the open recent view");
        player.OnLogin();
        var mode = typeof(RadioUi).GetField("library", flags)!;
        var exp = typeof(RadioUi).GetField("expansion", flags)!;
        mode.SetValue(ui, 0); exp.SetValue(ui, selected.Locations.Max(l => l.ExpansionId));
        var refresh = typeof(RadioUi).GetMethod("RefreshVisible", flags)!;
        config.ShowReusedTracks = true; refresh.Invoke(ui, [null]);
        Check(((List<Track>)visibleField.GetValue(ui)!).Contains(selected), "Later expansion includes reused audio when enabled");
        config.ShowReusedTracks = false; refresh.Invoke(ui, [null]);
        Check(!((List<Track>)visibleField.GetValue(ui)!).Contains(selected), "Duplicate preference immediately invalidates the visible-track cache");
        config.Favorites.Add(selected.Id); mode.SetValue(ui, 1); refresh.Invoke(ui, [null]);
        Check(((List<Track>)visibleField.GetValue(ui)!).Contains(selected), "Favorite view retains a reused song despite the duplicate preference");
        config.Favorites.Remove(selected.Id); config.ShowReusedTracks = true; exp.SetValue(ui, null); mode.SetValue(ui, 0);
        Check((data.SheetReads, data.FileChecks) == initialReads && saves == unchangedSaves, "History updates and duplicate filtering cause zero catalogue reads or configuration writes");
        Check(initialReads.SheetReads <= 12, "Initial sheet requests stay within the catalogue read budget");
        // Mini-player remains operable with a longer translation.
        config.Language = "de";
        typeof(RadioUi).GetField("open", flags)!.SetValue(ui, false); Frame();
        var oldRepeat = config.Repeat;
        Click(1213, 784);
        Check(config.Repeat != oldRepeat && config.Repeat == player.Queue.Repeat, "Localized mini repeat button updates playback");
        var volume = config.VolumePercent;
        Click(1370, 780);
        Check(config.VolumePercent != volume && config.VolumePercent > 100, "Localized mini volume control remains operable");
        player.Start(selected, [selected.Id]); Frame();
        var miniSave = saves;
        Click(1395, 848);
        Check(config.Favorites.Contains(selected.Id) && saves == miniSave + 1, "Mini favorite button adds the current track with one save");
        Click(1395, 848);
        Check(!config.Favorites.Contains(selected.Id) && saves == miniSave + 2, "Mini favorite button removes the current track with one save");
        var stops = engine.Stops;
        Click(1150, 848);
        Check(!player.IsPlaying && engine.Stops == stops + 1 && player.Current == selected, "Mini restore button stops the override while retaining manual resume");
        Check(saves == miniSave + 2, "Restoring game music does not save configuration");
        io.AddMousePosEvent(1100, 670); Frame(); io.AddMouseButtonEvent(0, true); Frame();
        io.AddMousePosEvent(1000, 580); Frame(); io.AddMouseButtonEvent(0, false); Frame();
        Check(config.Corner == 0 && config.MiniPosition != null, "Localized mini header remains draggable");
        for (var i = 0; i < 5; i++) Frame();
        Render(ImGui.GetDrawData(), textures, Path.Combine(output, "de-mini.bmp"));
        ImGui.DestroyContext(); player.Dispose();
    }
    static void Check(bool value, string label) { if (!value) throw new Exception(label); Console.WriteLine("PASS: " + label); }
}

sealed class Data(GameData game) : ITrackData
{
    public int SheetReads { get; private set; }
    public int FileChecks { get; private set; }
    public ExcelSheet<T> GetExcelSheet<T>(Language? language = null) where T : struct, IExcelRow<T> { SheetReads++; return game.GetExcelSheet<T>(language)!; }
    public bool FileExists(string path) { FileChecks++; return game.FileExists(path); }
}
public class Dummy : DispatchProxy
{
    public bool LoggedIn;
    protected override object? Invoke(MethodInfo? method, object?[]? args) => method!.Name == "get_IsLoggedIn" ? LoggedIn : method.ReturnType.IsValueType ? Activator.CreateInstance(method.ReturnType) : null;
}
public class CommandStub : DispatchProxy
{
    public Dictionary<string, CommandInfo> Handlers { get; } = [];
    protected override object? Invoke(MethodInfo? method, object?[]? args) => method!.Name switch
    {
        "AddHandler" => Handlers.TryAdd((string)args![0]!, (CommandInfo)args[1]!),
        "RemoveHandler" => Handlers.Remove((string)args![0]!),
        _ => method.ReturnType.IsValueType ? Activator.CreateInstance(method.ReturnType) : null,
    };
}

sealed class UiPlayback : IBgmPlayback
{
    public ushort? Current { get; private set; }
    public bool Locked { get; set; }
    public int VolumePercent { get; set; }
    public int Stops;
    public void Play(ushort id) => Current = id;
    public void Stop() { Current = null; Stops++; }
    public void ResetSession() => Current = null;
    public void ValidateOwner() { }
    public void Dispose() => Current = null;
}
