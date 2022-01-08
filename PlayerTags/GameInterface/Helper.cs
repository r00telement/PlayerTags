using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace PlayerTags.GameInterface
{
    public static class Helper
    {
        public static SeString ReadSeString(IntPtr ptr)
        {
            if (TryReadStringBytes(ptr, out var bytes) && bytes != null)
            {
                return SeString.Parse(bytes);
            }

            return new SeString();
        }

        public static bool TryReadSeString(IntPtr ptr, out SeString? seString)
        {
            seString = null;

            if (TryReadStringBytes(ptr, out var bytes) && bytes != null)
            {
                seString = SeString.Parse(bytes);
                return true;
            }

            return false;
        }

        public static string? ReadString(IntPtr ptr)
        {
            if (TryReadStringBytes(ptr, out var bytes) && bytes != null)
            {
                return Encoding.UTF8.GetString(bytes);
            }

            return null;
        }

        public static bool TryReadString(IntPtr ptr, out string? str)
        {
            str = null;

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

        public static IntPtr Allocate(SeString seString)
        {
            var bytes = seString.Encode();

            IntPtr pointer = Marshal.AllocHGlobal(bytes.Length + 1);
            Marshal.Copy(bytes, 0, pointer, bytes.Length);
            Marshal.WriteByte(pointer, bytes.Length, 0);

            return pointer;
        }

        public static void Free(ref IntPtr ptr)
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
    }
}
