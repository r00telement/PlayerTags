using Lumina.Excel.GeneratedSheets;
using Pilz.Dalamud.ActivityContexts;
using Pilz.Dalamud.Nameplates.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pilz.Dalamud.Nameplates.Tools
{
    public class StatusIconPriorizer
    {
        private static StatusIconPriorizerSettings DefaultSettings { get; } = new();
        public StatusIconPriorizerSettings Settings { get; init; }

        public StatusIconPriorizer() : this(DefaultSettings)
        {
        }

        public StatusIconPriorizer(StatusIconPriorizerSettings settings)
        {
            Settings = settings;
        }

        /// <summary>
        /// Check for an icon that should take priority over the job icon,
        /// taking into account whether or not the player is in a duty.
        /// </summary>
        /// <param name="iconId">The incoming icon id that is being overwritten by the plugin.</param>
        /// <param name="priorityIconId">The icon id that should be used.</param>
        /// <returns>Whether a priority icon was found.</returns>
        public bool IsPriorityIcon(int iconId, ActivityContext activityContext)
        {
            bool isPrioIcon;

            if (!Settings.UsePriorizedIcons && iconId != (int)StatusIcons.Disconnecting && iconId != (int)StatusIcons.Disconnecting + 50)
                isPrioIcon = false;
            else
            {
                // Select which set of priority icons to use based on whether we're in a duty
                // In the future, there can be a third list used when in combat
                var priorityIcons = GetPriorityIcons(activityContext);

                // Determine whether the incoming icon should take priority over the job icon
                // Check the id plus 50 as that's an alternately sized version
                isPrioIcon = priorityIcons.Contains(iconId) || priorityIcons.Contains(iconId + 50);
            }

            return isPrioIcon;
        }

        private IEnumerable<int> GetPriorityIcons(ActivityContext activityContext)
        {
            StatusIconPriorizerConditionSets set;

            if (activityContext.ZoneType == ZoneType.Foray)
                set = StatusIconPriorizerConditionSets.InForay;
            else if (activityContext.IsInDuty)
                set = StatusIconPriorizerConditionSets.InDuty;
            else
                set = StatusIconPriorizerConditionSets.Overworld;

            return Settings.GetConditionSet(set).Select(n => (int)n);
        }
    }
}
