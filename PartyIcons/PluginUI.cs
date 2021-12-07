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

                ImGui.Text("Overworld party nameplates:");
                ImGuiHelpTooltip("Nameplates used for your party while not in duty.");
                ModeSection("##overworld", () => _configuration.Overworld, (mode) => _configuration.Overworld = mode);

                ImGui.Text("Dungeon nameplates:");
                ImGuiHelpTooltip("Nameplates used for your party while in dungeon.");
                ModeSection("##dungeon", () => _configuration.Dungeon, (mode) => _configuration.Dungeon = mode);

                ImGui.Text("Raid nameplates:");
                ImGuiHelpTooltip("Nameplates used for your party while in raid.");
                ModeSection("##raid", () => _configuration.Raid, (mode) => _configuration.Raid = mode);

                ImGui.Text("Alliance Raid party nameplates:");
                ImGuiHelpTooltip("Nameplates used for your party while in alliance raid.");
                ModeSection("##alliance", () => _configuration.AllianceRaid, (mode) => _configuration.AllianceRaid = mode);

                ImGui.Text("Other PC nameplates:");
                ImGuiHelpTooltip("Nameplates used for non-party players.");
                ModeSection("##others", () => _configuration.Others, (mode) => _configuration.Others = mode);

                ImGui.Dummy(new Vector2(0, 25f));
                ImGui.TextWrapped("Nameplates are only applied when you are in a party with other people, unless testing mode is enabled. ");
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

        private void ModeSection(string label, Func<NameplateMode> getter, Action<NameplateMode> setter)
        {
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

        private void ImGuiHelpTooltip(string tooltip)
        {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1f), "?");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(tooltip);
            }
        }

        private string NameplateModeToString(NameplateMode mode)
        {
            return mode switch
            {
                NameplateMode.Default           => "Game default",
                NameplateMode.BigJobIcon        => "Big job icon",
                NameplateMode.SmallJobIcon      => "Small job icon and name",
                NameplateMode.BigJobIconAndRole => "Big job icon and role number",
                NameplateMode.BigRole           => "Role letters",
                _                               => throw new ArgumentException(),
            };
        }
    }
}