using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Lumina.Excel.GeneratedSheets;
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
        public uint? WorldId;
        public string? Id;

        public string? World
        {
            get
            {
                var worldId = WorldId;
                if (worldId != null)
                {
                    var worlds = PluginServices.DataManager.GetExcelSheet<World>();
                    if (worlds != null)
                    {
                        var world = worlds.FirstOrDefault(world => world.RowId == worldId.Value);
                        if (world != null)
                        {
                            return world.Name.RawString;
                        }
                    }
                }

                return null;
            }
        }

        private static Regex s_WorldRegex = new Regex(@"@([a-zA-Z0-9]+)");
        private static Regex s_IdRegex = new Regex(@"@([a-zA-Z0-9]+)");

        public Identity(string name)
        {
            Name = name;
            WorldId = null;
            Id = null;
        }

        public static Identity From(string str)
        {
            var identity = new Identity();

            while (s_WorldRegex.Match(str) is Match match && match.Success)
            {
                if (uint.TryParse(match.Groups.Values.Last().Value, out var value))
                {
                    identity.WorldId = value;
                }

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
                WorldId = playerCharacter.HomeWorld.GameData.RowId
            };
        }

        public static Identity From(PartyMember partyMember)
        {
            return new Identity(partyMember.Name.TextValue)
            {
                WorldId = partyMember.World.GameData.RowId
            };
        }

        public static Identity From(PlayerPayload playerPayload)
        {
            return new Identity(playerPayload.PlayerName)
            {
                WorldId = playerPayload.World.RowId
            };
        }

        public override string ToString()
        {
            string str = Name;

            if (WorldId != null)
            {
                str += $"@{World}";
            }

            if (Id != null)
            {
                str += $":{Id}";
            }

            return str;
        }

        public string ToDataString()
        {
            string str = Name;

            if (WorldId != null)
            {
                str += $"@{WorldId}";
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

            bool areNamesEqual = first.Name.ToLower().Trim() == second.Name.ToLower().Trim();

            // If one of the worlds are null then it's technically equal as it could be promoted to the identity that does have a world
            bool areWorldsEqual = first.WorldId == null || second.WorldId == null || first.WorldId == second.WorldId;

            return areNamesEqual && areWorldsEqual;
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
