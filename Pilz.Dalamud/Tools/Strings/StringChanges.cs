using Dalamud.Game.Text.SeStringHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pilz.Dalamud.Tools.Strings
{
    public class StringChanges
    {
        private readonly Dictionary<StringPosition, StringChange> changes = new();

        public StringChanges()
        {
            changes.Add(StringPosition.Before, new StringChange());
            changes.Add(StringPosition.After, new StringChange());
            changes.Add(StringPosition.Replace, new StringChange());
        }

        /// <summary>
        /// Gets a change of the position of your choice where you can add your payloads.
        /// </summary>
        /// <param name="position">The position of your choice.</param>
        /// <returns></returns>
        public StringChange GetChange(StringPosition position)
        {
            return changes[position];
        }

        /// <summary>
        /// Checks if there is any string change listed.
        /// </summary>
        /// <returns></returns>
        public bool Any()
        {
            return changes.Sum(n => n.Value.Payloads.Count) != 0;
        }
    }
}
