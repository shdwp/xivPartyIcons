using System;
using System.Linq;
using Dalamud.Game.ClientState;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.Logging;
using PartyIcons.Api;
using PartyIcons.Configuration;
using PartyIcons.Entities;
using PartyIcons.Utils;
using PartyIcons.View;

namespace PartyIcons.Runtime;

public sealed class NameplateUpdater : IDisposable
{
    private readonly Settings _configuration;
    private readonly NameplateView _view;
    private readonly PluginAddressResolver _address;
    private readonly ViewModeSetter _modeSetter;
    private readonly Hook<SetNamePlateDelegate> _hook;

    public int DebugIcon { get; set; } = -1;
    
    public NameplateUpdater(Settings configuration, PluginAddressResolver address, NameplateView view, ViewModeSetter modeSetter)
    {
        _configuration = configuration;
        _address = address;
        _view = view;
        _modeSetter = modeSetter;
        _hook = new Hook<SetNamePlateDelegate>(_address.AddonNamePlate_SetNamePlatePtr, SetNamePlateDetour);
    }

    public void Enable()
    {
        _hook.Enable();
    }
    
    public void Disable()
    {
        _hook.Disable();
    }

    public void Dispose()
    {
        Disable();
        _hook.Dispose();
    }

    public IntPtr SetNamePlateDetour(IntPtr namePlateObjectPtr, bool isPrefixTitle, bool displayTitle, IntPtr title,
        IntPtr name, IntPtr fcName, int iconID)
    {
        try
        {
            return SetNamePlate(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconID);
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "SetNamePlateDetour encountered a critical error");

            return _hook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconID);
        }
    }

    public IntPtr SetNamePlate(IntPtr namePlateObjectPtr, bool isPrefixTitle, bool displayTitle, IntPtr title,
        IntPtr name, IntPtr fcName, int iconID)
    {
        if (Service.ClientState.IsPvP)
        {
            // disable in PvP
            return _hook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconID);
        }

        var originalTitle = title;
        var originalName = name;
        var originalFcName = fcName;

        var npObject = new XivApi.SafeNamePlateObject(namePlateObjectPtr);

        if (npObject == null)
        {
            _view.SetupDefault(npObject);

            return _hook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconID);
        }

        var npInfo = npObject.NamePlateInfo;

        if (npInfo == null)
        {
            _view.SetupDefault(npObject);

            return _hook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconID);
        }

        var actorID = npInfo.Data.ObjectID.ObjectID;

        if (actorID == 0xE0000000)
        {
            _view.SetupDefault(npObject);

            return _hook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconID);
        }

        if (!npObject.IsPlayer)
        {
            _view.SetupDefault(npObject);

            return _hook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconID);
        }

        var jobID = npInfo.GetJobID();

        if (jobID < 1 || jobID >= Enum.GetValues(typeof(Job)).Length)
        {
            _view.SetupDefault(npObject);

            return _hook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconID);
        }

        var isPriorityIcon = IsPriorityIcon(iconID, out var priorityIconId);

        _view.NameplateDataForPC(npObject, ref isPrefixTitle, ref displayTitle, ref title, ref name, ref fcName, ref iconID);

        if (isPriorityIcon)
        {
            iconID = priorityIconId;
        }

        var result = _hook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconID);
        _view.SetupForPC(npObject, isPriorityIcon);

        if (originalName != name)
        {
            SeStringUtils.FreePtr(name);
        }

        if (originalTitle != title)
        {
            SeStringUtils.FreePtr(title);
        }

        if (originalFcName != fcName)
        {
            SeStringUtils.FreePtr(fcName);
        }

        return result;
    }

    /// <summary>
    /// Check for an icon that should take priority over the job icon,
    /// taking into account whether or not the player is in a duty.
    /// </summary>
    /// <param name="iconId">The incoming icon id that is being overwritten by the plugin.</param>
    /// <param name="priorityIconId">The icon id that should be used.</param>
    /// <returns>Whether a priority icon was found.</returns>
    private bool IsPriorityIcon(int iconId, out int priorityIconId)
    {
        PluginLog.Verbose($"Icon ID: {iconId}, Debug Icon ID: {DebugIcon}");
        priorityIconId = iconId;

        if (_configuration.UsePriorityIcons == false &&
            iconId != (int)Icon.Disconnecting && iconId != (int)Icon.Disconnecting + 50)
        {
            return false;
        }
        
        // Select which set of priority icons to use based on whether we're in a duty
        // In the future, there can be a third list used when in combat
        var priorityIcons = GetPriorityIcons();

        // Determine whether the incoming icon should take priority over the job icon
        // Check the id plus 50 as that's an alternately sized version
        bool isPriorityIcon = priorityIcons.Contains(iconId) || priorityIcons.Contains(iconId + 50);
        
        // Save the id of the icon
        priorityIconId = iconId;

        // If an icon was set with the plugin's debug command, always use that
        if (DebugIcon >= 0)
        {
            isPriorityIcon = true;
            priorityIconId = DebugIcon;
            PluginLog.Verbose($"Setting debug icon. Id: {DebugIcon}");
            
            DebugIcon++;
        }

        return isPriorityIcon;
    }

    private int[] GetPriorityIcons()
    {
        if (_modeSetter.ZoneType == ZoneType.Foray)
        {
            return priorityIconsInForay;
        }
        
        if (_modeSetter.InDuty)
        {
            return priorityIconsInDuty;
        }
        
        return priorityIconsOverworld;
    }

    public enum Icon
    {
        Disconnecting = 061503,
    }

    // This could be done as a range but
    private static readonly int[] priorityIconsOverworld =
    {
        061503, // Disconnecting
        061506, // In Duty
        061508, // Viewing Cutscene
        061509, // Busy
        061511, // Idle
        061514, // Looking for meld
        061515, // Looking for party
        061517, // Duty Finder
        061521, // Party Leader
        061522, // Party Member
        061524, // Game Master
        061532, // Game Master
        061533, // Event Participant
        061545, // Role Playing
        061546, // Group Pose
    };

    private static readonly int[] priorityIconsInDuty =
    {
        061503, // Disconnecting
        061508, // Viewing Cutscene
        061511, // Idle
        061546, // Group Pose
    };
    
    private static readonly int[] priorityIconsInForay =
    {
        // This allows you to see which players don't have a party
        061506, // In Duty
        
        061503, // Disconnecting
        061508, // Viewing Cutscene
        061511, // Idle
        061546, // Group Pose
    };
}
