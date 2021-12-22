using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using Dalamud.Logging;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using PlayerTags.Data;
using PlayerTags.Inheritables;
using PlayerTags.PluginStrings;
using PlayerTags.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace PlayerTags.Configuration
{
    public class PluginConfigurationUI
    {
        private struct PlayerInfo
        {
            public PlayerContext PlayerContext;
            public float Proximity;
        }

        private PluginConfiguration m_PluginConfiguration;
        private PluginData m_PluginData;

        private InheritableValue<ushort>? m_ColorPickerPopupDataContext;

        public PluginConfigurationUI(PluginConfiguration config, PluginData pluginData)
        {
            m_PluginConfiguration = config;
            m_PluginData = pluginData;
        }

        public void Draw()
        {
            if (m_PluginConfiguration == null || !m_PluginConfiguration.IsVisible)
            {
                return;
            }

            if (ImGui.Begin(Strings.Loc_Static_PluginName, ref m_PluginConfiguration.IsVisible))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0.8f, 0.5f, 1));
                ImGui.TextWrapped(Strings.Loc_Static_WarningMessage);
                ImGui.PopStyleColor();

                ImGui.Spacing();
                ImGui.Spacing();
                if (ImGui.BeginTabBar("MainTabs"))
                {
                    if (ImGui.BeginTabItem(Strings.Loc_Static_General))
                    {
                        ImGui.Spacing();
                        ImGui.Spacing();
                        DrawCheckbox(nameof(m_PluginConfiguration.IsCustomTagsContextMenuEnabled), true, ref m_PluginConfiguration.IsCustomTagsContextMenuEnabled, () => m_PluginConfiguration.Save(m_PluginData));


                        ImGui.Spacing();
                        ImGui.Spacing();
                        DrawHeading(Strings.Loc_Static_Nameplates);
                        DrawComboBox(true, true, false, ref m_PluginConfiguration.NameplateFreeCompanyVisibility, () => m_PluginConfiguration.Save(m_PluginData));
                        DrawComboBox(true, true, false, ref m_PluginConfiguration.NameplateTitleVisibility, () => m_PluginConfiguration.Save(m_PluginData));
                        DrawComboBox(true, true, false, ref m_PluginConfiguration.NameplateTitlePosition, () => m_PluginConfiguration.Save(m_PluginData));


                        ImGui.Spacing();
                        ImGui.Spacing();
                        DrawHeading(Strings.Loc_Static_Experimental);
                        DrawCheckbox(nameof(m_PluginConfiguration.IsPlayerNameRandomlyGenerated), true, ref m_PluginConfiguration.IsPlayerNameRandomlyGenerated, () => m_PluginConfiguration.Save(m_PluginData));
                        DrawCheckbox(nameof(m_PluginConfiguration.IsLinkSelfInChatEnabled), true, ref m_PluginConfiguration.IsLinkSelfInChatEnabled, () => m_PluginConfiguration.Save(m_PluginData));

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem(Strings.Loc_Static_Tags))
                    {
                        ImGui.Spacing();
                        ImGui.Spacing();
                        DrawCheckbox(nameof(m_PluginConfiguration.IsShowInheritedPropertiesEnabled), true, ref m_PluginConfiguration.IsShowInheritedPropertiesEnabled, () => m_PluginConfiguration.Save(m_PluginData));
                        ImGui.BeginGroup();
                        ImGui.Columns(2);

                        ImGui.Spacing();
                        ImGui.Spacing();
                        ImGui.BeginGroup();
                        DrawTree(m_PluginData.AllTags);
                        ImGui.EndGroup();

                        ImGui.NextColumn();
                        var selectedTag = m_PluginData.AllTags.Descendents.SingleOrDefault(descendent => descendent.IsSelected.Value);
                        if (selectedTag != null)
                        {
                            DrawControls(selectedTag);
                        }

                        ImGui.EndGroup();
                        ImGui.Columns(1);

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem(Strings.Loc_Static_QuickTag))
                    {
                        ImGui.Spacing();
                        ImGui.Spacing();
                        DrawCheckbox(nameof(m_PluginConfiguration.IsPlayersTabOrderedByProximity), true, ref m_PluginConfiguration.IsPlayersTabOrderedByProximity, () => m_PluginConfiguration.Save(m_PluginData));
                        DrawCheckbox(nameof(m_PluginConfiguration.IsPlayersTabSelfVisible), true, ref m_PluginConfiguration.IsPlayersTabSelfVisible, () => m_PluginConfiguration.Save(m_PluginData));
                        DrawCheckbox(nameof(m_PluginConfiguration.IsPlayersTabFriendsVisible), true, ref m_PluginConfiguration.IsPlayersTabFriendsVisible, () => m_PluginConfiguration.Save(m_PluginData));
                        DrawCheckbox(nameof(m_PluginConfiguration.IsPlayersTabPartyVisible), true, ref m_PluginConfiguration.IsPlayersTabPartyVisible, () => m_PluginConfiguration.Save(m_PluginData));
                        DrawCheckbox(nameof(m_PluginConfiguration.IsPlayersTabAllianceVisible), true, ref m_PluginConfiguration.IsPlayersTabAllianceVisible, () => m_PluginConfiguration.Save(m_PluginData));
                        DrawCheckbox(nameof(m_PluginConfiguration.IsPlayersTabEnemiesVisible), true, ref m_PluginConfiguration.IsPlayersTabEnemiesVisible, () => m_PluginConfiguration.Save(m_PluginData));
                        DrawCheckbox(nameof(m_PluginConfiguration.IsPlayersTabOthersVisible), true, ref m_PluginConfiguration.IsPlayersTabOthersVisible, () => m_PluginConfiguration.Save(m_PluginData));

                        ImGui.Spacing();
                        ImGui.Spacing();
                        if (ImGui.BeginTable("##PlayersTable", 1 + m_PluginData.CustomTags.Count))
                        {
                            ImGui.TableHeader(Strings.Loc_Static_PlayerName);
                            ImGui.TableSetupColumn(Strings.Loc_Static_PlayerName);
                            ImGui.NextColumn();
                            foreach (var customTag in m_PluginData.CustomTags)
                            {
                                ImGui.TableHeader(customTag.Text.InheritedValue);
                                ImGui.TableSetupColumn(customTag.Text.InheritedValue);
                                ImGui.NextColumn();
                            }
                            ImGui.TableHeadersRow();

                            if (PluginServices.ClientState.LocalPlayer != null)
                            {
                                Dictionary<string, PlayerInfo> playerNameContexts = PluginServices.ObjectTable
                                    .Where(gameObject => gameObject is PlayerCharacter)
                                    .Select(gameObject => gameObject as PlayerCharacter)
                                    .ToDictionary(
                                        playerCharacter => playerCharacter!.Name.TextValue,
                                        playerCharacter => new PlayerInfo()
                                        {
                                            PlayerContext = PlayerContextHelper.GetPlayerContext(playerCharacter!),
                                            Proximity = (playerCharacter!.Position - PluginServices.ClientState.LocalPlayer.Position).Length()
                                        });

                                // Include party members that aren't in the game object list
                                foreach (var partyMember in PluginServices.PartyList)
                                {
                                    if (!playerNameContexts.ContainsKey(partyMember.Name.TextValue))
                                    {
                                        playerNameContexts[partyMember.Name.TextValue] = new PlayerInfo()
                                        {
                                            PlayerContext = PlayerContext.Party,
                                            Proximity = int.MaxValue
                                        };
                                    }
                                }

                                var filteredPlayerNameContexts = playerNameContexts.Where(player => PlayerContextHelper.GetIsVisible(player.Value.PlayerContext,
                                    m_PluginConfiguration.IsPlayersTabSelfVisible,
                                    m_PluginConfiguration.IsPlayersTabFriendsVisible,
                                    m_PluginConfiguration.IsPlayersTabPartyVisible,
                                    m_PluginConfiguration.IsPlayersTabAllianceVisible,
                                    m_PluginConfiguration.IsPlayersTabEnemiesVisible,
                                    m_PluginConfiguration.IsPlayersTabOthersVisible));

                                var orderedPlayerNameContexts = filteredPlayerNameContexts.OrderBy(player => player.Key);
                                if (m_PluginConfiguration.IsPlayersTabOrderedByProximity)
                                {
                                    orderedPlayerNameContexts = filteredPlayerNameContexts.OrderBy(player => player.Value.Proximity);
                                }

                                int rowIndex = 0;
                                foreach (var player in orderedPlayerNameContexts)
                                {
                                    DrawQuickAddRow(new Identity(player.Key), rowIndex);
                                    ++rowIndex;
                                }
                            }

                            ImGui.EndTable();
                        }

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem(Strings.Loc_Static_TaggedPlayers))
                    {
                        ImGui.Spacing();
                        ImGui.Spacing();
                        if (ImGui.BeginTable("##TaggedPlayersTable", 1 + m_PluginData.CustomTags.Count))
                        {
                            ImGui.TableHeader(Strings.Loc_Static_PlayerName);
                            ImGui.TableSetupColumn(Strings.Loc_Static_PlayerName);
                            ImGui.NextColumn();
                            foreach (var customTag in m_PluginData.CustomTags)
                            {
                                ImGui.TableHeader(customTag.Text.InheritedValue);
                                ImGui.TableSetupColumn(customTag.Text.InheritedValue);
                                ImGui.NextColumn();
                            }
                            ImGui.TableHeadersRow();

                            int rowIndex = 0;
                            foreach (var identity in m_PluginData.CustomTags.SelectMany(customTag => customTag.IdentitiesToAddTo).Distinct().OrderBy(name => name).ToArray())
                            {
                                DrawQuickAddRow(identity, rowIndex);
                                ++rowIndex;
                            }

                            if (PluginServices.ObjectTable.Length == 0 && PluginServices.ClientState.LocalPlayer != null)
                            {
                                DrawQuickAddRow(new Identity(PluginServices.ClientState.LocalPlayer.Name.TextValue), 0);
                            }

                            ImGui.EndTable();
                        }

                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }

                ImGui.End();
            }

            if (!m_PluginConfiguration.IsVisible)
            {
                m_PluginConfiguration.Save(m_PluginData);
            }
        }

        void DrawQuickAddRow(Identity identity, int rowIndex)
        {
            ImGui.PushID(identity.ToString());

            ImGui.TableNextRow();

            if (rowIndex % 2 != 0)
            {
                var backgroundColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.05f));
                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, backgroundColor);
            }

            ImGui.TableNextColumn();

            ImGui.AlignTextToFramePadding();
            ImGui.Text(identity.Name);

            foreach (Tag customTag in m_PluginData.CustomTags)
            {
                ImGui.PushID(customTag.GetHashCode().ToString());

                ImGui.TableNextColumn();

                bool isTagAssigned = customTag.CanAddToIdentity(identity);

                DrawSimpleCheckbox(string.Format(Strings.Loc_Static_Format_AddTagToPlayer, customTag.Text.InheritedValue, identity.Name), ref isTagAssigned, () =>
                {
                    if (isTagAssigned)
                    {
                        customTag.AddIdentityToAddTo(identity);
                    }
                    else
                    {
                        customTag.RemoveIdentityToAddTo(identity);
                    }

                    m_PluginConfiguration.Save(m_PluginData);
                });

                ImGui.PopID();
            }

            ImGui.PopID();
        }

        public string GetTreeItemName(Tag tag)
        {
            string itemName = tag.Name.Value;
            if (m_PluginData.CustomTags.Contains(tag))
            {
                if (!string.IsNullOrWhiteSpace(tag.Text.InheritedValue))
                {
                    itemName = tag.Text.InheritedValue;
                }
                else
                {
                    itemName = Strings.Loc_Static_NoText;
                }
            }

            return itemName;
        }

        public void Select(Tag tag)
        {
            foreach (var descendent in m_PluginData.AllTags.Descendents)
            {
                descendent.IsSelected.Value = false;
            }

            tag.IsSelected.Value = true;
            m_PluginConfiguration.Save(m_PluginData);
        }

        public void DrawTree(Tag tag)
        {
            ImGui.PushID(tag.GetHashCode().ToString());

            // Build the tree node flags
            ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.SpanFullWidth | ImGuiTreeNodeFlags.AllowItemOverlap | ImGuiTreeNodeFlags.FramePadding;

            if (!tag.Children.Any())
            {
                flags |= ImGuiTreeNodeFlags.Leaf;
            }

            if (tag.IsSelected.Value)
            {
                flags |= ImGuiTreeNodeFlags.Selected;
            }

            if (tag.IsExpanded.Value)
            {
                flags |= ImGuiTreeNodeFlags.DefaultOpen;
            }

            // Render the tree node
            var beforeItemPos = ImGui.GetCursorScreenPos();
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0, 3));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));
            if (tag.TextColor.InheritedValue != null)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, UIColorHelper.ToColor(tag.TextColor.InheritedValue.Value));
            }
            bool isOpened = ImGui.TreeNodeEx($"{GetTreeItemName(tag)}###{tag.GetHashCode()}", flags);
            if (tag.TextColor.InheritedValue != null)
            {
                ImGui.PopStyleColor();
            }
            ImGui.PopStyleVar();
            ImGui.PopStyleVar();
            var afterItemPos = ImGui.GetCursorScreenPos();

            // Don't allow expanding the item to select the item
            // Don't allow clicking on add/remove buttons to select the item
            var deadzoneTopLeft = new Vector2(beforeItemPos.X + ImGui.GetContentRegionAvail().X - 23, beforeItemPos.Y - 2);
            var deadzoneBottomRight = new Vector2(beforeItemPos.X + ImGui.GetContentRegionAvail().X + 2, afterItemPos.Y + 2);
            if (!ImGui.IsItemToggledOpen() && !ImGui.IsMouseHoveringRect(deadzoneTopLeft, deadzoneBottomRight) && ImGui.IsItemClicked())
            {
                Select(tag);
            }

            // Render the custom tag button
            if (tag == m_PluginData.AllCustomTags)
            {
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.1f, 0.3f, 0.1f, 1));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.2f, 0.6f, 0.2f, 1));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.2f, 0.6f, 0.2f, 1));
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));
                ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, new Vector2(0, 0));
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.SetCursorPosX(ImGui.GetCursorPos().X + ImGui.GetContentRegionAvail().X - 23);
                if (ImGui.Button(FontAwesomeIcon.Plus.ToIconString()))
                {
                    var newTag = new Tag(new LocalizedPluginString(nameof(PluginData.CustomTags)))
                    {
                        IsExpanded = true,
                        Text = Strings.Loc_Static_NewTag,
                        GameObjectNamesToApplyTo = ""
                    };

                    m_PluginData.CustomTags.Add(newTag);
                    newTag.Parent = m_PluginData.AllCustomTags;

                    Select(newTag);
                }
                ImGui.PopFont();
                ImGui.PopStyleVar();
                ImGui.PopStyleVar();
                ImGui.PopStyleColor();
                ImGui.PopStyleColor();
                ImGui.PopStyleColor();

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Strings.Loc_Static_AddCustomTag_Description);
                }
            }


            // Render the remove custom tag button
            if (m_PluginData.CustomTags.Contains(tag))
            {
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.1f, 0.1f, 1));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.6f, 0.2f, 0.2f, 1));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.6f, 0.2f, 0.2f, 1));
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));
                ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, new Vector2(0, 0));
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.SetCursorPosX(ImGui.GetCursorPos().X + ImGui.GetContentRegionAvail().X - 23);
                if (ImGui.Button(FontAwesomeIcon.TrashAlt.ToIconString()))
                {
                    m_PluginData.AllCustomTags.Children.Remove(tag);
                    m_PluginData.CustomTags.Remove(tag);
                    m_PluginConfiguration.Save(m_PluginData);

                    Select(m_PluginData.AllCustomTags);
                }
                ImGui.PopFont();
                ImGui.PopStyleVar();
                ImGui.PopStyleVar();
                ImGui.PopStyleColor();
                ImGui.PopStyleColor();
                ImGui.PopStyleColor();

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Strings.Loc_Static_RemoveCustomTag_Description);
                }
            }
                        

            // Save the node expansion state
            if (isOpened && tag.Children.Any() && !tag.IsExpanded.Value)
            {
                tag.IsExpanded.Value = true;
                m_PluginConfiguration.Save(m_PluginData);
            }
            else if (!isOpened && tag.IsExpanded.Value)
            {
                tag.IsExpanded.Value = false;
                m_PluginConfiguration.Save(m_PluginData);
            }



            // Render the child nodes
            if (isOpened)
            {
                ImGui.TreePush();
                foreach (var childTag in tag.Children.OrderBy(child => GetTreeItemName(child)).ToArray())
                {
                    DrawTree(childTag);
                }
                ImGui.TreePop();
            }


            ImGui.PopID();
        }

        public void DrawControls(Tag tag)
        {
            ImGui.PushID(tag.GetHashCode().ToString());

            // Render the add property override button and popup
            if (ImGui.IsPopupOpen("AddPopup"))
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.6f, 0.2f, 1));
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.1f, 0.3f, 0.1f, 1));
            }
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.2f, 0.6f, 0.2f, 1));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.2f, 0.6f, 0.2f, 1));

            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button(FontAwesomeIcon.Plus.ToIconString(), new Vector2(23, 23)))
            {
                ImGui.OpenPopup("AddPopup");
            }
            ImGui.PopFont();
            ImGui.PopStyleColor();
            ImGui.PopStyleColor();
            ImGui.PopStyleColor();

            bool wasPaddingConsumed = false;
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 5));
            ImGui.SetNextWindowPos(ImGui.GetCursorScreenPos() - new Vector2(0, 4));
            if (ImGui.BeginPopup("AddPopup"))
            {
                wasPaddingConsumed = true;
                ImGui.PopStyleVar();

                ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0f, 0f, 0f, 0));
                if (ImGui.BeginListBox("###SelectableInheritables"))
                {
                    IEnumerable<KeyValuePair<string, IInheritable>> inheritables1 = tag.Inheritables.Where(inheritable => inheritable.Value.Behavior == InheritableBehavior.Inherit);
                    var selectedInheritables1 = inheritables1
                        .Select(inheritable =>
                        {
                            string? categoryId = null;

                            var field = tag.GetType().GetField(inheritable.Key);
                            if (field != null)
                            {
                                var inheritableCategory = field.GetCustomAttributes(typeof(InheritableCategoryAttribute), false).Cast<InheritableCategoryAttribute>().FirstOrDefault();
                                if (inheritableCategory != null)
                                {
                                    categoryId = inheritableCategory.CategoryId;
                                }
                            }

                            return new
                            {
                                Inheritable = inheritable,
                                CategoryId = categoryId,
                                LocalizedName = Localizer.GetString(inheritable.Key, false)
                            };
                        }).Where(inheritable => inheritable.CategoryId != null);

                    var inheritableGroups1 = selectedInheritables1.GroupBy(inheritable => inheritable.CategoryId);

                    foreach (var inheritableGroup in inheritableGroups1)
                    {
                    
                        if (inheritableGroup.Key != null)
                        {
                            DrawHeading(Localizer.GetString(inheritableGroup.Key, false));
                        }

                        foreach (var selectedInheritable in inheritableGroup)
                        {
                            bool isSelected = false;
                            if (ImGui.Selectable(selectedInheritable.LocalizedName, isSelected))
                            {
                                selectedInheritable.Inheritable.Value.Behavior = InheritableBehavior.Enabled;

                                if (selectedInheritable.Inheritable.Value is InheritableValue<bool> inheritableBool)
                                {
                                    inheritableBool.Value = true;
                                }

                                m_PluginConfiguration.Save(m_PluginData);
                                ImGui.CloseCurrentPopup();
                            }

                            if (isSelected)
                            {
                                ImGui.SetItemDefaultFocus();
                            }

                            if (ImGui.IsItemHovered())
                            {
                                ImGui.SetTooltip(Localizer.GetString(selectedInheritable.Inheritable.Key, true));
                            }
                        }
                    }

                    ImGui.EndListBox();
                }
                ImGui.PopStyleColor();
                ImGui.EndPopup();
            }
            if (!wasPaddingConsumed)
            {
                ImGui.PopStyleVar();
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Strings.Loc_Static_AddPropertyOverride_Description);
            }

            // Render all the property overrides, and optionally allow the inherited properties to be rendered
            IEnumerable<KeyValuePair<string, IInheritable>> inheritables = tag.Inheritables;
            if (!m_PluginConfiguration.IsShowInheritedPropertiesEnabled)
            {
                inheritables = inheritables.Where(inheritable => inheritable.Value.Behavior != InheritableBehavior.Inherit);
            }

            var selectedInheritables = inheritables
                .Select(inheritable =>
                {
                    string? categoryId = null;

                    var field = tag.GetType().GetField(inheritable.Key);
                    if (field != null)
                    {
                        var inheritableCategory = field.GetCustomAttributes(typeof(InheritableCategoryAttribute), false).Cast<InheritableCategoryAttribute>().FirstOrDefault();
                        if (inheritableCategory != null)
                        {
                            categoryId = inheritableCategory.CategoryId;
                        }
                    }

                    return new
                    {
                        Inheritable = inheritable,
                        CategoryId = categoryId,
                        LocalizedName = Localizer.GetString(inheritable.Key, false)
                    };
                }).Where(inheritable => inheritable.CategoryId != null);

            var inheritableGroups = selectedInheritables.GroupBy(inheritable => inheritable.CategoryId);

            foreach (var inheritableGroup in inheritableGroups)
            {
                ImGui.Spacing();
                ImGui.Spacing();
                if (inheritableGroup.Key != null)
                {
                    DrawHeading(Localizer.GetString(inheritableGroup.Key, false));
                }

                foreach (var selectedInheritable in inheritableGroup)
                {
                    if (selectedInheritable.Inheritable.Value is InheritableValue<bool> inheritableBool)
                    {
                        DrawInheritable(selectedInheritable.Inheritable.Key, inheritableBool);
                    }
                    else if (selectedInheritable.Inheritable.Value is InheritableValue<ushort> inheritableUshort)
                    {
                        DrawInheritable(selectedInheritable.Inheritable.Key, inheritableUshort);
                    }
                    else if (selectedInheritable.Inheritable.Value is InheritableValue<BitmapFontIcon> inheritableBitmapFontIcon)
                    {
                        DrawInheritable(selectedInheritable.Inheritable.Key, false, true, inheritableBitmapFontIcon);
                    }
                    else if (selectedInheritable.Inheritable.Value is InheritableValue<TagPosition> inheritableTagPosition)
                    {
                        DrawInheritable(selectedInheritable.Inheritable.Key, true, false, inheritableTagPosition);
                    }
                    else if (selectedInheritable.Inheritable.Value is InheritableValue<NameplateElement> inheritableNameplateElement)
                    {
                        DrawInheritable(selectedInheritable.Inheritable.Key, true, false, inheritableNameplateElement);
                    }
                    else if (selectedInheritable.Inheritable.Value is InheritableReference<string> inheritableString)
                    {
                        DrawInheritable(selectedInheritable.Inheritable.Key, inheritableString);
                    }
                    else
                    {
                        PluginLog.Warning($"Rendering for inheritable option not implemented: {selectedInheritable.Inheritable.Key}");
                    }
                }
            }


            ImGui.PopID();
        }

        private void DrawRemovePropertyOverrideButton(IInheritable inheritable)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0, 0));
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.1f, 0.1f, 1));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.6f, 0.2f, 0.2f, 1));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.6f, 0.2f, 0.2f, 1));
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button(FontAwesomeIcon.TrashAlt.ToIconString(), new Vector2(23, 23)))
            {
                inheritable.Behavior = InheritableBehavior.Inherit;
                m_PluginConfiguration.Save(m_PluginData);
            }
            ImGui.PopFont();
            ImGui.PopStyleColor();
            ImGui.PopStyleColor();
            ImGui.PopStyleColor();
            ImGui.PopStyleVar();
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Strings.Loc_Static_RemovePropertyOverride_Description);
            }
        }

        private void DrawInheritable(string localizedStringName, InheritableValue<bool> inheritable)
        {
            bool isDisabled = inheritable.Behavior == InheritableBehavior.Inherit;
            if (isDisabled)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.25f);
            }

            ImGui.BeginGroup();
            ImGui.BeginChild(inheritable.GetHashCode().ToString(), new Vector2(0    , 50));

            ImGui.Text(Localizer.GetString(localizedStringName, false));
            if (ImGui.IsItemHovered())
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 1f);
                ImGui.SetTooltip(Localizer.GetString(localizedStringName, true));
                ImGui.PopStyleVar();
            }

            if (isDisabled)
            {
                ImGui.SameLine();
                ImGui.Text(Strings.Loc_Static_Inherited);
            }

            if (isDisabled)
            {
                bool value = inheritable.InheritedValue != null ? inheritable.InheritedValue.Value : false;
                DrawCheckbox("IsEnabled", false, ref value, () =>
                {
                });
            }
            else
            {
                DrawCheckbox("IsEnabled", false, ref inheritable.Value, () =>
                {
                    m_PluginConfiguration.Save(m_PluginData);
                });

                ImGui.SameLine();
                DrawRemovePropertyOverrideButton(inheritable);
            }

            ImGui.EndChild();
            ImGui.EndGroup();

            if (isDisabled)
            {
                ImGui.PopStyleVar();
            }
        }

        private void DrawInheritable<TEnum>(string localizedStringName, bool shouldLocalizeNames, bool shouldOrderNames, InheritableValue<TEnum> inheritable)
            where TEnum : struct, Enum
        {
            bool isDisabled = inheritable.Behavior == InheritableBehavior.Inherit;
            if (isDisabled)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.25f);
            }

            ImGui.BeginGroup();
            ImGui.BeginChild(inheritable.GetHashCode().ToString(), new Vector2(0, 50));

            ImGui.Text(Localizer.GetString(localizedStringName, false));
            if (ImGui.IsItemHovered())
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 1f);
                ImGui.SetTooltip(Localizer.GetString(localizedStringName, true));
                ImGui.PopStyleVar();
            }

            if (isDisabled)
            {
                ImGui.SameLine();
                ImGui.Text(Strings.Loc_Static_Inherited);
            }

            if (isDisabled)
            {
                bool isEnabled = inheritable.InheritedValue != null;
                DrawCheckbox("IsEnabled", false, ref isEnabled, () => { });

                if (isEnabled)
                {
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(200);
                    ImGui.BeginChild(inheritable.GetHashCode().ToString(), new Vector2(200, 0));
                    TEnum value = inheritable.InheritedValue != null ? inheritable.InheritedValue.Value : default(TEnum);
                    DrawComboBox(false, shouldLocalizeNames, shouldOrderNames, ref value, () => { });
                    ImGui.EndChild();
                }
            }
            else
            {
                bool isEnabled = inheritable.Behavior == InheritableBehavior.Enabled;
                DrawCheckbox("IsEnabled", false, ref isEnabled, () =>
                {
                    if (isEnabled)
                    {
                        inheritable.Behavior = InheritableBehavior.Enabled;
                    }
                    else
                    {
                        inheritable.Behavior = InheritableBehavior.Disabled;
                    }
                    m_PluginConfiguration.Save(m_PluginData);
                });

                if (isEnabled)
                {
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(200);
                    ImGui.BeginChild(inheritable.GetHashCode().ToString(), new Vector2(200, 0));
                    DrawComboBox(false, shouldLocalizeNames, shouldOrderNames, ref inheritable.Value, () => { m_PluginConfiguration.Save(m_PluginData); });
                    ImGui.EndChild();
                }

                ImGui.SameLine();
                DrawRemovePropertyOverrideButton(inheritable);
            }

            ImGui.EndChild();
            ImGui.EndGroup();

            if (isDisabled)
            {
                ImGui.PopStyleVar();
            }
        }

        private void DrawInheritable(string localizedStringName, InheritableValue<ushort> inheritable)
        {
            bool isDisabled = inheritable.Behavior == InheritableBehavior.Inherit;
            if (isDisabled)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.25f);
            }

            ImGui.BeginGroup();
            ImGui.BeginChild(inheritable.GetHashCode().ToString(), new Vector2(180, 50));

            ImGui.Text(Localizer.GetString(localizedStringName, false));
            if (ImGui.IsItemHovered())
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 1f);
                ImGui.SetTooltip(Localizer.GetString(localizedStringName, true));
                ImGui.PopStyleVar();
            }

            if (isDisabled)
            {
                ImGui.SameLine();
                ImGui.Text(Strings.Loc_Static_Inherited);
            }

            if (isDisabled)
            {
                bool isEnabled = inheritable.InheritedValue != null;
                DrawCheckbox("IsEnabled", false, ref isEnabled, () => { });

                if (isEnabled)
                {
                    ImGui.SameLine();
                    ushort value = inheritable.InheritedValue != null ? inheritable.InheritedValue.Value : default(ushort);
                    DrawColorButton(value.ToString(), UIColorHelper.ToColor(value), () => { });
                }
            }
            else
            {
                bool isEnabled = inheritable.Behavior == InheritableBehavior.Enabled;
                DrawCheckbox("IsEnabled", false, ref isEnabled, () =>
                {
                    if (isEnabled)
                    {
                        inheritable.Behavior = InheritableBehavior.Enabled;
                    }
                    else
                    {
                        inheritable.Behavior = InheritableBehavior.Disabled;
                    }
                    m_PluginConfiguration.Save(m_PluginData);
                });

                if (isEnabled)
                {
                    ImGui.SameLine();
                    DrawColorButton(
                        inheritable.Value.ToString(),
                        UIColorHelper.ToColor(inheritable.Value),
                        () =>
                        {
                            m_ColorPickerPopupDataContext = inheritable;
                            ImGui.OpenPopup("ColorPickerPopup");
                        });
                }

                bool wasStyleConsumed = false;
                ImGui.SetNextWindowPos(ImGui.GetCursorScreenPos() + new Vector2(31, 0));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
                if (ImGui.BeginPopup("ColorPickerPopup"))
                {
                    wasStyleConsumed = true;
                    ImGui.PopStyleVar();

                    DrawUIColorPicker(
                        (UIColor value) =>
                        {
                            if (m_ColorPickerPopupDataContext != null)
                            {
                                m_ColorPickerPopupDataContext.Value = (ushort)value.RowId;
                                m_ColorPickerPopupDataContext = null;
                                m_PluginConfiguration.Save(m_PluginData);
                            }

                            ImGui.CloseCurrentPopup();
                        });

                    ImGui.EndPopup();
                }
                if (!wasStyleConsumed)
                {
                    ImGui.PopStyleVar();
                }

                ImGui.SameLine();
                DrawRemovePropertyOverrideButton(inheritable);
            }

            ImGui.EndChild();
            ImGui.EndGroup();

            if (isDisabled)
            {
                ImGui.PopStyleVar();
            }
        }

        private void DrawInheritable(string localizedStringName, InheritableReference<string> inheritable)
        {
            bool isDisabled = inheritable.Behavior == InheritableBehavior.Inherit;
            if (isDisabled)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.25f);
            }

            ImGui.BeginGroup();
            ImGui.BeginChild(inheritable.GetHashCode().ToString(), new Vector2(0, 50));

            ImGui.Text(Localizer.GetString(localizedStringName, false));
            if (ImGui.IsItemHovered())
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 1f);
                ImGui.SetTooltip(Localizer.GetString(localizedStringName, true));
                ImGui.PopStyleVar();
            }

            if (isDisabled)
            {
                ImGui.SameLine();
                ImGui.Text(Strings.Loc_Static_Inherited);
            }

            if (isDisabled)
            {
                bool isEnabled = inheritable.InheritedValue != null;
                DrawCheckbox("IsEnabled", false, ref isEnabled, () =>
                {
                });

                if (isEnabled)
                {
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(200);
                    ImGui.BeginChild(inheritable.GetHashCode().ToString(), new Vector2(200, 0));
                    string value = inheritable.Value;
                    DrawTextBox(localizedStringName, ref value, () => { });
                    ImGui.EndChild();
                }
            }
            else
            {
                bool isEnabled = inheritable.Behavior == InheritableBehavior.Enabled;
                DrawCheckbox("IsEnabled", false, ref isEnabled, () =>
                {
                    if (isEnabled)
                    {
                        inheritable.Behavior = InheritableBehavior.Enabled;
                    }
                    else
                    {
                        inheritable.Behavior = InheritableBehavior.Disabled;
                    }
                    m_PluginConfiguration.Save(m_PluginData);
                });

                if (isEnabled)
                {
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(200);
                    ImGui.BeginChild(inheritable.GetHashCode().ToString(), new Vector2(200, 0));
                    DrawTextBox(localizedStringName, ref inheritable.Value, () => { m_PluginConfiguration.Save(m_PluginData); });                
                    ImGui.EndChild();
                }

                ImGui.SameLine();
                DrawRemovePropertyOverrideButton(inheritable);
            }

            ImGui.EndChild();
            ImGui.EndGroup();

            if (isDisabled)
            {
                ImGui.PopStyleVar();
            }
        }

        private void DrawHeading(string label)
        {
            ImGui.TextColored(new Vector4(0.7f, 0.6f, 1f, 1f), label);
        }

        private void DrawComboBox<TEnum>(bool isLabelVisible, bool shouldLocalizeNames, bool shouldOrderNames, ref TEnum currentValue, System.Action changed)
            where TEnum : Enum
        {
            if (isLabelVisible)
            {
                ImGui.Text(Localizer.GetString<TEnum>(false));
            }

            var currentDisplayName = shouldLocalizeNames ? Localizer.GetString(currentValue, false) : currentValue.ToString();

            ImGui.SetNextItemWidth(ImGui.CalcItemWidth());
            if (ImGui.BeginCombo($"###{currentValue.GetType().Name}", currentDisplayName))
            {
                var displayNames = Enum.GetValues(typeof(TEnum)).Cast<TEnum>()
                    .Select(value => new { Value = value, DisplayName = shouldLocalizeNames ? Localizer.GetString(value, false) : value.ToString() });

                if (shouldOrderNames)
                {
                    displayNames = displayNames.OrderBy(displayEnum => displayEnum.DisplayName);
                }

                foreach (var orderedDisplayName in displayNames)
                {
                    bool isSelected = orderedDisplayName.Value.Equals(currentValue);
                    if (ImGui.Selectable(orderedDisplayName.DisplayName, isSelected))
                    {
                        currentValue = orderedDisplayName.Value;
                        changed();
                    }

                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }

                    if (ImGui.IsItemHovered() && shouldLocalizeNames)
                    {
                        ImGui.SetTooltip(Localizer.GetString(orderedDisplayName.Value, true));
                    }
                }

                ImGui.EndCombo();
            }

            if (ImGui.IsItemHovered() && shouldLocalizeNames)
            {
                ImGui.SetTooltip(Localizer.GetString(currentValue, true));
            }
        }

        private void DrawCheckbox(string localizedStringName, bool hasLabel, ref bool isChecked, System.Action changed)
        {
            if (ImGui.Checkbox(hasLabel ? Localizer.GetString(localizedStringName, false) : $"###{Localizer.GetString(localizedStringName, false)}", ref isChecked))
            {
                changed();
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Localizer.GetString(localizedStringName, true));
            }
        }

        private void DrawSimpleCheckbox(string localizedString, ref bool isChecked, System.Action changed)
        {
            if (ImGui.Checkbox($"###{localizedString}", ref isChecked))
            {
                changed();
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(localizedString);
            }
        }

        private void DrawColorButton(string colorId, Vector4 color, System.Action clicked)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, color);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, color);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, new Vector2(0, 0));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);
            if (ImGui.Button($"###{colorId}", new Vector2(23, 23)))
            {
                clicked();
            }
            ImGui.PopStyleVar();
            ImGui.PopStyleVar();
            ImGui.PopStyleVar();
            ImGui.PopStyleColor();
            ImGui.PopStyleColor();
            ImGui.PopStyleColor();

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(colorId);
            }
        }

        private void DrawTextBox(string localizedStringName, ref string text, System.Action changed)
        {
            ImGui.SetNextItemWidth(ImGui.CalcItemWidth());

            var oldText = text;
            ImGui.InputText($"###{localizedStringName}", ref text, 1024);
            if (text != oldText)
            {
                changed();
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Localizer.GetString(localizedStringName, true));
            }
        }

        private void DrawUIColorPicker(System.Action<UIColor> colorSelected)
        {
            const int columnCount = 12;

            int currentColumn = 0;
            foreach (var uiColor in UIColorHelper.UIColors)
            {
                if (currentColumn % columnCount != 0)
                {
                    ImGui.SameLine(0, 0);
                }

                DrawColorButton(uiColor.RowId.ToString(), UIColorHelper.ToColor(uiColor), () => { colorSelected(uiColor); });
                currentColumn++;
            }
        }
    }
}
