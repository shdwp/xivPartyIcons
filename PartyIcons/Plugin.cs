using System;
using System.IO;
using Dalamud.Logging;
using Dalamud.Plugin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PartyIcons.Api;
using PartyIcons.Configuration;
using PartyIcons.Runtime;
using PartyIcons.Stylesheet;
using PartyIcons.Utils;
using PartyIcons.View;

namespace PartyIcons;

public sealed class Plugin : IDalamudPlugin
{
    public string Name => "PartyIcons";

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
    

    private static int GetConfigFileVersion(string fileText)
    {
        var json = JObject.Parse(fileText);

        return json.GetValue("Version")?.Value<int>() ?? 0;
    }
    
    public Plugin(DalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();

        var config = LoadConfiguration();

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

    private static PluginConfiguration LoadConfiguration()
    {
        PluginConfiguration? config = null;
        
        try
        {
            //JsonConvert.DeserializeObject<CharacterConfiguration>(fileText);
            var configFileInfo = Service.PluginInterface.ConfigFile;

            if (configFileInfo.Exists)
            {
                var reader = new StreamReader(configFileInfo.FullName);
                var fileText = reader.ReadToEnd();
                reader.Dispose();

                var versionNumber = GetConfigFileVersion(fileText);
                PluginLog.Information($"Config is v{versionNumber}");

                if (versionNumber == PluginConfiguration.CurrentVersion)
                {
                    config = JsonConvert.DeserializeObject<PluginConfiguration>(fileText);
                }
                else if (versionNumber == 1)
                {
                    var configV1 = JsonConvert.DeserializeObject<ConfigurationV1>(fileText);
                    config = new PluginConfiguration(configV1);
                    config.Save();
                    PluginLog.Information($"Converted config v{versionNumber} to v{PluginConfiguration.CurrentVersion}.");
                }
                else
                {
                    PluginLog.Error($"No reader available for config v{versionNumber}");
                }
            }
        }
        catch (Exception e)
        {
            PluginLog.Error("Could not read config.");
            PluginLog.Error(e.ToString());
        }

        if (config != null)
        {
            return config;
        }

        PluginLog.Information("Creating a new config.");
        return new PluginConfiguration();
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