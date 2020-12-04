using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework.SkyrimSE;

namespace GamePlayTweaks.Mods
{
    class BlockPotionUse : Mod
    {
        public BlockPotionUse() : base()
        {
            settings.blockPotionInCombat = this.CreateSettingBool("BlockPotionInCombat", true, "Don't allow using potions in combat.");
            settings.blockPotionInCombatMsg = this.CreateSettingString("BlockPotionInCombatMsg", "You don't have time to drink potions right now!", "The message to show when trying to use potion in combat.");

            settings.blockPoisonInCombat = this.CreateSettingBool("BlockPoisonInCombat", true, "Don't allow using poisons in combat.");
            settings.blockPoisonInCombatMsg = this.CreateSettingString("BlockPoisonInCombatMsg", "You don't have time to use poisons right now!", "The message to show when trying to use poison in combat.");

            settings.blockFoodInCombat = this.CreateSettingBool("BlockFoodInCombat", true, "Don't allow using food or drink in combat.");
            settings.blockFoodInCombatMsg = this.CreateSettingString("BlockFoodInCombatMsg", "You don't have time to do that right now!", "The message to show when trying to use food or drink in combat.");

            settings.blockPotionWhenMagicEffectKeyword = this.CreateSettingString("BlockPotionWhenMagicEffectKeyword", "", "If player has any magic effects with specified keywords then can't use potions. Separate keywords with space.");
            settings.blockPotionWhenMagicEffectKeywordMsg = this.CreateSettingString("BlockPotionWhenMagicEffectKeywordMsg", "You can't do that right now!", "The message to show when it failed that way.");

            settings.blockPoisonWhenMagicEffectKeyword = this.CreateSettingString("BlockPoisonWhenMagicEffectKeyword", "", "If player has any magic effects with specified keywords then can't use poisons. Separate keywords with space.");
            settings.blockPoisonWhenMagicEffectKeywordMsg = this.CreateSettingString("BlockPoisonWhenMagicEffectKeywordMsg", "You can't do that right now!", "The message to show when it failed that way.");

            settings.blockFoodWhenMagicEffectKeyword = this.CreateSettingString("BlockFoodWhenMagicEffectKeyword", "", "If player has any magic effects with specified keywords then can't use food or drink. Separate keywords with space.");
            settings.blockFoodWhenMagicEffectKeywordMsg = this.CreateSettingString("BlockFoodWhenMagicEffectKeywordMsg", "You can't do that right now!", "The message to show when it failed that way.");

            settings.alwaysApplySpellOnPotionUse = this.CreateSettingForm<MagicItem>("AlwaysApplySpellOnPotionUse", "", "The spell to apply to player when they drink a potion.");
            settings.alwaysApplySpellOnPoisonUse = this.CreateSettingForm<MagicItem>("AlwaysApplySpellOnPoisonUse", "", "The spell to apply to player when they use a potion. This is applied to player and not item!");
            settings.alwaysApplySpellOnFoodUse = this.CreateSettingForm<MagicItem>("AlwaysApplySpellOnFoodUse", "", "The spell to apply to player when they eat a food or drink a drink.");

            settings.chanceNotRemovePotion = this.CreateSettingFloat("ChanceToNotRemovePotion", 0, "The chance from 0 to 100 of not removing the potion from inventory when you drink one.");
            settings.chanceNotRemovePoison = this.CreateSettingFloat("ChanceToNotRemovePoison", 0, "The chance from 0 to 100 of not removing the poison from inventory when you use one.");
            settings.chanceNotRemoveFood = this.CreateSettingFloat("ChanceToNotRemoveFood", 0, "The chance from 0 to 100 of not removing the food from inventory when you consume one.");
        }

        private static class settings
        {
            internal static SettingValue<bool> blockPotionInCombat;
            internal static SettingValue<string> blockPotionInCombatMsg;
            internal static SettingValue<bool> blockPoisonInCombat;
            internal static SettingValue<string> blockPoisonInCombatMsg;
            internal static SettingValue<bool> blockFoodInCombat;
            internal static SettingValue<string> blockFoodInCombatMsg;

