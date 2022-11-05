using Pilz.Dalamud.ActivityContexts;

namespace PlayerTags.Data
{
    public static class ActivityContextHelper
    {
        public static bool GetIsVisible(ActivityType playerContext, bool desiredPveDutyVisibility, bool desiredPvpDutyVisibility, bool desiredOthersVisibility)
        {
            bool isVisible = false;

            if (playerContext.HasFlag(ActivityType.PveDuty))
                isVisible |= desiredPveDutyVisibility;

            if (playerContext.HasFlag(ActivityType.PvpDuty))
                isVisible |= desiredPvpDutyVisibility;

            if (playerContext == ActivityType.None)
                isVisible |= desiredOthersVisibility;

            return isVisible;
        }
    }
}
