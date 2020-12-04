using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework;
using NetScriptFramework.SkyrimSE;

namespace NoLockPicking
{
    public sealed class LockpickPlugin : Plugin
    {
        public override string Key
        {
            get
            {
                return "lockpick";
            }
        }

        public override string Name
        {
            get
            {
                return "No Lock Picking";
            }
        }

        public override int Version
        {
            get
            {
                return 2;
            }
        }

        internal CachedVid Addr_DoorOpen_Func;
        internal CachedVid Addr_DoorOpen_Hook;
        internal CachedVid Addr_DoorOpen_Jmp;

        internal CachedVid Addr_ContOpen_Func;
        internal CachedVid Addr_ContOpen_Hook;

        internal CachedVid Addr_CheckItemCount_Func;
        internal CachedVid Addr_ImpossibleLock_Setting;
        internal CachedVid Addr_OutOfLockpick_Setting;
        internal CachedVid Addr_LockPickMisc_Var;
        internal CachedVid Addr_ApplyPerk_Func;
        internal CachedVid Addr_ActualUnlock_Func;
        internal CachedVid Addr_CurrentLockPickTarget_Var;
        internal CachedVid Addr_CurrentLockPickRank_Var;
        internal CachedVid Addr_CurrentLockPickThing_Var;
        internal CachedVid Addr_PlaySound_Func;
        internal CachedVid Addr_PickBreakSound_Var;
        internal CachedVid Addr_LockedSound_Var;

        internal IntPtr FakeMenu;

        public static Settings cfg
        {
            get;
            private set;
        }

