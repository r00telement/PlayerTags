using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Plugin.Internal;
using PlayerTags.Configuration;
using PlayerTags.Data;
using PlayerTags.Features;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PlayerTags
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "PlayerTags";
        private const string c_CommandName = "/playertags";
        private const string c_ChatTwo_InternalPluginName = "ChatTwo";

        private PluginConfiguration m_PluginConfiguration;
        private PluginData m_PluginData;
        private PluginConfigurationUI m_PluginConfigurationUI;

        private LinkSelfInChatFeature m_LinkSelfInChatFeature;
        private CustomTagsContextMenuFeature m_CustomTagsContextMenuFeature;
        private NameplateTagTargetFeature m_NameplatesTagTargetFeature;
        private ChatTagTargetFeature m_ChatTagTargetFeature;

        public Plugin(DalamudPluginInterface pluginInterface)
        {
            PluginServices.Initialize(pluginInterface);

            m_PluginConfiguration = PluginServices.DalamudPluginInterface.GetPluginConfig() as PluginConfiguration ?? new PluginConfiguration();
            m_PluginData = new PluginData(m_PluginConfiguration);
            m_PluginConfigurationUI = new PluginConfigurationUI(m_PluginConfiguration, m_PluginData);

            Localizer.SetLanguage(PluginServices.DalamudPluginInterface.UiLanguage);
            PluginServices.DalamudPluginInterface.LanguageChanged += DalamudPluginInterface_LanguageChanged;

            PluginServices.DalamudPluginInterface.UiBuilder.Draw += UiBuilder_Draw;
            PluginServices.DalamudPluginInterface.UiBuilder.OpenConfigUi += UiBuilder_OpenConfigUi;
            PluginServices.CommandManager.AddHandler(c_CommandName, new CommandInfo((string command, string arguments) =>
            {
                UiBuilder_OpenConfigUi();
            }) { HelpMessage = "Shows the config" });
            m_LinkSelfInChatFeature = new LinkSelfInChatFeature(m_PluginConfiguration, m_PluginData);
            m_LinkSelfInChatFeature.ShouldRemovePlayerNameTextPayload += LinkSelfInChatFeature_ShouldRemovePlayerNameTextPayload;
            m_CustomTagsContextMenuFeature = new CustomTagsContextMenuFeature(m_PluginConfiguration, m_PluginData);
            m_NameplatesTagTargetFeature = new NameplateTagTargetFeature(m_PluginConfiguration, m_PluginData);
            m_ChatTagTargetFeature = new ChatTagTargetFeature(m_PluginConfiguration, m_PluginData);
        }

        public void Dispose()
        {
            m_ChatTagTargetFeature.Dispose();
            m_NameplatesTagTargetFeature.Dispose();
            m_CustomTagsContextMenuFeature.Dispose();
            m_LinkSelfInChatFeature.Dispose();
            PluginServices.DalamudPluginInterface.LanguageChanged -= DalamudPluginInterface_LanguageChanged;
            PluginServices.CommandManager.RemoveHandler(c_CommandName);
            PluginServices.DalamudPluginInterface.UiBuilder.OpenConfigUi -= UiBuilder_OpenConfigUi;
            PluginServices.DalamudPluginInterface.UiBuilder.Draw -= UiBuilder_Draw;
        }

        private bool LinkSelfInChatFeature_ShouldRemovePlayerNameTextPayload(object sender)
        {
            return !IsChatTwoActive();
        }

        private bool IsChatTwoActive()
        {
            var file = Path.Combine(Path.GetDirectoryName(PluginServices.DalamudPluginInterface.ConfigDirectory.FullName), @"ChatTwo\chat-log.db");
            var exists = File.Exists(file);
            return exists;
        }

        private void DalamudPluginInterface_LanguageChanged(string langCode)
        {
            Localizer.SetLanguage(langCode);
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
    }
}
