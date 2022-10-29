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
    private bool _settingsVisible = false;
    private string _occupationNewName = "Character Name@World";
    private RoleId _occupationNewRole = RoleId.Undefined;

    private HttpClient _httpClient;
    private string? _noticeString;
    private string? _noticeUrl;

    private static WindowSizeHelper _windowSizeHelper = new();

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

    private Dictionary<NameplateMode, TextureWrap> _nameplateExamples;

    public SettingsWindow()
    {
        _httpClient = new HttpClient();
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

            _nameplateExamples[kv.Key] = Service.PluginInterface.UiBuilder.LoadImage(memoryStream.ToArray());
        }

        DownloadAndParseNotice();
        
        Service.PluginInterface.UiBuilder.Draw += DrawSettingsWindow;
        Service.PluginInterface.UiBuilder.OpenConfigUi += OpenSettingsWindow;
    }

    private void DownloadAndParseNotice()
    {
        try
        {
            var stringAsync = _httpClient.GetStringAsync("https://shdwp.github.io/ukraine/xiv_notice.txt");
            stringAsync.Wait();
            var strArray = stringAsync.Result.Split('|');

            if ((uint) strArray.Length > 0U)
            {
                _noticeString = Regex.Replace(strArray[0], "\n", "\n\n");
            }

            if (strArray.Length <= 1)
            {
                return;
            }

            _noticeUrl = strArray[1];

            if (!(_noticeUrl.StartsWith("http://") || _noticeUrl.StartsWith("https://")))
            {
                PluginLog.Warning($"Received invalid noticeUrl {_noticeUrl}, ignoring");
                _noticeUrl = null;
            }
        }
        catch (Exception ex) { }
    }

    private void DisplayNotice()
    {
        if (_noticeString == null)
        {
            return;
        }

        ImGui.Dummy(new Vector2(0.0f, 15f));
        ImGui.PushStyleColor((ImGuiCol) 0, ImGuiColors.DPSRed);
        ImGuiHelpers.SafeTextWrapped(_noticeString);

        if (_noticeUrl != null)
        {
            if (ImGui.Button(_noticeUrl))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _noticeUrl,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex) { }
            }
        }

        ImGui.PopStyleColor();
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
                        DrawGeneralSettings();
                        
                        ImGui.EndChild();
                    }
                    
                    ImGui.EndTabItem();
                }
                
                if (ImGui.BeginTabItem("Nameplates"))
                {
                    if (ImGui.BeginChild("##nameplates_content"))
                    {
                        DrawNameplateSettings();
                        
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

    private void DrawGeneralSettings()
    {
        ImGui.Dummy(new Vector2(0, 2f));

        var usePriorityIcons = Plugin.Settings.UsePriorityIcons;
        
        if (ImGui.Checkbox("##usePriorityIcons", ref usePriorityIcons))
        {
            Plugin.Settings.UsePriorityIcons = usePriorityIcons;
            Plugin.Settings.Save();
        }

        ImGui.SameLine();
        ImGui.Text("Prioritize status icons");
        ImGuiComponents.HelpMarker("Prioritizes certain status icons over job icons.\n\nInside of a duty, the only status icons that take priority are Disconnecting, Viewing Cutscene, Idle, and Group Pose.\n\nEven if this is unchecked, the Disconnecting icon will always take priority.");

        /*
        // Sample code for later when we want to incorporate icon previews into the UI.
        var iconTex = _dataManager.GetIcon(61508);
        //if (iconTex == null) return;

        if (iconTex != null)
        {
            var tex = Interface.UiBuilder.LoadImageRaw(iconTex.GetRgbaImageData(), iconTex.Header.Width, iconTex.Header.Height, 4);
        }
        
        // ...
        
        ImGui.Image(tex.ImGuiHandle, new Vector2(tex.Width, tex.Height));
        */
        
        var testingMode = Plugin.Settings.TestingMode;
        
        if (ImGui.Checkbox("##testingMode", ref testingMode))
        {
            Plugin.Settings.TestingMode = testingMode;
            Plugin.Settings.Save();
        }

        ImGui.SameLine();
        ImGui.Text("Enable testing mode");
        ImGuiComponents.HelpMarker("Applies settings to any player, contrary to only the ones that are in the party.");

        var chatContentMessage = Plugin.Settings.ChatContentMessage;

        if (ImGui.Checkbox("##chatmessage", ref chatContentMessage))
        {
            Plugin.Settings.ChatContentMessage = chatContentMessage;
            Plugin.Settings.Save();
        }

        ImGui.SameLine();
        ImGui.Text("Display chat message when entering duty");
        ImGuiComponents.HelpMarker("Can be used to determine the duty type before fully loading in.");

        DisplayNotice();
    }
    
    private void DrawNameplateSettings()
    {
        const float separatorPadding = 2f;
        
        ImGui.Dummy(new Vector2(0, 1f));
        ImGui.TextDisabled("Please note, it usually takes time for nameplates to reload.");
        ImGui.Dummy(new Vector2(0, 10f));
        
        var iconSetId = Plugin.Settings.IconSetId;
        ImGui.Text("Icon set:");
        ImGui.SameLine();
        SetComboWidth(Enum.GetValues<IconSetId>().Select(IconSetIdToString));

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
        SetComboWidth(Enum.GetValues<NameplateSizeMode>().Select(x => x.ToString()));

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

        ImGuiHelpTooltip("Affects all presets, except Game Default and Small Job Icon.");
        
        var hideLocalNameplate = Plugin.Settings.HideLocalPlayerNameplate;

        if (ImGui.Checkbox("##hidelocal", ref hideLocalNameplate))
        {
            Plugin.Settings.HideLocalPlayerNameplate = hideLocalNameplate;
            Plugin.Settings.Save();
        }

        ImGui.SameLine();
        ImGui.Text("Hide own nameplate");
        ImGuiHelpTooltip(
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

    private void CollapsibleExampleImage(NameplateMode mode, TextureWrap tex)
    {
        if (ImGui.CollapsingHeader(NameplateModeToString(mode)))
        {
            ImGui.Image(tex.ImGuiHandle, new Vector2(tex.Width, tex.Height));
        }
    }

    private void ImGuiHelpTooltip(string tooltip, bool experimental = false)
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

    private void NameplateModeSection(string label, Func<NameplateMode> getter, Action<NameplateMode> setter,
        string title = "Nameplate: ")
    {
        ImGui.SetCursorPosY(ImGui.GetCursorPos().Y + 3f);
        ImGui.Text(title);
        ImGui.SameLine(100f);
            ImGui.SetCursorPosY(ImGui.GetCursorPos().Y - 3f);
        SetComboWidth(Enum.GetValues<NameplateMode>().Select(x => x.ToString()));

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

    private string IconSetIdToString(IconSetId id)
    {
        return id switch
        {
            IconSetId.Framed => "Framed, role colored",
            IconSetId.GlowingColored => "Glowing, role colored",
            IconSetId.GlowingGold => "Glowing, gold"
        };
    }

    private string NameplateModeToString(NameplateMode mode)
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
    
    private void SetComboWidth(IEnumerable<string> values)
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
}
