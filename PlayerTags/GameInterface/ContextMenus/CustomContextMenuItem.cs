using Dalamud.Game.Text.SeStringHandling;

namespace PlayerTags.GameInterface.ContextMenus
{
    public class CustomContextMenuItem : ContextMenuItem
    {
        public CustomContextMenuItemSelectedDelegate CustomAction { get; }

        internal CustomContextMenuItem(SeString name, CustomContextMenuItemSelectedDelegate customAction)
            : base(name)
        {
            CustomAction = customAction;
        }
    }
}