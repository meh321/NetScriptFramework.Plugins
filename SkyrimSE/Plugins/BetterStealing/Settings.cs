using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework.Tools;

namespace BetterStealing
{
    public sealed class Settings
    {
        [ConfigValue("Enabled", "Enabled", "If you set this to false the whole stealing behavior of the mod is disabled. FixBadStolenItemCount is still applied if it is set to be enabled.")]
        public bool Enabled
        {
            get;
            set;
        } = true;

        [ConfigValue("FixBadStolenItemCount", "Fix bad stolen item count", "There is a bug in the game if you steal a stack of items from a container but the items don't have any other special properties it will not set the count correctly and treat as if you had only stolen one item. If you drop one item the rest of the stack will not be marked as stolen anymore. This fixes that issue. It is also very much needed for the mod to work correctly. Highly recommended to keep this enabled. This setting is still applied even if the Enabled setting is set to false.")]
        public bool FixBadStolenItemCount
        {
            get;
            set;
        } = true;

        [ConfigValue("IgnoreKeywords", "Ignore keywords", "Ignore objects with these keywords. If the stolen object has the keyword we will never remove the stolen state. Separate multiple keywords with ; symbol.")]
        public string IgnoreKeywords
        {
            get;
            set;
        } = "";

        [ConfigValue("AlwaysKeywords", "Always keywords", "Objects with these keywords are always considered when deciding whether to remove stolen state (as long as you didn't get caught). Objects with these keywords ignore the MaxPrice setting. Separate multiple keywords with ; symbol.")]
        public string AlwaysKeywords
        {
            get;
            set;
        } = "";

        [ConfigValue("RequireKeywords", "Require keywords", "If this setting is not empty then ONLY objects that have one of these keywords will be considered for stolen state removal. MaxPrice setting will still be considered as well. Separate multiple keywords with the ; symbol.")]
        public string RequireKeywords
        {
            get;
            set;
        } = "";

        [ConfigValue("MaxPrice", "Max price", "Objects that cost more than this amount will never remove stolen state.")]
        public int MaxPrice
        {
            get;
            set;
        } = 500;

        [ConfigValue("ExcludeEnchantedItems", "Exclude enchanted items", "Enchanted items will never become unstolen.")]
        public bool ExcludeEnchantedItems
        {
            get;
            set;
        } = false;

        [ConfigValue("ExcludeFormIds", "Exclude form IDs", "Exclude the following form IDs from this mod. These items are needed here or the thieves guild questline will not work properly.")]
        public string ExcludeFormIds
        {
            get;
            set;
        } = "44E63:Skyrim.esm;44E6A:Skyrim.esm;44E67:Skyrim.esm;44E6C:Skyrim.esm;44E6E:Skyrim.esm;44E65:Skyrim.esm;19958:Skyrim.esm;19952:Skyrim.esm;60CC2:Skyrim.esm;6F266:Skyrim.esm;5598C:Skyrim.esm;19954:Skyrim.esm;1994F:Skyrim.esm";
        
        public static Settings Instance
        {
            get;
            private set;
        }

        internal void Load()
        {
            Instance = this;
            ConfigFile.LoadFrom(this, "BetterStealing", true);
        }
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
            foreach(var x in spl)
            {
                string idstr;
                string file;

                int ix = x.IndexOf(':');
                if(ix <= 0)
                {
                    if(!dontWriteAnythingToLog)
                        NetScriptFramework.Main.Log.AppendLine("Failed to parse " + settingNameForLog + " for " + pluginForLog + "! Invalid input: `" + x + "`.");
                    return null;
                }

                idstr = x.Substring(0, ix);
                file = x.Substring(ix + 1);

                if(!idstr.All(q => (q >= '0' && q <= '9') || (q >= 'a' && q <= 'f') || (q >= 'A' && q <= 'F')))
                {
                    if (!dontWriteAnythingToLog)
                        NetScriptFramework.Main.Log.AppendLine("Failed to parse " + settingNameForLog + " for " + pluginForLog + "! Invalid form ID: `" + idstr + "`.");
                    return null;
                }

                if(string.IsNullOrEmpty(file))
                {
                    if (!dontWriteAnythingToLog)
                        NetScriptFramework.Main.Log.AppendLine("Failed to parse " + settingNameForLog + " for " + pluginForLog + "! Missing file name.");
                    return null;
                }

                uint id = 0;
                if(!uint.TryParse(idstr, System.Globalization.NumberStyles.HexNumber, null, out id) || (id & 0xFF000000) != 0)
                {
                    if (!dontWriteAnythingToLog)
                        NetScriptFramework.Main.Log.AppendLine("Failed to parse " + settingNameForLog + " for " + pluginForLog + "! Invalid form ID: `" + idstr + "`. Don't include plugin index in form ID.");
                    return null;
                }

                var form = NetScriptFramework.SkyrimSE.TESForm.LookupFormFromFile(id, file);
                if(form == null)
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
