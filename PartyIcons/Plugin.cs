using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Plugin;
using PartyIcons.Api;
using PartyIcons.Runtime;
using PartyIcons.Stylesheet;
using PartyIcons.Utils;
using PartyIcons.View;
using XivCommon;
using SigScanner = Dalamud.Game.SigScanner;

namespace PartyIcons
{
    public sealed class Plugin : IDalamudPlugin
    {
        public        string Name => "PartyIcons";
        private const string commandName = "/ppi";

        [PluginService] public DalamudPluginInterface Interface      { get; set; }
        [PluginService] public ClientState            ClientState    { get; set; }
        [PluginService] public Framework              Framework      { get; set; }
        [PluginService] public CommandManager         CommandManager { get; set; }
        [PluginService] public ObjectTable            ObjectTable    { get; set; }
        [PluginService] public GameGui                GameGui        { get; set; }
        [PluginService] public ChatGui                ChatGui        { get; set; }
        [PluginService] public PartyList              PartyList      { get; set; }
        [PluginService] public SigScanner             SigScanner     { get; set; }

        public  PluginAddressResolver Address { get; }
        private XivCommonBase         Base    { get; }

        private Configuration Configuration { get; }

        private readonly PlayerContextMenu    _contextMenu;
        private readonly PluginUI             _ui;
        private readonly NameplateUpdater     _nameplateUpdater;
        private readonly NPCNameplateFixer    _npcNameplateFixer;
        private readonly NameplateView        _nameplateView;
        private readonly RoleTracker          _roleTracker;
        private readonly ViewModeSetter  _modeSetter;
        private readonly ChatNameUpdater      _chatNameUpdater;
        private readonly PlayerStylesheet     _playerStylesheet;

        public Plugin()
        {
            Configuration = Interface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(Interface);
            Configuration.OnSave += OnConfigurationSave;

            CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "opens configuration window; \"reset\" or \"r\" resets all assignments; \"debug\" prints debugging info."
            });

            Address = new PluginAddressResolver();
            Address.Setup(SigScanner);
            _ui = new PluginUI(Configuration);
            Interface.Inject(_ui);

            Base = new XivCommonBase(Hooks.ContextMenu);
            XivApi.Initialize(this, Address);

            SeStringUtils.Initialize();

            _playerStylesheet = new PlayerStylesheet(Configuration);

            _roleTracker = new RoleTracker();
            Interface.Inject(_roleTracker);

            _nameplateView = new NameplateView(_roleTracker, Configuration, _playerStylesheet);
            Interface.Inject(_nameplateView);

            _chatNameUpdater = new ChatNameUpdater(_roleTracker, _playerStylesheet);
            Interface.Inject(_chatNameUpdater);

            _modeSetter = new ViewModeSetter(_nameplateView, Configuration, _chatNameUpdater);
            Interface.Inject(_modeSetter);

            _nameplateUpdater = new NameplateUpdater(Address, _nameplateView, Base);
            _npcNameplateFixer = new NPCNameplateFixer(_nameplateView);

            _contextMenu = new PlayerContextMenu(Base, _roleTracker, _playerStylesheet);
            Interface.Inject(_contextMenu);

            _ui.Initialize();
            Interface.UiBuilder.Draw += _ui.DrawSettingsWindow;
            Interface.UiBuilder.OpenConfigUi += _ui.OpenSettings;

            _roleTracker.OnAssignedRolesUpdated += OnAssignedRolesUpdated;

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
            _nameplateUpdater.ForceRefresh();
        }

        private void OnAssignedRolesUpdated()
        {
            _nameplateUpdater.ForceRefresh();
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
            else if (arguments == "debug")
            {
                ChatGui.Print($"Current mode is {_nameplateView.PartyMode}, party count {PartyList.Length}");
                ChatGui.Print(_roleTracker.DebugDescription());
            }
        }
    }
}