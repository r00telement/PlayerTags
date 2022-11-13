using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Pilz.Dalamud.Nameplates.Model
{
    public class SafeNameplateInfo
    {
        public readonly IntPtr Pointer;
        public readonly RaptureAtkModule.NamePlateInfo Data;

        public SafeNameplateInfo(IntPtr pointer)
        {
            Pointer = pointer;
            Data = Marshal.PtrToStructure<RaptureAtkModule.NamePlateInfo>(Pointer);
        }

        internal IntPtr NameAddress => GetStringPtr(nameof(RaptureAtkModule.NamePlateInfo.Name));
        internal IntPtr FcNameAddress => GetStringPtr(nameof(RaptureAtkModule.NamePlateInfo.FcName));
        internal IntPtr TitleAddress => GetStringPtr(nameof(RaptureAtkModule.NamePlateInfo.Title));
        internal IntPtr DisplayTitleAddress => GetStringPtr(nameof(RaptureAtkModule.NamePlateInfo.DisplayTitle));
        internal IntPtr LevelTextAddress => GetStringPtr(nameof(RaptureAtkModule.NamePlateInfo.LevelText));

        public string Name => GetString(NameAddress);
        public string FcName => GetString(FcNameAddress);
        public string Title => GetString(TitleAddress);
        public string DisplayTitle => GetString(DisplayTitleAddress);
        public string LevelText => GetString(LevelTextAddress);

        //public bool IsPlayerCharacter() => XivApi.IsPlayerCharacter(Data.ObjectID.ObjectID);

        //public bool IsPartyMember() => XivApi.IsPartyMember(Data.ObjectID.ObjectID);

        //public bool IsAllianceMember() => XivApi.IsAllianceMember(Data.ObjectID.ObjectID);

        //public uint GetJobID() => GetJobId(Data.ObjectID.ObjectID);

        private unsafe IntPtr GetStringPtr(string name)
        {
            var namePtr = Pointer + Marshal.OffsetOf(typeof(RaptureAtkModule.NamePlateInfo), name).ToInt32();
            var stringPtrPtr = namePtr + Marshal.OffsetOf(typeof(Utf8String), nameof(Utf8String.StringPtr)).ToInt32();
            var stringPtr = Marshal.ReadIntPtr(stringPtrPtr);
            return stringPtr;
        }

        private string GetString(IntPtr stringPtr)
        {
            return Marshal.PtrToStringUTF8(stringPtr);
        }
    }
}
