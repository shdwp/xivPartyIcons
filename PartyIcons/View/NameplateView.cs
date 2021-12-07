using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.IoC;
using Dalamud.Logging;
using PartyIcons.Api;
using PartyIcons.Entities;
using PartyIcons.Runtime;
using PartyIcons.Utils;

namespace PartyIcons.View
{
    public sealed class NameplateView : IDisposable
    {
        [PluginService] private PartyList   PartyList   { get; set; }
        [PluginService] private ObjectTable ObjectTable { get; set; }

        private RoleTracker _roleTracker;
        private IconSet     _iconSet;

        private IntPtr[]                _roleStringsWestern;
        private IntPtr[]                _roleStringsEastern;
        private IntPtr[]                _numberStrings;
        private IntPtr[]                _unknownRoleStrings;
        private Dictionary<int, IntPtr> _statusIconStrings;
        private IntPtr                  _bigIconNamePadding;

        public NameplateMode PartyMode  { get; set; }
        public NameplateMode OthersMode { get; set; }

        // tank, melee, ranged, healer
        private readonly ushort[]      _roleColors   = { 37, 524, 32, 42 };
        private readonly string[]      _roleIconSets = { "Grey", "Blue", "Blue", "Red", "Red", "Orange", "Orange", "Green", "Green", };
        private readonly Configuration _configuration;

        [PluginService] private ClientState ClientState { get; set; }

        public NameplateView(RoleTracker roleTracker, Configuration configuration)
        {
            _roleTracker = roleTracker;
            _configuration = configuration;
            OthersMode = NameplateMode.BigJobIcon;

            _iconSet = new IconSet();

            _roleStringsWestern = new[]
            {
                SeStringUtils.EmptyPtr,
                SeStringUtils.TextPtr("", _roleColors[0]),
                SeStringUtils.TextPtr("", _roleColors[0]),
                SeStringUtils.TextPtr("", _roleColors[1]),
                SeStringUtils.TextPtr("", _roleColors[1]),
                SeStringUtils.TextPtr("", _roleColors[2]),
                SeStringUtils.TextPtr("", _roleColors[2]),
                SeStringUtils.TextPtr("", _roleColors[3]),
                SeStringUtils.TextPtr("", _roleColors[3]),
            };

            _roleStringsEastern = new[]
            {
                SeStringUtils.EmptyPtr,
                SeStringUtils.TextPtr("", _roleColors[0]),
                SeStringUtils.TextPtr("", _roleColors[0]),
                SeStringUtils.TextPtr("", _roleColors[1]),
                SeStringUtils.TextPtr("", _roleColors[1]),
                SeStringUtils.TextPtr("", _roleColors[2]),
                SeStringUtils.TextPtr("", _roleColors[2]),
                SeStringUtils.TextPtr("", _roleColors[3]),
                SeStringUtils.TextPtr("", _roleColors[3]),
            };

            _unknownRoleStrings = new[]
            {
                SeStringUtils.TextPtr(""),
                SeStringUtils.TextPtr("", _roleColors[0]),
                SeStringUtils.TextPtr("", _roleColors[1]),
                SeStringUtils.TextPtr("", _roleColors[2]),
                SeStringUtils.TextPtr("", _roleColors[3]),
            };

            var numberOne = "   ";
            var numberTwo = "   ";

            _numberStrings = new[]
            {
                SeStringUtils.EmptyPtr,
                SeStringUtils.TextPtr(numberOne, _roleColors[0]),
                SeStringUtils.TextPtr(numberTwo, _roleColors[0]),
                SeStringUtils.TextPtr(numberOne, _roleColors[1]),
                SeStringUtils.TextPtr(numberTwo, _roleColors[1]),
                SeStringUtils.TextPtr(numberOne, _roleColors[2]),
                SeStringUtils.TextPtr(numberTwo, _roleColors[2]),
                SeStringUtils.TextPtr(numberOne, _roleColors[3]),
                SeStringUtils.TextPtr(numberTwo, _roleColors[3]),
            };

            _statusIconStrings = new Dictionary<int, IntPtr>();
            var prefix = "   ";
            _statusIconStrings[061523] = SeStringUtils.IconPtr(BitmapFontIcon.NewAdventurer, prefix);
            _statusIconStrings[061540] = SeStringUtils.IconPtr(BitmapFontIcon.Mentor, prefix);
            _statusIconStrings[061543] = SeStringUtils.IconPtr(BitmapFontIcon.MentorCrafting, prefix);
            _statusIconStrings[061542] = SeStringUtils.IconPtr(BitmapFontIcon.MentorPvE, prefix);
            _statusIconStrings[061547] = SeStringUtils.IconPtr(BitmapFontIcon.Returner, prefix);

            _bigIconNamePadding = SeStringUtils.TextPtr(prefix + " ");
        }

