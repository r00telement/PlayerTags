using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Lumina.Excel.GeneratedSheets;
using PlayerTags.Configuration;
using PlayerTags.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Action = System.Action;

namespace PlayerTags.Features
{
    /// <summary>
    /// A feature that adds tags to chat messages.
    /// </summary>
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
            public TextPayload? TextPayload { get; init; }

            /// <summary>
            /// The matching game object if one exists
            /// </summary>
            public GameObject? GameObject { get; init; }

            /// <summary>
            /// A matching player payload if one exists.
            /// </summary>
            public PlayerPayload? PlayerPayload { get; init; }

            public Payload? PreferredPayload
            {
                get
                {
                    if (TextPayload != null)
                    {
                        return TextPayload;
                    }

                    return PlayerPayload;
                }
            }

            public StringMatch(SeString seString)
            {
                SeString = seString;
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

                if (TextPayload != null)
                {
                    return TextPayload.Text;
                }

                if (PlayerPayload != null)
                {
                    return PlayerPayload.PlayerName;
                }

                return SeString.TextValue;
            }
        }

        private PluginConfiguration m_PluginConfiguration;
        private PluginData m_PluginData;

        public ChatTagTargetFeature(PluginConfiguration pluginConfiguration, PluginData pluginData)
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
            if (m_PluginConfiguration.GeneralOptions[ActivityContextManager.CurrentActivityContext].IsApplyTagsToAllChatMessagesEnabled || Enum.IsDefined(type))
            {
                AddTagsToChat(sender);
                AddTagsToChat(message);
            }
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

                    TextPayload? textPayload = null;

                    // The next payload MUST be a text payload
                    if (payloadIndex + 1 < seString.Payloads.Count)
                    {
                        textPayload = seString.Payloads[payloadIndex + 1] as TextPayload;

                        // Don't handle the text payload twice
                        payloadIndex++;
                    }

                    var stringMatch = new StringMatch(seString)
                    {
                        GameObject = gameObject,
                        PlayerPayload = playerPayload,
                        TextPayload = textPayload
                    };
                    stringMatches.Add(stringMatch);
                }
            }

            return stringMatches;
        }

        /// <summary>
        /// Adds all configured tags to a chat message.
        /// </summary>
        /// <param name="message">The message to change.</param>
        private void AddTagsToChat(SeString message)
        {
            var stringMatches = GetStringMatches(message);
            foreach (var stringMatch in stringMatches)
            {
                Dictionary<TagPosition, List<Payload>> stringChanges = new Dictionary<TagPosition, List<Payload>>();

                if (stringMatch.GameObject is PlayerCharacter playerCharacter)
                {
                    // Add the job tag
                    if (playerCharacter.ClassJob.GameData != null && m_PluginData.JobTags.TryGetValue(playerCharacter.ClassJob.GameData.Abbreviation, out var jobTag))
                    {
                        if (jobTag.TagPositionInChat.InheritedValue != null)
                        {
                            var payloads = GetPayloads(jobTag, stringMatch.GameObject);
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

                // Add custom tags
                if (stringMatch.PlayerPayload != null)
                {
                    Identity identity = m_PluginData.GetIdentity(stringMatch.PlayerPayload);
                    foreach (var customTagId in identity.CustomTagIds)
                    {
                        var customTag = m_PluginData.CustomTags.FirstOrDefault(tag => tag.CustomId.Value == customTagId);
                        if (customTag != null)
                        {
                            if (customTag.TagPositionInChat.InheritedValue != null)
                            {
                                var customTagPayloads = GetPayloads(customTag, stringMatch.GameObject);
                                if (customTagPayloads.Any())
                                {
                                    AddPayloadChanges(customTag.TagPositionInChat.InheritedValue.Value, customTagPayloads, stringChanges);
                                }
                            }
                        }
                    }
                }

                // An additional step to apply text color to additional locations
                if (stringMatch.PlayerPayload != null && stringMatch.PreferredPayload != null)
                {
                    Identity identity = m_PluginData.GetIdentity(stringMatch.PlayerPayload);
                    foreach (var customTagId in identity.CustomTagIds)
                    {
                        var customTag = m_PluginData.CustomTags.FirstOrDefault(tag => tag.CustomId.Value == customTagId);
                        if (customTag != null)
                            applyTextFormatting(customTag);
                    }

                    if (stringMatch.GameObject is PlayerCharacter playerCharacter1)
                    {
                        if (playerCharacter1.ClassJob.GameData != null && m_PluginData.JobTags.TryGetValue(playerCharacter1.ClassJob.GameData.Abbreviation, out var jobTag))
                            applyTextFormatting(jobTag);
                    }

                    void applyTextFormatting(Tag tag)
                        => ApplyTextFormatting(stringMatch.GameObject, tag, new[] { message }, new[] { tag.IsTextColorAppliedToChatName }, stringMatch.PreferredPayload);
                }

                ApplyStringChanges(message, stringChanges, stringMatch.PreferredPayload);
            }

            // Replace PlayerPayloads of your own character with TextPayloads
            foreach (var payload in message.Payloads.ToArray())
            {
                if (payload is PlayerPayload playerPayload && playerPayload.PlayerName.Contains(PluginServices.ClientState.LocalPlayer.Name.TextValue))
                    message.Payloads.Remove(payload);
            }
        }
    }
}
