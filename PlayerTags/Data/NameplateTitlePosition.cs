using Newtonsoft.Json;

namespace PlayerTags.Data
{
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum NameplateTitlePosition
    {
        Default,
        AlwaysAboveName,
        AlwaysBelowName
    }
}
