using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Pilz.Dalamud.Icons
{
    public class JobIconSet
    {
        private readonly int[] icons;

        public float IconScale { get; init; }

        public JobIconSet(int[] icons, float iconScale)
        {
            this.icons = icons;
            IconScale = iconScale;
        }

        public int GetIcon(uint jobID)
        {
            return icons[jobID - 1];
        }
    }
}
