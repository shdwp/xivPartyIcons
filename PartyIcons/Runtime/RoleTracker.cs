using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.IoC;
using Dalamud.Logging;
using PartyIcons.Entities;

namespace PartyIcons.Runtime
{
    public sealed class RoleTracker : IDisposable
    {
        public event Action<string, RoleId> OnRoleOccupied;
        public event Action<string, RoleId> OnRoleSuggested;
        public event Action                 OnAssignedRolesUpdated;

        [PluginService] private Framework   Framework   { get; set; }
        [PluginService] private ChatGui     ChatGui     { get; set; }
        [PluginService] private ClientState ClientState { get; set; }
        [PluginService] private Condition   Condition   { get; set; }
        [PluginService] private PartyList   PartyList   { get; set; }
        [PluginService] private ToastGui    ToastGui    { get; set; }

        private bool _currentlyInParty;
        private uint _territoryId;
        private int  _previousStateHash;

        private Dictionary<RoleId, string> _rolePatterns          = new();
        private Dictionary<RoleId, Regex>  _roleSuggestionRegexes = new();

        private Dictionary<string, RoleId> _occupiedRoles   = new();
        private Dictionary<string, RoleId> _assignedRoles   = new();
        private Dictionary<string, RoleId> _suggestedRoles  = new();
        private HashSet<RoleId>            _unassignedRoles = new();

        public RoleTracker()
        {
            foreach (var role in Enum.GetValues<RoleId>())
            {
                var roleIdentifier = role.ToString().ToLower();
                var regex = new Regex($"\\W{roleIdentifier}\\W");

                _rolePatterns[role] = $" {roleIdentifier} ";
                _roleSuggestionRegexes[role] = regex;
            }
        }

        public void Enable()
        {
            ChatGui.ChatMessage += OnChatMessage;
            Framework.Update += FrameworkOnUpdate;
        }

        public void Disable()
        {
            ChatGui.ChatMessage -= OnChatMessage;
            Framework.Update -= FrameworkOnUpdate;
        }

        public void Dispose()
        {
            Disable();
        }

        public bool TryGetSuggestedRole(string name, uint worldId, out RoleId roleId)
        {
            return _suggestedRoles.TryGetValue(PlayerId(name, worldId), out roleId);
        }

        public bool TryGetAssignedRole(string name, uint worldId, out RoleId roleId)
        {
            return _assignedRoles.TryGetValue(PlayerId(name, worldId), out roleId);
        }

        public void OccupyRole(string name, uint world, RoleId roleId)
        {
            foreach (var kv in _occupiedRoles.ToArray())
            {
                if (kv.Value == roleId)
                {
                    _occupiedRoles.Remove(kv.Key);
                }
            }

            _occupiedRoles[PlayerId(name, world)] = roleId;
            OnRoleOccupied?.Invoke(name, roleId);
            ToastGui.ShowQuest($"{name} occupied {roleId}");
        }

        public void SuggestRole(string name, uint world, RoleId roleId)
        {
            _suggestedRoles[PlayerId(name, world)] = roleId;
            OnRoleSuggested?.Invoke(name, roleId);
            ToastGui.ShowQuest($"{roleId} is now suggested for {name}");
        }

        public void ResetOccupations()
        {
            PluginLog.Debug("Resetting occupation");
            _occupiedRoles.Clear();
        }

        public void ResetAssignments()
        {
            PluginLog.Debug("Resetting assignments");
            _assignedRoles.Clear();
            _unassignedRoles.Clear();

            foreach (var role in Enum.GetValues<RoleId>())
            {
                if (role != default)
                {
                    _unassignedRoles.Add(role);
                }
            }
        }

        public void CalculateUnassignedPartyRoles()
        {
            ResetAssignments();

            foreach (var kv in _occupiedRoles)
            {
                PluginLog.Debug($"{kv.Key} == {kv.Value} as per occupation");

                _assignedRoles[kv.Key] = kv.Value;
                _unassignedRoles.Remove(kv.Value);
            }

            foreach (var member in PartyList)
            {
                if (_assignedRoles.ContainsKey(PlayerId(member)))
                {
                    PluginLog.Debug($"{PlayerId(member)} has already been assigned a role");
                    continue;
                }

                RoleId roleToAssign = FindUnassignedRoleForMemberRole(JobRoleExtensions.RoleFromByte(member.ClassJob.GameData.Role));
                if (roleToAssign != default)
                {
                    PluginLog.Debug($"{PlayerId(member)} == {roleToAssign} as per first available");
                    _assignedRoles[PlayerId(member)] = roleToAssign;
                    _unassignedRoles.Remove(roleToAssign);
                }
            }

            OnAssignedRolesUpdated?.Invoke();
        }

