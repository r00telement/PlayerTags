using Lumina.Excel.GeneratedSheets;
using System.Collections.Generic;

namespace PlayerTags.Data
{
    public static class WorldHelper
    {
        private static Dictionary<uint, string>? s_WorldNames = null;
        public static Dictionary<uint, string> WorldNames
        {
            get
            {
                if (s_WorldNames == null)
                {
                    s_WorldNames = new Dictionary<uint, string>();

                    var worlds = PluginServices.DataManager.GetExcelSheet<World>();
                    if (worlds != null)
                    {
                        foreach (var world in worlds)
                        {
                            s_WorldNames[world.RowId] = world.Name;
                        }
                    }
                }

                return s_WorldNames;
            }
        }

        public static string? GetWorldName(uint? worldId)
        {
            if (worldId != null && WorldNames.TryGetValue(worldId.Value, out var name))
            {
                return name;
            }

            return null;
        }
    }
}
