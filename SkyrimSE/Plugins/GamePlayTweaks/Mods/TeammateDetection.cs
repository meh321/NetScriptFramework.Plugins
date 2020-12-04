using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework.SkyrimSE;

namespace GamePlayTweaks.Mods
{
    class TeammateDetection : Mod
    {
        public TeammateDetection() : base()
        {
            settings.TeammateUndetectableSneak = this.CreateSettingBool("TeammateUndetectableWhenSneaking", true, "Player teammates are undetectable by non-teammates when they are sneaking.");
            settings.TeammateUndetectableInvis = this.CreateSettingBool("TeammateUndetectableWhenInvis", true, "Player teammates are undetectable by non-teammates if they have invisibility spell on (regardless of if they are sneaking).");
            settings.TeammateUndetectableBleedout = this.CreateSettingBool("TeammateUndetectableWhenBleedout", false, "Player teammates become undetectable when they enter the bleedout animation. This will mean enemies stop caring about them and move to other targets.");
            settings.TeammateUndetectableFavor = this.CreateSettingBool("TeammateUndetectableWhenFavor", false, "Player teammates become undetectable when they are following a specific command you gave them.");
            settings.TeammateUndetectableIfPlayer = this.CreateSettingBool("TeammateUndetectableIfPlayerIs", true, "All settings here require that player is not detected first. If player is detected then teammates won't be undetectable in any way (unless the cheat mode is enabled).");
            settings.TeammateUndetectableNoCombat = this.CreateSettingBool("TeammateUndetectableNoCombat", true, "All settings here require that player and teammate are both not in combat (unless cheat mode is enabled or the bleedout setting is triggered). If you want your teammates to be able to go undetectable in combat, such as by drinking an invisibility potion then disable this option.");
            settings.TeammateUndetectableCheat = this.CreateSettingBool("TeammateUndetectableCheat", false, "Player teammates are always undetectable no matter what. This is just a cheat.");
            settings.TeammateRequiresWeaponOut = this.CreateSettingBool("TeammateRequiresWeaponOut", false, "If enabled then teammate can't detect anyone (and thus can't aggro) unless they have a weapon out first.");
            settings.TeammateRequiresNotSneaking = this.CreateSettingBool("TeammateRequiresNotSneaking", false, "If enabled then teammate can't detect anyone (and thus can't aggro) if they are sneaking.");
            settings.UndetectableFaction = this.CreateSettingForm<TESFaction>("UndetectableFaction", "", "If set then anyone in this faction (rank >= 0) will be undetectable no matter what.");
        }

        private static class settings
        {
            internal static SettingValue<bool> TeammateUndetectableSneak;
            internal static SettingValue<bool> TeammateUndetectableInvis;
            internal static SettingValue<bool> TeammateUndetectableBleedout;
            internal static SettingValue<bool> TeammateUndetectableFavor;
            internal static SettingValue<bool> TeammateUndetectableIfPlayer;
            internal static SettingValue<bool> TeammateUndetectableNoCombat;
            internal static SettingValue<bool> TeammateUndetectableCheat;
            internal static SettingValue<bool> TeammateRequiresWeaponOut;
            internal static SettingValue<bool> TeammateRequiresNotSneaking;
            internal static SettingValue<TESFaction> UndetectableFaction;
        }

        internal override string Description
        {
            get
            {
                return "Settings related to making teammates undetectable.";
            }
        }

        internal override void Apply()
        {
            Events.OnCalculateDetection.Register(e =>
            {
                var source = e.SourceActor;
                var target = e.TargetActor;
                var plr = PlayerCharacter.Instance;

                if(settings.UndetectableFaction.Value != null && target != null && target.GetFactionRank(settings.UndetectableFaction.Value) >= 0)
                {
                    e.ResultValue = -1000;
                    return;
                }
                
                if(source != null && target != null && !source.Equals(target) && plr != null && !target.Equals(plr) && !source.Equals(plr))
                {
                    bool srcTeammate = source.IsPlayerTeammate;
                    bool tgTeammate = target.IsPlayerTeammate;

                    if (!srcTeammate && tgTeammate)
                    {
                        if (settings.TeammateUndetectableCheat.Value)
                        {
                            e.ResultValue = -1000;
                            return;
                        }

                        if (settings.TeammateUndetectableIfPlayer.Value)
                        {
                            if (source.CanDetect(plr))
                                return;
                        }

                        if (settings.TeammateUndetectableBleedout.Value)
                        {
                            if (target.IsBleedingOut)
                            {
                                e.ResultValue = -1000;
                                return;
                            }
                        }

                        if (settings.TeammateUndetectableNoCombat.Value)
                        {
                            if (target.IsInCombat || plr.IsInCombat)
                                return;
                        }

                        if(settings.TeammateUndetectableFavor.Value)
                        {
                            if(target.IsDoingFavor)
                            {
                                e.ResultValue = -1000;
                                return;
                            }
                        }

                        if (settings.TeammateUndetectableSneak.Value)
                        {
                            if (target.IsSneaking)
                            {
                                e.ResultValue = -1000;
                                return;
                            }
                        }

                        if (settings.TeammateUndetectableInvis.Value)
                        {
                            if (target.FindFirstEffectWithArchetype(Archetypes.Invisibility, false) != null)
                            {
                                e.ResultValue = -1000;
                                return;
                            }
                        }
                    }
                    else if(srcTeammate && !tgTeammate)
                    {
                        if(settings.TeammateRequiresWeaponOut.Value && !source.IsInCombat && !source.IsWeaponDrawn && !source.IsDoingFavor)
                        {
                            e.ResultValue = -1000;
                            return;
                        }

                        if(settings.TeammateRequiresNotSneaking.Value && !source.IsInCombat && source.IsSneaking && !source.IsDoingFavor)
                        {
                            e.ResultValue = -1000;
                            return;
                        }
                    }
                }
            }, -500);
        }
    }
}
