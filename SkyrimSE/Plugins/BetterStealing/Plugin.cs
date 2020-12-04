using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework;
using NetScriptFramework.SkyrimSE;

namespace BetterStealing
{
    public sealed class BetterStealingPlugin : Plugin
    {
        public override string Key
        {
            get
            {
                return "better_stealing";
            }
        }

        public override string Author
        {
            get
            {
                return "meh321";
            }
        }

        public override string Name
        {
            get
            {
                return "Better Stealing";
            }
        }

        public override int Version
        {
            get
            {
                return 2;
            }
        }

        protected override bool Initialize(bool loadedAny)
        {
            new Settings().Load();

            this.init();
            return true;
        }

        internal static IntPtr _SetItemOwner_Func;
        internal static IntPtr _SetListOwner_Func;
        internal static IntPtr _GetCount_Func;
        internal static IntPtr _SetNorth_Func;
        internal static IntPtr _GetNorth_Func;
        internal static IntPtr _ContOpenType_Var;
        internal static IntPtr _ContOpen_Var;
        internal static IntPtr _CalcCost_Func;
        internal static queued_ac ApplyStolenSoon = new queued_ac(() => StealInfo.ProcessFinished(), () =>
        {
            var main = NetScriptFramework.SkyrimSE.Main.Instance;
            return main != null && !main.IsGamePaused;
        });
        internal static queued_ac FinishPickPocketCheck = new queued_ac(() => ApplyStolenSoon.Queue(0), () =>
        {
            return _InventoryExtensions.GetCurrentlyPickPocketing() == null;
        });
        internal static queued_ac ApplyFixCDataSoon = new queued_ac(() =>
        {
            var plr = PlayerCharacter.Instance;
            if (plr != null)
                StealInfo.FixAllCustomDataItems(plr.Inventory);
        }, () =>
        {
            var main = NetScriptFramework.SkyrimSE.Main.Instance;
            return main != null && !main.IsGamePaused && !_InventoryExtensions.IsAnyContainerOpened() && !ApplyStolenSoon.IsQueued && !FinishPickPocketCheck.IsQueued;
        });
        internal static List<string> NeverKeywords;
        internal static List<string> AlwaysKeywords;
        internal static List<string> RequiredKeywords;
        internal static int MaxPrice;
        internal static CachedFormList ExcludeForms;

        internal sealed class queued_ac
        {
            internal queued_ac(Action func, Func<bool> cond)
            {
                this.Frames = -1;
                this.Func = func;
                this.Cond = cond;
            }

            private int Frames;
            private readonly Action Func;
            private readonly Func<bool> Cond;

            internal bool IsQueued
            {
                get
                {
                    return this.Frames >= 0;
                }
            }

            internal void Queue(int frames)
            {
                this.Frames = frames;
            }

            internal void Clear()
            {
                this.Frames = -1;
            }

            internal bool Do()
            {
                if (this.Frames < 0)
                    return false;

                if (this.Cond != null && !this.Cond())
                    return false;

                if(this.Frames > 0)
                {
                    this.Frames--;
                    return false;
                }

                this.Frames = -1;
                this.Func();
                return true;
            }
        }
        
