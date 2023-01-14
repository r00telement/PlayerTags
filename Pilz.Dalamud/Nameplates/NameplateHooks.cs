using Dalamud.Hooking;
using Pilz.Dalamud.Nameplates.EventArgs;
using Dalamud.Utility.Signatures;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pilz.Dalamud.Nameplates.Model;
using Lumina.Excel.GeneratedSheets;
using System.Xml.Linq;

namespace Pilz.Dalamud.Nameplates
{
    public class NameplateHooks : IDisposable
    {
        /// <summary>
        /// Will be executed when the the Game wants to update the content of a nameplate with the details of the Player.
        /// </summary>
        public event AddonNamePlate_SetPlayerNameEventHandler AddonNamePlate_SetPlayerName;
        public delegate void AddonNamePlate_SetPlayerNameEventHandler(AddonNamePlate_SetPlayerNameEventArgs eventArgs);

        /// <summary>
        /// Will be executed when the the Game wants to update the content of a nameplate with the details of the Player.
        /// This will event acts on a higher level with SeString instead of IntPtr e.g.
        /// </summary>
        public event AddonNamePlate_SetPlayerNameManagedEventHandler AddonNamePlate_SetPlayerNameManaged;
        public delegate void AddonNamePlate_SetPlayerNameManagedEventHandler(AddonNamePlate_SetPlayerNameManagedEventArgs eventArgs);

        [Signature("48 89 5C 24 ?? 48 89 6C 24 ?? 56 57 41 54 41 56 41 57 48 83 EC 40 44 0F B6 E2", DetourName = nameof(SetPlayerNameplateDetour))]
        private readonly Hook<AddonNamePlate_SetPlayerNameplateDetour>? hook_AddonNamePlate_SetPlayerNameplateDetour = null;
        private unsafe delegate IntPtr AddonNamePlate_SetPlayerNameplateDetour(IntPtr playerNameplateObjectPtr, bool isTitleAboveName, bool isTitleVisible, IntPtr titlePtr, IntPtr namePtr, IntPtr freeCompanyPtr, int iconId);

        /// <summary>
        /// Defines if all hooks are enabled. If this is false, then there might be something wrong or the class already has been disposed.
        /// </summary>
        public bool IsValid
        {
            get
            {
                var isValid = true;

                isValid &= IsHookEnabled(hook_AddonNamePlate_SetPlayerNameplateDetour);

                return isValid;
            }
        }

        /// <summary>
        /// Create a new instance of NAmeplateHooks and automatically initialize and enable all Hooks.
        /// </summary>
        public NameplateHooks()
        {
            SignatureHelper.Initialise(this);
        }

        ~NameplateHooks()
        {
            Dispose();
        }

        public void Dispose()
        {
            Unhook();
        }

        /// <summary>
        /// Initialize and enable all Hooks.
        /// </summary>
        internal void Initialize()
        {
            hook_AddonNamePlate_SetPlayerNameplateDetour?.Enable();
        }

        /// <summary>
        /// Disable all Hooks.
        /// </summary>
        internal void Unhook()
        {
            hook_AddonNamePlate_SetPlayerNameplateDetour?.Disable();
        }

        private static bool IsHookEnabled<T>(Hook<T> hook) where T : Delegate
        {
            return hook != null && hook.IsEnabled;
        }

