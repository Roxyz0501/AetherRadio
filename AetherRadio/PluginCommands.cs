using Dalamud.Game.Command;
using Dalamud.Plugin.Services;

namespace AetherRadio;

public sealed class PluginCommands : IDisposable
{
    public const string Main = "/bgmplayer";
    public const string Legacy = "/aetherradio";
    private readonly ICommandManager commands;
    private readonly Action open;
    private readonly Action stop;
    private readonly bool legacyRegistered;

    public PluginCommands(ICommandManager commands, Action open, Action stop)
    {
        this.commands = commands; this.open = open; this.stop = stop;
        if (!commands.AddHandler(Main, new CommandInfo(Handle) { HelpMessage = "BGMPlayerを開く。/bgmplayer stop でゲームBGMへ戻す。" }))
            throw new InvalidOperationException("/bgmplayer は別のプラグインに登録されています。");
        // Preserve old macros without taking ownership of another plugin's command.
        try { legacyRegistered = commands.AddHandler(Legacy, new CommandInfo(Handle) { ShowInHelp = false }); }
        catch { commands.RemoveHandler(Main); throw; }
    }

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
