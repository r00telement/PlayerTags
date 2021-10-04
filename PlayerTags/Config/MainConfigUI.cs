using Dalamud.Interface.Components;
using Dalamud.Logging;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using PlayerTags.Resources;
using System;
using System.Linq;
using System.Numerics;

namespace PlayerTags.Config
{
    public class MainConfigUI
    {
        private MainConfig m_Config;

        private bool m_OpenPopupRequested;

        private CustomColorConfig? m_ColorPickerPopupDataContext;

        private bool m_IsVisible = false;
        public bool IsVisible
        {
            get { return m_IsVisible; }
            set { m_IsVisible = value; }
        }

        public MainConfigUI(MainConfig config)
        {
            m_Config = config;
        }

        private string GetLocString<TEnum>(bool isDescription)
            where TEnum : Enum
        {
            return GetLocString(typeof(TEnum).Name, isDescription);
        }

        private string GetLocString<TEnum>(TEnum enumValue, bool isDescription)
            where TEnum : Enum
        {
            return GetLocString($"{typeof(TEnum).Name}_{enumValue}", isDescription);
        }

        private string GetLocString(string locStringId, bool isDescription)
        {
            string completeLocStringId = $"Loc_{locStringId}";

            if (isDescription)
            {
                completeLocStringId += "_Description";
            }

            return GetLocString(completeLocStringId);
        }

        private string GetLocString(string completeLocStringId)
        {
            string? value = Strings.ResourceManager.GetString(completeLocStringId, Strings.Culture);
            if (value != null)
            {
                return value;
            }

            PluginLog.Error($"Failed to get localized string for id {completeLocStringId}");
            return completeLocStringId;
        }

