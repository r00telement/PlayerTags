using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Runtime.InteropServices;

namespace FFXIVClientStructs.FFXIV.Client.UI.Agent
{
    [StructLayout(LayoutKind.Explicit)]
    public struct AgentContextItemData
    {
        [FieldOffset(0x0)] public ushort AtkValuesCount;
        [FieldOffset(0x8)] public AtkValue AtkValues;
        [FieldOffset(0x428)] public byte Actions;
    }
}
