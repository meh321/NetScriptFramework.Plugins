using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework.Tools;

namespace WeaponCharge
{
    public sealed class Settings
    {
        [ConfigValue("PettyChargePercentPerDay", "Charge per-cent per day (petty)", "How much to charge a weapon per day with this level of soulgem. This is per-cent of weapon's full charge, regardless of how large the charge on the weapon is.")]
        public float PettyChargePercentPerDay
        {
            get;
            set;
        } = 20.0f;

        [ConfigValue("LesserChargePercentPerDay", "Charge per-cent per day (lesser)", "How much to charge a weapon per day with this level of soulgem. This is per-cent of weapon's full charge, regardless of how large the charge on the weapon is.")]
        public float LesserChargePercentPerDay
        {
            get;
            set;
        } = 30.0f;

        [ConfigValue("CommonChargePercentPerDay", "Charge per-cent per day (common)", "How much to charge a weapon per day with this level of soulgem. This is per-cent of weapon's full charge, regardless of how large the charge on the weapon is.")]
        public float CommonChargePercentPerDay
        {
            get;
            set;
        } = 50.0f;

        [ConfigValue("GreaterChargePercentPerDay", "Charge per-cent per day (greater)", "How much to charge a weapon per day with this level of soulgem. This is per-cent of weapon's full charge, regardless of how large the charge on the weapon is.")]
        public float GreaterChargePercentPerDay
        {
            get;
            set;
        } = 100.0f;

        [ConfigValue("GrandChargePercentPerDay", "Charge per-cent per day (grand)", "How much to charge a weapon per day with this level of soulgem. This is per-cent of weapon's full charge, regardless of how large the charge on the weapon is.")]
        public float GrandChargePercentPerDay
        {
            get;
            set;
        } = 150.0f;

        [ConfigValue("MultipleWeaponsChargeSlower", "Multiple weapons charge slower", "If you have more than one weapon that needs recharging it will distribute the charge to each weapon equally and lower the speed at which you recharge.")]
        public bool MultipleWeaponsChargeSlower
        {
            get;
            set;
        } = true;

        [ConfigValue("SoulGemStacking", "Soul gem stacking", "Instead of finding the best soul-gem, this setting will add up all the soul-gems together. Meaning the more soulgems you have the faster you recharge stuff. This may be a bit cheaty if you have many soulgems, also inconvenient to have to carry them around.")]
        public bool SoulGemStacking
        {
            get;
            set;
        } = false;

        [ConfigValue("OnlyRechargeEquippedWeapons", "Only recharge equipped weapon", "Only equipped weapons are recharged. This can be useful if you pick up some enchanted loot. If the enemy has spent the charges on the weapon but you don't really want to recharge it then it can be helpful to enable this option.")]
        public bool OnlyRechargeEquippedWeapons
        {
            get;
            set;
        } = false;

        [ConfigValue("RechargeIntervalGameHours", "Recharge interval", "How often do we check to recharge weapons in inventory. This is in game hours which means the timescale setting affects it! 0.1 means update every 6 in-game minutes (not real-time minutes!) There shouldn't be much reason to change this because you will still charge the same amount in 24 hours.")]
        public float RechargeIntervalGameHours
        {
            get;
            set;
        } = 0.1f;

        internal void Load()
        {
            ConfigFile.LoadFrom<Settings>(this, "WeaponCharge", true);
        }
    }
}
