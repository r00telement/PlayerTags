using Dalamud.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace PlayerTags
{
    /// <summary>
    /// Generates names based on an existing list of words.
    /// </summary>
    public class RandomNameGenerator
    {
        private const string c_AdjectivesPath = "Resources/Words/Adjectives.txt";
        private string[]? m_Adjectives;

        private const string c_NounsPath = "Resources/Words/Nouns.txt";
        private string[]? m_Nouns;

        private Dictionary<int, string> m_GeneratedNames = new Dictionary<int, string>();

        private string? PluginDirectory
        {
            get { return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); }
        }

        public RandomNameGenerator()
        {
            try
            {
                m_Adjectives = File.ReadAllLines($"{PluginDirectory}/{c_AdjectivesPath}");
                m_Nouns = File.ReadAllLines($"{PluginDirectory}/{c_NounsPath}");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"RandomNameGenerator failed to create");
            }
        }

        /// <summary>
        /// Generates a name for the given string.
        /// </summary>
        /// <param name="str">The string to generate a name for.</param>
        /// <returns>A generated name.</returns>
        public string? GetGeneratedName(string str)
        {
            if (m_Adjectives == null || m_Nouns == null)
            {
                return null;
            }

            int hash = GetDeterministicHashCode(str);

            if (!m_GeneratedNames.ContainsKey(hash))
            {
                // Use the seed as the hash so that player always gets the same name
                Random random = new Random(hash);
                var adjective = m_Adjectives[random.Next(0, m_Adjectives.Length)];
                var noun = m_Nouns[random.Next(0, m_Nouns.Length)];
                var generatedName = $"{adjective} {noun}";

                m_GeneratedNames[hash] = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(generatedName);
            }

            return m_GeneratedNames[hash];
        }

        /// <summary>
        /// Gets a deterministic hash code for the given string/
        /// </summary>
        /// <param name="str">The string to hash.</param>
        /// <returns>A deterministic hash code.</returns>
        private static int GetDeterministicHashCode(string str)
        {
            unchecked
            {
                int hash1 = (5381 << 16) + 5381;
                int hash2 = hash1;

                for (int index = 0; index < str.Length; index += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[index];
                    if (index == str.Length - 1)
                    {
                        break;
                    }

                    hash2 = ((hash2 << 5) + hash2) ^ str[index + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }
    }
}
