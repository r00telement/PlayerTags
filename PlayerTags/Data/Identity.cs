using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace PlayerTags.Data
{
    public class Identity : IComparable<Identity>
    {
        public string Name { get; init; }
        public uint? WorldId { get; set; } = null;
        public List<Guid> CustomTagIds { get; init; } = new List<Guid>();

        [JsonIgnore]
        public string? World => WorldHelper.GetWorldName(WorldId);

        public Identity(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            string str = Name;

            if (WorldId != null)
            {
                str += $"@{World}";
            }

            return str;
        }

        public int CompareTo(Identity? other)
        {
            return ToString().CompareTo(other != null ? other.ToString() : null);
        }
    }
}
