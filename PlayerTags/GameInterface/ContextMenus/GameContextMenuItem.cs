using Dalamud.Game.Text.SeStringHandling;
using System;

namespace PlayerTags.GameInterface.ContextMenus
{
    public class GameContextMenuItem : ContextMenuItem
    {
        public byte Action { get; }

        public GameContextMenuItem(SeString name, byte action)
            : base(name)
        {
            Action = action;
        }
    }
}