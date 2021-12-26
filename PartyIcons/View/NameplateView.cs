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

namespace PartyIcons.View
{
    public sealed class NameplateView : IDisposable
    {
        [PluginService] private ObjectTable ObjectTable { get; set; }

        private readonly Configuration    _configuration;
        private readonly PlayerStylesheet _stylesheet;
        private readonly RoleTracker      _roleTracker;

        private readonly IconSet _iconSet;

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
                    npObject.SetNameScale(0.75f);

                    switch (_configuration.IconSizeMode)
                    {
                        case IconSizeMode.Smaller:
                            npObject.SetIconPosition(9, 60);
                            npObject.SetIconScale(1.5f);
                            break;

                        case IconSizeMode.Medium:
                            npObject.SetIconPosition(-12, 24);
                            npObject.SetIconScale(3f);
                            break;

                        case IconSizeMode.Bigger:
                            npObject.SetIconPosition(-27, -12);
                            npObject.SetIconScale(4f);
                            break;
                    }
                    break;

                case NameplateMode.BigJobIconAndPartySlot:
                    switch (_configuration.IconSizeMode)
                    {
                        case IconSizeMode.Smaller:
                            npObject.SetIconPosition(12, 68);
                            npObject.SetIconScale(1.2f);
                            npObject.SetNameScale(0.6f);
                            break;

                        case IconSizeMode.Medium:
                            npObject.SetIconPosition(-14, 41);
                            npObject.SetIconScale(2.3f);
                            npObject.SetNameScale(1f);
                            break;

                        case IconSizeMode.Bigger:
                            npObject.SetIconPosition(-32, 15);
                            npObject.SetIconScale(3f);
                            npObject.SetNameScale(1.5f);
                            break;
                    }
                    break;

                case NameplateMode.BigRole:
                    npObject.SetIconScale(0f);
                    npObject.SetNameScale(_configuration.IconSizeMode switch
                    {
                        IconSizeMode.Smaller => 0.5f,
                        IconSizeMode.Medium  => 1f,
                        IconSizeMode.Bigger  => 1.5f,
                    });
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
                    case NameplateMode.BigJobIconAndPartySlot:
                        name = SeStringUtils.emptyPtr;
                        fcName = SeStringUtils.emptyPtr;
                        displayTitle = false;
                        iconID = 0;
                        return;

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
                    iconID = GetClassIcon(npObject.NamePlateInfo);
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
                        str.Payloads.Insert(0, new TextPayload("   "));
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
            }
        }

        private int GetClassIcon(XivApi.SafeNamePlateInfo info)
        {
            var genericRole = JobExtensions.GetRole((Job)info.GetJobID());
            var iconSet = _stylesheet.GetGenericRoleIconset(genericRole);
            return _iconSet.GetJobIcon(iconSet, info.GetJobID());
        }

        private IntPtr GetStateNametext(int iconId)
        {
            var prefix = "   ";
            return iconId switch
            {
                061523 => SeStringUtils.SeStringToPtr(SeStringUtils.Icon(BitmapFontIcon.NewAdventurer, prefix)),
                061540 => SeStringUtils.SeStringToPtr(SeStringUtils.Icon(BitmapFontIcon.Mentor, prefix)),
                061543 => SeStringUtils.SeStringToPtr(SeStringUtils.Icon(BitmapFontIcon.Mentor, prefix)),
                061542 => SeStringUtils.SeStringToPtr(SeStringUtils.Icon(BitmapFontIcon.Mentor, prefix)),
                061547 => SeStringUtils.SeStringToPtr(SeStringUtils.Icon(BitmapFontIcon.Mentor, prefix)),
                _      => SeStringUtils.SeStringToPtr(SeStringUtils.Text(prefix + " "))
            };
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