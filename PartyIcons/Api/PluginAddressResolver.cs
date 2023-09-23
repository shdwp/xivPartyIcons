using System;
using System.Runtime.InteropServices;
using Dalamud.Game.Text;
using Dalamud.Utility.Signatures;

namespace PartyIcons.Api;

[UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
public delegate IntPtr SetNamePlateDelegate(IntPtr addon, bool isPrefixTitle, bool displayTitle, IntPtr title,
    IntPtr name, IntPtr fcName, IntPtr prefix, int iconID);

[UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
public delegate IntPtr AtkResNode_SetScaleDelegate(IntPtr node, float x, float y);

[UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
public delegate IntPtr AtkResNode_SetPositionShortDelegate(IntPtr node, short x, short y);

[UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
public delegate IntPtr Framework_GetUIModuleDelegate(IntPtr framework);

[UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
public delegate IntPtr UIModule_GetRaptureAtkModuleDelegate(IntPtr uiModule);

[UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
public delegate byte GroupManager_IsObjectIDInPartyDelegate(IntPtr groupManager, uint actorId);

[UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
public delegate byte GroupManager_IsObjectIDInAllianceDelegate(IntPtr groupManager, uint actorId);

[UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
public delegate IntPtr BattleCharaStore_LookupBattleCharaByObjectIDDelegate(IntPtr battleCharaStore, uint actorId);

[UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
public delegate IntPtr PrintMessage(IntPtr chatManager, XivChatType xivChatType, IntPtr senderName, IntPtr message,
    uint senderId, byte param);

public sealed class PluginAddressResolver
{
    [Signature("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B 5C 24 ?? 45 38 BE")]
    public IntPtr AddonNamePlate_SetNamePlatePtr;

    [Signature("8B 81 ?? ?? ?? ?? A8 01 75 ?? F3 0F 10 41 ?? 0F 2E C1 7A ?? 75 ?? F3 0F 10 41 ?? 0F 2E C2 7A ?? 74 ?? 83 C8 01 89 81 ?? ?? ?? ?? F3 0F 10 05 ?? ?? ?? ??")]
    public IntPtr AtkResNode_SetScalePtr;

    [Signature("48 85 C9 74 4A 41 0F BF C0 66 0F 6E C8 0F BF C2 0F 5B C9 66 0F 6E D0")]
    public IntPtr AtkResNode_SetPositionShortPtr;

    [Signature("E8 ?? ?? ?? ?? 80 7B 1D 01")]
    public IntPtr Framework_GetUIModulePtr;

    [Signature("48 8D 0D ?? ?? ?? ?? 44 8B E7")]
    public IntPtr GroupManagerPtr;

    [Signature("E8 ?? ?? ?? ?? EB B8 E8")]
    public IntPtr GroupManager_IsObjectIDInPartyPtr;

    [Signature("33 C0 44 8B CA F6 81 ?? ?? ?? ?? ??")]
    public IntPtr GroupManager_IsObjectIDInAlliancePtr;

    [Signature("8B D0 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B D8 48 85 C0 74 3A")]
    public IntPtr BattleCharaStorePtr;

    [Signature("E8 ?? ?? ?? ?? 48 8B D8 48 85 C0 74 3A 48 8B C8 E8 ?? ?? ?? ?? 84 C0")]
    public IntPtr BattleCharaStore_LookupBattleCharaByObjectIDPtr;

    [Signature("E8 ?? ?? ?? ?? 4C 8B BC 24 ?? ?? ?? ?? 4D 85 F6")]
    public IntPtr PrintChatMessagePtr;

    public PluginAddressResolver()
    {
        Service.GameInteropProvider.InitializeFromAttributes(this);
    }
}
