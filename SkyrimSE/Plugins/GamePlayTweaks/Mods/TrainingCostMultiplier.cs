using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework;

namespace GamePlayTweaks.Mods
{
    class TrainingCostMultiplier : Mod
    {
        public TrainingCostMultiplier()
        {
            this.FloatParameters["CostMultiplier"] = 1;
        }

        internal override string Description
        {
            get
            {
                return "Apply an extra multiplier to all training cost if enabled.";
            }
        }

        private IntPtr TrainingCostBasePtr;
        private double _mult;

        internal override void Apply()
        {
            this.TrainingCostBasePtr = NetScriptFramework.Main.GameInfo.GetAddressOf(505893, 8);
            _mult = this.FloatParameters["CostMultiplier"];

            if (_mult != 1.0)
            {
                Memory.WriteHook(new HookParameters()
                {
                    Address = NetScriptFramework.Main.GameInfo.GetAddressOf(25918, 0x50, 0, "F3 0F 58 05"),
                    IncludeLength = 0,
                    ReplaceLength = 8,
                    Before = ctx =>
                    {
                        float amt = ctx.XMM0f;
                        amt += Memory.ReadFloat(TrainingCostBasePtr);
                        amt *= Math.Max(0.0f, (float)_mult);
                        ctx.XMM0f = amt;
                    },
                });
            }
        }
    }
}
