using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamePlayTweaks.Mods
{
    class PlayerCreatedPotionCostChange : Mod
    {
        public PlayerCreatedPotionCostChange()
        {
            this.FloatParameters["CostMultiplier"] = 1.0;

            // Not necessary because it uses a different function to calculate at the time of crafting.
            //this.BoolParameters["ReverseBoostAlchemyXp"] = true;
        }

        internal override string Description
        {
            get
            {
                return "Change the cost multiplier (buy/sell value) of player created potions. For example 0.5 means all player created potions are sold for 50% less gold. This does not affect the experience gained from crafting potions.";
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
                    if (e.Form != null && e.Form is NetScriptFramework.SkyrimSE.AlchemyItem && e.Form.FormId >= 0xFF000000)
                    {
                        int amt = e.Value;
                        if (amt > 0)
                        {
                            amt = (int)(amt * _mult);
                            if (amt <= 0)
                                amt = 1;
                        }
                        e.Value = amt;
                    }
                });

                /*if(this.BoolParameters["ReverseBoostAlchemyXp"])
                {
                    NetScriptFramework.SkyrimSE.Events.OnGainSkillXP.Register(e =>
                    {
                        if (e.IsFromTrainingOrBook || e.Skill != NetScriptFramework.SkyrimSE.ActorValueIndices.Alchemy)
                            return;

                        if(_mult > 0.0)
                        {
                            double amt = 1.0 / _mult;
                            float old = e.Amount;
                            float cur = (float)(old * amt);
                            e.Amount = cur;
                        }
                    });
                }*/
            }
        }
    }
}
