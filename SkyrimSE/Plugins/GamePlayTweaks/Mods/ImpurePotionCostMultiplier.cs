using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamePlayTweaks.Mods
{
    class ImpurePotionCostMultiplier : Mod
    {
        public ImpurePotionCostMultiplier()
        {
            this.FloatParameters["CostMultiplier"] = 0.2;
        }

        internal override string Description
        {
            get
            {
                return "Add an extra cost multiplier to impure potions (have both good and bad effects). This does not change experience gain from crafting the potion.";
            }
        }

        private double _mult = 1.0;

        internal override void Apply()
        {
            _mult = this.FloatParameters["CostMultiplier"];
            if(_mult != 1.0)
            {
                NetScriptFramework.SkyrimSE.Events.OnCalculateFormGoldValue.Register(e =>
                {
                    var alch = e.Form as NetScriptFramework.SkyrimSE.AlchemyItem;
                    if (alch == null || !alch.HasKeywordText("MagicAlchHarmful") || !alch.HasKeywordText("MagicAlchBeneficial"))
                        return;

                    int amt = e.Value;
                    if(amt > 0)
                    {
                        amt = (int)(amt * _mult);
                        if (amt <= 0)
                            amt = 1;
                        e.Value = amt;
                    }
                });
            }
        }
    }
}
