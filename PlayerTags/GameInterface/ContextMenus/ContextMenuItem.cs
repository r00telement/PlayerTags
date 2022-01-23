using Dalamud.Game.Text.SeStringHandling;
using System;
using System.Numerics;

namespace PlayerTags.GameInterface.ContextMenus
{
    /// <summary>
    /// An item in a context menu.
    /// </summary>
    public abstract class ContextMenuItem
    {
        /// <summary>
        /// The name of the item.
        /// </summary>
        public SeString Name { get; set; }

        /// <summary>
        /// Whether the item is enabled. When enabled, an item is selectable.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// The indicator of the item.
        /// </summary>
        public ContextMenuItemIndicator Indicator { get; set; } = ContextMenuItemIndicator.None;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextMenuItem"/> class.
        /// </summary>
        /// <param name="name">The name of the item.</param>
        public ContextMenuItem(SeString name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name.ToString();
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + new BigInteger(Name.Encode()).GetHashCode();
                hash = hash * 23 + IsEnabled.GetHashCode();
                hash = hash * 23 + ((int)Indicator).GetHashCode();
                return hash;
            }
        }
    }
}
