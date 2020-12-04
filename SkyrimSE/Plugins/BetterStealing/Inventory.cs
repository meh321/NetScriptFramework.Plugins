using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework;
using NetScriptFramework.SkyrimSE;

namespace BetterStealing
{
    internal static class _InventoryExtensions
    {
        internal static int GetCount(this BSExtraDataList entry)
        {
            var ptr = entry.Cast<BSExtraDataList>();
            if (ptr != IntPtr.Zero)
                return Memory.InvokeCdecl(BetterStealingPlugin._GetCount_Func, ptr).ToInt32Safe();
            throw new ArgumentNullException();
        }

        internal static readonly uint StolenForeverOwnerFormId = 0x4F828;

        internal static bool HasEnchantment(this BSExtraDataList entry)
        {
            var n = entry.First;
            while(n != null)
            {
                if (n is ExtraEnchantment)
                    return true;

                n = n.Next;
            }

            return false;
        }

        internal static void SetStolenForever(this BSExtraDataList entry)
        {
            var ownerBad = TESForm.LookupFormById(StolenForeverOwnerFormId);
            if (ownerBad == null)
                return;

            var ownerPtr = ownerBad.Cast<TESForm>();
            if (ownerPtr == IntPtr.Zero)
                return;

            var ptr = entry.Cast<BSExtraDataList>();
            if (ptr != IntPtr.Zero)
            {
                Memory.InvokeCdecl(BetterStealingPlugin._SetListOwner_Func, ptr, ownerPtr);
                return;
            }
            throw new ArgumentNullException();
        }

        internal static bool IsStolenForever(this BSExtraDataList entry)
        {
            var owner = entry.GetOwner();
            return owner != null && owner.FormId == StolenForeverOwnerFormId;
        }

        internal static void SetOwner(this BSExtraDataList entry, TESForm owner)
        {
            var ownerptr = owner != null ? owner.Cast<TESForm>() : IntPtr.Zero;
            if (owner != null && ownerptr == IntPtr.Zero)
                throw new ArgumentOutOfRangeException();
            var ptr = entry.Cast<BSExtraDataList>();
            if (ptr != IntPtr.Zero)
            {
                Memory.InvokeCdecl(BetterStealingPlugin._SetListOwner_Func, ptr, ownerptr);
                return;
            }
            throw new ArgumentNullException();
        }

        internal static void SetCustomData(this BSExtraDataList entry, uint data)
        {
            var ptr = entry.Cast<BSExtraDataList>();
            if (ptr != IntPtr.Zero)
            {
                var bytes = BitConverter.GetBytes(data);
                float arg = BitConverter.ToSingle(bytes, 0);
                Memory.InvokeCdecl(BetterStealingPlugin._SetNorth_Func, ptr, arg);
                return;
            }
            throw new ArgumentNullException();
        }

        internal static uint GetCustomData(this BSExtraDataList entry)
        {
            var ptr = entry.Cast<BSExtraDataList>();
            if (ptr != IntPtr.Zero)
            {
                float arg = Memory.InvokeCdeclF(BetterStealingPlugin._GetNorth_Func, ptr);
                if (arg == 0.0f)
                    return 0;
                var bytes = BitConverter.GetBytes(arg);
                return BitConverter.ToUInt32(bytes, 0);
            }
            throw new ArgumentNullException();
        }

        internal delegate void ForeachItemDelegate(BSExtraDataList entry, int count);

        internal static void ForeachEntry(this ExtraContainerChanges.ItemEntry item, ForeachItemDelegate func)
        {
            if (item == null || func == null)
                return;

            int total = item.Count;
            int left = total;
            var ls = item.ExtraData;

            if(ls != null)
            {
                foreach(var x in ls)
                {
                    if (x != null)
                    {
                        int c = x.GetCount();
                        if (c <= 0)
                            continue;

                        left -= c;
                        func(x, c);
                    }
                }
            }

            if (left > 0)
                func(null, left);
        }

        internal static bool IsAnyContainerOpened()
        {
            return Memory.ReadUInt32(BetterStealingPlugin._ContOpen_Var) != 0;
        }
        
        internal static TESObjectREFR GetCurrentlyStealingFromContainer()
        {
            int type = Memory.ReadInt32(BetterStealingPlugin._ContOpenType_Var);
            if (type != 1)
                return null;

            uint handle = Memory.ReadUInt32(BetterStealingPlugin._ContOpen_Var);
            if (handle == 0)
                return null;

            TESObjectREFR obj = null;
            using (var objRef = new ObjectRefHolder(handle))
            {
                obj = objRef.Object;
            }

            return obj;
        }

        internal static Actor GetCurrentlyPickPocketing()
        {
            int type = Memory.ReadInt32(BetterStealingPlugin._ContOpenType_Var);
            if (type != 2)
                return null;

            uint handle = Memory.ReadUInt32(BetterStealingPlugin._ContOpen_Var);
            if (handle == 0)
                return null;

            TESObjectREFR obj = null;
            using (var objRef = new ObjectRefHolder(handle))
            {
                obj = objRef.Object;
            }

            return obj as Actor;
        }
    }
}
