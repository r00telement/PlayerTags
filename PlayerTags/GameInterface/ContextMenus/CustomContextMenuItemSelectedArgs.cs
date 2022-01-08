namespace PlayerTags.GameInterface.ContextMenus
{
    public class CustomContextMenuItemSelectedArgs
    {
        public ContextMenuOpenedArgs ContextMenuOpenedArgs { get; init; }

        public CustomContextMenuItem SelectedItem { get; init; }

        public CustomContextMenuItemSelectedArgs(ContextMenuOpenedArgs contextMenuOpenedArgs, CustomContextMenuItem selectedItem)
        {
            ContextMenuOpenedArgs = contextMenuOpenedArgs;
            SelectedItem = selectedItem;
        }
    }
}