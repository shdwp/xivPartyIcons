using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.IoC;
using Dalamud.Logging;
using PartyIcons.Api;
using PartyIcons.Entities;
using PartyIcons.Runtime;
using PartyIcons.Stylesheet;
using PartyIcons.Utils;
using System.Linq;

namespace PartyIcons.View
{
    public sealed class NameplateView : IDisposable
    {
        [PluginService] private ObjectTable ObjectTable { get; set; }

        private readonly Configuration        _configuration;
        private readonly PlayerStylesheet     _stylesheet;
        private readonly RoleTracker          _roleTracker;

        private readonly IconSet _iconSet;
        private const string _iconPrefix = "   ";
        private readonly int[] nameables = { 061521, 061522, 061523, 061540, 061542, 061543, 061544, 061547 };

        public NameplateMode PartyMode  { get; set; }
        public NameplateMode OthersMode { get; set; }

        [PluginService] private ClientState ClientState { get; set; }

        public NameplateView(RoleTracker roleTracker, Configuration configuration, PlayerStylesheet stylesheet)
        {
            _roleTracker = roleTracker;
            _configuration = configuration;
            _stylesheet = stylesheet;
            _iconSet = new IconSet();
        }

        public void Dispose()
        {
        }

        public void SetupDefault(XivApi.SafeNamePlateObject npObject)
        {
            npObject.SetIconScale(1f);
            npObject.SetNameScale(0.5f);
        }

        public void SetupForPC(XivApi.SafeNamePlateObject npObject, int oldIconID)
        {
            switch (GetModeForNameplate(npObject))
            {
                case NameplateMode.Default:
                case NameplateMode.SmallRole:
                    SetupDefault(npObject);
                    break;

                case NameplateMode.SmallJobIcon:
                case NameplateMode.SmallJobIconOnly:
                case NameplateMode.SmallJobIconAndPartySlot:
                    if (_configuration.FramedSmallIcons)
                    {
                        if (!_configuration.ShowPlayerStatus || IsIgnorableStatus(oldIconID))
                        {
                            npObject.AdjustIconPosition(16, 4);
                            npObject.SetIconScale(0.75f);
                            npObject.SetNameScale(0.5f);
                        }
                        else
                        {
                            npObject.AdjustIconPosition(10, 0);
                            SetupDefault(npObject);
                        }
                    }
                    else
                    {
                        npObject.AdjustIconPosition(10, 0);
                        SetupDefault(npObject);
                    }
                    break;

                case NameplateMode.BigJobIcon:
                    npObject.SetIconPosition(-11, 24);
                    npObject.SetIconScale(3f);
                    npObject.SetNameScale(0.75f);
                    break;

                case NameplateMode.BigJobIconAndPartySlot:
                    npObject.SetIconPosition(-14, 41);
                    npObject.SetIconScale(2.3f);
                    npObject.SetNameScale(1f);
                    break;

                case NameplateMode.BigRole:
                    npObject.SetIconScale(0f);
                    npObject.SetNameScale(1f);
                    break;
            }
        }

