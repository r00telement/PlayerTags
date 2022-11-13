using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Pilz.Dalamud.Nameplates.Model
{
    public class SafeNameplateObject
    {
        public IntPtr Pointer { get; }
        public AddonNamePlate.NamePlateObject Data { get; }

        private int _Index;
        private SafeNameplateInfo _NamePlateInfo;

        public SafeNameplateObject(IntPtr pointer, int index = -1)
        {
            Pointer = pointer;
            Data = Marshal.PtrToStructure<AddonNamePlate.NamePlateObject>(pointer);
            _Index = index;
        }

        public int Index
        {
            get
            {
                int result = _Index;

                if (_Index == -1)
                {
                    var addon = XivApi.GetSafeAddonNamePlate();
                    var npObject0 = addon.GetNamePlateObject(0);

                    if (npObject0 == null)
                        result = -1; // NamePlateObject0 was null
                    else
                    {
                        var npObjectBase = npObject0.Pointer;
                        var npObjectSize = Marshal.SizeOf(typeof(AddonNamePlate.NamePlateObject));
                        var index = (Pointer.ToInt64() - npObjectBase.ToInt64()) / npObjectSize;

                        if (index < 0 || index >= 50)
                            result = -2; // NamePlateObject index was out of bounds
                        else
                            result = _Index = (int)index;
                    }
                }

                return result;
            }
        }

        public SafeNameplateInfo NamePlateInfo
        {
            get
            {
                SafeNameplateInfo result = null;

                if (_NamePlateInfo != null)
                {
                    var rapturePtr = XivApi.RaptureAtkModulePtr;

                    if (rapturePtr != IntPtr.Zero)
                    {
                        var npInfoArrayPtr = rapturePtr + Marshal.OffsetOf(typeof(RaptureAtkModule), nameof(RaptureAtkModule.NamePlateInfoArray)).ToInt32();
                        var npInfoPtr = npInfoArrayPtr + Marshal.SizeOf(typeof(RaptureAtkModule.NamePlateInfo)) * Index;
                        result = _NamePlateInfo = new SafeNameplateInfo(npInfoPtr);
                    }
                }

                return result;
            }
        }

        #region Getters

        public unsafe IntPtr IconImageNodeAddress => Marshal.ReadIntPtr(Pointer + Marshal.OffsetOf(typeof(AddonNamePlate.NamePlateObject), nameof(AddonNamePlate.NamePlateObject.IconImageNode)).ToInt32());
        public unsafe IntPtr NameNodeAddress => Marshal.ReadIntPtr(Pointer + Marshal.OffsetOf(typeof(AddonNamePlate.NamePlateObject), nameof(AddonNamePlate.NamePlateObject.NameText)).ToInt32());

        public AtkImageNode IconImageNode => Marshal.PtrToStructure<AtkImageNode>(IconImageNodeAddress);
        public AtkTextNode NameTextNode => Marshal.PtrToStructure<AtkTextNode>(NameNodeAddress);

        #endregion

        public unsafe bool IsVisible => Data.IsVisible;
        public unsafe bool IsLocalPlayer => Data.IsLocalPlayer;
        public bool IsPlayer => Data.NameplateKind == 0;

        //public void SetIconScale(float scale, bool force = false)
        //{
        //    if (force || IconImageNode.AtkResNode.ScaleX != scale || IconImageNode.AtkResNode.ScaleY != scale)
        //    {
        //        Instance.SetNodeScale(IconImageNodeAddress, scale, scale);
        //    }
        //}

        //public void SetNameScale(float scale, bool force = false)
        //{
        //    if (force || NameTextNode.AtkResNode.ScaleX != scale || NameTextNode.AtkResNode.ScaleY != scale)
        //    {
        //        Instance.SetNodeScale(NameNodeAddress, scale, scale);
        //    }
        //}

        //public unsafe void SetName(IntPtr ptr)
        //{
        //    NameTextNode.SetText("aaa");
        //}

        //public void SetIcon(int icon)
        //{
        //    IconImageNode.LoadIconTexture(icon, 1);
        //}

        public void SetIconPosition(short x, short y)
        {
            var iconXAdjustPtr = Pointer + Marshal.OffsetOf(typeof(AddonNamePlate.NamePlateObject), nameof(AddonNamePlate.NamePlateObject.IconXAdjust)).ToInt32();
            var iconYAdjustPtr = Pointer + Marshal.OffsetOf(typeof(AddonNamePlate.NamePlateObject), nameof(AddonNamePlate.NamePlateObject.IconYAdjust)).ToInt32();
            Marshal.WriteInt16(iconXAdjustPtr, x);
            Marshal.WriteInt16(iconYAdjustPtr, y);
        }
    }
}
