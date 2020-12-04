using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework;
using NetScriptFramework.SkyrimSE;

namespace IFPV
{
    internal sealed class CameraHideHelper
    {
        internal CameraHideHelper(CameraMain cameraMain)
        {
            if (cameraMain == null)
                throw new ArgumentNullException("cameraMain");

            this.CameraMain = cameraMain;

            // 30 = head
            // 31 = hair
            // 32 = body
            // 42 = circlet
            // 43 = ears
            // 130 = head
            // 131 = hair
            // 142 = circlet
            // 143 = ears
            // 230 = head?

            if (Settings.Instance.HideHelmet)
                this.IsHelmetBipedMask = GenerateBipedMask(30, 31, 42, 43, 130, 131, 142, 143, 230);
            else if (Settings.Instance.HideHead)
                this.IsHelmetBipedMask = GenerateBipedMask(43);
            else
                this.IsHelmetBipedMask = new biped_mask();
            this.NotHelmetBipedMask = GenerateBipedMask(32);
        }

        internal readonly CameraMain CameraMain;

        private uint LastRaceId = 0;
        private uint LastFormId = 0;
        private IntPtr LastSkeleton = IntPtr.Zero;
        private IntPtr LastAddress = IntPtr.Zero;
        private HideFlags LastFlags = HideFlags.None;
        private NiAVObject FirstPersonSkeleton = null;
        private readonly List<NiAVObject> LastHelmet = new List<NiAVObject>();
        private readonly biped_mask IsHelmetBipedMask;
        private readonly biped_mask NotHelmetBipedMask;

        private sealed class biped_mask
        {
            internal biped_mask()
            {

            }

            private readonly ulong[] mask = new ulong[4];

            internal bool this[int index]
            {
                get
                {
                    if (index < 0 || index >= 256)
                        return false;

                    int i = index / 64;
                    int j = index % 64;
                    ulong fl = (ulong)1 << j;
                    return (this.mask[i] & fl) != 0;
                }
                set
                {
                    if (index < 0 || index >= 256)
                        return;

                    int i = index / 64;
                    int j = index % 64;
                    ulong fl = (ulong)1 << j;
                    if (value)
                        this.mask[i] |= fl;
                    else
                        this.mask[i] &= ~fl;
                }
            }
        }
        
        private static biped_mask GenerateBipedMask(params int[] slots)
        {
            var mask = new biped_mask();
            foreach (var s in slots)
                mask[s] = true;
            return mask;
        }

        [Flags]
        private enum HideFlags : uint
        {
            None = 0,

            Head = 1,
            Helmet = 2,
            Arms = 4,
            Show1st = 8,
            Has1st = 0x10,
            Head2 = 0x20,

            NeedUpdate = Head | Helmet | Arms | Head2,
        }

