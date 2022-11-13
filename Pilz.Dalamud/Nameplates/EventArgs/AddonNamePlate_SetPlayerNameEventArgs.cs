using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pilz.Dalamud.Nameplates.EventArgs
{
    public class AddonNamePlate_SetPlayerNameEventArgs : HookWithResultBaseEventArgs<IntPtr>
    {
        public IntPtr PlayerNameplateObjectPtr { get; set; }
        public IntPtr TitlePtr { get; set; }
        public IntPtr NamePtr { get; set; }
        public IntPtr FreeCompanyPtr { get; set; }
        public bool IsTitleAboveName { get; set; }
        public bool IsTitleVisible { get; set; }
        public int IconID { get; set; }
    }
}
