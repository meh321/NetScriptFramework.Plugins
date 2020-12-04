using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework;
using NetScriptFramework.SkyrimSE;

namespace IFPV
{
    internal sealed class CameraCull
    {
        internal CameraCull(CameraMain cameraMain)
        {
            if (cameraMain == null)
                throw new ArgumentNullException("cameraMain");

            this.CameraMain = cameraMain;
        }

        internal static float UnscaleAmount
        {
            get;
            set;
        } = 0.00087f;

        internal readonly CameraMain CameraMain;

        private readonly List<Tuple<NiAVObject, int>> Disabled = new List<Tuple<NiAVObject, int>>();

        private readonly List<Tuple<NiAVObject, float>> Unscaled = new List<Tuple<NiAVObject, float>>();
        
        private readonly object Locker = new object();

        internal void RemoveDisable(NiAVObject obj)
        {
            if (obj == null)
                return;

            bool had = false;
            int reset = 0;
            lock(this.Locker)
            {
                IntPtr addr = obj.Address;
                for(int i = 0; i < this.Disabled.Count; i++)
                {
                    if(this.Disabled[i].Item1.Address == addr)
                    {
                        reset = this.Disabled[i].Item2;
                        this.Disabled.RemoveAt(i);
                        had = true;
                        break;
                    }
                }
            }

            if (!had)
                return;

            if(reset > 0)
                SetEnabled(obj, true);
            obj.DecRef();
        }

        internal void AddDisable(NiAVObject obj)
        {
            if (obj == null)
                return;

            obj.IncRef();
            int reset = 0;
            if (this.ShouldObjectBeDisabled)
            {
                reset = IsEnabled(obj) ? 1 : -1;
                if(reset > 0)
                    SetEnabled(obj, false);
            }

            lock(this.Locker)
            {
                this.Disabled.Add(new Tuple<NiAVObject, int>(obj, reset));
            }
        }

        internal void AddUnscale(NiAVObject obj)
        {
            if (obj == null)
                return;

            obj.IncRef();
            float orig = obj.LocalTransform.Scale;
            if (orig == UnscaleAmount)
                orig = 1.0f;
            if(this.ShouldObjectBeUnscaled)
                SetScale(obj, UnscaleAmount, true);

            lock(this.Locker)
            {
                this.Unscaled.Add(new Tuple<NiAVObject, float>(obj, orig));
            }
        }

        internal void Clear()
        {
            lock(this.Locker)
            {
                foreach (var s in this.Disabled)
                {
                    if(s.Item2 > 0)
                        SetEnabled(s.Item1, true);
                    s.Item1.DecRef();
                }

                this.Disabled.Clear();

                foreach(var t in this.Unscaled)
                {
                    SetScale(t.Item1, t.Item2, true);
                    t.Item1.DecRef();
                }

                this.Unscaled.Clear();
            }
        }

        private int _state_cull = 0;
        private int _state_update = 0;
        private readonly HashSet<IntPtr> _put_back = new HashSet<IntPtr>();

        private void IncCull()
        {
            if (++this._state_cull != 1)
                return;

            for(int i = 0; i < this.Disabled.Count; i++)
            {
                var s = this.Disabled[i];
                int reset = s.Item2;
                if (reset == 0)
                {
                    reset = IsEnabled(s.Item1) ? 1 : -1;
                    this.Disabled[i] = new Tuple<NiAVObject, int>(s.Item1, reset);
                }
                if (!IsEnabled(s.Item1) && reset > 0)
                {
                    SetEnabled(s.Item1, true);
                    _put_back.Add(s.Item1.Address);
                }
            }

            if (this._state_update <= 0)
            {
                foreach (var t in this.Unscaled)
                    SetScale(t.Item1, t.Item2, true);
            }
        }

        private void DecCull()
        {
            if (--this._state_cull != 0)
                return;

            foreach (var s in this.Disabled)
            {
                if(_put_back.Contains(s.Item1.Address))
                    SetEnabled(s.Item1, false);
            }

            _put_back.Clear();

            if (this._state_update <= 0)
            {
                foreach (var t in this.Unscaled)
                    SetScale(t.Item1, UnscaleAmount, true);
            }
        }

        private void IncUpdate()
        {
            if (++this._state_update != 1)
                return;

            if(this._state_cull <= 0)
            {
                foreach (var t in this.Unscaled)
                    SetScale(t.Item1, t.Item2, false);
            }
        }

        private void DecUpdate()
        {
            if (--this._state_update != 0)
                return;

            if(this._state_cull <= 0)
            {
                foreach (var t in this.Unscaled)
                    SetScale(t.Item1, UnscaleAmount, false);
            }
        }

        internal void OnShadowCulling(int index)
        {
            lock(this.Locker)
            {
                if (index == 0)
                    this.IncCull();
                else if (index == 1)
                    this.DecCull();
            }
        }

        internal void OnUpdating(int index)
        {
            lock(this.Locker)
            {
                if (index == 0)
                    this.IncUpdate();
                else if(index == 1)
                    this.DecUpdate();
            }
        }

        private bool ShouldObjectBeDisabled
        {
            get
            {
                return this._state_cull <= 0;
            }
        }

        private bool ShouldObjectBeUnscaled
        {
            get
            {
                return this._state_cull <= 0 && this._state_update <= 0;
            }
        }

        private bool IsEnabled(NiAVObject obj)
        {
            uint fl = Memory.ReadUInt32(obj.Address + 0xF4);
            bool hadEnabled = (fl & 1) == 0;
            return hadEnabled;
        }

        private void SetEnabled(NiAVObject obj, bool enabled)
        {
            if (obj.Parent == null)
                return;
            
            uint fl = Memory.ReadUInt32(obj.Address + 0xF4);
            bool hadEnabled = (fl & 1) == 0;
            if (hadEnabled == enabled)
                return;

            if (enabled)
                fl &= ~(uint)1;
            else
                fl |= 1;

            Memory.WriteUInt32(obj.Address + 0xF4, fl);
        }
        
        private void SetScale(NiAVObject obj, float scale, bool cull)
        {
            if (obj.Parent == null)
                return;
            
            obj.LocalTransform.Scale = scale;
            obj.Update(0.0f);
        }
    }
}
