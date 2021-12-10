using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Lumina.Excel.GeneratedSheets;
using PlayerTags.Configuration;
using PlayerTags.Data;
using PlayerTags.Inheritables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayerTags.Features
{
    public abstract class TagTargetFeature : IDisposable
    {
        private PluginConfiguration m_PluginConfiguration;

        private ActivityContext m_ActivityContext;
        private Dictionary<Tag, Payload[]> m_TagPayloads;
        private TextPayload m_SpaceTextPayload;

        public TagTargetFeature(PluginConfiguration pluginConfiguration)
        {
            m_PluginConfiguration = pluginConfiguration;

            m_ActivityContext = ActivityContext.Overworld;
            m_TagPayloads = new Dictionary<Tag, Payload[]>();
            m_SpaceTextPayload = new TextPayload($" ");

            m_PluginConfiguration.Saved += PluginConfiguration_Saved;
            PluginServices.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
        }

        public virtual void Dispose()
        {
            PluginServices.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;
            m_PluginConfiguration.Saved -= PluginConfiguration_Saved;
        }

        protected abstract BitmapFontIcon? GetIcon(Tag tag);

        protected abstract string? GetText(Tag tag);

        private void PluginConfiguration_Saved()
        {
            // Invalidate the cached payloads so they get remade
            m_TagPayloads.Clear();
        }

        private void ClientState_TerritoryChanged(object? sender, ushort e)
        {
            m_ActivityContext = ActivityContext.Overworld;

            var contentFinderConditionsSheet = PluginServices.DataManager.GameData.GetExcelSheet<ContentFinderCondition>();
            if (contentFinderConditionsSheet != null)
            {
                var foundContentFinderCondition = contentFinderConditionsSheet.FirstOrDefault(contentFinderCondition => contentFinderCondition.TerritoryType.Row == PluginServices.ClientState.TerritoryType);
                if (foundContentFinderCondition != null)
                {
                    if (foundContentFinderCondition.PvP)
                    {
                        m_ActivityContext = ActivityContext.PvpDuty;
                    }
                    else
                    {
                        m_ActivityContext = ActivityContext.PveDuty;
                    }
                }
            }

            // Invalidate the cached payloads so they get remade
            m_TagPayloads.Clear();
        }

        /// <summary>
        /// Gets the payloads for the given tag. If the payloads don't yet exist then they will be created.
        /// </summary>
        /// <param name="tag">The tag config to get payloads for.</param>
        /// <returns>A list of payloads for the given tag.</returns>
        protected IEnumerable<Payload> GetPayloads(Tag tag)
        {
            if (m_TagPayloads.TryGetValue(tag, out var payloads))
            {
                return payloads;
            }

            m_TagPayloads[tag] = CreatePayloads(tag);
            return m_TagPayloads[tag];
        }

        private InheritableValue<bool>? GetInheritableVisibilityForActivity(Tag tag, ActivityContext activityContext)
        {
            switch (activityContext)
            {
                case ActivityContext.Overworld:
                    return tag.IsVisibleInOverworld;
                case ActivityContext.PveDuty:
                    return tag.IsVisibleInPveDuties;
                case ActivityContext.PvpDuty:
                    return tag.IsVisibleInPvpDuties;
            }

            return null;
        }

        public bool IsVisibleInActivity(Tag tag)
        {
            var inheritable = GetInheritableVisibilityForActivity(tag, m_ActivityContext);
            if (inheritable == null)
            {
                return false;
            }

            if (inheritable.InheritedValue == null || !inheritable.InheritedValue.Value)
            {
                return false;
            }

            return true;
        }

        private PlayerContext GetContextForPlayer(PlayerCharacter playerCharacter)
        {
            if (PluginServices.ClientState.LocalPlayer == playerCharacter)
            {
                return PlayerContext.Self;
            }

            if (playerCharacter.StatusFlags.HasFlag(StatusFlags.Friend))
            {
                return PlayerContext.Friend;
            }

            if (playerCharacter.StatusFlags.HasFlag(StatusFlags.PartyMember))
            {
                return PlayerContext.Party;
            }

            if (playerCharacter.StatusFlags.HasFlag(StatusFlags.AllianceMember))
            {
                return PlayerContext.Alliance;
            }

            if (playerCharacter.StatusFlags.HasFlag(StatusFlags.Hostile))
            {
                return PlayerContext.Enemy;
            }

            return PlayerContext.Other;
        }

        private InheritableValue<bool>? GetInheritableVisibilityForPlayer(Tag tag, PlayerContext playerContext)
        {
            switch (playerContext)
            {
                case PlayerContext.Self:
                    return tag.IsVisibleForSelf;
                case PlayerContext.Friend:
                    return tag.IsVisibleForFriendPlayers;
                case PlayerContext.Party:
                    return tag.IsVisibleForPartyPlayers;
                case PlayerContext.Alliance:
                    return tag.IsVisibleForAlliancePlayers;
                case PlayerContext.Enemy:
                    return tag.IsVisibleForEnemyPlayers;
                case PlayerContext.Other:
                    return tag.IsVisibleForOtherPlayers;
            }

            return null;
        }

        public bool IsVisibleForPlayer(Tag tag, PlayerCharacter playerCharacter)
        {
            var inheritable = GetInheritableVisibilityForPlayer(tag, GetContextForPlayer(playerCharacter));
            if (inheritable == null)
            {
                return false;
            }

            if (inheritable.InheritedValue == null || !inheritable.InheritedValue.Value)
            {
                return false;
            }

            return true;
        }

        private Payload[] CreatePayloads(Tag tag)
        {
            List<Payload> newPayloads = new List<Payload>();

            BitmapFontIcon? icon = GetIcon(tag);
            if (icon != null && icon.Value != BitmapFontIcon.None)
            {
                newPayloads.Add(new IconPayload(icon.Value));
            }

            string? text = GetText(tag);
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
        /// Adds the given payload changes to the dictionary.
        /// </summary>
        /// <param name="tagPosition">The position to add changes to.</param>
        /// <param name="payloads">The payloads to add.</param>
        /// <param name="stringChanges">The dictionary to add the changes to.</param>
        protected void AddPayloadChanges(TagPosition tagPosition, IEnumerable<Payload> payloads, Dictionary<TagPosition, List<Payload>> stringChanges)
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
        /// Applies changes to the given string.
        /// </summary>
        /// <param name="seString">The string to apply changes to.</param>
        /// <param name="stringChanges">The changes to apply.</param>
        /// <param name="anchorPayload">The payload in the string that changes should be anchored to. If there is no anchor, the changes will be applied to the entire string.</param>
        protected void ApplyStringChanges(SeString seString, Dictionary<TagPosition, List<Payload>> stringChanges, Payload? anchorPayload = null)
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
    }
}
