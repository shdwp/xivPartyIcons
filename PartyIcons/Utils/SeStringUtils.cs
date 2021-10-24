using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace PartyIcons.Utils
{
    public static class SeStringUtils
    {
        public static IntPtr EmptyPtr;

        public static void Initialize()
        {
            EmptyPtr = TextPtr("");
        }

        public static void Dispose()
        {
            Marshal.FreeHGlobal(EmptyPtr);
        }

        public static SeString SeStringFromPtr(IntPtr seStringPtr)
        {
            byte b;
            var offset = 0;
            unsafe
            {
                while ((b = *(byte*)(seStringPtr + offset)) != 0)
                    offset++;
            }
            var bytes = new byte[offset];
            Marshal.Copy(seStringPtr, bytes, 0, offset);
            return SeString.Parse(bytes);
        }

        public static IntPtr SeStringToPtr(SeString seString)
        {
            var bytes = seString.Encode();
            IntPtr pointer = Marshal.AllocHGlobal(bytes.Length + 1);
            Marshal.Copy(bytes, 0, pointer, bytes.Length);
            Marshal.WriteByte(pointer, bytes.Length, 0);
            return pointer;
        }

        public static IntPtr TextPtr(string rawText)
        {
            var seString = new SeString(new List<Payload>());
            seString.Append(new TextPayload(rawText));
            return SeStringToPtr(seString);
        }

        public static IntPtr TextPtr(string text, ushort color)
        {
            var seString = new SeString(new List<Payload>());
            seString.Append(new UIForegroundPayload(color));
            seString.Append(new TextPayload(text));
            seString.Append(UIForegroundPayload.UIForegroundOff);
            return SeStringToPtr(seString);
        }

        public static IntPtr IconPtr(BitmapFontIcon icon, string prefix=null)
        {
            var seString = new SeString(new List<Payload>());
            if (prefix != null)
            {
                seString.Append(new TextPayload(prefix));
            }

            seString.Append(new IconPayload(icon));
            return SeStringToPtr(seString);
        }
    }
}
