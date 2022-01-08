using Dalamud.Game.Text.SeStringHandling;

namespace PlayerTags.GameInterface.ContextMenus
{
    public class OpenSubContextMenuItem : ContextMenuItem
    {
        public ContextMenuOpenedDelegate OpenedAction { get; set; }

        internal OpenSubContextMenuItem(SeString name, ContextMenuOpenedDelegate openedAction)
            : base(name)
        {
            OpenedAction = openedAction;
            HasNextArrow = true;
        }
    }
}