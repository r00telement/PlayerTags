using Newtonsoft.Json;
using Pilz.Dalamud.ActivityContexts;
using Pilz.Dalamud.Nameplates.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pilz.Dalamud.Nameplates.Tools
{
    public class StatusIconPriorizerSettings
    {
        [JsonProperty("IconConditionSets")]
        private Dictionary<StatusIconPriorizerConditionSets, List<StatusIcons>> iconConditionSets = new();
        public bool UsePriorizedIcons { get; set; } = true;

        [JsonConstructor]
        private StatusIconPriorizerSettings(JsonConstructorAttribute dummy)
        {
        }

        public StatusIconPriorizerSettings() : this(false)
        {
        }

        public StatusIconPriorizerSettings(bool fillWithDefaultSettings)
        {
            foreach (StatusIconPriorizerConditionSets set in Enum.GetValues(typeof(StatusIconPriorizerConditionSets)))
                iconConditionSets.Add(set, new List<StatusIcons>());

            if (fillWithDefaultSettings)
                FillWithDefaultSettings();
        }

        public List<StatusIcons> GetConditionSet(StatusIconPriorizerConditionSets set)
        {
            return iconConditionSets[set];
        }

        public void ResetToEmpty()
        {
            foreach (var kvp in iconConditionSets)
                kvp.Value.Clear();
        }

        public void ResetToDefault()
        {
            ResetToEmpty();
            FillWithDefaultSettings();
        }

        private void FillWithDefaultSettings()
        {
            var setOverworld = GetConditionSet(StatusIconPriorizerConditionSets.Overworld);
            setOverworld.AddRange(new[]
            {
                StatusIcons.Disconnecting, // Disconnecting
                StatusIcons.InDuty, // In Duty
                StatusIcons.ViewingCutscene, // Viewing Cutscene
                StatusIcons.Busy, // Busy
                StatusIcons.Idle, // Idle
                StatusIcons.DutyFinder, // Duty Finder
                StatusIcons.PartyLeader, // Party Leader
                StatusIcons.PartyMember, // Party Member
                StatusIcons.RolePlaying, // Role Playing
                StatusIcons.GroupPose, // Group Pose
                StatusIcons.Mentor,
                StatusIcons.MentorCrafting,
                StatusIcons.MentorPvE,
                StatusIcons.MentorPvP,
                StatusIcons.Returner,
                StatusIcons.NewAdventurer,
            });

            var setInDuty = GetConditionSet(StatusIconPriorizerConditionSets.InDuty);
            setInDuty.AddRange(new[]
            {
                StatusIcons.Disconnecting, // Disconnecting
                StatusIcons.ViewingCutscene, // Viewing Cutscene
                StatusIcons.Idle, // Idle
                StatusIcons.GroupPose, // Group Pose
                StatusIcons.Mentor,
                StatusIcons.MentorCrafting,
                StatusIcons.MentorPvE,
                StatusIcons.MentorPvP,
                StatusIcons.Returner,
                StatusIcons.NewAdventurer,
            });

            var setInForay = GetConditionSet(StatusIconPriorizerConditionSets.InForay);
            setInForay.AddRange(new[]
            {
                // This allows you to see which players don't have a party
                StatusIcons.InDuty, // In Duty

                StatusIcons.Disconnecting, // Disconnecting
                StatusIcons.ViewingCutscene, // Viewing Cutscene
                StatusIcons.Idle, // Idle
                StatusIcons.GroupPose, // Group Pose
                StatusIcons.Mentor,
                StatusIcons.MentorCrafting,
                StatusIcons.MentorPvE,
                StatusIcons.MentorPvP,
                StatusIcons.Returner,
                StatusIcons.NewAdventurer,
            });
        }
    }
}
