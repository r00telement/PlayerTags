using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using PlayerTags.Resources;
using System;
using System.Linq;
using System.Numerics;

namespace PlayerTags
{
    public class PluginConfigurationUI
    {
        private PluginConfiguration m_PluginConfiguration;
        private PluginData m_PluginData;
        private ClientState m_ClientState;
        private PartyList m_PartyList;
        private InheritableValue<ushort>? m_ColorPickerPopupDataContext;

        public PluginConfigurationUI(PluginConfiguration config, PluginData pluginData, ClientState clientState, PartyList partyList)
        {
            m_PluginConfiguration = config;
            m_PluginData = pluginData;
            m_ClientState = clientState;
            m_PartyList = partyList;
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

                DrawHeading(Strings.Loc_Static_General);
                ImGui.TreePush();
                DrawCheckbox(nameof(m_PluginConfiguration.IsCustomTagContextMenuEnabled), true, ref m_PluginConfiguration.IsCustomTagContextMenuEnabled, () => m_PluginConfiguration.Save(m_PluginData));
                ImGui.TreePop();

                DrawHeading(Strings.Loc_Static_Nameplates);
                ImGui.TreePush();
                DrawComboBox(true, true, false, ref m_PluginConfiguration.NameplateFreeCompanyVisibility, () => m_PluginConfiguration.Save(m_PluginData));
                DrawComboBox(true, true, false, ref m_PluginConfiguration.NameplateTitleVisibility, () => m_PluginConfiguration.Save(m_PluginData));
                DrawComboBox(true, true, false, ref m_PluginConfiguration.NameplateTitlePosition, () => m_PluginConfiguration.Save(m_PluginData));
                ImGui.TreePop();

                DrawHeading(Strings.Loc_Static_Development);
                ImGui.TreePush();
                DrawCheckbox(nameof(m_PluginConfiguration.IsPlayerNameRandomlyGenerated), true, ref m_PluginConfiguration.IsPlayerNameRandomlyGenerated, () => m_PluginConfiguration.Save(m_PluginData));
                ImGui.TreePop();


                DrawHeading(Strings.Loc_Static_Tags);
                ImGui.TreePush();
                Draw(m_PluginData.AllTags);
                ImGui.TreePop();


                DrawHeading(Strings.Loc_Static_PartyAssign);
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

                    var drawPartyMember = (string playerName) =>
                    {
                        ImGui.PushID(playerName);

                        ImGui.TableNextRow();

                        ImGui.TableNextColumn();
                        ImGui.Text(playerName);

                        foreach (Tag customTag in m_PluginData.CustomTags)
                        {
                            ImGui.PushID(customTag.GetHashCode().ToString());

                            ImGui.TableNextColumn();

                            bool isTagAssigned = customTag.IncludesGameObjectNameToApplyTo(playerName);
                            
                            DrawCheckbox("IsEnabled", false, ref isTagAssigned, () =>
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
                    };

                    foreach (var partyMember in m_PartyList)
                    {
                        drawPartyMember(partyMember.Name.TextValue);
                    }

                    if (m_PartyList.Length == 0 && m_ClientState.LocalPlayer != null)
                    {
                        drawPartyMember(m_ClientState.LocalPlayer.Name.TextValue);
                    }

                    ImGui.EndTable();
                }
                ImGui.TreePop();

                ImGui.End();
            }

            if (!m_PluginConfiguration.IsVisible)
            {
                m_PluginConfiguration.Save(m_PluginData);
            }
        }

        public void Draw(Tag tag)
        {
            ImGui.PushID(tag.GetHashCode().ToString());

            string collapsingHeaderName = tag.Name.Value;
            if (m_PluginData.CustomTags.Contains(tag))
            {
                if (tag.Text.InheritedValue != null)
                {
                    collapsingHeaderName = tag.Text.InheritedValue;
                }
                else
                {
                    collapsingHeaderName = "";
                }
            }

            bool isVisible = true;
            if (ImGui.CollapsingHeader($"{collapsingHeaderName}###{tag.GetHashCode()}", ref isVisible, tag.IsExpanded.Value ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None))
            {
                if (!tag.IsExpanded.Value)
                {
                    tag.IsExpanded.Value = true;
                    m_PluginConfiguration.Save(m_PluginData);
                }

                ImGui.TreePush();
                ImGui.BeginGroup();

                ImGui.BeginGroup();
                var addButtonSize = new Vector2(23, 23);
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

                ImGui.Text("");
                var addPopupPos = ImGui.GetCursorPos();
                addPopupPos.Y -= 4;
                if (ImGui.Button("+", addButtonSize))
                {
                    ImGui.OpenPopup("AddPopup");
                }
                ImGui.PopStyleColor();
                ImGui.PopStyleColor();
                ImGui.PopStyleColor();

                bool wasPaddingConsumed = false;
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 5));
                ImGui.SetNextWindowPos(ImGui.GetCursorScreenPos() + addPopupPos);
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
                ImGui.EndGroup();

