using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace PlayerTags.Data
{
    // FirstName LastName
    // FirstName LastName@World
    // FirstName LastName:Id
    // FirstName LastName@World:Id
    public struct Identity : IComparable<Identity>, IEquatable<Identity>
    {
        public string Name;
        public string? World;
        public string? Id;

        public Identity(string name)
        {
            Name = name;
            World = null;
            Id = null;
        }

        private static Regex s_WorldRegex = new Regex(@"@([a-zA-Z0-9]+)");
        private static Regex s_IdRegex = new Regex(@"@([a-zA-Z0-9]+)");

        public static Identity From(string str)
        {
            var identity = new Identity();

            while (s_WorldRegex.Match(str) is Match match && match.Success)
            {
                identity.World = match.Groups.Values.Last().Value;
                str = str.Replace(match.Value, "");
            }

            while (s_IdRegex.Match(str) is Match match && match.Success)
            {
                identity.Id = match.Groups.Values.Last().Value;
                str = str.Replace(match.Value, "");
            }

            identity.Name = str;

            return identity;
        }

        public static Identity From(PlayerCharacter playerCharacter)
        {
            return new Identity(playerCharacter.Name.TextValue)
            {
                World = playerCharacter.HomeWorld.GameData.Name.RawString
            };
        }

        public static Identity From(PartyMember partyMember)
        {
            return new Identity(partyMember.Name.TextValue)
            {
                World = partyMember.World.GameData.Name.RawString
            };
        }

        public override string ToString()
        {
            string str = Name;

            if (World != null)
            {
                str += $"@{World}";
            }

            if (Id != null)
            {
                str += $":{Id}";
            }

            return str;
        }

        public override bool Equals(object? obj)
        {
            return obj is Identity identity && Equals(identity);
        }

        public bool Equals(Identity obj)
        {
            return this == obj;
        }

        public static bool operator ==(Identity first, Identity second)
        {
            if (first.Id != null || second.Id != null)
            {
                return first.Id == second.Id;
            }

            return first.Name.ToLower().Trim() == second.Name.ToLower().Trim();
        }

        public static bool operator !=(Identity first, Identity second)
        {
            return !(first == second);
        }

        public override int GetHashCode()
        {
            var hashCode = Name.GetHashCode();

            if (Id != null)
            {
                hashCode *= 17 ^ Id.GetHashCode();
            }

            return hashCode;
        }

        public int CompareTo(Identity other)
        {
            return Name.CompareTo(other.Name);
        }
    }
}
