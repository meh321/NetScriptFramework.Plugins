using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomSkills
{
    /// <summary>
    /// Global value helper.
    /// </summary>
    public abstract class GValue
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GValue"/> class.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="fileName">Name of the file.</param>
        internal GValue(uint formId, string fileName)
        {
            this.FormId = formId;
            this.FileName = fileName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GValue"/> class.
        /// </summary>
        /// <param name="gb">The gb.</param>
        internal GValue(NetScriptFramework.SkyrimSE.TESGlobal gb)
        {
            this.FormId = 0;
            this.FileName = "";

            if(gb != null)
            {
                this._triedForm = true;
                this._form = gb;
            }
        }

        /// <summary>
        /// The form identifier in the mod file.
        /// </summary>
        public readonly uint FormId;

        /// <summary>
        /// The file name of mod where the value is in.
        /// </summary>
        public readonly string FileName;

        /// <summary>
        /// Gets the game form for value. This may be null if not found!
        /// </summary>
        /// <value>
        /// The form.
        /// </value>
        public NetScriptFramework.SkyrimSE.TESGlobal Form
        {
            get
            {
                if (this._triedForm)
                    return this._form;

                this._triedForm = true;
                this._form = NetScriptFramework.SkyrimSE.TESForm.LookupFormFromFile(this.FormId, this.FileName) as NetScriptFramework.SkyrimSE.TESGlobal;
                return this._form;
            }
        }

        private bool _triedForm = false;
        private NetScriptFramework.SkyrimSE.TESGlobal _form;

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            var f = this._form;
            if (f != null)
                return f.ToString();

            return this.FormId.ToString("X") + ":" + this.FileName;
        }

        /// <summary>
        /// Gets the global value and ensures the type matches.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        /// <exception cref="System.NullReferenceException">The form  + this.ToString() +  was not found! Make sure the associated plugin file is loaded and has the global value form.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">The form  + this.ToString() +  had invalid value type on global! Expected  + type.ToString() +  but was set to  + form.ValueType.ToString() +  instead.</exception>
        internal NetScriptFramework.SkyrimSE.TESGlobal EnsureGlobalWithType(NetScriptFramework.SkyrimSE.TESGlobal.GlobalValueTypes type)
        {
            var form = this.Form;
            if (form == null)
                throw new NullReferenceException("The form " + this.ToString() + " was not found! Make sure the associated plugin file is loaded and has the global value form.");

            /*if (form.ValueType != type)
                throw new ArgumentOutOfRangeException("The form " + this.ToString() + " had invalid value type on global! Expected " + type.ToString() + " but was set to " + form.ValueType.ToString() + " instead.");*/

            return form;
        }
    }

    /// <summary>
    /// Int value type.
    /// </summary>
    /// <seealso cref="CustomSkills.GValue" />
    public sealed class GValueInt : GValue
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GValueInt"/> class.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="fileName">Name of the file.</param>
        public GValueInt(uint formId, string fileName) : base(formId, fileName)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GValueInt"/> class.
        /// </summary>
        /// <param name="gb">The gb.</param>
        public GValueInt(NetScriptFramework.SkyrimSE.TESGlobal gb) : base(gb)
        {

        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public int Value
        {
            get
            {
                var form = this.EnsureGlobalWithType(NetScriptFramework.SkyrimSE.TESGlobal.GlobalValueTypes.Int32);
                var addr = form.Cast<NetScriptFramework.SkyrimSE.TESGlobal>() + 0x34;
                float fv = NetScriptFramework.Memory.ReadFloat(addr);
                return (int)Math.Round(fv);
            }
            set
            {
                var form = this.EnsureGlobalWithType(NetScriptFramework.SkyrimSE.TESGlobal.GlobalValueTypes.Int32);
                var addr = form.Cast<NetScriptFramework.SkyrimSE.TESGlobal>() + 0x34;
                NetScriptFramework.Memory.WriteFloat(addr, value);
            }
        }
    }

    /// <summary>
    /// Short value type.
    /// </summary>
    /// <seealso cref="CustomSkills.GValue" />
    public sealed class GValueShort : GValue
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GValueShort"/> class.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="fileName">Name of the file.</param>
        public GValueShort(uint formId, string fileName) : base(formId, fileName)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GValueShort"/> class.
        /// </summary>
        /// <param name="gb">The gb.</param>
        public GValueShort(NetScriptFramework.SkyrimSE.TESGlobal gb) : base(gb)
        {

        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public short Value
        {
            get
            {
                var form = this.EnsureGlobalWithType(NetScriptFramework.SkyrimSE.TESGlobal.GlobalValueTypes.Int16);
                var addr = form.Cast<NetScriptFramework.SkyrimSE.TESGlobal>() + 0x34;
                float fv = NetScriptFramework.Memory.ReadFloat(addr);
                return (short)Math.Round(fv);
            }
            set
            {
                var form = this.EnsureGlobalWithType(NetScriptFramework.SkyrimSE.TESGlobal.GlobalValueTypes.Int16);
                var addr = form.Cast<NetScriptFramework.SkyrimSE.TESGlobal>() + 0x34;
                NetScriptFramework.Memory.WriteFloat(addr, value);
            }
        }
    }

    /// <summary>
    /// Float value type.
    /// </summary>
    /// <seealso cref="CustomSkills.GValue" />
    public sealed class GValueFloat : GValue
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GValueFloat"/> class.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="fileName">Name of the file.</param>
        public GValueFloat(uint formId, string fileName) : base(formId, fileName)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GValueFloat"/> class.
        /// </summary>
        /// <param name="gb">The gb.</param>
        public GValueFloat(NetScriptFramework.SkyrimSE.TESGlobal gb) : base(gb)
        {

        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public float Value
        {
            get
            {
                var form = this.EnsureGlobalWithType(NetScriptFramework.SkyrimSE.TESGlobal.GlobalValueTypes.Float);
                var addr = form.Cast<NetScriptFramework.SkyrimSE.TESGlobal>() + 0x34;
                float fv = NetScriptFramework.Memory.ReadFloat(addr);
                return fv;
            }
            set
            {
                var form = this.EnsureGlobalWithType(NetScriptFramework.SkyrimSE.TESGlobal.GlobalValueTypes.Float);
                var addr = form.Cast<NetScriptFramework.SkyrimSE.TESGlobal>() + 0x34;
                NetScriptFramework.Memory.WriteFloat(addr, value);
            }
        }
    }
}
