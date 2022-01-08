using Dalamud.Game.Text.SeStringHandling;
using System;
using System.Collections.Generic;

namespace PlayerTags.GameInterface.ContextMenus
{
    public class ContextMenuOpenedArgs
    {
        public IntPtr Addon { get; }

        public IntPtr Agent { get; }

        public string? ParentAddonName { get; }

        public List<ContextMenuItem> ContextMenuItems { get; }

        public ContextMenuItem? SelectedItem { get; init; }

        public GameObjectContext? GameObjectContext { get; init; }

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
