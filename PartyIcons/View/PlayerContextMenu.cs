using System;
using Dalamud.IoC;
using Dalamud.Logging;
using PartyIcons.Entities;
using PartyIcons.Runtime;
using PartyIcons.Stylesheet;
using Dalamud.ContextMenu;
using PartyIcons.Configuration;

namespace PartyIcons.View
{
    public sealed class PlayerContextMenu : IDisposable
    {
        private DalamudContextMenu _contextMenu = new();
        
        // Whether to indicate context menu items are from Dalamud.
        // Setting this to true at least sets apart the menu items given that submenus are not currently supported in Dalamud.ContextMenu.
        private static bool _useDalamudIndicator = true;
        
        private readonly RoleTracker _roleTracker;
        private readonly Settings _configuration;
        private readonly PlayerStylesheet _stylesheet;

        public PlayerContextMenu(RoleTracker roleTracker, Settings configuration, PlayerStylesheet stylesheet)
        {
            _roleTracker = roleTracker;
            _configuration = configuration;
            _stylesheet = stylesheet;
        }

        public void Enable()
        {
            _contextMenu.OnOpenGameObjectContextMenu += OnOpenContextMenu;
        }

        public void Disable()
        {
            _contextMenu.OnOpenGameObjectContextMenu -= OnOpenContextMenu;
        }

        public void Dispose()
        {
            Disable();
        }
        
        private void OnOpenContextMenu(GameObjectContextMenuOpenArgs args)
        {
            if (!_configuration.UseContextMenu || args.Text == null || !IsMenuValid(args))
            {
                return;
            }
        
            var playerName = args.Text.TextValue;
            var playerWorld = args.ObjectWorld;
        
            PluginLog.Verbose($"Opening menu for {playerName}");
        
            AddSuggestedRoleMenuItem(playerName, playerWorld, args);
            AddSwapRoleMenuItem(playerName, playerWorld, args);
            AddAssignPartyRoleMenuItems(playerName, playerWorld, args);
        }
        
        private void AddSuggestedRoleMenuItem(string playerName, ushort playerWorld, GameObjectContextMenuOpenArgs args)
        {
            if (_roleTracker.TryGetSuggestedRole(playerName, playerWorld, out var role))
            {
                var roleName = _stylesheet.GetRoleName(role);
        
                var contextMenuItem = new GameObjectContextMenuItem(
                    $"Assign {roleName} (suggested)",
                    _ => OnAssignRole(playerName, playerWorld, role),
                    _useDalamudIndicator);
        
                args.AddCustomItem(contextMenuItem);
            }
        }
        
        private void AddSwapRoleMenuItem(string playerName, ushort playerWorld, GameObjectContextMenuOpenArgs args)
        {
            if (_roleTracker.TryGetAssignedRole(playerName, playerWorld, out var currentRole))
            {
                var swappedRole = RoleIdUtils.Counterpart(currentRole);
                var swappedRoleName = _stylesheet.GetRoleName(swappedRole);
        
                var contextMenuItem = new GameObjectContextMenuItem(
                    $"Swap to {swappedRoleName}",
                    _ => OnAssignRole(playerName, playerWorld, swappedRole),
                    _useDalamudIndicator);
        
                args.AddCustomItem(contextMenuItem);
            }
        }
        
        private void AddAssignPartyRoleMenuItems(string playerName, ushort playerWorld, GameObjectContextMenuOpenArgs args)
        {
            foreach (var role in Enum.GetValues<RoleId>())
            {
                if (role == RoleId.Undefined)
                {
                    continue;
                }
        
                var contextMenuItem = new GameObjectContextMenuItem(
                    $"Assign {_stylesheet.GetRoleName(role)}",
                    _ => OnAssignRole(playerName, playerWorld, role),
                    _useDalamudIndicator);
        
                args.AddCustomItem(contextMenuItem);
            }
        }
        
        private void OnAssignRole(string playerName, ushort playerWorld, RoleId role)
        {
            _roleTracker.OccupyRole(playerName, playerWorld, role);
        
            _roleTracker.CalculateUnassignedPartyRoles();
        }
        
        private bool IsMenuValid(GameObjectContextMenuOpenArgs args)
        {
            PluginLog.LogDebug($"ParentAddonName {args.ParentAddonName}");
        
            switch (args.ParentAddonName)
            {
                case null: // Nameplate/Model menu
                case "PartyMemberList":
                case "ChatLog":
                case "_PartyList":
                case "ContentMemberList": // Eureka/Bozja/...
                    return args.Text != null &&
                           args.ObjectWorld != 0 && // Player
                           args.ObjectWorld != 65535;
        
                default:
                    return false;
            }
        }
    }
}
