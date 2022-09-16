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

            public List<Payload> DisplayTextPayloads { get; init; } = new();

            /// <summary>
            /// The matching game object if one exists
            /// </summary>
            public GameObject? GameObject { get; init; }

            /// <summary>
            /// A matching player payload if one exists.
            /// </summary>
            public PlayerPayload? PlayerPayload { get; init; }

            public Payload? PlayerNamePayload
            {
                get
                {
                    Payload textPayload = null;
                    string textMatch = GetMatchText();

                    textPayload = DisplayTextPayloads.FirstOrDefault(n => n is TextPayload textPayload && textPayload.Text.Contains(textMatch));
                    textPayload ??= PlayerPayload;
                    textPayload ??= DisplayTextPayloads.FirstOrDefault();

                    return textPayload;
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
                AddTagsToChat(sender, type, true);
                AddTagsToChat(message, type, false);
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
            List<StringMatch> stringMatches = new();
            Stack<PlayerPayload> curPlayerPayload = new();
            Stack<List<Payload>> curRefPayloads = new();
            var defaultRawPayload = RawPayload.LinkTerminator.Data;

            foreach (var payload in seString.Payloads)
            {

                if (payload is PlayerPayload playerPayload)
                {
                    curPlayerPayload.Push(playerPayload);
                    curRefPayloads.Push(new List<Payload>());
                }
                else if (payload is RawPayload rawPayload)
                {
                    if (defaultRawPayload.SequenceEqual(rawPayload.Data))
                        finishCurrentMatch();
                }
                else
                {
                    if (curRefPayloads.TryPeek(out List<Payload> result))
                        result.Add(payload);
                }
            }

            // Finally finish, if not closed by RawPayload
            finishCurrentMatch();

            void finishCurrentMatch()
            {
                if (curPlayerPayload.TryPop(out PlayerPayload playerPayload))
                {
                    var gameObject = PluginServices.ObjectTable.FirstOrDefault(gameObject => gameObject.Name.TextValue == playerPayload.PlayerName);
                    var stringMatch = new StringMatch(seString)
                    {
                        GameObject = gameObject,
                        PlayerPayload = playerPayload,
                        DisplayTextPayloads = curRefPayloads.Pop()
                    };
                    stringMatches.Add(stringMatch);
                }
            }

            return stringMatches;
        }

        private void SplitOffPartyNumberPrefix(SeString sender, XivChatType type)
        {
            if (type == XivChatType.Party || type == XivChatType.Alliance)
            {
                PlayerPayload lastPlayerPayload = null;
                foreach (var payload in sender.Payloads.ToArray())
                {
                    if (payload is PlayerPayload playerPayload)
                        lastPlayerPayload = playerPayload;
                    else if (payload is TextPayload playerNamePayload && lastPlayerPayload != null)
                    {
                        // Get position of player name in payload
                        var indexOfPlayerName = playerNamePayload.Text.IndexOf(lastPlayerPayload.PlayerName);

                        if (indexOfPlayerName > 0)
                        {
                            // Split off the name from the prefix number
                            var prefixPayload = new TextPayload(playerNamePayload.Text[..indexOfPlayerName]);
                            playerNamePayload.Text = playerNamePayload.Text[indexOfPlayerName..];

                            // Add prefix number before the player name payload
                            var playerNamePayloadIndex = sender.Payloads.IndexOf(playerNamePayload);
                            sender.Payloads.Insert(playerNamePayloadIndex, prefixPayload);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds all configured tags to a chat message.
        /// </summary>
        /// <param name="message">The message to change.</param>
        private void AddTagsToChat(SeString message, XivChatType chatType, bool isSender)
        {
            // Split out the party/alliance number from the PlayerPayload
            if (isSender)
                SplitOffPartyNumberPrefix(message, chatType);

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
                if (stringMatch.PlayerPayload != null && stringMatch.DisplayTextPayloads.Any())
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
                        => ApplyTextFormatting(stringMatch.GameObject, tag, new[] { message }, new[] { tag.IsTextColorAppliedToChatName }, stringMatch.DisplayTextPayloads);
                }

                ApplyStringChanges(message, stringChanges, stringMatch.DisplayTextPayloads, stringMatch.PlayerNamePayload);
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
