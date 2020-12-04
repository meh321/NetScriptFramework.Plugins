using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework;

namespace GamePlayTweaks.Mods
{
    class TrainingGoldDisappearsFromNPC : Mod
    {
        internal override string Description
        {
            get
            {
                return "If enabled then whenever you train at an NPC the gold you pay them always disappears instead of going to their inventory.";
            }
        }

        internal override void Apply()
        {
            Memory.WriteHook(new HookParameters()
            {
                Address = NetScriptFramework.Main.GameInfo.GetAddressOf(51793, 0xB9, 0, "E8"),
                IncludeLength = 5,
                ReplaceLength = 5,
                Before = ctx =>
                {
                    ctx.DX = IntPtr.Zero;
                }
            });
        }
    }
}
