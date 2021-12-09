using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace PlayerTags
{
    public static class UIColorHelper
    {
        private class UIColorComparer : IEqualityComparer<UIColor>
        {
            public bool Equals(UIColor? left, UIColor? right)
            {
                if (left != null && right != null)
                {
                    return left.UIForeground == right.UIForeground;
                }

                return false;
            }

            public int GetHashCode(UIColor obj)
            {
                return obj.UIForeground.GetHashCode();
            }
        }

        private static UIColor[] s_UIColors = null!;

        public static IEnumerable<UIColor> UIColors
        {
            get
            {
                if (s_UIColors == null)
                {
                    s_UIColors = CreateUIColors();
                }

                return s_UIColors;
            }
        }

        public static Vector4 ToColor(UIColor uiColor)
        {
            var uiColorBytes = BitConverter.GetBytes(uiColor.UIForeground);
            return
                new Vector4((float)uiColorBytes[3] / 255,
                (float)uiColorBytes[2] / 255,
                (float)uiColorBytes[1] / 255,
                (float)uiColorBytes[0] / 255);
        }

        public static Vector4 ToColor(ushort colorId)
        {
            foreach (var uiColor in UIColors)
            {
                if ((ushort)uiColor.RowId == colorId)
                {
                    return ToColor(uiColor);
                }
            }

            return new Vector4();
        }

        private static UIColor[] CreateUIColors()
        {
            var uiColors = PluginServices.DataManager.GetExcelSheet<UIColor>();
            if (uiColors != null)
            {
                var filteredUIColors = new List<UIColor>(uiColors.Distinct(new UIColorComparer()).Where(uiColor => uiColor.UIForeground != 0 && uiColor.UIForeground != 255));

                filteredUIColors.Sort((left, right) =>
                {
                    var leftColor = ToColor(left);
                    var rightColor = ToColor(right);
                    ImGui.ColorConvertRGBtoHSV(leftColor.X, leftColor.Y, leftColor.Z, out float leftHue, out float leftSaturation, out float leftValue);
                    ImGui.ColorConvertRGBtoHSV(rightColor.X, rightColor.Y, rightColor.Z, out float rightHue, out float rightSaturation, out float rightValue);

                    var hueDifference = leftHue.CompareTo(rightHue);
                    if (hueDifference != 0)
                    {
                        return hueDifference;
                    }

                    var valueDifference = leftValue.CompareTo(rightValue);
                    if (valueDifference != 0)
                    {
                        return valueDifference;
                    }

                    var saturationDifference = leftSaturation.CompareTo(rightSaturation);
                    if (saturationDifference != 0)
                    {
                        return saturationDifference;
                    }

                    return 0;
                });

                return filteredUIColors.ToArray();
            }

            return new UIColor[] { };
        }
    }
}
