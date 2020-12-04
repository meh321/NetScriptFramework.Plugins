using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework;
using NetScriptFramework.SkyrimSE;

namespace InfiniteArrows
{
    public class InfiniteArrowsPlugin : Plugin
    {
        public override string Key
        {
            get
            {
                return "infarrows";
            }
        }

        public override string Name
        {
            get
            {
                return "Infinite Arrows";
            }
        }

        public override string Author
        {
            get
            {
                return "meh321";
            }
        }

        public override int RequiredLibraryVersion
        {
            get
            {
                return 10;
            }
        }

        public override int Version
        {
            get
            {
                return 1;
            }
        }

        protected override bool Initialize(bool loadedAny)
        {
            this.init();

            return true;
        }

        public Settings Settings
        {
            get;
            private set;
        }

        private bool IsAlwaysInfiniteAmmoForPlayer()
        {
            return string.IsNullOrEmpty(this.Settings.OnlyIfWeaponHasKeyword) && string.IsNullOrEmpty(this.Settings.RequiredMagicEffectWithKeyword) && this.Settings.InfiniteArrowsForPlayer;
        }

        private bool _had_infinite_ammo = false;
        
        private void init()
        {
            this.Settings = new Settings();
            this.Settings.Load();

            Events.OnSpendAmmo.Register(e =>
            {
                var actor = e.Spender;
                if (actor == null)
                    return;

                bool isPlayer = actor.IsPlayer;
                bool isTeamMate = !isPlayer && actor.IsPlayerTeammate;
                bool shouldForce = !isPlayer && this.Settings.ForceAllNPCsToUseArrows;
                int has = e.HasAmount;

                if (isPlayer)
                    this._had_infinite_ammo = false;

                if (!string.IsNullOrEmpty(this.Settings.OnlyIfWeaponHasKeyword))
                {
                    var weap = e.Weapon;
                    if (weap == null || !weap.HasKeywordText(this.Settings.OnlyIfWeaponHasKeyword))
                        return;
                }

                if(!string.IsNullOrEmpty(this.Settings.RequiredMagicEffectWithKeyword))
                {
                    MagicItem item = null;
                    if (!actor.HasMagicEffectWithKeywordText(this.Settings.RequiredMagicEffectWithKeyword, ref item))
                        return;
                }

                if (has <= this.Settings.InfiniteArrowsOnlyIfHasThisAmountOrLess)
                {
                    if (this.Settings.InfiniteArrowsForPlayer && isPlayer)
                    {
                        e.ReduceAmount = 0;
                        this._had_infinite_ammo = true;
                        return;
                    }

                    if (this.Settings.InfiniteArrowsForNPC && !isPlayer)
                    {
                        e.ReduceAmount = 0;
                        return;
                    }

                    if (this.Settings.InfiniteArrowsForTeamMate && isTeamMate)
                    {
                        e.ReduceAmount = 0;
                        return;
                    }
                }

                if (shouldForce)
                    e.Force = true;
            }, 100);

            if(this.Settings.InfiniteArrowsForPlayer)
            {
                if (this.Settings.DontShowAmmoCounterInHUDIfInfiniteForPlayer && this.IsAlwaysInfiniteAmmoForPlayer())
                {
                    var addr = NetScriptFramework.Main.GameInfo.GetAddressOf(50734, 0, 0, "40 57 41 56 41 57");
                    /*var alloc = Memory.Allocate(0x10, 0, true);
                    alloc.Pin();
                    Memory.WriteUInt8(alloc.Address, 0xC3, true);
                    var retaddr = alloc.Address;
                    Memory.WriteHook(new HookParameters()
                    {
                        Address = addr,
                        ReplaceLength = 6,
                        IncludeLength = 6,
                        Before = ctx =>
                        {
                            if(_had_infinite_ammo)
                            {
                                ctx.Skip();
                                ctx.IP = retaddr;
                            }
                        },
                    });*/
                    Memory.WriteUInt8(addr, 0xC3, true);
                }
                else
                {
                    Events.OnReduceHUDAmmoCounter.Register(e =>
                    {
                        /*int has = e.HasAmount;
                        if (has <= this.Settings.InfiniteArrowsOnlyIfHasThisAmountOrLess)*/
                        if(this._had_infinite_ammo)
                            e.ReduceAmount = 0;
                    }, 100);
                }
            }
        }
    }
}
