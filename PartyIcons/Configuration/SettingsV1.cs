using System;
using System.Collections.Generic;
using PartyIcons.Entities;

namespace PartyIcons.Configuration;

public class SettingsV1
{
    public enum ChatModeV1
    {
        GameDefault,
        OnlyColor,
        Role,
        Job
    }

    public static ChatConfig ToChatConfig(ChatModeV1 chatModeV1)
    {
        switch (chatModeV1)
        {
            case ChatModeV1.GameDefault:
                return new ChatConfig(ChatMode.GameDefault, useRoleColor: false);
                
            case ChatModeV1.OnlyColor:
                return new ChatConfig(ChatMode.GameDefault, useRoleColor: true);
            
            case ChatModeV1.Role:
                return new ChatConfig(ChatMode.Role, useRoleColor: true);

            case ChatModeV1.Job:
                return new ChatConfig(ChatMode.Job, useRoleColor: true);
            
            default:
                throw new ArgumentOutOfRangeException(nameof(chatModeV1), chatModeV1, null);
        }
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