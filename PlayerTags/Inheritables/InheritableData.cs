using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace PlayerTags.Inheritables
{
    [Serializable]
    public struct InheritableData
    {
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("Behavior")]
        public InheritableBehavior Behavior;

        [JsonProperty("Value")]
        public object Value;
    }
}
