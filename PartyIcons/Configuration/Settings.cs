using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Dalamud.Configuration;
using Dalamud.Logging;
using PartyIcons.Entities;

namespace PartyIcons.Configuration;

[Serializable]
public class Settings : IPluginConfiguration
{
    public static int CurrentVersion = 2;

    public int Version { get; set; } = CurrentVersion;

    public bool ChatContentMessage = true;
    public bool HideLocalPlayerNameplate = false;
    public bool TestingMode = true;
    public bool EasternNamingConvention = false;
    public bool DisplayRoleInPartyList = false;
    public bool UseContextMenu = false;
    public bool AssignFromChat = true;
    public bool UsePriorityIcons = true;

    public IconSetId IconSetId { get; set; } = IconSetId.GlowingColored;
    public NameplateSizeMode SizeMode { get; set; } = NameplateSizeMode.Medium;

    public NameplateMode NameplateOverworld { get; set; } = NameplateMode.SmallJobIcon;
    public NameplateMode NameplateAllianceRaid { get; set; } = NameplateMode.BigJobIconAndPartySlot;
    public NameplateMode NameplateDungeon { get; set; } = NameplateMode.BigJobIconAndPartySlot;
    public NameplateMode NameplateBozjaParty { get; set; } = NameplateMode.BigJobIconAndPartySlot;
    public NameplateMode NameplateBozjaOthers { get; set; } = NameplateMode.Default;
    public NameplateMode NameplateRaid { get; set; } = NameplateMode.RoleLetters;
    public NameplateMode NameplateOthers { get; set; } = NameplateMode.SmallJobIcon;

    public ChatConfig ChatOverworld { get; set; } = new (ChatMode.Role);
    public ChatConfig ChatAllianceRaid { get; set; } = new (ChatMode.Role);
    public ChatConfig ChatDungeon { get; set; } = new (ChatMode.Job);
    public ChatConfig ChatRaid { get; set; } = new (ChatMode.Role);
    public ChatConfig ChatOthers { get; set; } = new (ChatMode.Job);

    public Dictionary<string, RoleId> StaticAssignments { get; set; } = new();

    public event Action OnSave;

    public Settings() {}

    public Settings(SettingsV1 configV1)
    {
        ChatContentMessage = configV1.ChatContentMessage;
        HideLocalPlayerNameplate = configV1.HideLocalPlayerNameplate;
        TestingMode = configV1.TestingMode;
        EasternNamingConvention = configV1.EasternNamingConvention;
        DisplayRoleInPartyList = configV1.DisplayRoleInPartyList;
        UseContextMenu = configV1.UseContextMenu;
        AssignFromChat = configV1.AssignFromChat;
        UsePriorityIcons = configV1.UsePriorityIcons;
        
        IconSetId = configV1.IconSetId;
        SizeMode = configV1.SizeMode;
        NameplateOverworld = configV1.NameplateOverworld;
        NameplateAllianceRaid = configV1.NameplateAllianceRaid;
        NameplateDungeon = configV1.NameplateDungeon;
        NameplateBozjaParty = configV1.NameplateBozjaParty;
        NameplateBozjaOthers = configV1.NameplateBozjaOthers;
        NameplateRaid = configV1.NameplateRaid;
        NameplateOthers = configV1.NameplateOthers;

        ChatOverworld = SettingsV1.ToChatConfig(configV1.ChatOverworld);
        ChatAllianceRaid = SettingsV1.ToChatConfig(configV1.ChatAllianceRaid);
        ChatDungeon = SettingsV1.ToChatConfig(configV1.ChatDungeon);
        ChatRaid = SettingsV1.ToChatConfig(configV1.ChatRaid);
        ChatOthers = SettingsV1.ToChatConfig(configV1.ChatOthers);

        StaticAssignments = configV1.StaticAssignments;
    }
    
    public static Settings Load()
    {
        Settings? config = null;
        
        try
        {
            var configFileInfo = Service.PluginInterface.ConfigFile;

            if (configFileInfo.Exists)
            {
                var reader = new StreamReader(configFileInfo.FullName);
                var fileText = reader.ReadToEnd();
                reader.Dispose();

                var versionNumber = GetConfigFileVersion(fileText);

                if (versionNumber == Settings.CurrentVersion)
                {
                    config = JsonConvert.DeserializeObject<Settings>(fileText);
                    PluginLog.Information($"Loaded configuration v{versionNumber} (current)");
                }
                else if (versionNumber == 1)
                {
                    var configV1 = JsonConvert.DeserializeObject<SettingsV1>(fileText);
                    config = new Settings(configV1);
                    config.Save();
                    PluginLog.Information($"Converted configuration v{versionNumber} to v{Settings.CurrentVersion}");
                }
                else
                {
                    PluginLog.Error($"No reader available for configuration v{versionNumber}");
                }
            }
        }
        catch (Exception e)
        {
            PluginLog.Error("Could not read configuration.");
            PluginLog.Error(e.ToString());
        }

        if (config != null)
        {
            return config;
        }

        PluginLog.Information("Creating a new configuration.");
        return new Settings();
    }

    public void Save()
    {
        Service.PluginInterface.SavePluginConfig(this);
        OnSave?.Invoke();
    }

    private static int GetConfigFileVersion(string fileText)
    {
        var json = JObject.Parse(fileText);

        return json.GetValue("Version")?.Value<int>() ?? 0;
    }
}