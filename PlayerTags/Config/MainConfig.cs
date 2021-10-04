using Dalamud.Configuration;
using Dalamud.Data;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayerTags.Config
{
    [Serializable]
    public class MainConfig : IPluginConfiguration
    {
        public static Dictionary<byte, Role> RolesById { get; } = new Dictionary<byte, Role>()
        {
            { 0, Role.LandHand },
            { 1, Role.Tank },
            { 2, Role.DPS },
            { 3, Role.DPS },
            { 4, Role.Healer },
        };

        public int Version { get; set; } = 0;

        public FreeCompanyNameplateVisibility FreeCompanyVisibility = FreeCompanyNameplateVisibility.Default;
        public TitleNameplateVisibility TitleVisibility = TitleNameplateVisibility.Default;
        public TitleNameplatePosition TitlePosition = TitleNameplatePosition.Default;

        public RoleTagConfig RoleTag = new RoleTagConfig();

        public List<CustomTagConfig> CustomTagConfigs = new List<CustomTagConfig>();

        public bool IsPlayerNameRandomlyGenerated = false;

        [NonSerialized]
        private DalamudPluginInterface? m_PluginInterface;

        public event System.Action? Saved;

        public void Initialize(DalamudPluginInterface pluginInterface, DataManager dataManager)
        {
            m_PluginInterface = pluginInterface;

            // Populate each role config with all of its jobs if they aren't already in it
            foreach (var roleConfigPair in RoleTag.RoleOverrideConfigs)
            {
                var role = roleConfigPair.Key;
                var roleConfig = roleConfigPair.Value;

                var classJobs = dataManager.GetExcelSheet<ClassJob>();
                if (classJobs != null)
                {
                    foreach (var classJob in classJobs.Where(classJob => RolesById[classJob.Role] == role))
                    {
                        if (!roleConfig.JobOverrideConfigs.ContainsKey(classJob.Abbreviation))
                        {
                            roleConfig.JobOverrideConfigs[classJob.Abbreviation] = new JobOverrideConfig();
                        }
                    }
                }
            }
        }

        public void Save()
        {
            if (m_PluginInterface != null)
            {
                m_PluginInterface.SavePluginConfig(this);
                Saved?.Invoke();
            };
        }
    }
}
