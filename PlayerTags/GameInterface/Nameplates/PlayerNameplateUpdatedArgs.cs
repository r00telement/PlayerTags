using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;

namespace PlayerTags.GameInterface.Nameplates
{
    public class PlayerNameplateUpdatedArgs
    {
        public PlayerCharacter PlayerCharacter { get; }

        public SeString Name { get; }

        public SeString Title { get; }

        public SeString FreeCompany { get; }

        public bool IsTitleVisible { get; set; }

        public bool IsTitleAboveName { get; set; }

        public int IconId { get; set; }

        public PlayerNameplateUpdatedArgs(PlayerCharacter playerCharacter, SeString name, SeString title, SeString freeCompany, bool isTitleVisible, bool isTitleAboveName, int iconId)
        {
            PlayerCharacter = playerCharacter;
            Name = name;
            Title = title;
            FreeCompany = freeCompany;
            IsTitleVisible = isTitleVisible;
            IsTitleAboveName = isTitleAboveName;
            IconId = iconId;
        }
    }
}
