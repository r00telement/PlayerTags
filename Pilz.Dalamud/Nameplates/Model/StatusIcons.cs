using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pilz.Dalamud.Nameplates.Model
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum StatusIcons
    {
        Disconnecting = 061503,
        InDuty = 061506,
        ViewingCutscene = 061508,
        Busy = 061509,
        Idle = 061511,
        DutyFinder = 061517,
        PartyLeader = 061521,
        PartyMember = 061522,
        RolePlaying = 061545,
        GroupPose = 061546,
        NewAdventurer = 061523,
        Mentor = 061540,
        MentorPvE = 061542,
        MentorCrafting = 061543,
        MentorPvP = 061544,
        Returner = 061547,
    }
}
