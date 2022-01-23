using Dalamud.Game;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
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
            }
        }

        private readonly AtkValueChangeTypeDelegate_Unmanaged? m_AtkValueChangeType;
        private readonly AtkValueSetStringDelegate_Unmanaged? m_AtkValueSetString;

        private unsafe delegate AddonInterface* GetAddonByIdDelegate_Unmanaged(RaptureAtkUnitManager* raptureAtkUnitManager, ushort id);
        private readonly GetAddonByIdDelegate_Unmanaged? m_GetAddonById;

        private unsafe delegate bool OpenSubContextMenuDelegate_Unmanaged(AgentContext* agentContext);
        private readonly OpenSubContextMenuDelegate_Unmanaged? m_OpenSubContextMenu;

        private unsafe delegate IntPtr ContextMenuOpeningDelegate_Unmanaged(IntPtr a1, IntPtr a2, IntPtr a3, uint a4, IntPtr a5, AgentContextInterface* agentContextInterface, IntPtr a7, ushort a8);
        private Hook<ContextMenuOpeningDelegate_Unmanaged>? m_ContextMenuOpeningHook;

        private unsafe delegate bool ContextMenuOpenedDelegate_Unmanaged(AddonContextMenu* addonContextMenu, int menuSize, AtkValue* atkValueArgs);
        private Hook<ContextMenuOpenedDelegate_Unmanaged>? m_ContextMenuOpenedHook;
        private Hook<ContextMenuOpenedDelegate_Unmanaged>? m_SubContextMenuOpenedHook;

        private unsafe delegate bool ContextMenuItemSelectedDelegate_Unmanaged(AddonContextMenu* addonContextMenu, int selectedIndex, byte a3);
        private Hook<ContextMenuItemSelectedDelegate_Unmanaged>? m_ContextMenuItemSelectedHook;

        private unsafe delegate bool SubContextMenuOpeningDelegate_Unmanaged(AgentContext* agentContext);
        private Hook<SubContextMenuOpeningDelegate_Unmanaged>? m_SubContextMenuOpeningHook;

        private PluginAddressResolver m_PluginAddressResolver;

        private const int MaxContextMenuItemsPerContextMenu = 32;

        private unsafe AgentContextInterface* m_CurrentAgentContextInterface;
        private IntPtr m_CurrentSubContextMenuTitle;

        private OpenSubContextMenuItem? m_SelectedOpenSubContextMenuItem;
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
                unsafe
                {
                    m_ContextMenuOpeningHook = new Hook<ContextMenuOpeningDelegate_Unmanaged>(m_PluginAddressResolver.ContextMenuOpeningPtr.Value, ContextMenuOpeningDetour);
                }
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
                unsafe
                {
                    m_ContextMenuItemSelectedHook = new Hook<ContextMenuItemSelectedDelegate_Unmanaged>(m_PluginAddressResolver.ContextMenuItemSelectedPtr.Value, ContextMenuItemSelectedDetour);
                }
                m_ContextMenuItemSelectedHook?.Enable();
            }

            if (m_PluginAddressResolver.SubContextMenuOpeningPtr.HasValue)
            {
                unsafe
                {
                    m_SubContextMenuOpeningHook = new Hook<SubContextMenuOpeningDelegate_Unmanaged>(m_PluginAddressResolver.SubContextMenuOpeningPtr.Value, SubContextMenuOpeningDetour);
                }
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
        }

        public void Dispose()
        {
            m_SubContextMenuOpeningHook?.Disable();
            m_ContextMenuItemSelectedHook?.Disable();
            m_SubContextMenuOpenedHook?.Disable();
            m_ContextMenuOpenedHook?.Disable();
            m_ContextMenuOpeningHook?.Disable();
        }

        private unsafe IntPtr ContextMenuOpeningDetour(IntPtr a1, IntPtr a2, IntPtr a3, uint a4, IntPtr a5, AgentContextInterface* agentContextInterface, IntPtr a7, ushort a8)
        {
            m_CurrentAgentContextInterface = agentContextInterface;
            return m_ContextMenuOpeningHook!.Original(a1, a2, a3, a4, a5, agentContextInterface, a7, a8);
        }

        private unsafe bool ContextMenuOpenedDetour(AddonContextMenu* addonContextMenu, int atkValueCount, AtkValue* atkValues)
        {
            if (m_ContextMenuOpenedHook == null)
            {
                return false;
            }

            try
            {
                ContextMenuOpenedImplementation(addonContextMenu, ref atkValueCount, ref atkValues);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "ContextMenuOpenedDetour");
            }

            return m_ContextMenuOpenedHook.Original(addonContextMenu, atkValueCount, atkValues);
        }

        private unsafe void ContextMenuOpenedImplementation(AddonContextMenu* addonContextMenu, ref int atkValueCount, ref AtkValue* atkValues)
        {
            if (m_AtkValueChangeType == null
                || m_AtkValueSetString == null
                || ContextMenuOpened == null
                || m_CurrentAgentContextInterface == null)
            {
                return;
            }

            ContextMenuReaderWriter contextMenuReaderWriter = new ContextMenuReaderWriter(m_CurrentAgentContextInterface, atkValueCount, atkValues);

            // Check for a title.
            string? title = null;
            if (m_SelectedOpenSubContextMenuItem != null)
            {
                title = m_SelectedOpenSubContextMenuItem.Name.TextValue;

                // Write the custom title
                var titleAtkValue = &atkValues[1];
                fixed (byte* TtlePtr = m_SelectedOpenSubContextMenuItem.Name.Encode().NullTerminate())
                {
                    m_AtkValueSetString(titleAtkValue, TtlePtr);
                }
            }
            else if (contextMenuReaderWriter.Title != null)
            {
                title = contextMenuReaderWriter.Title.TextValue;
            }

            // Determine which event to raise.
            ContextMenuOpenedDelegate contextMenuOpenedDelegate = ContextMenuOpened;
            if (m_SelectedOpenSubContextMenuItem is OpenSubContextMenuItem openSubContextMenuItem)
            {
                contextMenuOpenedDelegate = openSubContextMenuItem.Opened;
            }

            // Get the existing items from the game.
            // TODO: For inventory sub context menus, we take only the last item -- the return item.
            // This is because we're doing a hack to spawn a Second Tier sub context menu and then appropriating it.
            var contextMenuItems = contextMenuReaderWriter.Read();
            if (IsInventoryContext(m_CurrentAgentContextInterface) && m_SelectedOpenSubContextMenuItem != null)
            {
                contextMenuItems = contextMenuItems.TakeLast(1).ToArray();
            }

            int beforeHashCode = GetContextMenuItemsHashCode(contextMenuItems);

            // Raise the event and get the context menu changes.
            m_CurrentContextMenuOpenedArgs = NotifyContextMenuOpened(addonContextMenu, m_CurrentAgentContextInterface, title, contextMenuOpenedDelegate, contextMenuItems);
            if (m_CurrentContextMenuOpenedArgs == null)
            {
                return;
            }

            int afterHashCode = GetContextMenuItemsHashCode(m_CurrentContextMenuOpenedArgs.Items);

            PluginLog.Warning($"{beforeHashCode}={afterHashCode}");

            // Only write to memory if the items were actually changed.
            if (beforeHashCode != afterHashCode)
            {
                // Write the new changes.
                contextMenuReaderWriter.Write(m_CurrentContextMenuOpenedArgs.Items, m_AtkValueChangeType, m_AtkValueSetString);

                // Update the addon.
                atkValueCount = *(&addonContextMenu->AtkValuesCount) = (ushort)contextMenuReaderWriter.AtkValueCount;
                atkValues = *(&addonContextMenu->AtkValues) = contextMenuReaderWriter.AtkValues;
            }
        }

        private unsafe bool SubContextMenuOpeningDetour(AgentContext* agentContext)
        {
            if (m_SubContextMenuOpeningHook == null)
            {
                return false;
            }

            if (SubContextMenuOpeningImplementation(agentContext))
            {
                return true;
            }

            return m_SubContextMenuOpeningHook.Original(agentContext);
        }

        private unsafe bool SubContextMenuOpeningImplementation(AgentContext* agentContext)
        {
            if (m_OpenSubContextMenu == null
                || m_AtkValueChangeType == null
                || m_AtkValueSetString == null
                || !(m_SelectedOpenSubContextMenuItem is OpenSubContextMenuItem))
            {
                return false;
            }

            // The important things to make this work are:
            // 1. Allocate a temporary sub context menu title. The value doesn't matter, we'll set it later.
            // 2. Context menu item count must equal 1 to tell the game there is enough space for the "< Return" item.
            // 3. Atk value count must equal the index of the first context menu item.
            //    This is enough to keep the base data, but excludes the context menu item data.
            //    We want to exclude context menu item data in this function because the game sometimes includes garbage items which can cause problems.
            //    After this function, the game adds the "< Return" item, and THEN we add our own items after that.

            m_OpenSubContextMenu(agentContext);

            // Allocate a new 1 byte title. This is required for the game to render the titled context menu style.
            // The actual value doesn't matter at this point, we'll set it later.
            GameInterfaceHelper.GameFree(ref m_CurrentSubContextMenuTitle, (ulong)IntPtr.Size);
            m_CurrentSubContextMenuTitle = GameInterfaceHelper.GameUIAllocate(1);
            *(&(&agentContext->AgentContextInterface)->SubContextMenuTitle) = (byte*)m_CurrentSubContextMenuTitle;
            *(byte*)m_CurrentSubContextMenuTitle = 0;

            // Expect at least 1 context menu item.
            (&agentContext->Items->AtkValues)[0].UInt = 1;

            // Expect a title. This isn't needed by the game, it's needed by ContextMenuReaderWriter which uses this to check if it's a context menu
            m_AtkValueChangeType(&(&agentContext->Items->AtkValues)[1], FFXIVClientStructs.FFXIV.Component.GUI.ValueType.String);
            (&agentContext->Items->AtkValues)[1].String = (byte*)0;

            ContextMenuReaderWriter contextMenuReaderWriter = new ContextMenuReaderWriter(&agentContext->AgentContextInterface, agentContext->Items->AtkValueCount, &agentContext->Items->AtkValues);
            *(&agentContext->Items->AtkValueCount) = (ushort)contextMenuReaderWriter.FirstContextMenuItemIndex;

            return true;
        }

        private unsafe bool SubContextMenuOpenedDetour(AddonContextMenu* addonContextMenu, int atkValueCount, AtkValue* atkValues)
        {
            if (m_SubContextMenuOpenedHook == null)
            {
                return false;
            }

            try
            {
                SubContextMenuOpenedImplementation(addonContextMenu, ref atkValueCount, ref atkValues);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "SubContextMenuOpenedDetour");
            }

            return m_SubContextMenuOpenedHook.Original(addonContextMenu, atkValueCount, atkValues);
        }

        private unsafe void SubContextMenuOpenedImplementation(AddonContextMenu* addonContextMenu, ref int atkValueCount, ref AtkValue* atkValues)
        {
            ContextMenuOpenedImplementation(addonContextMenu, ref atkValueCount, ref atkValues);
        }

        private int GetContextMenuItemsHashCode(IEnumerable<ContextMenuItem> contextMenuItems)
        {
            unchecked
            {
                int hash = 17;
                foreach (var item in contextMenuItems)
                {
                    hash = hash * 23 + item.GetHashCode();
                }
                return hash;
            }
        }

        private unsafe ContextMenuOpenedArgs? NotifyContextMenuOpened(AddonContextMenu* addonContextMenu, AgentContextInterface* agentContextInterface, string? title, ContextMenuOpenedDelegate contextMenuOpenedDelegate, IEnumerable<ContextMenuItem> initialContextMenuItems)
        {
            var parentAddonName = GetParentAddonName(&addonContextMenu->AddonInterface);

            InventoryItemContext? inventoryItemContext = null;
            GameObjectContext? gameObjectContext = null;
            if (IsInventoryContext(agentContextInterface))
            {
                var agentInventoryContext = (AgentInventoryContext*)agentContextInterface;
                inventoryItemContext = new InventoryItemContext(agentInventoryContext->InventoryItemId, agentInventoryContext->InventoryItemCount, agentInventoryContext->InventoryItemIsHighQuality);
            }
            else
            {
                var agentContext = (AgentContext*)agentContextInterface;

                uint? id = agentContext->GameObjectId;
                if (id == 0)
                {
                    id = null;
                }

                ulong? contentId = agentContext->GameObjectContentId;
                if (contentId == 0)
                {
                    contentId = null;
                }

                string? name;
                unsafe
                {
                    name = GameInterfaceHelper.ReadSeString((IntPtr)agentContext->GameObjectName.StringPtr).TextValue;
                    if (string.IsNullOrEmpty(name))
                    {
                        name = null;
                    }
                }

                ushort? worldId = agentContext->GameObjectWorldId;
                if (worldId == 0)
                {
                    worldId = null;
                }

                if (id != null
                    || contentId != null
                    || name != null
                    || worldId != null)
                {
                    gameObjectContext = new GameObjectContext(id, contentId, name, worldId);
                }
            }

            // Temporarily remove the < Return item, for UX we should enforce that it is always last in the list.
            var lastContextMenuItem = initialContextMenuItems.LastOrDefault();
            if (lastContextMenuItem is GameContextMenuItem gameContextMenuItem && gameContextMenuItem.SelectedAction == 102)
            {
                initialContextMenuItems = initialContextMenuItems.SkipLast(1);
            }

            var contextMenuOpenedArgs = new ContextMenuOpenedArgs((IntPtr)addonContextMenu, (IntPtr)agentContextInterface, parentAddonName, initialContextMenuItems)
            {
                Title = title,
                InventoryItemContext = inventoryItemContext,
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

            // Readd the < Return item
            if (lastContextMenuItem is GameContextMenuItem gameContextMenuItem1 && gameContextMenuItem1.SelectedAction == 102)
            {
                contextMenuOpenedArgs.Items.Add(lastContextMenuItem);
            }

            foreach (var contextMenuItem in contextMenuOpenedArgs.Items.ToArray())
            {
                // TODO: Game doesn't support nested sub context menus, but we might be able to.
                if (contextMenuItem is OpenSubContextMenuItem && contextMenuOpenedArgs.Title != null)
                {
                    contextMenuOpenedArgs.Items.Remove(contextMenuItem);
                    PluginLog.Warning($"Context menu '{contextMenuOpenedArgs.Title}' item '{contextMenuItem}' has been removed because nested sub context menus are not supported.");
                }                
            }

            if (contextMenuOpenedArgs.Items.Count > MaxContextMenuItemsPerContextMenu)
            {
                PluginLog.LogWarning($"Context menu requesting {contextMenuOpenedArgs.Items.Count} of max {MaxContextMenuItemsPerContextMenu} items. Resizing list to compensate.");
                contextMenuOpenedArgs.Items.RemoveRange(MaxContextMenuItemsPerContextMenu, contextMenuOpenedArgs.Items.Count - MaxContextMenuItemsPerContextMenu);
            }

            return contextMenuOpenedArgs;
        }

        private unsafe bool ContextMenuItemSelectedDetour(AddonContextMenu* addonContextMenu, int selectedIndex, byte a3)
        {
            if (m_ContextMenuItemSelectedHook == null)
            {
                return false;
            }

            try
            {
                ContextMenuItemSelectedImplementation(addonContextMenu, selectedIndex);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "ContextMenuItemSelectedDetour");
            }

            return m_ContextMenuItemSelectedHook.Original(addonContextMenu, selectedIndex, a3);
        }

        private unsafe void ContextMenuItemSelectedImplementation(AddonContextMenu* addonContextMenu, int selectedIndex)
        {
            if (m_CurrentContextMenuOpenedArgs == null || selectedIndex == -1)
            {
                m_CurrentContextMenuOpenedArgs = null;
                m_SelectedOpenSubContextMenuItem = null;
                return;
            }

            // Read the selected item directly from the game
            ContextMenuReaderWriter contextMenuReaderWriter = new ContextMenuReaderWriter(m_CurrentAgentContextInterface, addonContextMenu->AtkValuesCount, addonContextMenu->AtkValues);
            var gameContextMenuItems = contextMenuReaderWriter.Read();
            var gameSelectedItem = gameContextMenuItems.ElementAtOrDefault(selectedIndex);

            // This should be impossible
            if (gameSelectedItem == null)
            {
                m_CurrentContextMenuOpenedArgs = null;
                m_SelectedOpenSubContextMenuItem = null;
                return;
            }

            // Match it with the items we already know about based on its name.
            // We can get into a state where we have a game item we don't recognize when another plugin has added one.
            var selectedItem = m_CurrentContextMenuOpenedArgs.Items.FirstOrDefault(item => item.Name.Encode().SequenceEqual(gameSelectedItem.Name.Encode()));

            m_SelectedOpenSubContextMenuItem = null;
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
            else if (selectedItem is OpenSubContextMenuItem openSubContextMenuItem)
            {
                m_SelectedOpenSubContextMenuItem = openSubContextMenuItem;
            }

            m_CurrentContextMenuOpenedArgs = null;
        }

        private unsafe string? GetParentAddonName(AddonInterface* addonInterface)
        {
            if (m_GetAddonById == null)
            {
                return null;
            }

            var parentAddonId = addonInterface->ParentAddonId;
            if (parentAddonId == 0)
            {
                return null;
            }

            var atkStage = AtkStage.GetSingleton();
            var parentAddon = m_GetAddonById(atkStage->RaptureAtkUnitManager, parentAddonId);
            return GameInterfaceHelper.ReadString((IntPtr)(&parentAddon->Name));
        }

        private unsafe bool IsInventoryContext(AgentContextInterface* agentContextInterface)
        {
            if (agentContextInterface == AgentInventoryContext.Instance())
            {
                return true;
            }

            return false;
        }

        private unsafe AddonInterface* GetAddonFromAgent(AgentInterface* agentInterface)
        {
            if (m_GetAddonById == null)
            {
                return null;
            }

            if (agentInterface->AddonId == 0)
            {
                return null;
            }

            return m_GetAddonById(AtkStage.GetSingleton()->RaptureAtkUnitManager, (ushort)agentInterface->AddonId);
        }
    }
}
