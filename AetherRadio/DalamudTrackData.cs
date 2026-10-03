using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Data;
using Dalamud.Game;

namespace AetherRadio;

public sealed class DalamudTrackData(IDataManager data) : ITrackData
{
    public ExcelSheet<T> GetExcelSheet<T>(Language? language = null) where T : struct, IExcelRow<T> =>
        data.GetExcelSheet<T>(language == Language.English ? ClientLanguage.English : null);
    public bool FileExists(string path) => data.FileExists(path);
}