            internal static SettingValue<string> blockPotionWhenMagicEffectKeyword;
            internal static SettingValue<string> blockPotionWhenMagicEffectKeywordMsg;
            internal static SettingValue<string> blockPoisonWhenMagicEffectKeyword;
            internal static SettingValue<string> blockPoisonWhenMagicEffectKeywordMsg;
            internal static SettingValue<string> blockFoodWhenMagicEffectKeyword;
            internal static SettingValue<string> blockFoodWhenMagicEffectKeywordMsg;

            internal static SettingValue<MagicItem> alwaysApplySpellOnPotionUse;
            internal static SettingValue<MagicItem> alwaysApplySpellOnPoisonUse;
            internal static SettingValue<MagicItem> alwaysApplySpellOnFoodUse;

            internal static SettingValue<double> chanceNotRemovePotion;
            internal static SettingValue<double> chanceNotRemovePoison;
            internal static SettingValue<double> chanceNotRemoveFood;
        }

        internal override string Description
        {
            get
            {
                return "Block eating or drinking in some cases.";
            }
        }

        private void OnUsedItem(Actor actor, TESForm obj, ref int removeCount)
        {
            if (actor == null || obj == null || !actor.Equals(PlayerCharacter.Instance))
                return;

            switch (obj.FormType)
            {
                case FormTypes.Potion:
                    {
                        var potion = obj as AlchemyItem;
                        if (potion != null)
                        {
                            if ((potion.Data.Flags & AlchemyItem.AlchemyItemFlags.FoodItem) != AlchemyItem.AlchemyItemFlags.None)
                            {
                                if (settings.alwaysApplySpellOnFoodUse.Value != null)
                                    actor.CastSpell(settings.alwaysApplySpellOnFoodUse.Value, actor, null);

                                if(removeCount > 0 && settings.chanceNotRemoveFood.Value > 0.0)
                                {
                                    if (NetScriptFramework.Tools.Randomizer.Roll(settings.chanceNotRemoveFood.Value * 0.01))
                                        removeCount = 0;
                                }
                            }
                            else if ((potion.Data.Flags & AlchemyItem.AlchemyItemFlags.Poison) != AlchemyItem.AlchemyItemFlags.None)
                            {
                                if (settings.alwaysApplySpellOnPoisonUse.Value != null)
                                    actor.CastSpell(settings.alwaysApplySpellOnPoisonUse.Value, actor, null);

                                if (removeCount > 0 && settings.chanceNotRemovePoison.Value > 0.0)
                                {
                                    if (NetScriptFramework.Tools.Randomizer.Roll(settings.chanceNotRemovePoison.Value * 0.01))
                                        removeCount = 0;
                                }
                            }
                            else
                            {
                                if (settings.alwaysApplySpellOnPotionUse.Value != null)
                                    actor.CastSpell(settings.alwaysApplySpellOnPotionUse.Value, actor, null);

                                if (removeCount > 0 && settings.chanceNotRemovePotion.Value > 0.0)
                                {
                                    if (NetScriptFramework.Tools.Randomizer.Roll(settings.chanceNotRemovePotion.Value * 0.01))
                                        removeCount = 0;
                                }
                            }
                        }
                    }
                    break;

                case FormTypes.Ingredient:
                    {

                    }
                    break;
            }
        }

        private string[] mekw_potion;
        private string[] mekw_poison;
        private string[] mekw_food;

