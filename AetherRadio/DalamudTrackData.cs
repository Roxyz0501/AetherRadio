using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Data;
using Dalamud.Game;

namespace AetherRadio;

public sealed class DalamudTrackData(IDataManager data) : ITrackData
{
    public ExcelSheet<T> GetExcelSheet<T>(Language? language = null) where T : struct, IExcelRow<T> =>
        data.GetExcelSheet<T>(language switch
        {
            Language.Japanese => ClientLanguage.Japanese, Language.English => ClientLanguage.English,
            Language.German => ClientLanguage.German, Language.French => ClientLanguage.French, _ => null,
        });
    public bool FileExists(string path) => data.FileExists(path);
}
