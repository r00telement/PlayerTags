namespace PlayerTags.GameInterface.ContextMenus
{
    /// <summary>
    /// Provides item context to a context menu.
    /// </summary>
    public class ItemContext
    {
        /// <summary>
        /// The id of the item.
        /// </summary>
        public uint Id { get; }

        /// <summary>
        /// The count of the item in the stack.
        /// </summary>
        public uint Count { get; }

        /// <summary>
        /// Whether the item is high quality.
        /// </summary>
        public bool IsHighQuality { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemContext"/> class.
        /// </summary>
        /// <param name="id">The id of the item.</param>
        /// <param name="count">The count of the item in the stack.</param>
        /// <param name="isHighQuality">Whether the item is high quality.</param>
        public ItemContext(uint id, uint count, bool isHighQuality)
        {
            Id = id;
            Count = count;
            IsHighQuality = isHighQuality;
        }
    }
}
