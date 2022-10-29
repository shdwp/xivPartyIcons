using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;
using PartyIcons.Configuration;

namespace PartyIcons.UI;

public sealed class ChatNameSettings
{
    public void DrawChatNameSettings()
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
    
    private static void ChatModeSection(string label, Func<ChatConfig> getter, Action<ChatConfig> setter, string title = "Chat name: ")
    {
        ChatConfig NewConf = new ChatConfig(ChatMode.GameDefault, true);

        ImGui.Text(title);
        ImGui.SameLine(100f);
        SettingsWindow.SetComboWidth(Enum.GetValues<ChatMode>().Select(ChatModeToString));

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
    
    private static string ChatModeToString(ChatMode mode)
    {
        return mode switch
        {
            ChatMode.GameDefault => "Game Default",
            ChatMode.Role => "Role",
            ChatMode.Job => "Job abbreviation",
            _ => throw new ArgumentException()
        };
    }
}