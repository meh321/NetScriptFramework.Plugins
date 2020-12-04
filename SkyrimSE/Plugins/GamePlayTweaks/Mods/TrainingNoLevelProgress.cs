using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamePlayTweaks.Mods
{
    class TrainingNoLevelProgress : Mod
    {
        public TrainingNoLevelProgress()
        {
            this.FloatParameters["LevelXPMultiplier"] = 0;
        }

        internal override string Description
        {
            get
            {
                return "If enabled and you train at an NPC or read skill book, then you will not gain any progress towards character level up. Or you can set a custom rate for this, this is a multiplier so 1 is normal and 0.5 is half.";
            }
        }

        private double _mult = 1;

        internal override void Apply()
        {
            _mult = this.FloatParameters["LevelXPMultiplier"];

            if (_mult != 1.0)
            {
                NetScriptFramework.SkyrimSE.Events.OnGainLevelXP.Register(e =>
                {
                    //NetScriptFramework.Main.WriteDebugMessage("OnGainLevelXP(" + e.Skill.ToString() + ", " + e.Amount + ", " + e.IsFromTrainingOrBook.ToString() + ")");

                    if (e.IsFromTrainingOrBook)
                        e.Amount = (float)Math.Max(0.0, (e.Amount * _mult));
                });
            }
        }
    }
}
