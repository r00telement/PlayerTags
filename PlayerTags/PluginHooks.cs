using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
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
        private class PluginAddressResolver : BaseAddressResolver
        {
            private const string SetNameplateSignature = "48 89 5C 24 ?? 48 89 6C 24 ?? 56 57 41 54 41 56 41 57 48 83 EC 40 44 0F B6 E2";
            internal IntPtr SetNameplatePtr;

            protected override void Setup64Bit(SigScanner scanner)
            {
                SetNameplatePtr = scanner.ScanText(SetNameplateSignature);
            }
        }

        private delegate IntPtr SetPlayerNameplateDelegate_Unmanaged(IntPtr playerNameplateObjectPtr, bool isTitleAboveName, bool isTitleVisible, IntPtr titlePtr, IntPtr namePtr, IntPtr freeCompanyPtr, int iconId);

        private Framework m_Framework;
        private ObjectTable m_ObjectTable;
        private GameGui m_GameGui;
        private SetPlayerNameplateDelegate m_SetPlayerNameplate;

        private PluginAddressResolver m_PluginAddressResolver;
        private Hook<SetPlayerNameplateDelegate_Unmanaged> m_SetPlayerNameplateHook;

        public PluginHooks(Framework framework, ObjectTable objectTable, GameGui gameGui, SetPlayerNameplateDelegate setPlayerNameplate)
        {
            m_Framework = framework;
            m_ObjectTable = objectTable;
            m_GameGui = gameGui;
            m_SetPlayerNameplate = setPlayerNameplate;

            m_PluginAddressResolver = new PluginAddressResolver();
            m_PluginAddressResolver.Setup();

            m_SetPlayerNameplateHook = new Hook<SetPlayerNameplateDelegate_Unmanaged>(m_PluginAddressResolver.SetNameplatePtr, new SetPlayerNameplateDelegate_Unmanaged(SetPlayerNameplateDetour));
            m_SetPlayerNameplateHook.Enable();
        }

        public void Dispose()
        {
            m_SetPlayerNameplateHook.Disable();
        }

        private IntPtr SetPlayerNameplateDetour(IntPtr playerNameplateObjectPtrOriginal, bool isTitleAboveNameOriginal, bool isTitleVisibleOriginal, IntPtr titlePtrOriginal, IntPtr namePtrOriginal, IntPtr freeCompanyPtrOriginal, int iconIdOriginal)
        {
            if (m_SetPlayerNameplate != null)
            {
                try
                {
                    PlayerCharacter? playerCharacter = GetNameplateGameObject<PlayerCharacter>(playerNameplateObjectPtrOriginal);
                    if (playerCharacter != null)
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
                        m_SetPlayerNameplate(playerCharacter, name, title, freeCompany, ref isTitleVisible, ref isTitleAboveName, ref iconId, out isNameChanged, out isTitleChanged, out isFreeCompanyChanged);

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

                        var result = m_SetPlayerNameplateHook.Original(playerNameplateObjectPtrOriginal, isTitleAboveName, isTitleVisible, titlePtr, namePtr, freeCompanyPtr, iconId);

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

            return m_SetPlayerNameplateHook.Original(playerNameplateObjectPtrOriginal, isTitleAboveNameOriginal, isTitleVisibleOriginal, titlePtrOriginal, namePtrOriginal, freeCompanyPtrOriginal, iconIdOriginal);
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

        private T? GetNameplateGameObject<T>(IntPtr nameplateObjectPtr)
            where T : GameObject
        {
            // Get the nameplate object array
            var nameplateAddonPtr = m_GameGui.GetAddonByName("NamePlate", 1);
            var nameplateObjectArrayPtrPtr = nameplateAddonPtr + Marshal.OffsetOf(typeof(AddonNamePlate), nameof(AddonNamePlate.NamePlateObjectArray)).ToInt32();
            var nameplateObjectArrayPtr = Marshal.ReadIntPtr(nameplateObjectArrayPtrPtr);
            if (nameplateObjectArrayPtr == IntPtr.Zero)
            {
                return null;
            }

            // Determine the index of the nameplate object within the nameplate object array
            var namePlateObjectSize = Marshal.SizeOf(typeof(AddonNamePlate.NamePlateObject));
            var namePlateObjectPtr0 = nameplateObjectArrayPtr + namePlateObjectSize * 0;
            var namePlateIndex = (nameplateObjectPtr.ToInt64() - namePlateObjectPtr0.ToInt64()) / namePlateObjectSize;
            if (namePlateIndex < 0 || namePlateIndex >= 50)
            {
                return null;
            }

            // Get the nameplate info array
            IntPtr nameplateInfoArrayPtr = IntPtr.Zero;
            unsafe
            {
                var framework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance();
                var ui3DModule = framework->GetUiModule()->GetUI3DModule();
                nameplateInfoArrayPtr = new IntPtr(&(framework->GetUiModule()->GetRaptureAtkModule()->NamePlateInfoArray));
            }

            // Get the nameplate info for the nameplate object
            var namePlateInfoPtr = new IntPtr(nameplateInfoArrayPtr.ToInt64() + Marshal.SizeOf(typeof(RaptureAtkModule.NamePlateInfo)) * namePlateIndex);
            RaptureAtkModule.NamePlateInfo namePlateInfo = Marshal.PtrToStructure<RaptureAtkModule.NamePlateInfo>(namePlateInfoPtr);

            // Return the object for its object id
            var objectId = namePlateInfo.ObjectID.ObjectID;
            return m_ObjectTable.SearchById(objectId) as T;
        }
    }
}
