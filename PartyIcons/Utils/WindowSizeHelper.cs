using System.Numerics;
using Dalamud.Interface.Utility;
using ImGuiNET;

namespace PartyIcons.Utils;

public class WindowSizeHelper
{
    /// <summary>
    /// Sets the next window's size and position.
    /// </summary>
    /// <remarks>
    /// This additionally checks for whether an unreasonably large or offscreen window was detected.
    /// If so, the window size and position are forced to be the default instead of what was stored
    /// in the ImGui ini file.
    /// </remarks>
    public void SetWindowSize()
    {
        var condition = ImGuiCond.FirstUseEver;
        
        if (_shouldForceSize)
        {
            condition = ImGuiCond.Always;
            
            _shouldForceSize = false;
        }
        
        SetDefaultWindowSizeAndPos(condition);
    }

    /// <summary>
    /// Check for an unreasonably large or offscreen window.
    /// </summary>
    public void CheckWindowSize()
    {
        if (_sizeAlreadyChecked)
        {
            return;
        }
        
        _sizeAlreadyChecked = true;
            
        Vector2 currentSize = ImGui.GetWindowSize();
        Vector2 currentPos = ImGui.GetWindowPos();
        
        var boundsToCheck = currentPos + currentSize;
        
        if (boundsToCheck.X > ImGuiHelpers.MainViewport.Size.X ||
            boundsToCheck.Y > ImGuiHelpers.MainViewport.Size.Y)
        {
            Service.Log.Debug("Unreasonable window size detected as a result of a previously fixed bug. Forcing a reasonable window size.");
            
            _shouldForceSize = true;
        }
    }
    
    /// <summary>
    /// Force the next window to use the default size and position.
    /// </summary>
    public void ForceSize()
    {
        if (_shouldForceSize)
        {
            SetDefaultWindowSizeAndPos(ImGuiCond.Always);
            
            _shouldForceSize = false;
        }
    }

    private static void SetDefaultWindowSizeAndPos(ImGuiCond condition)
    {
        Vector2 initialSize = ImGuiHelpers.MainViewport.Size / 2f;
        Vector2 minimumSize = initialSize / 2f;
        Vector2 initialPosition = ImGuiHelpers.MainViewport.Size / 5f;
        
        ImGui.SetNextWindowSize(initialSize, condition);
        ImGui.SetNextWindowSizeConstraints(minimumSize, new Vector2(float.MaxValue));
        
        ImGuiHelpers.SetNextWindowPosRelativeMainViewport(initialPosition, condition);
    }
    
    /// <summary>
    /// Whether constraints were already checked. 
    /// </summary>
    private bool _sizeAlreadyChecked;
    
    /// <summary>
    /// Whether constraints should be checked next time the window is rendered.
    /// </summary>
    private bool _shouldForceSize;
}