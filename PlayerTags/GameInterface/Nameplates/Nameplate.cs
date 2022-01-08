using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Runtime.InteropServices;

namespace PlayerTags.GameInterface.Nameplates
{
    /// <summary>
    /// Provides an interface to modify nameplates.
    /// </summary>
    public class Nameplate : IDisposable
    {
        private class PluginAddressResolver : BaseAddressResolver
        {
            private const string c_SetPlayerNameplateSignature = "48 89 5C 24 ?? 48 89 6C 24 ?? 56 57 41 54 41 56 41 57 48 83 EC 40 44 0F B6 E2";
            public IntPtr? SetPlayerNameplatePtr { get; private set; }

            protected override void Setup64Bit(SigScanner scanner)
            {
                if (scanner.TryScanText(c_SetPlayerNameplateSignature, out var setPlayerNameplatePtr))
                {
                    SetPlayerNameplatePtr = setPlayerNameplatePtr;
                }
            }
        }

        private delegate IntPtr SetPlayerNameplateDelegate_Unmanaged(IntPtr playerNameplateObjectPtr, bool isTitleAboveName, bool isTitleVisible, IntPtr titlePtr, IntPtr namePtr, IntPtr freeCompanyPtr, int iconId);
        private Hook<SetPlayerNameplateDelegate_Unmanaged>? m_SetPlayerNameplateHook;

        private PluginAddressResolver m_PluginAddressResolver;

        /// <summary>
        /// Occurs when a player nameplate is updated by the game.
        /// </summary>
        public event PlayerNameplateUpdatedDelegate? PlayerNameplateUpdated;

        /// <summary>
        /// Whether the required hooks are in place and this instance is valid.
        /// </summary>
        public bool IsValid
        {
            get
            {
                if (!m_PluginAddressResolver.SetPlayerNameplatePtr.HasValue)
                {
                    return false;
                }

                return true;
            }
        }

        public Nameplate()
        {
            m_PluginAddressResolver = new PluginAddressResolver();
            m_PluginAddressResolver.Setup();
            if (!IsValid)
            {
                return;
            }

            if (m_PluginAddressResolver.SetPlayerNameplatePtr.HasValue)
            {
                m_SetPlayerNameplateHook = new Hook<SetPlayerNameplateDelegate_Unmanaged>(m_PluginAddressResolver.SetPlayerNameplatePtr.Value, new SetPlayerNameplateDelegate_Unmanaged(SetPlayerNameplateDetour));
                m_SetPlayerNameplateHook?.Enable();
            }
        }

        public void Dispose()
        {
            m_SetPlayerNameplateHook?.Disable();
        }

        private IntPtr SetPlayerNameplateDetour(IntPtr playerNameplateObjectPtr, bool isTitleAboveName, bool isTitleVisible, IntPtr titlePtr, IntPtr namePtr, IntPtr freeCompanyPtr, int iconId)
        {
            if (m_SetPlayerNameplateHook == null)
            {
                return IntPtr.Zero;
            }

            try
            {
                PlayerCharacter? playerCharacter = GetNameplateGameObject<PlayerCharacter>(playerNameplateObjectPtr);
                if (playerCharacter != null)
                {
                    PlayerNameplateUpdatedArgs playerNameplateUpdatedArgs = new PlayerNameplateUpdatedArgs(
                        playerCharacter,
                        GameInterfaceHelper.ReadSeString(namePtr),
                        GameInterfaceHelper.ReadSeString(titlePtr),
                        GameInterfaceHelper.ReadSeString(freeCompanyPtr),
                        isTitleVisible,
                        isTitleAboveName,
                        iconId);

                    var beforeNameHashCode = playerNameplateUpdatedArgs.Name.GetHashCode();
                    var beforeTitleHashCode = playerNameplateUpdatedArgs.Title.GetHashCode();
                    var beforeFreeCompanyHashCode = playerNameplateUpdatedArgs.FreeCompany.GetHashCode();

                    PlayerNameplateUpdated?.Invoke(playerNameplateUpdatedArgs);

                    IntPtr newNamePtr = namePtr;
                    bool hasNameChanged = beforeNameHashCode != playerNameplateUpdatedArgs.Name.GetHashCode();
                    if (hasNameChanged)
                    {
                        newNamePtr = GameInterfaceHelper.PluginAllocate(playerNameplateUpdatedArgs.Name);
                    }

                    IntPtr newTitlePtr = titlePtr;
                    bool hasTitleChanged = beforeTitleHashCode != playerNameplateUpdatedArgs.Title.GetHashCode();
                    if (hasTitleChanged)
                    {
                        newTitlePtr = GameInterfaceHelper.PluginAllocate(playerNameplateUpdatedArgs.Title);
                    }

                    IntPtr newFreeCompanyPtr = freeCompanyPtr;
                    bool hasFreeCompanyChanged = beforeFreeCompanyHashCode != playerNameplateUpdatedArgs.FreeCompany.GetHashCode();
                    if (hasFreeCompanyChanged)
                    {
                        newFreeCompanyPtr = GameInterfaceHelper.PluginAllocate(playerNameplateUpdatedArgs.FreeCompany);
                    }

                    var result = m_SetPlayerNameplateHook.Original(playerNameplateObjectPtr, playerNameplateUpdatedArgs.IsTitleAboveName, playerNameplateUpdatedArgs.IsTitleVisible, newNamePtr, newTitlePtr, newFreeCompanyPtr, playerNameplateUpdatedArgs.IconId);

                    if (hasNameChanged)
                    {
                        GameInterfaceHelper.PluginFree(ref newNamePtr);
                    }

                    if (hasTitleChanged)
                    {
                        GameInterfaceHelper.PluginFree(ref newTitlePtr);
                    }

                    if (hasFreeCompanyChanged)
                    {
                        GameInterfaceHelper.PluginFree(ref newFreeCompanyPtr);
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"SetPlayerNameplateDetour");
            }

            return m_SetPlayerNameplateHook.Original(playerNameplateObjectPtr, isTitleAboveName, isTitleVisible, titlePtr, namePtr, freeCompanyPtr, iconId);
        }

        private T? GetNameplateGameObject<T>(IntPtr nameplateObjectPtr)
            where T : GameObject
        {
            // Get the nameplate object array
            var nameplateAddonPtr = PluginServices.GameGui.GetAddonByName("NamePlate", 1);
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
            if (namePlateIndex < 0 || namePlateIndex >= AddonNamePlate.NumNamePlateObjects)
            {
                return null;
            }

            // Get the nameplate info array
            IntPtr nameplateInfoArrayPtr = IntPtr.Zero;
            unsafe
            {
                var framework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance();
                nameplateInfoArrayPtr = new IntPtr(&framework->GetUiModule()->GetRaptureAtkModule()->NamePlateInfoArray);
            }

            // Get the nameplate info for the nameplate object
            var namePlateInfoPtr = new IntPtr(nameplateInfoArrayPtr.ToInt64() + Marshal.SizeOf(typeof(RaptureAtkModule.NamePlateInfo)) * namePlateIndex);
            RaptureAtkModule.NamePlateInfo namePlateInfo = Marshal.PtrToStructure<RaptureAtkModule.NamePlateInfo>(namePlateInfoPtr);

            // Return the object for its object id
            var objectId = namePlateInfo.ObjectID.ObjectID;
            return PluginServices.ObjectTable.SearchById(objectId) as T;
        }
    }
}
