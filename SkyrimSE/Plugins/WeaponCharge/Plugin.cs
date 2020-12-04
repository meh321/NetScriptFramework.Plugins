using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework;
using NetScriptFramework.SkyrimSE;

namespace WeaponCharge
{
    public sealed class WeaponChargePlugin : Plugin
    {
        public override string Key
        {
            get
            {
                return "weapon_charge";
            }
        }

        public override string Name
        {
            get
            {
                return "Weapon Charge";
            }
        }

        public override int Version
        {
            get
            {
                return 2;
            }
        }

        public override string Author
        {
            get
            {
                return "meh321";
            }
        }

        private IntPtr Addr_GetCurrentGameTime_Func;
        private IntPtr Addr_RealHoursPassed_Func;
        private IntPtr Addr_GetSoul_Func;
        private IntPtr Addr_GetCharge_Func;
        private IntPtr Addr_SetCharge_Func;
        private IntPtr Addr_GetMaxCharge_Func;
        private IntPtr Addr_GetEnchantment_Func;
        private IntPtr Addr_GetActorValueForCost_Func;
        private IntPtr Addr_IsWorn_Func;
        private IntPtr Addr_RemoveExtraData_Func;
        private IntPtr Addr_GetCount_Func;

        internal Settings Settings;

        protected override bool Initialize(bool loadedAny)
        {
            this.Settings = new Settings();
            this.Settings.Load();

            this._rechargeInterval = (long)(this.Settings.RechargeIntervalGameHours * 3600.0f) * 1000;

            this.Addr_GetCurrentGameTime_Func = NetScriptFramework.Main.GameInfo.GetAddressOf(56475);
            this.Addr_RealHoursPassed_Func = NetScriptFramework.Main.GameInfo.GetAddressOf(54842);
            this.Addr_GetSoul_Func = NetScriptFramework.Main.GameInfo.GetAddressOf(11561);
            this.Addr_SetCharge_Func = NetScriptFramework.Main.GameInfo.GetAddressOf(11473);
            this.Addr_GetCharge_Func = NetScriptFramework.Main.GameInfo.GetAddressOf(11560);
            this.Addr_GetMaxCharge_Func = NetScriptFramework.Main.GameInfo.GetAddressOf(14412);
            this.Addr_GetEnchantment_Func = NetScriptFramework.Main.GameInfo.GetAddressOf(14411);
            this.Addr_GetActorValueForCost_Func = NetScriptFramework.Main.GameInfo.GetAddressOf(33817);
            this.Addr_IsWorn_Func = NetScriptFramework.Main.GameInfo.GetAddressOf(11550);
            this.Addr_RemoveExtraData_Func = NetScriptFramework.Main.GameInfo.GetAddressOf(12229);
            this.Addr_GetCount_Func = NetScriptFramework.Main.GameInfo.GetAddressOf(11558);

            Events.OnFrame.Register(e =>
            {
                if(_stop == 0)
                    this.UpdateRecharge();
            });

            Events.OnMainMenu.Register(e =>
            {
                this.Clear();

                if(e.Entering)
                    _stop++;
                else
                    _stop--;
            });

            return true;
        }

        private long? _lastGameMs = null;
        private long? _lastRealMs = null;
        private int _stop = 0;
        private long _rechargeInterval;

        private void UpdateRecharge()
        {
            var plr = PlayerCharacter.Instance;
            if(plr == null)
            {
                this.Clear();
                return;
            }

            long nowRealMs = (long)(Memory.InvokeCdeclF(this.Addr_RealHoursPassed_Func) * 3600000.0);

            // First update, or load or suddenly jumped more than 3 seconds of real time ahead.
            if (!_lastRealMs.HasValue || nowRealMs < _lastRealMs.Value || (nowRealMs - _lastRealMs.Value) >= 3000)
                this.Clear();

            _lastRealMs = nowRealMs;

            var main = NetScriptFramework.SkyrimSE.Main.Instance;
            if (main != null && !main.IsGamePaused)
            {
                long nowGameMs = (long)(Memory.InvokeCdeclF(this.Addr_GetCurrentGameTime_Func) * 24.0 * 3600000.0);
                if (!_lastGameMs.HasValue || _lastGameMs.Value > nowGameMs)
                    _lastGameMs = nowGameMs;
                else if (_lastGameMs.Value < nowGameMs)
                {
                    long diff = nowGameMs - _lastGameMs.Value;
                    if (diff >= this._rechargeInterval)
                    {
                        int mult = (int)(diff / this._rechargeInterval);
                        diff -= mult * this._rechargeInterval;
                        this._lastGameMs = nowGameMs - diff;
                        this.RechargeNow(mult);
                    }
                }
            }
        }

