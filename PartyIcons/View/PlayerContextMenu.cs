using System;
using Dalamud.Logging;
using PartyIcons.Entities;
using PartyIcons.Runtime;
using PartyIcons.Stylesheet;
using XivCommon;
using XivCommon.Functions.ContextMenu;

namespace PartyIcons.View
{
    public sealed class PlayerContextMenu : IDisposable
    {
        private readonly XivCommonBase    _base;
        private readonly RoleTracker      _roleTracker;
        private readonly PlayerStylesheet _stylesheet;

        public PlayerContextMenu(XivCommonBase @base, RoleTracker roleTracker, PlayerStylesheet stylesheet)
        {
            _base = @base;
            _roleTracker = roleTracker;
            _stylesheet = stylesheet;
        }

        public void Enable()
        {
            _base.Functions.ContextMenu.OpenContextMenu += OnOpenContextMenu;
        }

        public void Disable()
        {
            _base.Functions.ContextMenu.OpenContextMenu -= OnOpenContextMenu;
        }

        public void Dispose()
        {
            Disable();
        }

        private void OnOpenContextMenu(ContextMenuOpenArgs args)
        {
            if (!IsMenuValid(args))
            {
                return;
            }

            PluginLog.Debug($"Opening submenu for {args.ObjectId}");

            if (_roleTracker.TryGetSuggestedRole(args.Text.TextValue, args.ObjectWorld, out var role))
            {
                var roleName = _stylesheet.GetRoleName(role);
                args.Items.Add(new NormalContextMenuItem($"Assign to {roleName} (suggested)", (args) => OnAssignRole(args, role)));
            }

            if (_roleTracker.TryGetAssignedRole(args.Text.TextValue, args.ObjectWorld, out var currentRole))
            {
                var swappedRole = RoleIdUtils.Counterpart(currentRole);
                var swappedRoleName = _stylesheet.GetRoleName(swappedRole);
                args.Items.Add(new NormalContextMenuItem($"Party role swap to {swappedRoleName}", (args) => OnAssignRole(args, swappedRole)));
            }

            args.Items.Add(new NormalContextSubMenuItem("Party role assign ", OnAssignSubMenuOpen));
        }

        private void OnAssignRole(ContextMenuItemSelectedArgs args, RoleId roleId)
        {
            _roleTracker.OccupyRole(args.Text.TextValue, args.ObjectWorld, roleId);
            _roleTracker.CalculateUnassignedPartyRoles();
        }

        private void OnAssignSubMenuOpen(ContextMenuOpenArgs args)
        {
            foreach (var role in Enum.GetValues<RoleId>())
            {
                if (role == RoleId.Undefined)
                {
                    continue;
                }

                args.Items.Add(new NormalContextMenuItem(_stylesheet.GetRoleName(role), (args) => OnAssignRole(args, role)));
            }
        }

        private bool IsMenuValid(BaseContextMenuArgs args)
        {
            switch (args.ParentAddonName)
            {
                case null: // Nameplate/Model menu
                case "LookingForGroup":
                case "PartyMemberList":
                case "FriendList":
                case "FreeCompany":
                case "SocialList":
                case "ContactList":
                case "ChatLog":
                case "_PartyList":
                case "LinkShell":
                case "CrossWorldLinkshell":
                case "ContentMemberList": // Eureka/Bozja/...
                    return args.Text != null && args.ObjectWorld != 0 && args.ObjectWorld != 65535;

                default:
                    return false;
            }
        }
    }
}