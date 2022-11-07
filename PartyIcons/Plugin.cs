using Dalamud.Plugin;
using PartyIcons.Api;
using PartyIcons.Configuration;
using PartyIcons.Runtime;
using PartyIcons.Stylesheet;
using PartyIcons.UI;
using PartyIcons.Utils;
using PartyIcons.View;

namespace PartyIcons;

public sealed class Plugin : IDalamudPlugin
{
    public string Name => "Party Icons";

    public PluginAddressResolver Address { get; }

    public static PartyListHUDView PartyHudView { get; private set; } = null!;
    public static PartyListHUDUpdater PartyListHudUpdater { get; private set; } = null!;
    public static SettingsWindow SettingsWindow { get; private set; } = null!;
    public static NameplateUpdater NameplateUpdater { get; private set; } = null!;
    public static NPCNameplateFixer NpcNameplateFixer { get; private set; } = null!;
    public static NameplateView NameplateView { get; private set; } = null!;
    public static RoleTracker RoleTracker { get; private set; } = null!;
    public static ViewModeSetter ModeSetter { get; private set; } = null!;
    public static ChatNameUpdater ChatNameUpdater { get; private set; } = null!;
    public static PlayerContextMenu ContextMenu { get; private set; } = null!;
    public static CommandHandler CommandHandler { get; private set; } = null!;
    public static Settings Settings { get; private set; } = null!;
    public static PlayerStylesheet PlayerStylesheet { get; private set; } = null!;

    public Plugin(DalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();

        Settings = Settings.Load();

        Address = new PluginAddressResolver();
        Address.Setup(Service.SigScanner);

        PlayerStylesheet = new PlayerStylesheet(Settings);

        SettingsWindow = new SettingsWindow();

        XivApi.Initialize(this, Address);

        SeStringUtils.Initialize();

        PartyHudView = new PartyListHUDView(Service.GameGui, PlayerStylesheet);
        RoleTracker = new RoleTracker(Settings);
        NameplateView = new NameplateView(RoleTracker, Settings, PlayerStylesheet, PartyHudView);
        ChatNameUpdater = new ChatNameUpdater(RoleTracker, PlayerStylesheet);
        PartyListHudUpdater = new PartyListHUDUpdater(PartyHudView, RoleTracker, Settings);
        ModeSetter = new ViewModeSetter(NameplateView, Settings, ChatNameUpdater, PartyListHudUpdater);
        NameplateUpdater = new NameplateUpdater(Settings, Address, NameplateView, ModeSetter);
        NpcNameplateFixer = new NPCNameplateFixer(NameplateView);
        ContextMenu = new PlayerContextMenu(RoleTracker, Settings, PlayerStylesheet);
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