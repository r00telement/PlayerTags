using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;

namespace PlayerTags.Data
{
    public static class PlayerContextHelper
    {
        public static PlayerContext GetPlayerContext(PlayerCharacter playerCharacter)
        {
            PlayerContext playerContext = PlayerContext.None;

            if (PluginServices.ClientState.LocalPlayer == playerCharacter)
            {
                playerContext |= PlayerContext.Self;
            }

            if (playerCharacter.StatusFlags.HasFlag(StatusFlags.Friend))
            {
                playerContext |= PlayerContext.Friend;
            }

            if (playerCharacter.StatusFlags.HasFlag(StatusFlags.PartyMember))
            {
                playerContext |= PlayerContext.Party;
            }

            if (playerCharacter.StatusFlags.HasFlag(StatusFlags.AllianceMember))
            {
                playerContext |= PlayerContext.Alliance;
            }

            if (playerCharacter.StatusFlags.HasFlag(StatusFlags.Hostile))
            {
                playerContext |= PlayerContext.Enemy;
            }

            return playerContext;
        }

        public static bool GetIsVisible(PlayerContext playerContext, bool desiredSelfVisibility, bool desiredFriendsVisibility, bool desiredPartyVisibility, bool desiredAllianceVisibility, bool desiredEnemiesVisibility, bool desiredOthersVisibility)
        {
            if (playerContext.HasFlag(PlayerContext.Self))
            {
                return desiredSelfVisibility;
            }

            bool isVisible = false;
            if (playerContext.HasFlag(PlayerContext.Friend))
            {
                isVisible |= desiredFriendsVisibility;
            }

            if (playerContext.HasFlag(PlayerContext.Party))
            {
                isVisible |= desiredPartyVisibility;
            }

            if (!playerContext.HasFlag(PlayerContext.Party) && playerContext.HasFlag(PlayerContext.Alliance))
            {
                isVisible |= desiredAllianceVisibility;
            }

            if (playerContext.HasFlag(PlayerContext.Enemy))
            {
                isVisible |= desiredEnemiesVisibility;
            }

            if (playerContext == PlayerContext.None)
            {
                isVisible |= desiredOthersVisibility;
            }

            return isVisible;
        }

        public static bool GetIsVisible(PlayerCharacter playerCharacter, bool desiredSelfVisibility, bool desiredFriendsVisibility, bool desiredPartyVisibility, bool desiredAllianceVisibility, bool desiredEnemiesVisibility, bool desiredOthersVisibility)
        {
            return GetIsVisible(GetPlayerContext(playerCharacter), desiredSelfVisibility, desiredFriendsVisibility, desiredPartyVisibility, desiredAllianceVisibility, desiredEnemiesVisibility, desiredOthersVisibility);
        }
    }
}
