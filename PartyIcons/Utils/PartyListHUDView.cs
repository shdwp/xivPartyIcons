using System;
using System.Text;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using PartyIcons.Entities;
using PartyIcons.Stylesheet;

namespace PartyIcons.Utils;

public unsafe class PartyListHUDView : IDisposable
{
    // [PluginService]
    // public PartyList PartyList { get; set; }

    private readonly PlayerStylesheet _stylesheet;
    private readonly GameGui _gameGui;

    public PartyListHUDView(GameGui gameGui, PlayerStylesheet stylesheet)
    {
        _gameGui = gameGui;
        _stylesheet = stylesheet;
    }

    public void Dispose()
    {
        RevertSlotNumbers();
    }

    public void RevertSlotNumbers()
    {
        for (uint i = 0; i < 8; i++)
        {
            var memberStructOptional = GetPartyMemberStruct(i);

            if (!memberStructOptional.HasValue)
            {
                PluginLog.Warning($"Failed to dispose member HUD changes - struct null!");

                continue;
            }

            var memberStruct = memberStructOptional.Value;
            var nameNode = memberStruct.Name;
            nameNode->AtkResNode.SetPositionShort(19, 0);

            var numberNode = nameNode->AtkResNode.PrevSiblingNode->GetAsAtkTextNode();
            numberNode->AtkResNode.SetPositionShort(0, 0);
            numberNode->SetText(_stylesheet.BoxedCharacterString((i + 1).ToString()));
        }
    }

    public uint? GetPartySlotIndex(uint objectId)
    {
        var hud =
            FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule()->GetAgentModule()->
                GetAgentHUD();

        if (hud == null)
        {
            PluginLog.Warning("AgentHUD null!");

            return null;
        }

        // 9 instead of 8 is used here, in case the player has a pet out
        if (hud->PartyMemberCount > 9)
        {
            // hud->PartyMemberCount gives out special (?) value when in trust
            PluginLog.Verbose("GetPartySlotIndex - trust detected, returning null");

            return null;
        }

        var list = (HudPartyMember*) hud->PartyMemberList;

        for (var i = 0; i < hud->PartyMemberCount; i++)
        {
            if (list[i].ObjectId == objectId)
            {
                return (uint) i;
            }
        }

        return null;
    }

    public void SetPartyMemberRole(string name, uint objectId, RoleId roleId)
    {
        var index = GetPartySlotIndex(objectId);

        for (uint i = 0; i < 8; i++)
        {
            var memberStruct = GetPartyMemberStruct(i);

            if (memberStruct.HasValue)
            {
                var nameString = memberStruct.Value.Name->NodeText.ToString();
                var strippedName = StripSpecialCharactersFromName(nameString);

                if (name.Contains(strippedName))
                {
                    if (!index.HasValue || index.Value != i)
                    {
                        PluginLog.Warning("PartyHUD and HUDAgent id's mismatch!");
                        // PluginLog.Warning(GetDebugInfo());
                    }

                    SetPartyMemberRole(i, roleId);

                    return;
                }
            }
        }

        PluginLog.Verbose($"Member struct by the name {name} not found.");
    }

    public void SetPartyMemberRole(uint index, RoleId roleId)
    {
        var memberStructOptional = GetPartyMemberStruct(index);

        if (!memberStructOptional.HasValue)
        {
            PluginLog.Warning($"Failed to set party member HUD role to {roleId} - struct null!");

            return;
        }

        var memberStruct = memberStructOptional.Value;

        var nameNode = memberStruct.Name;
        nameNode->AtkResNode.SetPositionShort(29, 0);

        var numberNode = nameNode->AtkResNode.PrevSiblingNode->GetAsAtkTextNode();
        numberNode->AtkResNode.SetPositionShort(6, 0);

        var seString = _stylesheet.GetRolePlate(roleId);
        var buf = seString.Encode();

        fixed (byte* ptr = buf)
        {
            numberNode->SetText(ptr);
        }
    }

    public string GetDebugInfo()
    {
        var hud =
            FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule()->GetAgentModule()->
                GetAgentHUD();

        if (hud == null)
        {
            PluginLog.Warning("AgentHUD null!");

            return null;
        }

        if (hud->PartyMemberCount > 9)
        {
            // hud->PartyMemberCount gives out special (?) value when in trust
            PluginLog.Verbose("GetPartySlotIndex - trust detected, returning null");

            return null;
        }

        var result = new StringBuilder();
        result.AppendLine($"PARTY ({Service.PartyList.Length}):");

        foreach (var member in Service.PartyList)
        {
            var index = GetPartySlotIndex(member.ObjectId);
            result.AppendLine(
                $"PartyList name {member.Name} oid {member.ObjectId} worldid {member.World.Id} slot index {index}");
        }

        result.AppendLine("STRUCTS:");
        var memberList = (HudPartyMember*) hud->PartyMemberList;

        for (var i = 0; i < Math.Min(hud->PartyMemberCount, 8u); i++)
        {
            var memberStruct = GetPartyMemberStruct((uint) i);

            if (memberStruct.HasValue)
            {
                /*
                for (var pi = 0; pi < memberStruct.Value.ClassJobIcon->PartsList->PartCount; pi++)
                {
                    var part = memberStruct.Value.ClassJobIcon->PartsList->Parts[pi];
                    result.Append($"icon {part.UldAsset->AtkTexture.Resource->TexFileResourceHandle->ResourceHandle.FileName}");
                }
                */

                var strippedName = StripSpecialCharactersFromName(memberStruct.Value.Name->NodeText.ToString());
                result.AppendLine(
                    $"PartyMemberStruct index {i} name '{strippedName}', id matched {memberList[i].ObjectId}");

                var byteCount = 0;

                while (byteCount < 16 && memberList[i].Name[byteCount++] != 0) { }

                var memberListName = Encoding.UTF8.GetString(memberList[i].Name, byteCount - 1);
                result.AppendLine($"HudPartyMember index {i} name {memberListName} {memberList[i].ObjectId}");
            }
            else
            {
                result.AppendLine($"PartyMemberStruct null at {i}");
            }
        }

        return result.ToString();
    }

    private AddonPartyList.PartyListMemberStruct? GetPartyMemberStruct(uint idx)
    {
        var partyListAddon = (AddonPartyList*) _gameGui.GetAddonByName("_PartyList", 1);

        if (partyListAddon == null)
        {
            PluginLog.Warning("PartyListAddon null!");

            return null;
        }

        return idx switch
        {
            0 => partyListAddon->PartyMember.PartyMember0,
            1 => partyListAddon->PartyMember.PartyMember1,
            2 => partyListAddon->PartyMember.PartyMember2,
            3 => partyListAddon->PartyMember.PartyMember3,
            4 => partyListAddon->PartyMember.PartyMember4,
            5 => partyListAddon->PartyMember.PartyMember5,
            6 => partyListAddon->PartyMember.PartyMember6,
            7 => partyListAddon->PartyMember.PartyMember7,
            _ => throw new ArgumentException($"Invalid index: {idx}")
        };
    }

    private string StripSpecialCharactersFromName(string name)
    {
        var result = new StringBuilder();

        for (var i = 0; i < name.Length; i++)
        {
            var ch = name[i];

            if (ch >= 65 && ch <= 90 || ch >= 97 && ch <= 122 || ch == 45 || ch == 32 || ch == 39)
            {
                result.Append(name[i]);
            }
        }

        return result.ToString().Trim();
    }
}
