using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework;
using NetScriptFramework.SkyrimSE;

namespace BetterStealing
{
    internal sealed class StealInfo
    {
        private StealInfo()
        {

        }

        #region Members

        internal Actor Thief = null;
        internal TESObjectREFR TargetObjRef = null;
        internal TESForm TargetObjBase = null;
        internal TESForm OwnerForm = null;
        internal int TargetObjCount = 0;
        internal int UnspecifiedItemsWorth = 0;
        internal StealTypes Type = StealTypes.Unknown;
        internal bool WasDetected = false;
        internal int State = 0;

        #endregion

        #region Methods

        private void Process()
        {
            if (this.Thief == null || !this.Thief.IsPlayer)
                return;
            
            switch(this.Type)
            {
                case StealTypes.Take:
                    {
                        if (this.TargetObjBase != null && this.TargetObjCount > 0 && this.OwnerForm != null)
                            this.ProcessInventory(this.Thief.Inventory, this.TargetObjRef, this.TargetObjBase, this.OwnerForm, this.TargetObjCount);
                        return;
                    }

                case StealTypes.Harvest:
                    {
                        if (this.TargetObjBase != null && this.TargetObjCount > 0 && this.OwnerForm != null)
                            this.ProcessInventory(this.Thief.Inventory, null, this.TargetObjBase, this.OwnerForm, this.TargetObjCount);
                        return;
                    }

                case StealTypes.Container:
                    {
                        var items = ProcessContainer();
                        if(items != null)
                        {
                            var inv = this.Thief.Inventory;
                            foreach (var x in items)
                                this.ProcessInventory(inv, null, x.BaseForm, x.Owner, x.Count, x.CData);
                        }
                        FixAllCustomDataItems(this.Thief.Inventory);
                        return;
                    }

                case StealTypes.PickPocketCustom:
                    {
                        var items = ProcessContainer();
                        if (items != null)
                        {
                            var inv = this.Thief.Inventory;
                            foreach (var x in items)
                                this.ProcessInventory(inv, null, x.BaseForm, x.Owner, x.Count, x.CData);
                        }
                        FixAllCustomDataItems(this.Thief.Inventory);
                        return;
                    }

                case StealTypes.Mount:
                    {
                        // Unhandled for now but target ref is the actor and obj base is the actor base form.
                        return;
                    }
            }
        }

        internal static void FixAllCustomDataItems(ExtraContainerChanges.Data inventory)
        {
            if(inventory != null)
            {
                var objects = inventory.Objects;
                if(objects != null)
                {
                    foreach(var o in objects)
                    {
                        var els = o.ExtraData;
                        if(els != null)
                        {
                            foreach(var edata in els)
                            {
                                uint ex = 0;
                                if (edata != null && (ex = edata.GetCustomData()) != 0 && (ex & 0x80000000) == 0)
                                    edata.SetCustomData(0);
                            }
                        }
                    }
                }
            }
        }

        private bool ProcessInventory(ExtraContainerChanges.Data inventory, TESObjectREFR instance, TESForm item, TESForm owner, int count, uint? cdata = null)
        {
            if (inventory == null || item == null)
                return false;

            // Special case for references, it might already be handled before. Containers have this check elsewhere.
            if (instance != null && instance.ExtraDataList.IsStolenForever())
                return false;
            
            ExtraContainerChanges.ItemEntry entry = null;
            BSExtraDataList best = null;
            int bestCount = 0;

            foreach(var o in inventory.Objects)
            {
                var template = o.Template;
                if (template == null)
                    continue;
                
                if (!item.Equals(template))
                    continue;

                entry = o;

                o.ForeachEntry((data, icount) =>
                {
                    // Already not stolen.
                    var iowner = data != null ? data.GetOwner() : null;
                    if (iowner == null)
                        return;

                    if (cdata.HasValue && cdata.Value != data.GetCustomData())
                        return;

                    if (data.IsStolenForever())
                        return;
                    
                    if (!CheckConditions(o, data, item))
                        return;
                    
                    if(best == null || Math.Abs(count - bestCount) > Math.Abs(count - icount))
                    {
                        best = data;
                        bestCount = icount;
                    }
                });

                break;
            }

            if (best == null)
                return false;

            this.ApplyResult(entry, best, this.WasDetected);
            return true;
        }