        public void Dispose()
        {
            foreach (var ptr in _roleStringsWestern)
            {
                Marshal.FreeHGlobal(ptr);
            }
            _roleStringsWestern = null;

            foreach (var ptr in _roleStringsEastern)
            {
                Marshal.FreeHGlobal(ptr);
            }
            _roleStringsEastern = null;

            foreach (var ptr in _numberStrings)
            {
                Marshal.FreeHGlobal(ptr);
            }
            _numberStrings = null;

            foreach (var ptr in _statusIconStrings.Values)
            {
                Marshal.FreeHGlobal(ptr);
            }
            _statusIconStrings = null;

            Marshal.FreeHGlobal(_bigIconNamePadding);
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
                        name = SeStringUtils.EmptyPtr;
                        fcName = SeStringUtils.EmptyPtr;
                        displayTitle = false;
                        iconID = 0;
                        return;

                    case NameplateMode.BigJobIconAndRole:
                    case NameplateMode.BigRole:
                        if (!_configuration.TestingMode && !npObject.NamePlateInfo.IsPartyMember())
                        {
                            name = SeStringUtils.EmptyPtr;
                            fcName = SeStringUtils.EmptyPtr;
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
                    iconID = GetClassIcon(npObject.NamePlateInfo);
                    break;

                case NameplateMode.BigJobIcon:
                    name = GetStateNametext(iconID);
                    fcName = SeStringUtils.EmptyPtr;
                    displayTitle = false;
                    iconID = GetClassIcon(npObject.NamePlateInfo);
                    break;

                case NameplateMode.BigJobIconAndRole:
                    fcName = SeStringUtils.EmptyPtr;
                    displayTitle = false;
                    if (hasRole)
                    {
                        name = _numberStrings[(int)roleId];
                        iconID = GetClassRoleColoredIcon(npObject.NamePlateInfo, roleId);
                    }
                    else
                    {
                        name = SeStringUtils.EmptyPtr;
                        iconID = GetClassIcon(npObject.NamePlateInfo);
                    }
                    break;

                case NameplateMode.BigRole:
                    if (hasRole)
                    {
                        if (_configuration.EasternNamingConvention)
                        {
                            name = _roleStringsEastern[(int)roleId];
                        }
                        else
                        {
                            name = _roleStringsWestern[(int)roleId];
                        }
                    }
                    else
                    {
                        var generalRole = JobExtensions.GetRole((Job)npObject.NamePlateInfo.GetJobID());
                        if ((int)generalRole <= 3)
                        {
                            name = _unknownRoleStrings[(int)generalRole + 1];
                        }
                        else
                        {
                            name = _unknownRoleStrings[0];
                        }
                    }

                    fcName = SeStringUtils.EmptyPtr;
                    displayTitle = false;
                    break;
            }
        }

        private int GetClassIcon(XivApi.SafeNamePlateInfo info)
        {
            switch (JobExtensions.GetRole((Job)info.GetJobID()))
            {
                case JobRole.Tank:
                    return _iconSet.GetJobIcon("Blue", info.GetJobID());

                case JobRole.Healer:
                    return _iconSet.GetJobIcon("Green", info.GetJobID());

                case JobRole.Melee:
                    return _iconSet.GetJobIcon("Red", info.GetJobID());

                case JobRole.Ranged:
                    return _iconSet.GetJobIcon("Orange", info.GetJobID());

                default:
                    return 0;

            }
        }

        private int GetClassRoleColoredIcon(XivApi.SafeNamePlateInfo info, RoleId roleId)
        {
            return _iconSet.GetJobIcon(_roleIconSets[(int)roleId], info.GetJobID());
        }

        private IntPtr GetStateNametext(int iconId)
        {
            if (_statusIconStrings.TryGetValue(iconId, out var ptr))
            {
                return ptr;
            }
            else
            {
                return _bigIconNamePadding;
            }
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