                var selectedInheritables = tag.Inheritables.Where(inheritable => inheritable.Value.Behavior != InheritableBehavior.Inherit).ToDictionary(item => item.Key, item => item.Value);
                var selectedInheritablesWithLocalizedNames = selectedInheritables
                    .Select(inheritable => new { Name = inheritable.Key, LocalizedName = Localizer.GetString(inheritable.Key, false) })
                    .OrderBy(item => item.LocalizedName);

                foreach (var selectedInheritablesWithLocalizedName in selectedInheritablesWithLocalizedNames)
                {
                    ImGui.SameLine();
                    ImGui.BeginGroup();

                    ImGui.Indent();
                    ImGui.BeginChild(selectedInheritablesWithLocalizedName.Name, new Vector2(180, 50));

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

                    ImGui.EndChild();
                    ImGui.EndGroup();
                }

                if (m_PluginData.CustomTags.Contains(tag))
                {
                    ImGui.SameLine();
                    ImGui.BeginGroup();
                    ImGui.Indent();
                    ImGui.Text("");

                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.1f, 0.1f, 1));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.6f, 0.2f, 0.2f, 1));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.6f, 0.2f, 0.2f, 1));
                    if (ImGui.Button(Strings.Loc_Static_RemoveCustomTag))
                    {
                        m_PluginData.AllCustomTags.Children.Remove(tag);
                        m_PluginData.CustomTags.Remove(tag);
                        m_PluginConfiguration.Save(m_PluginData);
                    }
                    ImGui.PopStyleColor();
                    ImGui.PopStyleColor();
                    ImGui.PopStyleColor();
                    ImGui.EndGroup();
                }

                foreach (var childTag in tag.Children.ToArray())
                {
                    Draw(childTag);
                }

                if (tag == m_PluginData.AllCustomTags)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.1f, 0.3f, 0.1f, 1));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.2f, 0.6f, 0.2f, 1));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.2f, 0.6f, 0.2f, 1));
                    if (ImGui.Button(Strings.Loc_Static_AddCustomTag))
                    {
                        var newTag = new Tag(new LocalizedPluginString(nameof(PluginData.CustomTags)))
                        {
                            Text = "",
                            GameObjectNamesToApplyTo = ""
                        };

                        m_PluginData.CustomTags.Add(newTag);
                        newTag.Parent = m_PluginData.AllCustomTags;
                    }
                    ImGui.PopStyleColor();
                    ImGui.PopStyleColor();
                    ImGui.PopStyleColor();
                }

                ImGui.EndGroup();
                ImGui.TreePop();
            }
            else if (tag.IsExpanded.Value)
            {
                tag.IsExpanded.Value = false;
                m_PluginConfiguration.Save(m_PluginData);
            }

            ImGui.PopID();
        }

        private void DrawRemoveButton(IInheritable inheritable)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0, 0));
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.1f, 0.1f, 1));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.6f, 0.2f, 0.2f, 1));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.6f, 0.2f, 0.2f, 1));
            if (ImGui.Button($"x", new Vector2(23, 23)))
            {
                inheritable.Behavior = InheritableBehavior.Inherit;
                m_PluginConfiguration.Save(m_PluginData);
            }
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
            ImGui.Text(Localizer.GetString(localizedStringName, false));
            DrawCheckbox("IsEnabled", false, ref inheritable.Value, () =>
            {
                m_PluginConfiguration.Save(m_PluginData);
            });
        }

        private void DrawInheritable<TEnum>(string localizedStringName, bool shouldLocalizeNames, bool shouldOrderNames, InheritableValue<TEnum> inheritable)
            where TEnum : struct, Enum
        {
            ImGui.Text(Localizer.GetString(localizedStringName, false));

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
                DrawComboBox(false, shouldLocalizeNames, shouldOrderNames, ref inheritable.Value, () => { m_PluginConfiguration.Save(m_PluginData); });
            }

            ImGui.SameLine();
            DrawRemoveButton(inheritable);
        }

        private void DrawInheritable(string localizedStringName, InheritableValue<ushort> inheritable)
        {
            ImGui.Text(Localizer.GetString(localizedStringName, false));

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
            DrawRemoveButton(inheritable);
        }

        private void DrawInheritable(string localizedStringName, InheritableReference<string> inheritable)
        {
            ImGui.Text(Localizer.GetString(localizedStringName, false));

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
                DrawTextBox(localizedStringName, ref inheritable.Value, () => { m_PluginConfiguration.Save(m_PluginData); });
            }

            ImGui.SameLine();
            DrawRemoveButton(inheritable);
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
