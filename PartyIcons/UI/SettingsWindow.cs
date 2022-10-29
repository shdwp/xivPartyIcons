using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Logging;
using ImGuiNET;
using ImGuiScene;
using PartyIcons.Configuration;
using PartyIcons.Entities;
using PartyIcons.Stylesheet;
using PartyIcons.Utils;

namespace PartyIcons.UI;

public sealed class SettingsWindow : IDisposable
{
    public bool SettingsVisible
    {
        get => _settingsVisible;
        set
        {
            _settingsVisible = value;

            if (value)
            {
                _windowSizeHelper.ForceSize();
            }
        }
    }

    public void Initialize()
    {
        Service.PluginInterface.UiBuilder.Draw += DrawSettingsWindow;
        Service.PluginInterface.UiBuilder.OpenConfigUi += OpenSettingsWindow;
        
        _generalSettings.Initialize();
        _nameplateSettings.Initialize();
    }

    public void Dispose()
    {
        Service.PluginInterface.UiBuilder.Draw -= DrawSettingsWindow;
        Service.PluginInterface.UiBuilder.OpenConfigUi -= OpenSettingsWindow;
    }

    public void OpenSettingsWindow()
    {
        SettingsVisible = true;
    }
    
    public void ToggleSettingsWindow()
    {
        SettingsVisible = !SettingsVisible;
    }
    
