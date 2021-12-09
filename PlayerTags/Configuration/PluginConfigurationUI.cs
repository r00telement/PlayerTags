using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface;
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
                        ImGui.TreePush();
                        DrawCheckbox(nameof(m_PluginConfiguration.IsCustomTagsContextMenuEnabled), true, ref m_PluginConfiguration.IsCustomTagsContextMenuEnabled, () => m_PluginConfiguration.Save(m_PluginData));
                        ImGui.TreePop();


                        ImGui.Spacing();
                        ImGui.Spacing();
                        DrawHeading(Strings.Loc_Static_Nameplates);
                        ImGui.TreePush();
                        DrawComboBox(true, true, false, ref m_PluginConfiguration.NameplateFreeCompanyVisibility, () => m_PluginConfiguration.Save(m_PluginData));
                        DrawComboBox(true, true, false, ref m_PluginConfiguration.NameplateTitleVisibility, () => m_PluginConfiguration.Save(m_PluginData));
                        DrawComboBox(true, true, false, ref m_PluginConfiguration.NameplateTitlePosition, () => m_PluginConfiguration.Save(m_PluginData));
                        ImGui.TreePop();


                        ImGui.Spacing();
                        ImGui.Spacing();
                        DrawHeading(Strings.Loc_Static_Development);
                        ImGui.TreePush();
                        DrawCheckbox(nameof(m_PluginConfiguration.IsPlayerNameRandomlyGenerated), true, ref m_PluginConfiguration.IsPlayerNameRandomlyGenerated, () => m_PluginConfiguration.Save(m_PluginData));
                        ImGui.TreePop();

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem(Strings.Loc_Static_Tags))
                    {
                        ImGui.Spacing();
                        ImGui.Spacing();
                        ImGui.TreePush();
                        DrawCheckbox(nameof(m_PluginConfiguration.IsShowInheritedPropertiesEnabled), true, ref m_PluginConfiguration.IsShowInheritedPropertiesEnabled, () => m_PluginConfiguration.Save(m_PluginData));
                        ImGui.BeginGroup();
                        ImGui.Columns(2);

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
                        ImGui.TreePop();
                        ImGui.Columns(1);

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem(Strings.Loc_Static_Party))
                    {
                        ImGui.Spacing();
                        ImGui.Spacing();
                        ImGui.TreePush();
                        if (ImGui.BeginTable("##PartyAssignTable", 1 + m_PluginData.CustomTags.Count))
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
                            foreach (var partyMember in PluginServices.PartyList.OrderBy(obj => obj.Name.TextValue).ToArray())
                            {
                                DrawPlayerAssignmentRow(partyMember.Name.TextValue, rowIndex);
                                ++rowIndex;
                            }

                            if (PluginServices.PartyList.Length == 0 && PluginServices.ClientState.LocalPlayer != null)
                            {
                                DrawPlayerAssignmentRow(PluginServices.ClientState.LocalPlayer.Name.TextValue, 0);
                            }

                            ImGui.EndTable();
                        }
                        ImGui.TreePop();

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem(Strings.Loc_Static_Vicinity))
                    {
                        ImGui.Spacing();
                        ImGui.Spacing();
                        ImGui.TreePush();
                        if (ImGui.BeginTable("##VicinityAssignTable", 1 + m_PluginData.CustomTags.Count))
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
                            foreach (var gameObject in PluginServices.ObjectTable.Where(obj => obj is PlayerCharacter).OrderBy(obj => obj.Name.TextValue))
                            {
                                DrawPlayerAssignmentRow(gameObject.Name.TextValue, rowIndex);
                                ++rowIndex;
                            }

                            if (PluginServices.ObjectTable.Length == 0 && PluginServices.ClientState.LocalPlayer != null)
                            {
                                DrawPlayerAssignmentRow(PluginServices.ClientState.LocalPlayer.Name.TextValue, 0);
                            }

                            ImGui.EndTable();
                        }
                        ImGui.TreePop();

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem(Strings.Loc_Static_TaggedPlayers))
                    {
                        ImGui.Spacing();
                        ImGui.Spacing();
                        ImGui.TreePush();
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
                            foreach (var gameObjectName in m_PluginData.CustomTags.SelectMany(customTag => customTag.SplitGameObjectNamesToApplyTo).Distinct().OrderBy(name => name).ToArray())
                            {
                                DrawPlayerAssignmentRow(gameObjectName, rowIndex);
                                ++rowIndex;
                            }

                            if (PluginServices.ObjectTable.Length == 0 && PluginServices.ClientState.LocalPlayer != null)
                            {
                                DrawPlayerAssignmentRow(PluginServices.ClientState.LocalPlayer.Name.TextValue, 0);
                            }

                            ImGui.EndTable();
                        }
                        ImGui.TreePop();

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

        void DrawPlayerAssignmentRow(string playerName, int rowIndex)
        {
            ImGui.PushID(playerName);

            ImGui.TableNextRow();

            if (rowIndex % 2 != 0)
            {
                var backgroundColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.05f));
                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, backgroundColor);
            }

            ImGui.TableNextColumn();

            ImGui.AlignTextToFramePadding();
            ImGui.Text(playerName);

            foreach (Tag customTag in m_PluginData.CustomTags)
            {
                ImGui.PushID(customTag.GetHashCode().ToString());

                ImGui.TableNextColumn();

                bool isTagAssigned = customTag.IncludesGameObjectNameToApplyTo(playerName);

                DrawSimpleCheckbox(string.Format(Strings.Loc_Static_Format_AddTagToPlayer, customTag.Text.InheritedValue, playerName), ref isTagAssigned, () =>
                {
                    if (isTagAssigned)
                    {
                        customTag.AddGameObjectNameToApplyTo(playerName);
                    }
                    else
                    {
                        customTag.RemoveGameObjectNameToApplyTo(playerName);
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
                if (tag.Text.InheritedValue != null)
                {
                    itemName = tag.Text.InheritedValue;
                }
                else
                {
                    itemName = "";
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
            ImGui.TreePush();

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
                    var selectableInheritables = tag.Inheritables.Where(inheritable => inheritable.Value.Behavior == InheritableBehavior.Inherit).ToDictionary(item => item.Key, item => item.Value);
                    var selectableInheritablesWithLocalizedNames = selectableInheritables
                        .Select(inheritable => new { Name = inheritable.Key, LocalizedName = Localizer.GetString(inheritable.Key, false) })
                        .OrderBy(item => item.LocalizedName);

                    foreach (var inheritableLocalizedName in selectableInheritablesWithLocalizedNames)
                    {
                        bool isSelected = false;
                        if (ImGui.Selectable(inheritableLocalizedName.LocalizedName, isSelected))
                        {
                            selectableInheritables[inheritableLocalizedName.Name].Behavior = InheritableBehavior.Enabled;

                            if (selectableInheritables[inheritableLocalizedName.Name] is InheritableValue<bool> inheritableBool)
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
                            ImGui.SetTooltip(Localizer.GetString(inheritableLocalizedName.Name, true));
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

            var selectedInheritables = inheritables.ToDictionary(item => item.Key, item => item.Value);
            var selectedInheritablesWithLocalizedNames = selectedInheritables
                .Select(inheritable => new { Name = inheritable.Key, LocalizedName = Localizer.GetString(inheritable.Key, false) })
                .OrderBy(item => item.LocalizedName);

            foreach (var selectedInheritablesWithLocalizedName in selectedInheritablesWithLocalizedNames)
            {
                switch (selectedInheritablesWithLocalizedName.Name)
                {
                    case nameof(tag.Icon):
                        DrawInheritable(nameof(tag.Icon), false, true, tag.Icon);
                        break;
                    case nameof(tag.IsIconVisibleInChat):
                        DrawInheritable(nameof(tag.IsIconVisibleInChat), tag.IsIconVisibleInChat);
                        break;
                    case nameof(tag.IsIconVisibleInNameplates):
                        DrawInheritable(nameof(tag.IsIconVisibleInNameplates), tag.IsIconVisibleInNameplates);
                        break;
                    case nameof(tag.Text):
                        DrawInheritable(nameof(tag.Text), tag.Text);
                        break;
                    case nameof(tag.TextColor):
                        DrawInheritable(nameof(tag.TextColor), tag.TextColor);
                        break;
                    case nameof(tag.TextGlowColor):
                        DrawInheritable(nameof(tag.TextGlowColor), tag.TextGlowColor);
                        break;
                    case nameof(tag.IsTextItalic):
                        DrawInheritable(nameof(tag.IsTextItalic), tag.IsTextItalic);
                        break;
                    case nameof(tag.IsTextVisibleInChat):
                        DrawInheritable(nameof(tag.IsTextVisibleInChat), tag.IsTextVisibleInChat);
                        break;
                    case nameof(tag.IsTextVisibleInNameplates):
                        DrawInheritable(nameof(tag.IsTextVisibleInNameplates), tag.IsTextVisibleInNameplates);
                        break;
                    case nameof(tag.TagPositionInChat):
                        DrawInheritable(nameof(tag.TagPositionInChat), true, false, tag.TagPositionInChat);
                        break;
                    case nameof(tag.TagPositionInNameplates):
                        DrawInheritable(nameof(tag.TagPositionInNameplates), true, false, tag.TagPositionInNameplates);
                        break;
                    case nameof(tag.TagTargetInNameplates):
                        DrawInheritable(nameof(tag.TagTargetInNameplates), true, false, tag.TagTargetInNameplates);
                        break;
                    case nameof(tag.GameObjectNamesToApplyTo):
                        DrawInheritable(nameof(tag.GameObjectNamesToApplyTo), tag.GameObjectNamesToApplyTo);
                        break;
                    default:
                        break;
                }
            }


            ImGui.TreePop();
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
