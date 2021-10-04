using System;
using System.Collections.Generic;

namespace PlayerTags.Config
{
    [Serializable]
    public class RoleOverrideConfig
    {
        public bool IsEnabled = true;
        public string Name = "";
        public CustomColorConfig CustomColor = new CustomColorConfig();
        public Dictionary<string, JobOverrideConfig> JobOverrideConfigs = new Dictionary<string, JobOverrideConfig>();
    }
}
