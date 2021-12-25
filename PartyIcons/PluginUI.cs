using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using ImGuiNET;
using ImGuiScene;
using PartyIcons.View;

namespace PartyIcons
{
    class PluginUI : IDisposable
    {
        [PluginService] private DalamudPluginInterface Interface { get; set; }

        private readonly Configuration _configuration;
        private          bool          _settingsVisible = false;
        private          Vector2       _windowSize;

        public bool SettingsVisible
        {
            get { return this._settingsVisible; }
            set { this._settingsVisible = value; }
        }

        private Dictionary<NameplateMode, TextureWrap> _nameplateExamples;

        public PluginUI(Configuration configuration)
        {
            this._configuration = configuration;
        }

        public void Initialize()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var examplesImageNames = new Dictionary<NameplateMode, string>
            {
                { NameplateMode.SmallJobIcon, "PartyIcons.Resources.1.png" },
                { NameplateMode.BigJobIcon, "PartyIcons.Resources.2.png" },
                { NameplateMode.BigJobIconAndRole, "PartyIcons.Resources.3.png" },
                { NameplateMode.BigRole, "PartyIcons.Resources.4.png" },
            };

            _nameplateExamples = new Dictionary<NameplateMode, TextureWrap>();

            foreach (var kv in examplesImageNames)
            {
                using var fileStream = assembly.GetManifestResourceStream(kv.Value);
                if (fileStream == null)
                {
                    PluginLog.Error($"Failed to get resource stream for {kv.Value}");
                    continue;
                }

                using var memoryStream = new MemoryStream();
                fileStream.CopyTo(memoryStream);

                _nameplateExamples[kv.Key] = Interface.UiBuilder.LoadImage(memoryStream.ToArray());
            }
        }

        public void Dispose()
        {
        }

        public void OpenSettings()
        {
            SettingsVisible = true;
        }

        public void DrawSettingsWindow()
        {
            if (!SettingsVisible)
            {
                return;
            }

            if (_windowSize == default)
            {
                _windowSize = new Vector2(1200, 1400);
            }

            ImGui.SetNextWindowSize(_windowSize, ImGuiCond.Always);
            if (ImGui.Begin("PartyIcons", ref this._settingsVisible))
            {
                var testingMode = _configuration.TestingMode;
                if (ImGui.Checkbox("##testingMode", ref testingMode))
                {
                    _configuration.TestingMode = testingMode;
                    _configuration.Save();
                }
                ImGui.SameLine();
                ImGui.Text("Enable testing mode");
                ImGuiHelpTooltip("Applied settings to any player, contrary to only the ones that are in the party.");

                var chatContentMessage = _configuration.ChatContentMessage;
                if (ImGui.Checkbox("##chatmessage", ref chatContentMessage))
                {
                    _configuration.ChatContentMessage = chatContentMessage;
                    _configuration.Save();
                }
                ImGui.SameLine();
                ImGui.Text("Display chat message when entering duty");
                ImGuiHelpTooltip("Can be used to determine the duty type before fully loading in.");

                var easternNamingConvention = _configuration.EasternNamingConvention;
                if (ImGui.Checkbox("##easteannaming", ref easternNamingConvention))
                {
                    _configuration.EasternNamingConvention = easternNamingConvention;
                    _configuration.Save();
                }
                ImGui.SameLine();
                ImGui.Text("Eastern role naming convention");
                ImGuiHelpTooltip("Use japanese data center role naming convention (MT ST D1-D4 H1-2).");

                var hideLocalNameplate = _configuration.HideLocalPlayerNameplate;
                if (ImGui.Checkbox("##hidelocal", ref hideLocalNameplate))
                {
                    _configuration.HideLocalPlayerNameplate = hideLocalNameplate;
                    _configuration.Save();
                }
                ImGui.SameLine();
                ImGui.Text("Hide own nameplate");
                ImGuiHelpTooltip("You can turn your own nameplate on and also turn this\nsetting own to only use nameplate to display own raid position.\nIf you don't want your position displayed with this setting you can simply disable\nyour nameplates in the Character settings.");

                var showPlayerStatus = _configuration.ShowPlayerStatus;
                if (ImGui.Checkbox("##showplayerstatus", ref showPlayerStatus))
                {
                    _configuration.ShowPlayerStatus = showPlayerStatus;
                    _configuration.Save();
                }

                ImGui.SameLine();
                ImGui.Text("Show player status");
                ImGuiHelpTooltip("Display player status, or at least if it's a new adventurer or a mentor if possible");

                var framedSmallIcons = _configuration.FramedSmallIcons;
                if (ImGui.Checkbox("##framedsmallicons", ref framedSmallIcons))
                {
                    _configuration.FramedSmallIcons = framedSmallIcons;
                    _configuration.Save();
                }

                ImGui.SameLine();
                ImGui.Text("Show small icons as plates");
                ImGuiHelpTooltip("Small icons will be displayed in their framed version.");

                ImGui.Dummy(new Vector2(0, 25f));
                ImGui.Text("Dungeon:");
                ImGuiHelpTooltip("Modes used for your party while in dungeon.");
                NameplateModeSection("##np_dungeon", () => _configuration.NameplateDungeon, (mode) => _configuration.NameplateDungeon = mode);
                ImGui.SameLine();
                ChatModeSection("##chat_dungeon", () => _configuration.ChatDungeon, (mode) => _configuration.ChatDungeon = mode);
                ImGui.Dummy(new Vector2(0, 15f));

                ImGui.Text("Raid:");
                ImGuiHelpTooltip("Modes used for your party while in raid.");
                NameplateModeSection("##np_raid", () => _configuration.NameplateRaid, (mode) => _configuration.NameplateRaid = mode);
                ImGui.SameLine();
                ChatModeSection("##chat_raid", () => _configuration.ChatRaid, (mode) => _configuration.ChatRaid = mode);
                ImGui.Dummy(new Vector2(0, 15f));

                ImGui.Text("Alliance Raid party:");
                ImGuiHelpTooltip("Modes used for your party while in alliance raid.");
                NameplateModeSection("##np_alliance", () => _configuration.NameplateAllianceRaid, (mode) => _configuration.NameplateAllianceRaid = mode);
                ImGui.SameLine();
                ChatModeSection("##chat_alliance", () => _configuration.ChatAllianceRaid, (mode) => _configuration.ChatAllianceRaid = mode);
                ImGui.Dummy(new Vector2(0, 15f));

                ImGui.Text("Overworld party:");
                ImGuiHelpTooltip("Modes used for your party while not in duty.");
                NameplateModeSection("##np_overworld", () => _configuration.NameplateOverworld, (mode) => _configuration.NameplateOverworld = mode);
                ImGui.SameLine();
                ChatModeSection("##chat_overworld", () => _configuration.ChatOverworld, (mode) => _configuration.ChatOverworld = mode);
                ImGui.Dummy(new Vector2(0, 15f));

                ImGui.Text("Other player characters:");
                ImGuiHelpTooltip("Modes used for non-party players.");
                NameplateModeSection("##np_others", () => _configuration.NameplateOthers, (mode) => _configuration.NameplateOthers = mode);
                ImGui.SameLine();
                ChatModeSection("##chat_others", () => _configuration.ChatOthers, (mode) => _configuration.ChatOthers = mode);

                ImGui.Dummy(new Vector2(0, 25f));
                ImGui.TextWrapped("Please note that it usually takes a some time for nameplates to reload, especially for own character nameplate.");

                ImGui.Dummy(new Vector2(0, 15f));
                ImGui.Text("Nameplate examples:");

                foreach (var kv in _nameplateExamples)
                {
                    CollapsibleExampleImage(kv.Key, kv.Value);
                }
            }

            _windowSize = ImGui.GetWindowSize();
            ImGui.End();
        }

