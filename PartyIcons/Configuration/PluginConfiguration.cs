using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using Dalamud.Plugin;
using PartyIcons.Entities;
using PartyIcons.View;

namespace PartyIcons.Configuration;

[Serializable]
public class PluginConfiguration : IPluginConfiguration
{
    public static int CurrentVersion = 2;
    
    public event Action OnSave;

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

    public ChatConfig ChatOverworld { get; set; } = new ChatConfig(ChatMode.Role, true);
    public ChatConfig ChatAllianceRaid { get; set; } = new ChatConfig(ChatMode.Role, true);
    public ChatConfig ChatDungeon { get; set; } = new ChatConfig(ChatMode.Job, true);
    public ChatConfig ChatRaid { get; set; } = new ChatConfig(ChatMode.Role, true);
    public ChatConfig ChatOthers { get; set; } = new ChatConfig(ChatMode.Job, true);

    public Dictionary<string, RoleId> StaticAssignments { get; set; } = new();

    private DalamudPluginInterface _interface;

    public PluginConfiguration()
    {
        
    }

    public PluginConfiguration(ConfigurationV1 configV1)
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

        ChatOverworld = ConfigurationV1.ToChatConfig(configV1.ChatOverworld);
        ChatAllianceRaid = ConfigurationV1.ToChatConfig(configV1.ChatAllianceRaid);
        ChatDungeon = ConfigurationV1.ToChatConfig(configV1.ChatDungeon);
        ChatRaid = ConfigurationV1.ToChatConfig(configV1.ChatRaid);
        ChatOthers = ConfigurationV1.ToChatConfig(configV1.ChatOthers);

        StaticAssignments = configV1.StaticAssignments;
    }

    public void Save()
    {
        Service.PluginInterface.SavePluginConfig(this);
        OnSave?.Invoke();
    }
}

public class ConfigurationV1
{
    public enum ChatModeV1
    {
        GameDefault,
        OnlyColor,
        Role,
        Job
    }

    public static ChatMode Convert(ChatModeV1 chatModeV1)
    {
        switch (chatModeV1)
        {
            case ChatModeV1.GameDefault:
            case ChatModeV1.OnlyColor:
                return ChatMode.GameDefault;
            
            case ChatModeV1.Role:
                return ChatMode.Job;

            case ChatModeV1.Job:
                return ChatMode.Job;
            default:
                throw new ArgumentOutOfRangeException(nameof(chatModeV1), chatModeV1, null);
        }
    }

    public static ChatConfig ToChatConfig(ChatModeV1 chatModeV1)
    {
        var chatMode = Convert(chatModeV1);

        return new ChatConfig(chatMode);
    }
    
    public int Version { get; set; } = 1;
    
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

    public ChatModeV1 ChatOverworld { get; set; } = ChatModeV1.Role;
    public ChatModeV1 ChatAllianceRaid { get; set; } = ChatModeV1.Role;
    public ChatModeV1 ChatDungeon { get; set; } = ChatModeV1.Job;
    public ChatModeV1 ChatRaid { get; set; } = ChatModeV1.Role;
    public ChatModeV1 ChatOthers { get; set; } = ChatModeV1.Job;

    public Dictionary<string, RoleId> StaticAssignments { get; set; } = new();
}
