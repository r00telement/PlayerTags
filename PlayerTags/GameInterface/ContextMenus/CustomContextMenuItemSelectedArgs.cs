namespace PlayerTags.GameInterface.ContextMenus
{
    /// <summary>
    /// Provides data for <see cref="CustomContextMenuItemSelectedDelegate"/> events.
    /// </summary>
    public class CustomContextMenuItemSelectedArgs
    {
        /// <summary>
        /// The currently opened context menu.
        /// </summary>
        public ContextMenuOpenedArgs ContextMenuOpenedArgs { get; init; }

        /// <summary>
        /// The selected item within the currently opened context menu.
        /// </summary>
        public CustomContextMenuItem SelectedItem { get; init; }

        public CustomContextMenuItemSelectedArgs(ContextMenuOpenedArgs contextMenuOpenedArgs, CustomContextMenuItem selectedItem)
        {
            ContextMenuOpenedArgs = contextMenuOpenedArgs;
            SelectedItem = selectedItem;
        }
    }
}