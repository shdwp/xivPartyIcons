using System;
using Dalamud.Hooking;
using Dalamud.Logging;
using PartyIcons.Api;
using PartyIcons.Entities;
using PartyIcons.View;
using XivCommon;

namespace PartyIcons.Runtime
{
    public sealed class NameplateUpdater : IDisposable
    {
        private NameplateView              _view;
        private PluginAddressResolver      _address;
        private Hook<SetNamePlateDelegate> _hook;
        private XivCommonBase              _base;
        private int                        _forceRedrawCount = 0;

        public NameplateUpdater(PluginAddressResolver address, NameplateView view, XivCommonBase @base)
        {
            _address = address;
            _view = view;
            _base = @base;
            _hook = new Hook<SetNamePlateDelegate>(_address.AddonNamePlate_SetNamePlatePtr, SetNamePlateDetour);
        }

        public void Enable()
        {
            _hook.Enable();
        }

        public void ForceRefresh()
        {
            PluginLog.Debug("Enabled force redrawing nameplates");
            _base.Functions.NamePlates.ForceRedraw = true;
            _forceRedrawCount = 0;
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

        public IntPtr SetNamePlateDetour(IntPtr namePlateObjectPtr, bool isPrefixTitle, bool displayTitle, IntPtr title, IntPtr name, IntPtr fcName, int iconID)
        {
            try
            {
                return SetNamePlate(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconID);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"SetNamePlateDetour encountered a critical error");
                return _hook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconID);
            }
        }

        public IntPtr SetNamePlate(IntPtr namePlateObjectPtr, bool isPrefixTitle, bool displayTitle, IntPtr title, IntPtr name, IntPtr fcName, int iconID)
        {
            if (_base.Functions.NamePlates.ForceRedraw)
            {
                _forceRedrawCount++;

                if (_forceRedrawCount > 200)
                {
                    _base.Functions.NamePlates.ForceRedraw = false;
                    PluginLog.Debug("Disabled force redraw");
                }
            }

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

            _view.NameplateDataForPC(npObject, ref isPrefixTitle, ref displayTitle, ref title, ref name, ref fcName, ref iconID);

            var result = _hook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconID);
            _view.SetupForPC(npObject);
            return result;
        }
    }
}