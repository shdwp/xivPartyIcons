using System;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Game.Text;

namespace PartyIcons.Api
{
    [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
    public delegate IntPtr SetNamePlateDelegate(IntPtr addon, bool isPrefixTitle, bool displayTitle, IntPtr title, IntPtr name, IntPtr fcName, int iconID);

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
    public delegate IntPtr PrintMessage(IntPtr chatManager, XivChatType xivChatType, IntPtr senderName, IntPtr message, uint senderId, byte param);

    public sealed class PluginAddressResolver : BaseAddressResolver
    {
        private const string AddonNamePlate_SetNamePlateSignature = "48 89 5C 24 ?? 48 89 6C 24 ?? 56 57 41 54 41 56 41 57 48 83 EC 40 44 0F B6 E2";
        public        IntPtr AddonNamePlate_SetNamePlatePtr;

        private const string AtkResNode_SetScaleSignature = "E8 ?? ?? ?? ?? 48 8D 7F 38";
        public        IntPtr AtkResNode_SetScalePtr;

        private const string AtkResNode_SetPositionShortSignature = "E8 ?? ?? ?? ?? 8D 56 B5";
        public        IntPtr AtkResNode_SetPositionShortPtr;

        private const string Framework_GetUIModuleSignature = "E8 ?? ?? ?? ?? 80 7B 1D 01";
        public        IntPtr Framework_GetUIModulePtr;

        private const string GroupManagerSignature = "33 D2 48 8D 0D ?? ?? ?? ?? 33 DB";
        public        IntPtr GroupManagerPtr;

        private const string GroupManager_IsObjectIDInPartySignature = "E8 ?? ?? ?? ?? EB B8 E8";
        public        IntPtr GroupManager_IsObjectIDInPartyPtr;

        private const string GroupManager_IsObjectIDInAllianceSignature = "33 C0 44 8B CA F6 81";
        public        IntPtr GroupManager_IsObjectIDInAlliancePtr;

        private const string BattleCharaStoreSignature = "8B D0 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B D8 48 85 C0 74 3A";
        public        IntPtr BattleCharaStorePtr;

        private const string BattleCharaStore_LookupBattleCharaByObjectIDSignature = "E8 ?? ?? ?? ?? 48 89 84 1D";
        public        IntPtr BattleCharaStore_LookupBattleCharaByObjectIDPtr;

        protected override void Setup64Bit(SigScanner scanner)
        {
            AddonNamePlate_SetNamePlatePtr = scanner.ScanText(AddonNamePlate_SetNamePlateSignature);
            AtkResNode_SetScalePtr = scanner.ScanText(AtkResNode_SetScaleSignature);
            AtkResNode_SetPositionShortPtr = scanner.ScanText(AtkResNode_SetPositionShortSignature);
            Framework_GetUIModulePtr = scanner.ScanText(Framework_GetUIModuleSignature);
            GroupManagerPtr = scanner.GetStaticAddressFromSig(GroupManagerSignature);
            GroupManager_IsObjectIDInPartyPtr = scanner.ScanText(GroupManager_IsObjectIDInPartySignature);
            GroupManager_IsObjectIDInAlliancePtr = scanner.ScanText(GroupManager_IsObjectIDInAllianceSignature);
            BattleCharaStorePtr = scanner.GetStaticAddressFromSig(BattleCharaStoreSignature);
            BattleCharaStore_LookupBattleCharaByObjectIDPtr = scanner.ScanText(BattleCharaStore_LookupBattleCharaByObjectIDSignature);
        }
    }
}
