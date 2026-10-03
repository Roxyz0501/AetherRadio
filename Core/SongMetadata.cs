using System.Globalization;
using Microsoft.VisualBasic.FileIO;

namespace AetherRadio.Core;

public sealed record SongMetadata(string Title, string SearchTerms, int? Seconds);

public static class MetadataStore
{
    public static Dictionary<ushort, SongMetadata> Load()
    {
        var durations = Read("xiv_bgm_metadata.csv").Where(x => x.Length >= 2 && ushort.TryParse(x[0], out _))
            .ToDictionary(x => ushort.Parse(x[0]), x => int.TryParse(x[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) && n > 0 && n <= 7200 ? (int?)n : null);
        return Read("xiv_bgm_ja.csv").Where(x => x.Length >= 6 && ushort.TryParse(x[0], out _) && x[1].Trim(' ', '?', '？', '-').Length > 0)
            .ToDictionary(x => ushort.Parse(x[0]), x => new SongMetadata(x[1], string.Join(' ', x.Skip(2)), durations.GetValueOrDefault(ushort.Parse(x[0]))));
    }

    private static IEnumerable<string[]> Read(string name)
    {
        using var stream = typeof(MetadataStore).Assembly.GetManifestResourceStream("AetherRadio.Data." + name)
            ?? throw new InvalidOperationException("Missing bundled metadata: " + name);
        using var parser = new TextFieldParser(stream) { HasFieldsEnclosedInQuotes = true, TrimWhiteSpace = true };
        parser.SetDelimiters(",");
        while (!parser.EndOfData) if (parser.ReadFields() is { } row) yield return row;
    }
}
