using System;
using Dalamud.Game.Libc;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.Logging;
using PartyIcons.Api;

namespace PartyIcons.Runtime
{
    public sealed class ChatNameUpdater
    {
        private PluginAddressResolver _address;

        private Hook<PrintMessage> _hook;

        [PluginService] private LibcFunction LibcFunction { get; set; }

        public ChatNameUpdater(PluginAddressResolver address)
        {
            _address = address;
            _hook = new Hook<PrintMessage>(_address.PrintChatMessagePtr, PrintMessageDetour);
        }

        public void Enable()
        {
            _hook.Enable();
        }

        public void Disable()
        {
            _hook.Disable();
        }

        private bool Parse(ref SeString sender)
        {
            return true;
        }

        private IntPtr PrintMessageDetour(IntPtr raptureLogModule, XivChatType xivChatType, IntPtr senderName, IntPtr message, uint senderId, byte param)
        {
            try
            {
                if (xivChatType == XivChatType.Party || xivChatType == XivChatType.Say)
                {
                    var stdSender = StdString.ReadFromPointer(senderName);
                    var parsedSender = SeString.Parse(stdSender.RawData);

                    if (Parse(ref parsedSender))
                    {
                        stdSender.RawData = parsedSender.Encode();
                        var allocatedString = LibcFunction.NewString(stdSender.RawData);
                        var retVal = _hook.Original(raptureLogModule, xivChatType, allocatedString.Address, message, senderId, param);
                        allocatedString.Dispose();
                        return retVal;
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error("Exception in detour: ", ex);
            }

            return _hook.Original(raptureLogModule, xivChatType, senderName, message, senderId, param);
        }
    }
}
