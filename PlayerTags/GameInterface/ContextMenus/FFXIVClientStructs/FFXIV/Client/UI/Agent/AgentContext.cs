using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Runtime.InteropServices;

namespace FFXIVClientStructs.FFXIV.Client.UI.Agent
{
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct AgentContext
    {
        public static AgentContext* Instance() => (AgentContext*)System.Framework.Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentByInternalId(AgentId.Context);

        [FieldOffset(0x0)] public AgentInterface AgentInterface;
        [FieldOffset(0x670)] public unsafe byte SelectedIndex;
        [FieldOffset(0x690)] public byte* Unk1;
        [FieldOffset(0xD08)] public byte* SubContextMenuTitle;
        [FieldOffset(0xD18)] public unsafe AgentContextItemData* ItemData;
        [FieldOffset(0xE08)] public Utf8String ObjectName;
        [FieldOffset(0xEE0)] public ulong GameObjectContentId;
        [FieldOffset(0xEF0)] public uint GameObjectId;
        [FieldOffset(0xF00)] public ushort GameObjectWorldId;
        [FieldOffset(0x1740)] public bool IsSubContextMenu;
    }
}
