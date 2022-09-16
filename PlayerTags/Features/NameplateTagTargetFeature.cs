using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Lumina.Excel.GeneratedSheets;
using PlayerTags.Configuration;
using PlayerTags.Data;
using PlayerTags.GameInterface.Nameplates;
using PlayerTags.Inheritables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayerTags.Features
{
    /// <summary>
    /// A feature that adds tags to nameplates.
    /// </summary>
    public class NameplateTagTargetFeature : TagTargetFeature
    {
        private PluginConfiguration m_PluginConfiguration;
        private PluginData m_PluginData;
        private Nameplate? m_Nameplate;

        public NameplateTagTargetFeature(PluginConfiguration pluginConfiguration, PluginData pluginData)
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
            if (m_Nameplate == null)
            {
                m_Nameplate = new Nameplate();
                if (!m_Nameplate.IsValid)
                {
                    m_Nameplate = null;
                }

                if (m_Nameplate != null)
                {
                    m_Nameplate.PlayerNameplateUpdated += Nameplate_PlayerNameplateUpdated;
                }
            }
        }

        private void Unhook()
        {
            if (m_Nameplate != null)
            {
                m_Nameplate.PlayerNameplateUpdated -= Nameplate_PlayerNameplateUpdated;
                m_Nameplate.Dispose();
                m_Nameplate = null;
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

        protected override bool IsIconVisible(Tag tag)
        {
            if (tag.IsIconVisibleInNameplates.InheritedValue != null)
            {
                return tag.IsIconVisibleInNameplates.InheritedValue.Value;
            }

            return false;
        }

        protected override bool IsTextVisible(Tag tag)
        {
            if (tag.IsTextVisibleInNameplates.InheritedValue != null)
            {
                return tag.IsTextVisibleInNameplates.InheritedValue.Value;
            }

            return false;
        }

        private void Nameplate_PlayerNameplateUpdated(PlayerNameplateUpdatedArgs args)
        {
            var beforeTitleBytes = args.Title.Encode();
            AddTagsToNameplate(args.PlayerCharacter, args.Name, args.Title, args.FreeCompany);
            
            if (m_PluginConfiguration.GeneralOptions[ActivityContextManager.CurrentActivityContext].NameplateTitlePosition == NameplateTitlePosition.AlwaysAboveName)
                args.IsTitleAboveName = true;
            else if (m_PluginConfiguration.GeneralOptions[ActivityContextManager.CurrentActivityContext].NameplateTitlePosition == NameplateTitlePosition.AlwaysBelowName)
                args.IsTitleAboveName = false;

            if (m_PluginConfiguration.GeneralOptions[ActivityContextManager.CurrentActivityContext].NameplateTitleVisibility == NameplateTitleVisibility.Always)
                args.IsTitleVisible = true;
            else if (m_PluginConfiguration.GeneralOptions[ActivityContextManager.CurrentActivityContext].NameplateTitleVisibility == NameplateTitleVisibility.Never)
                args.IsTitleVisible = false;
            else if (m_PluginConfiguration.GeneralOptions[ActivityContextManager.CurrentActivityContext].NameplateTitleVisibility == NameplateTitleVisibility.WhenHasTags)
            {
                bool hasTitleChanged = !beforeTitleBytes.SequenceEqual(args.Title.Encode());
                args.IsTitleVisible = hasTitleChanged;
            }

            if (m_PluginConfiguration.GeneralOptions[ActivityContextManager.CurrentActivityContext].NameplateFreeCompanyVisibility == NameplateFreeCompanyVisibility.Never)
                args.FreeCompany.Payloads.Clear();
        }

        /// <summary>
        /// Adds the given payload changes to the specified locations.
        /// </summary>
        /// <param name="nameplateElement">The nameplate element of the changes.</param>
        /// <param name="tagPosition">The position of the changes.</param>
        /// <param name="payloadChanges">The payload changes to add.</param>
        /// <param name="nameplateChanges">The dictionary to add changes to.</param>
        private void AddPayloadChanges(NameplateElement nameplateElement, TagPosition tagPosition, IEnumerable<Payload> payloadChanges, Dictionary<NameplateElement, Dictionary<TagPosition, StringChanges>> nameplateChanges, bool forceUsingSingleAnchorPayload)
        {
            if (!payloadChanges.Any())
            {
                return;
            }

            if (!nameplateChanges.Keys.Contains(nameplateElement))
            {
                nameplateChanges[nameplateElement] = new();
            }

            AddPayloadChanges(tagPosition, payloadChanges, nameplateChanges[nameplateElement], forceUsingSingleAnchorPayload);
        }

        /// <summary>
        /// Adds tags to the nameplate of a game object.
        /// </summary>
        /// <param name="gameObject">The game object context.</param>
        /// <param name="name">The name text to change.</param>
        /// <param name="title">The title text to change.</param>
        /// <param name="freeCompany">The free company text to change.</param>
        private void AddTagsToNameplate(GameObject gameObject, SeString name, SeString title, SeString freeCompany)
        {
            Dictionary<NameplateElement, Dictionary<TagPosition, StringChanges>> nameplateChanges = new();

            if (gameObject is PlayerCharacter playerCharacter)
            {
                // Add the job tags
                if (playerCharacter.ClassJob.GameData != null && m_PluginData.JobTags.TryGetValue(playerCharacter.ClassJob.GameData.Abbreviation, out var jobTag))
                {
                    if (jobTag.TagTargetInNameplates.InheritedValue != null && jobTag.TagPositionInNameplates.InheritedValue != null)
                        checkTag(jobTag);
                }

                // Add the randomly generated name tag payload
                if (m_PluginConfiguration.IsPlayerNameRandomlyGenerated)
                {
                    var characterName = playerCharacter.Name.TextValue;
                    if (characterName != null)
                    {
                        var generatedName = RandomNameGenerator.Generate(characterName);
                        if (generatedName != null)
                            AddPayloadChanges(NameplateElement.Name, TagPosition.Replace, Enumerable.Empty<Payload>().Append(new TextPayload(generatedName)), nameplateChanges, false);
                    }
                }

                // Add custom tags
                Identity identity = m_PluginData.GetIdentity(playerCharacter);
                foreach (var customTagId in identity.CustomTagIds)
                {
                    var customTag = m_PluginData.CustomTags.FirstOrDefault(tag => tag.CustomId.Value == customTagId);
                    if (customTag != null)
                        checkTag(customTag);
                }

                void checkTag(Tag tag)
                {
                    if (tag.TagTargetInNameplates.InheritedValue != null && tag.TagPositionInNameplates.InheritedValue != null)
                    {
                        var payloads = GetPayloads(tag, gameObject);
                        if (payloads.Any())
                            AddPayloadChanges(tag.TagTargetInNameplates.InheritedValue.Value, tag.TagPositionInNameplates.InheritedValue.Value, payloads, nameplateChanges, false);
                    }
                }
            }

            // Build the final strings out of the payloads
            foreach ((var nameplateElement, var stringChanges) in nameplateChanges)
            {
                SeString? seString = null;

                if (nameplateElement == NameplateElement.Name)
                    seString = name;
                else if (nameplateElement == NameplateElement.Title)
                    seString = title;
                else if (nameplateElement == NameplateElement.FreeCompany)
                    seString = freeCompany;

                if (seString != null)
                    ApplyStringChanges(seString, stringChanges);
            }

            if (gameObject is PlayerCharacter playerCharacter1)
            {
                // An additional step to apply text color to additional locations
                Identity identity = m_PluginData.GetIdentity(playerCharacter1);
                foreach (var customTagId in identity.CustomTagIds)
                {
                    var customTag = m_PluginData.CustomTags.FirstOrDefault(tag => tag.CustomId.Value == customTagId);
                    if (customTag != null)
                        applyTextFormatting(customTag);
                }

                if (playerCharacter1.ClassJob.GameData != null && m_PluginData.JobTags.TryGetValue(playerCharacter1.ClassJob.GameData.Abbreviation, out var jobTag))
                    applyTextFormatting(jobTag);

                void applyTextFormatting(Tag tag)
                {
                    var destStrings = new[] { name, title, freeCompany };
                    var isTextColorApplied = new[] { tag.IsTextColorAppliedToNameplateName, tag.IsTextColorAppliedToNameplateTitle, tag.IsTextColorAppliedToNameplateFreeCompany };
                    ApplyTextFormatting(gameObject, tag, new[] { name, title, freeCompany }, isTextColorApplied, null);
                }
            }
        }
    }
}
