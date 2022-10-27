using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using Dalamud.Data;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Utility;
using ImGuiNET;
using ImGuiScene;
using PartyIcons.Entities;
using PartyIcons.Stylesheet;
using PartyIcons.Utils;
using PartyIcons.View;

namespace PartyIcons;

internal class PluginUI : IDisposable
{
    private readonly Configuration _configuration;
    private readonly DataManager _dataManager;
    private readonly PlayerStylesheet _stylesheet;

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

    public PluginUI()
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
    
    public void Dispose() { }

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

        var usePriorityIcons = _configuration.UsePriorityIcons;
        
        if (ImGui.Checkbox("##usePriorityIcons", ref usePriorityIcons))
        {
            _configuration.UsePriorityIcons = usePriorityIcons;
            _configuration.Save();
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
        
        var testingMode = _configuration.TestingMode;
        
        if (ImGui.Checkbox("##testingMode", ref testingMode))
        {
            _configuration.TestingMode = testingMode;
            _configuration.Save();
        }

        ImGui.SameLine();
        ImGui.Text("Enable testing mode");
        ImGuiComponents.HelpMarker("Applies settings to any player, contrary to only the ones that are in the party.");

        var chatContentMessage = _configuration.ChatContentMessage;

        if (ImGui.Checkbox("##chatmessage", ref chatContentMessage))
        {
            _configuration.ChatContentMessage = chatContentMessage;
            _configuration.Save();
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
        
        var iconSetId = _configuration.IconSetId;
        ImGui.Text("Icon set:");
        ImGui.SameLine();
        SetComboWidth(Enum.GetValues<IconSetId>().Select(IconSetIdToString));

        if (ImGui.BeginCombo("##icon_set", IconSetIdToString(iconSetId)))
        {
            foreach (var id in Enum.GetValues<IconSetId>())
            {
                if (ImGui.Selectable(IconSetIdToString(id) + "##icon_set_" + id))
                {
                    _configuration.IconSetId = id;
                    _configuration.Save();
                }
            }

            ImGui.EndCombo();
        }

        var iconSizeMode = _configuration.SizeMode;
        ImGui.Text("Nameplate size:");
        ImGui.SameLine();
        SetComboWidth(Enum.GetValues<NameplateSizeMode>().Select(x => x.ToString()));

        if (ImGui.BeginCombo("##icon_size", iconSizeMode.ToString()))
        {
            foreach (var mode in Enum.GetValues<NameplateSizeMode>())
            {
                if (ImGui.Selectable(mode + "##icon_set_" + mode))
                {
                    _configuration.SizeMode = mode;
                    _configuration.Save();
                }
            }

            ImGui.EndCombo();
        }

        ImGuiHelpTooltip("Affects all presets, except Game Default and Small Job Icon.");
        
        var hideLocalNameplate = _configuration.HideLocalPlayerNameplate;

        if (ImGui.Checkbox("##hidelocal", ref hideLocalNameplate))
        {
            _configuration.HideLocalPlayerNameplate = hideLocalNameplate;
            _configuration.Save();
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
            NameplateModeSection("##np_overworld", () => _configuration.NameplateOverworld,
                (mode) => _configuration.NameplateOverworld = mode,
                "Party:");
    
            NameplateModeSection("##np_others", () => _configuration.NameplateOthers,
                (mode) => _configuration.NameplateOthers = mode,
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
            NameplateModeSection("##np_dungeon", () => _configuration.NameplateDungeon,
                (mode) => _configuration.NameplateDungeon = mode,
                "Dungeon:");

            NameplateModeSection("##np_raid", () => _configuration.NameplateRaid,
                (mode) => _configuration.NameplateRaid = mode,
                "Raid:");

            NameplateModeSection("##np_alliance", () => _configuration.NameplateAllianceRaid,
                (mode) => _configuration.NameplateAllianceRaid = mode,
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
            
            NameplateModeSection("##np_bozja_party", () => _configuration.NameplateBozjaParty,
                mode => _configuration.NameplateBozjaParty = mode, "Party:");
            
            NameplateModeSection("##np_bozja_others", () => _configuration.NameplateBozjaOthers,
                mode => _configuration.NameplateBozjaOthers = mode, "Others:");
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
            ChatModeSection("##chat_overworld", () => _configuration.ChatOverworld,
                (mode) => _configuration.ChatOverworld = mode,
                "Party:");
            
            ChatModeSection("##chat_others", () => _configuration.ChatOthers,
                (mode) => _configuration.ChatOthers = mode,
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
            ChatModeSection("##chat_dungeon", () => _configuration.ChatDungeon,
                (mode) => _configuration.ChatDungeon = mode,
                "Dungeon:");

            ChatModeSection("##chat_raid", () => _configuration.ChatRaid, (mode) => _configuration.ChatRaid = mode,
                "Raid:");

            ChatModeSection("##chat_alliance", () => _configuration.ChatAllianceRaid,
                (mode) => _configuration.ChatAllianceRaid = mode,
                "Alliance:");
        }
        ImGui.Indent(-15 * ImGuiHelpers.GlobalScale);
        ImGui.Dummy(new Vector2(0, 2f));
    }

    private void DrawStaticAssignmentsSettings()
    {
        ImGui.Dummy(new Vector2(0, 2f));
        
        var easternNamingConvention = _configuration.EasternNamingConvention;

        if (ImGui.Checkbox("##easteannaming", ref easternNamingConvention))
        {
            _configuration.EasternNamingConvention = easternNamingConvention;
            _configuration.Save();
        }

        ImGui.SameLine();
        ImGui.Text("Eastern role naming convention");
        ImGuiComponents.HelpMarker("Use Japanese data center role naming convention (MT ST D1-D4 H1-2).");

        var displayRoleInPartyList = _configuration.DisplayRoleInPartyList;

        if (ImGui.Checkbox("##displayrolesinpartylist", ref displayRoleInPartyList))
        {
            _configuration.DisplayRoleInPartyList = displayRoleInPartyList;
            _configuration.Save();
        }

        ImGui.SameLine();
        ImGui.Text("Replace party numbers with role in Party List");
        ImGuiHelpTooltip(
            "EXPERIMENTAL. Only works when nameplates set to 'Role letters' and Party List player character names are shown in full (not abbreviated).",
            true);
        
        var useContextMenu = _configuration.UseContextMenu;
        
        if (ImGui.Checkbox("##useContextMenu", ref useContextMenu))
        {
            _configuration.UseContextMenu = useContextMenu;
            _configuration.Save();
        }

        ImGui.SameLine();
        ImGui.Text("Add context menu commands to assign roles");
        ImGuiComponents.HelpMarker("Adds context menu commands to assign roles to players. When applicable, commands to swap role and use a suggested role are also added.");

        var assignFromChat = _configuration.AssignFromChat;

        if (ImGui.Checkbox("##assignFromChat", ref assignFromChat))
        {
            _configuration.AssignFromChat = assignFromChat;
            _configuration.Save();
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
        foreach (var kv in new Dictionary<string, RoleId>(_configuration.StaticAssignments))
        {
            if (ImGui.Button("x##remove_occupation_" + kv.Key))
            {
                _configuration.StaticAssignments.Remove(kv.Key);
                _configuration.Save();

                continue;
            }

            ImGui.SameLine();
            SetComboWidth(Enum.GetValues<RoleId>().Select(x => _stylesheet.GetRoleName(x)));

            if (ImGui.BeginCombo("##role_combo_" + kv.Key,
                    _stylesheet.GetRoleName(_configuration.StaticAssignments[kv.Key])))
            {
                foreach (var roleId in Enum.GetValues<RoleId>())
                {
                    if (ImGui.Selectable(_stylesheet.GetRoleName(roleId) + "##role_combo_option_" + kv.Key + "_" +
                                         roleId))
                    {
                        _configuration.StaticAssignments[kv.Key] = roleId;
                        _configuration.Save();
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.SameLine();
            ImGui.Text(kv.Key);
        }

        if (ImGui.Button("+##add_occupation"))
        {
            _configuration.StaticAssignments[_occupationNewName] = _occupationNewRole;
            _configuration.Save();
        }

        ImGui.SameLine();
        SetComboWidth(Enum.GetValues<RoleId>().Select(x => _stylesheet.GetRoleName(x)));

        if (ImGui.BeginCombo("##new_role_combo", _stylesheet.GetRoleName(_occupationNewRole)))
        {
            foreach (var roleId in Enum.GetValues<RoleId>())
            {
                if (ImGui.Selectable(_stylesheet.GetRoleName(roleId) + "##new_role_combo_option_" + "_" + roleId))
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

    private void ChatModeSection(string label, Func<ChatMode> getter, Action<ChatMode> setter, string title = "Chat name: ")
    {
        ImGui.Text(title);
        ImGui.SameLine(100f);
        SetComboWidth(Enum.GetValues<ChatMode>().Select(ChatModeToString));

        // hack to fix incorrect configurations
        try
        {
            getter();
        }
        catch (ArgumentException ex)
        {
            setter(ChatMode.GameDefault);
            _configuration.Save();
        }

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
            ChatMode.GameDefault => "Game Default",
            ChatMode.Role => "Role",
            ChatMode.Job => "Job abbreviation",
            ChatMode.OnlyColor => "Color only",
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
            _configuration.Save();
        }

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
