using Dalamud.Game.Text.SeStringHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pilz.Dalamud.Tools.Strings
{
    public class StringChange
    {
        /// <summary>
        /// The payloads to use for inserting/replacing.
        /// </summary>
        public List<Payload> Payloads { get; init; } = new();

        /// <summary>
        /// Defines if only one anchor payload should be used, if using anchor payloads.
        /// With this true the single anchor payload will be used in StringUpdateFactory instead of the anchor payload list.
        /// Not needed to be true for the most cases.
        /// </summary>
        public bool ForceUsingSingleAnchorPayload { get; set; } = false;
    }
}
