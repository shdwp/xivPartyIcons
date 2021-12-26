using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace PartyIcons.Utils
{
    public static class PartyListHUD
    {
        public static unsafe uint? GetPartySlotNumber(uint objectId)
        {
            var hud = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentHUD();
            var list = (HudPartyMember*)hud->PartyMemberList;

            var result = new List<uint>();
            for (var i = 0; i < hud->PartyMemberCount; i++)
            {
                if (list[i].ObjectId == objectId)
                {
                    return (uint)i + 1;
                }
            }

            return null;
        }
    }
}