        private void ApplyResult(ExtraContainerChanges.ItemEntry entry, BSExtraDataList data, bool detected)
        {
            if (!detected)
            {
                data.SetOwner(null);
            }
            else
            {
                data.SetStolenForever();
            }
        }
        
        internal static bool CheckConditions(ExtraContainerChanges.ItemEntry entry, BSExtraDataList data, TESForm item)
        {
            // Game has a hardcoded check to ignore gold when it comes to ownership.
            if (item.FormId == 0xF)
                return false;

            {
                var excludeForms = BetterStealingPlugin.ExcludeForms;
                if (excludeForms != null && excludeForms.Contains(item.FormId))
                    return false;
            }

            if(Settings.Instance.ExcludeEnchantedItems)
            {
                if (data != null && data.HasEnchantment())
                    return false;
            }

            var ls = BetterStealingPlugin.NeverKeywords;
            if (ls.Count != 0)
            {
                foreach (var x in ls)
                {
                    if (item.HasKeywordText(x))
                        return false;
                }
            }

            ls = BetterStealingPlugin.AlwaysKeywords;
            if (ls.Count != 0)
            {
                foreach (var x in ls)
                {
                    if (item.HasKeywordText(x))
                        return true;
                }
            }

            ls = BetterStealingPlugin.RequiredKeywords;
            if(ls.Count != 0)
            {
                bool has = false;
                foreach(var x in ls)
                {
                    if(item.HasKeywordText(x))
                    {
                        has = true;
                        break;
                    }
                }

                if (!has)
                    return false;
            }

            int maxPrice = BetterStealingPlugin.MaxPrice;
            if(maxPrice > 0)
            {
                int cost;
                if (entry != null)
                    cost = Memory.InvokeCdecl(BetterStealingPlugin._CalcCost_Func, entry.Cast<ExtraContainerChanges.ItemEntry>()).ToInt32Safe();
                else
                    cost = item.GoldValue;

                if (cost > maxPrice)
                    return false;
            }

            // Price, keyword, ID, type, etc.

            return true;
        }

        #endregion

        #region Static members

        private static readonly object Locker = new object();
        private static readonly Dictionary<int, List<StealInfo>> Map = new Dictionary<int, List<StealInfo>>();
        private static List<queued_item> QueuedContainer = null;

        internal sealed class queued_item
        {
            internal TESForm BaseForm;
            internal int Count;
            internal TESForm Owner;
            internal uint CData;
        }

        internal static void BeginContainer()
        {
            lock(Locker)
            {
                QueuedContainer = new List<queued_item>();
            }
        }

        internal static void EndContainer()
        {
            lock(Locker)
            {
                QueuedContainer = null;
            }
        }

        private static List<queued_item> ProcessContainer()
        {
            List<queued_item> rs = null;
            lock(Locker)
            {
                rs = QueuedContainer;
                QueuedContainer = null;
            }

            return rs;
        }

        private static int _cdata_counter = 0;

        internal static void PushContainerItem(TESForm baseForm, BSExtraDataList data, int count)
        {
            if (baseForm == null || count <= 0)
                return;

            if (data == null)
                return;

            var owner = data.GetOwner();
            uint cdata = data.GetCustomData();

            if (owner == null || (cdata & 0x80000000) != 0)
                return;

            if (owner.FormId == _InventoryExtensions.StolenForeverOwnerFormId)
                return;

            int ndata = ++_cdata_counter;
            if (_cdata_counter >= 0x40000000)
                _cdata_counter = 0;

            data.SetCustomData((uint)ndata);

            var ci = new queued_item();
            ci.BaseForm = baseForm;
            ci.Count = count;
            ci.Owner = owner;
            ci.CData = (uint)ndata;

            lock(Locker)
            {
                if (QueuedContainer != null)
                    QueuedContainer.Add(ci);
            }
        }