        private string CanUseItem(Actor actor, TESForm obj)
        {
            if (actor == null || obj == null || !actor.Equals(PlayerCharacter.Instance))
                return null;

            switch(obj.FormType)
            {
                case FormTypes.Potion:
                    {
                        var potion = obj as AlchemyItem;
                        if(potion != null)
                        {
                            MagicItem itm = null;
                            if ((potion.Data.Flags & AlchemyItem.AlchemyItemFlags.FoodItem) != AlchemyItem.AlchemyItemFlags.None)
                            {
                                if (settings.blockFoodInCombat.Value && actor.IsInCombat)
                                    return settings.blockFoodInCombatMsg.Value ?? "";

                                if (mekw_food != null && mekw_food.Any(q => actor.HasMagicEffectWithKeywordText(q, ref itm)))
                                    return settings.blockFoodWhenMagicEffectKeywordMsg.Value ?? "";
                            }
                            else if((potion.Data.Flags & AlchemyItem.AlchemyItemFlags.Poison) != AlchemyItem.AlchemyItemFlags.None)
                            {
                                if(settings.blockPoisonInCombat.Value && actor.IsInCombat)
                                    return settings.blockPoisonInCombatMsg.Value ?? "";

                                if (mekw_poison != null && mekw_poison.Any(q => actor.HasMagicEffectWithKeywordText(q, ref itm)))
                                    return settings.blockPoisonWhenMagicEffectKeywordMsg.Value ?? "";
                            }
                            else
                            {
                                if (settings.blockPotionInCombat.Value && actor.IsInCombat)
                                    return settings.blockPotionInCombatMsg.Value ?? "";

                                if (mekw_potion != null && mekw_potion.Any(q => actor.HasMagicEffectWithKeywordText(q, ref itm)))
                                    return settings.blockPotionWhenMagicEffectKeywordMsg.Value ?? "";
                            }
                        }
                    }
                    break;

                case FormTypes.Ingredient:
                    {

                    }
                    break;
            }

            return null;
        }

        internal override void Apply()
        {
            Tools.BlockEquipHook.Install(CanUseItem);

            mekw_food = (settings.blockFoodWhenMagicEffectKeyword.Value ?? "").Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (mekw_food.Length == 0)
                mekw_food = null;

            mekw_potion = (settings.blockPotionWhenMagicEffectKeyword.Value ?? "").Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (mekw_potion.Length == 0)
                mekw_potion = null;

            mekw_poison = (settings.blockPoisonWhenMagicEffectKeyword.Value ?? "").Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (mekw_poison.Length == 0)
                mekw_poison = null;

            var addr = NetScriptFramework.Main.GameInfo.GetAddressOf(37797, 0xE1 - 0x70, 0, "FF 90 B0 02 00 00");
            NetScriptFramework.Memory.WriteHook(new NetScriptFramework.HookParameters()
            {
                Address = addr,
                IncludeLength = 6,
                ReplaceLength = 6,
                Before = ctx =>
                {
                    try
                    {
                        var actor = NetScriptFramework.MemoryObject.FromAddress<Actor>(ctx.CX);
                        var obj = NetScriptFramework.MemoryObject.FromAddress<TESForm>(ctx.R8);
                        int rcount = NetScriptFramework._IntPtrExtensions.ToInt32Safe(ctx.R9);
                        int rcount2 = rcount;
                        this.OnUsedItem(actor, obj, ref rcount2);
                        if (rcount != rcount2)
                            ctx.R9 = new IntPtr(rcount2);
                    }
                    catch
                    {

                    }
                },
            });

            addr = NetScriptFramework.Main.GameInfo.GetAddressOf(39407, 0x15F - 0x20, 0, "FF 90 B0 02 00 00");
            NetScriptFramework.Memory.WriteHook(new NetScriptFramework.HookParameters()
            {
                Address = addr,
                IncludeLength = 6,
                ReplaceLength = 6,
                Before = ctx =>
                {
                    try
                    {
                        var actor = NetScriptFramework.MemoryObject.FromAddress<Actor>(ctx.CX);
                        var obj = NetScriptFramework.MemoryObject.FromAddress<TESForm>(ctx.R8);
                        int rcount = NetScriptFramework._IntPtrExtensions.ToInt32Safe(ctx.R9);
                        int rcount2 = rcount;
                        this.OnUsedItem(actor, obj, ref rcount2);
                        if (rcount != rcount2)
                            ctx.R9 = new IntPtr(rcount2);
                    }
                    catch
                    {

                    }
                },
            });
        }
    }
}
