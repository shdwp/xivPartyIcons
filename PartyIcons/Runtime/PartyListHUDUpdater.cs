using System;
using System.Linq;
using Dalamud.Game;
using Dalamud.Game.ClientState.Party;
using Dalamud.IoC;
using Dalamud.Logging;
using PartyIcons.Utils;

namespace PartyIcons.Runtime
{
    public sealed class PartyListHUDUpdater : IDisposable
    {
        public bool UpdateHUD = false;

        [PluginService] public PartyList PartyList { get; set; }
        [PluginService] public Framework Framework { get; set; }

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
            _roleTracker.OnAssignedRolesUpdated += OnRolesUpdated;
            Framework.Update += OnUpdate;
        }

        public void Dispose()
        {
            Framework.Update += OnUpdate;
            _roleTracker.OnAssignedRolesUpdated -= OnRolesUpdated;
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

        private void OnRolesUpdated()
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