        protected override bool Initialize(bool loadedAny)
        {
            cfg = new Settings();
            cfg.Load();

            if (!cfg.Enabled)
                return true;
            
            this.Addr_DoorOpen_Func = CachedVid.Initialize(17521);
            this.Addr_DoorOpen_Hook = CachedVid.Initialize(17521, 0x313, 0, "E8 ? ? ? ? E9");
            this.Addr_DoorOpen_Jmp = CachedVid.Initialize(17521, 0x65B, 0, "48 8B 9C 24 D0 01 00 00 48 81 C4 80 01 00 00");

            this.Addr_ContOpen_Func = CachedVid.Initialize(17485);
            this.Addr_ContOpen_Hook = CachedVid.Initialize(17485, 0x1AC, 0, "E8 ? ? ? ? 32 C0");

            this.Addr_CheckItemCount_Func = CachedVid.Initialize(19275);
            this.Addr_ImpossibleLock_Setting = CachedVid.Initialize(506405);
            this.Addr_OutOfLockpick_Setting = CachedVid.Initialize(506408);
            this.Addr_LockPickMisc_Var = CachedVid.Initialize(514921);
            this.Addr_ApplyPerk_Func = CachedVid.Initialize(23073);
            this.Addr_ActualUnlock_Func = CachedVid.Initialize(51088);
            this.Addr_CurrentLockPickTarget_Var = CachedVid.Initialize(519716);
            this.Addr_CurrentLockPickRank_Var = CachedVid.Initialize(510072);
            this.Addr_CurrentLockPickThing_Var = CachedVid.Initialize(519717);
            this.Addr_PlaySound_Func = CachedVid.Initialize(52054);
            this.Addr_PickBreakSound_Var = CachedVid.Initialize(269252);
            this.Addr_LockedSound_Var = CachedVid.Initialize(269257);

            {
                var fakealloc = Memory.Allocate(0x200); // 0x110
                Memory.WriteZero(fakealloc.Address, 0x200);
                fakealloc.Pin();
                this.FakeMenu = fakealloc.Address;
            }

            Memory.WriteHook(new HookParameters()
            {
                Address = this.Addr_DoorOpen_Hook.Value,
                IncludeLength = 5,
                ReplaceLength = 5,
                Before = ctx =>
                {
                    var actor = MemoryObject.FromAddress<Actor>(ctx.DI);
                    var obj = MemoryObject.FromAddress<TESObjectREFR>(ctx.BX);

                    lock(_tryUnlockLocker)
                    {
                        _tryUnlockCounter++;
                        var r = TryToUnlockObject(actor, obj);
                        switch (r)
                        {
                            case OpenResults.Vanilla: break;

                            case OpenResults.Return0:
                                ctx.Skip();
                                ctx.IP = this.Addr_DoorOpen_Jmp.Value;
                                ctx.AX = IntPtr.Zero;
                                break;
                            case OpenResults.Return1:
                                ctx.Skip();
                                ctx.IP = this.Addr_DoorOpen_Jmp.Value;
                                ctx.AX = new IntPtr(1);
                                break;
                            case OpenResults.TryOpenAgain:
                                {
                                    ctx.Skip();
                                    ctx.IP = this.Addr_DoorOpen_Jmp.Value;

                                    var sp = ctx.SP + 0x180;
                                    IntPtr a1 = Memory.ReadPointer(sp + 0x40);
                                    IntPtr a2 = ctx.BX;
                                    IntPtr a3 = ctx.R14;
                                    IntPtr a4 = IntPtr.Zero; // Doesn't seem to be used.
                                    IntPtr a5 = IntPtr.Zero; // Not used for door?
                                    IntPtr a6 = IntPtr.Zero; // Not used for door?

                                    ctx.AX = Memory.InvokeCdecl(this.Addr_DoorOpen_Func.Value, a1, a2, a3, a4, a5, a6);
                                }
                                break;

                            default: break;
                        }
                        _tryUnlockCounter--;
                    }
                },
            });

            Memory.WriteHook(new HookParameters()
            {
                Address = this.Addr_ContOpen_Hook.Value,
                IncludeLength = 7,
                ReplaceLength = 7,
                Before = ctx =>
                {
                    var actor = MemoryObject.FromAddress<Actor>(ctx.DI);
                    var obj = MemoryObject.FromAddress<TESObjectREFR>(ctx.BX);

                    lock(_tryUnlockLocker)
                    {
                        _tryUnlockCounter++;
                        var r = TryToUnlockObject(actor, obj);
                        switch(r)
                        {
                            case OpenResults.Vanilla: break;

                            case OpenResults.Return0:
                                ctx.Skip();
                                ctx.AX = IntPtr.Zero;
                                break;
                            case OpenResults.Return1:
                                ctx.Skip();
                                ctx.AX = new IntPtr(1);
                                break;
                            case OpenResults.TryOpenAgain:
                                {
                                    ctx.Skip();

                                    var sp = ctx.SP + 0x178;
                                    IntPtr a1 = ctx.R14;
                                    IntPtr a2 = ctx.BX;
                                    IntPtr a3 = ctx.DI;
                                    IntPtr a4 = IntPtr.Zero; // ? = a4 (not used?)
                                    IntPtr a5 = Memory.ReadPointer(sp + 0x28);
                                    IntPtr a6 = Memory.ReadPointer(sp + 0x30);

                                    ctx.AX = Memory.InvokeCdecl(this.Addr_ContOpen_Func.Value, a1, a2, a3, a4, a5, a6);
                                }
                                break;

                            default: break;
                        }
                        _tryUnlockCounter--;
                    }
                }
            });

            return true;
        }

        private static readonly object _tryUnlockLocker = new object();
        private static int _tryUnlockCounter = 0;

        private enum OpenResults
        {
            /// <summary>
            /// Don't do anything, let vanilla lock picking menu logic take over.
            /// </summary>
            Vanilla,

            /// <summary>
            /// Try to run the same activate function again. This is usually called when we actually unlock the object.
            /// </summary>
            TryOpenAgain,

            /// <summary>
            /// Return failed to activate without doing anything else.
            /// </summary>
            Return0,

            /// <summary>
            /// Return activated without doing anything else.
            /// </summary>
            Return1,
        }

        /// <summary>
        /// Reports the specified message in HUD.
        /// </summary>
        /// <param name="msg">The message.</param>
        /// <param name="sound">Play the locked sound.</param>
        /// <param name="force">Force show this.</param>
        private static void Report(string msg, bool sound = true, bool force = false)
        {
            MenuManager.ShowHUDMessage(msg, sound ? "DRSLocked" : null, true);
        }

        /// <summary>
        /// Reports the impossible to open lock.
        /// </summary>
        private void ReportImpossible()
        {
            var ptr = Memory.ReadPointer(this.Addr_ImpossibleLock_Setting.Value + 8);
            if (ptr == IntPtr.Zero)
                return;

            string sx = Memory.ReadString(ptr, false);
            Report(sx);
        }

