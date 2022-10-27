using Dalamud.Plugin;
using PartyIcons.Api;
using PartyIcons.Runtime;
using PartyIcons.Stylesheet;
using PartyIcons.Utils;
using PartyIcons.View;

namespace PartyIcons;

public sealed class Plugin : IDalamudPlugin
{
    public string Name => "PartyIcons";

    public PluginAddressResolver Address { get; }
    
    public static PartyListHUDView _partyHUDView { get; private set; }
    public static PartyListHUDUpdater _partyListHudUpdater { get; private set; }
    public static PluginUI _ui { get; private set; }
    public static NameplateUpdater _nameplateUpdater { get; private set; }
    public static NPCNameplateFixer _npcNameplateFixer { get; private set; }
    public static NameplateView _nameplateView { get; private set; }
    public static RoleTracker _roleTracker { get; private set; }
    public static ViewModeSetter _modeSetter { get; private set; }
    public static ChatNameUpdater _chatNameUpdater { get; private set; }
    public static PlayerContextMenu _contextMenu { get; private set; }
    public static CommandHandler _commandHandler { get; private set; }

    public Plugin(DalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();
        
        var config = Service.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        config.Initialize(Service.PluginInterface);


        Address = new PluginAddressResolver();
        Address.Setup(Service.SigScanner);

        var playerStylesheet = new PlayerStylesheet(config);

        _ui = new PluginUI(config, playerStylesheet);

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
        _commandHandler = new CommandHandler();

        _ui.Initialize();

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
        _partyHUDView.Dispose();
        _partyListHudUpdater.Dispose();
        _chatNameUpdater.Dispose();
        _contextMenu.Dispose();
        _nameplateUpdater.Dispose();
        _npcNameplateFixer.Dispose();
        _roleTracker.Dispose();
        _modeSetter.Dispose();
        _ui.Dispose();

        SeStringUtils.Dispose();
        XivApi.DisposeInstance();
    }
}