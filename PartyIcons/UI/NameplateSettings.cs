using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Dalamud.Interface;
using Dalamud.Logging;
using ImGuiNET;
using ImGuiScene;
using PartyIcons.Configuration;

namespace PartyIcons.UI;

public sealed class NameplateSettings
{
    public NameplateSettings()
    {
        _nameplateExamples = new Dictionary<NameplateMode, TextureWrap>();
    }

    public void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var examplesImageNames = new Dictionary<NameplateMode, string>
        {
            {NameplateMode.SmallJobIcon, "PartyIcons.Resources.1.png"},
            {NameplateMode.BigJobIcon, "PartyIcons.Resources.2.png"},
            {NameplateMode.BigJobIconAndPartySlot, "PartyIcons.Resources.3.png"},
            {NameplateMode.RoleLetters, "PartyIcons.Resources.4.png"}
        };

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

            _nameplateExamples[kv.Key] = Service.PluginInterface.UiBuilder.LoadImage(memoryStream.ToArray());
        }
    }

    public void DrawNameplateSettings()
    {
        const float separatorPadding = 2f;
        
        ImGui.Dummy(new Vector2(0, 1f));
        ImGui.TextDisabled("Please note, it usually takes time for nameplates to reload.");
        ImGui.Dummy(new Vector2(0, 10f));
        
        var iconSetId = Plugin.Settings.IconSetId;
        ImGui.Text("Icon set:");
        ImGui.SameLine();
        SettingsWindow.SetComboWidth(Enum.GetValues<IconSetId>().Select(IconSetIdToString));

        if (ImGui.BeginCombo("##icon_set", IconSetIdToString(iconSetId)))
        {
            foreach (var id in Enum.GetValues<IconSetId>())
            {
                if (ImGui.Selectable(IconSetIdToString(id) + "##icon_set_" + id))
                {
                    Plugin.Settings.IconSetId = id;
                    Plugin.Settings.Save();
                }
            }

            ImGui.EndCombo();
        }

        var iconSizeMode = Plugin.Settings.SizeMode;
        ImGui.Text("Nameplate size:");
        ImGui.SameLine();
        SettingsWindow.SetComboWidth(Enum.GetValues<NameplateSizeMode>().Select(x => x.ToString()));

        if (ImGui.BeginCombo("##icon_size", iconSizeMode.ToString()))
        {
            foreach (var mode in Enum.GetValues<NameplateSizeMode>())
            {
                if (ImGui.Selectable(mode + "##icon_set_" + mode))
                {
                    Plugin.Settings.SizeMode = mode;
                    Plugin.Settings.Save();
                }
            }

            ImGui.EndCombo();
        }

        SettingsWindow.ImGuiHelpTooltip("Affects all presets, except Game Default and Small Job Icon.");
        
        var hideLocalNameplate = Plugin.Settings.HideLocalPlayerNameplate;

        if (ImGui.Checkbox("##hidelocal", ref hideLocalNameplate))
        {
            Plugin.Settings.HideLocalPlayerNameplate = hideLocalNameplate;
            Plugin.Settings.Save();
        }

        ImGui.SameLine();
        ImGui.Text("Hide own nameplate");
        SettingsWindow.ImGuiHelpTooltip(
            "You can turn your own nameplate on and also turn this\nsetting own to only use nameplate to display own raid position.\nIf you don't want your position displayed with this setting you can simply disable\nyour nameplates in the Character settings.");

        ImGui.Dummy(new Vector2(0f, 10f));
        
        ImGui.PushStyleColor(0, ImGuiHelpers.DefaultColorPalette()[0]);
        ImGui.Text("Overworld");
        ImGui.PopStyleColor();
        ImGui.Separator();
        ImGui.Dummy(new Vector2(0, separatorPadding));
        ImGui.Indent(15 * ImGuiHelpers.GlobalScale);
        {
            NameplateModeSection("##np_overworld", () => Plugin.Settings.NameplateOverworld,
                (mode) => Plugin.Settings.NameplateOverworld = mode,
                "Party:");

            NameplateModeSection("##np_others", () => Plugin.Settings.NameplateOthers,
                (mode) => Plugin.Settings.NameplateOthers = mode,
                "Others:");
        }
        ImGui.Indent(-15 * ImGuiHelpers.GlobalScale);
        ImGui.Dummy(new Vector2(0, 2f));
        
        ImGui.PushStyleColor(0, ImGuiHelpers.DefaultColorPalette()[0]);
        ImGui.Text("Instances");
        ImGui.PopStyleColor();
        ImGui.Separator();
        ImGui.Dummy(new Vector2(0, separatorPadding));
        ImGui.Indent(15 * ImGuiHelpers.GlobalScale);
        {
            NameplateModeSection("##np_dungeon", () => Plugin.Settings.NameplateDungeon,
                (mode) => Plugin.Settings.NameplateDungeon = mode,
                "Dungeon:");

            NameplateModeSection("##np_raid", () => Plugin.Settings.NameplateRaid,
                (mode) => Plugin.Settings.NameplateRaid = mode,
                "Raid:");

            NameplateModeSection("##np_alliance", () => Plugin.Settings.NameplateAllianceRaid,
                (mode) => Plugin.Settings.NameplateAllianceRaid = mode,
                "Alliance:");
        }
        ImGui.Indent(-15 * ImGuiHelpers.GlobalScale);
        ImGui.Dummy(new Vector2(0, 2f));
        
        ImGui.PushStyleColor(0, ImGuiHelpers.DefaultColorPalette()[0]);
        ImGui.Text("Forays");
        ImGui.PopStyleColor();
        ImGui.Separator();
        ImGui.Dummy(new Vector2(0, separatorPadding));
        ImGui.Indent(15 * ImGuiHelpers.GlobalScale);
        {
            ImGui.TextDisabled("e.g. Eureka, Bozja");

            NameplateModeSection("##np_bozja_party", () => Plugin.Settings.NameplateBozjaParty,
                mode => Plugin.Settings.NameplateBozjaParty = mode, "Party:");

            NameplateModeSection("##np_bozja_others", () => Plugin.Settings.NameplateBozjaOthers,
                mode => Plugin.Settings.NameplateBozjaOthers = mode, "Others:");
        }
        ImGui.Indent(-15 * ImGuiHelpers.GlobalScale);
        ImGui.Dummy(new Vector2(0, 2f));

        ImGui.PushStyleColor(0, ImGuiHelpers.DefaultColorPalette()[0]);
        ImGui.Text("PvP");
        ImGui.PopStyleColor();
        ImGui.Separator();
        ImGui.Dummy(new Vector2(0, 2f));
        ImGui.Indent(15 * ImGuiHelpers.GlobalScale);
        {
            ImGui.TextDisabled("This plugin is intentionally disabled during PvP matches.");
        }
        ImGui.Indent(-15 * ImGuiHelpers.GlobalScale);
        ImGui.Dummy(new Vector2(0, 10f));

        ImGui.Dummy(new Vector2(0, 10f));

        if (ImGui.CollapsingHeader("Examples"))
        {
            foreach (var kv in _nameplateExamples)
            {
                CollapsibleExampleImage(kv.Key, kv.Value);
            }
        }
    }

    private void CollapsibleExampleImage(NameplateMode mode, TextureWrap tex)
    {
        if (ImGui.CollapsingHeader(NameplateModeToString(mode)))
        {
            ImGui.Image(tex.ImGuiHandle, new Vector2(tex.Width, tex.Height));
        }
    }

    private static string IconSetIdToString(IconSetId id)
    {
        return id switch
        {
            IconSetId.Framed => "Framed, role colored",
            IconSetId.GlowingColored => "Glowing, role colored",
            IconSetId.GlowingGold => "Glowing, gold"
        };
    }

    private static string NameplateModeToString(NameplateMode mode)
    {
        return mode switch
        {
            NameplateMode.Default => "Game default",
            NameplateMode.Hide => "Hide",
            NameplateMode.BigJobIcon => "Big job icon",
            NameplateMode.SmallJobIcon => "Small job icon and name",
            NameplateMode.SmallJobIconAndRole => "Small job icon, role and name",
            NameplateMode.BigJobIconAndPartySlot => "Big job icon and party number",
            NameplateMode.RoleLetters => "Role letters",
            _ => throw new ArgumentException()
        };
    }

    private static void NameplateModeSection(string label, Func<NameplateMode> getter, Action<NameplateMode> setter, string title = "Nameplate: ")
    {
        ImGui.SetCursorPosY(ImGui.GetCursorPos().Y + 3f);
        ImGui.Text(title);
        ImGui.SameLine(100f);
        ImGui.SetCursorPosY(ImGui.GetCursorPos().Y - 3f);
        SettingsWindow.SetComboWidth(Enum.GetValues<NameplateMode>().Select(x => x.ToString()));

        // hack to fix incorrect configurations
        try
        {
            getter();
        }
        catch (ArgumentException ex)
        {
            setter(NameplateMode.Default);
            Plugin.Settings.Save();
        }

        if (ImGui.BeginCombo(label, NameplateModeToString(getter())))
        {
            foreach (var mode in Enum.GetValues<NameplateMode>())
            {
                if (ImGui.Selectable(NameplateModeToString(mode), mode == getter()))
                {
                    setter(mode);
                    Plugin.Settings.Save();
                }
            }

            ImGui.EndCombo();
        }
    }
    
    private readonly Dictionary<NameplateMode, TextureWrap> _nameplateExamples;
}