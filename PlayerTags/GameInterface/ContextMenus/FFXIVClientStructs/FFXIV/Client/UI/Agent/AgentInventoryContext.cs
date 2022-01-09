using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Runtime.InteropServices;

namespace FFXIVClientStructs.FFXIV.Client.UI.Agent
{
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct AgentInventoryContext
    {
        public static AgentInventoryContext* Instance() => (AgentInventoryContext*)System.Framework.Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentByInternalId(AgentId.InventoryContext);

        [FieldOffset(0x0)] public AgentInterface AgentInterface;
        [FieldOffset(0x558)] public unsafe byte Actions;
        [FieldOffset(0x5F8)] public uint ItemId;
        [FieldOffset(0x5FC)] public uint ItemCount;
        [FieldOffset(0x604)] public bool IsHighQuality;
        [FieldOffset(0x670)] public unsafe byte SelectedIndex;
        [FieldOffset(0x690)] public byte* Unk1;
        [FieldOffset(0xD08)] public byte* SubContextMenuTitle;
        [FieldOffset(0x1740)] public bool IsSubContextMenu;
    }
}
