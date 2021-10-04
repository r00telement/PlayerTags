using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace PlayerTags
{
    public class PluginHooks : IDisposable
    {
        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        private delegate IntPtr SetNameplateDelegate_Private(IntPtr nameplateObjectPtr, bool isTitleAboveName, bool isTitleVisible, IntPtr titlePtr, IntPtr namePtr, IntPtr freeCompanyPtr, int iconId);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        private delegate IntPtr UIModule_GetRaptureAtkModuleDelegate_Private(IntPtr uiModule);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        private delegate IntPtr Framework_GetUIModuleDelegate_Private(IntPtr framework);

        private class PluginAddressResolver : BaseAddressResolver
        {
            private const string SetNameplateSignature = "48 89 5C 24 ?? 48 89 6C 24 ?? 56 57 41 54 41 56 41 57 48 83 EC 40 44 0F B6 E2";
            internal IntPtr SetNameplatePtr;

            private const string Framework_GetUIModuleSignature = "E8 ?? ?? ?? ?? 48 8B C8 48 8B 10 FF 92 ?? ?? ?? ?? 48 8B C8 BA ?? ?? ?? ??";
            internal IntPtr Framework_GetUIModulePtr;

            protected override void Setup64Bit(SigScanner scanner)
            {
                SetNameplatePtr = scanner.ScanText(SetNameplateSignature);
                Framework_GetUIModulePtr = scanner.ScanText(Framework_GetUIModuleSignature);
            }
        }

        private Framework m_Framework;
        private ObjectTable m_ObjectTable;
        private GameGui m_GameGui;
        private SetNameplateDelegate m_SetNameplate;

        private PluginAddressResolver m_PluginAddressResolver;
        private Hook<SetNameplateDelegate_Private> m_SetNameplateHook;
        private readonly Framework_GetUIModuleDelegate_Private m_GetUIModule;
        private IntPtr? m_NameplateObjectArrayPtr;
        private IntPtr? m_NameplateInfoArrayPtr;

        public PluginHooks(Framework framework, ObjectTable objectTable, GameGui gameGui, SetNameplateDelegate setNameplate)
        {
            m_Framework = framework;
            m_ObjectTable = objectTable;
            m_GameGui = gameGui;
            m_SetNameplate = setNameplate;

            m_PluginAddressResolver = new PluginAddressResolver();
            m_PluginAddressResolver.Setup();

            m_GetUIModule = Marshal.GetDelegateForFunctionPointer<Framework_GetUIModuleDelegate_Private>(m_PluginAddressResolver.Framework_GetUIModulePtr);

            m_SetNameplateHook = new Hook<SetNameplateDelegate_Private>(m_PluginAddressResolver.SetNameplatePtr, new SetNameplateDelegate_Private(SetNameplateDetour));
            m_SetNameplateHook.Enable();
        }

        public void Dispose()
        {
            m_SetNameplateHook.Disable();
        }

        private IntPtr SetNameplateDetour(IntPtr nameplateObjectPtrOriginal, bool isTitleAboveNameOriginal, bool isTitleVisibleOriginal, IntPtr titlePtrOriginal, IntPtr namePtrOriginal, IntPtr freeCompanyPtrOriginal, int iconIdOriginal)
        {
            if (m_SetNameplate != null)
            {
                try
                {
                    GameObject? gameObject = GetNameplateObject<GameObject>(nameplateObjectPtrOriginal);
                    if (gameObject != null)
                    {
                        SeString title = ReadSeString(titlePtrOriginal);
                        SeString name = ReadSeString(namePtrOriginal);
                        SeString freeCompany = ReadSeString(freeCompanyPtrOriginal);
                        bool isTitleVisible = isTitleVisibleOriginal;
                        bool isTitleAboveName = isTitleAboveNameOriginal;
                        int iconId = iconIdOriginal;
                        bool isTitleChanged;
                        bool isNameChanged;
                        bool isFreeCompanyChanged;
                        m_SetNameplate(gameObject, name, title, freeCompany, ref isTitleVisible, ref isTitleAboveName, ref iconId, out isNameChanged, out isTitleChanged, out isFreeCompanyChanged);

                        IntPtr namePtr = namePtrOriginal;
                        if (isNameChanged)
                        {
                            namePtr = Allocate(name);
                        }

                        IntPtr titlePtr = titlePtrOriginal;
                        if (isTitleChanged)
                        {
                            titlePtr = Allocate(title);
                        }

                        IntPtr freeCompanyPtr = freeCompanyPtrOriginal;
                        if (isFreeCompanyChanged)
                        {
                            freeCompanyPtr = Allocate(freeCompany);
                        }

                        var result = m_SetNameplateHook.Original(nameplateObjectPtrOriginal, isTitleAboveName, isTitleVisible, titlePtr, namePtr, freeCompanyPtr, iconId);

                        if (isNameChanged)
                        {
                            Release(ref namePtr);
                        }

                        if (isTitleChanged)
                        {
                            Release(ref titlePtr);
                        }

                        if (isFreeCompanyChanged)
                        {
                            Release(ref freeCompanyPtr);
                        }

                        return result;
                    }
                }
                catch (Exception ex)
                {
                    PluginLog.Error(ex, $"SetNameplateDetour encountered a critical error");
                }
            }

            return m_SetNameplateHook.Original(nameplateObjectPtrOriginal, isTitleAboveNameOriginal, isTitleVisibleOriginal, titlePtrOriginal, namePtrOriginal, freeCompanyPtrOriginal, iconIdOriginal);
        }

        private static SeString ReadSeString(IntPtr stringPtr)
        {
            return SeString.Parse(ReadStringBytes(stringPtr));
        }

        private static byte[] ReadStringBytes(IntPtr stringPtr)
        {
            if (stringPtr == IntPtr.Zero)
            {
                return null!;
            }

            var size = 0;
            while (Marshal.ReadByte(stringPtr, size) != 0)
            {
                size++;
            }

            var bytes = new byte[size];
            Marshal.Copy(stringPtr, bytes, 0, size);
            return bytes;
        }

        private static IntPtr Allocate(SeString seString)
        {
            var bytes = seString.Encode();
            IntPtr pointer = Marshal.AllocHGlobal(bytes.Length + 1);
            Marshal.Copy(bytes, 0, pointer, bytes.Length);
            Marshal.WriteByte(pointer, bytes.Length, 0);
            return pointer;
        }

        private static void Release(ref IntPtr ptr)
        {
            Marshal.FreeHGlobal(ptr);
            ptr = IntPtr.Zero;
        }

        private T? GetNameplateObject<T>(IntPtr nameplateObjectPtr)
            where T : GameObject
        {
            if (!m_NameplateInfoArrayPtr.HasValue)
            {
                // Get the nameplate object array
                var namePlateAddonPtr = m_GameGui.GetAddonByName("NamePlate", 1);
                var namePlateObjectArrayPtrPtr = namePlateAddonPtr + Marshal.OffsetOf(typeof(AddonNamePlate), nameof(AddonNamePlate.NamePlateObjectArray)).ToInt32();
                var nameplateObjectArrayPtr = Marshal.ReadIntPtr(namePlateObjectArrayPtrPtr);
                if (nameplateObjectArrayPtr == IntPtr.Zero)
                {
                    return null!;
                }

                m_NameplateObjectArrayPtr = nameplateObjectArrayPtr;

                // Get the nameplate info
                IntPtr raptureAtkModulePtr;
                var frameworkPtr = m_Framework.Address.BaseAddress;
                var uiModulePtr = m_GetUIModule(frameworkPtr);
                unsafe
                {
                    var uiModule = *(UIModule*)uiModulePtr;
                    var UIModule_GetRaptureAtkModuleAddress = new IntPtr(uiModule.vfunc[7]);
                    var GetRaptureAtkModule = Marshal.GetDelegateForFunctionPointer<UIModule_GetRaptureAtkModuleDelegate_Private>(UIModule_GetRaptureAtkModuleAddress);
                    raptureAtkModulePtr = GetRaptureAtkModule(uiModulePtr);
                }

                if (raptureAtkModulePtr == IntPtr.Zero)
                {
                    return null!;
                }

                m_NameplateInfoArrayPtr = raptureAtkModulePtr + Marshal.OffsetOf(typeof(RaptureAtkModule), nameof(RaptureAtkModule.NamePlateInfoArray)).ToInt32();
            }

            // Determine the index of this nameplate
            var namePlateObjectSize = Marshal.SizeOf(typeof(AddonNamePlate.NamePlateObject));
            var namePlateObjectPtr0 = m_NameplateObjectArrayPtr!.Value + namePlateObjectSize * 0;
            var namePlateIndex = (nameplateObjectPtr.ToInt64() - namePlateObjectPtr0.ToInt64()) / namePlateObjectSize;
            if (namePlateIndex < 0 || namePlateIndex >= 50)
            {
                return null!;
            }

            var namePlateInfoPtr = new IntPtr(m_NameplateInfoArrayPtr.Value.ToInt64() + Marshal.SizeOf(typeof(RaptureAtkModule.NamePlateInfo)) * namePlateIndex);
            RaptureAtkModule.NamePlateInfo namePlateInfo = Marshal.PtrToStructure<RaptureAtkModule.NamePlateInfo>(namePlateInfoPtr);

            // Get the player character for this nameplate info
            var objectId = namePlateInfo.ObjectID.ObjectID;

            T? gameObject = m_ObjectTable.FirstOrDefault(obj => obj.ObjectId == objectId) as T;
            if (gameObject == null)
            {
                return null!;
            }

            return gameObject!;
        }
    }
}
