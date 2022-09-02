using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using PlayerTags.Configuration;
using PlayerTags.Data;
using System;
using System.Collections.Generic;

namespace PlayerTags.Features
{
    public class LinkSelfInChatFeature : IDisposable
    {
        private PluginConfiguration m_PluginConfiguration;
        private PluginData m_PluginData;
        private ActivityContextManager activityContextManager;

        public LinkSelfInChatFeature(PluginConfiguration pluginConfiguration, PluginData pluginData)
        {
            m_PluginConfiguration = pluginConfiguration;
            m_PluginData = pluginData;
            activityContextManager = new();

            PluginServices.ChatGui.ChatMessage += Chat_ChatMessage;
        }

        public void Dispose()
        {
            PluginServices.ChatGui.ChatMessage -= Chat_ChatMessage;
            activityContextManager.Dispose();
        }

        private void Chat_ChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            if (m_PluginConfiguration.GeneralOptions[activityContextManager.CurrentActivityContext].IsLinkSelfInChatEnabled)
            {
                ParsePayloads(sender);
                ParsePayloads(message);
            }
        }

        private void ParsePayloads(SeString seString)
        {
            if (PluginServices.ClientState.LocalPlayer != null)
            {
                foreach (var payload in seString.Payloads.ToArray())
                {
                    if (payload is not TextPayload textPayload)
                    {
                        continue;
                    }

                    List<TextPayload> playerTextPayloads = new List<TextPayload>();

                    var playerName = PluginServices.ClientState.LocalPlayer.Name.TextValue;

                    if (textPayload.Text == playerName)
                    {
                        playerTextPayloads.Add(textPayload);
                        textPayload.Text = textPayload.Text;
                    }
                    else
                    {
                        var textMatchIndex = textPayload.Text.IndexOf(playerName);

                        while (textMatchIndex >= 0)
                        {
                            var textPayloadIndex = seString.Payloads.IndexOf(payload);

                            // Chop text to the left and insert it as a new payload
                            if (textMatchIndex > 0)
                            {
                                // Add the content before the player
                                seString.Payloads.Insert(textPayloadIndex, new TextPayload(textPayload.Text.Substring(0, textMatchIndex)));

                                // Remove from the chopped text from the original payload
                                textPayload.Text = textPayload.Text.Substring(textMatchIndex, textPayload.Text.Length - textMatchIndex);
                            }

                            // This is the last reference to the local player in this payload
                            if (textPayload.Text.Length == playerName.Length)
                            {
                                playerTextPayloads.Add(textPayload);
                                break;
                            }

                            // Create the new name payload and add it
                            var playerTextPayload = new TextPayload(playerName);
                            playerTextPayloads.Add(playerTextPayload);
                            seString.Payloads.Insert(textPayloadIndex, playerTextPayload);

                            // Remove from the chopped text from the original payload
                            textPayload.Text = textPayload.Text.Substring(0, playerName.Length);

                            textMatchIndex = textPayload.Text.IndexOf(playerName);
                        }
                    }

                    foreach (var playerTextPayload in playerTextPayloads)
                    {
                        // This does some dodgy shit for an unknown reason.
                        // Typically when you receive a player payload followed by a text payload, it displays the text
                        // and links it with the player payload. When trying to make one of these manually, it displays the player payload separately,
                        // effectively doubling up the player name.
                        // For now, don't follow up with a text payload. Only use a player payload.
                        var playerPayload = new PlayerPayload(playerName, PluginServices.ClientState.LocalPlayer.HomeWorld.Id);
                        seString.Payloads.Insert(seString.Payloads.IndexOf(playerTextPayload), playerPayload);
                        seString.Payloads.Remove(playerTextPayload);
                    }
                }
            }
        }
    }
}