    public void DrawSettingsWindow()
    {
        if (!SettingsVisible)
        {
            return;
        }

        _windowSizeHelper.SetWindowSize();

        if (ImGui.Begin("PartyIcons", ref _settingsVisible))
        {
            _windowSizeHelper.CheckWindowSize();

            if (ImGui.BeginTabBar("##tabbar"))
            {
                if (ImGui.BeginTabItem("General##general"))
                {
                    if (ImGui.BeginChild("##general_content"))
                    {
                        _generalSettings.DrawGeneralSettings();
                        
                        ImGui.EndChild();
                    }
                    
                    ImGui.EndTabItem();
                }
                
                if (ImGui.BeginTabItem("Nameplates"))
                {
                    if (ImGui.BeginChild("##nameplates_content"))
                    {
                        _nameplateSettings.DrawNameplateSettings();
                        
                        ImGui.EndChild();
                    }
                    
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Chat Names"))
                {
                    if (ImGui.BeginChild("##chat_names_content"))
                    {
                        DrawChatNameSettings();
                        
                        ImGui.EndChild();
                    }
                    
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Roles##static_assignments"))
                {
                    if (ImGui.BeginChild("##static_assignments_content"))
                    {
                        DrawStaticAssignmentsSettings();
                        
                        ImGui.EndChild();
                    }
                    
                    ImGui.EndTabItem();
                }
                
                ImGui.EndTabBar();
            }
        }

        ImGui.End();
    }

    private void DrawChatNameSettings()
    {
        const float separatorPadding = 2f;
        ImGui.Dummy(new Vector2(0, separatorPadding));
        
        ImGui.PushStyleColor(0, ImGuiHelpers.DefaultColorPalette()[0]);
        ImGui.Text("Overworld");
        ImGui.PopStyleColor();
        ImGui.Separator();
        ImGui.Dummy(new Vector2(0, separatorPadding));
        ImGui.Indent(15 * ImGuiHelpers.GlobalScale);
        {
            ChatModeSection("##chat_overworld",
                () => Plugin.Settings.ChatOverworld,
                (config) => Plugin.Settings.ChatOverworld = config,
                "Party:");

            ChatModeSection("##chat_others",
                () => Plugin.Settings.ChatOthers,
                (config) => Plugin.Settings.ChatOthers = config,
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
            ChatModeSection("##chat_dungeon",
                () => Plugin.Settings.ChatDungeon,
                (config) => Plugin.Settings.ChatDungeon = config,
                "Dungeon:");
            
            ChatModeSection("##chat_raid",
                () => Plugin.Settings.ChatRaid,
                (config) => Plugin.Settings.ChatRaid = config,
                "Raid:");
            
            ChatModeSection("##chat_alliance",
                () => Plugin.Settings.ChatAllianceRaid,
                (config) => Plugin.Settings.ChatAllianceRaid = config,
                "Alliance:");
        }
        ImGui.Indent(-15 * ImGuiHelpers.GlobalScale);
        ImGui.Dummy(new Vector2(0, 2f));
    }

    private void DrawStaticAssignmentsSettings()
    {
        ImGui.Dummy(new Vector2(0, 2f));
        
        var easternNamingConvention = Plugin.Settings.EasternNamingConvention;

        if (ImGui.Checkbox("##easteannaming", ref easternNamingConvention))
        {
            Plugin.Settings.EasternNamingConvention = easternNamingConvention;
            Plugin.Settings.Save();
        }

        ImGui.SameLine();
        ImGui.Text("Eastern role naming convention");
        ImGuiComponents.HelpMarker("Use Japanese data center role naming convention (MT ST D1-D4 H1-2).");

        var displayRoleInPartyList = Plugin.Settings.DisplayRoleInPartyList;

        if (ImGui.Checkbox("##displayrolesinpartylist", ref displayRoleInPartyList))
        {
            Plugin.Settings.DisplayRoleInPartyList = displayRoleInPartyList;
            Plugin.Settings.Save();
        }

        ImGui.SameLine();
        ImGui.Text("Replace party numbers with role in Party List");
        ImGuiHelpTooltip(
            "EXPERIMENTAL. Only works when nameplates set to 'Role letters' and Party List player character names are shown in full (not abbreviated).",
            true);
        
        var useContextMenu = Plugin.Settings.UseContextMenu;
        
        if (ImGui.Checkbox("##useContextMenu", ref useContextMenu))
        {
            Plugin.Settings.UseContextMenu = useContextMenu;
            Plugin.Settings.Save();
        }

        ImGui.SameLine();
        ImGui.Text("Add context menu commands to assign roles");
        ImGuiComponents.HelpMarker("Adds context menu commands to assign roles to players. When applicable, commands to swap role and use a suggested role are also added.");

        var assignFromChat = Plugin.Settings.AssignFromChat;

        if (ImGui.Checkbox("##assignFromChat", ref assignFromChat))
        {
            Plugin.Settings.AssignFromChat = assignFromChat;
            Plugin.Settings.Save();
        }

        ImGui.SameLine();
        ImGui.Text("Allow party members to self-assign roles via party chat");
        ImGuiComponents.HelpMarker("Allows party members to assign themselves a role, e.g. saying 'h1' in party chat will give that player the healer 1 role.");
        
        ImGui.Dummy(new Vector2(0, 2f));
        
        
        
        
        ImGui.PushStyleColor(0, ImGuiHelpers.DefaultColorPalette()[0]);
        ImGui.Text("Static Roles");
        ImGui.PopStyleColor();
        ImGui.Separator();
        ImGui.Dummy(new Vector2(0, 2f));
        // ImGui.Indent(15 * ImGuiHelpers.GlobalScale);
        {
            ImGui.PushStyleColor(0, ImGuiColors.ParsedGrey);
            {
                ImGui.TextWrapped(
                    "Name should include world name, separated by @. Keep in mind that if players job is not appropriate for the assigned role, the assignment will be ignored!");
                ImGui.Dummy(new Vector2(0f, 25f));
            }
            ImGui.PopStyleColor();
        }
        // ImGui.Indent(-15 * ImGuiHelpers.GlobalScale);
        // ImGui.Dummy(new Vector2(0, 2f));
        
        
        
        
        
        
        ImGui.SetCursorPosY(ImGui.GetCursorPos().Y - 22f);
        foreach (var kv in new Dictionary<string, RoleId>(Plugin.Settings.StaticAssignments))
        {
            if (ImGui.Button("x##remove_occupation_" + kv.Key))
            {
                Plugin.Settings.StaticAssignments.Remove(kv.Key);
                Plugin.Settings.Save();

                continue;
            }

            ImGui.SameLine();
            SetComboWidth(Enum.GetValues<RoleId>().Select(x => Plugin.PlayerStylesheet.GetRoleName(x)));

            if (ImGui.BeginCombo("##role_combo_" + kv.Key,
                    Plugin.PlayerStylesheet.GetRoleName(Plugin.Settings.StaticAssignments[kv.Key])))
            {
                foreach (var roleId in Enum.GetValues<RoleId>())
                {
                    if (ImGui.Selectable(Plugin.PlayerStylesheet.GetRoleName(roleId) + "##role_combo_option_" + kv.Key + "_" +
                                         roleId))
                    {
                        Plugin.Settings.StaticAssignments[kv.Key] = roleId;
                        Plugin.Settings.Save();
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.SameLine();
            ImGui.Text(kv.Key);
        }

        if (ImGui.Button("+##add_occupation"))
        {
            Plugin.Settings.StaticAssignments[_occupationNewName] = _occupationNewRole;
            Plugin.Settings.Save();
        }

        ImGui.SameLine();
        SetComboWidth(Enum.GetValues<RoleId>().Select(x => Plugin.PlayerStylesheet.GetRoleName(x)));

        if (ImGui.BeginCombo("##new_role_combo", Plugin.PlayerStylesheet.GetRoleName(_occupationNewRole)))
        {
            foreach (var roleId in Enum.GetValues<RoleId>())
            {
                if (ImGui.Selectable(Plugin.PlayerStylesheet.GetRoleName(roleId) + "##new_role_combo_option_" + "_" + roleId))
                {
                    _occupationNewRole = roleId;
                }
            }

            ImGui.EndCombo();
        }

        ImGui.SameLine();
        ImGui.InputText("##new_role_name", ref _occupationNewName, 64);
        
        ImGui.SetCursorPosY(ImGui.GetCursorPos().Y + 22f);

    }

    private void ChatModeSection(string label, Func<ChatConfig> getter, Action<ChatConfig> setter, string title = "Chat name: ")
    {
        ChatConfig NewConf = new ChatConfig(ChatMode.GameDefault, true);

        ImGui.Text(title);
        ImGui.SameLine(100f);
        SetComboWidth(Enum.GetValues<ChatMode>().Select(ChatModeToString));

        // hack to fix incorrect configurations
        try
        {
            NewConf = getter();
        }
        catch (ArgumentException ex)
        {
            setter(NewConf);
            Plugin.Settings.Save();
        }

        if (ImGui.BeginCombo(label, ChatModeToString(NewConf.Mode)))
        {
            foreach (var mode in Enum.GetValues<ChatMode>())
            {
                if (ImGui.Selectable(ChatModeToString(mode), mode == NewConf.Mode))
                {
                    NewConf.Mode = mode;;
                    setter(NewConf);
                    Plugin.Settings.Save();
                }
            }

            ImGui.EndCombo();
        }

        ImGui.SameLine();
        var colored = NewConf.UseRoleColor;

        if (ImGui.Checkbox($"Role Color{label}", ref colored))
        {
            NewConf.UseRoleColor = colored;
            setter(NewConf);
            Plugin.Settings.Save();
        }
    }

    private string ChatModeToString(ChatMode mode)
    {
        return mode switch
        {
            ChatMode.GameDefault => "Game Default",
            ChatMode.Role => "Role",
            ChatMode.Job => "Job abbreviation",
            _ => throw new ArgumentException()
        };
    }
    
    public static void SetComboWidth(IEnumerable<string> values)
    {
        const float paddingMultiplier = 1.05f; 
        float maxItemWidth = float.MinValue;

        foreach (var text in values)
        {
            var itemWidth = ImGui.CalcTextSize(text).X + ImGui.GetStyle().ScrollbarSize * 3f;
            maxItemWidth = Math.Max(maxItemWidth, itemWidth);
        }

        ImGui.SetNextItemWidth(maxItemWidth * paddingMultiplier);
    }
    
    public static void ImGuiHelpTooltip(string tooltip, bool experimental = false)
    {
        ImGui.SameLine();

        if (experimental)
        {
            ImGui.TextColored(new Vector4(0.8f, 0.0f, 0.0f, 1f), "!");
        }
        else
        {
            ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1f), "?");
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(tooltip);
        }
    }
    
    private bool _settingsVisible = false;
    private string _occupationNewName = "Character Name@World";
    private RoleId _occupationNewRole = RoleId.Undefined;
    private static WindowSizeHelper _windowSizeHelper = new();
    private readonly GeneralSettings _generalSettings = new();
    private readonly NameplateSettings _nameplateSettings = new();
}
