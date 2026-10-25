using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AetherRadio.Core;

public static class ConfigurationCodec
{
    private static JsonSerializer Serializer() => JsonSerializer.Create(new JsonSerializerSettings {
        TypeNameHandling = TypeNameHandling.None, Culture = CultureInfo.InvariantCulture,
        FloatParseHandling = FloatParseHandling.Double,
    });
    public static Configuration Decode(byte[] bytes)
    {
        try
        {
            var root = JObject.Parse(new UTF8Encoding(false, true).GetString(bytes).TrimStart('\uFEFF'));
            if (root["Version"]?.Type != JTokenType.Integer || (int)root["Version"]! < 1)
                throw new InvalidDataException("Missing settings version.");
            if ((int)root["Version"]! > 2) throw new NotSupportedException("Settings were written by a newer BGMPlayer. Files were preserved.");
            var config = root.ToObject<Configuration>(Serializer()) ?? throw new InvalidDataException("Empty settings.");
            if (!float.IsFinite(config.Opacity) || config.MiniPosition is { } p && (!float.IsFinite(p.X) || !float.IsFinite(p.Y)))
                throw new InvalidDataException("Invalid numeric settings.");
            return config;
        }
        catch (Exception e) when (e is JsonException or DecoderFallbackException or OverflowException or FormatException)
        { throw new InvalidDataException("Invalid BGMPlayer settings JSON.", e); }
    }
    public static byte[] Encode(Configuration config)
    {
        var root = JObject.FromObject(config, Serializer());
        // Retain the host's root type marker for rollback to releases using GetPluginConfig.
        // Decode always ignores type metadata and constructs only Configuration.
        root.AddFirst(new JProperty("$type", "AetherRadio.Configuration, AetherRadio"));
        // Favorites are a set; remove/re-add order is not a semantic configuration change.
        if (config.Favorites != null) root["Favorites"] = new JArray(config.Favorites.Order());
        return Encoding.UTF8.GetBytes(root.ToString(Formatting.Indented));
    }
}
