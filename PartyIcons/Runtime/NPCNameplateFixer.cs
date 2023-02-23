using System;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game;
using Dalamud.Logging;
using PartyIcons.Api;
using PartyIcons.View;

namespace PartyIcons.Runtime;

/// <summary>
/// Reverts NPC nameplates that have had their icon or name text scaled and
/// also reverts all nameplates when the plugin is unloading.
/// </summary>
public sealed class NPCNameplateFixer : IDisposable
{
    private const uint NoTarget = 0xE0000000;
    private readonly NameplateView _view;

    public NPCNameplateFixer(NameplateView view)
    {
        _view = view;
    }

    public void Enable()
    {
        Service.Framework.Update += OnUpdate;
    }

    public void Dispose()
    {
        Service.Framework.Update -= OnUpdate;
        RevertAll();
    }

    private void OnUpdate(Framework framework)
    {
        RevertNPC();
    }

    private void RevertNPC()
    {
        var addon = XivApi.GetSafeAddonNamePlate();

        for (var i = 0; i < 50; i++)
        {
            var npObject = addon.GetNamePlateObject(i);

            if (npObject == null || !npObject.IsVisible)
            {
                continue;
            }

            var npInfo = npObject.NamePlateInfo;

            if (npInfo == null)
            {
                continue;
            }

            var actorID = npInfo.Data.ObjectID.ObjectID;

            if (actorID == NoTarget)
            {
                continue;
            }

            var isPC = npInfo.IsPlayerCharacter();

            if (!isPC && _view.SetupDefault(npObject))
            {
                PluginLog.Verbose($"Reverted NPC {actorID} (#{i})");
            }
        }
    }

    private void RevertAll()
    {
        var addon = XivApi.GetSafeAddonNamePlate();

        for (var i = 0; i < 50; i++)
        {
            var npObject = addon.GetNamePlateObject(i);

            if (npObject == null)
            {
                continue;
            }

            var npInfo = npObject.NamePlateInfo;

            if (npInfo == null)
            {
                continue;
            }

            var actorID = npInfo.Data.ObjectID.ObjectID;

            if (actorID == NoTarget)
            {
                continue;
            }

            if (_view.SetupDefault(npObject))
            {
                PluginLog.Verbose($"Reverted {actorID} (#{i})");
            }
        }
    }
}
