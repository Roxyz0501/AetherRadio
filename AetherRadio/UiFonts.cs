using System.Globalization;
using AetherRadio.Core;
using Dalamud.Interface;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Plugin.Services;

namespace AetherRadio;

/// <summary>A single plugin-owned atlas covers every UI language without rebuilding on a language change.</summary>
public sealed class UiFonts : IDisposable
{
    private readonly IFontAtlas atlas;
    private readonly IFontHandle font;
    private bool missingGlyphs;
    public bool Failed => missingGlyphs || font.LoadException != null;

    public UiFonts(IUiBuilder builder, IPluginLog log)
    {
        atlas = builder.CreateFontAtlas(FontAtlasAutoRebuildMode.OnNewFrame);
        font = atlas.NewDelegateFontHandle(step => step.OnPreBuild(tk =>
        {
            tk.AddDalamudDefaultFont(-1);
            foreach (var code in Languages.Codes)
            {
                try
                {
                    tk.AttachWindowsDefaultFont(CultureInfo.GetCultureInfo(code), new SafeFontConfig
                    {
                        SizePx = builder.FontDefaultSizePx,
                        MergeFont = tk.Font,
                        GlyphRanges = Localization.GlyphRanges(code),
                    });
                }
                catch (Exception e) { log.Warning(e, "Optional UI font unavailable for {Language}", code); }
            }
        }).OnPostBuild(tk =>
        {
            missingGlyphs = HasMissingGlyphs(tk);
            if (missingGlyphs) log.Warning("BGMPlayer UI font is missing glyphs. Check Windows optional language fonts.");
        }));
    }

    private static unsafe bool HasMissingGlyphs(IFontAtlasBuildToolkitPostBuild toolkit) =>
        Localization.Resources.Values.SelectMany(r => r.Values).Append(string.Join("", Languages.Names))
            .SelectMany(s => s).Where(c => !char.IsControl(c)).Distinct()
            .Any(c => toolkit.Font.FindGlyphNoFallback(c) == null);

    public IDisposable? Push() => font.Available ? font.Push() : null;
    public void Dispose() { font.Dispose(); atlas.Dispose(); }
}