        internal static void RemoveContainerItem(TESForm baseForm, BSExtraDataList data, int count)
        {
            if (baseForm == null || count <= 0)
                return;

            if (data == null)
                return;

            var owner = data.GetOwner();
            uint cdata = data.GetCustomData();

            if (owner == null || (cdata & 0x80000000) != 0)
                return;

            if (owner.FormId == _InventoryExtensions.StolenForeverOwnerFormId)
                return;

            if(cdata != 0)
                data.SetCustomData(0);

            lock (Locker)
            {
                if (QueuedContainer == null)
                    return;

                for (int i = QueuedContainer.Count - 1; i >= 0 && count > 0; i--)
                {
                    var ci = QueuedContainer[i];

                    if (!ci.BaseForm.Equals(baseForm))
                        continue;

                    if (ci.CData != cdata)
                        continue;

                    if (!ci.Owner.Equals(owner))
                        continue;

                    if (count >= ci.Count)
                    {
                        QueuedContainer.RemoveAt(i);
                        count -= ci.Count;
                    }
                    else
                    {
                        ci.Count -= count;
                        count = 0;
                    }
                }
            }
        }

        internal static StealInfo Begin()
        {
            int t = Memory.GetCurrentNativeThreadId();
            StealInfo info = null;
            lock(Locker)
            {
                List<StealInfo> ls = null;
                if(!Map.TryGetValue(t, out ls))
                {
                    ls = new List<StealInfo>(1);
                    Map[t] = ls;
                }

                if (ls.Count != 0)
                    info = ls[ls.Count - 1];
                if (info == null || info.State != 0)
                {
                    info = new StealInfo();
                    ls.Add(info);
                }
            }
            return info;
        }
        
        internal static void ProcessFinished()
        {
            List<StealInfo> todo = null;
            lock(Locker)
            {
                if (Map.Count == 0)
                    return;

                todo = new List<StealInfo>(1);
                var all = Map.ToList();
                foreach(var pair in all)
                {
                    var ls = pair.Value;
                    for(int i = ls.Count - 1; i >= 0; i--)
                    {
                        var info = ls[i];
                        if (info.State == 0)
                            continue;

                        ls.RemoveAt(i);
                        if (info.State > 0)
                            todo.Add(info);
                    }

                    if (ls.Count == 0)
                        Map.Remove(pair.Key);
                }
            }

            if(todo != null)
            {
                foreach (var info in todo)
                    info.Process();
            }
        }

        internal static StealInfo Get()
        {
            int t = Memory.GetCurrentNativeThreadId();
            StealInfo info = null;
            lock(Locker)
            {
                List<StealInfo> ls = null;
                if(Map.TryGetValue(t, out ls) && ls.Count != 0)
                {
                    info = ls[ls.Count - 1];
                    if (info.State != 0)
                        info = null;
                }
            }
            return info;
        }

        internal static StealTypes GetCalledFromType(IntPtr addr)
        {
            ulong vid = 0;
            lock(CFLocker)
            {
                if(!CFMap.TryGetValue(addr, out vid))
                {
                    var fn = NetScriptFramework.Main.GameInfo.GetFunctionInfo(addr, true);
                    if (fn != null)
                        vid = fn.Id;
                    CFMap[addr] = vid;
                }
            }

            switch(vid)
            {
                case 50196: return StealTypes.Container;
                case 55690: return StealTypes.PapyrusScript;
                case 39456: return StealTypes.Take;
                case 39280: return StealTypes.Mount;
                case 36521: return StealTypes.Take;
                case 22198: return StealTypes.GameScript;
                case 17485: return StealTypes.PickLock;
                case 14692: return StealTypes.Harvest;
            }

            return StealTypes.Unknown;
        }

        private static readonly object CFLocker = new object();
        private static readonly Dictionary<IntPtr, ulong> CFMap = new Dictionary<IntPtr, ulong>();

        #endregion
    }

    internal enum StealTypes : int
    {
        Unknown,

        Mount,
        Container,
        Pickpocket,
        Take,
        Harvest,
        PapyrusScript,
        GameScript,
        PickLock,
        PickPocketCustom,
    }
}