        private void init()
        {
            NeverKeywords = (Settings.Instance.IgnoreKeywords ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            AlwaysKeywords = (Settings.Instance.AlwaysKeywords ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            RequiredKeywords = (Settings.Instance.RequireKeywords ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            MaxPrice = Settings.Instance.MaxPrice;
            
            if (Settings.Instance.Enabled)
            {
                Events.OnMainMenu.Register(e =>
                {
                    ExcludeForms = CachedFormList.TryParse(Settings.Instance.ExcludeFormIds ?? "", "BetterStealing", "ExcludeFormIds");
                });

                this.InstallHook("send steal alarm #1", 36427, 0, 7, "48 8B C4 44 89 48 20", ctx =>
                {
                    var info = StealInfo.Begin();
                    if (info == null)
                        return;

                    info.Type = StealInfo.GetCalledFromType(Memory.ReadPointer(ctx.SP));
                    info.Thief = MemoryObject.FromAddress<Actor>(ctx.CX);
                    info.TargetObjRef = MemoryObject.FromAddress<TESObjectREFR>(ctx.DX);
                    info.TargetObjBase = MemoryObject.FromAddress<TESForm>(ctx.R8);
                    info.TargetObjCount = ctx.R9.ToInt32Safe();
                    info.UnspecifiedItemsWorth = Memory.ReadInt32(ctx.SP + 0x28);
                    info.OwnerForm = MemoryObject.FromAddress<TESForm>(Memory.ReadPointer(ctx.SP + 0x30));
                });

                this.InstallHook("send steal alarm #2", 36427, 0x9D3, 7, "48 81 C4 38 01 00 00", ctx =>
                {
                    var info = StealInfo.Get();
                    if (info == null)
                        return;

                    info.WasDetected = Memory.ReadUInt8(ctx.SP + 0x40) != 0;
                    info.State = 1;
                    ApplyStolenSoon.Queue(1);
                });

                this.InstallHook("open container", 50195, 0x20, 5, "E8", ctx =>
                {
                    StealInfo.BeginContainer();

                    ApplyFixCDataSoon.Queue(3);

                    var pickpocket = _InventoryExtensions.GetCurrentlyPickPocketing();
                    if(pickpocket != null)
                    {
                        var info = StealInfo.Begin();
                        if(info != null)
                        {
                            info.Type = StealTypes.PickPocketCustom;
                            info.Thief = PlayerCharacter.Instance;
                            info.TargetObjRef = pickpocket;
                            info.TargetObjBase = null;
                            info.TargetObjCount = 0;
                            info.WasDetected = false;
                            info.UnspecifiedItemsWorth = 0;
                            info.OwnerForm = pickpocket.BaseForm;
                            info.State = 1;
                            FinishPickPocketCheck.Queue(3);
                        }
                    }
                });
                
                this.InstallHook("actor add item", 36525, 0, 5, "44 89 4C 24 20", ctx =>
                {
                    var pickpocket = _InventoryExtensions.GetCurrentlyPickPocketing();
                    if (pickpocket != null)
                    {
                        var who = MemoryObject.FromAddress<TESObjectREFR>(ctx.CX);
                        if (who == null || !(who is Actor) || !((Actor)who).IsPlayer)
                            return;

                        var where = MemoryObject.FromAddress<TESObjectREFR>(Memory.ReadPointer(ctx.SP + 0x28));
                        if (where == null || !where.Equals(pickpocket))
                            return;

                        var item = MemoryObject.FromAddress<TESForm>(ctx.DX);
                        var data = MemoryObject.FromAddress<BSExtraDataList>(ctx.R8);
                        int count = ctx.R9.ToInt32Safe();

                        StealInfo.PushContainerItem(item, data, count);
                    }
                    else
                    {
                        var stealing = _InventoryExtensions.GetCurrentlyStealingFromContainer();
                        if (stealing == null)
                            return;

                        var who = MemoryObject.FromAddress<TESObjectREFR>(ctx.CX);
                        if (who == null || !(who is Actor) || !((Actor)who).IsPlayer)
                            return;

                        var where = MemoryObject.FromAddress<TESObjectREFR>(Memory.ReadPointer(ctx.SP + 0x28));
                        if (where == null || !where.Equals(stealing))
                            return;

                        var item = MemoryObject.FromAddress<TESForm>(ctx.DX);
                        var data = MemoryObject.FromAddress<BSExtraDataList>(ctx.R8);
                        int count = ctx.R9.ToInt32Safe();

                        StealInfo.PushContainerItem(item, data, count);
                    }
                });

                this.InstallHook("object add item", 19282, 0, 6, "40 55 56 57 41 54", ctx =>
                {
                    var pickpocket = _InventoryExtensions.GetCurrentlyPickPocketing();
                    if (pickpocket != null)
                    {
                        var who = MemoryObject.FromAddress<TESObjectREFR>(ctx.CX);
                        if (who == null || !who.Equals(pickpocket))
                            return;

                        var where = MemoryObject.FromAddress<TESObjectREFR>(Memory.ReadPointer(ctx.SP + 0x28));
                        if (where == null || !(where is Actor) || !((Actor)where).IsPlayer)
                            return;

                        var item = MemoryObject.FromAddress<TESForm>(ctx.DX);
                        var data = MemoryObject.FromAddress<BSExtraDataList>(ctx.R8);
                        int count = ctx.R9.ToInt32Safe();

                        StealInfo.RemoveContainerItem(item, data, count);
                    }
                    else
                    {
                        var stealing = _InventoryExtensions.GetCurrentlyStealingFromContainer();
                        if (stealing == null)
                            return;

                        var who = MemoryObject.FromAddress<TESObjectREFR>(ctx.CX);
                        if (who == null || !who.Equals(stealing))
                            return;

                        var where = MemoryObject.FromAddress<TESObjectREFR>(Memory.ReadPointer(ctx.SP + 0x28));
                        if (where == null || !(where is Actor) || !((Actor)where).IsPlayer)
                            return;

                        var item = MemoryObject.FromAddress<TESForm>(ctx.DX);
                        var data = MemoryObject.FromAddress<BSExtraDataList>(ctx.R8);
                        int count = ctx.R9.ToInt32Safe();

                        StealInfo.RemoveContainerItem(item, data, count);
                    }
                });
                
                Events.OnFrame.Register(e =>
                {
                    FinishPickPocketCheck.Do();
                    ApplyStolenSoon.Do();
                    ApplyFixCDataSoon.Do();
                });

                _SetItemOwner_Func = NetScriptFramework.Main.GameInfo.GetAddressOf(15784);
                _SetListOwner_Func = NetScriptFramework.Main.GameInfo.GetAddressOf(11463);
                _GetCount_Func = NetScriptFramework.Main.GameInfo.GetAddressOf(11558);
                _GetNorth_Func = NetScriptFramework.Main.GameInfo.GetAddressOf(11773);
                _SetNorth_Func = NetScriptFramework.Main.GameInfo.GetAddressOf(11774);
                _ContOpenType_Var = NetScriptFramework.Main.GameInfo.GetAddressOf(519396);
                _ContOpen_Var = NetScriptFramework.Main.GameInfo.GetAddressOf(519421);
                _CalcCost_Func = NetScriptFramework.Main.GameInfo.GetAddressOf(15757);
            }

            if (Settings.Instance.FixBadStolenItemCount)
            {
                var ptr = NetScriptFramework.Main.GameInfo.GetAddressOf(15821, 0x110A, 0, "0F B7 95 A0 01 00 00");
                Memory.WriteBytes(ptr, new byte[] { 0x8B, 0x54, 0x24, 0x60, 0x90, 0x90, 0x90 }, true);
            }
        }

        private void InstallHook(string name, ulong vid, int offset, int length, string hex, Action<CPURegisters> func, Action<CPURegisters> after = null, bool skip = false)
        {
            var fn = NetScriptFramework.Main.GameInfo.TryGetAddressOf(vid, offset);
            if (!fn.HasValue)
                throw new InvalidOperationException(this.Name + " couldn't find '" + name + "' function in debug library! Plugin must be updated manually.");

            IntPtr addr = fn.Value;
            if (!string.IsNullOrEmpty(hex) && !Memory.VerifyBytes(addr, hex))
                throw new InvalidOperationException(this.Name + " couldn't match function '" + name + "' bytes! Plugin must be updated manually or there is a conflict with another plugin.");

            Memory.WriteHook(new HookParameters()
            {
                Address = addr,
                IncludeLength = skip ? 0 : length,
                ReplaceLength = length,
                Before = func,
                After = after
            });
        }
    }
}