        private void RechargeNow(double mult)
        {
            var plr = PlayerCharacter.Instance;
            if (plr == null)
                return;

            var inventory = plr.Inventory;
            if (inventory == null)
                return;

            var objects = inventory.Objects;
            if (objects == null)
                return;
            
            double bestSoul = 0.0;
            double totalSoul = 0.0;
            List<Tuple<BSExtraDataList, float, float, int, TESForm>> todo = new List<Tuple<BSExtraDataList, float, float, int, TESForm>>(4);
            foreach(var o in objects)
            {
                if (o == null)
                    continue;

                var item = o.Template;
                if (item == null)
                    continue;

                var fptr = item.Cast<TESForm>();
                if (fptr == IntPtr.Zero)
                    continue;

                bool canSoul = item.FormType == FormTypes.SoulGem;
                byte baseSoul = canSoul ? Memory.ReadUInt8(fptr + 0x108) : (byte)0;
                int left = o.Count;

                var els = o.ExtraData;
                if(els != null)
                {
                    foreach(var edata in els)
                    {
                        if (edata == null)
                            continue;

                        var eptr = edata.Cast<BSExtraDataList>();
                        if (eptr == IntPtr.Zero)
                            continue;

                        int count = Memory.InvokeCdecl(this.Addr_GetCount_Func, eptr).ToInt32Safe();
                        left -= count;

                        if (canSoul)
                        {
                            byte soul = Memory.InvokeCdecl(this.Addr_GetSoul_Func, eptr).ToUInt8();
                            if (soul == 0)
                                soul = baseSoul;
                            
                            if (soul != 0)
                            {
                                double amt = 0.0;
                                switch (soul)
                                {
                                    case 1: amt = Settings.PettyChargePercentPerDay; break;
                                    case 2: amt = Settings.LesserChargePercentPerDay; break;
                                    case 3: amt = Settings.CommonChargePercentPerDay; break;
                                    case 4: amt = Settings.GreaterChargePercentPerDay; break;
                                    case 5: amt = Settings.GrandChargePercentPerDay; break;
                                }

                                if (amt > 0.0)
                                {
                                    amt /= 24.0;
                                    amt *= Settings.RechargeIntervalGameHours;

                                    if (amt > bestSoul)
                                        bestSoul = amt;

                                    if (amt > 0.0)
                                        totalSoul += amt * count;
                                }
                            }
                        }

                        float max = Memory.InvokeCdecl(this.Addr_GetMaxCharge_Func, fptr, eptr).ToInt32Safe();
                        if (max <= 0.0f)
                            continue;

                        bool isWorn = Memory.InvokeCdecl(this.Addr_IsWorn_Func, eptr, 1, 0).ToInt32Safe() != 0;
                        int av = -1;
                        if(isWorn)
                        {
                            var enchantment = Memory.InvokeCdecl(this.Addr_GetEnchantment_Func, fptr, eptr);
                            if (enchantment != IntPtr.Zero)
                            {
                                //bool isLeft = Memory.InvokeCdecl(this.Addr_IsWorn_Func, eptr, 0, 1).ToInt32Safe() != 0;
                                bool isLeft = Memory.InvokeCdecl(this.Addr_IsWorn_Func, eptr, 0, 0).ToInt32Safe() == 0 && Memory.InvokeCdecl(this.Addr_IsWorn_Func, eptr, 0, 1).ToInt32Safe() != 0;
                                av = Memory.InvokeCdecl(this.Addr_GetActorValueForCost_Func, enchantment, isLeft ? 0 : 1).ToInt32Safe();
                                if (av != 0x40 && av != 0x52)
                                    av = -1;
                            }
                        }
                        else if (Settings.OnlyRechargeEquippedWeapons)
                            continue;

                        float charge;
                        if (av >= 0)
                            charge = plr.GetActorValue((ActorValueIndices)av);
                        else
                            charge = Memory.InvokeCdeclF(this.Addr_GetCharge_Func, eptr);

                        if (charge < 0.0f)
                            continue;

                        if (charge + 0.001f >= max)
                            continue;
                        
                        todo.Add(new Tuple<BSExtraDataList, float, float, int, TESForm>(edata, charge, max, av, item));
                    }
                }

                if(left > 0)
                {
                    if(canSoul && baseSoul != 0)
                    {
                        byte soul = baseSoul;
                        double amt = 0.0;
                        switch (soul)
                        {
                            case 1: amt = Settings.PettyChargePercentPerDay; break;
                            case 2: amt = Settings.LesserChargePercentPerDay; break;
                            case 3: amt = Settings.CommonChargePercentPerDay; break;
                            case 4: amt = Settings.GreaterChargePercentPerDay; break;
                            case 5: amt = Settings.GrandChargePercentPerDay; break;
                        }

                        if (amt > 0.0)
                        {
                            amt /= 24.0;
                            amt *= Settings.RechargeIntervalGameHours;

                            if (amt > bestSoul)
                                bestSoul = amt;

                            if (amt > 0.0)
                                totalSoul += amt * left;
                        }
                    }
                }
            }
            
            if (todo.Count == 0)
                return;

            double soulAmt = (Settings.SoulGemStacking ? totalSoul : bestSoul) * mult * 0.01;
            if (Settings.MultipleWeaponsChargeSlower)
                soulAmt /= todo.Count;
            
            if (soulAmt <= 0.0)
                return;

            foreach(var t in todo)
            {
                double prev_ratio = (double)t.Item2 / t.Item3;
                double new_ratio = prev_ratio + soulAmt;
                if (new_ratio > 1.0)
                    new_ratio = 1.0;

                float new_amt = (float)(new_ratio * t.Item3);
                var eptr = t.Item1.Cast<BSExtraDataList>();
                if(eptr != IntPtr.Zero)
                {
                    if (t.Item4 >= 0)
                    {
                        plr.RestoreActorValue((ActorValueIndices)t.Item4, new_amt - t.Item2);
                        float nowValue = plr.GetActorValue((ActorValueIndices)t.Item4);
                        if(nowValue + 0.001f < new_amt)
                            plr.SetActorValue((ActorValueIndices)t.Item4, new_amt);
                    }
                    else
                    {
                        if (new_ratio < 1.0)
                            Memory.InvokeCdecl(this.Addr_SetCharge_Func, eptr, new_amt);
                        else
                            Memory.InvokeCdecl(this.Addr_RemoveExtraData_Func, eptr, 0x28);
                    }
                }
            }
        }

        private void Clear()
        {
            this._lastGameMs = null;
            this._lastRealMs = null;
        }
    }
}
