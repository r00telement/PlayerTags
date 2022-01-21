using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace PlayerTags.GameInterface.ContextMenus
{
    internal unsafe class ContextMenuReaderWriter
    {
        private enum SubContextMenuStructLayout
        {
            Main,
            Alternate
        }

        private AgentContextInterface* m_AgentContextInterface;

        private int m_AtkValueCount;
        public int AtkValueCount
        {
            get { return m_AtkValueCount; }
        }

        private AtkValue* m_AtkValues;
        public AtkValue* AtkValues
        {
            get { return m_AtkValues; }
        }

        public int ContextMenuItemCount
        {
            get { return m_AtkValues[0].Int; }
        }

        public bool HasTitle
        {
            get
            {
                bool isStringType =
                    (int)m_AtkValues[1].Type == 8
                    || (int)m_AtkValues[1].Type == 38
                    || m_AtkValues[1].Type == FFXIVClientStructs.FFXIV.Component.GUI.ValueType.String;

                return isStringType;
            }
        }

        public SeString? Title
        {
            get
            {
                if (HasTitle)
                {
                    GameInterfaceHelper.TryReadSeString((IntPtr)(&m_AtkValues[1])->String, out var str);
                    return str;
                }

                return null;
            }
        }

        public int HasPreviousIndicatorFlagsIndex
        {
            get
            {
                if (HasTitle)
                {
                    return 6;
                }

                return 2;
            }
        }

        public int HasNextIndicatorFlagsIndex
        {
            get
            {
                if (HasTitle)
                {
                    return 5;
                }

                return 3;
            }
        }

        public int FirstContextMenuItemIndex
        {
            get
            {
                if (HasTitle)
                {
                    return 8;
                }

                return 7;
            }
        }

        public int NameIndexOffset
        {
            get
            {
                if (HasTitle && StructLayout == SubContextMenuStructLayout.Alternate)
                {
                    return 1;
                }

                return 0;
            }
        }

        public int IsDisabledIndexOffset
        {
            get
            {
                if (HasTitle && StructLayout == SubContextMenuStructLayout.Alternate)
                {
                    return 2;
                }

                return ContextMenuItemCount;
            }
        }

        /// <summary>
        /// 0x14000000 | action
        /// </summary>
        public int? MaskedActionIndexOffset
        {
            get
            {
                if (HasTitle && StructLayout == SubContextMenuStructLayout.Alternate)
                {
                    return 3;
                }

                return null;
            }
        }

        public int SequentialAtkValuesPerContextMenuItem
        {
            get
            {
                if (HasTitle && StructLayout == SubContextMenuStructLayout.Alternate)
                {
                    return 4;
                }

                return 1;
            }
        }

        public int TotalDesiredAtkValuesPerContextMenuItem
        {
            get
            {
                if (HasTitle && StructLayout == SubContextMenuStructLayout.Alternate)
                {
                    return 4;
                }

                return 2;
            }
        }

        public Vector2? Position
        {
            get
            {
                if (HasTitle)
                {
                    return new Vector2(m_AtkValues[2].Int, m_AtkValues[3].Int);
                }

                return null;
            }
        }

        public unsafe bool IsInventoryContext
        {
            get
            {
                if ((IntPtr)m_AgentContextInterface == (IntPtr)AgentInventoryContext.Instance())
                {
                    return true;
                }

                return false;
            }
        }

        private SubContextMenuStructLayout? StructLayout
        {
            get
            {
                if (HasTitle)
                {
                    if (m_AtkValues[7].Int == 8)
                    {
                        return SubContextMenuStructLayout.Alternate;
                    }
                    else if (m_AtkValues[7].Int == 1)
                    {
                        return SubContextMenuStructLayout.Main;
                    }
                }

                return null;
            }
        }

        public byte NoopAction
        {
            get
            {
                if (IsInventoryContext)
                {
                    return 0xff;
                }
                else
                {
                    return 0x67;
                }
            }
        }

        public byte OpenSubContextMenuAction
        {
            get
            {
                if (IsInventoryContext)
                {
                    // This is actually the action to open the Second Tier context menu and we just hack around it
                    return 0x31;
                }
                else
                {
                    return 0x66;
                }
            }
        }

        public byte? FirstUnhandledAction
        {
            get
            {
                if (StructLayout != null && StructLayout == SubContextMenuStructLayout.Alternate)
                {
                    return 0x68;
                }

                return null;
            }
        }

        public ContextMenuReaderWriter(AgentContextInterface* agentContextInterface, int atkValueCount, AtkValue* atkValues)
        {
            m_AgentContextInterface = agentContextInterface;
            m_AtkValueCount = atkValueCount;
            m_AtkValues = atkValues;
        }

        public GameContextMenuItem[] Read()
        {
            List<GameContextMenuItem> gameContextMenuItems = new List<GameContextMenuItem>();
            for (var contextMenuItemIndex = 0; contextMenuItemIndex < ContextMenuItemCount; contextMenuItemIndex++)
            {
                var contextMenuItemAtkValueBaseIndex = FirstContextMenuItemIndex + (contextMenuItemIndex * SequentialAtkValuesPerContextMenuItem);

                // Get the name
                var nameAtkValue = &m_AtkValues[contextMenuItemAtkValueBaseIndex + NameIndexOffset];
                if (nameAtkValue->Type == 0)
                {
                    continue;
                }
                var name = GameInterfaceHelper.ReadSeString((IntPtr)nameAtkValue->String);

                // Get the enabled state. Note that SE stores this as IsDisabled, NOT IsEnabled (those heathens)
                var isEnabled = true;
                bool isDisabledDefined = FirstContextMenuItemIndex + ContextMenuItemCount < AtkValueCount;
                if (isDisabledDefined)
                {
                    var isDisabledAtkValue = &m_AtkValues[contextMenuItemAtkValueBaseIndex + IsDisabledIndexOffset];
                    isEnabled = isDisabledAtkValue->Int == 0;
                }

                // Get the action
                byte action = 0;
                if (IsInventoryContext)
                {
                    var actions = &((AgentInventoryContext*)m_AgentContextInterface)->Actions;
                    action = *(actions + contextMenuItemAtkValueBaseIndex);
                }
                else if (StructLayout != null && StructLayout.Value == SubContextMenuStructLayout.Alternate)
                {
                    var redButtonActions = &((AgentContext*)m_AgentContextInterface)->Items->RedButtonActions;
                    action = (byte)*(redButtonActions + contextMenuItemIndex);
                }
                else
                {
                    var actions = &((AgentContext*)m_AgentContextInterface)->Items->Actions;
                    action = *(actions + contextMenuItemAtkValueBaseIndex);
                }                

                // Get the has previous indicator flag
                var hasPreviousIndicatorFlagsAtkValue = &m_AtkValues[HasPreviousIndicatorFlagsIndex];
                var hasPreviousIndicator = HasFlag(hasPreviousIndicatorFlagsAtkValue->UInt, contextMenuItemIndex);

                // Get the has next indicator flag
                var hasNextIndicatorlagsAtkValue = &m_AtkValues[HasNextIndicatorFlagsIndex];
                var hasNextIndicator = HasFlag(hasNextIndicatorlagsAtkValue->UInt, contextMenuItemIndex);

                ContextMenuItemIndicator indicator = ContextMenuItemIndicator.None;
                if (hasPreviousIndicator)
                {
                    indicator = ContextMenuItemIndicator.Previous;
                }
                else if (hasNextIndicator)
                {
                    indicator = ContextMenuItemIndicator.Next;
                }

                var gameContextMenuItem = new GameContextMenuItem(name, action)
                {
                    Agent = (IntPtr)m_AgentContextInterface,
                    IsEnabled = isEnabled,
                    Indicator = indicator
                };

                gameContextMenuItems.Add(gameContextMenuItem);
            }

            return gameContextMenuItems.ToArray();
        }

        public unsafe void Write(ContextMenuOpenedArgs contextMenuOpenedArgs, ContextMenuItem? selectedContextMenuItem, AtkValueChangeTypeDelegate_Unmanaged atkValueChangeType, AtkValueSetStringDelegate_Unmanaged atkValueSetString, bool allowReallocate = true)
        {
            if (allowReallocate)
            {
                var newAtkValuesCount = FirstContextMenuItemIndex + (contextMenuOpenedArgs.Items.Count() * TotalDesiredAtkValuesPerContextMenuItem);

                // Allocate the new array. We have to do a little dance with the first 8 bytes which represents the array count
                const int arrayCountSize = 8;
                var newAtkValuesArraySize = arrayCountSize + Marshal.SizeOf<AtkValue>() * newAtkValuesCount;
                var newAtkValuesArray = GameInterfaceHelper.GameUIAllocate((ulong)newAtkValuesArraySize);
                if (newAtkValuesArray == IntPtr.Zero)
                {
                    return;
                }

                var newAtkValues = (AtkValue*)(newAtkValuesArray + arrayCountSize);

                // Zero the memory, then copy the atk values up to the first context menu item atk value
                Marshal.Copy(new byte[newAtkValuesArraySize], 0, newAtkValuesArray, newAtkValuesArraySize);
                Buffer.MemoryCopy(m_AtkValues, newAtkValues, newAtkValuesArraySize - arrayCountSize, (long)sizeof(AtkValue) * FirstContextMenuItemIndex);

                // Free the old array
                IntPtr oldArray = (IntPtr)m_AtkValues - arrayCountSize;
                ulong oldArrayCount = *(ulong*)oldArray;
                ulong oldArraySize = arrayCountSize + ((ulong)sizeof(AtkValue) * oldArrayCount);
                GameInterfaceHelper.GameFree(ref oldArray, oldArraySize);

                // Set the array count
                *(ulong*)newAtkValuesArray = (ulong)newAtkValuesCount;

                m_AtkValueCount = newAtkValuesCount;
                m_AtkValues = newAtkValues;
            }

            // Set the custom title if appropriate
            if (selectedContextMenuItem is OpenSubContextMenuItem)
            {
                var titleAtkValue = &m_AtkValues[1];
                fixed (byte* TtlePtr = selectedContextMenuItem.Name.Encode().NullTerminate())
                {
                    atkValueSetString(titleAtkValue, TtlePtr);
                }
            }

            // Set the context menu item count
            const int contextMenuItemCountAtkValueIndex = 0;
            var contextMenuItemCountAtkValue = &m_AtkValues[contextMenuItemCountAtkValueIndex];
            contextMenuItemCountAtkValue->UInt = (uint)contextMenuOpenedArgs.Items.Count();

            // Clear the previous arrow flags
            var hasPreviousIndicatorAtkValue = &m_AtkValues[HasPreviousIndicatorFlagsIndex];
            hasPreviousIndicatorAtkValue->UInt = 0;

            // Clear the next arrow flags
            var hasNextIndiactorFlagsAtkValue = &m_AtkValues[HasNextIndicatorFlagsIndex];
            hasNextIndiactorFlagsAtkValue->UInt = 0;

            for (int contextMenuItemIndex = 0; contextMenuItemIndex < contextMenuOpenedArgs.Items.Count(); ++contextMenuItemIndex)
            {
                var contextMenuItem = contextMenuOpenedArgs.Items.ElementAt(contextMenuItemIndex);

                var contextMenuItemAtkValueBaseIndex = FirstContextMenuItemIndex + (contextMenuItemIndex * SequentialAtkValuesPerContextMenuItem);

                // Set the name
                var nameAtkValue = &m_AtkValues[contextMenuItemAtkValueBaseIndex + NameIndexOffset];
                atkValueChangeType(nameAtkValue, FFXIVClientStructs.FFXIV.Component.GUI.ValueType.String);
                fixed (byte* nameBytesPtr = contextMenuItem.Name.Encode().NullTerminate())
                {
                    atkValueSetString(nameAtkValue, nameBytesPtr);
                }

                // Set the enabled state. Note that SE stores this as IsDisabled, NOT IsEnabled (those heathens)
                var disabledAtkValue = &m_AtkValues[contextMenuItemAtkValueBaseIndex + IsDisabledIndexOffset];
                atkValueChangeType(disabledAtkValue, FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int);
                disabledAtkValue->Int = contextMenuItem.IsEnabled ? 0 : 1;

                // Set the action
                byte action = 0;
                if (contextMenuItem is GameContextMenuItem gameContextMenuItem)
                {
                    action = gameContextMenuItem.SelectedAction;
                }
                else if (contextMenuItem is CustomContextMenuItem customContextMenuItem)
                {
                    action = NoopAction;
                }
                else if (contextMenuItem is OpenSubContextMenuItem openSubContextMenuItem)
                {
                    action = OpenSubContextMenuAction;
                }

                if (IsInventoryContext)
                {
                    var actions = &((AgentInventoryContext*)m_AgentContextInterface)->Actions;
                    *(actions + FirstContextMenuItemIndex + contextMenuItemIndex) = action;
                }
                else if (StructLayout != null && StructLayout.Value == SubContextMenuStructLayout.Alternate && FirstUnhandledAction != null)
                {
                    // Some weird placeholder goes here
                    var actions = &((AgentContext*)m_AgentContextInterface)->Items->Actions;
                    *(actions + FirstContextMenuItemIndex + contextMenuItemIndex) = (byte)(FirstUnhandledAction.Value + contextMenuItemIndex);

                    // Make sure there's one of these function pointers for every item.
                    // The function needs to be the same, so we just copy the first one into every index.
                    var unkFunctionPointers = &((AgentContext*)m_AgentContextInterface)->Items->UnkFunctionPointers;
                    *(unkFunctionPointers + FirstContextMenuItemIndex + contextMenuItemIndex) = *(unkFunctionPointers + FirstContextMenuItemIndex);

                    // The real action goes here
                    var redButtonActions = &((AgentContext*)m_AgentContextInterface)->Items->RedButtonActions;
                    *(redButtonActions + contextMenuItemIndex) = action;
                }
                else
                {
                    var actions = &((AgentContext*)m_AgentContextInterface)->Items->Actions;
                    *(actions + FirstContextMenuItemIndex + contextMenuItemIndex) = action;
                }

                if (contextMenuItem.Indicator == ContextMenuItemIndicator.Previous)
                {
                    SetFlag(ref hasPreviousIndicatorAtkValue->UInt, contextMenuItemIndex, true);
                }
                else if (contextMenuItem.Indicator == ContextMenuItemIndicator.Next)
                {
                    SetFlag(ref hasNextIndiactorFlagsAtkValue->UInt, contextMenuItemIndex, true);
                }
            }
        }

        private bool HasFlag(uint mask, int itemIndex)
        {
            return (mask & (1 << itemIndex)) > 0;
        }

        private void SetFlag(ref uint mask, int itemIndex, bool value)
        {
            mask &= ~((uint)1 << itemIndex);

            if (value)
            {
                mask |= (uint)(1 << itemIndex);
            }
        }

        public void Log()
        {
            Log(m_AtkValueCount, m_AtkValues);
        }

        public static void Log(int atkValueCount, AtkValue* atkValues)
        {
            PluginLog.Debug($"ContextMenuReader.Log");

            for (int atkValueIndex = 0; atkValueIndex < atkValueCount; ++atkValueIndex)
            {
                var atkValue = &atkValues[atkValueIndex];

                object? value = null;
                if (atkValue->Type == FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int)
                {
                    value = $"{atkValue->Int:X}";
                }
                else if (atkValue->Type == FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Bool)
                {
                    value = atkValue->Byte;
                }
                else if (atkValue->Type == FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt)
                {
                    value = atkValue->UInt;
                }
                else if (atkValue->Type == FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Float)
                {
                    value = atkValue->Float;
                }
                else if (atkValue->Type == FFXIVClientStructs.FFXIV.Component.GUI.ValueType.String
                    || (int)atkValue->Type == 38
                    || (int)atkValue->Type == 8)
                {
                    if (GameInterfaceHelper.TryReadSeString((IntPtr)atkValue->String, out var str))
                    {
                        value = str;
                    }
                }
                else
                {
                    value = $"{(IntPtr)atkValue->String:X}";
                }

                PluginLog.Debug($"atkValues[{atkValueIndex}]={(IntPtr)atkValue:X}   {atkValue->Type}={value}");
            }
        }
    }
}
