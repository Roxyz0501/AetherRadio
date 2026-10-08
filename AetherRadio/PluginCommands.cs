using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using AetherRadio.Core;

namespace AetherRadio;

public sealed class PluginCommands : IDisposable
{
    public const string Main = "/bgmplayer";
    public const string Legacy = "/aetherradio";
    private readonly ICommandManager commands;
    private readonly Action open;
    private readonly Action stop;
    private readonly bool legacyRegistered;
    private readonly Localization text;
    private readonly CommandInfo mainInfo;

    public PluginCommands(ICommandManager commands, Action open, Action stop, Localization? text = null)
    {
        this.commands = commands; this.open = open; this.stop = stop;
        this.text = text ?? new Localization(() => "en");
        mainInfo = new CommandInfo(Handle) { HelpMessage = this.text["CommandHelp"] };
        if (!commands.AddHandler(Main, mainInfo))
            throw new InvalidOperationException(this.text["CommandConflict"]);
        // Preserve old macros without taking ownership of another plugin's command.
        try { legacyRegistered = commands.AddHandler(Legacy, new CommandInfo(Handle) { ShowInHelp = false }); }
        catch { commands.RemoveHandler(Main); throw; }
    }

    public void RefreshLanguage() => mainInfo.HelpMessage = text["CommandHelp"];

    private void Handle(string command, string args)
    {
        if (args.Trim().Equals("stop", StringComparison.OrdinalIgnoreCase)) stop();
        else open();
    }

    public void Dispose()
    {
        commands.RemoveHandler(Main);
        if (legacyRegistered) commands.RemoveHandler(Legacy);
    }
}
