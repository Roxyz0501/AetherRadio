using AetherRadio;
using AetherRadio.Core;
using Newtonsoft.Json;

static class ConfigurationTests
{
    public static void Run(Action<bool, string> check)
    {
        var fresh = new Configuration();
        check(fresh.Repeat == RepeatMode.One && !fresh.Upgrade(), "New installations default to one-track repeat");
        var old = JsonConvert.DeserializeObject<Configuration>("""{"Version":1,"Repeat":1,"VolumePercent":175,"Favorites":[12],"Playlists":[{"Name":"Saved","Tracks":[12,34]}]}""")!;
        check(old.Upgrade() && old.Version == 2 && old.Repeat == RepeatMode.One, "Existing installations switch to one-track repeat on upgrade");
        check(old.VolumePercent == 175 && old.Favorites.Contains(12) && old.Playlists[0].Tracks.SequenceEqual(new ushort[] { 12, 34 }), "Repeat migration preserves volume, favorites and playlists");
        old.Repeat = RepeatMode.Off;
        var reloaded = JsonConvert.DeserializeObject<Configuration>(JsonConvert.SerializeObject(old))!;
        check(!reloaded.Upgrade() && reloaded.Repeat == RepeatMode.Off, "A later user choice survives reload without repeated migration");
    }
}
