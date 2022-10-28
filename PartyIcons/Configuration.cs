using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using Dalamud.Plugin;
using PartyIcons.Entities;
using PartyIcons.View;

namespace PartyIcons;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public event Action OnSave;

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

    public ChatConfig ChatOverworld { get; set; } = new ChatConfig(ChatMode.Role, true);
    public ChatConfig ChatAllianceRaid { get; set; } = new ChatConfig(ChatMode.Role, true);
    public ChatConfig ChatDungeon { get; set; } = new ChatConfig(ChatMode.Job, true);
    public ChatConfig ChatRaid { get; set; } = new ChatConfig(ChatMode.Role, true);
    public ChatConfig ChatOthers { get; set; } = new ChatConfig(ChatMode.Job, true);

    public Dictionary<string, RoleId> StaticAssignments { get; set; } = new();

    private DalamudPluginInterface _interface;

    public void Initialize(DalamudPluginInterface @interface)
    {
        _interface = @interface;
    }

    public void Save()
    {
        _interface.SavePluginConfig(this);
        OnSave?.Invoke();
    }
}