        public string DebugDescription()
        {
            var sb = new StringBuilder();
            sb.Append($"Assignments:\n");
            foreach (var kv in _assignedRoles)
            {
                sb.Append($"Role {kv.Value} assigned to {kv.Key}\n");
            }

            sb.Append($"\nOccupations:\n");
            foreach (var kv in _occupiedRoles)
            {
                sb.Append($"Role {kv.Value} occupied by {kv.Key}\n");
            }

            sb.Append("\nUnassigned roles:\n");

            foreach (var k in _unassignedRoles)
            {
                sb.Append(" " + k);
            }

            return sb.ToString();
        }

        private void FrameworkOnUpdate(Framework framework)
        {
            if (!Condition[ConditionFlag.ParticipatingInCrossWorldPartyOrAlliance] && PartyList.Length == 0 && _occupiedRoles.Any())
            {
                PluginLog.Debug("Resetting occupations, no longer in a party");
                ResetOccupations();
                return;
            }

            var partyHash = 17;
            foreach (var member in PartyList)
            {
                unchecked
                {
                    partyHash = partyHash * 23 + (int)member.ObjectId;
                }
            }

            if (partyHash != _previousStateHash)
            {
                PluginLog.Debug($"Party hash changed ({partyHash}, prev {_previousStateHash}), recalculating roles");
                CalculateUnassignedPartyRoles();
            }

            _previousStateHash = partyHash;
        }

        private string PlayerId(string name, uint worldId)
        {
            return $"{name}@{worldId}";
        }

        private string PlayerId(PartyMember member)
        {
            return $"{member.Name.TextValue}@{member.World.Id}";
        }

        private RoleId FindUnassignedRoleForMemberRole(JobRole role)
        {
            RoleId roleToAssign = default;

            switch (role)
            {
                case JobRole.Tank:
                    roleToAssign = _unassignedRoles.FirstOrDefault(s => s == RoleId.MT || s == RoleId.OT);
                    break;

                case JobRole.Melee:
                    roleToAssign = _unassignedRoles.FirstOrDefault(s => s == RoleId.M1 || s == RoleId.M2);
                    if (roleToAssign == default)
                    {
                        roleToAssign = _unassignedRoles.FirstOrDefault(s => s == RoleId.R1 || s == RoleId.R2);
                    }
                    break;

                case JobRole.Ranged:
                    roleToAssign = _unassignedRoles.FirstOrDefault(s => s == RoleId.R1 || s == RoleId.R2);
                    if (roleToAssign == default)
                    {
                        roleToAssign = _unassignedRoles.FirstOrDefault(s => s == RoleId.M1 || s == RoleId.M2);
                    }
                    break;

                case JobRole.Healer:
                    roleToAssign = _unassignedRoles.FirstOrDefault(s => s == RoleId.H1 || s == RoleId.H2);
                    break;
            }

            return roleToAssign;
        }

        private void OnChatMessage(XivChatType type, uint senderid, ref SeString sender, ref SeString message, ref bool ishandled)
        {
            if (type == XivChatType.Party || type == XivChatType.CrossParty || type == XivChatType.Say)
            {
                string? playerName = null;
                uint? playerWorld = null;

                var playerPayload = sender.Payloads.FirstOrDefault(p => p is PlayerPayload) as PlayerPayload;
                if (playerPayload == null)
                {
                    PluginLog.Debug($"Message with senderid {senderid} and null player payload {sender} {message}");
                    playerName = ClientState.LocalPlayer.Name.TextValue;
                    playerWorld = ClientState.LocalPlayer.HomeWorld.Id;
                }
                else
                {
                    playerName = playerPayload?.PlayerName;
                    playerWorld = playerPayload?.World.RowId;
                }

                if (playerName == null || !playerWorld.HasValue)
                {
                    PluginLog.Debug($"Failed to get player data from {senderid}, {sender} ({sender.Payloads})");
                    return;
                }

                var text = message.TextValue.Trim().ToLower();
                var paddedText = $" {text} ";

                var assignmentsChanged = false;
                foreach (var role in Enum.GetValues<RoleId>())
                {
                    if (paddedText.Equals(_rolePatterns[role]))
                    {
                        PluginLog.Debug($"Message contained role occupation ({playerName}@{playerWorld} - {text}, detected role {role})");
                        assignmentsChanged = true;
                        OccupyRole(playerName, playerWorld.Value, role);
                        break;
                    }

                    if (_roleSuggestionRegexes[role].IsMatch(paddedText))
                    {
                        PluginLog.Debug($"Message contained role suggestion ({playerName}@{playerWorld}: {text}, detected {role}");
                        SuggestRole(playerName, playerWorld.Value, role);
                    }
                }

                if (assignmentsChanged)
                {
                    PluginLog.Debug($"Recalculating assignments due to new occupations");
                    CalculateUnassignedPartyRoles();
                }
            }
        }
    }
}
