using Dalamud.Game.Text.SeStringHandling;
using System;
using System.Collections.Generic;

namespace PlayerTags.GameInterface.ContextMenus
{
    /// <summary>
    /// Provides data for <see cref="ContextMenuOpenedDelegate"/> methods.
    /// </summary>
    public class ContextMenuOpenedArgs
    {
        /// <summary>
        /// The addon associated with the context menu.
        /// </summary>
        public IntPtr Addon { get; }

        /// <summary>
        /// The agent associated with the context menu.
        /// </summary>
        public IntPtr Agent { get; }

        /// <summary>
        /// The the name of the parent addon associated with the context menu.
        /// </summary>
        public string? ParentAddonName { get; }

        /// <summary>
        /// The items in the context menu.
        /// </summary>
        public List<ContextMenuItem> ContextMenuItems { get; }

        /// <summary>
        /// The game object context associated with the context menu.
        /// </summary>
        public GameObjectContext? GameObjectContext { get; init; }

        /// <summary>
        /// The item context associated with the context menu.
        /// </summary>
        public ItemContext? ItemContext { get; init; }

        public ContextMenuOpenedArgs(IntPtr addon, IntPtr agent, string? parentAddonName, IEnumerable<ContextMenuItem> contextMenuItems)
        {
            Addon = addon;
            Agent = agent;
            ParentAddonName = parentAddonName;
            ContextMenuItems = new List<ContextMenuItem>(contextMenuItems);
        }
    }
}