        public void NameplateDataForPC(
            XivApi.SafeNamePlateObject npObject,
            ref bool isPrefixTitle,
            ref bool displayTitle,
            ref IntPtr title,
            ref IntPtr name,
            ref IntPtr fcName,
            ref int iconID
        )
        {
            var uid = npObject.NamePlateInfo.Data.ObjectID.ObjectID;
            var mode = GetModeForNameplate(npObject);

            if (_configuration.HideLocalPlayerNameplate && uid == ClientState.LocalPlayer?.ObjectId)
            {
                switch (mode)
                {
                    case NameplateMode.Default:
                    case NameplateMode.SmallJobIcon:
                    case NameplateMode.BigJobIcon:
                        name = SeStringUtils.emptyPtr;
                        fcName = SeStringUtils.emptyPtr;
                        displayTitle = false;
                        iconID = 0;
                        return;

                    case NameplateMode.BigJobIconAndPartySlot:
                    case NameplateMode.BigRole:
                        if (!_configuration.TestingMode && !npObject.NamePlateInfo.IsPartyMember())
                        {
                            name = SeStringUtils.emptyPtr;
                            fcName = SeStringUtils.emptyPtr;
                            displayTitle = false;
                            iconID = 0;
                            return;
                        }
                        break;
                }
            }

            var playerCharacter = ObjectTable.SearchById(uid) as PlayerCharacter;
            if (playerCharacter == null)
            {
                return;
            }

            var hasRole = _roleTracker.TryGetAssignedRole(playerCharacter.Name.TextValue, playerCharacter.HomeWorld.Id, out var roleId);
            switch (mode)
            {
                case NameplateMode.Default:
                    break;

                case NameplateMode.SmallJobIcon:
                    name = GetStateNametext(_configuration.ShowPlayerStatus ? iconID : -1, _iconPrefix, SeStringUtils.SeStringFromPtr(name));
                    iconID = GetClassIcon(npObject.NamePlateInfo, (_configuration.ShowPlayerStatus) ? iconID : -1, _configuration.FramedSmallIcons);
                    break;

                case NameplateMode.BigJobIcon:
                    name = GetStateNametext(iconID);
                    fcName = SeStringUtils.emptyPtr;
                    displayTitle = false;
                    iconID = GetClassIcon(npObject.NamePlateInfo);
                    break;

                case NameplateMode.BigJobIconAndPartySlot:
                    fcName = SeStringUtils.emptyPtr;
                    displayTitle = false;
                    var partySlot = PartyListHUD.GetPartySlotNumber(npObject.NamePlateInfo.Data.ObjectID.ObjectID);
                    if (partySlot != null)
                    {
                        var genericRole = JobExtensions.GetRole((Job)npObject.NamePlateInfo.GetJobID());
                        var str = _stylesheet.GetPartySlotNumber(partySlot.Value, genericRole);
                        str.Payloads.Insert(0, new TextPayload("    "));
                        name = SeStringUtils.SeStringToPtr(str);
                        iconID = GetClassIcon(npObject.NamePlateInfo);
                    }
                    else
                    {
                        name = SeStringUtils.emptyPtr;
                        iconID = GetClassIcon(npObject.NamePlateInfo);
                    }
                    break;

                case NameplateMode.BigRole:
                    if (hasRole)
                    {
                        name = SeStringUtils.SeStringToPtr(_stylesheet.GetRolePlate(roleId));
                    }
                    else
                    {
                        var genericRole = JobExtensions.GetRole((Job)npObject.NamePlateInfo.GetJobID());
                        name = SeStringUtils.SeStringToPtr(_stylesheet.GetGenericRolePlate(genericRole));
                    }

                    fcName = SeStringUtils.emptyPtr;
                    displayTitle = false;
                    break;
 
                case NameplateMode.SmallJobIconOnly:
                    name = _configuration.ShowPlayerStatus ? GetStateNametext(iconID) : SeStringUtils.emptyPtr;
                    fcName = SeStringUtils.emptyPtr;
                    displayTitle = false;
                    iconID = GetClassIcon(npObject.NamePlateInfo, _configuration.ShowPlayerStatus ? iconID : -1, _configuration.FramedSmallIcons);
                    break;

                case NameplateMode.SmallJobIconAndPartySlot:
                    fcName = SeStringUtils.emptyPtr;
                    displayTitle = false;
                    partySlot = PartyListHUD.GetPartySlotNumber(npObject.NamePlateInfo.Data.ObjectID.ObjectID);
                    if (partySlot != null)
                    {
                        var genericRole = JobExtensions.GetRole((Job)npObject.NamePlateInfo.GetJobID());
                        if (_configuration.ShowPlayerStatus)
                            name = GetStateNametext(iconID, _iconPrefix, _stylesheet.GetPartySlotNumber(partySlot.Value, genericRole));
                        else
                            name = GetStateNametext(-1, _iconPrefix, _stylesheet.GetPartySlotNumber(partySlot.Value, genericRole));

                        iconID = GetClassIcon(npObject.NamePlateInfo, _configuration.ShowPlayerStatus ? iconID : -1, _configuration.FramedSmallIcons);
                    }
                    else
                    {
                        name = _configuration.ShowPlayerStatus ? GetStateNametext(iconID) : SeStringUtils.emptyPtr;
                        iconID = GetClassIcon(npObject.NamePlateInfo, _configuration.ShowPlayerStatus ? iconID : -1, _configuration.FramedSmallIcons);
                    }
                    break;

                case NameplateMode.SmallRole:
                    if (!_configuration.ShowPlayerStatus)
                        iconID = -1;
                    if (hasRole)
                    {
                        name = SeStringUtils.SeStringToPtr(iconID == -1 ?  _stylesheet.GetRolePlate(roleId) : SeStringUtils.Text(" ").Append(_stylesheet.GetRolePlate(roleId)));
                    }
                    else
                    {
                        var genericRole = JobExtensions.GetRole((Job)npObject.NamePlateInfo.GetJobID());
                        name = SeStringUtils.SeStringToPtr(iconID == -1 ? _stylesheet.GetGenericRolePlate(genericRole) : SeStringUtils.Text(" ").Append(_stylesheet.GetGenericRolePlate(genericRole)));
                    }

                    fcName = SeStringUtils.emptyPtr;
                    displayTitle = false;
                    break;                    
            }
        }

