using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayerTags.Data
{
    [Serializable]
    public class Identity : IComparable<Identity>
    {
        public string Name;
        public uint? WorldId;

        public List<Guid> CustomTagIds = new List<Guid>();

        [JsonIgnore]
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

        public Identity(string name)
        {
            Name = name;
            WorldId = null;
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
            string? otherName = null;
            if (other != null)
            {
                otherName = other.Name;
            }

            return Name.CompareTo(otherName);
        }
    }
}
