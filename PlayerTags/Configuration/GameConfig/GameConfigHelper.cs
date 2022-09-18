using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerTags.Configuration.GameConfig
{
    public class GameConfigHelper
    {
        private static GameConfigHelper instance = null;
        private unsafe static ConfigModule* configModule = null;

        public static GameConfigHelper Instance
        {
            get
            {
                instance ??= new GameConfigHelper();
                return instance;
            }
        }

        private GameConfigHelper()
        {
            unsafe
            {
                configModule = ConfigModule.Instance();
            }
        }

        private int? GetIntValue(ConfigOption option)
        {
            int? value = null;

            unsafe
            {
                var index = configModule->GetIndex(option);
                if (index.HasValue)
                    value = configModule->GetIntValue(index.Value);
            }

            return value;
        }

        public LogNameType? GetLogNameType()
        {
            LogNameType? logNameType = null;
            int? value = GetIntValue(ConfigOption.LogNameType);

            if (value.HasValue)
            {
                switch (value)
                {
                    case 0:
                        logNameType = LogNameType.FullName;
                        break;
                    case 1:
                        logNameType = LogNameType.LastNameShorted;
                        break;
                    case 2:
                        logNameType = LogNameType.FirstNameShorted;
                        break;
                    case 3:
                        logNameType = LogNameType.Initials;
                        break;
                }
            }

            return logNameType;
        }
    }
}
