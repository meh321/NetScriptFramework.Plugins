using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework;
using NetScriptFramework.SkyrimSE;

namespace GrassControl
{
    // not actually cached anything here at the moment
    internal sealed class RaycastHelper
    {
        internal RaycastHelper(int version, float rayHeight, float rayDepth, string layers, CachedFormList ignored)
        {
            this.Version = version;
            this.RayHeight = rayHeight;
            this.RayDepth = rayDepth;
            this.Ignore = ignored;
            
            var spl = (layers ?? "").Split(new[] { ' ', ',', '\t', '+' }, StringSplitOptions.RemoveEmptyEntries);
            ulong mask = 0;
            foreach(var x in spl)
            {
                int y;
                if (int.TryParse(x, System.Globalization.NumberStyles.None, null, out y) && y >= 0 && y < 64)
                    mask |= (ulong)1 << y;
            }
            this.RaycastMask = mask;
        }

        internal readonly int Version;
        
        internal readonly float RayHeight;

        internal readonly float RayDepth;

        internal readonly ulong RaycastMask;

        internal readonly CachedFormList Ignore;
        
        internal bool CanPlaceGrass(TESObjectCELL cell, TESObjectLAND land, float x, float y, float z)
        {
            if (cell == null)
                return true;

            // Currently not dealing with this.
            if (cell.IsInterior || !cell.IsAttached)
                return true;

            var rp = new RayCastParameters();
            rp.Begin = new float[] { x, y, z + this.RayHeight };
            rp.End = new float[] { x, y, z - this.RayDepth };
            rp.Cell = cell;
            var rs = TESObjectCELL.RayCast(rp);
            foreach (var r in rs)
            {
                if (r.Fraction >= 1.0f || r.HavokObject == IntPtr.Zero)
                    continue;

                uint flags = Memory.ReadUInt32(r.HavokObject + 0x2C) & 0x7F;
                ulong mask = (ulong)1 << (int)flags;
                if ((this.RaycastMask & mask) == 0)
                    continue;

                if (this.Ignore != null && this.IsIgnoredObject(r))
                    continue;
                
                return false;
            }

            return true;
        }

        private bool IsIgnoredObject(RayCastResult r)
        {
            bool result = false;
            try
            {
                var o = r.Object;
                int tries = 0;
                while(o != null && tries++ < 10)
                {
                    var obj = o.OwnerObject;
                    if(obj != null)
                    {
                        /*if (this.Ignore.Contains(obj.FormId))
                            result = true;
                        else*/
                        {
                            var baseForm = obj.BaseForm;
                            if (baseForm != null)
                            {
                                if (this.Ignore.Contains(baseForm.FormId))
                                    result = true;
                            }
                        }
                        break;
                    }

                    o = o.Parent;
                }
            }
            catch
            {

            }

            return result;
        }
    }
}
