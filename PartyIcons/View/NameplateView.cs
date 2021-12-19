﻿using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.IoC;
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
        private readonly int[] nameables = { 061523, 061540, 061542, 061543, 061544, 061547 };

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

        public void SetupForPC(XivApi.SafeNamePlateObject npObject)
        {
            switch (GetModeForNameplate(npObject))
            {
                case NameplateMode.Default:
                case NameplateMode.SmallJobIcon:
                    SetupDefault(npObject);
                    break;


                case NameplateMode.BigJobIcon:
                    npObject.SetIconPosition(-11, 24);
                    npObject.SetIconScale(3f);
                    npObject.SetNameScale(0.75f);
                    break;

                case NameplateMode.BigJobIconAndRole:
                    npObject.SetIconPosition(-14, 41);
                    npObject.SetIconScale(2.3f);
                    npObject.SetNameScale(1f);
                    break;

                case NameplateMode.BigRole:
                    npObject.SetIconScale(0f);
                    npObject.SetNameScale(1f);
                    break;

                case NameplateMode.SmallJobIconOnly:
                case NameplateMode.SmallJobIconAndRole:
                    npObject.SetIconPosition(16, 70);
                    npObject.SetIconScale(1f);
                    npObject.SetNameScale(0.7f);
                    break;


                case NameplateMode.SmallRole:
                    npObject.SetIconPosition(0, 72);
                    if(_configuration.ShowPlayerStatus)
                        npObject.SetIconScale(1f);
                    else 
                        npObject.SetIconScale(0f);
                    npObject.SetNameScale(0.5f);
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

                    case NameplateMode.BigJobIconAndRole:
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
                    if(_configuration.ShowPlayerStatus)
                        name = SeStringUtils.SeStringToPtr(GetStateNametextS(iconID, null).Append(SeStringUtils.SeStringFromPtr(name)));
                    iconID = GetClassIcon(npObject.NamePlateInfo, (_configuration.ShowPlayerStatus) ? iconID : -1);
                    break;

                case NameplateMode.BigJobIcon:
                    name = GetStateNametext(iconID);
                    fcName = SeStringUtils.emptyPtr; 
                    displayTitle = false;
                    iconID = GetClassIcon(npObject.NamePlateInfo);
                    break;

                case NameplateMode.BigJobIconAndRole:
                    fcName = SeStringUtils.emptyPtr;
                    displayTitle = false;
                    if (hasRole)
                    {
                        name = SeStringUtils.SeStringToPtr(_stylesheet.GetRolePlateNumber(roleId));
                        iconID = GetClassRoleColoredIcon(npObject.NamePlateInfo, roleId);
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
                        name = SeStringUtils.SeStringToPtr(_stylesheet.GetRolePlate(roleId).Append(GetStateNametextS(iconID)));
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
                    name = (_configuration.ShowPlayerStatus) ? GetStateNametext(iconID) : SeStringUtils.SeStringToPtr(SeStringUtils.Text(_iconPrefix));
                    fcName = SeStringUtils.emptyPtr;
                    displayTitle = false;
                    iconID = GetClassIcon(npObject.NamePlateInfo, (_configuration.ShowPlayerStatus) ? iconID : -1);
                    break;

                case NameplateMode.SmallJobIconAndRole:
                    fcName = SeStringUtils.emptyPtr;
                    displayTitle = false;
                    if (hasRole)
                    {
                        if(_configuration.ShowPlayerStatus)
                            name = SeStringUtils.SeStringToPtr(GetStateNametextS(iconID).Append(_stylesheet.GetRolePlateNumber(roleId)));
                        else
                            name = SeStringUtils.SeStringToPtr(_stylesheet.GetRolePlateNumber(roleId));
                        iconID = GetClassRoleColoredIcon(npObject.NamePlateInfo, roleId);
                    }
                    else
                    {
                        name = (_configuration.ShowPlayerStatus) ? GetStateNametext(iconID) : SeStringUtils.SeStringToPtr(SeStringUtils.Text(_iconPrefix));
                        iconID = GetClassIcon(npObject.NamePlateInfo, (_configuration.ShowPlayerStatus) ? iconID : -1);
                    }
                    break;

                case NameplateMode.SmallRole:
                    if (hasRole)
                    {
                        if (_configuration.ShowPlayerStatus)
                            name = SeStringUtils.SeStringToPtr(GetStateNametextS(iconID).Append(_stylesheet.GetRolePlate(roleId)));
                        else
                            name = SeStringUtils.SeStringToPtr(_stylesheet.GetRolePlate(roleId));
                    }
                    else
                    {

                        var genericRole = JobExtensions.GetRole((Job)npObject.NamePlateInfo.GetJobID());
                        if (_configuration.ShowPlayerStatus)
                            name = SeStringUtils.SeStringToPtr(GetStateNametextS(iconID).Append(_stylesheet.GetGenericRolePlate(genericRole)));
                        else
                            name = SeStringUtils.SeStringToPtr(_stylesheet.GetGenericRolePlate(genericRole));
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

            
            switch (JobExtensions.GetRole((Job)info.GetJobID()))
            {
                case GenericRole.Tank:
                    return _iconSet.GetJobIcon("Blue", info.GetJobID());

                case GenericRole.Healer:
                    return _iconSet.GetJobIcon("Green", info.GetJobID());

                case GenericRole.Melee:
                    return _iconSet.GetJobIcon("Red", info.GetJobID());

                case GenericRole.Ranged:
                    return _iconSet.GetJobIcon("Orange", info.GetJobID());

                case GenericRole.Crafter:
                    return _iconSet.GetJobIcon("Gold", info.GetJobID());

                case GenericRole.Gatherer:
                    return _iconSet.GetJobIcon("Yellow", info.GetJobID());
                default:
                    return 0;
            }
        }

        private int GetClassRoleColoredIcon(XivApi.SafeNamePlateInfo info, RoleId roleId, int def = -1)
        {
            if (def != -1 && !nameables.Contains(def))
                return def;

            return _iconSet.GetJobIcon(_stylesheet.GetRoleIconset(roleId), info.GetJobID());
        }

        private SeString GetStateNametextS(int iconId, string? prefix = _iconPrefix)
        {
            return iconId switch
            {
                061523 => SeStringUtils.Icon(BitmapFontIcon.NewAdventurer, prefix),
                061540 => SeStringUtils.Icon(BitmapFontIcon.Mentor, prefix),
                061542 => SeStringUtils.Icon(BitmapFontIcon.MentorPvP, prefix),
                061543 => SeStringUtils.Icon(BitmapFontIcon.MentorCrafting, prefix),
                061544 => SeStringUtils.Icon(BitmapFontIcon.MentorPvE, prefix),
                061547 => SeStringUtils.Icon(BitmapFontIcon.Returner, prefix),
                _ => SeStringUtils.Text(prefix + " ")
            };
        }

        private IntPtr GetStateNametext(int iconId)
        {
            return SeStringUtils.SeStringToPtr(GetStateNametextS(iconId));
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