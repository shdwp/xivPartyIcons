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

    [PluginService] public DalamudPluginInterface Interface { get; set; }

    [PluginService] public ClientState ClientState { get; set; }

    [PluginService] public Framework Framework { get; set; }

    [PluginService] public CommandManager CommandManager { get; set; }

    [PluginService] public ObjectTable ObjectTable { get; set; }

    [PluginService] public GameGui GameGui { get; set; }

    [PluginService] public ChatGui ChatGui { get; set; }

    [PluginService] public PartyList PartyList { get; set; }

    [PluginService] public SigScanner SigScanner { get; set; }

    public PluginAddressResolver Address { get; }

    private Configuration Configuration { get; }

    private readonly PartyListHUDView _partyHUDView;

    private readonly PartyListHUDUpdater _partyListHudUpdater;

    private readonly PlayerContextMenu _contextMenu;
    private readonly PluginUI _ui;
    private readonly NameplateUpdater _nameplateUpdater;
    private readonly NPCNameplateFixer _npcNameplateFixer;
    private readonly NameplateView _nameplateView;
    private readonly RoleTracker _roleTracker;
    private readonly ViewModeSetter _modeSetter;
    private readonly ChatNameUpdater _chatNameUpdater;
    private readonly PlayerStylesheet _playerStylesheet;

    public Plugin()
    {
        Configuration = Interface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(Interface);
        Configuration.OnSave += OnConfigurationSave;

        CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
        {
            HelpMessage =
                "opens configuration window; \"reset\" or \"r\" resets all assignments; \"debug\" prints debugging info"
        });

        Address = new PluginAddressResolver();
        Address.Setup(SigScanner);

        _playerStylesheet = new PlayerStylesheet(Configuration);

        _ui = new PluginUI(Configuration, _playerStylesheet);
        Interface.Inject(_ui);

        XivApi.Initialize(this, Address);

        SeStringUtils.Initialize();

        _partyHUDView = new PartyListHUDView(GameGui, _playerStylesheet);
        Interface.Inject(_partyHUDView);

        _roleTracker = new RoleTracker(Configuration);
        Interface.Inject(_roleTracker);

        _nameplateView = new NameplateView(_roleTracker, Configuration, _playerStylesheet, _partyHUDView);
        Interface.Inject(_nameplateView);

        _chatNameUpdater = new ChatNameUpdater(_roleTracker, _playerStylesheet);
        Interface.Inject(_chatNameUpdater);

        _partyListHudUpdater = new PartyListHUDUpdater(_partyHUDView, _roleTracker, Configuration);
        Interface.Inject(_partyListHudUpdater);

        _nameplateUpdater = new NameplateUpdater(Address, _nameplateView);
        Interface.Inject(_nameplateUpdater);

        _npcNameplateFixer = new NPCNameplateFixer(_nameplateView);

        _contextMenu = new PlayerContextMenu(_roleTracker, Configuration, _playerStylesheet);
        Interface.Inject(_contextMenu);

        _ui.Initialize();
        Interface.UiBuilder.Draw += _ui.DrawSettingsWindow;
        Interface.UiBuilder.OpenConfigUi += _ui.OpenSettings;

        _roleTracker.OnAssignedRolesUpdated += OnAssignedRolesUpdated;

        _modeSetter = new ViewModeSetter(_nameplateView, Configuration, _chatNameUpdater, _partyListHudUpdater);
        Interface.Inject(_modeSetter);

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
        Interface.UiBuilder.Draw -= _ui.DrawSettingsWindow;
        Interface.UiBuilder.OpenConfigUi -= _ui.OpenSettings;
        _ui.Dispose();

        SeStringUtils.Dispose();
        XivApi.DisposeInstance();

        CommandManager.RemoveHandler(commandName);
        Configuration.OnSave -= OnConfigurationSave;
    }

    private void OnConfigurationSave()
    {
        _modeSetter.ForceRefresh();
    }

    private void OnAssignedRolesUpdated()
    {
    }

    private void OnCommand(string command, string arguments)
    {
        arguments = arguments.Trim().ToLower();

        if (arguments == "" || arguments == "config")
        {
            _ui.OpenSettings();
        }
        else if (arguments == "reset" || arguments == "r")
        {
            _roleTracker.ResetOccupations();
            _roleTracker.ResetAssignments();
            _roleTracker.CalculateUnassignedPartyRoles();
            ChatGui.Print("Occupations are reset, roles are auto assigned.");
        }
        else if (arguments == "dbg state")
        {
            ChatGui.Print($"Current mode is {_nameplateView.PartyMode}, party count {PartyList.Length}");
            ChatGui.Print(_roleTracker.DebugDescription());
        }
        else if (arguments == "dbg party")
        {
            ChatGui.Print(_partyHUDView.GetDebugInfo());
        }
    }
}