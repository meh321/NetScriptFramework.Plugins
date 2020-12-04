using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework.Tools;

namespace NoLockPicking
{
    public sealed class Settings
    {
        [ConfigValue("Enabled", "Enabled", "Enable the plugin or disable.")]
        public bool Enabled
        {
            get;
            set;
        } = true;

        [ConfigValue("SuperCheatMode", "Cheater!", "Enable this if you want nothing to require any lock picks ever.")]
        public bool SuperCheatMode
        {
            get;
            set;
        } = false;
        
        [ConfigValue("LockPickCostRelativeVeryEasy", "Lock pick cost (very easy)", "How many lock picks will be taken if you pick a relatively very easy lock. Relatively means the lock is easy for your current skill level. You can set value with fractions, if you set 1.3 then it means take 1 lock pick and 30% chance to take a second lock pick. Set negative to prevent this from being opened at current skill level.")]
        public double CostVeryEasy
        {
            get;
            set;
        } = 0.1;

        [ConfigValue("LockPickCostRelativeEasy", "Lock pick cost (easy)", "How many lock picks will be taken if you pick a relatively easy lock. Relatively means the lock is easy for your current skill level. You can set value with fractions, if you set 1.3 then it means take 1 lock pick and 30% chance to take a second lock pick. Set negative to prevent this from being opened at current skill level.")]
        public double CostEasy
        {
            get;
            set;
        } = 0.3;

        [ConfigValue("LockPickCostRelativeMedium", "Lock pick cost (medium)", "How many lock picks will be taken if you pick a relatively average lock. Relatively means the lock is average for your current skill level. You can set value with fractions, if you set 1.3 then it means take 1 lock pick and 30% chance to take a second lock pick. Set negative to prevent this from being opened at current skill level.")]
        public double CostMedium
        {
            get;
            set;
        } = 0.9;

        [ConfigValue("LockPickCostRelativeHard", "Lock pick cost (hard)", "How many lock picks will be taken if you pick a relatively hard lock. Relatively means the lock is hard for your current skill level. You can set value with fractions, if you set 1.3 then it means take 1 lock pick and 30% chance to take a second lock pick. Set negative to prevent this from being opened at current skill level.")]
        public double CostHard
        {
            get;
            set;
        } = 2.0;

        [ConfigValue("LockPickCostRelativeVeryHard", "Lock pick cost (very hard)", "How many lock picks will be taken if you pick a relatively very hard lock. Relatively means the lock is hard for your current skill level. You can set value with fractions, if you set 1.3 then it means take 1 lock pick and 30% chance to take a second lock pick. Set negative to prevent this from being opened at current skill level.")]
        public double CostVeryHard
        {
            get;
            set;
        } = 5.0;

        [ConfigValue("BonusReducesPickUsage", "Bonus reduces pick usage", "This will cause bonus effects to reduce the amount of picks used. For example +20% easier lock picking enchantment/perk will take (x / 1.2) of actual picks. It will reduce the amount of picks you use on average.")]
        public bool BonusReducesPickUsage
        {
            get;
            set;
        } = true;

        [ConfigValue("RequireAtLeastOneLockPickInInventory", "Require lock pick in inventory", "Requires at least one lock pick in inventory to begin? If cost is 0 then you don't need any actual lock picks. This check here will still require at least one lock pick in your inventory to pick locks anyway.")]
        public bool RequireAtLeastOneLockPickInInventory
        {
            get;
            set;
        } = true;
        
        [ConfigValue("AllowPickKeyDoors", "Allow pick key doors", "Some doors in the game are marked as only being possible to open if you have the key. This setting will allow bypass this and open the door anyway. WARNING: It is highly recommended to not enable this setting as it may break quests in your game if you go somewhere you're not supposed to be yet!")]
        public bool AllowPickKeyDoors
        {
            get;
            set;
        } = false;

        [ConfigValue("OnlyWorksWhenSneaking", "Only works when sneaking", "If enabled then locks are picked only when sneaking, otherwise it will not do anything.")]
        public bool OnlyWorksWhenSneaking
        {
            get;
            set;
        } = false;

        internal void Load()
        {
            ConfigFile.LoadFrom<Settings>(this, "NoLockPicking", true);
        }
    }
}