        private void CollapsibleExampleImage(NameplateMode mode, TextureWrap tex)
        {
            if (ImGui.CollapsingHeader(NameplateModeToString(mode)))
            {
                ImGui.Image(tex.ImGuiHandle, new Vector2(tex.Width, tex.Height));
            }
        }

        private void ImGuiHelpTooltip(string tooltip)
        {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1f), "?");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(tooltip);
            }
        }

        private void ChatModeSection(string label, Func<ChatMode> getter, Action<ChatMode> setter)
        {
            ImGui.Text("Chat name: ");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(400f);
            if (ImGui.BeginCombo(label, ChatModeToString(getter())))
            {
                foreach (var mode in Enum.GetValues<ChatMode>())
                {
                    if (ImGui.Selectable(ChatModeToString(mode), mode == getter()))
                    {
                        setter(mode);
                        _configuration.Save();
                    }
                }
                ImGui.EndCombo();
            }
        }

        private string ChatModeToString(ChatMode mode)
        {
            return mode switch
            {
                ChatMode.GameDefault      => "Game Default",
                ChatMode.Role             => "Role",
                ChatMode.Job              => "Job abbreviation",
                _                         => throw new ArgumentException(),
            };
        }

        private void NameplateModeSection(string label, Func<NameplateMode> getter, Action<NameplateMode> setter)
        {
            ImGui.Text("Nameplate: ");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(400f);
            if (ImGui.BeginCombo(label, NameplateModeToString(getter())))
            {
                foreach (var mode in Enum.GetValues<NameplateMode>())
                {
                    if (ImGui.Selectable(NameplateModeToString(mode), mode == getter()))
                    {
                        setter(mode);
                        _configuration.Save();
                    }
                }
                ImGui.EndCombo();
            }
        }

        private string NameplateModeToString(NameplateMode mode)
        {
            return mode switch
            {
                NameplateMode.Default               => "Game default",
                NameplateMode.BigJobIcon            => "Big job icon",
                NameplateMode.SmallJobIcon          => "Small job icon and name",
                NameplateMode.BigJobIconAndRole     => "Big job icon and role number",
                NameplateMode.BigRole               => "Role letters",
                NameplateMode.SmallJobIconOnly      => "small job icon without name",
                NameplateMode.SmallJobIconAndRole   => "small job icon and role number",
                NameplateMode.SmallRole             => "small job role letters",
                _                                   => throw new ArgumentException(),
            };
        }
    }
}