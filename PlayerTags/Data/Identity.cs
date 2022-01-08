using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace PlayerTags.Data
{
    public class Identity : IComparable<Identity>, IEquatable<Identity>
    {
        public string Name { get; init; }
        public uint? WorldId { get; set; } = null;
        public List<Guid> CustomTagIds { get; init; } = new List<Guid>();

        [JsonIgnore]
        public string? WorldName => WorldHelper.GetWorldName(WorldId);

        public Identity(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            string str = Name;

            if (WorldId != null)
            {
                str += $"@{WorldName}";
            }

            return str;
        }

        public int CompareTo(Identity? other)
        {
            string? otherToString = null;
            if (!(other is null))
            {
                otherToString = other.ToString();
            }

            return ToString().CompareTo(otherToString);
        }

        public override bool Equals(object? obj)
        {
            return obj is Identity identity && Equals(identity);
        }

        public bool Equals(Identity? obj)
        {
            if (obj is null)
            {
                return false;
            }

            return this == obj;
        }

        public static bool operator ==(Identity? first, Identity? second)
        {
            if (ReferenceEquals(first, second))
            {
                return true;
            }

            if (first is null && second is null)
            {
                return true;
            }

            if (first is null || second is null)
            {
                return false;
            }

            bool areNamesEqual = first.Name.ToLower().Trim() == second.Name.ToLower().Trim();

            // If one of the worlds are null then it's technically equal as it could be promoted to the identity that does have a world
            bool areWorldsEqual = first.WorldId == null || second.WorldId == null || first.WorldId == second.WorldId;

            return areNamesEqual && areWorldsEqual;
        }

        public static bool operator !=(Identity? first, Identity? second)
        {
            return !(first == second);
        }

        public override int GetHashCode()
        {
            var hashCode = Name.GetHashCode();

            if (WorldName != null)
            {
                hashCode *= 17 ^ WorldName.GetHashCode();
            }

            return hashCode;
        }
    }
}
