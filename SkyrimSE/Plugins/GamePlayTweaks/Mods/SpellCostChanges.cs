using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework.SkyrimSE;

namespace GamePlayTweaks.Mods
{
    class SpellCostChanges : Mod
    {
        public SpellCostChanges() : base()
        {
            settings.OnlyPlayer = this.CreateSettingBool("OnlyPlayer", true, "The cost changes only apply to player. If false then also applies to NPCs.");
            settings.OnlySpells = this.CreateSettingBool("OnlySpells", true, "Only modify cost of spells. If this is false it may also modify things like shout, enchantments, powers, and so on. Recommended to set this to true.");
            settings.CombatMultiplier = this.CreateSettingFloat("CombatMultiplier", 1.0, "Cost multiplier when caster is in combat.");
            settings.RunMultiplier = this.CreateSettingFloat("RunMultiplier", 1.0, "Cost multiplier when caster is moving quickly.");
            settings.WalkMultiplier = this.CreateSettingFloat("WalkMultiplier", 1.0, "Cost multiplier when caster is moving slowly.");
            settings.IdleMultiplier = this.CreateSettingFloat("IdleMultiplier", 1.0, "Cost multiplier when caster is not moving.");
            settings.HeavyArmorMultiplier = this.CreateSettingFloat("HeavyArmorMultiplier", 2.0, "Cost multiplier when caster is wearing heavy armor in body (main armor) slot.");
            settings.LightArmorMultiplier = this.CreateSettingFloat("LightArmorMultiplier", 1.5, "Cost multiplier when caster is wearing light armor in body (main armor) slot.");
            settings.NoneArmorMultiplier = this.CreateSettingFloat("NoneArmorMultiplier", 1.0, "Cost multiplier when caster is not wearing heavy or light armor in body (main armor) slot. This could be clothing or naked.");
            settings.StaminaFullMultiplier = this.CreateSettingFloat("StaminaFullMultiplier", 1.0, "Cost multiplier when stamina is full. If stamina is somewhere in between the cost is scaled between these two settings.");
            settings.StaminaEmptyMultiplier = this.CreateSettingFloat("StaminaEmptyMultiplier", 1.0, "Cost multiplier when stamina is empty. If stamina is somewhere in between the cost is scaled between these two settings.");
            settings.GlobalMultiplier = this.CreateSettingForm<TESGlobal>("GlobalMultiplier", "", "If this points to a global variable then magicka cost is multiplied by this variable's value.");
        }

        internal override string Description
        {
            get
            {
                return "Adds various modifiers to spell costs.";
            }
        }

        private static class settings
        {
            internal static SettingValue<bool> OnlyPlayer;
            internal static SettingValue<bool> OnlySpells;
            internal static SettingValue<double> CombatMultiplier;
            internal static SettingValue<double> RunMultiplier;
            internal static SettingValue<double> WalkMultiplier;
            internal static SettingValue<double> IdleMultiplier;
            internal static SettingValue<double> HeavyArmorMultiplier;
            internal static SettingValue<double> LightArmorMultiplier;
            internal static SettingValue<double> NoneArmorMultiplier;
            internal static SettingValue<double> StaminaFullMultiplier;
            internal static SettingValue<double> StaminaEmptyMultiplier;
            internal static SettingValue<TESGlobal> GlobalMultiplier;
        }

        private static void _mod(Actor caster, MagicItem item, ref double cost)
        {
            if (cost <= 0.0)
                return;

            if (settings.OnlyPlayer.Value && !caster.IsPlayer)
                return;

            if (settings.OnlySpells.Value)
            {
                var spell = item as SpellItem;
                if (spell == null)
                    return;

                switch(spell.SpellData.SpellType)
                {
                    case SpellTypes.LeveledSpell:
                    case SpellTypes.Spell:
                        break;

                    default:
                        return;
                }
            }
            
            if (settings.CombatMultiplier.Value != 1.0 && caster.IsInCombat)
                cost *= settings.CombatMultiplier.Value;

            if(settings.RunMultiplier.Value != 1.0 || settings.WalkMultiplier.Value != 1.0 || settings.IdleMultiplier.Value != 1.0)
            {
                uint flags = NetScriptFramework.Memory.ReadUInt32(caster.Cast<Actor>() + 0xC0) & 0x3FFF;
                bool mounted = caster.IsOnMount || caster.IsOnFlyingMount;
                var pcont = PlayerControls.Instance.Cast<PlayerControls>();

                if (!mounted && (
                    (!caster.IsSneaking && (flags & 0x100) != 0) // sprint
                    || ((flags & 0x180) == 0x80 && NetScriptFramework.Memory.ReadUInt8(pcont + 73) != 0) // run
                    ))
                    cost *= settings.RunMultiplier.Value;
                else if (!mounted && (flags & 0x1C0) == 0x40) // walk
                    cost *= settings.WalkMultiplier.Value;
                else
                    cost *= settings.IdleMultiplier.Value;
            }

            if(settings.HeavyArmorMultiplier.Value != 1.0 || settings.LightArmorMultiplier.Value != 1.0 || settings.NoneArmorMultiplier.Value != 1.0)
            {
                var armor = caster.GetEquippedArmorInSlot(EquipSlots.Body);
                BGSBipedObjectForm.ArmorTypes atype = BGSBipedObjectForm.ArmorTypes.Clothing;
                if (armor != null)
                    atype = armor.ModelData.ArmorType;

                if (atype == BGSBipedObjectForm.ArmorTypes.HeavyArmor)
                    cost *= settings.HeavyArmorMultiplier.Value;
                else if (atype == BGSBipedObjectForm.ArmorTypes.LightArmor)
                    cost *= settings.LightArmorMultiplier.Value;
                else
                    cost *= settings.NoneArmorMultiplier.Value;
            }

            if(settings.StaminaFullMultiplier.Value != 1.0 || settings.StaminaEmptyMultiplier.Value != 1.0)
            {
                double ratio = caster.GetActorValue(ActorValueIndices.Stamina);
                double max = caster.GetActorValueMax(ActorValueIndices.Stamina);
                if (max <= 0.0 || ratio >= max)
                    ratio = 1.0;
                else if (ratio < 0.0)
                    ratio = 0.0;
                else
                    ratio /= max;

                cost *= (settings.StaminaFullMultiplier.Value - settings.StaminaEmptyMultiplier.Value) * ratio + settings.StaminaEmptyMultiplier.Value;
            }

            if (settings.GlobalMultiplier.Value != null)
                cost *= settings.GlobalMultiplier.Value.FloatValue;
        }

        internal override void Apply()
        {
            Tools.SpellCostHook.apply(p =>
            {
                double cost = p.Cost;
                double orig = cost;
                _mod(p.Caster, p.Spell, ref cost);
                if(orig != cost)
                    p.Cost = (float)cost;
            });
        }
    }
}