        internal void Update(CameraUpdate update)
        {
            var actor = update.Target.Actor;
            if (actor == null || update.Target.RootNode == null)
            {
                this.Clear(update);
                return;
            }

            {
                IntPtr addr = actor.Address;
                uint formId = actor.FormId;
                var race = actor.Race;
                uint raceId = race != null ? race.FormId : 0;
                var node = update.Target.RootNode;
                IntPtr addr2 = node != null ? node.Address : IntPtr.Zero;

                if (addr != this.LastAddress || formId != this.LastFormId || raceId != this.LastRaceId || this.LastSkeleton != addr2)
                {
                    this.Clear(update);

                    this.LastAddress = addr;
                    this.LastSkeleton = addr2;
                    this.LastFormId = formId;
                    this.LastRaceId = raceId;
                }
            }

            HideFlags wantFlags = HideFlags.None;
            if (update.Values.HideHead.CurrentValue >= 0.5)
                wantFlags |= HideFlags.Head | HideFlags.Helmet;
            if (update.Values.HideHead2.CurrentValue >= 0.5)
                wantFlags |= HideFlags.Head2;
            if (update.Values.HideArms.CurrentValue >= 0.5)
                wantFlags |= HideFlags.Arms;
            if (update.Values.Show1stPersonArms.CurrentValue >= 0.5)
                wantFlags |= HideFlags.Show1st;
            switch(update.GameCameraState.Id)
            {
                case TESCameraStates.FirstPerson:
                case TESCameraStates.Free:
                    break;

                default:
                    wantFlags |= HideFlags.Has1st;
                    break;
            }

            if (wantFlags == this.LastFlags)
            {
                if ((wantFlags & HideFlags.Helmet) != HideFlags.None)
                    this.UpdateHelmet(update.Target.RootNode);
                return;
            }

            if ((wantFlags & HideFlags.NeedUpdate) != (this.LastFlags & HideFlags.NeedUpdate))
            {
                this.CameraMain.Cull.Clear();
                this.ClearHelmet();
                this.LastFlags &= ~HideFlags.NeedUpdate;
                this.UpdateHideWithCull(actor, wantFlags, update.Target.RootNode as NiNode);
            }

            if ((wantFlags & HideFlags.Has1st) != (this.LastFlags & HideFlags.Has1st))
            {
                if ((wantFlags & HideFlags.Has1st) != HideFlags.None)
                {
                    var skeleton = actor.GetSkeletonNode(true);
                    if (skeleton != null)
                    {
                        this.SetFirstPersonSkeleton(skeleton, update.GameCameraState.Id == TESCameraStates.FirstPerson, (wantFlags & HideFlags.Show1st) != HideFlags.None);
                        this.LastFlags |= HideFlags.Has1st;
                        if ((wantFlags & HideFlags.Show1st) != HideFlags.None)
                            this.LastFlags |= HideFlags.Show1st;
                    }
                }
                else
                {
                    this.LastFlags &= ~(HideFlags.Has1st | HideFlags.Show1st);
                    this.SetFirstPersonSkeleton(null, update.GameCameraState.Id == TESCameraStates.FirstPerson, false);
                }
            }
        }

        internal void UpdateFirstPersonSkeletonRotation(CameraUpdate update)
        {
            if (this.FirstPersonSkeleton == null)
                return;

            var transform = this.FirstPersonSkeleton.LocalTransform;
            var rot = transform.Rotation;

            var result = update.Result.Transform.Rotation;
            rot.CopyFrom(result);
            
            double ymult = update.Values.FirstPersonSkeletonRotateYMultiplier.CurrentValue;
            if (ymult != 1.0)
            {
                var temp = this.CameraMain.TempResult.Transform.Rotation;
                temp.Identity(1.0f);

                float y = (float)(update.Values.InputRotationY.CurrentValue * update.Values.InputRotationYMultiplier.CurrentValue * (ymult - 1.0));
                if (y != 0.0f)
                    temp.RotateX(y, temp);

                rot.Multiply(temp, rot);
            }
            
            this.FirstPersonSkeleton.Update(0.0f);
        }

        internal void Clear(CameraUpdate update)
        {
            if (this.LastAddress == IntPtr.Zero)
                return;

            this.LastAddress = IntPtr.Zero;
            this.LastSkeleton = IntPtr.Zero;
            this.LastFormId = 0;
            this.LastFlags = HideFlags.None;
            this.LastRaceId = 0;

            this.CameraMain.Cull.Clear();
            this.SetFirstPersonSkeleton(null, update.GameCameraState.Id == TESCameraStates.FirstPerson, false);
            this.ClearHelmet();
        }

        private void UpdateHideWithCull(Actor actor, HideFlags want, NiNode root)
        {
            if (root == null)
                return;

            if ((want & HideFlags.Head) != HideFlags.None)
            {
                var node = root.LookupNodeByName("BSFaceGenNiNodeSkinned");
                if (node != null)
                {
                    this.LastFlags |= HideFlags.Head;
                    this.CameraMain.Cull.AddDisable(node);
                }
            }
            
            if((want & HideFlags.Head2) != HideFlags.None)
            {
                var node = root.LookupNodeByName("WereWolfLowHead01");
                if (node != null)
                {
                    this.LastFlags |= HideFlags.Head2;
                    this.CameraMain.Cull.AddDisable(node);

                    node = root.LookupNodeByName("WereWolfTeeth");
                    if (node != null)
                        this.CameraMain.Cull.AddDisable(node);

                    node = root.LookupNodeByName("EyesMaleWerewolfBeast");
                    if (node != null)
                        this.CameraMain.Cull.AddDisable(node);
                }
                else
                {
                    node = root.LookupNodeByName("NPC Head [Head]");
                    if (node != null)
                    {
                        this.LastFlags |= HideFlags.Head2;
                        this.CameraMain.Cull.AddUnscale(node);
                    }
                }
            }

            if ((want & HideFlags.Arms) != HideFlags.None)
            {
                var left = root.LookupNodeByName("NPC L UpperArm [LUar]");
                var right = root.LookupNodeByName("NPC R UpperArm [RUar]");
                if (left != null && right != null)
                {
                    this.LastFlags |= HideFlags.Arms;
                    this.CameraMain.Cull.AddUnscale(left);
                    this.CameraMain.Cull.AddUnscale(right);
                }
            }

            if ((want & HideFlags.Helmet) != HideFlags.None)
                this.InitializeHelmet(root, null);
        }

