using System;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using PartyIcons.Api;
using PartyIcons.Runtime;
using PartyIcons.Stylesheet;
using PartyIcons.Utils;
using PartyIcons.View;
using SigScanner = Dalamud.Game.SigScanner;

namespace PartyIcons;

public sealed class Plugin : IDalamudPlugin
{
    public string Name => "PartyIcons";
    private const string commandName = "/ppi";

    public PluginAddressResolver Address { get; }

    private readonly PartyListHUDView _partyHUDView;
    private readonly PartyListHUDUpdater _partyListHudUpdater;
    private readonly PluginUI _ui;
    private readonly NameplateUpdater _nameplateUpdater;
    private readonly NPCNameplateFixer _npcNameplateFixer;
    private readonly NameplateView _nameplateView;
    private readonly RoleTracker _roleTracker;
    private readonly ViewModeSetter _modeSetter;
    private readonly ChatNameUpdater _chatNameUpdater;
    private readonly PlayerContextMenu _contextMenu;

    public Plugin(DalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();
        
        var config = Service.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        config.Initialize(Service.PluginInterface);
        
        Service.CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
        {
            HelpMessage =
                "opens configuration window; \"reset\" or \"r\" resets all assignments; \"debug\" prints debugging info"
        });

        Address = new PluginAddressResolver();
        Address.Setup(Service.SigScanner);

        var playerStylesheet = new PlayerStylesheet(config);

        _ui = new PluginUI(config, playerStylesheet);
        Service.PluginInterface.Inject(_ui);

        XivApi.Initialize(this, Address);

        SeStringUtils.Initialize();

        _partyHUDView = new PartyListHUDView(Service.GameGui, playerStylesheet);
        _roleTracker = new RoleTracker(config);
        _nameplateView = new NameplateView(_roleTracker, config, playerStylesheet, _partyHUDView);
        _chatNameUpdater = new ChatNameUpdater(_roleTracker, playerStylesheet);
        _partyListHudUpdater = new PartyListHUDUpdater(_partyHUDView, _roleTracker, config);
        _modeSetter = new ViewModeSetter(_nameplateView, config, _chatNameUpdater, _partyListHudUpdater);
        _nameplateUpdater = new NameplateUpdater(config, Address, _nameplateView, _modeSetter);
        _npcNameplateFixer = new NPCNameplateFixer(_nameplateView);
        _contextMenu = new PlayerContextMenu(_roleTracker, config, playerStylesheet);

        _ui.Initialize();
        Service.PluginInterface.UiBuilder.Draw += _ui.DrawSettingsWindow;
        Service.PluginInterface.UiBuilder.OpenConfigUi += _ui.OpenSettingsWindow;

        _roleTracker.OnAssignedRolesUpdated += OnAssignedRolesUpdated;

        _partyListHudUpdater.Enable();
        _modeSetter.Enable();
        _roleTracker.Enable();
        _nameplateUpdater.Enable();
        _npcNameplateFixer.Enable();
        _chatNameUpdater.Enable();
        _contextMenu.Enable();
    }

    public void Dispose()
    {
        _roleTracker.OnAssignedRolesUpdated -= OnAssignedRolesUpdated;

        _partyHUDView.Dispose();
        _partyListHudUpdater.Dispose();
        _chatNameUpdater.Dispose();
        _contextMenu.Dispose();
        _nameplateUpdater.Dispose();
        _npcNameplateFixer.Dispose();
        _roleTracker.Dispose();
        _modeSetter.Dispose();
        Service.PluginInterface.UiBuilder.Draw -= _ui.DrawSettingsWindow;
        Service.PluginInterface.UiBuilder.OpenConfigUi -= _ui.OpenSettingsWindow;
        _ui.Dispose();

        SeStringUtils.Dispose();
        XivApi.DisposeInstance();

        Service.CommandManager.RemoveHandler(commandName);
    }

    private void OnAssignedRolesUpdated()
    {
    }

    private void OnCommand(string command, string arguments)
    {
        arguments = arguments.Trim().ToLower();

        if (arguments == "" || arguments == "config")
        {
            _ui.ToggleSettingsWindow();
        }
        else if (arguments == "reset" || arguments == "r")
        {
            _roleTracker.ResetOccupations();
            _roleTracker.ResetAssignments();
            _roleTracker.CalculateUnassignedPartyRoles();
            Service.ChatGui.Print("Occupations are reset, roles are auto assigned.");
        }
        else if (arguments == "dbg state")
        {
            Service.ChatGui.Print($"Current mode is {_nameplateView.PartyMode}, party count {Service.PartyList.Length}");
            Service.ChatGui.Print(_roleTracker.DebugDescription());
        }
        else if (arguments == "dbg party")
        {
            Service.ChatGui.Print(_partyHUDView.GetDebugInfo());
        }
        else if (arguments.Contains("dbg icon"))
        {
            var argv = arguments.Split(' ');

            if (argv.Length == 3)
            {
                try
                {
                    _nameplateUpdater.DebugIcon = int.Parse(argv[2]);
                    PluginLog.Debug($"Set debug icon to {_nameplateUpdater.DebugIcon}");
                }
                catch (Exception)
                {
                    PluginLog.Debug("Invalid icon id given for debug icon.");
                    _nameplateUpdater.DebugIcon = -1;
                }
            }
            else
            {
                _nameplateUpdater.DebugIcon = -1;
            }
        }
    }
}