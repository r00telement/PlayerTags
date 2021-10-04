using System;
using System.Collections.Generic;

namespace PlayerTags.Config
{
    [Serializable]
    public class RoleTagConfig : TagConfig
    {
        public RoleTagFormat Format = RoleTagFormat.AbbreviatedJobName;

        public Dictionary<Role, RoleOverrideConfig> RoleOverrideConfigs = new Dictionary<Role, RoleOverrideConfig>()
        {
            { Role.LandHand, new RoleOverrideConfig() { IsEnabled = true, Name = "Land/Hand", CustomColor = new CustomColorConfig() { Id = 3 } } },
            { Role.Tank, new RoleOverrideConfig() { IsEnabled = true, Name = "Tank", CustomColor = new CustomColorConfig() { Id = 542 } } },
            { Role.Healer, new RoleOverrideConfig() { IsEnabled = true, Name = "Healer", CustomColor = new CustomColorConfig() { Id = 45 } } },
            { Role.DPS, new RoleOverrideConfig() { IsEnabled = true, Name = "DPS", CustomColor = new CustomColorConfig() { Id = 511 } } },
        };
    }
}
