using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using PlayerTags.Config;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayerTags
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Player Tags";

        private const string c_CommandName = "/playertags";

        [PluginService]
        private static DalamudPluginInterface PluginInterface { get; set; } = null!;

        [PluginService]
        private static Framework Framework { get; set; } = null!;

        [PluginService]
        private static ChatGui ChatGui { get; set; } = null!;

        [PluginService]
        private static GameGui GameGui { get; set; } = null!;

        [PluginService]
        private static ObjectTable ObjectTable { get; set; } = null!;

        [PluginService]
        private static DataManager DataManager { get; set; } = null!;

        [PluginService]
        private static CommandManager CommandManager { get; set; } = null!;

        private MainConfig m_Config;

        private MainConfigUI m_ConfigUI;

        private Dictionary<string, Payload[]> m_JobTagPayloads = new Dictionary<string, Payload[]>();

        private Dictionary<CustomTagConfig, Payload[]> m_CustomTagPayloads = new Dictionary<CustomTagConfig, Payload[]>();

        private TextPayload m_SpaceTextPayload = new TextPayload($" ");

        private PluginHooks m_PluginHooks;

        private RandomNameGenerator? m_RandomNameGenerator = null;

        public Plugin()
        {
            UIColorHelper.Initialize(DataManager);

            m_Config = PluginInterface.GetPluginConfig() as MainConfig ?? new MainConfig();
            m_Config.Initialize(PluginInterface, DataManager);
            m_Config.Saved += Configuration_Saved;

            m_ConfigUI = new MainConfigUI(m_Config);

            CommandManager.AddHandler(c_CommandName, new CommandInfo((string command, string arguments) =>
            {
                m_ConfigUI.IsVisible = true;
            })
            {
                HelpMessage = "Shows the config"
            });

            PluginInterface.UiBuilder.Draw += UiBuilder_Draw;
            PluginInterface.UiBuilder.OpenConfigUi += UiBuilder_OpenConfigUi;

            m_PluginHooks = new PluginHooks(Framework, ObjectTable, GameGui, SetNameplate);

            ChatGui.ChatMessage += Chat_ChatMessage;

            if (m_Config.IsPlayerNameRandomlyGenerated && m_RandomNameGenerator == null)
            {
                m_RandomNameGenerator = new RandomNameGenerator();
            }
        }

        public void Dispose()
        {
            PluginInterface.UiBuilder.OpenConfigUi -= UiBuilder_OpenConfigUi;
            CommandManager.RemoveHandler(c_CommandName);
            ChatGui.ChatMessage -= Chat_ChatMessage;
            m_Config.Saved -= Configuration_Saved;
            m_PluginHooks?.Dispose();
        }

        private void Configuration_Saved()
        {
            // Invalidate the cached payloads so they get remade
            m_JobTagPayloads.Clear();
            m_CustomTagPayloads.Clear();

            if (m_Config.IsPlayerNameRandomlyGenerated && m_RandomNameGenerator == null)
            {
                m_RandomNameGenerator = new RandomNameGenerator();
            }
        }

        private void UiBuilder_Draw()
        {
            m_ConfigUI.Draw();
        }

        private void UiBuilder_OpenConfigUi()
        {
            m_ConfigUI.IsVisible = true;
        }

        private void Chat_ChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            AddTagsToChat(sender, out _);
            AddTagsToChat(message, out _);
        }

        /// <summary>
        /// Sets the strings on a nameplate.
        /// </summary>
        /// <param name="gameObject">The game object context.</param>
        /// <param name="name">The name text.</param>
        /// <param name="title">The title text.</param>
        /// <param name="freeCompany">The free company text.</param>
        /// <param name="isTitleVisible">Whether the title is visible.</param>
        /// <param name="isTitleAboveName">Whether the title is above the name or below it.</param>
        /// <param name="iconId">The icon id.</param>
        /// <param name="isNameChanged">Whether the name was changed.</param>
        /// <param name="isTitleChanged">Whether the title was changed.</param>
        /// <param name="isFreeCompanyChanged">Whether the free company was changed.</param>
        private void SetNameplate(GameObject gameObject, SeString name, SeString title, SeString freeCompany, ref bool isTitleVisible, ref bool isTitleAboveName, ref int iconId, out bool isNameChanged, out bool isTitleChanged, out bool isFreeCompanyChanged)
        {
            AddTagsToNameplate(gameObject, name, title, freeCompany, out isNameChanged, out isTitleChanged, out isFreeCompanyChanged);

            if (m_Config.TitlePosition == TitleNameplatePosition.AlwaysAboveName)
            {
                isTitleAboveName = true;
            }
            else if (m_Config.TitlePosition == TitleNameplatePosition.AlwaysBelowName)
            {
                isTitleAboveName = false;
            }

            if (m_Config.TitleVisibility == TitleNameplateVisibility.Default)
            {
            }
            else if (m_Config.TitleVisibility == TitleNameplateVisibility.Always)
            {
                isTitleVisible = true;
            }
            else if (m_Config.TitleVisibility == TitleNameplateVisibility.Never)
            {
                isTitleVisible = false;
            }
            else if (m_Config.TitleVisibility == TitleNameplateVisibility.WhenHasTags)
            {
                isTitleVisible = isTitleChanged;
            }

            if (m_Config.FreeCompanyVisibility == FreeCompanyNameplateVisibility.Default)
            {
            }
            else if (m_Config.FreeCompanyVisibility == FreeCompanyNameplateVisibility.Never)
            {
                freeCompany.Payloads.Clear();
                isFreeCompanyChanged = true;
            }
        }

        /// <summary>
        /// Gets the job tag payloads for the given character. If the payloads don't yet exist then they are created.
        /// </summary>
        /// <param name="character">The character to get job tag payloads for.</param>
        /// <returns>A list of job tag payloads for the given character.</returns>
        private IEnumerable<Payload> GetJobTagPayloads(Character character)
        {
            var roleId = character.ClassJob.GameData.Role;
            var jobAbbreviation = character.ClassJob.GameData.Abbreviation;
            var role = MainConfig.RolesById[roleId];

            var roleConfig = m_Config.RoleTag.RoleOverrideConfigs[role];
            if (!roleConfig.IsEnabled)
            {
                return new Payload[] { };
            }

            if (m_JobTagPayloads.TryGetValue(jobAbbreviation, out var payloads))
            {
                return payloads;
            }

            string text = "";
            if (m_Config.RoleTag.Format == RoleTagFormat.AbbreviatedJobName)
            {
                text = character.ClassJob.GameData.Abbreviation;
            }
            else if (m_Config.RoleTag.Format == RoleTagFormat.JobName)
            {
                text = character.ClassJob.GameData.NameEnglish;
            }
            else if (m_Config.RoleTag.Format == RoleTagFormat.RoleName)
            {
                text = m_Config.RoleTag.RoleOverrideConfigs[role].Name;
            }

            List<Payload> newPayloads = new List<Payload>();

            // There will always be a text payload
            newPayloads.Add(new TextPayload(text));

            ushort? colorId = null;

            // Pick a color id if one is available
            if (roleConfig.JobOverrideConfigs[jobAbbreviation].CustomColor.Id != null)
            {
                colorId = roleConfig.JobOverrideConfigs[jobAbbreviation].CustomColor.Id!.Value;
            }
            else if (roleConfig.CustomColor.Id != null)
            {
                colorId = roleConfig.CustomColor.Id.Value;
            }

            // If we picked a color id, add the payloads for it
            if (colorId != null)
            {
                newPayloads.Insert(0, new UIForegroundPayload(colorId.Value));
                newPayloads.Add(new UIForegroundPayload(0));
            }

            var newPayloadsArray = newPayloads.ToArray();
            m_JobTagPayloads[jobAbbreviation] = newPayloadsArray;

            return newPayloadsArray;
        }

        /// <summary>
        /// Gets the payloads for the given custom tag. If the payloads don't yet exist then they are created.
        /// </summary>
        /// <param name="customTagConfig">The custom tag config to get payloads for.</param>
        /// <returns>A list of payloads for the given custom tag.</returns>
        private IEnumerable<Payload> GetCustomTagPayloads(CustomTagConfig customTagConfig)
        {
            if (m_CustomTagPayloads.TryGetValue(customTagConfig, out var payloads))
            {
                return payloads;
            }

            List<Payload> newPayloads = new List<Payload>();

            // There will always be a text payload
            newPayloads.Add(new TextPayload(customTagConfig.Name));

            ushort? colorId = null;

            // Pick a color id if one is available
            if (customTagConfig.CustomColor.Id != null)
            {
                colorId = customTagConfig.CustomColor.Id!.Value;
            }

            // If we picked a color id, add the payloads for it
            if (colorId != null)
            {
                newPayloads.Insert(0, new UIForegroundPayload(colorId.Value));
                newPayloads.Add(new UIForegroundPayload(0));
            }

            var newPayloadsArray = newPayloads.ToArray();
            m_CustomTagPayloads[customTagConfig] = newPayloadsArray;

            return newPayloadsArray;
        }


        /// <summary>
        /// Adds an additional space text payload in between any existing text payloads.
        /// </summary>
        /// <param name="payloads">The payloads to add spaces between.</param>
        private void AddSpacesBetweenTextPayloads(List<Payload> payloads)
        {
            var textPayloads = payloads.Where(payload => payload is TextPayload).ToList();
            foreach (var textPayload in textPayloads.Skip(1))
            {
                var index = payloads.IndexOf(textPayload);
                payloads.Insert(index, m_SpaceTextPayload);
            }
        }

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
            /// The matching game object.
            /// </summary>
            public GameObject? GameObject { get; init; }

            public StringMatch(SeString seString, TextPayload textPayload, GameObject? gameObject = null)
            {
                SeString = seString;
                TextPayload = textPayload;
                GameObject = gameObject;
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
                    var gameObject = ObjectTable.FirstOrDefault(gameObject => gameObject.Name.TextValue == playerPayload.PlayerName);

                    // The next payload MUST be a text payload
                    if (payloadIndex + 1 < seString.Payloads.Count && seString.Payloads[payloadIndex + 1] is TextPayload textPayload)
                    {
                        var stringMatch = new StringMatch(seString, textPayload, gameObject);
                        stringMatches.Add(stringMatch);

                        // Don't handle the text payload twice
                        payloadIndex++;
                    }
                    else
                    {
                        PluginLog.Error("Expected payload after player payload to be a text payload but it wasn't");
                    }
                }

                // If it's just a text payload then either a character NEEDS to exist for it, or it needs to be identified as a character by custom tag configs
                else if (payload is TextPayload textPayload)
                {
                    var gameObject = ObjectTable.FirstOrDefault(gameObject => gameObject.Name.TextValue == textPayload.Text);
                    var isIncludedInCustomTagConfig = m_Config.CustomTagConfigs.Any(customTagConfig => customTagConfig.IncludesGameObjectName(textPayload.Text));

                    if (gameObject != null || isIncludedInCustomTagConfig)
                    {
                        var stringMatch = new StringMatch(seString, textPayload, gameObject);
                        stringMatches.Add(stringMatch);
                    }
                }
            }

            return stringMatches;
        }

        /// <summary>
        /// Adds the given payload changes to the dictionary.
        /// </summary>
        /// <param name="stringPosition">The position of the string to add changes to.</param>
        /// <param name="payloads">The payloads to add.</param>
        /// <param name="stringChanges">The dictionary to add the changes to.</param>
        private void AddPayloadChanges(StringPosition stringPosition, IEnumerable<Payload> payloads, Dictionary<StringPosition, List<Payload>> stringChanges)
        {
            if (!payloads.Any())
            {
                return;
            }

            if (!stringChanges.Keys.Contains(stringPosition))
            {
                stringChanges[stringPosition] = new List<Payload>();
            }

            stringChanges[stringPosition].AddRange(payloads);
        }

        /// <summary>
        /// Adds the given payload changes to the dictionary.
        /// </summary>
        /// <param name="nameplateElement">The nameplate element to add changes to.</param>
        /// <param name="stringPosition">The position of the string to add changes to.</param>
        /// <param name="payloads">The payloads to add.</param>
        /// <param name="nameplateChanges">The dictionary to add the changes to.</param>
        private void AddPayloadChanges(NameplateElement nameplateElement, StringPosition stringPosition, IEnumerable<Payload> payloads, Dictionary<NameplateElement, Dictionary<StringPosition, List<Payload>>> nameplateChanges)
        {
            if (!payloads.Any())
            {
                return;
            }

            if (!nameplateChanges.Keys.Contains(nameplateElement))
            {
                nameplateChanges[nameplateElement] = new Dictionary<StringPosition, List<Payload>>();
            }

            AddPayloadChanges(stringPosition, payloads, nameplateChanges[nameplateElement]);
        }

        /// <summary>
        /// Applies changes to the given string.
        /// </summary>
        /// <param name="seString">The string to apply changes to.</param>
        /// <param name="stringChanges">The changes to apply.</param>
        /// <param name="anchorPayload">The payload in the string that changes should be anchored to. If there is no anchor, the changes will be applied to the entire string.</param>
        private void ApplyStringChanges(SeString seString, Dictionary<StringPosition, List<Payload>> stringChanges, Payload? anchorPayload = null)
        {
            foreach ((var stringPosition, var payloads) in stringChanges)
            {
                if (!payloads.Any())
                {
                    continue;
                }

                AddSpacesBetweenTextPayloads(payloads);

                if (stringPosition == StringPosition.Before)
                {
                    if (anchorPayload != null)
                    {
                        var payloadIndex = seString.Payloads.IndexOf(anchorPayload);
                        seString.Payloads.InsertRange(payloadIndex, payloads.Append(m_SpaceTextPayload));
                    }
                    else
                    {
                        seString.Payloads.InsertRange(0, payloads.Append(m_SpaceTextPayload));
                    }
                }
                else if (stringPosition == StringPosition.After)
                {
                    if (anchorPayload != null)
                    {
                        var payloadIndex = seString.Payloads.IndexOf(anchorPayload);
                        seString.Payloads.InsertRange(payloadIndex + 1, payloads.Prepend(m_SpaceTextPayload));
                    }
                    else
                    {
                        seString.Payloads.AddRange(payloads.Prepend(m_SpaceTextPayload));
                    }
                }
                else if (stringPosition == StringPosition.Replace)
                {
                    if (anchorPayload != null)
                    {
                        var payloadIndex = seString.Payloads.IndexOf(anchorPayload);
                        seString.Payloads.InsertRange(payloadIndex, payloads);
                        seString.Payloads.Remove(anchorPayload);
                    }
                    else
                    {
                        seString.Payloads.Clear();
                        seString.Payloads.AddRange(payloads);
                    }
                }
            }
        }

        /// <summary>
        /// Adds all configured tags to the nameplate of a game object.
        /// </summary>
        /// <param name="gameObject">The game object context.</param>
        /// <param name="name">The name text to change.</param>
        /// <param name="title">The title text to change.</param>
        /// <param name="freeCompany">The free company text to change.</param>
        /// <param name="isNameChanged">Whether the name was changed.</param>
        /// <param name="isTitleChanged">Whether the title was changed.</param>
        /// <param name="isFreeCompanyChanged">Whether the free company was changed.</param>
        private void AddTagsToNameplate(GameObject gameObject, SeString name, SeString title, SeString freeCompany, out bool isNameChanged, out bool isTitleChanged, out bool isFreeCompanyChanged)
        {
            isNameChanged = false;
            isTitleChanged = false;
            isFreeCompanyChanged = false;

            Dictionary<NameplateElement, Dictionary<StringPosition, List<Payload>>> nameplateChanges = new Dictionary<NameplateElement, Dictionary<StringPosition, List<Payload>>>();

            if (gameObject is Character character)
            {
                // Add the role tag payloads
                if (m_Config.RoleTag.NameplatePosition != StringPosition.None)
                {
                    AddPayloadChanges(m_Config.RoleTag.NameplateElement, m_Config.RoleTag.NameplatePosition, GetJobTagPayloads(character), nameplateChanges);
                }

                // Add randomly generated name tag payload
                if (m_Config.IsPlayerNameRandomlyGenerated && m_RandomNameGenerator != null)
                {
                    var characterName = character.Name.TextValue;
                    if (characterName != null)
                    {
                        var generatedName = m_RandomNameGenerator.GetGeneratedName(characterName);
                        if (generatedName != null)
                        {
                            AddPayloadChanges(NameplateElement.Name, StringPosition.Replace, Enumerable.Empty<Payload>().Append(new TextPayload(generatedName)), nameplateChanges);
                        }
                    }
                }
            }

            // Add the custom tag payloads
            foreach (var customTagConfig in m_Config.CustomTagConfigs)
            {
                if (customTagConfig.NameplatePosition != StringPosition.None && customTagConfig.FormattedGameObjectNames.Split(',').Contains(gameObject.Name.TextValue))
                {
                    AddPayloadChanges(customTagConfig.NameplateElement, customTagConfig.NameplatePosition, GetCustomTagPayloads(customTagConfig), nameplateChanges);
                }
            }

            // Build the final strings out of the payloads
            foreach ((var nameplateElement, var stringChanges) in nameplateChanges)
            {
                SeString? seString = null;
                if (nameplateElement == NameplateElement.Name)
                {
                    seString = name;
                    isNameChanged = true;
                }
                else if (nameplateElement == NameplateElement.Title)
                {
                    seString = title;
                    isTitleChanged = true;
                }
                else if (nameplateElement == NameplateElement.FreeCompany)
                {
                    seString = freeCompany;
                    isFreeCompanyChanged = true;
                }

                if (seString != null)
                {
                    ApplyStringChanges(seString, stringChanges);
                }
            }
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
                Dictionary<StringPosition, List<Payload>> stringChanges = new Dictionary<StringPosition, List<Payload>>();

                // The role tag payloads
                if (stringMatch.GameObject is Character character)
                {
                    if (m_Config.RoleTag.ChatPosition != StringPosition.None)
                    {
                        AddPayloadChanges(m_Config.RoleTag.ChatPosition, GetJobTagPayloads(character), stringChanges);
                    }
                }

                // Add randomly generated name tag payload
                if (m_Config.IsPlayerNameRandomlyGenerated && m_RandomNameGenerator != null)
                {
                    var playerName = stringMatch.GetMatchText();
                    if (playerName != null)
                    {
                        var generatedName = m_RandomNameGenerator.GetGeneratedName(playerName);
                        if (generatedName != null)
                        {
                            AddPayloadChanges(StringPosition.Replace, Enumerable.Empty<Payload>().Append(new TextPayload(generatedName)), stringChanges);
                        }
                    }
                }

                // Add the custom tag payloads
                foreach (var customTagConfig in m_Config.CustomTagConfigs)
                {
                    if (customTagConfig.IncludesGameObjectName(stringMatch.GetMatchText()))
                    {
                        AddPayloadChanges(customTagConfig.ChatPosition, GetCustomTagPayloads(customTagConfig), stringChanges);
                    }
                }

                ApplyStringChanges(message, stringChanges, stringMatch.TextPayload);
                isMessageChanged = true;
            }
        }
    }
}