        public void Draw()
        {
            if (m_Config == null || !IsVisible)
            {
                return;
            }

            if (ImGui.Begin(Strings.Loc_Static_PluginName, ref m_IsVisible))
            {
                if (ImGui.BeginTabBar("MainTabBar"))
                {
                    if (ImGui.BeginTabItem(Strings.Loc_Static_General))
                    {
                        DrawHeading(Strings.Loc_Static_Nameplates);
                        DrawEnumComboBox(
                            ref m_Config.FreeCompanyVisibility,
                            () => m_Config.Save());
                        DrawEnumComboBox(
                            ref m_Config.TitleVisibility,
                            () => m_Config.Save());
                        DrawEnumComboBox(
                            ref m_Config.TitlePosition,
                            () => m_Config.Save());
                        DrawHeading(Strings.Loc_Static_Development);
                        DrawCheckbox(
                            nameof(m_Config.IsPlayerNameRandomlyGenerated),
                            ref m_Config.IsPlayerNameRandomlyGenerated,
                            () => m_Config.Save());

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem(Strings.Loc_Static_RoleAndJobTags))
                    {
                        DrawEnumComboBox(
                            ref m_Config.RoleTag.Format,
                            () => m_Config.Save());
                        DrawTagConfig(m_Config.RoleTag);

                        DrawHeading(Strings.Loc_Static_Roles);
                        if (ImGui.BeginTabBar("JobAndRolesTabBar"))
                        {
                            foreach (var rolePair in m_Config.RoleTag.RoleOverrideConfigs)
                            {
                                var role = rolePair.Key;
                                var roleConfig = rolePair.Value;

                                if (ImGui.BeginTabItem(GetLocString($"{role.GetType().Name}_{role}", false)))
                                {
                                    DrawCheckbox(
                                        $"{roleConfig.GetType().Name}_{nameof(roleConfig.IsEnabled)}",
                                        ref roleConfig.IsEnabled,
                                        () => m_Config.Save());
                                    DrawTextBox(
                                        $"{roleConfig.GetType().Name}_{nameof(roleConfig.Name)}",
                                        ref roleConfig.Name,
                                        () => m_Config.Save());
                                    DrawOptionalCustomColor(
                                        $"{roleConfig.CustomColor.GetType().Name}_IsEnabled",
                                        roleConfig.CustomColor.Id.ToString()!,
                                        roleConfig.CustomColor);
                                    DrawHeading(Strings.Loc_Static_Jobs);
                                    foreach (var key in roleConfig.JobOverrideConfigs.Keys.OrderBy(key => key))
                                    {
                                        if (string.IsNullOrEmpty(key))
                                        {
                                            continue;
                                        }

                                        JobOverrideConfig jobConfig = roleConfig.JobOverrideConfigs[key];

                                        ImGui.Columns(2, "columns", false);
                                        ImGui.SetColumnWidth(0, 42);
                                        ImGui.Text(key);
                                        ImGui.NextColumn();
                                        DrawOptionalCustomColor(
                                            $"{roleConfig.CustomColor.GetType().Name}_IsEnabled",
                                            key,
                                            jobConfig.CustomColor);

                                        ImGui.Columns();
                                    }

                                    ImGui.EndTabItem();
                                }
                            }

                            ImGui.EndTabBar();
                        }

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem(Strings.Loc_Static_CustomTags))
                    {
                        if (ImGui.Button(Strings.Loc_Static_AddCustomTag))
                        {
                            m_Config.CustomTagConfigs.Add(new CustomTagConfig());
                            m_Config.Save();
                        }

                        if (!m_Config.CustomTagConfigs.Any())
                        {
                            ImGui.Text(Strings.Loc_Static_NoCustomTagsAdded);
                        }
                        else
                        {
                            foreach (var customTagConfig in m_Config.CustomTagConfigs.ToArray())
                            {
                                ImGui.PushID(customTagConfig.GetHashCode().ToString());
                                ImGui.Separator();
                                DrawTextBox(
                                    $"{customTagConfig.GetType().Name}_{nameof(customTagConfig.Name)}",
                                    ref customTagConfig.Name,
                                    () => { m_Config.Save(); });
                                DrawOptionalCustomColor(
                                    $"{customTagConfig.CustomColor.GetType().Name}_IsEnabled",
                                    customTagConfig.CustomColor.Id.ToString()!,
                                    customTagConfig.CustomColor);
                                DrawTextBox(
                                    $"{customTagConfig.GetType().Name}_{nameof(customTagConfig.FormattedGameObjectNames)}",
                                    ref customTagConfig.FormattedGameObjectNames,
                                    () => { m_Config.Save(); });
                                DrawTagConfig(customTagConfig);
                                ImGui.Spacing();
                                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.4f, 0.1f, 0.1f, 1));
                                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.6f, 0.2f, 0.2f, 1));
                                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.6f, 0.2f, 0.2f, 1));
                                if (ImGui.Button(Strings.Loc_Static_RemoveCustomTag))
                                {
                                    m_Config.CustomTagConfigs.Remove(customTagConfig);
                                    m_Config.Save();
                                }
                                ImGui.PopStyleColor();
                                ImGui.PopStyleColor();
                                ImGui.PopStyleColor();
                                ImGui.PopID();
                            }
                        }

                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }

                if (m_OpenPopupRequested == true)
                {
                    m_OpenPopupRequested = false;
                    ImGui.OpenPopup("ColorPickerPopup");
                }

                ImGui.SetNextWindowSize(new Vector2(400, 284));
                if (ImGui.BeginPopup("ColorPickerPopup"))
                {
                    DrawUIColorPicker(
                        (UIColor value) =>
                        {
                            if (m_ColorPickerPopupDataContext != null)
                            {
                                m_ColorPickerPopupDataContext.Id = (ushort)value.RowId;
                                m_ColorPickerPopupDataContext = null;
                                m_Config.Save();
                            }

                            ImGui.CloseCurrentPopup();
                        });

                    ImGui.EndPopup();
                }

                ImGui.End();
            }
        }

        private void DrawTagConfig(TagConfig tagConfig)
        {
            if (m_Config == null || !IsVisible)
            {
                return;
            }

            DrawHeading(Strings.Loc_Static_ChatTag);
            ImGui.PushID("Chat");
            DrawEnumComboBox(
                ref tagConfig.ChatPosition,
                () => m_Config.Save());
            ImGui.PopID();
            DrawHeading(Strings.Loc_Static_NameplateTag);
            ImGui.PushID("Nameplate");
            DrawEnumComboBox(
                ref tagConfig.NameplatePosition,
                () => m_Config.Save());
            DrawEnumComboBox(
                ref tagConfig.NameplateElement,
                () => m_Config.Save());
            ImGui.PopID();
        }

        private void DrawOptionalCustomColor(string locStringId, string colorId, CustomColorConfig customColorConfig)
        {
            if (customColorConfig.Id.HasValue)
            {
                DrawColorButton(
                    colorId,
                    UIColorHelper.ToColor(customColorConfig.Id.Value),
                    () =>
                    {
                        m_ColorPickerPopupDataContext = customColorConfig;
                        m_OpenPopupRequested = true;
                    });

                ImGui.SameLine();
            }

            var isChecked = customColorConfig.Id != null;
            DrawCheckbox(
                locStringId,
                ref isChecked,
                () =>
                {
                    if (!isChecked)
                    {
                        customColorConfig.Id = null;
                        m_Config.Save();
                    }
                    else
                    {
                        customColorConfig.Id = (ushort)UIColorHelper.UIColors.First().RowId;
                        m_Config.Save();
                    }
                });
        }

        private void DrawSeparator()
        {
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();
        }

        private void DrawHeading(string label)
        {
            ImGui.TextColored(new Vector4(0.7f, 0.6f, 1f, 1f), label);
        }

        private void DrawEnumComboBox<TEnum>(ref TEnum currentValue, System.Action changed)
            where TEnum : Enum
        {
            ImGui.Text(GetLocString<TEnum>(false));
            ImGuiComponents.HelpMarker(GetLocString<TEnum>(true));

            if (ImGui.BeginCombo($"###{currentValue.GetType().Name}", GetLocString(currentValue, false)))
            {
                foreach (string enumValueString in typeof(TEnum).GetEnumNames())
                {
                    TEnum enumValue = (TEnum)Enum.Parse(typeof(TEnum), enumValueString);
                    bool isSelected = enumValueString == currentValue.ToString();
                    if (ImGui.Selectable($"{GetLocString(enumValue, false)}###{enumValueString}", isSelected))
                    {
                        currentValue = (TEnum)Enum.Parse(typeof(TEnum), enumValueString);
                        ImGui.SetItemDefaultFocus();
                        changed();
                    }

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(GetLocString(enumValue, true));
                    }
                }

                ImGui.EndCombo();
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(GetLocString(currentValue, true));
            }
        }

        private void DrawCheckbox(string locStringId, ref bool isChecked, System.Action changed)
        {
            if (ImGui.Checkbox(GetLocString(locStringId, false), ref isChecked))
            {
                changed();
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(GetLocString(locStringId, true));
            }
        }

        private void DrawColorButton(string colorId, Vector4 color, System.Action clicked)
        {
            if (ImGui.ColorButton(colorId, color))
            {
                clicked();
            }
        }

        private void DrawTextBox(string locStringId, ref string text, System.Action changed)
        {
            ImGui.Text(GetLocString(locStringId, false));
            ImGuiComponents.HelpMarker(GetLocString(locStringId, true));

            var oldText = text;
            ImGui.InputText($"###{locStringId}", ref text, 1024);
            if (text != oldText)
            {
                changed();
            }
        }

        private void DrawUIColorPicker(System.Action<UIColor> clicked)
        {
            ImGui.PushID(clicked.GetHashCode());
            ImGui.Columns(12, "columns", false);
            foreach (var uiColor in UIColorHelper.UIColors)
            {
                DrawColorButton(
                    uiColor.RowId.ToString(),
                    UIColorHelper.ToColor(uiColor),
                    () =>
                    {
                        clicked(uiColor);
                    });

                ImGui.NextColumn();
            }
            ImGui.Columns();
            ImGui.PopID();
        }
    }
}
