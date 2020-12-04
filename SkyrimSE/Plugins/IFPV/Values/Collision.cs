using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IFPV.Values;

namespace IFPV.Values
{
    internal sealed class CollisionEnabled : CameraValueSimple
    {
        internal CollisionEnabled() : base(null, 1.0, 1.0)
        {
            this.Flags |= CameraValueFlags.NoTween;
        }
    }
}

namespace IFPV
{
    partial class CameraValueMap
    {
        internal readonly CollisionEnabled CollisionEnabled = new CollisionEnabled();
    }
}
