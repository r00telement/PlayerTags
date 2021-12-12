namespace PlayerTags.Data
{
    public static class ActivityContextHelper
    {
        public static bool GetIsVisible(ActivityContext playerContext, bool desiredPveDutyVisibility, bool desiredPvpDutyVisibility, bool desiredOthersVisibility)
        {
            bool isVisible = false;

            if (playerContext.HasFlag(ActivityContext.PveDuty))
            {
                isVisible |= desiredPveDutyVisibility;
            }

            if (playerContext.HasFlag(ActivityContext.PvpDuty))
            {
                isVisible |= desiredPvpDutyVisibility;
            }

            if (playerContext == ActivityContext.None)
            {
                isVisible |= desiredOthersVisibility;
            }

            return isVisible;
        }
    }
}
