using Newtonsoft.Json;
using System;

namespace PlayerTags.Data
{
    [Obsolete]
    [Flags]
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum ActivityContext
    {
        None = 0x0,
        PveDuty = 0x1,
        PvpDuty = 0x2,
    }
}
