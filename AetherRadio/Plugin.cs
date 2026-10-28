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
    private readonly CancellationTokenSource catalogCancellation = new();
    private bool catalogReported;
    private readonly object lifecycleGate = new();
    private readonly DeferredSettings<Configuration> settings;
    private CancellationTokenRegistration gameExit, hostExit;
    private bool disposed;

    public Plugin(IDalamudPluginInterface pi, ICommandManager commands, IDataManager data,
        IClientState client, IFramework framework, IGameInteropProvider interop, IPluginLog log, IGameLifecycle lifecycle)
    {
        this.pi = pi; this.framework = framework; this.log = log; this.client = client;
        var file = new AtomicSettingsFile<Configuration>(pi.ConfigFile.FullName, ConfigurationCodec.Decode, ConfigurationCodec.Encode);
        var loaded = file.Load(() => new Configuration());
        var config = loaded.Value;
        settings = new DeferredSettings<Configuration>(file, () => config, loaded.Baseline, loaded.Recovered,
            e => log.Error(e, "BGMPlayer settings save failed; use Retry save in Settings after resolving the cause."));
        if (loaded.Recovered) log.Warning("BGMPlayer settings recovered from a valid backup; the damaged file will be preserved on save.");
        config.InitializeLanguage(() => client.ClientLanguage.ToCode(), () => pi.UiLanguage);
        var text = new Localization(() => config.Language);
        config.Favorites ??= []; config.Playlists ??= [];
        config.Playlists.RemoveAll(x => x == null);
        foreach (var list in config.Playlists) { list.Tracks ??= []; list.Name ??= text["Playlists"]; }
        config.TrackSeconds = Math.Clamp(config.TrackSeconds, 15, 1800);
        config.Opacity = Math.Clamp(config.Opacity, 0.5f, 1);
        config.VolumePercent = Math.Clamp(config.VolumePercent, 0, Core.BgmVolumeSession.MaxPercent);
        config.Corner = Math.Clamp(config.Corner, 0, 4);
        config.Upgrade();
        settings.MarkDirty(); // Compare normalized settings at the next save boundary, including first-run language.
        var catalog = new Catalog(new DalamudTrackData(data), (e, label) => log.Warning(e, "Catalogue enrichment: {Label}", label));
        player = new Player(config, catalog, null, client, log,
            () => new BgmPlayback(interop, () => client.IsLoggedIn, catalog));
        ui = new RadioUi(config, catalog, player, settings.MarkDirty);
        ui.PersistenceStatus = () => settings.Stopped ? "SaveStopped" : settings.LastError != null ? "SaveFailed" : settings.BackupCleanupFailed ? "BackupCleanupFailed" : settings.Recovered ? "SettingsRecovered" : null;
        ui.RetrySave = settings.Retry;
        try { this.commands = new PluginCommands(commands, OpenMain, StopPlayback, text); }
        catch { try { settings.Dispose(); } finally { player.Dispose(); } throw; }
        try { fonts = new UiFonts(pi.UiBuilder, log); }
        catch { try { settings.Dispose(); } finally { this.commands.Dispose(); player.Dispose(); } throw; }
        ui.LanguageChanged = this.commands.RefreshLanguage;
        ui.FontFailed = () => fonts.Failed;
        catalogTask = Task.Run(() => catalog.Load(catalogCancellation.Token));
        pi.UiBuilder.Draw += Draw;
        pi.UiBuilder.OpenMainUi += OpenMain;
        pi.UiBuilder.OpenConfigUi += OpenSettings;
        client.Login += OnLogin;
        client.Logout += OnLogout;
        framework.Update += Update;
        gameExit = lifecycle.GameShuttingDownToken.Register(Dispose);
        hostExit = lifecycle.DalamudUnloadingToken.Register(Dispose);
        if (disposed) { gameExit.Unregister(); hostExit.Unregister(); }
    }
    private void OpenMain() { lock (lifecycleGate) { if (!disposed) ui.Open(); } }
    private void OpenSettings() { lock (lifecycleGate) { if (!disposed) ui.OpenSettings(); } }
    private void StopPlayback() { lock (lifecycleGate) { if (!disposed) player.Stop(); } }
    private void OnLogin() { lock (lifecycleGate) { if (!disposed) player.OnLogin(); } }
    private void Draw()
    {
        lock (lifecycleGate)
        {
            if (disposed) return;
            using var scope = fonts.Push();
            ui.Draw();
        }
    }
    private void Update(IFramework _)
    {
        lock (lifecycleGate)
        {
            if (disposed) return;
            if (!catalogReported && catalogTask.IsCompleted)
            {
                catalogReported = true;
                if (catalogTask.IsFaulted) { player.Error = "CatalogError"; log.Error(catalogTask.Exception!, "Catalogue load failed"); }
                else ui.CatalogReady = true;
            }
            player.Update();
            settings.Tick();
        }
    }
    private void OnLogout(int type, int code)
    {
        lock (lifecycleGate)
        {
            if (disposed) return;
            try { settings.Flush(); } finally { player.OnLogout(type, code); }
        }
    }
    public void Dispose()
    {
        lock (lifecycleGate)
        {
            if (disposed) return;
            disposed = true;
            gameExit.Unregister(); hostExit.Unregister();
            framework.Update -= Update;
            client.Login -= OnLogin;
            client.Logout -= OnLogout;
            pi.UiBuilder.Draw -= Draw;
            pi.UiBuilder.OpenMainUi -= OpenMain;
            pi.UiBuilder.OpenConfigUi -= OpenSettings;
            try { commands.Dispose(); }
            finally
            {
                catalogCancellation.Cancel();
                // Worker cancellation and saving never marshal back onto the UI thread.
                try { catalogTask.GetAwaiter().GetResult(); } catch { /* Reported or cancelled during unload. */ }
                catalogCancellation.Dispose();
                try { settings.Dispose(); }
                finally { try { player.Dispose(); } finally { fonts.Dispose(); } }
            }
        }
    }
}
