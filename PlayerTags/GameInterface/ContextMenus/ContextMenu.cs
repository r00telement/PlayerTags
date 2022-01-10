using Dalamud.Game;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace PlayerTags.GameInterface.ContextMenus
{
    internal unsafe delegate void AtkValueChangeTypeDelegate_Unmanaged(AtkValue* thisPtr, FFXIVClientStructs.FFXIV.Component.GUI.ValueType type);
    internal unsafe delegate void AtkValueSetStringDelegate_Unmanaged(AtkValue* thisPtr, byte* bytes);

    /// <summary>
    /// Provides an interface to modify context menus.
    /// </summary>
    public class ContextMenu : IDisposable
    {
        private class PluginAddressResolver : BaseAddressResolver
        {
            private const string c_AtkValueChangeType = "E8 ?? ?? ?? ?? 45 84 F6 48 8D 4C 24";
            public IntPtr? AtkValueChangeTypePtr { get; private set; }

            private const string c_AtkValueSetString = "E8 ?? ?? ?? ?? 41 03 ED";
            public IntPtr? AtkValueSetStringPtr { get; private set; }

            private const string c_GetAddonById = "E8 ?? ?? ?? ?? 8B 6B 20";
            public IntPtr? GetAddonByIdPtr { get; private set; }

            private const string c_OpenSubContextMenu = "E8 ?? ?? ?? ?? 44 39 A3 ?? ?? ?? ?? 0F 86";
            public IntPtr? OpenSubContextMenuPtr { get; private set; }

            private const string c_ContextMenuOpening = "E8 ?? ?? ?? ?? 0F B7 C0 48 83 C4 60";
            public IntPtr? ContextMenuOpeningPtr { get; private set; }

            private const string c_ContextMenuOpened = "48 8B C4 57 41 56 41 57 48 81 EC";
            public IntPtr? ContextMenuOpenedPtr { get; private set; }

            private const string c_ContextMenuItemSelected = "48 89 5C 24 ?? 55 57 41 56 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 44 24 ?? 80 B9";
            public IntPtr? ContextMenuItemSelectedPtr { get; private set; }

            private const string c_SubContextMenuOpening = "E8 ?? ?? ?? ?? 44 39 A3 ?? ?? ?? ?? 0F 84";
            public IntPtr? SubContextMenuOpeningPtr { get; private set; }

            private const string c_SubContextMenuOpened = "48 8B C4 57 41 55 41 56 48 81 EC";
            public IntPtr? SubContextMenuOpenedPtr { get; private set; }

            private const string c_OpenInventoryContextMenu = "44 88 44 24 ?? 88 54 24 10 53";
            public IntPtr? OpenInventoryContextMenuPtr { get; private set; }

            private const string c_InventoryContextMenuEvent30 = "E8 ?? ?? ?? ?? 48 83 C4 30 5B C3 8B 83";
            public IntPtr? InventoryContextMenuEvent30Ptr { get; private set; }

            protected override void Setup64Bit(SigScanner scanner)
            {
                if (scanner.TryScanText(c_AtkValueChangeType, out var atkValueChangeTypePtr))
                {
                    AtkValueChangeTypePtr = atkValueChangeTypePtr;
                }

                if (scanner.TryScanText(c_AtkValueSetString, out var atkValueSetStringPtr))
                {
                    AtkValueSetStringPtr = atkValueSetStringPtr;
                }

                if (scanner.TryScanText(c_GetAddonById, out var getAddonByIdPtr))
                {
                    GetAddonByIdPtr = getAddonByIdPtr;
                }

                if (scanner.TryScanText(c_OpenSubContextMenu, out var openSubContextMenuPtr))
                {
                    OpenSubContextMenuPtr = openSubContextMenuPtr;
                }

                if (scanner.TryScanText(c_ContextMenuOpening, out var someOpenAddonThingPtr))
                {
                    ContextMenuOpeningPtr = someOpenAddonThingPtr;
                }

                if (scanner.TryScanText(c_ContextMenuOpened, out var contextMenuOpenedPtr))
                {
                    ContextMenuOpenedPtr = contextMenuOpenedPtr;
                }

                if (scanner.TryScanText(c_ContextMenuItemSelected, out var contextMenuItemSelectedPtr))
                {
                    ContextMenuItemSelectedPtr = contextMenuItemSelectedPtr;
                }

                if (scanner.TryScanText(c_SubContextMenuOpening, out var subContextMenuOpening))
                {
                    SubContextMenuOpeningPtr = subContextMenuOpening;
                }

                if (scanner.TryScanText(c_SubContextMenuOpened, out var titleScreenContextMenuOpenedPtr))
                {
                    SubContextMenuOpenedPtr = titleScreenContextMenuOpenedPtr;
                }

                if (scanner.TryScanText(c_OpenInventoryContextMenu, out var setUpInventoryContextSubMenuPtr))
                {
                    OpenInventoryContextMenuPtr = setUpInventoryContextSubMenuPtr;
                }

                if (scanner.TryScanText(c_InventoryContextMenuEvent30, out var inventoryContextMenuEvent30Ptr))
                {
                    InventoryContextMenuEvent30Ptr = inventoryContextMenuEvent30Ptr;
                }
            }
        }

        private readonly AtkValueChangeTypeDelegate_Unmanaged? m_AtkValueChangeType;
        private readonly AtkValueSetStringDelegate_Unmanaged? m_AtkValueSetString;

        private delegate IntPtr GetAddonByIdDelegate_Unmanaged(IntPtr raptureAtkUnitManager, ushort id);
        private readonly GetAddonByIdDelegate_Unmanaged? m_GetAddonById;

        private delegate bool OpenSubContextMenuDelegate_Unmanaged(IntPtr agent);
        private readonly OpenSubContextMenuDelegate_Unmanaged? m_OpenSubContextMenu;

        private delegate IntPtr ContextMenuOpeningDelegate_Unmanaged(IntPtr a1, IntPtr a2, IntPtr a3, uint a4, IntPtr a5, IntPtr agent, IntPtr a7, ushort a8);
        private Hook<ContextMenuOpeningDelegate_Unmanaged>? m_ContextMenuOpeningHook;

        private unsafe delegate bool ContextMenuOpenedDelegate_Unmanaged(IntPtr addon, int menuSize, AtkValue* atkValueArgs);
        private Hook<ContextMenuOpenedDelegate_Unmanaged>? m_ContextMenuOpenedHook;
        private Hook<ContextMenuOpenedDelegate_Unmanaged>? m_SubContextMenuOpenedHook;

        private delegate bool ContextMenuItemSelectedDelegate_Unmanaged(IntPtr addon, int selectedIndex, byte a3);
        private Hook<ContextMenuItemSelectedDelegate_Unmanaged>? m_ContextMenuItemSelectedHook;

        private delegate bool SubContextMenuOpeningDelegate_Unmanaged(IntPtr agent);
        private Hook<SubContextMenuOpeningDelegate_Unmanaged>? m_SubContextMenuOpeningHook;

        private delegate IntPtr OpenInventoryContextMenuDelegate_Unmanaged(IntPtr agent, byte hasTitle, byte zero);
        private readonly OpenInventoryContextMenuDelegate_Unmanaged? m_OpenInventoryContextMenu;

        private delegate void InventoryContextMenuEvent30Delegate_Unmanaged(IntPtr agent, IntPtr a2, int a3, int a4, short a5);
        private Hook<InventoryContextMenuEvent30Delegate_Unmanaged>? m_InventoryContextMenuEvent30Hook;

        private PluginAddressResolver m_PluginAddressResolver;

        private const int MaxContextMenuItemsPerContextMenu = 32;

        private IntPtr m_CurrentContextMenuAgent;
        private IntPtr m_CurrentSubContextMenuTitle;

        private ContextMenuItem? m_CurrentSelectedItem;
        private ContextMenuOpenedArgs? m_CurrentContextMenuOpenedArgs;

        /// <summary>
        /// Occurs when a context menu is opened by the game.
        /// </summary>
        public event ContextMenuOpenedDelegate? ContextMenuOpened;

        /// <summary>
        /// Whether the required hooks are in place and this instance is valid.
        /// </summary>
        public bool IsValid
        {
            get
            {
                if (!m_PluginAddressResolver.AtkValueChangeTypePtr.HasValue
                    || !m_PluginAddressResolver.AtkValueSetStringPtr.HasValue
                    || !m_PluginAddressResolver.GetAddonByIdPtr.HasValue
                    || !m_PluginAddressResolver.OpenSubContextMenuPtr.HasValue
                    || !m_PluginAddressResolver.ContextMenuOpeningPtr.HasValue
                    || !m_PluginAddressResolver.ContextMenuOpenedPtr.HasValue
                    || !m_PluginAddressResolver.ContextMenuItemSelectedPtr.HasValue
                    || !m_PluginAddressResolver.SubContextMenuOpeningPtr.HasValue
                    || !m_PluginAddressResolver.SubContextMenuOpenedPtr.HasValue
                    || !m_PluginAddressResolver.OpenInventoryContextMenuPtr.HasValue
                    || !m_PluginAddressResolver.InventoryContextMenuEvent30Ptr.HasValue
                    )
                {
                    return false;
                }

                return true;
            }
        }

        public ContextMenu()
        {
            m_PluginAddressResolver = new PluginAddressResolver();
            m_PluginAddressResolver.Setup();
            if (!IsValid)
            {
                return;
            }

            if (m_PluginAddressResolver.AtkValueChangeTypePtr.HasValue)
            {
                m_AtkValueChangeType = Marshal.GetDelegateForFunctionPointer<AtkValueChangeTypeDelegate_Unmanaged>(m_PluginAddressResolver.AtkValueChangeTypePtr.Value);
            }

            if (m_PluginAddressResolver.AtkValueSetStringPtr.HasValue)
            {
                m_AtkValueSetString = Marshal.GetDelegateForFunctionPointer<AtkValueSetStringDelegate_Unmanaged>(m_PluginAddressResolver.AtkValueSetStringPtr.Value);
            }

            if (m_PluginAddressResolver.GetAddonByIdPtr.HasValue)
            {
                m_GetAddonById = Marshal.GetDelegateForFunctionPointer<GetAddonByIdDelegate_Unmanaged>(m_PluginAddressResolver.GetAddonByIdPtr.Value);
            }

            if (m_PluginAddressResolver.OpenSubContextMenuPtr.HasValue)
            {
                m_OpenSubContextMenu = Marshal.GetDelegateForFunctionPointer<OpenSubContextMenuDelegate_Unmanaged>(m_PluginAddressResolver.OpenSubContextMenuPtr.Value);
            }

            if (m_PluginAddressResolver.ContextMenuOpeningPtr.HasValue)
            {
                m_ContextMenuOpeningHook = new Hook<ContextMenuOpeningDelegate_Unmanaged>(m_PluginAddressResolver.ContextMenuOpeningPtr.Value, ContextMenuOpeningDetour);
                m_ContextMenuOpeningHook?.Enable();
            }

            if (m_PluginAddressResolver.ContextMenuOpenedPtr.HasValue)
            {
                unsafe
                {
                    m_ContextMenuOpenedHook = new Hook<ContextMenuOpenedDelegate_Unmanaged>(m_PluginAddressResolver.ContextMenuOpenedPtr.Value, ContextMenuOpenedDetour);
                }
                m_ContextMenuOpenedHook?.Enable();
            }

            if (m_PluginAddressResolver.ContextMenuItemSelectedPtr.HasValue)
            {
                m_ContextMenuItemSelectedHook = new Hook<ContextMenuItemSelectedDelegate_Unmanaged>(m_PluginAddressResolver.ContextMenuItemSelectedPtr.Value, ContextMenuItemSelectedDetour);
                m_ContextMenuItemSelectedHook?.Enable();
            }

            if (m_PluginAddressResolver.SubContextMenuOpeningPtr.HasValue)
            {
                m_SubContextMenuOpeningHook = new Hook<SubContextMenuOpeningDelegate_Unmanaged>(m_PluginAddressResolver.SubContextMenuOpeningPtr.Value, SubContextMenuOpeningDetour);
                m_SubContextMenuOpeningHook?.Enable();
            }

            if (m_PluginAddressResolver.SubContextMenuOpenedPtr.HasValue)
            {
                unsafe
                {
                    m_SubContextMenuOpenedHook = new Hook<ContextMenuOpenedDelegate_Unmanaged>(m_PluginAddressResolver.SubContextMenuOpenedPtr.Value, SubContextMenuOpenedDetour);
                }
                m_SubContextMenuOpenedHook?.Enable();
            }

            if (m_PluginAddressResolver.OpenInventoryContextMenuPtr.HasValue)
            {
                m_OpenInventoryContextMenu = Marshal.GetDelegateForFunctionPointer<OpenInventoryContextMenuDelegate_Unmanaged>(m_PluginAddressResolver.OpenInventoryContextMenuPtr.Value);
            }

            if (m_PluginAddressResolver.InventoryContextMenuEvent30Ptr.HasValue)
            {
                m_InventoryContextMenuEvent30Hook = new Hook<InventoryContextMenuEvent30Delegate_Unmanaged>(m_PluginAddressResolver.InventoryContextMenuEvent30Ptr.Value, InventoryContextMenuEvent30Detour);
                m_InventoryContextMenuEvent30Hook?.Enable();
            }
        }

        public void Dispose()
        {
            m_InventoryContextMenuEvent30Hook?.Disable();
            m_SubContextMenuOpeningHook?.Disable();
            m_ContextMenuItemSelectedHook?.Disable();
            m_SubContextMenuOpenedHook?.Disable();
            m_ContextMenuOpenedHook?.Disable();
            m_ContextMenuOpeningHook?.Disable();
        }

        private unsafe IntPtr ContextMenuOpeningDetour(IntPtr a1, IntPtr a2, IntPtr a3, uint a4, IntPtr a5, IntPtr agent, IntPtr a7, ushort a8)
        {
            PluginLog.Debug($"ContextMenuOpeningDetour");
            m_CurrentContextMenuAgent = agent;
            return m_ContextMenuOpeningHook!.Original(a1, a2, a3, a4, a5, agent, a7, a8);
        }

        private unsafe bool ContextMenuOpenedDetour(IntPtr addon, int atkValueCount, AtkValue* atkValues)
        {
            PluginLog.Debug($"ContextMenuOpenedDetour");
            if (m_ContextMenuOpenedHook == null)
            {
                return false;
            }

            try
            {
                ContextMenuOpenedImplementation(addon, ref atkValueCount, ref atkValues);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "ContextMenuOpenedDetour");
            }

            return m_ContextMenuOpenedHook.Original(addon, atkValueCount, atkValues);
        }

        private unsafe void ContextMenuOpenedImplementation(IntPtr addon, ref int atkValueCount, ref AtkValue* atkValues)
        {
            PluginLog.Debug($"ContextMenuOpenedImplementation   {m_CurrentSelectedItem}");

            if (m_AtkValueChangeType == null
                || m_AtkValueSetString == null
                || ContextMenuOpened == null
                || m_CurrentContextMenuAgent == IntPtr.Zero)
            {
                return;
            }

            ContextMenuOpenedDelegate contextMenuOpenedDelegate = ContextMenuOpened;
            if (m_CurrentSelectedItem is OpenSubContextMenuItem openSubContextMenuItem)
            {
                contextMenuOpenedDelegate = openSubContextMenuItem.Opened;
            }

            // Read the context menu items from the game, then allow subscribers to modify them
            ContextMenuReaderWriter contextMenuReaderWriter = new ContextMenuReaderWriter(m_CurrentContextMenuAgent, atkValueCount, atkValues);
            m_CurrentContextMenuOpenedArgs = NotifyContextMenuOpened(addon, m_CurrentContextMenuAgent, m_CurrentSelectedItem, contextMenuOpenedDelegate, contextMenuReaderWriter.Read());
            if (m_CurrentContextMenuOpenedArgs == null)
            {
                return;
            }

            contextMenuReaderWriter.Write(m_CurrentContextMenuOpenedArgs, m_AtkValueChangeType, m_AtkValueSetString);

            // Update the addon
            var addonContext = (AddonContext*)addon;
            atkValueCount = *(&addonContext->AtkValuesCount) = (ushort)contextMenuReaderWriter.AtkValueCount;
            atkValues = *(&addonContext->AtkValues) = contextMenuReaderWriter.AtkValues;
        }

        private bool SubContextMenuOpeningDetour(IntPtr agent)
        {
            PluginLog.Debug($"SubContextMenuOpeningDetour");
            if (m_SubContextMenuOpeningHook == null)
            {
                return false;
            }

            if (SubContextMenuOpeningImplementation(agent))
            {
                return true;
            }

            return m_SubContextMenuOpeningHook.Original(agent);
        }

        private unsafe bool SubContextMenuOpeningImplementation(IntPtr agent)
        {
            PluginLog.Debug($"SubContextMenuOpeningImplementation   {m_CurrentSelectedItem}");

            if (m_OpenSubContextMenu == null
                || m_OpenInventoryContextMenu == null
                || m_AtkValueChangeType == null
                || !(m_CurrentSelectedItem is OpenSubContextMenuItem))
            {
                return false;
            }
        
            // The important things to make this work are:
            // 1. Temporary allocate a sub context menu title
            // 1. Temporarily increase the atk value count by 1 so the game knows to expect at least 1 context menu item
            // Other than those requirements, the data is irrelevant and will be set when the menu has actually opened.

            var agentContext = (AgentContext*)agent;
            if (IsInventoryContext(agent))
            {
                m_OpenInventoryContextMenu(agent, 0, 0);
            }
            else
            {
                m_OpenSubContextMenu(agent);

                // Free any sub context menu title we've already allocated
                GameInterfaceHelper.GameFree(ref m_CurrentSubContextMenuTitle, (ulong)IntPtr.Size);

                // Allocate a new 1 byte title. Without this, a title won't be rendered.
                // The actual value doesn't matter at this point, we'll set it later.
                m_CurrentSubContextMenuTitle = GameInterfaceHelper.GameUIAllocate(1);
                *(&agentContext->SubContextMenuTitle) = (byte*)m_CurrentSubContextMenuTitle;
            }

            var atkValues = &agentContext->ItemData->AtkValues;

            // Let the game know the context menu will have at least 1 item in it
            var newAtkValuesCount = agentContext->ItemData->AtkValuesCount + 1;
            *(&agentContext->ItemData->AtkValuesCount) = (ushort)(newAtkValuesCount);
            atkValues[0].UInt = 1;

            ContextMenuReaderWriter.Print(agentContext->ItemData->AtkValuesCount, atkValues);

            return true;
        }

        private unsafe bool SubContextMenuOpenedDetour(IntPtr addon, int atkValueCount, AtkValue* atkValues)
        {
            PluginLog.Debug($"SubContextMenuOpenedDetour");
            if (m_SubContextMenuOpenedHook == null)
            {
                return false;
            }

            try
            {
                ContextMenuOpenedImplementation(addon, ref atkValueCount, ref atkValues);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "SubContextMenuOpenedDetour");
            }

            return m_SubContextMenuOpenedHook.Original(addon, atkValueCount, atkValues);
        }

        private unsafe ContextMenuOpenedArgs? NotifyContextMenuOpened(IntPtr addon, IntPtr agent, ContextMenuItem? selectedContextMenuItem, ContextMenuOpenedDelegate contextMenuOpenedDelegate, IEnumerable<ContextMenuItem> initialContextMenuItems)
        {
            var parentAddonName = GetParentAddonName(addon);

            ItemContext? itemContext = null;
            GameObjectContext? gameObjectContext = null;
            if (IsInventoryContext(agent))
            {
                var agentInventoryContext = (AgentInventoryContext*)agent;
                itemContext = new ItemContext(agentInventoryContext->ItemId, agentInventoryContext->ItemCount, agentInventoryContext->IsHighQuality);
            }
            else
            {
                var agentContext = (AgentContext*)agent;
                if (agentContext->GameObjectLowerContentId != 0 || agentContext->GameObjectWorldId != 0)
                {
                    SeString objectName;
                    unsafe
                    {
                        objectName = GameInterfaceHelper.ReadSeString((IntPtr)agentContext->ObjectName.StringPtr);
                    }

                    gameObjectContext = new GameObjectContext(agentContext->GameObjectId, agentContext->GameObjectLowerContentId, objectName, agentContext->GameObjectWorldId);
                }
            }

            var contextMenuOpenedArgs = new ContextMenuOpenedArgs(addon, agent, parentAddonName, initialContextMenuItems)
            {
                SelectedItem = selectedContextMenuItem,
                ItemContext = itemContext,
                GameObjectContext = gameObjectContext
            };

            try
            {
                contextMenuOpenedDelegate.Invoke(contextMenuOpenedArgs);
            }
            catch (Exception ex)
            {
                PluginLog.LogError(ex, "NotifyContextMenuOpened");
                return null;
            }

            foreach (var contextMenuItem in contextMenuOpenedArgs.ContextMenuItems)
            {
                contextMenuItem.Agent = agent;
            }

            if (contextMenuOpenedArgs.ContextMenuItems.Count > MaxContextMenuItemsPerContextMenu)
            {
                PluginLog.LogWarning($"Context menu requesting {contextMenuOpenedArgs.ContextMenuItems.Count} of max {MaxContextMenuItemsPerContextMenu} items. Resizing list to compensate.");
                contextMenuOpenedArgs.ContextMenuItems.RemoveRange(MaxContextMenuItemsPerContextMenu, contextMenuOpenedArgs.ContextMenuItems.Count - MaxContextMenuItemsPerContextMenu);
            }

            return contextMenuOpenedArgs;
        }

        private unsafe bool ContextMenuItemSelectedDetour(IntPtr addon, int selectedIndex, byte a3)
        {
            PluginLog.Debug($"ContextMenuItemSelectedDetour   selectedIndex={selectedIndex}");
            if (m_ContextMenuItemSelectedHook == null)
            {
                return false;
            }

            try
            {
                ContextMenuItemSelectedImplementation(addon, selectedIndex);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "ContextMenuItemSelectedDetour");
            }

            return m_ContextMenuItemSelectedHook.Original(addon, selectedIndex, a3);
        }

        private unsafe void ContextMenuItemSelectedImplementation(IntPtr addon, int selectedIndex)
        {
            PluginLog.Debug($"ContextMenuItemSelectedImplementation");

            if (m_CurrentContextMenuOpenedArgs == null)
            {
                m_CurrentContextMenuOpenedArgs = null;
                m_CurrentSelectedItem = null;
                return;
            }

            // Read the selected item directly from the game
            var addonContext = (AddonContext*)addon;
            ContextMenuReaderWriter contextMenuReaderWriter = new ContextMenuReaderWriter(m_CurrentContextMenuAgent, addonContext->AtkValuesCount, addonContext->AtkValues);
            var gameContextMenuItems = contextMenuReaderWriter.Read();
            var gameSelectedItem = gameContextMenuItems.ElementAtOrDefault(selectedIndex);
            if (gameSelectedItem == null)
            {
                return;
            }

            // Match it with the items we already know about based on its name.
            // We need to do this dance because other plugins may have written new items to memory that we didn't know about, so we can't match directly on index.
            var selectedItem = m_CurrentContextMenuOpenedArgs.ContextMenuItems.FirstOrDefault(item => item.Name.Encode().SequenceEqual(gameSelectedItem.Name.Encode()));
            if (selectedItem == null)
            {
                m_CurrentContextMenuOpenedArgs = null;
                m_CurrentSelectedItem = null;
                return;
            }

            if (selectedItem is CustomContextMenuItem customContextMenuItem)
            {
                try
                {
                    var customContextMenuItemSelectedArgs = new CustomContextMenuItemSelectedArgs(m_CurrentContextMenuOpenedArgs, customContextMenuItem);
                    customContextMenuItem.ItemSelected(customContextMenuItemSelectedArgs);
                }
                catch (Exception ex)
                {
                    PluginLog.LogError(ex, "ContextMenuItemSelectedImplementation");
                }
            }

            m_CurrentSelectedItem = selectedItem;
            m_CurrentContextMenuOpenedArgs = null;
        }

        private void InventoryContextMenuEvent30Detour(IntPtr agent, IntPtr a2, int a3, int a4, short a5)
        {
            PluginLog.Debug($"InventoryContextMenuEvent30Detour");
            if (m_InventoryContextMenuEvent30Hook == null)
            {
                return;
            }

            if (SubContextMenuOpeningImplementation(agent))
            {
                return;
            }

            m_InventoryContextMenuEvent30Hook.Original(agent, a2, a3, a4, a5);
        }

        private unsafe string? GetParentAddonName(IntPtr addon)
        {
            if (m_GetAddonById == null)
            {
                return null;
            }

            var parentAddonId = Marshal.PtrToStructure<Addon>(addon).ParentAddonId;
            if (parentAddonId == 0)
            {
                return null;
            }

            var atkStage = AtkStage.GetSingleton();
            var parentAddonPtr = m_GetAddonById((IntPtr)atkStage->RaptureAtkUnitManager, parentAddonId);

            return GameInterfaceHelper.ReadString(parentAddonPtr + 8);
        }

        private unsafe bool IsInventoryContext(IntPtr agent)
        {
            if (agent == (IntPtr)AgentInventoryContext.Instance())
            {
                return true;
            }

            return false;
        }

        private unsafe IntPtr GetAddonFromAgent(IntPtr agent)
        {
            if (m_GetAddonById == null)
            {
                return IntPtr.Zero;
            }

            var agentInterface = (AgentInterface*)agent;
            if (agentInterface->AddonId == 0)
            {
                return IntPtr.Zero;
            }

            return m_GetAddonById((IntPtr)AtkStage.GetSingleton()->RaptureAtkUnitManager, (ushort)agentInterface->AddonId);
        }
    }
}
