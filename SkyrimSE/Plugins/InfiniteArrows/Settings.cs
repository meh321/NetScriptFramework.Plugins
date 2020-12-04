using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework.Tools;

namespace InfiniteArrows
{
    public sealed class Settings
    {
        [ConfigValue("InfiniteArrowsForPlayer", "Infinite arrows for player", "Allow player to have infinite arrows?")]
        public bool InfiniteArrowsForPlayer
        {
            get;
            set;
        } = true;

        [ConfigValue("InfiniteArrowsForNPC", "Infinite arrows for NPCs", "Allow NPCs to have infinite arrows? Usually NPCs will only use ammo if the weapon they are using specifically says NPCs must use ammo.")]
        public bool InfiniteArrowsForNPC
        {
            get;
            set;
        } = false;

        [ConfigValue("InfiniteArrowsForTeamMate", "Infinite arrows for team mate", "Allow team mates (followers) to have infinite arrows?")]
        public bool InfiniteArrowsForTeamMate
        {
            get;
            set;
        } = true;

        [ConfigValue("InfiniteArrowsOnlyIfHasThisAmountOrLess", "Infinite arrows based on count", "Only have infinite arrows if we are carrying this many or less arrows. This can be useful if you want to get rid of excess arrows. For example if you set this to 1 then game will use arrows normally until you have only 1 arrow left and then it will be infinite.")]
        public int InfiniteArrowsOnlyIfHasThisAmountOrLess
        {
            get;
            set;
        } = 999999;

        [ConfigValue("ForceAllNPCsToUseArrows", "Force all NPCs to use arrows", "Normally an NPC will not have their arrows removed if they fire a bow unless the bow is specifically marked as forcing the NPC to use ammo. This setting will bypass that and always force all NPCs to use arrows regardless of the option on the weapon form.")]
        public bool ForceAllNPCsToUseArrows
        {
            get;
            set;
        } = false;

        [ConfigValue("DontShowAmmoCounterInHUDIfInfiniteForPlayer", "Don't show ammo in HUD", "This setting will make it so that the ammo will not show up in HUD at all if it's always infinite for you. It should not be necessary to show Iron Arrow (24) if you don't care about how many arrows you have since you know it will be infinite anyway.")]
        public bool DontShowAmmoCounterInHUDIfInfiniteForPlayer
        {
            get;
            set;
        } = true;

        [ConfigValue("OnlyIfWeaponHasKeyword", "Required keyword on weapon", "If this setting is not empty then weapon will have infinite ammo only if it has a specific keyword on it. This is for weapon itself and not the ammo! You can use this setting to only make a specific weapon have infinite ammo.")]
        public string OnlyIfWeaponHasKeyword
        {
            get;
            set;
        } = "";

        [ConfigValue("RequiredMagicEffectWithKeyword", "Required magic effect keyword", "If this setting is not empty then actor who fires the arrow will only have infinite ammo if they have a magic effect with this keyword on them. You can use this setting to make a temporary buff that grants infinite ammo.")]
        public string RequiredMagicEffectWithKeyword
        {
            get;
            set;
        } = "";

        internal void Load()
        {
            ConfigFile.LoadFrom(this, "InfiniteArrows", true);
        }
    }
}
