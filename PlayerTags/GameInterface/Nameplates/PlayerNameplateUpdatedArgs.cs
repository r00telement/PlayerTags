using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using Pilz.Dalamud.Nameplates.EventArgs;

namespace PlayerTags.GameInterface.Nameplates
{
    public class PlayerNameplateUpdatedArgs
    {
        private readonly AddonNamePlate_SetPlayerNameManagedEventArgs eventArgs;

        public PlayerCharacter PlayerCharacter { get; }

        public SeString Name
        {
            get => eventArgs.Name;
        }

        public SeString Title
        {
            get => eventArgs.Title;
        }

        public SeString FreeCompany
        {
            get => eventArgs.FreeCompany;
        }

        public bool IsTitleVisible
        {
            get => eventArgs.IsTitleVisible;
            set => eventArgs.IsTitleVisible = value;
        }

        public bool IsTitleAboveName
        {
            get => eventArgs.IsTitleAboveName;
            set => eventArgs.IsTitleAboveName = value;
        }

        public int IconId
        {
            get => eventArgs.IconID;
            set => eventArgs.IconID = value;
        }

        public PlayerNameplateUpdatedArgs(PlayerCharacter playerCharacter, AddonNamePlate_SetPlayerNameManagedEventArgs eventArgs)
        {
            PlayerCharacter = playerCharacter;
            this.eventArgs = eventArgs;
        }
    }
}
