using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using PlayerTags.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using XivCommon;
using XivCommon.Functions.ContextMenu;

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

        [PluginService]
        private static ClientState ClientState { get; set; } = null!;

        [PluginService]
        private static PartyList PartyList { get; set; } = null!;

        private PluginConfiguration m_PluginConfiguration;
        private PluginConfigurationUI m_PluginConfigurationUI;
        private RandomNameGenerator m_RandomNameGenerator = new RandomNameGenerator();
        private PluginHooks? m_PluginHooks = null;
        private Dictionary<Tag, Dictionary<TagTarget, Payload[]>> m_TagTargetPayloads = new Dictionary<Tag, Dictionary<TagTarget, Payload[]>>();
        private TextPayload m_SpaceTextPayload = new TextPayload($" ");
        private PluginData m_PluginData = new PluginData();
        private XivCommonBase XivCommon;

        public Plugin()
        {
            UIColorHelper.Initialize(DataManager);
            m_PluginConfiguration = PluginInterface.GetPluginConfig() as PluginConfiguration ?? new PluginConfiguration();
            m_PluginConfiguration.Initialize(PluginInterface);
            m_PluginData.Initialize(DataManager, m_PluginConfiguration);
            m_PluginConfigurationUI = new PluginConfigurationUI(m_PluginConfiguration, m_PluginData, ClientState, PartyList);

            ClientState.Login += ClientState_Login;
            ClientState.Logout += ClientState_Logout;
            ChatGui.ChatMessage += Chat_ChatMessage;
            PluginInterface.UiBuilder.Draw += UiBuilder_Draw;
            PluginInterface.UiBuilder.OpenConfigUi += UiBuilder_OpenConfigUi;
            m_PluginConfiguration.Saved += PluginConfiguration_Saved;
            CommandManager.AddHandler(c_CommandName, new CommandInfo((string command, string arguments) =>
            {
                m_PluginConfiguration.IsVisible = true;
                m_PluginConfiguration.Save(m_PluginData);
            })
            {
                HelpMessage = "Shows the config"
            });
            Hook();
            XivCommon = new XivCommonBase(Hooks.ContextMenu);
            XivCommon.Functions.ContextMenu.OpenContextMenu += ContextMenu_OpenContextMenu;
        }

        public void Dispose()
        {
            XivCommon.Functions.ContextMenu.OpenContextMenu -= ContextMenu_OpenContextMenu;
            XivCommon.Dispose();
            Unhook();
            CommandManager.RemoveHandler(c_CommandName);
            m_PluginConfiguration.Saved -= PluginConfiguration_Saved;
            PluginInterface.UiBuilder.OpenConfigUi -= UiBuilder_OpenConfigUi;
            PluginInterface.UiBuilder.Draw -= UiBuilder_Draw;
            ChatGui.ChatMessage -= Chat_ChatMessage;
            ClientState.Logout -= ClientState_Logout;
            ClientState.Login -= ClientState_Login;
        }

        private void Hook()
        {
            if (m_PluginHooks == null)
            {
                m_PluginHooks = new PluginHooks(Framework, ObjectTable, GameGui, SetPlayerNameplate);
            }
        }

        private void Unhook()
        {
            if (m_PluginHooks != null)
            {
                m_PluginHooks.Dispose();
                m_PluginHooks = null;
            }
        }

        private void ClientState_Login(object? sender, EventArgs e)
        {
            Hook();
        }

        private void ClientState_Logout(object? sender, EventArgs e)
        {
            Unhook();
        }

        private void PluginConfiguration_Saved()
        {
            // Invalidate the cached payloads so they get remade
            m_TagTargetPayloads.Clear();
        }

        private void ContextMenu_OpenContextMenu(ContextMenuOpenArgs args)
        {
            if (!m_PluginConfiguration.IsCustomTagContextMenuEnabled || !CanContextMenuSupportTagOptions(args))
            {
                return;
            }

            string gameObjectName = args.Text!.TextValue;

            var notAddedTags = m_PluginData.CustomTags.Where(tag => !tag.IncludesGameObjectNameToApplyTo(gameObjectName));
            if (notAddedTags.Any())
            {
                args.Items.Add(new NormalContextSubMenuItem(Strings.Loc_Static_ContextMenu_AddTag, (itemArgs =>
                {
                    foreach (var notAddedTag in notAddedTags)
                    {
                        itemArgs.Items.Add(new NormalContextMenuItem(notAddedTag.Text.Value, (args =>
                        {
                            notAddedTag.AddGameObjectNameToApplyTo(gameObjectName);
                        })));
                    }
                })));
            }

            var addedTags = m_PluginData.CustomTags.Where(tag => tag.IncludesGameObjectNameToApplyTo(gameObjectName));
            if (addedTags.Any())
            {
                args.Items.Add(new NormalContextSubMenuItem(Strings.Loc_Static_ContextMenu_RemoveTag, (itemArgs =>
                {
                    foreach (var addedTag in addedTags)
                    {
                        itemArgs.Items.Add(new NormalContextMenuItem(addedTag.Text.Value, (args =>
                        {
                            addedTag.RemoveGameObjectNameToApplyTo(gameObjectName);
                        })));
                    }
                })));
            }
        }

        private bool CanContextMenuSupportTagOptions(BaseContextMenuArgs args)
        {
            if (args.Text == null || args.ObjectWorld == 0 || args.ObjectWorld == 65535)
            {
                return false;
            }

            switch (args.ParentAddonName)
            {
                case null:
                case "_PartyList":
                case "ChatLog":
                case "ContactList":
                case "ContentMemberList":
                case "CrossWorldLinkshell":
                case "FreeCompany":
                case "FriendList":
                case "LookingForGroup":
                case "LinkShell":
                case "PartyMemberList":
                case "SocialList":
                    return true;

                default:
                    return false;
            }
        }

        private void UiBuilder_Draw()
        {
            if (m_PluginConfiguration.IsVisible)
            {
                m_PluginConfigurationUI.Draw();
            }
        }

        private void UiBuilder_OpenConfigUi()
        {
            m_PluginConfiguration.IsVisible = true;
            m_PluginConfiguration.Save(m_PluginData);
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
        private void SetPlayerNameplate(PlayerCharacter playerCharacter, SeString name, SeString title, SeString freeCompany, ref bool isTitleVisible, ref bool isTitleAboveName, ref int iconId, out bool isNameChanged, out bool isTitleChanged, out bool isFreeCompanyChanged)
        {
            AddTagsToNameplate(playerCharacter, name, title, freeCompany, out isNameChanged, out isTitleChanged, out isFreeCompanyChanged);

            if (m_PluginConfiguration.NameplateTitlePosition == NameplateTitlePosition.AlwaysAboveName)
            {
                isTitleAboveName = true;
            }
            else if (m_PluginConfiguration.NameplateTitlePosition == NameplateTitlePosition.AlwaysBelowName)
            {
                isTitleAboveName = false;
            }

            if (m_PluginConfiguration.NameplateTitleVisibility == NameplateTitleVisibility.Default)
            {
            }
            else if (m_PluginConfiguration.NameplateTitleVisibility == NameplateTitleVisibility.Always)
            {
                isTitleVisible = true;
            }
            else if (m_PluginConfiguration.NameplateTitleVisibility == NameplateTitleVisibility.Never)
            {
                isTitleVisible = false;
            }
            else if (m_PluginConfiguration.NameplateTitleVisibility == NameplateTitleVisibility.WhenHasTags)
            {
                isTitleVisible = isTitleChanged;
            }

            if (m_PluginConfiguration.NameplateFreeCompanyVisibility == NameplateFreeCompanyVisibility.Default)
            {
            }
            else if (m_PluginConfiguration.NameplateFreeCompanyVisibility == NameplateFreeCompanyVisibility.Never)
            {
                freeCompany.Payloads.Clear();
                isFreeCompanyChanged = true;
            }
        }

        private Payload[] CreateTagPayloads(TagTarget tagTarget, Tag tag)
        {
            List<Payload> newPayloads = new List<Payload>();

            BitmapFontIcon? icon = null;
            if (tagTarget == TagTarget.Chat && tag.IsIconVisibleInChat.InheritedValue != null && tag.IsIconVisibleInChat.InheritedValue.Value)
            {
                icon = tag.Icon.InheritedValue;
            }
            else if (tagTarget == TagTarget.Nameplate && tag.IsIconVisibleInNameplates.InheritedValue != null && tag.IsIconVisibleInNameplates.InheritedValue.Value)
            {
                icon = tag.Icon.InheritedValue;
            }

            string? text = null;
            if (tagTarget == TagTarget.Chat && tag.IsTextVisibleInChat.InheritedValue != null && tag.IsTextVisibleInChat.InheritedValue.Value)
            {
                text = tag.Text.InheritedValue;
            }
            else if (tagTarget == TagTarget.Nameplate && tag.IsTextVisibleInNameplates.InheritedValue != null && tag.IsTextVisibleInNameplates.InheritedValue.Value)
            {
                text = tag.Text.InheritedValue;
            }

            if (!m_TagTargetPayloads.ContainsKey(tag))
            {
                m_TagTargetPayloads[tag] = new Dictionary<TagTarget, Payload[]>();
            }

            if (icon != null && icon.Value != BitmapFontIcon.None)
            {
                newPayloads.Add(new IconPayload(icon.Value));
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                if (tag.IsTextItalic.InheritedValue != null && tag.IsTextItalic.InheritedValue.Value)
                {
                    newPayloads.Add(new EmphasisItalicPayload(true));
                }

                if (tag.TextGlowColor.InheritedValue != null)
                {
                    newPayloads.Add(new UIGlowPayload(tag.TextGlowColor.InheritedValue.Value));
                }

                if (tag.TextColor.InheritedValue != null)
                {
                    newPayloads.Add(new UIForegroundPayload(tag.TextColor.InheritedValue.Value));
                }

                newPayloads.Add(new TextPayload(text));

                if (tag.TextColor.InheritedValue != null)
                {
                    newPayloads.Add(new UIForegroundPayload(0));
                }

                if (tag.TextGlowColor.InheritedValue != null)
                {
                    newPayloads.Add(new UIGlowPayload(0));
                }

                if (tag.IsTextItalic.InheritedValue != null && tag.IsTextItalic.InheritedValue.Value)
                {
                    newPayloads.Add(new EmphasisItalicPayload(false));
                }
            }

            return newPayloads.ToArray();
        }

        /// <summary>
        /// Gets the payloads for the given custom tag. If the payloads don't yet exist then they are created.
        /// </summary>
        /// <param name="customTagConfig">The custom tag config to get payloads for.</param>
        /// <returns>A list of payloads for the given custom tag.</returns>
        private IEnumerable<Payload> GetTagPayloads(TagTarget tagTarget, Tag tag)
        {
            if (m_TagTargetPayloads.TryGetValue(tag, out var tagTargetPayloads))
            {
                if (tagTargetPayloads.TryGetValue(tagTarget, out var payloads))
                {
                    return payloads;
                }
            }
            else
            {
                m_TagTargetPayloads[tag] = new Dictionary<TagTarget, Payload[]>();
            }

            m_TagTargetPayloads[tag][tagTarget] = CreateTagPayloads(tagTarget, tag);
            return m_TagTargetPayloads[tag][tagTarget];
        }

        /// <summary>
        /// Adds an additional space text payload in between any existing text payloads. If there is an icon payload between two text payloads then the space is skipped.
        /// Also adds an extra space to the beginning or end depending on the tag position and whether the most significant payload in either direction is a text payload.
        /// In spirit, this is to ensure there is always a space between 2 text payloads, including between these payloads and the target payload.
        /// </summary>
        /// <param name="payloads">The payloads to add spaces between.</param>
        private void AddSpacesBetweenTextPayloads(List<Payload> payloads, TagPosition tagPosition)
        {
            if (payloads == null)
            {
                return;
            }

            if (!payloads.Any())
            {
                return;
            }

            List<int> indicesToInsertSpacesAt = new List<int>();
            int lastTextPayloadIndex = -1;
            foreach (var payload in payloads.Reverse<Payload>())
            {
                if (payload is IconPayload iconPayload)
                {
                    lastTextPayloadIndex = -1;
                }
                else if (payload is TextPayload textPayload)
                {
                    if (lastTextPayloadIndex != -1)
                    {
                        indicesToInsertSpacesAt.Add(payloads.IndexOf(textPayload) + 1);
                    }

                    lastTextPayloadIndex = payloads.IndexOf(textPayload);
                }
            }

            foreach (var indexToInsertSpaceAt in indicesToInsertSpacesAt)
            {
                payloads.Insert(indexToInsertSpaceAt, m_SpaceTextPayload);
            }

            // Decide whether to add a space to the end
            if (tagPosition == TagPosition.Before)
            {
                var significantPayloads = payloads.Where(payload => payload is TextPayload || payload is IconPayload);
                if (significantPayloads.Last() is TextPayload)
                {
                    payloads.Add(m_SpaceTextPayload);
                }
            }
            // Decide whether to add a space to the beginning
            else if (tagPosition == TagPosition.After)
            {
                var significantPayloads = payloads.Where(payload => payload is TextPayload || payload is IconPayload);
                if (significantPayloads.First() is TextPayload)
                {
                    payloads.Insert(0, m_SpaceTextPayload);
                }
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
        /// Adds the given payload changes to the dictionary.
        /// </summary>
        /// <param name="tagPosition">The position to add changes to.</param>
        /// <param name="payloads">The payloads to add.</param>
        /// <param name="stringChanges">The dictionary to add the changes to.</param>
        private void AddPayloadChanges(TagPosition tagPosition, IEnumerable<Payload> payloads, Dictionary<TagPosition, List<Payload>> stringChanges)
        {
            if (payloads == null || !payloads.Any())
            {
                return;
            }

            if (stringChanges == null)
            {
                return;
            }

            if (!stringChanges.Keys.Contains(tagPosition))
            {
                stringChanges[tagPosition] = new List<Payload>();
            }

            stringChanges[tagPosition].AddRange(payloads);
        }

        /// <summary>
        /// Adds the given payload changes to the dictionary.
        /// </summary>
        /// <param name="nameplateElement">The nameplate element to add changes to.</param>
        /// <param name="tagPosition">The position to add changes to.</param>
        /// <param name="payloads">The payloads to add.</param>
        /// <param name="nameplateChanges">The dictionary to add the changes to.</param>
        private void AddPayloadChanges(NameplateElement nameplateElement, TagPosition tagPosition, IEnumerable<Payload> payloads, Dictionary<NameplateElement, Dictionary<TagPosition, List<Payload>>> nameplateChanges)
        {
            if (!payloads.Any())
            {
                return;
            }

            if (!nameplateChanges.Keys.Contains(nameplateElement))
            {
                nameplateChanges[nameplateElement] = new Dictionary<TagPosition, List<Payload>>();
            }

            AddPayloadChanges(tagPosition, payloads, nameplateChanges[nameplateElement]);
        }

        /// <summary>
        /// Applies changes to the given string.
        /// </summary>
        /// <param name="seString">The string to apply changes to.</param>
        /// <param name="stringChanges">The changes to apply.</param>
        /// <param name="anchorPayload">The payload in the string that changes should be anchored to. If there is no anchor, the changes will be applied to the entire string.</param>
        private void ApplyStringChanges(SeString seString, Dictionary<TagPosition, List<Payload>> stringChanges, Payload? anchorPayload = null)
        {
            if (stringChanges.Count == 0)
            {
                return;
            }

            List<TagPosition> tagPositionsOrdered = new List<TagPosition>();
            // If there's no anchor payload, do replaces first so that befores and afters are based on the replaced data
            if (anchorPayload == null)
            {
                tagPositionsOrdered.Add(TagPosition.Replace);
            }

            tagPositionsOrdered.Add(TagPosition.Before);
            tagPositionsOrdered.Add(TagPosition.After);

            // If there is an anchor payload, do replaces last so that we still know which payload needs to be removed
            if (anchorPayload != null)
            {
                tagPositionsOrdered.Add(TagPosition.Replace);
            }

            foreach (var tagPosition in tagPositionsOrdered)
            {
                if (stringChanges.TryGetValue(tagPosition, out var payloads) && payloads.Any())
                {
                    AddSpacesBetweenTextPayloads(stringChanges[tagPosition], tagPosition);
                    if (tagPosition == TagPosition.Before)
                    {
                        if (anchorPayload != null)
                        {
                            var anchorPayloadIndex = seString.Payloads.IndexOf(anchorPayload);
                            seString.Payloads.InsertRange(anchorPayloadIndex, payloads);
                        }
                        else
                        {
                            seString.Payloads.InsertRange(0, payloads);
                        }
                    }
                    else if (tagPosition == TagPosition.After)
                    {
                        if (anchorPayload != null)
                        {
                            var anchorPayloadIndex = seString.Payloads.IndexOf(anchorPayload);
                            seString.Payloads.InsertRange(anchorPayloadIndex + 1, payloads);
                        }
                        else
                        {
                            seString.Payloads.AddRange(payloads);
                        }
                    }
                    else if (tagPosition == TagPosition.Replace)
                    {
                        if (anchorPayload != null)
                        {
                            var anchorPayloadIndex = seString.Payloads.IndexOf(anchorPayload);
                            seString.Payloads.InsertRange(anchorPayloadIndex, payloads);
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

            Dictionary<NameplateElement, Dictionary<TagPosition, List<Payload>>> nameplateChanges = new Dictionary<NameplateElement, Dictionary<TagPosition, List<Payload>>>();

            if (gameObject is Character character)
            {
                // Add the job tag
                if (m_PluginData.JobTags.TryGetValue(character.ClassJob.GameData.Abbreviation, out var jobTag))
                {
                    if (jobTag.TagTargetInNameplates.InheritedValue != null && jobTag.TagPositionInNameplates.InheritedValue != null)
                    {
                        var payloads = GetTagPayloads(TagTarget.Nameplate, jobTag);
                        if (payloads.Any())
                        {
                            AddPayloadChanges(jobTag.TagTargetInNameplates.InheritedValue.Value, jobTag.TagPositionInNameplates.InheritedValue.Value, payloads, nameplateChanges);
                        }
                    }
                }

                // Add the randomly generated name tag payload
                if (m_PluginConfiguration.IsPlayerNameRandomlyGenerated && m_RandomNameGenerator != null)
                {
                    var characterName = character.Name.TextValue;
                    if (characterName != null)
                    {
                        var generatedName = m_RandomNameGenerator.GetGeneratedName(characterName);
                        if (generatedName != null)
                        {
                            AddPayloadChanges(NameplateElement.Name, TagPosition.Replace, Enumerable.Empty<Payload>().Append(new TextPayload(generatedName)), nameplateChanges);
                        }
                    }
                }
            }

            // Add the custom tag payloads
            foreach (var customTag in m_PluginData.CustomTags)
            {
                if (customTag.TagTargetInNameplates.InheritedValue != null && customTag.TagPositionInNameplates.InheritedValue != null)
                {
                    if (customTag.IncludesGameObjectNameToApplyTo(gameObject.Name.TextValue))
                    {
                        var payloads = GetTagPayloads(TagTarget.Nameplate, customTag);
                        if (payloads.Any())
                        {
                            AddPayloadChanges(customTag.TagTargetInNameplates.InheritedValue.Value, customTag.TagPositionInNameplates.InheritedValue.Value, payloads, nameplateChanges);
                        }
                    }
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
                Dictionary<TagPosition, List<Payload>> stringChanges = new Dictionary<TagPosition, List<Payload>>();

                // The role tag payloads
                if (stringMatch.GameObject is Character character)
                {
                    // Add the job tag
                    if (m_PluginData.JobTags.TryGetValue(character.ClassJob.GameData.Abbreviation, out var jobTag))
                    {
                        if (jobTag.TagPositionInChat.InheritedValue != null)
                        {
                            var payloads = GetTagPayloads(TagTarget.Chat, jobTag);
                            if (payloads.Any())
                            {
                                AddPayloadChanges(jobTag.TagPositionInChat.InheritedValue.Value, payloads, stringChanges);
                            }
                        }
                    }

                    // Add randomly generated name tag payload
                    if (m_PluginConfiguration.IsPlayerNameRandomlyGenerated && m_RandomNameGenerator != null)
                    {
                        var playerName = stringMatch.GetMatchText();
                        if (playerName != null)
                        {
                            var generatedName = m_RandomNameGenerator.GetGeneratedName(playerName);
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
                            var customTagPayloads = GetTagPayloads(TagTarget.Chat, customTag);
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
