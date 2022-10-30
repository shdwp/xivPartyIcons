using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using ImGuiNET;
using PartyIcons.Configuration;
using PartyIcons.Entities;
using PartyIcons.UI.Controls;
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
                _generalTabText.IsFlashing = Plugin.Settings.TestingMode;
                
                if (_generalTabText.Draw(() => ImGui.BeginTabItem("General##general")))
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
                        _chatNameSettings.DrawChatNameSettings();
                        
                        ImGui.EndChild();
                    }
                    
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Roles##static_assignments"))
                {
                    if (ImGui.BeginChild("##static_assignments_content"))
                    {
                        _staticAssignmentsSettings.DrawStaticAssignmentsSettings();
                        
                        ImGui.EndChild();
                    }
                    
                    ImGui.EndTabItem();
                }
                
                ImGui.EndTabBar();
            }
        }

        ImGui.End();
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
    private static WindowSizeHelper _windowSizeHelper = new();
    private readonly GeneralSettings _generalSettings = new();
    private readonly NameplateSettings _nameplateSettings = new();
    private readonly ChatNameSettings _chatNameSettings = new();
    private readonly StaticAssignmentsSettings _staticAssignmentsSettings = new StaticAssignmentsSettings();
    
    private FlashingText _generalTabText = new();
}
