using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace AetherRadio;

public sealed class Plugin : IDalamudPlugin
{
    private readonly IDalamudPluginInterface pi;
    private readonly ICommandManager commands;
    private readonly IFramework framework;
    private readonly BgmPlayback? engine;
    private readonly Player player;
    private readonly RadioUi ui;
    private readonly IPluginLog log;
    private readonly Task catalogTask;
    private bool catalogReported;

    public Plugin(IDalamudPluginInterface pi, ICommandManager commands, IDataManager data,
        IClientState client, IFramework framework, IGameInteropProvider interop, IGameConfig gameConfig, IPluginLog log)
    {
        this.pi = pi; this.commands = commands; this.framework = framework; this.log = log;
        var config = pi.GetPluginConfig() as Configuration ?? new Configuration();
        config.Favorites ??= []; config.Playlists ??= [];
        config.Playlists.RemoveAll(x => x == null);
        foreach (var list in config.Playlists) { list.Tracks ??= []; list.Name ??= "マイリスト"; }
        config.TrackSeconds = Math.Clamp(config.TrackSeconds, 15, 1800);
        config.Opacity = Math.Clamp(config.Opacity, 0.5f, 1);
        config.Corner = Math.Clamp(config.Corner, 0, 4);
        var catalog = new Catalog(new DalamudTrackData(data), (e, label) => log.Warning(e, "Catalogue enrichment: {Label}", label));
        string? engineError = null;
        try { engine = new BgmPlayback(interop); }
        catch (Exception e) { engineError = e.Message; log.Error(e, "BGM engine unavailable"); }
        player = new Player(config, catalog, engine, client, log) { Error = engineError };
        ui = new RadioUi(config, catalog, player, gameConfig, () => pi.SavePluginConfig(config));
        catalogTask = Task.Run(catalog.Load);
        commands.AddHandler("/aetherradio", new CommandInfo(OnCommand) { HelpMessage = "BGMプレイヤーを開く。/aetherradio stop でゲームBGMへ戻す。" });
        pi.UiBuilder.Draw += ui.Draw;
        pi.UiBuilder.OpenMainUi += ui.Open;
        pi.UiBuilder.OpenConfigUi += ui.Open;
        framework.Update += Update;
    }
    private void OnCommand(string command, string args)
    {
        if (args.Trim().Equals("stop", StringComparison.OrdinalIgnoreCase)) player.Stop();
        else ui.Open();
    }
    private void Update(IFramework _)
    {
        if (!catalogReported && catalogTask.IsCompleted)
        {
            catalogReported = true;
            if (catalogTask.IsFaulted) { player.Error = "曲一覧を読み込めませんでした。Dalamudログを確認してください。"; log.Error(catalogTask.Exception!, "Catalogue load failed"); }
            else ui.CatalogReady = true;
        }
        player.Update();
    }
    public void Dispose()
    {
        framework.Update -= Update;
        pi.UiBuilder.Draw -= ui.Draw;
        pi.UiBuilder.OpenMainUi -= ui.Open;
        pi.UiBuilder.OpenConfigUi -= ui.Open;
        commands.RemoveHandler("/aetherradio");
        // The task reads only game data; join before releasing this plugin's services.
        try { catalogTask.GetAwaiter().GetResult(); } catch { /* Already reported above or during unload. */ }
        engine?.Dispose();
    }
}
