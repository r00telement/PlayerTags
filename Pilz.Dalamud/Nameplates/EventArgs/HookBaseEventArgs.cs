﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pilz.Dalamud.Nameplates.EventArgs
{
    public abstract class HookBaseEventArgs
    {
        internal event Action CallOriginal;

        public void Original()
        {
            CallOriginal?.Invoke();
        }
    }
}