        private void SetFirstPersonSkeleton(NiAVObject node, bool is1stPersonCam, bool shouldShow)
        {
            if (this.FirstPersonSkeleton != null)
                this.FirstPersonSkeleton.DecRef();

            this.FirstPersonSkeleton = node;

            if (this.FirstPersonSkeleton != null)
                this.FirstPersonSkeleton.IncRef();
        }
        
        private void UpdateHelmet(NiAVObject root)
        {
            if (root == null)
                return;

            var rootNode = root as NiNode;
            if (rootNode == null)
                return;

            var ls = this.GetHelmetNodes(rootNode);
            bool changed = ls.Count != this.LastHelmet.Count;
            if (!changed)
            {
                for (int i = 0; i < ls.Count; i++)
                {
                    if (!ls[i].Equals(this.LastHelmet[i]))
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (changed)
            {
                this.ClearHelmet();
                this.InitializeHelmet(root, ls);
            }
        }

        private void ClearHelmet()
        {
            this.LastFlags &= ~HideFlags.Helmet;

            foreach (var x in this.LastHelmet)
                this.CameraMain.Cull.RemoveDisable(x);
            this.LastHelmet.Clear();
        }

        private void InitializeHelmet(NiAVObject root, List<NiAVObject> calculated)
        {
            this.LastFlags |= HideFlags.Helmet;

            var rootNode = root as NiNode;
            if (rootNode == null)
                return;

            var ls = calculated ?? this.GetHelmetNodes(rootNode);
            foreach (var x in ls)
            {
                this.CameraMain.Cull.AddDisable(x);
                this.LastHelmet.Add(x);
            }
        }

        private void FillValidBipedObjects(NiNode root, List<NiAVObject> ls)
        {
            this.ExploreEquipment(root, root, ls);
        }
        
        private void ExploreEquipment(NiNode root, NiAVObject current, List<NiAVObject> ls)
        {
            // Special case, we should ignore and stop looking here, head is handled elsewhere.
            if(current != null)
            {
                string nm = current.Name.Text ?? "";
                if (nm.Equals("BSFaceGenNiNodeSkinned", StringComparison.OrdinalIgnoreCase))
                    return;
            }

            if (current is BSGeometry)
            {
                var g = (BSGeometry)current;
                var skin = g.Skin.Value;
                if(skin != null && skin is BSDismemberSkinInstance)
                {
                    int count = Memory.ReadInt32(skin.Address + 0x88);
                    if(count > 0)
                    {
                        var pbuf = Memory.ReadPointer(skin.Address + 0x90);
                        if(pbuf != IntPtr.Zero)
                        {
                            bool isHelm = false;
                            for(int i = 0; i < count; i++)
                            {
                                int m = (int)Memory.ReadUInt16(pbuf + (4 * i + 2));
                                if(this.NotHelmetBipedMask[m])
                                {
                                    isHelm = false;
                                    break;
                                }
                                else if (this.IsHelmetBipedMask[m])
                                    isHelm = true;
                            }

                            if (isHelm)
                                ls.Add(current);
                        }
                    }
                }
            }

            if(current is NiNode)
            {
                var n = current as NiNode;
                foreach (var ch in n.Children)
                    this.ExploreEquipment(root, ch, ls);
            }
        }

        private List<NiAVObject> GetHelmetNodes(NiNode root)
        {
            var ls = new List<NiAVObject>();

            if (root != null)
                this.FillValidBipedObjects(root, ls);

            return ls;
        }
    }
}
