using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Pilz.Dalamud.ActivityContexts;
using Pilz.Dalamud.Nameplates.Model;
using Pilz.Dalamud.Tools;
using Pilz.Dalamud.Tools.Strings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pilz.Dalamud.Nameplates.Tools
{
    public static class NameplateUpdateFactory
    {
        public static void ApplyNameplateChanges(NameplateChangesProps props)
        {
            foreach (NameplateElements element in Enum.GetValues(typeof(NameplateElements)))
            {
                var change = props.Changes.GetProps(element);
                StringUpdateFactory.ApplyStringChanges(change);
            }
        }

        public static bool ApplyStatusIconWithPrio(ref int statusIcon, int newStatusIcon, StringChange stringChange, ActivityContext activityContext, StatusIconPriorizer priorizer)
        {
            var isPrio = priorizer.IsPriorityIcon(statusIcon, activityContext);

            if (!isPrio)
            {
                var fontIcon = StatusIconFontConverter.GetBitmapFontIconFromStatusIcon((StatusIcons)statusIcon);

                if (fontIcon != null)
                {
                    // Set new font icon as string change
                    var iconPayload = new IconPayload(fontIcon.Value);
                    stringChange.Payloads.Insert(0, iconPayload);

                    // Use new status icon as status icon
                    statusIcon = newStatusIcon;
                }
            }

            return isPrio;
        }
    }
}