        /// <summary>
        /// Reports the no picks.
        /// </summary>
        private void ReportNoPicks()
        {
            var ptr = Memory.ReadPointer(this.Addr_OutOfLockpick_Setting.Value + 8);
            if (ptr == IntPtr.Zero)
                return;

            string sx = Memory.ReadString(ptr, false);
            Report(sx);
        }

        internal static int IsDoingOurUnlocking = 0;

        private OpenResults TryToUnlockObject(Actor actor, TESObjectREFR obj)
        {
            if(_tryUnlockCounter != 1 || IsDoingOurUnlocking > 0)
            {
                Report("NoLockPicking: ERROR #1!");
                return OpenResults.Return0;
            }

            IntPtr plrPtr;
            IntPtr objPtr;
            if (actor == null || !actor.IsPlayer || obj == null || (plrPtr = actor.Cast<PlayerCharacter>()) == IntPtr.Zero || (objPtr = obj.Cast<TESObjectREFR>()) == IntPtr.Zero)
            {
                Report("NoLockPicking: ERROR #2!");
                return OpenResults.Return0;
            }

            if(cfg.OnlyWorksWhenSneaking && !actor.IsSneaking)
            {
                Memory.InvokeCdecl(this.Addr_PlaySound_Func.Value, this.Addr_LockedSound_Var.Value);
                return OpenResults.Return0;
            }

            var extraLock = obj.GetLockData(true);
            if(extraLock == null)
            {
                Report("NoLockPicking: ERROR #3!");
                return OpenResults.Return0;
            }

            var lockData = extraLock.LockData;
            if(lockData == null)
            {
                Report("NoLockPicking: ERROR #4!");
                return OpenResults.Return0;
            }

            if(lockData.IsBroken)
            {
                this.ReportImpossible();
                return OpenResults.Return0;
            }

            int lockLevel = lockData.LockLevel;
            ExtraLockDifficultyRanks lockDifficulty = lockData.GetDifficultyRank(obj);

            if(lockDifficulty == ExtraLockDifficultyRanks.Impossible)
            {
                if(!cfg.AllowPickKeyDoors)
                {
                    this.ReportImpossible();
                    return OpenResults.Return0;
                }

                lockDifficulty = ExtraLockDifficultyRanks.VeryHard;
                lockLevel = 100;
            }

            bool hasSkeletonKey = false;
            int lockPickCount = 0;

            if (cfg.SuperCheatMode)
                hasSkeletonKey = true;
            else
            {
                var objMgr = BGSDefaultObjectManager.Instance;
                IntPtr mgrPtr;
                if(objMgr != null && (mgrPtr = objMgr.Cast<BGSDefaultObjectManager>()) != IntPtr.Zero)
                {
                    if(Memory.ReadUInt8(mgrPtr + 0xB85) != 0)
                    {
                        var skey = Memory.ReadPointer(mgrPtr + 0x48);
                        if (skey != IntPtr.Zero && Memory.InvokeCdecl(this.Addr_CheckItemCount_Func.Value, plrPtr, skey).ToInt32Safe() > 0)
                            hasSkeletonKey = true;
                    }
                }
            }

            if(!hasSkeletonKey)
            {
                var lpick = Memory.ReadPointer(this.Addr_LockPickMisc_Var.Value);
                if (lpick != IntPtr.Zero)
                    lockPickCount = Memory.InvokeCdecl(this.Addr_CheckItemCount_Func.Value, plrPtr, lpick).ToInt32Safe();
            }

            int requiredPicks = 0;
            int requiredInventoryPicks = cfg.RequireAtLeastOneLockPickInInventory ? 1 : 0;
            if(!hasSkeletonKey)
            {
                float mySkillMult = 1.0f;
                if (cfg.BonusReducesPickUsage)
                {
                    using (var alloc = Memory.Allocate(0x10))
                    {
                        Memory.WriteZero(alloc.Address, 0x10);
                        Memory.WriteFloat(alloc.Address, 1.0f);
                        Memory.InvokeCdecl(this.Addr_ApplyPerk_Func.Value, (int)PerkEntryPoints.Modify_Lockpick_Sweet_Spot, plrPtr, objPtr, alloc.Address);
                        mySkillMult = Memory.ReadFloat(alloc.Address);
                    }
                }

                float mySkill = actor.GetActorValue(ActorValueIndices.Lockpicking);
                //float mySkillExtra = actor.GetActorValue(ActorValueIndices.LockpickingMod);
                //float mySkillExtraPower = actor.GetActorValue(ActorValueIndices.LockpickingPowerMod);

                if(mySkillMult <= 0.0f)
                {
                    this.ReportImpossible();
                    return OpenResults.Return0;
                }

                int diffIndex = 0;
                {
                    int diffDiff = (int)mySkill - lockLevel;
                    if (diffDiff > 40) // My skill is 40+ to lock's
                        diffIndex = 0;
                    else if (diffDiff > 15) // My skill is 15+ to lock's
                        diffIndex = 1;
                    else if (diffDiff > -15) // My skill is -15 to 15 to lock's
                        diffIndex = 2;
                    else if (diffDiff > -40) // My skill is -40 to -15 to lock's
                        diffIndex = 3;
                    else
                        diffIndex = 4; // My skill is -40 or lower to lock's
                }

                double cost = 0.0;
                switch(diffIndex)
                {
                    case 0: cost = cfg.CostVeryEasy; break;
                    case 1: cost = cfg.CostEasy; break;
                    case 2: cost = cfg.CostMedium; break;
                    case 3: cost = cfg.CostHard; break;
                    case 4: cost = cfg.CostVeryHard; break;
                }

                if(cost < 0.0)
                {
                    this.ReportImpossible();
                    return OpenResults.Return0;
                }

                if (cost > 0.0)
                {
                    float unbreakable = 0.0f;
                    using (var alloc3 = Memory.Allocate(0x10))
                    {
                        Memory.WriteFloat(alloc3.Address, 0.0f);
                        Memory.InvokeCdecl(this.Addr_ApplyPerk_Func.Value, 0x41, plrPtr, alloc3.Address, IntPtr.Zero);
                        unbreakable = Memory.ReadFloat(alloc3.Address);
                    }
                    if (unbreakable >= 0.5f)
                        cost = 0.0;
                }

                if (cost > 0.0)
                {
                    cost /= mySkillMult;

                    requiredPicks = (int)cost;
                    int maxCost = requiredPicks;
                    cost -= requiredPicks;
                    if (cost > 0.000001)
                    {
                        maxCost++;

                        if (NetScriptFramework.Tools.Randomizer.NextDouble() < cost)
                            requiredPicks++;
                    }

                    requiredInventoryPicks = Math.Max(requiredInventoryPicks, maxCost);
                }

                if (requiredInventoryPicks > lockPickCount || requiredPicks > lockPickCount)
                {
                    this.ReportNoPicks();
                    return OpenResults.Return0;
                }
            }

            // Take picks.
            if (!hasSkeletonKey && requiredPicks > 0)
            {
                actor.InvokeVTableThisCall<TESObjectREFR>(0x2B0, NetScriptFramework.Main.TrashMemory, Memory.ReadPointer(this.Addr_LockPickMisc_Var.Value), requiredPicks, 0, 0, 0, 0, 0);
                Memory.InvokeCdecl(this.Addr_PlaySound_Func.Value, this.Addr_PickBreakSound_Var.Value);
            }

            // Unlock object.
            {
                IsDoingOurUnlocking++;
                Memory.WritePointer(this.Addr_CurrentLockPickTarget_Var.Value, objPtr);
                Memory.WriteInt32(this.Addr_CurrentLockPickRank_Var.Value, Math.Max(0, Math.Min(4, (int)lockDifficulty)));
                Memory.WritePointer(this.Addr_CurrentLockPickThing_Var.Value, IntPtr.Zero);
                Memory.WriteUInt8(this.FakeMenu + 0x10D, 0);
                Memory.WriteUInt8(this.FakeMenu + 0x10B, 1);
                Memory.InvokeCdecl(this.Addr_ActualUnlock_Func.Value, this.FakeMenu);
                Memory.WritePointer(this.Addr_CurrentLockPickTarget_Var.Value, IntPtr.Zero);
                IsDoingOurUnlocking--;
            }
            
            //return OpenResults.TryOpenAgain;
            return OpenResults.Return0;
        }
    }
}
