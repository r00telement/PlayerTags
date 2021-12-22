using Dalamud.Configuration;
using Newtonsoft.Json;
using PlayerTags.Data;
using PlayerTags.Inheritables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayerTags.Configuration
{
    [Serializable]
    public class PluginConfiguration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public bool IsVisible = false;
        public NameplateFreeCompanyVisibility NameplateFreeCompanyVisibility = NameplateFreeCompanyVisibility.Default;
        public NameplateTitleVisibility NameplateTitleVisibility = NameplateTitleVisibility.WhenHasTags;
        public NameplateTitlePosition NameplateTitlePosition = NameplateTitlePosition.AlwaysAboveName;
        public bool IsPlayerNameRandomlyGenerated = false;
        public bool IsCustomTagsContextMenuEnabled = true;
        public bool IsShowInheritedPropertiesEnabled = true;
        public bool IsPlayersTabOrderedByProximity = false;
        public bool IsPlayersTabSelfVisible = true;
        public bool IsPlayersTabFriendsVisible = true;
        public bool IsPlayersTabPartyVisible = true;
        public bool IsPlayersTabAllianceVisible = true;
        public bool IsPlayersTabEnemiesVisible = true;
        public bool IsPlayersTabOthersVisible = false;
        public bool IsLinkSelfInChatEnabled = false;

        [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)]
        public Dictionary<string, InheritableData> AllTagsChanges = new Dictionary<string, InheritableData>();

        [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)]
        public Dictionary<string, InheritableData> AllRoleTagsChanges = new Dictionary<string, InheritableData>();

        [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)]
        public Dictionary<Role, Dictionary<string, InheritableData>> RoleTagsChanges = new Dictionary<Role, Dictionary<string, InheritableData>>();

        [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)]
        public Dictionary<DpsRole, Dictionary<string, InheritableData>> DpsRoleTagsChanges = new Dictionary<DpsRole, Dictionary<string, InheritableData>>();

        [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)]
        public Dictionary<RangedDpsRole, Dictionary<string, InheritableData>> RangedDpsRoleTagsChanges = new Dictionary<RangedDpsRole, Dictionary<string, InheritableData>>();

        [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)]
        public Dictionary<LandHandRole, Dictionary<string, InheritableData>> LandHandRoleTagsChanges = new Dictionary<LandHandRole, Dictionary<string, InheritableData>>();

        [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)]
        public Dictionary<string, Dictionary<string, InheritableData>> JobTagsChanges = new Dictionary<string, Dictionary<string, InheritableData>>();

        [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)]
        public Dictionary<string, InheritableData> AllCustomTagsChanges = new Dictionary<string, InheritableData>();

        [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)]
        public List<Dictionary<string, InheritableData>> CustomTagsChanges = new List<Dictionary<string, InheritableData>>();

        public event System.Action? Saved;

        public void Save(PluginData pluginData)
        {
            AllTagsChanges = pluginData.AllTags.GetChanges(pluginData.Default.AllTagsChanges);
            AllRoleTagsChanges = pluginData.AllRoleTags.GetChanges(pluginData.Default.AllRoleTagsChanges);

            RoleTagsChanges = new Dictionary<Role, Dictionary<string, InheritableData>>();
            foreach ((var role, var roleTag) in pluginData.RoleTags)
            {
                Dictionary<string, InheritableData>? defaultChanges;
                pluginData.Default.RoleTagsChanges.TryGetValue(role, out defaultChanges);

                var changes = roleTag.GetChanges(defaultChanges);
                if (changes.Any())
                {
                    RoleTagsChanges[role] = changes;
                }
            }

            DpsRoleTagsChanges = new Dictionary<DpsRole, Dictionary<string, InheritableData>>();
            foreach ((var dpsRole, var dpsRoleTag) in pluginData.DpsRoleTags)
            {
                Dictionary<string, InheritableData>? defaultChanges;
                pluginData.Default.DpsRoleTagsChanges.TryGetValue(dpsRole, out defaultChanges);

                var changes = dpsRoleTag.GetChanges(defaultChanges);
                if (changes.Any())
                {
                    DpsRoleTagsChanges[dpsRole] = changes;
                }
            }

            RangedDpsRoleTagsChanges = new Dictionary<RangedDpsRole, Dictionary<string, InheritableData>>();
            foreach ((var rangedDpsRole, var rangedDpsRoleTag) in pluginData.RangedDpsRoleTags)
            {
                Dictionary<string, InheritableData>? defaultChanges;
                pluginData.Default.RangedDpsRoleTagsChanges.TryGetValue(rangedDpsRole, out defaultChanges);

                var changes = rangedDpsRoleTag.GetChanges(defaultChanges);
                if (changes.Any())
                {
                    RangedDpsRoleTagsChanges[rangedDpsRole] = changes;
                }
            }

            LandHandRoleTagsChanges = new Dictionary<LandHandRole, Dictionary<string, InheritableData>>();
            foreach ((var landHandRole, var landHandRoleTag) in pluginData.LandHandRoleTags)
            {
                Dictionary<string, InheritableData>? defaultChanges;
                pluginData.Default.LandHandRoleTagsChanges.TryGetValue(landHandRole, out defaultChanges);

                var changes = landHandRoleTag.GetChanges(defaultChanges);
                if (changes.Any())
                {
                    LandHandRoleTagsChanges[landHandRole] = changes;
                }
            }

            JobTagsChanges = new Dictionary<string, Dictionary<string, InheritableData>>();
            foreach ((var jobAbbreviation, var jobTag) in pluginData.JobTags)
            {
                Dictionary<string, InheritableData>? defaultChanges;
                pluginData.Default.JobTagsChanges.TryGetValue(jobAbbreviation, out defaultChanges);

                var changes = jobTag.GetChanges(defaultChanges);
                if (changes.Any())
                {
                    JobTagsChanges[jobAbbreviation] = changes;
                }
            }

            AllCustomTagsChanges = pluginData.AllCustomTags.GetChanges(pluginData.Default.AllCustomTagsChanges);

            CustomTagsChanges = new List<Dictionary<string, InheritableData>>();
            foreach (var customTag in pluginData.CustomTags)
            {
                CustomTagsChanges.Add(customTag.GetChanges());
            }

            PluginServices.DalamudPluginInterface.SavePluginConfig(this);
            Saved?.Invoke();
        }
    }
}
