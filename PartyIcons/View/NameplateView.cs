using System;
using System.Collections.Generic;
using System.Numerics;
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

        private readonly Configuration    _configuration;
        private readonly PlayerStylesheet _stylesheet;
        private readonly RoleTracker      _roleTracker;
        private readonly PartyListHUDView _partyListHudView;

        private readonly IconSet _iconSet;
        private const string _iconPrefix = "   ";
        private readonly int[] nameables = { 061521, 061522, 061523, 061540, 061542, 061543, 061544, 061547 };

        public NameplateMode PartyMode  { get; set; }
        public NameplateMode OthersMode { get; set; }

        [PluginService] private ClientState ClientState { get; set; }

        public NameplateView(RoleTracker roleTracker, Configuration configuration, PlayerStylesheet stylesheet, PartyListHUDView partyListHudView)
        {
            _roleTracker = roleTracker;
            _configuration = configuration;
            _stylesheet = stylesheet;
            _partyListHudView = partyListHudView;
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

        public void SetupForPC(XivApi.SafeNamePlateObject npObject, int oldIconId)
        {
            var nameScale = 0.75f;
            var iconScale = 1f;
            var iconOffset = new Vector2(0, 0);

            switch (GetModeForNameplate(npObject))
            {
                case NameplateMode.Default:
                    SetupDefault(npObject);
                    return;

                case NameplateMode.SmallJobIcon:

                    if (_configuration.IconSetId == IconSetId.Framed)
                    {
                        if (oldIconId != -1 && !nameables.Contains(oldIconId))
                        {
                            SetupDefault(npObject);
                            npObject.AdjustIconPosition(10, 0);
                            return;
                        }
                        npObject.SetIconScale(0.75f);
                        npObject.SetNameScale(0.5f);
                        npObject.AdjustIconPosition(14, 4);
                    }
                    else
                    {
                        SetupDefault(npObject);
                        npObject.AdjustIconPosition(10, 0);
                    }
                    return;

                case NameplateMode.BigJobIcon:
                    nameScale = 0.75f;

                    switch (_configuration.SizeMode)
                    {
                        case NameplateSizeMode.Smaller:
                            iconOffset = new Vector2(9, 50);
                            iconScale = 1.5f;
                            break;

                        case NameplateSizeMode.Medium:
                            iconOffset = new Vector2(-12, 24);
                            iconScale = 3f;
                            break;

                        case NameplateSizeMode.Bigger:
                            iconOffset = new Vector2(-27, -12);
                            iconScale = 4f;
                            break;

                        case NameplateSizeMode.Tiny:
                            iconOffset = new Vector2(15, 74);
                            iconScale = 1f;
                            nameScale = 0.5f;
                            break;
                    }
                    break;

                case NameplateMode.BigJobIconAndPartySlot:
                    switch (_configuration.SizeMode)
                    {
                        case NameplateSizeMode.Smaller:
                            iconOffset = new Vector2(12, 62);
                            iconScale = 1.2f;
                            nameScale = 0.6f;
                            break;

                        case NameplateSizeMode.Medium:
                            iconOffset = new Vector2(-14, 41);
                            iconScale = 2.3f;
                            nameScale = 1f;
                            break;

                        case NameplateSizeMode.Bigger:
                            iconOffset = new Vector2(-32, 15);
                            iconScale = 3f;
                            nameScale = 1.5f;
                            break;

                        case NameplateSizeMode.Tiny:
                            iconOffset = new Vector2(15, 74);
                            iconScale = 1f;
                            nameScale = 0.5f;
                            break;
                    }
                    break;

                case NameplateMode.RoleLetters:
                    if (_configuration.ShowPlayerStatus)
                        switch (_configuration.SizeMode)
                        {
                            case NameplateSizeMode.Smaller:
                            case NameplateSizeMode.Tiny:
                                iconOffset = new Vector2(15, 74);
                                iconScale = 1f;
                                break;
                            case NameplateSizeMode.Medium:
                                iconOffset = new Vector2(0, 53);
                                iconScale = 1.5f;
                                break;
                            case NameplateSizeMode.Bigger:
                                iconOffset = new Vector2(-15, 34);
                                iconScale = 2f;
                                break;
                        }
                    else iconScale = 0f;

                    nameScale = _configuration.SizeMode switch
                    {
                        NameplateSizeMode.Smaller => 0.5f,
                        NameplateSizeMode.Medium  => 1f,
                        NameplateSizeMode.Bigger  => 1.5f,
                        NameplateSizeMode.Tiny => 0.5f
                    };
                    break;
            }

            if (GetModeForNameplate(npObject) < NameplateMode.RoleLetters && _configuration.IconSetId == IconSetId.Framed)
            {
                iconScale *=  0.75f;
                iconOffset.Y += 4;
                iconOffset.X += 2;
            }

            npObject.SetIconPosition((short)iconOffset.X, (short)iconOffset.Y);
            npObject.SetIconScale(iconScale);
            npObject.SetNameScale(nameScale);
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
                    case NameplateMode.BigJobIconAndPartySlot:
                        name = SeStringUtils.emptyPtr;
                        fcName = SeStringUtils.emptyPtr;
                        displayTitle = false;
                        iconID = -1;
                        return;

                    case NameplateMode.RoleLetters:
                        if (!_configuration.TestingMode && !npObject.NamePlateInfo.IsPartyMember())
                        {
                            name = SeStringUtils.emptyPtr;
                            fcName = SeStringUtils.emptyPtr;
                            displayTitle = false;
                            iconID = -1;
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
                    iconID = GetClassIcon(npObject.NamePlateInfo, (_configuration.ShowPlayerStatus) ? iconID : -1);
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
                    var partySlot = _partyListHudView.GetPartySlotIndex(npObject.NamePlateInfo.Data.ObjectID.ObjectID) + 1;
                    if (partySlot != null)
                    {
                        var genericRole = JobExtensions.GetRole((Job)npObject.NamePlateInfo.GetJobID());
                        //var str = _stylesheet.GetPartySlotNumber(partySlot.Value, genericRole);
                        //str.Payloads.Insert(0, new TextPayload(_iconPrefix));
                        name = GetStateNametext(_configuration.ShowPlayerStatus ? iconID : -1, _iconPrefix, _stylesheet.GetPartySlotNumber(partySlot.Value, genericRole));
                        iconID = GetClassIcon(npObject.NamePlateInfo);
                    }
                    else
                    {
                        name = GetStateNametext(_configuration.ShowPlayerStatus ? iconID : -1);
                        iconID = GetClassIcon(npObject.NamePlateInfo);
                    }
                    break;

                case NameplateMode.RoleLetters:
                    if (hasRole)
                    {
                        name = GetStateNametext(-1, _configuration.ShowPlayerStatus ? _iconPrefix : null, _stylesheet.GetRolePlate(roleId));
                    }
                    else
                    {
                        var genericRole = JobExtensions.GetRole((Job)npObject.NamePlateInfo.GetJobID());
                        name = GetStateNametext(-1, _configuration.ShowPlayerStatus ? _iconPrefix : null, _stylesheet.GetGenericRolePlate(genericRole));
                    }

                    fcName = SeStringUtils.emptyPtr;
                    displayTitle = false;
                    break;
            }
        }

        private int GetClassIcon(XivApi.SafeNamePlateInfo info, int def = -1)
        {
            if (def != -1 && !nameables.Contains(def))
                return def;
        	
            var genericRole = JobExtensions.GetRole((Job)info.GetJobID());
            var iconSet = _stylesheet.GetGenericRoleIconset(genericRole);
            return _iconSet.GetJobIcon(iconSet, info.GetJobID());
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
                061542 => SeStringUtils.Icon(BitmapFontIcon.MentorPvE, prefix),
                061543 => SeStringUtils.Icon(BitmapFontIcon.MentorCrafting, prefix),
                061544 => SeStringUtils.Icon(BitmapFontIcon.MentorPvP, prefix),
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