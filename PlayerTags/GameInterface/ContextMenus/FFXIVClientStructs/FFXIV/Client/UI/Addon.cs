using System.Runtime.InteropServices;

namespace FFXIVClientStructs.FFXIV.Client.UI
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Addon
    {
        [FieldOffset(0x1D2)] public ushort ParentAddonId;
    }
}
