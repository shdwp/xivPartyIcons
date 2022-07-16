using System;
using Dalamud.Game.Gui.ContextMenus;
using Dalamud.IoC;
using Dalamud.Logging;
using PartyIcons.Entities;
using PartyIcons.Runtime;
using PartyIcons.Stylesheet;

namespace PartyIcons.View
{
    public sealed class PlayerContextMenu : IDisposable
    {
        [PluginService] private ContextMenu ContextMenu { get; set; }

        private readonly RoleTracker _roleTracker;
        private readonly PlayerStylesheet _stylesheet;

        public PlayerContextMenu(RoleTracker roleTracker, PlayerStylesheet stylesheet)
        {
            _roleTracker = roleTracker;
            _stylesheet = stylesheet;
        }

        public void Enable()
        {
            ContextMenu.ContextMenuOpened += OnOpenContextMenu;
        }

        public void Disable()
        {
            ContextMenu.ContextMenuOpened -= OnOpenContextMenu;
        }

        public void Dispose()
        {
            Disable();
        }

        private void OnOpenContextMenu(ContextMenuOpenedArgs args)
        {
            if (!IsMenuValid(args))
            {
                return;
            }

            var playerName = args.GameObjectContext.Name;
            var playerWorld = args.GameObjectContext.WorldId.Value;
            PluginLog.Debug($"Opening submenu for {playerName}");

            if (_roleTracker.TryGetSuggestedRole(playerName, playerWorld, out var role))
            {
                var roleName = _stylesheet.GetRoleName(role);
                args.AddCustomItem($"Assign to {roleName} (suggested)", args => OnAssignRole(args, role));
            }

            if (_roleTracker.TryGetAssignedRole(playerName, playerWorld, out var currentRole))
            {
                var swappedRole = RoleIdUtils.Counterpart(currentRole);
                var swappedRoleName = _stylesheet.GetRoleName(swappedRole);
                args.AddCustomItem($"Party role swap to {swappedRoleName}", args => OnAssignRole(args, swappedRole));
            }

            args.AddCustomSubMenu("Party role assign ", OnAssignSubMenuOpen);
        }

        private void OnAssignRole(CustomContextMenuItemSelectedArgs args, RoleId roleId)
        {
            _roleTracker.OccupyRole(args.ContextMenuOpenedArgs.GameObjectContext.Name, args.ContextMenuOpenedArgs.GameObjectContext.WorldId.Value, roleId);
            _roleTracker.CalculateUnassignedPartyRoles();
        }

        private void OnAssignSubMenuOpen(ContextMenuOpenedArgs args)
        {
            foreach (var role in Enum.GetValues<RoleId>())
            {
                if (role == RoleId.Undefined)
                {
                    continue;
                }

                args.AddCustomItem(_stylesheet.GetRoleName(role), (args) => OnAssignRole(args, role));
            }

            args.AddCustomItem("Return", _ => { });
        }

        private bool IsMenuValid(ContextMenuOpenedArgs args)
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
                    return args.GameObjectContext.Name != null && args.GameObjectContext.WorldId != 0 &&
                           args.GameObjectContext.WorldId != 65535;

                default:
                    return false;
            }
        }
    }
}