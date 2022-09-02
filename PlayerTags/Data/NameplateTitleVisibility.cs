using Newtonsoft.Json;

namespace PlayerTags.Data
{
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum NameplateTitleVisibility
    {
        Default,
        Always,
        Never,
        WhenHasTags
    }
}
