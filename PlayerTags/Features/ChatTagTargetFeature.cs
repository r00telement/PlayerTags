using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Logging;
using PlayerTags.Configuration;
using PlayerTags.Data;
using System.Collections.Generic;
using System.Linq;

namespace PlayerTags.Features
{
    public class ChatTagTargetFeature : TagTargetFeature
    {
        /// <summary>
        /// A match found within a string.
        /// </summary>
        private class StringMatch
        {
            /// <summary>
            /// The string that the match was found in.
            /// </summary>
            public SeString SeString { get; init; }

            /// <summary>
            /// The matching text payload.
            /// </summary>
            public TextPayload TextPayload { get; init; }

            /// <summary>
            /// The matching game object if one exists
            /// </summary>
            public GameObject? GameObject { get; init; }

            /// <summary>
            /// A matching player payload if one exists.
            /// </summary>
            public PlayerPayload? PlayerPayload { get; init; }

            public StringMatch(SeString seString, TextPayload textPayload)
            {
                SeString = seString;
                TextPayload = textPayload;
            }

            /// <summary>
            /// Gets the matches text.
            /// </summary>
            /// <returns>The match text.</returns>
            public string GetMatchText()
            {
                if (GameObject != null)
                {
                    return GameObject.Name.TextValue;
                }

                return TextPayload.Text;
            }
        }

        private PluginConfiguration m_PluginConfiguration;
        private PluginData m_PluginData;

        public ChatTagTargetFeature(PluginConfiguration pluginConfiguration, PluginData pluginData)
             : base(pluginConfiguration)
        {
            m_PluginConfiguration = pluginConfiguration;
            m_PluginData = pluginData;

            PluginServices.ChatGui.ChatMessage += Chat_ChatMessage;
        }

        public override void Dispose()
        {
            PluginServices.ChatGui.ChatMessage -= Chat_ChatMessage;
            base.Dispose();
        }

        private void Chat_ChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            AddTagsToChat(sender, out _);
            AddTagsToChat(message, out _);
        }

        protected override bool IsIconVisible(Tag tag)
        {
            if (tag.IsIconVisibleInChat.InheritedValue != null)
            {
                return tag.IsIconVisibleInChat.InheritedValue.Value;
            }

            return false;
        }

        protected override bool IsTextVisible(Tag tag)
        {
            if (tag.IsTextVisibleInChat.InheritedValue != null)
            {
                return tag.IsTextVisibleInChat.InheritedValue.Value;
            }

            return false;
        }

        /// <summary>
        /// Searches the given string for game object matches.
        /// </summary>
        /// <param name="seString">The string to search.</param>
        /// <returns>A list of matched game objects.</returns>
        private List<StringMatch> GetStringMatches(SeString seString)
        {
            List<StringMatch> stringMatches = new List<StringMatch>();

            for (int payloadIndex = 0; payloadIndex < seString.Payloads.Count; ++payloadIndex)
            {
                var payload = seString.Payloads[payloadIndex];
                if (payload is PlayerPayload playerPayload)
                {
                    var gameObject = PluginServices.ObjectTable.FirstOrDefault(gameObject => gameObject.Name.TextValue == playerPayload.PlayerName);

                    // The next payload MUST be a text payload
                    if (payloadIndex + 1 < seString.Payloads.Count && seString.Payloads[payloadIndex + 1] is TextPayload textPayload)
                    {
                        var stringMatch = new StringMatch(seString, textPayload)
                        {
                            GameObject = gameObject,
                            PlayerPayload = playerPayload
                        };
                        stringMatches.Add(stringMatch);

                        // Don't handle the text payload twice
                        payloadIndex++;
                    }
                    else
                    {
                        PluginLog.Error("Expected payload after player payload to be a text payload but it wasn't");
                    }
                }

                /// TODO: Not sure if this is desirable. Enabling this allows tags to appear next to the name of the local player by text in chat because the local player doesn't have a player payload.
                /// But because it's just a simple string comparison, it won't work in all circumstances. E.g. in party chat the player name is wrapped in (). To be comprehensive we need to search substring.
                /// This means we would need to think about breaking down existing payloads to split them out.
                /// If we decide to do that, we could even for example find unlinked player names in chat and add player payloads for them.
                // If it's just a text payload then either a character NEEDS to exist for it, or it needs to be identified as a character by custom tag configs
                //else if (payload is TextPayload textPayload)
                //{
                //    var gameObject = ObjectTable.FirstOrDefault(gameObject => gameObject.Name.TextValue == textPayload.Text);
                //    var isIncludedInCustomTagConfig = m_Config.CustomTags.Any(customTagConfig => customTagConfig.IncludesGameObjectName(textPayload.Text));

                //    if (gameObject != null || isIncludedInCustomTagConfig)
                //    {
                //        var stringMatch = new StringMatch(seString, textPayload)
                //        {
                //            GameObject = gameObject
                //        };
                //        stringMatches.Add(stringMatch);
                //    }
                //}
            }

            return stringMatches;
        }

        /// <summary>
        /// Adds all configured tags to chat.
        /// </summary>
        /// <param name="message">The message to change.</param>
        /// <param name="isMessageChanged">Whether the message was changed.</param>
        private void AddTagsToChat(SeString message, out bool isMessageChanged)
        {
            isMessageChanged = false;

            var stringMatches = GetStringMatches(message);
            foreach (var stringMatch in stringMatches)
            {
                Dictionary<TagPosition, List<Payload>> stringChanges = new Dictionary<TagPosition, List<Payload>>();

                // The role tag payloads
                if (stringMatch.GameObject is Character character)
                {
                    // Add the job tag
                    if (m_PluginData.JobTags.TryGetValue(character.ClassJob.GameData.Abbreviation, out var jobTag))
                    {
                        if (jobTag.TagPositionInChat.InheritedValue != null)
                        {
                            var payloads = GetPayloads(stringMatch.GameObject, jobTag);
                            if (payloads.Any())
                            {
                                AddPayloadChanges(jobTag.TagPositionInChat.InheritedValue.Value, payloads, stringChanges);
                            }
                        }
                    }

                    // Add randomly generated name tag payload
                    if (m_PluginConfiguration.IsPlayerNameRandomlyGenerated)
                    {
                        var playerName = stringMatch.GetMatchText();
                        if (playerName != null)
                        {
                            var generatedName = RandomNameGenerator.Generate(playerName);
                            if (generatedName != null)
                            {
                                AddPayloadChanges(TagPosition.Replace, Enumerable.Empty<Payload>().Append(new TextPayload(generatedName)), stringChanges);
                            }
                        }
                    }
                }

                // Add the custom tag payloads
                foreach (var customTag in m_PluginData.CustomTags)
                {
                    if (customTag.TagPositionInChat.InheritedValue != null)
                    {
                        if (customTag.IncludesGameObjectNameToApplyTo(stringMatch.GetMatchText()))
                        {
                            var customTagPayloads = GetPayloads(stringMatch.GameObject, customTag);
                            if (customTagPayloads.Any())
                            {
                                AddPayloadChanges(customTag.TagPositionInChat.InheritedValue.Value, customTagPayloads, stringChanges);
                            }
                        }
                    }
                }

                ApplyStringChanges(message, stringChanges, stringMatch.TextPayload);
                isMessageChanged = true;
            }
        }
    }
}
