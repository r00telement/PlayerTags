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

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextMenuOpenedArgs"/> class.
        /// </summary>
        /// <param name="addon">The addon associated with the context menu.</param>
        /// <param name="agent">The agent associated with the context menu.</param>
        /// <param name="parentAddonName">The the name of the parent addon associated with the context menu.</param>
        /// <param name="contextMenuItems">The items in the context menu.</param>
        public ContextMenuOpenedArgs(IntPtr addon, IntPtr agent, string? parentAddonName, IEnumerable<ContextMenuItem> contextMenuItems)
        {
            Addon = addon;
            Agent = agent;
            ParentAddonName = parentAddonName;
            ContextMenuItems = new List<ContextMenuItem>(contextMenuItems);
        }
    }
}
