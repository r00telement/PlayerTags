using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using PlayerTags.Configuration;
using PlayerTags.Data;
using PlayerTags.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayerTags.Features
{
    public class NameplatesTagTargetFeature : TagTargetFeature
    {
        private PluginConfiguration m_PluginConfiguration;
        private PluginData m_PluginData;

        private NameplateHooks? m_NameplateHooks;

        public NameplatesTagTargetFeature(PluginConfiguration pluginConfiguration, PluginData pluginData)
            : base(pluginConfiguration)
        {
            m_PluginConfiguration = pluginConfiguration;
            m_PluginData = pluginData;

            PluginServices.ClientState.Login += ClientState_Login;
            PluginServices.ClientState.Logout += ClientState_Logout;
            Hook();
        }

        public override void Dispose()
        {
            Unhook();
            PluginServices.ClientState.Logout -= ClientState_Logout;
            PluginServices.ClientState.Login -= ClientState_Login;
            base.Dispose();
        }

        private void Hook()
        {
            if (m_NameplateHooks == null)
            {
                m_NameplateHooks = new NameplateHooks(SetPlayerNameplate);
            }
        }

        private void Unhook()
        {
            if (m_NameplateHooks != null)
            {
                m_NameplateHooks.Dispose();
                m_NameplateHooks = null;
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

        protected override BitmapFontIcon? GetIcon(Tag tag)
        {
            if (tag.IsIconVisibleInNameplates.InheritedValue != null && tag.IsIconVisibleInNameplates.InheritedValue.Value)
            {
                return tag.Icon.InheritedValue;
            }

            return null;
        }

        protected override string? GetText(Tag tag)
        {
            if (tag.IsTextVisibleInNameplates.InheritedValue != null && tag.IsTextVisibleInNameplates.InheritedValue.Value)
            {
                return tag.Text.InheritedValue;
            }

            return null;
        }

        /// <summary>
        /// Sets the strings on a nameplate.
        /// </summary>
        /// <param name="playerCharacter">The player character context.</param>
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
                    bool isVisible = IsVisibleInActivity(jobTag) &&
                        (!(gameObject is PlayerCharacter playerCharacter) || IsVisibleForPlayer(jobTag, playerCharacter));

                    if (isVisible)
                    {
                        if (jobTag.TagTargetInNameplates.InheritedValue != null && jobTag.TagPositionInNameplates.InheritedValue != null)
                        {
                            var payloads = GetPayloads(jobTag);
                            if (payloads.Any())
                            {
                                AddPayloadChanges(jobTag.TagTargetInNameplates.InheritedValue.Value, jobTag.TagPositionInNameplates.InheritedValue.Value, payloads, nameplateChanges);
                            }
                        }
                    }
                }

                // Add the randomly generated name tag payload
                if (m_PluginConfiguration.IsPlayerNameRandomlyGenerated)
                {
                    var characterName = character.Name.TextValue;
                    if (characterName != null)
                    {
                        var generatedName = RandomNameGenerator.Generate(characterName);
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
                bool isVisible = IsVisibleInActivity(customTag) &&
                    (!(gameObject is PlayerCharacter playerCharacter) || IsVisibleForPlayer(customTag, playerCharacter));

                if (isVisible)
                {
                    if (customTag.TagTargetInNameplates.InheritedValue != null && customTag.TagPositionInNameplates.InheritedValue != null)
                    {
                        if (customTag.IncludesGameObjectNameToApplyTo(gameObject.Name.TextValue))
                        {
                            var payloads = GetPayloads(customTag);
                            if (payloads.Any())
                            {
                                AddPayloadChanges(customTag.TagTargetInNameplates.InheritedValue.Value, customTag.TagPositionInNameplates.InheritedValue.Value, payloads, nameplateChanges);
                            }
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
    }
}
