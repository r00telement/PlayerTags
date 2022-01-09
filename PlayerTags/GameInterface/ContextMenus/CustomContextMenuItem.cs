using Dalamud.Game.Text.SeStringHandling;

namespace PlayerTags.GameInterface.ContextMenus
{
    /// <summary>
    /// An item in a context menu with a user defined action.
    /// </summary>
    public class CustomContextMenuItem : ContextMenuItem
    {
        /// <summary>
        /// The action that will be called when the item is selected.
        /// </summary>
        public CustomContextMenuItemSelectedDelegate ItemSelected { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomContextMenuItem"/> class.
        /// </summary>
        /// <param name="name">The name of the item.</param>
        /// <param name="itemSelected">The action that will be called when the item is selected.</param>
        internal CustomContextMenuItem(SeString name, CustomContextMenuItemSelectedDelegate itemSelected)
            : base(name)
        {
            ItemSelected = itemSelected;
        }
    }
}