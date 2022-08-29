using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerTags.Data
{
    public class ActivityContextManager : IDisposable
    {
        private ActivityContext m_CurrentActivityContext;

        public ActivityContext CurrentActivityContext => m_CurrentActivityContext;

        public ActivityContextManager()
        {
            m_CurrentActivityContext = ActivityContext.None;
            PluginServices.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
        }

        private void ClientState_TerritoryChanged(object? sender, ushort e)
        {
            m_CurrentActivityContext = ActivityContext.None;

            var contentFinderConditionsSheet = PluginServices.DataManager.GameData.GetExcelSheet<ContentFinderCondition>();
            if (contentFinderConditionsSheet != null)
            {
                var foundContentFinderCondition = contentFinderConditionsSheet.FirstOrDefault(contentFinderCondition => contentFinderCondition.TerritoryType.Row == PluginServices.ClientState.TerritoryType);
                if (foundContentFinderCondition != null)
                {
                    if (foundContentFinderCondition.PvP)
                    {
                        m_CurrentActivityContext = ActivityContext.PvpDuty;
                    }
                    else
                    {
                        m_CurrentActivityContext = ActivityContext.PveDuty;
                    }
                }
            }
        }

        public void Dispose()
        {
            PluginServices.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;
        }
    }
}
