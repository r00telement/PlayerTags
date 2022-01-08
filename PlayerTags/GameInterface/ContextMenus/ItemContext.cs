namespace PlayerTags.GameInterface.ContextMenus
{
    public class ItemContext
    {
        public uint Id { get; }

        public uint Count { get; }

        public bool IsHighQuality { get; }

        public ItemContext(uint id, uint count, bool isHighQuality)
        {
            Id = id;
            Count = count;
            IsHighQuality = isHighQuality;
        }
    }
}
