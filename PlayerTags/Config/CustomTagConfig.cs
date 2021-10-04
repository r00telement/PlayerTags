using System;
using System.Linq;

namespace PlayerTags.Config
{
    [Serializable]
    public class CustomTagConfig : TagConfig
    {
        public CustomColorConfig CustomColor = new CustomColorConfig();
        public string Name = "";
        public string FormattedGameObjectNames = "";

        private string[] GameObjectNames
        {
            get
            {
                return FormattedGameObjectNames.Split(';').Select(gameObjectName => gameObjectName.ToLower().Trim()).ToArray();
            }
        }

        public bool IncludesGameObjectName(string gameObjectName)
        {
            return GameObjectNames.Contains(gameObjectName);
        }
    }
}
