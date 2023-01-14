using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pilz.Dalamud.Nameplates.EventArgs
{
    public abstract class HookWithResultBaseEventArgs<TResult>
    {
        internal event Func<TResult> CallOriginal;

        public TResult Result { get; set; }

        // Call Original based on the given properties
        public TResult Original()
        {
            return CallOriginal.Invoke();
        }
    }
}
