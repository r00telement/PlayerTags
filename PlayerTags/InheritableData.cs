using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace PlayerTags
{
    [Serializable]
    public struct InheritableData
    {
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("Behavior")]
        public InheritableBehavior Behavior;

        [JsonProperty("Value")]
        [JsonConverter(typeof(GeneralConverter))]
        public object Value;
    }
}
