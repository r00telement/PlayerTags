using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace PlayerTags.GameInterface.Nameplates
{
    /// <summary>
    /// Provides an interface to modify nameplates.
    /// </summary>
    public class Nameplate : IDisposable
    {
        [Signature("48 89 5C 24 ?? 48 89 6C 24 ?? 56 57 41 54 41 56 41 57 48 83 EC 40 44 0F B6 E2", DetourName = nameof(SetPlayerNameplateDetour))]
        private readonly Hook<AddonNamePlate_SetPlayerNameplateDetour>? hook_AddonNamePlate_SetPlayerNameplateDetour = null;
        private unsafe delegate IntPtr AddonNamePlate_SetPlayerNameplateDetour(IntPtr playerNameplateObjectPtr, bool isTitleAboveName, bool isTitleVisible, IntPtr titlePtr, IntPtr namePtr, IntPtr freeCompanyPtr, int iconId);

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
                return hook_AddonNamePlate_SetPlayerNameplateDetour != null
                    && hook_AddonNamePlate_SetPlayerNameplateDetour.IsEnabled;
            }
        }

        public Nameplate()
        {
            SignatureHelper.Initialise(this);
            hook_AddonNamePlate_SetPlayerNameplateDetour?.Enable();
        }

        public void Dispose()
        {
            hook_AddonNamePlate_SetPlayerNameplateDetour?.Disable();
        }

        private IntPtr SetPlayerNameplateDetour(IntPtr playerNameplateObjectPtr, bool isTitleAboveName, bool isTitleVisible, IntPtr titlePtr, IntPtr namePtr, IntPtr freeCompanyPtr, int iconId)
        {
            if (hook_AddonNamePlate_SetPlayerNameplateDetour == null)
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

                    byte[] beforeNameBytes = playerNameplateUpdatedArgs.Name.Encode();
                    byte[] beforeTitleBytes = playerNameplateUpdatedArgs.Title.Encode();
                    byte[] beforeFreeCompanyBytes = playerNameplateUpdatedArgs.FreeCompany.Encode();

                    PlayerNameplateUpdated?.Invoke(playerNameplateUpdatedArgs);

                    byte[] afterNameBytes = playerNameplateUpdatedArgs.Name.Encode();
                    byte[] afterTitleBytes = playerNameplateUpdatedArgs.Title.Encode();
                    byte[] afterFreeCompanyBytes = playerNameplateUpdatedArgs.FreeCompany.Encode();

                    IntPtr newNamePtr = namePtr;
                    bool hasNameChanged = !beforeNameBytes.SequenceEqual(afterNameBytes);
                    if (hasNameChanged)
                    {
                        newNamePtr = GameInterfaceHelper.PluginAllocate(afterNameBytes);
                    }

                    IntPtr newTitlePtr = titlePtr;
                    bool hasTitleChanged = !beforeTitleBytes.SequenceEqual(afterTitleBytes);
                    if (hasTitleChanged)
                    {
                        newTitlePtr = GameInterfaceHelper.PluginAllocate(afterTitleBytes);
                    }

                    IntPtr newFreeCompanyPtr = freeCompanyPtr;
                    bool hasFreeCompanyChanged = !beforeFreeCompanyBytes.SequenceEqual(afterFreeCompanyBytes);
                    if (hasFreeCompanyChanged)
                    {
                        newFreeCompanyPtr = GameInterfaceHelper.PluginAllocate(afterFreeCompanyBytes);
                    }

                    var result = hook_AddonNamePlate_SetPlayerNameplateDetour.Original(playerNameplateObjectPtr, playerNameplateUpdatedArgs.IsTitleAboveName, playerNameplateUpdatedArgs.IsTitleVisible, newTitlePtr, newNamePtr, newFreeCompanyPtr, playerNameplateUpdatedArgs.IconId);

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

            return hook_AddonNamePlate_SetPlayerNameplateDetour.Original(playerNameplateObjectPtr, isTitleAboveName, isTitleVisible, titlePtr, namePtr, freeCompanyPtr, iconId);
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
