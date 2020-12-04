using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugFixesSSE
{
    /// <summary>
    /// This class handles finding, setting up, configuring and applying all the fixes in this plugin.
    /// </summary>
    internal static class FixHandler
    {
        /// <summary>
        /// The initialized state.
        /// </summary>
        private static int is_init = 0;

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
            foreach(var t in types)
            {
                if (t.IsAbstract || !t.IsSubclassOf(typeof(Fix)))
                    continue;

                var cis = t.GetConstructors().Where(q => q.GetParameters().Length == 0).ToList();
                if (cis.Count != 1)
                    throw new ArgumentException("Warning: " + parent.Name + " fix " + t.Name + " does not have valid parameterless constructor!");

                Fix f = (Fix)cis[0].Invoke(new object[0]);
                fixes.Add(f);
            }

            var config = new NetScriptFramework.Tools.ConfigFile(configKeyword);
            foreach(var f in fixes)
            {
                config.AddSetting(f.Key + ".Enabled", new NetScriptFramework.Tools.Value(f.Enabled), null, f.Description);
                foreach (var pair in f.IntParameters)
                    config.AddSetting(f.Key + "." + pair.Key, new NetScriptFramework.Tools.Value(pair.Value));
                foreach (var pair in f.FloatParameters)
                    config.AddSetting(f.Key + "." + pair.Key, new NetScriptFramework.Tools.Value(pair.Value));
                foreach (var pair in f.StringParameters)
                    config.AddSetting(f.Key + "." + pair.Key, new NetScriptFramework.Tools.Value(pair.Value));
                foreach (var pair in f.BoolParameters)
                    config.AddSetting(f.Key + "." + pair.Key, new NetScriptFramework.Tools.Value(pair.Value));
            }
            if (!config.Load())
                config.Save();

            foreach(var f in fixes)
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

            fixes.Sort((u, v) => u.SortValue.CompareTo(v.SortValue));

            foreach(var f in fixes)
            {
                if (f.Enabled)
                    f.Apply();
            }
        }

        /// <summary>
        /// The fixes.
        /// </summary>
        private static readonly List<Fix> fixes = new List<Fix>(32);

        /// <summary>
        /// Gets the fixes.
        /// </summary>
        /// <value>
        /// The fixes.
        /// </value>
        internal static IReadOnlyList<Fix> Fixes
        {
            get
            {
                return fixes;
            }
        }

        /// <summary>
        /// Finds the fix.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="mustEnabled">if set to <c>true</c> [must enabled].</param>
        /// <returns></returns>
        internal static Fix FindFix(string key, bool mustEnabled = false)
        {
            Fix r = null;
            foreach(var x in fixes)
            {
                if(x.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
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
    /// Instance of a fix.
    /// </summary>
    internal abstract class Fix
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
        /// Gets or sets a value indicating whether this <see cref="Fix"/> is enabled by default.
        /// </summary>
        /// <value>
        ///   <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        internal bool Enabled
        {
            get;
            set;
        } = true;

        /// <summary>
        /// Gets or sets the sort value. Higher value means fix will be applied later.
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
        /// Applies this instance.
        /// </summary>
        internal abstract void Apply();
    }
}
