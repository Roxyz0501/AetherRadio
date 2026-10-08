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
        var catalog = new Catalog(new Data(game), (e, s) => throw new Exception(s, e)); catalog.Load();
        var config = new Configuration { Corner = 4, Language = "ja" };
        var player = new Player(config, catalog, null, DispatchProxy.Create<IClientState, Dummy>(), DispatchProxy.Create<IPluginLog, Dummy>());
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
        foreach (var code in Languages.Codes)
        {
            config.Language = code; ui.OpenSettings(); for (var i = 0; i < 3; i++) Frame();
            Render(ImGui.GetDrawData(), textures, Path.Combine(output, code + "-settings.bmp"));
            search.SetValue(ui, "水車"); newList.SetValue(ui, "私の 음악");
            var text = new Localization(() => code);
            // Main window tab positions derive from the current font metrics.
            Click(250, 188); Frame();
            Check(search.GetValue(ui)?.ToString() == "水車" && newList.GetValue(ui)?.ToString() == "私の 음악", $"Input survives language/tab switching: {code}");
            Render(ImGui.GetDrawData(), textures, Path.Combine(output, code + "-library.bmp"));
            var supportX = 238 + ImGui.CalcTextSize(text["Library"]).X + ImGui.CalcTextSize(text["Settings"]).X + 55;
            Click(supportX, 188);
            Render(ImGui.GetDrawData(), textures, Path.Combine(output, code + "-support.bmp"));
        }
        config.Language = "ja"; ui.OpenSettings(); for (var i = 0; i < 3; i++) Frame();
        // Exercise the real combo rather than just setting the language property.
        var before = saves;
        for (var i = 1; i <= 7; i++)
        {
            Click(420, 244);
            Render(ImGui.GetDrawData(), textures, Path.Combine(output, "language-dropdown.bmp"));
            Click(275, 279 + 25 * (i % 7));
            Check(config.Language == Languages.Codes[i % 7] && saves == before + i && changes == i, $"Language combo switches and saves: {Languages.Codes[i % 7]}");
            Check(search.GetValue(ui)?.ToString() == "水車" && newList.GetValue(ui)?.ToString() == "私の 음악", "Pending input retained after combo change");
        }
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
        // Mini-player remains operable with a longer translation.
        config.Language = "de";
        typeof(RadioUi).GetField("open", flags)!.SetValue(ui, false); Frame();
        var oldRepeat = config.Repeat;
        Click(1213, 784);
        Check(config.Repeat != oldRepeat && config.Repeat == player.Queue.Repeat, "Localized mini repeat button updates playback");
        var volume = config.VolumePercent;
        Click(1370, 780);
        Check(config.VolumePercent != volume && config.VolumePercent > 100, "Localized mini volume control remains operable");
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
    public ExcelSheet<T> GetExcelSheet<T>(Language? language = null) where T : struct, IExcelRow<T> => game.GetExcelSheet<T>(language)!;
    public bool FileExists(string path) => game.FileExists(path);
}
public class Dummy : DispatchProxy
{
    protected override object? Invoke(MethodInfo? method, object?[]? args) => method!.ReturnType.IsValueType ? Activator.CreateInstance(method.ReturnType) : null;
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