        private IntPtr SetPlayerNameplateDetour(IntPtr playerNameplateObjectPtr, bool isTitleAboveName, bool isTitleVisible, IntPtr titlePtr, IntPtr namePtr, IntPtr freeCompanyPtr, int iconId)
        {
            var result = IntPtr.Zero;

            if (IsHookEnabled(hook_AddonNamePlate_SetPlayerNameplateDetour))
            {
                var eventArgs = new AddonNamePlate_SetPlayerNameEventArgs
                {
                    PlayerNameplateObjectPtr = playerNameplateObjectPtr,
                    TitlePtr = titlePtr,
                    NamePtr = namePtr,
                    FreeCompanyPtr = freeCompanyPtr,
                    IsTitleAboveName = isTitleAboveName,
                    IsTitleVisible = isTitleVisible,
                    IconID = iconId
                };

                void callOriginal()
                {
                    eventArgs.Result = eventArgs.Original();
                }

                // Add handler for the Original call
                eventArgs.CallOriginal += () =>
                {
                    return hook_AddonNamePlate_SetPlayerNameplateDetour.Original(
                        eventArgs.PlayerNameplateObjectPtr,
                        eventArgs.IsTitleAboveName,
                        eventArgs.IsTitleVisible,
                        eventArgs.TitlePtr,
                        eventArgs.NamePtr,
                        eventArgs.FreeCompanyPtr,
                        eventArgs.IconID);
                };

                // Invoke Event
                var hasDefaultHookEvent = AddonNamePlate_SetPlayerName != null;
                AddonNamePlate_SetPlayerName?.Invoke(eventArgs);

                if (AddonNamePlate_SetPlayerNameManaged != null)
                {
                    var freeTitle = false;
                    var freeName = false;
                    var freeFreeCompany = false;

                    // Create NamePlateObject if possible
                    var namePlateObj = new SafeNameplateObject(playerNameplateObjectPtr);

                    // Create new event
                    var managedEventArgs = new AddonNamePlate_SetPlayerNameManagedEventArgs
                    {
                        OriginalEventArgs = eventArgs,
                        SafeNameplateObject = namePlateObj,
                        Title = GameInterfaceHelper.ReadSeString(eventArgs.TitlePtr),
                        Name = GameInterfaceHelper.ReadSeString(eventArgs.NamePtr),
                        FreeCompany = GameInterfaceHelper.ReadSeString(eventArgs.FreeCompanyPtr)
                    };

                    // Get raw string content
                    var titleRaw = managedEventArgs.Title.Encode();
                    var nameRaw = managedEventArgs.Name.Encode();
                    var freeCompanyRaw = managedEventArgs.FreeCompany.Encode();

                    // Invoke Managed Event
                    AddonNamePlate_SetPlayerNameManaged.Invoke(managedEventArgs);

                    // Get new Title string ontent
                    var titleNewRaw = managedEventArgs.Title.Encode();
                    if (!titleRaw.SequenceEqual(titleNewRaw))
                    {
                        eventArgs.TitlePtr = GameInterfaceHelper.PluginAllocate(titleNewRaw);
                        freeTitle = true;
                    }

                    // Get new Name string ontent
                    var nameNewRaw = managedEventArgs.Name.Encode();
                    if (!nameRaw.SequenceEqual(nameNewRaw))
                    {
                        eventArgs.NamePtr = GameInterfaceHelper.PluginAllocate(nameNewRaw);
                        freeName = true;
                    }

                    // Get new Free Company string ontent
                    var freeCompanyNewRaw = managedEventArgs.FreeCompany.Encode();
                    if (!freeCompanyRaw.SequenceEqual(freeCompanyNewRaw))
                    {
                        eventArgs.FreeCompanyPtr = GameInterfaceHelper.PluginAllocate(freeCompanyNewRaw);
                        freeFreeCompany = true;
                    }

                    // Call Original as we changed something
                    callOriginal();

                    // Free memory
                    if (freeTitle)
                        GameInterfaceHelper.PluginFree(eventArgs.TitlePtr);
                    if (freeName)
                        GameInterfaceHelper.PluginFree(eventArgs.NamePtr);
                    if (freeFreeCompany)
                        GameInterfaceHelper.PluginFree(eventArgs.FreeCompanyPtr);
                }
                else if(!hasDefaultHookEvent)
                {
                    // Call original in case of nothing get called, just to get secure it will not break the game when not calling it.
                    callOriginal();
                }

                // Set result
                result = eventArgs.Result;
            }

            return result;
        }
    }
}
