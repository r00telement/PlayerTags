using Newtonsoft.Json;
using System;

namespace PlayerTags
{
    public class GeneralConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value != null && value.GetType().IsEnum)
            {
                writer.WriteValue(Enum.GetName(value.GetType(), value));
            }
            else
            {
                writer.WriteValue(value);
            }
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            return reader.Value;
        }

        public override bool CanConvert(Type objectType)
        {
            return true;
        }
    }
}
