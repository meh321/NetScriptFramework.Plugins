using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework.SkyrimSE;

namespace GamePlayTweaks.Tools
{
    internal static class SpellCostHook
    {
        internal sealed class spell_cost_param
        {
            internal spell_cost_param(Actor caster, MagicItem spell, float cost)
            {
                this.Caster = caster;
                this.Spell = spell;
                this.Cost = cost;
                this.OriginalCost = cost;
            }

            internal readonly Actor Caster;
            internal readonly MagicItem Spell;
            internal readonly float OriginalCost;
            internal float Cost;
        }

        private static List<Action<spell_cost_param>> hooks = null;

        internal static void apply(Action<spell_cost_param> func)
        {
            if(hooks != null)
            {
                hooks.Add(func);
                return;
            }

            hooks = new List<Action<spell_cost_param>>();
            hooks.Add(func);

            NetScriptFramework.Memory.WriteHook(new NetScriptFramework.HookParameters()
            {
                Address = NetScriptFramework.Main.GameInfo.GetAddressOf(11213, 0xD00 - 0xC20, 0, "F3 0F 10 44 24 40"),
                IncludeLength = 6,
                ReplaceLength = 6,
                After = ctx =>
                {
                    try
                    {
                        var actor = NetScriptFramework.MemoryObject.FromAddress<Actor>(ctx.BP);
                        var item = NetScriptFramework.MemoryObject.FromAddress<MagicItem>(ctx.DI);

                        if (actor == null || item == null)
                            return;

                        float orig = ctx.XMM0f;

                        var p = new spell_cost_param(actor, item, orig);
                        foreach (var h in hooks)
                            h(p);

                        float cur = p.Cost;
                        if (cur != orig)
                            ctx.XMM0f = cur;
                    }
                    catch
                    {

                    }
                },
            });
        }
    }
}
