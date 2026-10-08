using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using AetherRadio.Core;

namespace AetherRadio;

public sealed class Plugin : IDalamudPlugin
{
    private readonly IDalamudPluginInterface pi;
    private readonly PluginCommands commands;
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly Player player;
    private readonly RadioUi ui;
    private readonly UiFonts fonts;
    private readonly IPluginLog log;
    private readonly Task catalogTask;
    private bool catalogReported;

    public Plugin(IDalamudPluginInterface pi, ICommandManager commands, IDataManager data,
        IClientState client, IFramework framework, IGameInteropProvider interop, IPluginLog log)
    {
        this.pi = pi; this.framework = framework; this.log = log; this.client = client;
        var config = pi.GetPluginConfig() as Configuration ?? new Configuration();
        var changed = config.InitializeLanguage(() => client.ClientLanguage.ToCode(), () => pi.UiLanguage);
        var text = new Localization(() => config.Language);
        config.Favorites ??= []; config.Playlists ??= [];
        config.Playlists.RemoveAll(x => x == null);
        foreach (var list in config.Playlists) { list.Tracks ??= []; list.Name ??= text["Playlists"]; }
        config.TrackSeconds = Math.Clamp(config.TrackSeconds, 15, 1800);
        config.Opacity = Math.Clamp(config.Opacity, 0.5f, 1);
        config.VolumePercent = Math.Clamp(config.VolumePercent, 0, Core.BgmVolumeSession.MaxPercent);
        config.Corner = Math.Clamp(config.Corner, 0, 4);
        if (config.Upgrade() | changed) pi.SavePluginConfig(config);
        var catalog = new Catalog(new DalamudTrackData(data), (e, label) => log.Warning(e, "Catalogue enrichment: {Label}", label));
        player = new Player(config, catalog, null, client, log,
            () => new BgmPlayback(interop, () => client.IsLoggedIn));
        ui = new RadioUi(config, catalog, player, () => pi.SavePluginConfig(config));
        try { this.commands = new PluginCommands(commands, ui.Open, player.Stop, text); }
        catch { player.Dispose(); throw; }
        try { fonts = new UiFonts(pi.UiBuilder, log); }
        catch { this.commands.Dispose(); player.Dispose(); throw; }
        ui.LanguageChanged = this.commands.RefreshLanguage;
        ui.FontFailed = () => fonts.Failed;
        catalogTask = Task.Run(catalog.Load);
        pi.UiBuilder.Draw += Draw;
        pi.UiBuilder.OpenMainUi += ui.Open;
        pi.UiBuilder.OpenConfigUi += ui.OpenSettings;
        client.Login += player.OnLogin;
        client.Logout += player.OnLogout;
        framework.Update += Update;
    }
    private void Draw()
    {
        using var scope = fonts.Push();
        ui.Draw();
    }
    private void Update(IFramework _)
    {
        if (!catalogReported && catalogTask.IsCompleted)
        {
            catalogReported = true;
            if (catalogTask.IsFaulted) { player.Error = "CatalogError"; log.Error(catalogTask.Exception!, "Catalogue load failed"); }
            else ui.CatalogReady = true;
        }
        player.Update();
    }
    public void Dispose()
    {
        framework.Update -= Update;
        client.Login -= player.OnLogin;
        client.Logout -= player.OnLogout;
        pi.UiBuilder.Draw -= Draw;
        pi.UiBuilder.OpenMainUi -= ui.Open;
        pi.UiBuilder.OpenConfigUi -= ui.OpenSettings;
        commands.Dispose();
        // The task reads only game data; join before releasing this plugin's services.
        try { catalogTask.GetAwaiter().GetResult(); } catch { /* Already reported above or during unload. */ }
        try { player.Dispose(); }
        finally { fonts.Dispose(); }
    }
}
