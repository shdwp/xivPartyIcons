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
    
    public static PartyListHUDView PartyHudView { get; private set; }
    public static PartyListHUDUpdater PartyListHudUpdater { get; private set; }
    public static SettingsWindow SettingsWindow { get; private set; }
    public static NameplateUpdater NameplateUpdater { get; private set; }
    public static NPCNameplateFixer NpcNameplateFixer { get; private set; }
    public static NameplateView NameplateView { get; private set; }
    public static RoleTracker RoleTracker { get; private set; }
    public static ViewModeSetter ModeSetter { get; private set; }
    public static ChatNameUpdater ChatNameUpdater { get; private set; }
    public static PlayerContextMenu ContextMenu { get; private set; }
    public static CommandHandler CommandHandler { get; private set; }

    public Plugin(DalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();
        
        var config = Service.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        config.Initialize(Service.PluginInterface);

        Address = new PluginAddressResolver();
        Address.Setup(Service.SigScanner);

        var playerStylesheet = new PlayerStylesheet(config);

        SettingsWindow = new SettingsWindow(config, playerStylesheet);

        XivApi.Initialize(this, Address);

        SeStringUtils.Initialize();

        PartyHudView = new PartyListHUDView(Service.GameGui, playerStylesheet);
        RoleTracker = new RoleTracker(config);
        NameplateView = new NameplateView(RoleTracker, config, playerStylesheet, PartyHudView);
        ChatNameUpdater = new ChatNameUpdater(RoleTracker, playerStylesheet);
        PartyListHudUpdater = new PartyListHUDUpdater(PartyHudView, RoleTracker, config);
        ModeSetter = new ViewModeSetter(NameplateView, config, ChatNameUpdater, PartyListHudUpdater);
        NameplateUpdater = new NameplateUpdater(config, Address, NameplateView, ModeSetter);
        NpcNameplateFixer = new NPCNameplateFixer(NameplateView);
        ContextMenu = new PlayerContextMenu(RoleTracker, config, playerStylesheet);
        CommandHandler = new CommandHandler();

        SettingsWindow.Initialize();

        PartyListHudUpdater.Enable();
        ModeSetter.Enable();
        RoleTracker.Enable();
        NameplateUpdater.Enable();
        NpcNameplateFixer.Enable();
        ChatNameUpdater.Enable();
        ContextMenu.Enable();
    }

    public void Dispose()
    {
        PartyHudView.Dispose();
        PartyListHudUpdater.Dispose();
        ChatNameUpdater.Dispose();
        ContextMenu.Dispose();
        NameplateUpdater.Dispose();
        NpcNameplateFixer.Dispose();
        RoleTracker.Dispose();
        ModeSetter.Dispose();
        SettingsWindow.Dispose();
        CommandHandler.Dispose();

        SeStringUtils.Dispose();
        XivApi.DisposeInstance();
    }
}