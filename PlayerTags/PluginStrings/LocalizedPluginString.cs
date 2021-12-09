namespace PlayerTags.PluginStrings
{
    public class LocalizedPluginString : IPluginString
    {
        public string Key { get; init; }
        public string Value => Localizer.GetString(Key, false);

        public LocalizedPluginString(string key)
        {
            Key = key;
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
