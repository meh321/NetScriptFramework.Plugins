//#define DEBUG_TEST
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugFixesSSE.Fixes
{
    class FixAbilityConditionBug : Fix
    {
        public FixAbilityConditionBug()
        {
            BoolParameters["ForceAccurateCheck"] = true;
        }

        internal override string Description
        {
            get
            {
                return "When an ablity/spell has been active for a very long time (70+ hours) it will stop updating conditions because the elapsed time gets so big that it will have floating point inaccuracy when checking for frame time difference. This fix will replace the check with its own timer system that will be completely accurate no matter how large the elapsed duration is. It will also fix a problem where around every 10th or 20th second the game would skip the conditions update because of reasons. The separate parameter ForceAccurateCheck will always use new system if True and only use new system when the elapsed duration gets large enough if False. If you are experiencing stutters in combat that you didn't have before you can disable this and see if that was the cause (if it was, please report it on the mod page).";
            }
        }

        private IntPtr NoConditionCheck;
        private IntPtr GameSettingValue;

#if DEBUG_TEST
        private int _debug_lastup = 0;
        private int _debug_did = 0;
        private long? _debug_ptr = null;
#endif

        private readonly object Locker = new object();
        private readonly HashSet<long> Done = new HashSet<long>();
        private long LastCounter = -1;
        private long UpdateDiff = -1;
        private long UpdateTimer = 0;
        private int? LastTick = null;
        private bool ForceAccurate = true;

        private void UpdateTime()
        {
            int now = Environment.TickCount;
            if (!this.LastTick.HasValue)
                this.LastTick = now;
            else
            {
                int diff = unchecked(now - this.LastTick.Value);
                if (diff == 0)
                    return;

                this.LastTick = now;
                this.UpdateTimer += diff;
            }

            if(this.UpdateDiff < 0)
            {
                double interval = Math.Max(0.001f, NetScriptFramework.Memory.ReadFloat(this.GameSettingValue));
                this.UpdateDiff = (long)(1000.0 / interval);
                if (this.UpdateDiff <= 0)
                    this.UpdateDiff = 1;
            }

            long curCounter = this.UpdateTimer / this.UpdateDiff;
            if(curCounter != this.LastCounter)
            {
                this.LastCounter = curCounter;
                this.Done.Clear();
            }
        }
        
        internal override void Apply()
        {
            this.ForceAccurate = this.BoolParameters["ForceAccurateCheck"];

            ulong funcVid = 33287;
            var loc1 = NetScriptFramework.Main.GameInfo.GetAddressOf(funcVid, 0xDD, 0, "F3 0F 10 4F 70");
            this.NoConditionCheck = NetScriptFramework.Main.GameInfo.GetAddressOf(funcVid, 0x1DD, 0, "48 8B 5C 24 58");
            this.GameSettingValue = NetScriptFramework.Main.GameInfo.GetAddressOf(506258, 8);
            
            NetScriptFramework.Memory.WriteHook(new NetScriptFramework.HookParameters()
            {
                Address = loc1,
                IncludeLength = 0,
                ReplaceLength = 0x79,
                Before = ctx =>
                {
#if DEBUG_TEST
                    bool did = true;
#endif
                    double diff = ctx.XMM6f;
                    if (diff <= 0.0)
                    {
                        ctx.IP = this.NoConditionCheck;
#if DEBUG_TEST
                        did = false;
#endif
                    }
                    else
                    {
                        double elapsedSeconds = !this.ForceAccurate ? NetScriptFramework.Memory.ReadFloat(ctx.DI + 0x70) : 0.0;

                        // No floating point inaccuracy, do this without locking.
                        if (!this.ForceAccurate && elapsedSeconds <= 86400.0)
                        {
                            double calculatedInterval = NetScriptFramework.Memory.ReadFloat(this.GameSettingValue);
                            long prevCounter = (long)(elapsedSeconds * calculatedInterval);
                            long nextCounter = (long)((elapsedSeconds + diff) * calculatedInterval);

                            if (prevCounter == nextCounter || diff == 0.0)
                            {
                                ctx.IP = this.NoConditionCheck;
#if DEBUG_TEST
                                did = false;
#endif
                            }
                        }
                        else
                        {
                            lock(this.Locker)
                            {
                                this.UpdateTime();

                                if(!this.Done.Add(ctx.DI.ToInt64()))
                                {
                                    ctx.IP = this.NoConditionCheck;
#if DEBUG_TEST
                                    did = false;
#endif
                                }
                            }
                        }
                    }

#if DEBUG_TEST
                    if(did && _debug_ptr.HasValue && _debug_ptr.Value == ctx.DI.ToInt64())
                        _debug_did++;
#endif
                }
            });

#if DEBUG_TEST
            NetScriptFramework.SkyrimSE.Events.OnFrame.Register(e =>
            {
                var main = NetScriptFramework.SkyrimSE.Main.Instance;
                if (main == null || main.IsGamePaused)
                    return;

                int now = Environment.TickCount / 1000;
                if (now != _debug_lastup)
                {
                    _debug_lastup = now;
                    if (!_debug_ptr.HasValue)
                    {
                        var plr = NetScriptFramework.SkyrimSE.PlayerCharacter.Instance;
                        uint fid = 0x35E38;
                        var ef = plr.ActiveEffects.FirstOrDefault(q => (q.Item.FormId & 0x00FFFFFF) == fid || (q.BaseEffect.FormId & 0x00FFFFFF) == fid);
                        if(ef != null)
                            _debug_ptr = ef.Address.ToInt64();
                    }
                    else
                    {
                        var ef = NetScriptFramework.MemoryObject.FromAddress<NetScriptFramework.SkyrimSE.ActiveEffect>(new IntPtr(_debug_ptr.Value));
                        NetScriptFramework.Main.WriteDebugMessage("Effect " + ef.ToString() + " elapsed time: " + ef.ElapsedSeconds + " did: " + _debug_did);
                    }
                    _debug_did = 0;
                }
            });
#endif
        }
    }
}
