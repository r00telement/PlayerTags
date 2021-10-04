using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text.SeStringHandling;

namespace PlayerTags
{
    public delegate void SetNameplateDelegate(GameObject gameObject, SeString name, SeString title, SeString freeCompany, ref bool isTitleVisible, ref bool isTitleAboveName, ref int iconId, out bool isNameChanged, out bool isTitleChanged, out bool isFreeCompanyChanged);
}
