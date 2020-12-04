using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugFixesSSE.Fixes
{
    class FixBuySellStackSpeechGain : Fix
    {
        internal override string Description
        {
            get
            {
                return "If you buy or sell a stack of items there is a bug where you only get enough speechcraft skill as if you bought or sold one item of the stack. This fixes the bug so you correctly get the full amount of speechcraft skill as if you had sold each item individually.";
            }
        }

        internal override void Apply()
        {
            ulong funcVid = 50007;
            var loc1 = NetScriptFramework.Main.GameInfo.GetAddressOf(funcVid, 0x1BE, 0, "E8");
            var loc2 = NetScriptFramework.Main.GameInfo.GetAddressOf(funcVid, 0xD1, 0, "E8");

            NetScriptFramework.Memory.WriteHook(new NetScriptFramework.HookParameters()
            {
                Address = loc1,
                IncludeLength = 5,
                ReplaceLength = 5,
                After = ctx =>
                {
                    int count = NetScriptFramework.Memory.ReadInt32(ctx.SP + 0xD0);
                    float giveXp = ctx.XMM0f;
                    //NetScriptFramework.Main.WriteDebugMessage("Debug: selling items (x" + count + ") and advancing speechcraft by " + giveXp);
                    if(count > 1)
                        ctx.XMM0f = giveXp * count;
                }
            });

            NetScriptFramework.Memory.WriteHook(new NetScriptFramework.HookParameters()
            {
                Address = loc2,
                IncludeLength = 5,
                ReplaceLength = 5,
                After = ctx =>
                {
                    int count = NetScriptFramework.Memory.ReadInt32(ctx.SP + 0xD0);
                    float giveXp = ctx.XMM0f;
                    //NetScriptFramework.Main.WriteDebugMessage("Debug: buying items (x" + count + ") and advancing speechcraft by " + giveXp);
                    if (count > 1)
                        ctx.XMM0f = giveXp * count;
                }
            });
        }
    }
}
