using System;
using System.Text;
using Dalamud.Game.Gui;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using PartyIcons.Entities;
using PartyIcons.Stylesheet;

namespace PartyIcons.Utils
{
    public unsafe class PartyListHUDView : IDisposable
    {
        private readonly PlayerStylesheet _stylesheet;
        private readonly GameGui          _gameGui;

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
            var hud = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentHUD();
            if (hud == null)
            {
                PluginLog.Warning("AgentHUD null!");
                return null;
            }

            if (hud->PartyMemberCount > 8)
            {
                // hud->PartyMemberCount gives out special (?) value when in trust
                PluginLog.Debug("GetPartySlotIndex - trust detected, returning null");
                return null;
            }

            var list = (HudPartyMember*)hud->PartyMemberList;
            for (var i = 0; i < hud->PartyMemberCount; i++)
            {
                if (list[i].ObjectId == objectId)
                {
                    return (uint)i;
                }
            }

            return null;
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
            var hud = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentHUD();
            if (hud == null)
            {
                PluginLog.Warning("AgentHUD null!");
                return null;
            }

            if (hud->PartyMemberCount > 8)
            {
                // hud->PartyMemberCount gives out special (?) value when in trust
                PluginLog.Debug("GetPartySlotIndex - trust detected, returning null");
                return "trust detected";
            }

            var result = new StringBuilder();
            var list = (HudPartyMember*)hud->PartyMemberList;
            for (var i = 0; i < hud->PartyMemberCount; i++)
            {
                var memberStruct = GetPartyMemberStruct((uint)i);
                if (memberStruct.HasValue)
                {
                    result.AppendLine($"member index {i} name {memberStruct.Value.Name->NodeText}");
                }
            }

            return result.ToString();
        }

        private AddonPartyList.PartyListMemberStruct? GetPartyMemberStruct(uint idx)
        {
            var partyListAddon = (AddonPartyList*)_gameGui.GetAddonByName("_PartyList", 1);
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

    }
}