using System;
using System.Linq;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Network;
using Dalamud.IoC;
using Dalamud.Logging;
using PartyIcons.Utils;

namespace PartyIcons.Runtime
{
    public sealed class PartyListHUDUpdater : IDisposable
    {
        public bool UpdateHUD = false;

        [PluginService] public PartyList   PartyList   { get; set; }
        [PluginService] public Framework   Framework   { get; set; }
        [PluginService] public GameNetwork GameNetwork { get; set; }
        [PluginService] public ClientState ClientState { get; set; }

        private readonly Configuration    _configuration;
        private readonly PartyListHUDView _view;
        private readonly RoleTracker      _roleTracker;

        private bool _previousInParty = false;

        public PartyListHUDUpdater(PartyListHUDView view, RoleTracker roleTracker, Configuration configuration)
        {
            _view = view;
            _roleTracker = roleTracker;
            _configuration = configuration;
        }

        public void Enable()
        {
            _roleTracker.OnAssignedRolesUpdated += OnAssignedRolesUpdated;
            Framework.Update += OnUpdate;
            GameNetwork.NetworkMessage += OnNetworkMessage;
        }

        public void Dispose()
        {
            GameNetwork.NetworkMessage -= OnNetworkMessage;
            Framework.Update -= OnUpdate;
            _roleTracker.OnAssignedRolesUpdated -= OnAssignedRolesUpdated;
        }

        private void OnAssignedRolesUpdated()
        {
            PluginLog.Debug("PartyListHUDUpdater forcing update due to assignments update");
            UpdatePartyListHUD();
        }

        private void OnNetworkMessage(IntPtr dataptr, ushort opcode, uint sourceactorid, uint targetactorid, NetworkMessageDirection direction)
        {
            if (direction == NetworkMessageDirection.ZoneDown && opcode == 0x2ac && targetactorid == ClientState.LocalPlayer?.ObjectId)
            {
                PluginLog.Debug("PartyListHUDUpdater Forcing update due to zoning");
                UpdatePartyListHUD();
            }
        }

        private void OnUpdate(Framework framework)
        {
            var inParty = PartyList.Any();
            if (!inParty && _previousInParty)
            {
                PluginLog.Debug("No longer in party, reverting party list HUD changes");
                _view.RevertSlotNumbers();
            }

            _previousInParty = inParty;
        }

        private void UpdatePartyListHUD()
        {
            if (!_configuration.DisplayRoleInPartyList)
            {
                return;
            }

            if (!UpdateHUD)
            {
                return;
            }

            PluginLog.Debug("Updating party list HUD");
            foreach (var member in PartyList)
            {
                var index = _view.GetPartySlotIndex(member.ObjectId);
                if (index != null && _roleTracker.TryGetAssignedRole(member.Name.ToString(), member.World.Id, out var role))
                {
                    PluginLog.Debug($"Updating party list hud: member {member.Name} index {index} to {role}");
                    _view.SetPartyMemberRole(index.Value, role);
                }
            }
        }
    }
}