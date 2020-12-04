using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework.Tools;

namespace InfinitePoison
{
    public sealed class Settings
    {
        [ConfigValue("InfinitePoisonForPlayer", "Infinite for player", "Is the poison on weapons infinite for player?")]
        public bool InfinitePoisonForPlayer
        {
            get;
            set;
        } = true;

        [ConfigValue("InfinitePoisonForTeamMates", "Infinite for team-mates", "Is the poison on weapon infinite for player's team-mates?")]
        public bool InfinitePoisonForTeamMates
        {
            get;
            set;
        } = true;

        [ConfigValue("InfinitePoisonForOthers", "Infinite for others", "Is the poison on weapon infinite for other NPCs or enemies?")]
        public bool InfinitePoisonForOthers
        {
            get;
            set;
        } = false;

        [ConfigValue("ApplyReplacesPoison", "Apply replaces poison", "When you apply a poison to weapon it will always replace previous poison. If this setting is enabled and you try to apply a poison it will remove the previous poison first. If the confirmation dialogue comes up and you click 'no' it will remove the previous poison. If you click 'yes' it will replace the previous poison. This is necessary because if you have infinite poison it will never remove it on its own and you are stuck with it otherwise.")]
        public bool ApplyReplacesPoison
        {
            get;
            set;
        } = true;

        [ConfigValue("RequiredWeaponKeyword", "Required weapon keyword", "Poison is only infinite if the weapon has this keyword. For example if you only wanted daggers to have infinite poison you could type here \"WeapTypeDagger\" and any weapons with this keyword will have infinite poison but other weapons will not.")]
        public string RequiredWeaponKeyword
        {
            get;
            set;
        } = "";

        [ConfigValue("RequiredActorMagicEffectKeyword", "Required actor magic effect keyword", "Enter this if you only want to have infinite poison while attacker has magic effect with this keyword. It can be useful to create a buff that gives you infinite poison while it's on.")]
        public string RequiredActorMagicEffectKeyword
        {
            get;
            set;
        } = "";

        [ConfigValue("ChanceToBeInfinitePoison", "Chance to be infinite", "This setting will allow you to control the chance of not taking a poison charge. For example if you want there to be a 20% chance to take poison charge anyway set 80 here.")]
        public float ChanceToBeInfinitePoison
        {
            get;
            set;
        } = 100.0f;

        internal void Load()
        {
            ConfigFile.LoadFrom(this, "InfinitePoison", true);
        }
    }
}
