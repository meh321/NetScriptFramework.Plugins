using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamePlayTweaks
{
    /// <summary>
    /// This class handles finding, setting up, configuring and applying all the stuff in this plugin.
    /// </summary>
    internal static class ModHandler
    {
        /// <summary>
        /// The initialized state.
        /// </summary>
        private static int is_init = 0;

        /// <summary>
        /// Gets or sets a value indicating whether we had main menu reached at any point.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [had main menu]; otherwise, <c>false</c>.
        /// </value>
        internal static bool HadMainMenu
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes the fixes and applies them.
        /// </summary>
        /// <param name="parent">The parent plugin.</param>
        /// <param name="configKeyword">The configuration file keyword.</param>
        /// <exception cref="System.ArgumentException">Warning: parent.Name fix t.Name does not have valid parameterless constructor!</exception>
        internal static void init(NetScriptFramework.Plugin parent, string configKeyword)
        {
            if (System.Threading.Interlocked.Exchange(ref is_init, 1) != 0)
                throw new InvalidOperationException();

            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            var types = asm.GetTypes();
            foreach (var t in types)
            {
                if (t.IsAbstract || !t.IsSubclassOf(typeof(Mod)))
                    continue;

                var cis = t.GetConstructors().Where(q => q.GetParameters().Length == 0).ToList();
                if (cis.Count != 1)
                    throw new ArgumentException("Warning: " + parent.Name + " mod " + t.Name + " does not have valid parameterless constructor!");

                Mod f = (Mod)cis[0].Invoke(new object[0]);
                mods.Add(f);
            }

            var config = new NetScriptFramework.Tools.ConfigFile(configKeyword);
            foreach (var f in mods)
            {
                config.AddSetting(f.Key + ".Enabled", new NetScriptFramework.Tools.Value(f.Enabled), null, f.Description);
                foreach (var okey in f.OrderOfParams)
                {
                    long val_i;
                    double val_f;
                    string val_s;
                    bool val_b;

                    if(f.IntParameters.TryGetValue(okey, out val_i))
                    {
                        string cmt = null;
                        f.ParameterComments.TryGetValue(okey, out cmt);
                        NetScriptFramework.Tools.ConfigEntryFlags fl = !string.IsNullOrEmpty(cmt) ? NetScriptFramework.Tools.ConfigEntryFlags.VeryShortComment : NetScriptFramework.Tools.ConfigEntryFlags.NoNewLineBefore;
                        config.AddSetting(f.Key + "." + okey, new NetScriptFramework.Tools.Value(val_i), null, cmt, fl);
                    }
                    if(f.FloatParameters.TryGetValue(okey, out val_f))
                    {
                        string cmt = null;
                        f.ParameterComments.TryGetValue(okey, out cmt);
                        NetScriptFramework.Tools.ConfigEntryFlags fl = !string.IsNullOrEmpty(cmt) ? NetScriptFramework.Tools.ConfigEntryFlags.VeryShortComment : NetScriptFramework.Tools.ConfigEntryFlags.NoNewLineBefore;
                        config.AddSetting(f.Key + "." + okey, new NetScriptFramework.Tools.Value(val_f), null, cmt, fl);
                    }
                    if(f.StringParameters.TryGetValue(okey, out val_s))
                    {
                        string cmt = null;
                        f.ParameterComments.TryGetValue(okey, out cmt);
                        NetScriptFramework.Tools.ConfigEntryFlags fl = !string.IsNullOrEmpty(cmt) ? NetScriptFramework.Tools.ConfigEntryFlags.VeryShortComment : NetScriptFramework.Tools.ConfigEntryFlags.NoNewLineBefore;
                        config.AddSetting(f.Key + "." + okey, new NetScriptFramework.Tools.Value(val_s), null, cmt, fl);
                    }
                    if(f.BoolParameters.TryGetValue(okey, out val_b))
                    {
                        string cmt = null;
                        f.ParameterComments.TryGetValue(okey, out cmt);
                        NetScriptFramework.Tools.ConfigEntryFlags fl = !string.IsNullOrEmpty(cmt) ? NetScriptFramework.Tools.ConfigEntryFlags.VeryShortComment : NetScriptFramework.Tools.ConfigEntryFlags.NoNewLineBefore;
                        config.AddSetting(f.Key + "." + okey, new NetScriptFramework.Tools.Value(val_b), null, cmt, fl);
                    }
                }
                foreach (var pair in f.IntParameters)
                {
                    if (f.OrderOfParams.Contains(pair.Key))
                        continue;
                    string cmt = null;
                    f.ParameterComments.TryGetValue(pair.Key, out cmt);
                    NetScriptFramework.Tools.ConfigEntryFlags fl = !string.IsNullOrEmpty(cmt) ? NetScriptFramework.Tools.ConfigEntryFlags.VeryShortComment : NetScriptFramework.Tools.ConfigEntryFlags.NoNewLineBefore;
                    config.AddSetting(f.Key + "." + pair.Key, new NetScriptFramework.Tools.Value(pair.Value), null, cmt, fl);
                }
                foreach (var pair in f.FloatParameters)
                {
                    if (f.OrderOfParams.Contains(pair.Key))
                        continue;
                    string cmt = null;
                    f.ParameterComments.TryGetValue(pair.Key, out cmt);
                    NetScriptFramework.Tools.ConfigEntryFlags fl = !string.IsNullOrEmpty(cmt) ? NetScriptFramework.Tools.ConfigEntryFlags.VeryShortComment : NetScriptFramework.Tools.ConfigEntryFlags.NoNewLineBefore;
                    config.AddSetting(f.Key + "." + pair.Key, new NetScriptFramework.Tools.Value(pair.Value), null, cmt, fl);
                }
                foreach (var pair in f.StringParameters)
                {
                    if (f.OrderOfParams.Contains(pair.Key))
                        continue;
                    string cmt = null;
                    f.ParameterComments.TryGetValue(pair.Key, out cmt);
                    NetScriptFramework.Tools.ConfigEntryFlags fl = !string.IsNullOrEmpty(cmt) ? NetScriptFramework.Tools.ConfigEntryFlags.VeryShortComment : NetScriptFramework.Tools.ConfigEntryFlags.NoNewLineBefore;
                    config.AddSetting(f.Key + "." + pair.Key, new NetScriptFramework.Tools.Value(pair.Value), null, cmt, fl);
                }
                foreach (var pair in f.BoolParameters)
                {
                    if (f.OrderOfParams.Contains(pair.Key))
                        continue;
                    string cmt = null;
                    f.ParameterComments.TryGetValue(pair.Key, out cmt);
                    NetScriptFramework.Tools.ConfigEntryFlags fl = !string.IsNullOrEmpty(cmt) ? NetScriptFramework.Tools.ConfigEntryFlags.VeryShortComment : NetScriptFramework.Tools.ConfigEntryFlags.NoNewLineBefore;
                    config.AddSetting(f.Key + "." + pair.Key, new NetScriptFramework.Tools.Value(pair.Value), null, cmt, fl);
                }
            }
            if (!config.Load())
                config.Save();

            foreach (var f in mods)
            {
                var v = config.GetValue(f.Key + ".Enabled");
                bool ub;
                if (v != null && v.TryToBoolean(out ub))
                    f.Enabled = ub;

                {
                    var map = f.IntParameters;
                    var ls = map.ToList();
                    foreach (var pair in ls)
                    {
                        string k = f.Key + "." + pair.Key;
                        v = config.GetValue(k);
                        long uv;
                        if (v != null && v.TryToInt64(out uv))
                            map[pair.Key] = uv;
                    }
                }

                {
                    var map = f.FloatParameters;
                    var ls = map.ToList();
                    foreach (var pair in ls)
                    {
                        string k = f.Key + "." + pair.Key;
                        v = config.GetValue(k);
                        double uv;
                        if (v != null && v.TryToDouble(out uv))
                            map[pair.Key] = uv;
                    }
                }

                {
                    var map = f.BoolParameters;
                    var ls = map.ToList();
                    foreach (var pair in ls)
                    {
                        string k = f.Key + "." + pair.Key;
                        v = config.GetValue(k);
                        bool uv;
                        if (v != null && v.TryToBoolean(out uv))
                            map[pair.Key] = uv;
                    }
                }

                {
                    var map = f.StringParameters;
                    var ls = map.ToList();
                    foreach (var pair in ls)
                    {
                        string k = f.Key + "." + pair.Key;
                        v = config.GetValue(k);
                        if (v != null)
                            map[pair.Key] = v.ToString();
                    }
                }
            }

            mods.Sort((u, v) => u.SortValue.CompareTo(v.SortValue));

            foreach (var f in mods)
            {
                if (f.Enabled)
                    f.Apply();
            }
        }

        /// <summary>
        /// The mods.
        /// </summary>
        private static readonly List<Mod> mods = new List<Mod>(32);

        /// <summary>
        /// Gets the mods.
        /// </summary>
        /// <value>
        /// The mods.
        /// </value>
        internal static IReadOnlyList<Mod> Mods
        {
            get
            {
                return mods;
            }
        }

        /// <summary>
        /// Finds the mod.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="mustEnabled">if set to <c>true</c> [must enabled].</param>
        /// <returns></returns>
        internal static Mod FindMod(string key, bool mustEnabled = false)
        {
            Mod r = null;
            foreach (var x in mods)
            {
                if (x.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    r = x;
                    break;
                }
            }

            if (mustEnabled && r != null && !r.Enabled)
                r = null;
            return r;
        }
    }

    /// <summary>
    /// Instance of a modification.
    /// </summary>
    internal abstract class Mod
    {
        /// <summary>
        /// Gets the key. This is the name of type by default.
        /// </summary>
        /// <value>
        /// The key.
        /// </value>
        internal virtual string Key
        {
            get
            {
                return this.GetType().Name;
            }
        }

        /// <summary>
        /// Gets the description for configuration file.
        /// </summary>
        /// <value>
        /// The description.
        /// </value>
        internal abstract string Description
        {
            get;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Mod"/> is enabled by default.
        /// </summary>
        /// <value>
        ///   <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        internal bool Enabled
        {
            get;
            set;
        } = false;

        /// <summary>
        /// Gets or sets the sort value. Higher value means mod will be applied later.
        /// </summary>
        /// <value>
        /// The sort value.
        /// </value>
        internal int SortValue
        {
            get;
            set;
        } = 0;

        /// <summary>
        /// Creates the setting value of type.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <param name="comment">The comment.</param>
        /// <returns></returns>
        internal SettingValue<long> CreateSettingInt(string key, long defaultValue, string comment = null)
        {
            this.IntParameters.Add(key, defaultValue);
            this.OrderOfParams.Add(key);
            if (!string.IsNullOrEmpty(comment))
                this.ParameterComments.Add(key, comment);

            var sv = new SettingValueInt(this, key);
            return sv;
        }

        /// <summary>
        /// Creates the setting value of type.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <param name="comment">The comment.</param>
        /// <returns></returns>
        internal SettingValue<double> CreateSettingFloat(string key, double defaultValue, string comment = null)
        {
            this.FloatParameters.Add(key, defaultValue);
            this.OrderOfParams.Add(key);
            if (!string.IsNullOrEmpty(comment))
                this.ParameterComments.Add(key, comment);

            var sv = new SettingValueFloat(this, key);
            return sv;
        }

        /// <summary>
        /// Creates the setting value of type.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <param name="comment">The comment.</param>
        /// <returns></returns>
        internal SettingValue<bool> CreateSettingBool(string key, bool defaultValue, string comment = null)
        {
            this.BoolParameters.Add(key, defaultValue);
            this.OrderOfParams.Add(key);
            if (!string.IsNullOrEmpty(comment))
                this.ParameterComments.Add(key, comment);

            var sv = new SettingValueBool(this, key);
            return sv;
        }

        /// <summary>
        /// Creates the setting value of type.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <param name="comment">The comment.</param>
        /// <returns></returns>
        internal SettingValue<string> CreateSettingString(string key, string defaultValue, string comment = null)
        {
            this.StringParameters.Add(key, defaultValue);
            this.OrderOfParams.Add(key);
            if (!string.IsNullOrEmpty(comment))
                this.ParameterComments.Add(key, comment);

            var sv = new SettingValueString(this, key);
            return sv;
        }

        /// <summary>
        /// Creates the setting value of type.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <param name="comment">The comment.</param>
        /// <returns></returns>
        internal SettingValue<T> CreateSettingForm<T>(string key, string defaultValue, string comment = null) where T : NetScriptFramework.SkyrimSE.TESForm
        {
            this.StringParameters.Add(key, defaultValue);
            this.OrderOfParams.Add(key);
            if (!string.IsNullOrEmpty(comment))
                this.ParameterComments.Add(key, comment);

            var sv = new SettingValueForm<T>(this, key);
            return sv;
        }

        /// <summary>
        /// Creates the setting value of type.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <param name="comment">The comment.</param>
        /// <returns></returns>
        internal SettingValue<IReadOnlyList<T>> CreateSettingFormList<T>(string key, string defaultValue, string comment = null) where T : NetScriptFramework.SkyrimSE.TESForm
        {
            this.StringParameters.Add(key, defaultValue);
            this.OrderOfParams.Add(key);
            if (!string.IsNullOrEmpty(comment))
                this.ParameterComments.Add(key, comment);

            var sv = new SettingValueFormList<T>(this, key);
            return sv;
        }

        /// <summary>
        /// The int parameters.
        /// </summary>
        internal readonly Dictionary<string, long> IntParameters = new Dictionary<string, long>();

        /// <summary>
        /// The float parameters.
        /// </summary>
        internal readonly Dictionary<string, double> FloatParameters = new Dictionary<string, double>();

        /// <summary>
        /// The string parameters.
        /// </summary>
        internal readonly Dictionary<string, string> StringParameters = new Dictionary<string, string>();

        /// <summary>
        /// The bool parameters.
        /// </summary>
        internal readonly Dictionary<string, bool> BoolParameters = new Dictionary<string, bool>();

        /// <summary>
        /// The parameter comments.
        /// </summary>
        internal readonly Dictionary<string, string> ParameterComments = new Dictionary<string, string>();

        /// <summary>
        /// The order of parameters.
        /// </summary>
        internal readonly List<string> OrderOfParams = new List<string>();

        /// <summary>
        /// Applies this instance.
        /// </summary>
        internal abstract void Apply();
    }

    /// <summary>
    /// Base setting value.
    /// </summary>
    internal abstract class SettingValueBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingValueBase"/> class.
        /// </summary>
        /// <param name="mod">The mod.</param>
        /// <param name="key">The key.</param>
        protected SettingValueBase(Mod mod, string key)
        {
            this.Mod = mod;
            this.Key = key;
        }

        /// <summary>
        /// The mod.
        /// </summary>
        internal readonly Mod Mod;

        /// <summary>
        /// The key.
        /// </summary>
        internal readonly string Key;
    }

    /// <summary>
    /// Typed setting value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="GamePlayTweaks.SettingValueBase" />
    internal abstract class SettingValue<T> : SettingValueBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingValue"/> class.
        /// </summary>
        /// <param name="mod">The mod.</param>
        /// <param name="key">The key.</param>
        protected SettingValue(Mod mod, string key) : base(mod, key)
        {
            
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        internal abstract T Value
        {
            get;
        }
    }

    /// <summary>
    /// Typed setting value.
    /// </summary>
    /// <seealso cref="GamePlayTweaks.SettingValueBase" />
    internal sealed class SettingValueInt : SettingValue<long>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingValueInt"/> class.
        /// </summary>
        /// <param name="mod">The mod.</param>
        /// <param name="key">The key.</param>
        internal SettingValueInt(Mod mod, string key) : base(mod, key)
        {
            
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        internal override long Value
        {
            get
            {
                if (this._cached.HasValue)
                    return this._cached.Value;
                this._cached = this.Mod.IntParameters[this.Key];
                return this._cached.Value;
            }
        }
        private long? _cached;
    }

    /// <summary>
    /// Typed setting value.
    /// </summary>
    /// <seealso cref="GamePlayTweaks.SettingValueBase" />
    internal sealed class SettingValueFloat : SettingValue<double>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingValueFloat"/> class.
        /// </summary>
        /// <param name="mod">The mod.</param>
        /// <param name="key">The key.</param>
        internal SettingValueFloat(Mod mod, string key) : base(mod, key)
        {

        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        internal override double Value
        {
            get
            {
                if (this._cached.HasValue)
                    return this._cached.Value;
                this._cached = this.Mod.FloatParameters[this.Key];
                return this._cached.Value;
            }
        }
        private double? _cached;
    }

    /// <summary>
    /// Typed setting value.
    /// </summary>
    /// <seealso cref="GamePlayTweaks.SettingValueBase" />
    internal sealed class SettingValueBool : SettingValue<bool>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingValueBool"/> class.
        /// </summary>
        /// <param name="mod">The mod.</param>
        /// <param name="key">The key.</param>
        internal SettingValueBool(Mod mod, string key) : base(mod, key)
        {

        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        internal override bool Value
        {
            get
            {
                if (this._cached.HasValue)
                    return this._cached.Value;
                this._cached = this.Mod.BoolParameters[this.Key];
                return this._cached.Value;
            }
        }
        private bool? _cached;
    }

    /// <summary>
    /// Typed setting value.
    /// </summary>
    /// <seealso cref="GamePlayTweaks.SettingValueBase" />
    internal sealed class SettingValueString : SettingValue<string>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingValueString"/> class.
        /// </summary>
        /// <param name="mod">The mod.</param>
        /// <param name="key">The key.</param>
        internal SettingValueString(Mod mod, string key) : base(mod, key)
        {

        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        internal override string Value
        {
            get
            {
                if (this._cached != null)
                    return this._cached;
                this._cached = this.Mod.StringParameters[this.Key];
                return this._cached;
            }
        }
        private string _cached;
    }

    /// <summary>
    /// Typed setting value.
    /// </summary>
    /// <seealso cref="GamePlayTweaks.SettingValueBase" />
    internal sealed class SettingValueForm<T> : SettingValue<T> where T : NetScriptFramework.SkyrimSE.TESForm
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingValueString"/> class.
        /// </summary>
        /// <param name="mod">The mod.</param>
        /// <param name="key">The key.</param>
        internal SettingValueForm(Mod mod, string key) : base(mod, key)
        {

        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        internal override T Value
        {
            get
            {
                if (!ModHandler.HadMainMenu)
                    throw new InvalidOperationException("Trying to read form setting value before reaching main menu!");

                if (this._dcached)
                    return this._cached;
                string input = this.Mod.StringParameters[this.Key];
                var cached = CachedFormList.TryParse(input, "GamePlayTweaks", this.Mod.Key + "." + this.Key, true, false);
                if(cached != null)
                {
                    if (cached.All.Count == 1)
                    {
                        if(cached.All[0] is T)
                        {
                            var f = (T)cached.All[0];
                            this._cached = f;
                        }
                        else
                            NetScriptFramework.Main.Log.AppendLine("Failed to parse " + this.Mod.Key + "." + this.Key + " for GamePlayTweaks! Expected form type " + typeof(T).Name + " but had " + cached.All[0].ToString() + " instead.");
                    }
                    else if(cached.All.Count > 1)
                    {
                        NetScriptFramework.Main.Log.AppendLine("Failed to parse " + this.Mod.Key + "." + this.Key + " for GamePlayTweaks! Expected one form only but had " + cached.All.Count + " instead.");
                    }
                }
                this._dcached = true;
                return this._cached;
            }
        }
        private T _cached;
        private bool _dcached;
    }

    /// <summary>
    /// Typed setting value.
    /// </summary>
    /// <seealso cref="GamePlayTweaks.SettingValueBase" />
    internal sealed class SettingValueFormList<T> : SettingValue<IReadOnlyList<T>> where T : NetScriptFramework.SkyrimSE.TESForm
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingValueString"/> class.
        /// </summary>
        /// <param name="mod">The mod.</param>
        /// <param name="key">The key.</param>
        internal SettingValueFormList(Mod mod, string key) : base(mod, key)
        {

        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        internal override IReadOnlyList<T> Value
        {
            get
            {
                if (!ModHandler.HadMainMenu)
                    throw new InvalidOperationException("Trying to read form setting value before reaching main menu!");

                if (this._dcached)
                    return this._cached;
                string input = this.Mod.StringParameters[this.Key];
                var cached = CachedFormList.TryParse(input, "GamePlayTweaks", this.Mod.Key + "." + this.Key, true, false);
                var lsc = new List<T>();
                if (cached != null)
                {
                    foreach(var x in cached.All)
                    {
                        if (x is T)
                            lsc.Add((T)x);
                        else
                            NetScriptFramework.Main.Log.AppendLine("Failed to parse " + this.Mod.Key + "." + this.Key + " for GamePlayTweaks! Expected form type " + typeof(T).Name + " but had " + x.ToString() + " instead. Skipping this form in the resulting form list.");
                    }
                }
                this._cached = lsc;
                this._dcached = true;
                return this._cached;
            }
        }
        private List<T> _cached;
        private bool _dcached;
    }

    /// <summary>
    /// Cached form list for lookups later.
    /// </summary>
    public sealed class CachedFormList
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="CachedFormList"/> class from being created.
        /// </summary>
        private CachedFormList()
        {
        }

        /// <summary>
        /// The forms.
        /// </summary>
        private readonly List<NetScriptFramework.SkyrimSE.TESForm> Forms = new List<NetScriptFramework.SkyrimSE.TESForm>();

        /// <summary>
        /// The ids.
        /// </summary>
        private readonly HashSet<uint> Ids = new HashSet<uint>();

        /// <summary>
        /// Tries to parse from input. Returns null if failed.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="pluginForLog">The plugin for log.</param>
        /// <param name="settingNameForLog">The setting name for log.</param>
        /// <param name="warnOnMissingForm">If set to <c>true</c> warn on missing form.</param>
        /// <param name="dontWriteAnythingToLog">Don't write any errors to log if failed to parse.</param>
        /// <returns></returns>
        public static CachedFormList TryParse(string input, string pluginForLog, string settingNameForLog, bool warnOnMissingForm = true, bool dontWriteAnythingToLog = false)
        {
            if (string.IsNullOrEmpty(settingNameForLog))
                settingNameForLog = "unknown form list setting";
            if (string.IsNullOrEmpty(pluginForLog))
                pluginForLog = "unknown plugin";

            var ls = new CachedFormList();
            var spl = input.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var x in spl)
            {
                string idstr;
                string file;

                int ix = x.IndexOf(':');
                if (ix <= 0)
                {
                    if (!dontWriteAnythingToLog)
                        NetScriptFramework.Main.Log.AppendLine("Failed to parse " + settingNameForLog + " for " + pluginForLog + "! Invalid input: `" + x + "`.");
                    return null;
                }

                idstr = x.Substring(0, ix);
                file = x.Substring(ix + 1);

                if (!idstr.All(q => (q >= '0' && q <= '9') || (q >= 'a' && q <= 'f') || (q >= 'A' && q <= 'F')))
                {
                    if (!dontWriteAnythingToLog)
                        NetScriptFramework.Main.Log.AppendLine("Failed to parse " + settingNameForLog + " for " + pluginForLog + "! Invalid form ID: `" + idstr + "`.");
                    return null;
                }

                if (string.IsNullOrEmpty(file))
                {
                    if (!dontWriteAnythingToLog)
                        NetScriptFramework.Main.Log.AppendLine("Failed to parse " + settingNameForLog + " for " + pluginForLog + "! Missing file name.");
                    return null;
                }

                uint id = 0;
                if (!uint.TryParse(idstr, System.Globalization.NumberStyles.HexNumber, null, out id) || (id & 0xFF000000) != 0)
                {
                    if (!dontWriteAnythingToLog)
                        NetScriptFramework.Main.Log.AppendLine("Failed to parse " + settingNameForLog + " for " + pluginForLog + "! Invalid form ID: `" + idstr + "`. Don't include plugin index in form ID.");
                    return null;
                }

                var form = NetScriptFramework.SkyrimSE.TESForm.LookupFormFromFile(id, file);
                if (form == null)
                {
                    if (!dontWriteAnythingToLog && warnOnMissingForm)
                        NetScriptFramework.Main.Log.AppendLine("Failed to find form " + settingNameForLog + " for " + pluginForLog + "! Form ID was " + id.ToString("X") + " and file was " + file + ".");
                    continue;
                }

                if (ls.Ids.Add(form.FormId))
                    ls.Forms.Add(form);
            }

            return ls;
        }

        /// <summary>
        /// Determines whether this list contains the specified form.
        /// </summary>
        /// <param name="form">The form.</param>
        /// <returns></returns>
        public bool Contains(NetScriptFramework.SkyrimSE.TESForm form)
        {
            if (form == null)
                return false;

            return Contains(form.FormId);
        }

        /// <summary>
        /// Determines whether this list contains the specified form identifier.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <returns></returns>
        public bool Contains(uint formId)
        {
            return this.Ids.Contains(formId);
        }

        /// <summary>
        /// Gets all forms in this list.
        /// </summary>
        /// <value>
        /// All.
        /// </value>
        public IReadOnlyList<NetScriptFramework.SkyrimSE.TESForm> All
        {
            get
            {
                return this.Forms;
            }
        }
    }
}
