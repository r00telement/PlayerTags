using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayerTags
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
        public bool IsCustomTagContextMenuEnabled = true;

        [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)]
        public Dictionary<string, InheritableData> AllTagsChanges = new Dictionary<string, InheritableData>();

        [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)]
        public Dictionary<string, InheritableData> AllRoleTagsChanges = new Dictionary<string, InheritableData>();

        [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)]
        public Dictionary<Role, Dictionary<string, InheritableData>> RoleTagsChanges = new Dictionary<Role, Dictionary<string, InheritableData>>();

        [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)]
        public Dictionary<string, Dictionary<string, InheritableData>> JobTagsChanges = new Dictionary<string, Dictionary<string, InheritableData>>();

        [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)]
        public Dictionary<string, InheritableData> AllCustomTagsChanges = new Dictionary<string, InheritableData>();

        [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)]
        public List<Dictionary<string, InheritableData>> CustomTagsChanges = new List<Dictionary<string, InheritableData>>();

        [NonSerialized]
        private DalamudPluginInterface? m_PluginInterface;

        public event System.Action? Saved;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            m_PluginInterface = pluginInterface;
        }

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

            if (m_PluginInterface != null)
            {
                m_PluginInterface.SavePluginConfig(this);
                Saved?.Invoke();
            };
        }
    }
}
