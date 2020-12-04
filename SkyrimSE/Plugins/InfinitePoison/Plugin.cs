using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework;
using NetScriptFramework.SkyrimSE;

namespace InfinitePoison
{
    public sealed class InfinitePoisonPlugin : Plugin
    {
        public override string Key
        {
            get
            {
                return "infpoison";
            }
        }

        public override string Name
        {
            get
            {
                return "Infinite Poison";
            }
        }

        public override int Version
        {
            get
            {
                return 1;
            }
        }

        public override int RequiredLibraryVersion
        {
            get
            {
                return 10;
            }
        }

        internal Settings Settings
        {
            get;
            private set;
        }

        private IntPtr _RemovePoison_Func;

        protected override bool Initialize(bool loadedAny)
        {
            this.Settings = new Settings();
            this.Settings.Load();

            Events.OnSpendPoison.Register(e =>
            {
                var attacker = e.Spender;
                if (attacker == null)
                    return;

                bool isPlayer = attacker.IsPlayer;
                bool isTeam = attacker.IsPlayerTeammate;

                if (isPlayer && !this.Settings.InfinitePoisonForPlayer)
                    return;

                if (isTeam && !isPlayer && !this.Settings.InfinitePoisonForTeamMates)
                    return;

                if (!isTeam && !isPlayer && !this.Settings.InfinitePoisonForOthers)
                    return;

                if(!string.IsNullOrEmpty(this.Settings.RequiredActorMagicEffectKeyword))
                {
                    MagicItem item = null;
                    if (!attacker.HasMagicEffectWithKeywordText(this.Settings.RequiredActorMagicEffectKeyword, ref item))
                        return;
                }

                if(!string.IsNullOrEmpty(this.Settings.RequiredWeaponKeyword))
                {
                    var used = e.Item;
                    if (used == null)
                        return;

                    var wpn = used.Template;
                    if (wpn == null || !wpn.HasKeywordText(this.Settings.RequiredWeaponKeyword))
                        return;
                }

                float chance = this.Settings.ChanceToBeInfinitePoison;
                if(chance < 100.0f)
                {
                    if (chance <= 0.0f)
                        return;

                    double roll = NetScriptFramework.Tools.Randomizer.NextDouble() * 100.0;
                    if (roll >= chance)
                        return;
                }

                e.Skip = true;
            }, 100);

            if(this.Settings.ApplyReplacesPoison)
            {
                _RemovePoison_Func = NetScriptFramework.Main.GameInfo.GetAddressOf(12229);
                var ptr = NetScriptFramework.Main.GameInfo.GetAddressOf(39406, 0x89, 0, "E8 ? ? ? ? 48 85 C0");
                Memory.WriteHook(new HookParameters()
                {
                    Address = ptr,
                    IncludeLength = 0,
                    ReplaceLength = 0x13,
                    Before = ctx =>
                    {
                        var item = MemoryObject.FromAddress<ExtraContainerChanges.ItemEntry>(ctx.CX);
                        if(item != null)
                        {
                            var list = item.ExtraData;
                            if(list != null)
                            {
                                foreach(var obj in list)
                                {
                                    if(obj != null)
                                    {
                                        var extraDataPtr = obj.Cast<BSExtraDataList>();
                                        if (extraDataPtr != IntPtr.Zero)
                                            Memory.InvokeCdecl(_RemovePoison_Func, extraDataPtr, 0x3E);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                });
            }

            return true;
        }
    }
}
