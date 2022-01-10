using Dalamud.Game.Text.SeStringHandling;

namespace PlayerTags.GameInterface.ContextMenus
{
    /// <summary>
    /// Provides game object context to a context menu.
    /// </summary>
    public class GameObjectContext
    {
        /// <summary>
        /// The id of the game object.
        /// </summary>
        public uint Id { get; }

        /// <summary>
        /// The content id of the game object.
        /// </summary>
        public ulong ContentId { get; }

        /// <summary>
        /// The name of the game object.
        /// </summary>
        public SeString Name { get; }

        /// <summary>
        /// The world id of the game object.
        /// </summary>
        public ushort WorldId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameObjectContext"/> class.
        /// </summary>
        /// <param name="id">The id of the game object.</param>
        /// <param name="contentId">The lower content id of the game object.</param>
        /// <param name="name">The name of the game object.</param>
        /// <param name="worldId">The world id of the game object.</param>
        public GameObjectContext(uint id, ulong contentId, SeString name, ushort worldId)
        {
            Id = id;
            ContentId = contentId;
            Name = name;
            WorldId = worldId;
        }
    }
}
