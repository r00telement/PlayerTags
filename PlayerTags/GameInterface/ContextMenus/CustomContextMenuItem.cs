using Dalamud.Game.Text.SeStringHandling;

namespace PlayerTags.GameInterface.ContextMenus
{
    public class CustomContextMenuItem : ContextMenuItem
    {
        public CustomContextMenuItemSelectedDelegate ItemSelected { get; }

        internal CustomContextMenuItem(SeString name, CustomContextMenuItemSelectedDelegate itemSelected)
            : base(name)
        {
            ItemSelected = itemSelected;
        }
    }
}