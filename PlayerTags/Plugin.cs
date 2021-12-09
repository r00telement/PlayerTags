using Dalamud.Game.Command;
using Dalamud.Plugin;
using PlayerTags.Configuration;
using PlayerTags.Data;
using PlayerTags.Features;
using XivCommon;

namespace PlayerTags
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Player Tags";
        private const string c_CommandName = "/playertags";

        private XivCommonBase m_XivCommon;

        private PluginConfiguration m_PluginConfiguration;
        private PluginData m_PluginData;
        private PluginConfigurationUI m_PluginConfigurationUI;

        private CustomTagsContextMenuFeature m_CustomTagsContextMenuFeature;
        private NameplatesTagTargetFeature m_NameplatesTagTargetFeature;
        private ChatTagTargetFeature m_ChatTagTargetFeature;

        public Plugin(DalamudPluginInterface pluginInterface)
        {
            PluginServices.Initialize(pluginInterface);

            m_PluginConfiguration = PluginServices.DalamudPluginInterface.GetPluginConfig() as PluginConfiguration ?? new PluginConfiguration();
            m_PluginData = new PluginData(m_PluginConfiguration);
            m_PluginConfigurationUI = new PluginConfigurationUI(m_PluginConfiguration, m_PluginData);

            m_XivCommon = new XivCommonBase(XivCommon.Hooks.ContextMenu);
            PluginServices.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
            PluginServices.DalamudPluginInterface.UiBuilder.Draw += UiBuilder_Draw;
            PluginServices.DalamudPluginInterface.UiBuilder.OpenConfigUi += UiBuilder_OpenConfigUi;
            PluginServices.CommandManager.AddHandler(c_CommandName, new CommandInfo((string command, string arguments) =>
            {
                m_PluginConfiguration.IsVisible = true;
                m_PluginConfiguration.Save(m_PluginData);
            }) { HelpMessage = "Shows the config" });
            m_CustomTagsContextMenuFeature = new CustomTagsContextMenuFeature(m_XivCommon, m_PluginConfiguration, m_PluginData);
            m_NameplatesTagTargetFeature = new NameplatesTagTargetFeature(m_PluginConfiguration, m_PluginData);
            m_ChatTagTargetFeature = new ChatTagTargetFeature(m_PluginConfiguration, m_PluginData);
        }

        //private ExcelSheet<ContentFinderCondition> _contentFinderConditionsSheet;
        private void ClientState_TerritoryChanged(object? sender, ushort e)
        {
            //_contentFinderConditionsSheet = DataManager.GameData.GetExcelSheet<ContentFinderCondition>();
            //var content = _contentFinderConditionsSheet.FirstOrDefault(t => t.TerritoryType.Row == PluginServices.ClientState.TerritoryType);
            //content.ContentMemberType.Row
        }

        public void Dispose()
        {
            m_ChatTagTargetFeature.Dispose();
            m_NameplatesTagTargetFeature.Dispose();
            m_CustomTagsContextMenuFeature.Dispose();                    
            PluginServices.CommandManager.RemoveHandler(c_CommandName);
            PluginServices.DalamudPluginInterface.UiBuilder.OpenConfigUi -= UiBuilder_OpenConfigUi;
            PluginServices.DalamudPluginInterface.UiBuilder.Draw -= UiBuilder_Draw;
            PluginServices.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;
            m_XivCommon.Dispose();
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
