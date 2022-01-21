using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PlayerTags.GameInterface
{
    public static class GameInterfaceHelper
    {
        public static SeString ReadSeString(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return new SeString();
            }

            if (TryReadStringBytes(ptr, out var bytes) && bytes != null)
            {
                return SeString.Parse(bytes);
            }

            return new SeString();
        }

        public static bool TryReadSeString(IntPtr ptr, out SeString? seString)
        {
            seString = null;
            if (ptr == IntPtr.Zero)
            {
                return false;
            }

            if (TryReadStringBytes(ptr, out var bytes) && bytes != null)
            {
                seString = SeString.Parse(bytes);
                return true;
            }

            return false;
        }

        public static string? ReadString(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            if (TryReadStringBytes(ptr, out var bytes) && bytes != null)
            {
                return Encoding.UTF8.GetString(bytes);
            }

            return null;
        }

        public static bool TryReadString(IntPtr ptr, out string? str)
        {
            str = null;
            if (ptr == IntPtr.Zero)
            {
                return false;
            }

            if (TryReadStringBytes(ptr, out var bytes) && bytes != null)
            {
                str = Encoding.UTF8.GetString(bytes);
                return true;
            }

            return false;
        }

        public static bool TryReadStringBytes(IntPtr ptr, out byte[]? bytes)
        {
            bytes = null;
            if (ptr == IntPtr.Zero)
            {
                return false;
            }

            var size = 0;
            while (Marshal.ReadByte(ptr, size) != 0)
            {
                size++;
            }

            bytes = new byte[size];
            Marshal.Copy(ptr, bytes, 0, size);

            return true;
        }

        public static IntPtr PluginAllocate(byte[] bytes)
        {
            IntPtr pointer = Marshal.AllocHGlobal(bytes.Length + 1);
            Marshal.Copy(bytes, 0, pointer, bytes.Length);
            Marshal.WriteByte(pointer, bytes.Length, 0);

            return pointer;
        }

        public static IntPtr PluginAllocate(SeString seString)
        {
            return PluginAllocate(seString.Encode());
        }

        public static void PluginFree(ref IntPtr ptr)
        {
            Marshal.FreeHGlobal(ptr);
            ptr = IntPtr.Zero;
        }

        public static byte[] NullTerminate(this byte[] bytes)
        {
            if (bytes.Length == 0 || bytes[bytes.Length - 1] != 0)
            {
                var newBytes = new byte[bytes.Length + 1];
                Array.Copy(bytes, newBytes, bytes.Length);
                newBytes[^1] = 0;

                return newBytes;
            }

            return bytes;
        }

        public static unsafe IntPtr GameUIAllocate(ulong size)
        {
            return (IntPtr)IMemorySpace.GetUISpace()->Malloc(size, 0);
        }

        public static unsafe void GameFree(ref IntPtr ptr, ulong size)
        {
            if (ptr == IntPtr.Zero)
            {
                return;
            }

            IMemorySpace.Free((void*)ptr, size);
            ptr = IntPtr.Zero;
        }
    }
}
