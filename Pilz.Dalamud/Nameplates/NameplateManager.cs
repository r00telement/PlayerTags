using Dalamud.Hooking;
using Pilz.Dalamud.Nameplates.EventArgs;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.UI;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects.Types;
using Pilz.Dalamud.Nameplates.Model;

namespace Pilz.Dalamud.Nameplates
{
    public class NameplateManager : IDisposable
    {
        /// <summary>
        /// Provides events that you can hook to.
        /// </summary>
        public NameplateHooks Hooks { get; init; } = new();

        /// <summary>
        /// Defines if all hooks are enabled and the NameplateManager is ready to go. If this is false, then there might be something wrong or something already has been disposed.
        /// </summary>
        public bool IsValid => Hooks.IsValid;

        /// <summary>
        /// Creates a new instance of the NameplateManager.
        /// </summary>
        public NameplateManager()
        {
            Hooks.Initialize();
        }

        ~NameplateManager()
        {
            Dispose();
        }

        public void Dispose()
        {
            Hooks?.Dispose();
        }

        public static T? GetNameplateGameObject<T>(SafeNameplateObject namePlateObject) where T : GameObject
        {
            return GetNameplateGameObject<T>(namePlateObject.Pointer);
        }

        public static T? GetNameplateGameObject<T>(IntPtr nameplateObjectPtr) where T : GameObject
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
