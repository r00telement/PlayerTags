using Dalamud.Game.Text.SeStringHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pilz.Dalamud
{
    public static class Extensions
    {
        /// <summary>
        /// Removes a Payload from a given SeString.
        /// Using <code>SeString.Payloads.Remove()</code> does not use the reference to compare for some reason. Tis is a workaround.
        /// </summary>
        /// <param name="seString"></param>
        /// <param name="payload"></param>
        public static void Remove(this SeString seString, Payload payload)
        {
            Remove(seString.Payloads, payload);
        }

        /// <summary>
        /// Removes a Payload from a given list.
        /// Using <code>List.Remove()</code> does not use the reference to compare for some reason. Tis is a workaround.
        /// </summary>
        /// <param name="seString"></param>
        /// <param name="payload"></param>
        public static void Remove(this List<Payload> payloads, Payload payload)
        {
            for (int i = 0; i < payloads.Count; i++)
            {
                if (ReferenceEquals(payloads[i], payload))
                {
                    payloads.RemoveAt(i);
                    break;
                }
            }
        }
    }
}
