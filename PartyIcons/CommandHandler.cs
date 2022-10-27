using System;
using Dalamud.Game.Command;
using Dalamud.Logging;

namespace PartyIcons;

public class CommandHandler : IDisposable
{
    private const string commandName = "/ppi";
    
    public CommandHandler()
    {
        Service.CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
        {
            HelpMessage =
                "opens configuration window; \"reset\" or \"r\" resets all assignments; \"debug\" prints debugging info"
        });
    }
    
    public void Dispose()
    {
        Service.CommandManager.RemoveHandler(commandName);
    }

    private void OnCommand(string command, string arguments)
    {
        arguments = arguments.Trim().ToLower();

        if (arguments == "" || arguments == "config")
        {
            Plugin._ui.ToggleSettingsWindow();
        }
        else if (arguments == "reset" || arguments == "r")
        {
            Plugin._roleTracker.ResetOccupations();
            Plugin._roleTracker.ResetAssignments();
            Plugin._roleTracker.CalculateUnassignedPartyRoles();
            Service.ChatGui.Print("Occupations are reset, roles are auto assigned.");
        }
        else if (arguments == "dbg state")
        {
            Service.ChatGui.Print($"Current mode is {Plugin._nameplateView.PartyMode}, party count {Service.PartyList.Length}");
            Service.ChatGui.Print(Plugin._roleTracker.DebugDescription());
        }
        else if (arguments == "dbg party")
        {
            Service.ChatGui.Print(Plugin._partyHUDView.GetDebugInfo());
        }
        else if (arguments.Contains("dbg icon"))
        {
            var argv = arguments.Split(' ');

            if (argv.Length == 3)
            {
                try
                {
                    Plugin._nameplateUpdater.DebugIcon = int.Parse(argv[2]);
                    PluginLog.Debug($"Set debug icon to {Plugin._nameplateUpdater.DebugIcon}");
                }
                catch (Exception)
                {
                    PluginLog.Debug("Invalid icon id given for debug icon.");
                    Plugin._nameplateUpdater.DebugIcon = -1;
                }
            }
            else
            {
                Plugin._nameplateUpdater.DebugIcon = -1;
            }
        }
    }
}