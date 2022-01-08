using Dalamud.Game.Text.SeStringHandling;
using System;

namespace PlayerTags.GameInterface.ContextMenus
{
    public abstract class ContextMenuItem
    {
        public SeString Name { get; set; }

        public bool IsEnabled { get; set; } = true;

        public bool HasPreviousArrow { get; set; } = false;

        public bool HasNextArrow { get; set; } = false;

        internal IntPtr Agent { get; set; }

        public ContextMenuItem(SeString name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
