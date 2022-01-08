using Dalamud.Game.Text.SeStringHandling;

namespace PlayerTags.GameInterface.ContextMenus
{
    public class GameObjectContext
    {
        public uint Id { get; }

        public uint ContentIdLower { get; }

        public SeString? Name { get; }

        public ushort WorldId { get; }

        public GameObjectContext(uint id, uint contentIdLower, SeString name, ushort worldId)
        {
            Id = id;
            ContentIdLower = contentIdLower;
            Name = name;
            WorldId = worldId;
        }
    }
}
