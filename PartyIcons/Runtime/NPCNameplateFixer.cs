using System;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;
using PartyIcons.Api;
using PartyIcons.View;

namespace PartyIcons.Runtime;

public sealed class NPCNameplateFixer : IDisposable
{
    private readonly CancellationTokenSource FixNonPlayerCharacterNamePlatesTokenSource = new();
    private readonly NameplateView _view;

    public NPCNameplateFixer(NameplateView view)
    {
        _view = view;
    }

    public void Enable()
    {
        Task.Run(() => TaskMain(FixNonPlayerCharacterNamePlatesTokenSource.Token));
    }

    public void Dispose()
    {
        PluginLog.Debug("NPCNameplateFixer dispose, cancelling the token");
        FixNonPlayerCharacterNamePlatesTokenSource.Cancel();
        RevertAll();
    }

    private void TaskMain(CancellationToken token)
    {
        try
        {
            PluginLog.Debug("NPCNameplateFixer thread started");

            while (!token.IsCancellationRequested)
            {
                RevertNPC();
                Task.Delay(16, token).Wait(token);
            }

            PluginLog.Debug("NPCNameplateFixer thread halted");
        }
        catch (OperationCanceledException)
        {
            PluginLog.Debug("NPCNameplateFixed thread stopped");
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Non-PC Updater loop has crashed");
        }
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

            if (actorID == 0xE0000000)
            {
                continue;
            }

            var isPC = npInfo.IsPlayerCharacter();

            if (!isPC)
            {
                _view.SetupDefault(npObject);
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

            if (actorID == 0xE0000000)
            {
                continue;
            }

            _view.SetupDefault(npObject);
        }
    }
}