        private int GetClassIcon(XivApi.SafeNamePlateInfo info, int def = -1, bool framed = false)
        {
            if (def != -1 && !nameables.Contains(def))
                return def;

            var jobID = info.GetJobID();
            if (framed)
            {
                return _iconSet.GetJobIcon("Framed", jobID);
            }

            switch (JobExtensions.GetRole((Job)jobID))
            {
                case GenericRole.Tank:
                    return _iconSet.GetJobIcon("Blue", jobID);

                case GenericRole.Healer:
                    return _iconSet.GetJobIcon("Green", jobID);

                case GenericRole.Melee:
                    return _iconSet.GetJobIcon("Red", jobID);

                case GenericRole.Ranged:
                    return _iconSet.GetJobIcon("Orange", jobID);

                case GenericRole.Crafter:
                    return _iconSet.GetJobIcon("Gold", jobID);

                case GenericRole.Gatherer:
                    return _iconSet.GetJobIcon("Yellow", jobID);
                default:
                    return 0;
            }
        }

        private bool IsIgnorableStatus(int statusIcon)
        {
            return statusIcon == -1 || nameables.Contains(statusIcon);
        }

        private int GetClassRoleColoredIcon(XivApi.SafeNamePlateInfo info, RoleId roleId, int def = -1)
        {
            if (!IsIgnorableStatus(def))
                return def;

            return _iconSet.GetJobIcon(_stylesheet.GetRoleIconset(roleId), info.GetJobID());
        }

        private SeString GetStateNametextS(int iconId, string? prefix = _iconPrefix, SeString? append = null)
        {

            SeString? val = iconId switch
            {
                //061521 - party leader
                //061522 - party member
                061523 => SeStringUtils.Icon(BitmapFontIcon.NewAdventurer, prefix),
                061540 => SeStringUtils.Icon(BitmapFontIcon.Mentor, prefix),
                061542 => SeStringUtils.Icon(BitmapFontIcon.MentorPvP, prefix),
                061543 => SeStringUtils.Icon(BitmapFontIcon.MentorCrafting, prefix),
                061544 => SeStringUtils.Icon(BitmapFontIcon.MentorPvE, prefix),
                061547 => SeStringUtils.Icon(BitmapFontIcon.Returner, prefix),
                _ => null
            };

            return append == null ? val == null ? SeString.Empty : val : val == null ? prefix == null ? append : SeStringUtils.Text(_iconPrefix).Append(append) : val.Append(append);
        }

        private IntPtr GetStateNametext(int iconId, string? prefix = _iconPrefix, SeString? append = null)
        {
            return SeStringUtils.SeStringToPtr(GetStateNametextS(iconId, prefix, append));
        }

        private NameplateMode GetModeForNameplate(XivApi.SafeNamePlateObject npObject)
        {
            var uid = npObject.NamePlateInfo.Data.ObjectID.ObjectID;
            var mode = OthersMode;
            if (_configuration.TestingMode || npObject.NamePlateInfo.IsPartyMember() || uid == ClientState.LocalPlayer?.ObjectId)
            {
                return PartyMode;
            }
            else
            {
                return OthersMode;
            }
        }
    }
}