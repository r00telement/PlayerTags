using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pilz.Dalamud.ActivityContexts
{
    public class ActivityContext
    {
        public ActivityType ActivityType { get; init; }
        public ZoneType ZoneType { get; init; }

        public ActivityContext(ActivityType activityType, ZoneType zoneType)
        {
            ActivityType = activityType;
            ZoneType = zoneType;
        }

        public bool IsInDuty
        {
            get => ZoneType != ZoneType.Overworld;
        }
    }
}
