using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Lumina.Excel.GeneratedSheets;
using Pilz.Dalamud.ActivityContexts;
using PlayerTags.Configuration.GameConfig;
using PlayerTags.Data;
using PlayerTags.Inheritables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using GameObject = Dalamud.Game.ClientState.Objects.Types.GameObject;

namespace PlayerTags.Features
{
    /// <summary>
    /// The base of a feature that adds tags to UI elements.
    /// </summary>
    public abstract class TagTargetFeature : IDisposable
    {
        protected class StringChanges
        {
            public List<Payload> Payloads { get; init; } = new();
            public bool ForceUsingSingleAnchorPayload { get; set; } = false;
        }

        public ActivityContextManager ActivityContextManager { get; init; }

        public TagTargetFeature()
        {
            ActivityContextManager = new();
        }

        public virtual void Dispose()
        {
            ActivityContextManager.Dispose();
        }

        protected abstract bool IsIconVisible(Tag tag);

        protected abstract bool IsTextVisible(Tag tag);

        protected bool IsTagVisible(Tag tag, GameObject? gameObject)
        {
            bool isVisibleForActivity = ActivityContextHelper.GetIsVisible(ActivityContextManager.CurrentActivityContext.ActivityType,
                tag.IsVisibleInPveDuties.InheritedValue ?? false,
                tag.IsVisibleInPvpDuties.InheritedValue ?? false,
                tag.IsVisibleInOverworld.InheritedValue ?? false);

            if (!isVisibleForActivity)
            {
                return false;
            }

            if (gameObject is PlayerCharacter playerCharacter)
            {
                bool isVisibleForPlayer = PlayerContextHelper.GetIsVisible(playerCharacter,
                    tag.IsVisibleForSelf.InheritedValue ?? false,
                    tag.IsVisibleForFriendPlayers.InheritedValue ?? false,
                    tag.IsVisibleForPartyPlayers.InheritedValue ?? false,
                    tag.IsVisibleForAlliancePlayers.InheritedValue ?? false,
                    tag.IsVisibleForEnemyPlayers.InheritedValue ?? false,
                    tag.IsVisibleForOtherPlayers.InheritedValue ?? false);

                if (!isVisibleForPlayer)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets the payloads for the given tag and game object depending on visibility conditions.
        /// </summary>
        /// <param name="gameObject">The game object to get payloads for.</param>
        /// <param name="tag">The tag config to get payloads for.</param>
        /// <returns>A list of payloads for the given tag.</returns>
        protected Payload[] GetPayloads(Tag tag, GameObject? gameObject)
        {
            if (!IsTagVisible(tag, gameObject))
            {
                return Array.Empty<Payload>();
            }

            return CreatePayloads(tag);
        }

        /// <summary>
        /// Creates payloads for the given tag.
        /// </summary>
        /// <param name="tag">The tag to create payloads for.</param>
        /// <returns>The payloads for the given tag.</returns>
        private Payload[] CreatePayloads(Tag tag)
        {
            List<Payload> newPayloads = new List<Payload>();

            BitmapFontIcon? icon = null;
            if (IsIconVisible(tag))
            {
                icon = tag.Icon.InheritedValue;
            }

            if (icon != null && icon.Value != BitmapFontIcon.None)
            {
                newPayloads.Add(new IconPayload(icon.Value));
            }

            string? text = null;
            if (IsTextVisible(tag))
            {
                text = tag.Text.InheritedValue;
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
                payloads.Insert(indexToInsertSpaceAt, new TextPayload($" "));
            }

            // Decide whether to add a space to the end
            if (tagPosition == TagPosition.Before)
            {
                var significantPayloads = payloads.Where(payload => payload is TextPayload || payload is IconPayload);
                if (significantPayloads.Last() is TextPayload)
                {
                    payloads.Add(new TextPayload($" "));
                }
            }
            // Decide whether to add a space to the beginning
            else if (tagPosition == TagPosition.After)
            {
                var significantPayloads = payloads.Where(payload => payload is TextPayload || payload is IconPayload);
                if (significantPayloads.First() is TextPayload)
                {
                    payloads.Insert(0, new TextPayload($" "));
                }
            }
        }

        protected static string BuildPlayername(string name)
        {
            var logNameType = GameConfigHelper.Instance.GetLogNameType();
            var result = string.Empty;

            if (logNameType != null && !string.IsNullOrEmpty(name))
            {
                var nameSplitted = name.Split(' ');

                if (nameSplitted.Length > 1)
                {
                    var firstName = nameSplitted[0];
                    var lastName = nameSplitted[1];

                    switch (logNameType)
                    {
                        case LogNameType.FullName:
                            result = $"{firstName} {lastName}";
                            break;
                        case LogNameType.LastNameShorted:
                            result = $"{firstName} {lastName[..1]}.";
                            break;
                        case LogNameType.FirstNameShorted:
                            result = $"{firstName[..1]}. {lastName}";
                            break;
                        case LogNameType.Initials:
                            result = $"{firstName[..1]}. {lastName[..1]}.";
                            break;
                    }
                }
            }

            if (string.IsNullOrEmpty(result))
                result = name;

            return result;
        }

        /// <summary>
        /// Adds the given payload changes to the dictionary.
        /// </summary>
        /// <param name="tagPosition">The position to add changes to.</param>
        /// <param name="payloads">The payloads to add.</param>
        /// <param name="stringChanges">The dictionary to add the changes to.</param>
        protected void AddPayloadChanges(TagPosition tagPosition, IEnumerable<Payload> payloads, Dictionary<TagPosition, StringChanges> stringChanges, bool forceUsingSingleAnchorPayload)
        {
            if (payloads != null && payloads.Any() && stringChanges != null)
            {
                if (!stringChanges.Keys.Contains(tagPosition))
                    stringChanges[tagPosition] = new();

                var changes = stringChanges[tagPosition];
                changes.Payloads.AddRange(payloads);
                changes.ForceUsingSingleAnchorPayload = forceUsingSingleAnchorPayload;
            }
        }

        /// <summary>
        /// Applies changes to the given string.
        /// </summary>
        /// <param name="seString">The string to apply changes to.</param>
        /// <param name="stringChanges">The changes to apply.</param>
        /// <param name="anchorPayload">The payload in the string that changes should be anchored to. If there is no anchor, the changes will be applied to the entire string.</param>
        protected void ApplyStringChanges(SeString seString, Dictionary<TagPosition, StringChanges> stringChanges, List<Payload> anchorPayloads = null, Payload anchorReplacePayload = null)
        {
            if (stringChanges.Count == 0)
            {
                return;
            }

            List<TagPosition> tagPositionsOrdered = new List<TagPosition>();
            // If there's no anchor payload, do replaces first so that befores and afters are based on the replaced data
            if (anchorPayloads == null || !anchorPayloads.Any())
            {
                tagPositionsOrdered.Add(TagPosition.Replace);
            }

            tagPositionsOrdered.Add(TagPosition.Before);
            tagPositionsOrdered.Add(TagPosition.After);

            // If there is an anchor payload, do replaces last so that we still know which payload needs to be removed
            if (anchorPayloads != null && anchorPayloads.Any())
            {
                tagPositionsOrdered.Add(TagPosition.Replace);
            }

            foreach (var tagPosition in tagPositionsOrdered)
            {
                if (stringChanges.TryGetValue(tagPosition, out var payloads) && payloads.Payloads.Any())
                {
                    AddSpacesBetweenTextPayloads(stringChanges[tagPosition].Payloads, tagPosition);
                    if (tagPosition == TagPosition.Before)
                    {
                        Payload anchorFirst = payloads.ForceUsingSingleAnchorPayload ? anchorReplacePayload : anchorPayloads?.FirstOrDefault();

                        if (anchorFirst != null)
                        {
                            var anchorPayloadIndex = seString.Payloads.IndexOf(anchorFirst);
                            seString.Payloads.InsertRange(anchorPayloadIndex, payloads.Payloads);
                        }
                        else
                        {
                            seString.Payloads.InsertRange(0, payloads.Payloads);
                        }
                    }
                    else if (tagPosition == TagPosition.After)
                    {
                        Payload anchorLast = payloads.ForceUsingSingleAnchorPayload? anchorReplacePayload : anchorPayloads?.LastOrDefault();

                        if (anchorLast != null)
                        {
                            var anchorPayloadIndex = seString.Payloads.IndexOf(anchorLast);
                            seString.Payloads.InsertRange(anchorPayloadIndex + 1, payloads.Payloads);
                        }
                        else
                        {
                            seString.Payloads.AddRange(payloads.Payloads);
                        }
                    }
                    else if (tagPosition == TagPosition.Replace)
                    {
                        Payload anchorReplace = anchorReplacePayload;

                        if (anchorReplace != null)
                        {
                            var anchorPayloadIndex = seString.Payloads.IndexOf(anchorReplace);
                            seString.Payloads.InsertRange(anchorPayloadIndex, payloads.Payloads);
                            seString.Remove(anchorReplace);
                        }
                        else
                        {
                            seString.Payloads.Clear();
                            seString.Payloads.AddRange(payloads.Payloads);
                        }
                    }
                }
            }
        }

        protected void ApplyTextFormatting(GameObject gameObject, Tag tag, SeString[] destStrings, InheritableValue<bool>[] textColorApplied, List<Payload> preferedPayloads)
        {
            if (IsTagVisible(tag, gameObject))
            {
                for (int i = 0; i < destStrings.Length; i++)
                {
                    var destString = destStrings[i];
                    var isTextColorApplied = textColorApplied[i];
                    applyTextColor(destString, isTextColorApplied, tag.TextColor);
                    //applyTextGlowColor(destString, isTextColorApplied, tag.TextGlowColor);
                    //applyTextItalicColor(destString, tag.IsTextItalic); // Disabled, because that is needed only for a few parts somewhere else.
                }
            }

            void applyTextColor(SeString destPayload, InheritableValue<bool> enableFlag, InheritableValue<ushort> colorValue)
            {
                if (shouldApplyFormattingPayloads(destPayload)
                            && enableFlag.InheritedValue != null
                            && enableFlag.InheritedValue.Value
                            && colorValue.InheritedValue != null)
                    applyTextFormattingPayloads(destPayload, new UIForegroundPayload(colorValue.InheritedValue.Value), new UIForegroundPayload(0));
            }

            //void applyTextGlowColor(SeString destPayload, InheritableValue<bool> enableFlag, InheritableValue<ushort> colorValue)
            //{
            //    if (shouldApplyFormattingPayloads(destPayload)
            //                && enableFlag.InheritedValue != null
            //                && enableFlag.InheritedValue.Value
            //                && colorValue.InheritedValue != null)
            //        applyTextFormattingPayloads(destPayload, new UIGlowPayload(colorValue.InheritedValue.Value), new UIGlowPayload(0));
            //}

            //void applyTextItalicColor(SeString destPayload, InheritableValue<bool> italicValue)
            //{
            //    if (shouldApplyFormattingPayloads(destPayload)
            //                && italicValue.InheritedValue != null
            //                && italicValue.InheritedValue.Value)
            //        applyTextFormattingPayloads(destPayload, new EmphasisItalicPayload(true), new EmphasisItalicPayload(false));
            //}

            bool shouldApplyFormattingPayloads(SeString destPayload)
                => destPayload.Payloads.Any(payload => payload is TextPayload || payload is PlayerPayload);

            void applyTextFormattingPayloads(SeString destPayload, Payload startPayload, Payload endPayload)
            {
                if (preferedPayloads == null || !preferedPayloads.Any())
                    applyTextFormattingPayloadToStartAndEnd(destPayload, startPayload, endPayload);
                else
                    applyTextFormattingPayloadsToSpecificPosition(destPayload, startPayload, endPayload, preferedPayloads);
            }
            
            void applyTextFormattingPayloadToStartAndEnd(SeString destPayload, Payload startPayload, Payload endPayload)
            {
                destPayload.Payloads.Insert(0, startPayload);
                destPayload.Payloads.Add(endPayload);
            }

            void applyTextFormattingPayloadsToSpecificPosition(SeString destPayload, Payload startPayload, Payload endPayload, List<Payload> preferedPayload)
            {
                int payloadStartIndex = destPayload.Payloads.IndexOf(preferedPayloads.First());
                destPayload.Payloads.Insert(payloadStartIndex, startPayload);

                int payloadEndIndex = destPayload.Payloads.IndexOf(preferedPayloads.Last());
                destPayload.Payloads.Insert(payloadEndIndex + 1, endPayload);
            }
        }
    }
}
