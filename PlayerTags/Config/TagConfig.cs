using System;

namespace PlayerTags.Config
{
    [Serializable]
    public class TagConfig
    {
        public NameplateElement NameplateElement = NameplateElement.Name;

        public StringPosition NameplatePosition = StringPosition.Before;

        public StringPosition ChatPosition = StringPosition.Before;
    }
}
