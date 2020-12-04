using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework.Tools;

namespace GrassControl
{
    public sealed class Settings
    {
        [ConfigValue("RayCast", "Ray cast enabled", "Use ray casting to detect where grass shouldn't grow (inside rocks or roads).")]
        public bool RayCast
        {
            get;
            set;
        } = true;

        [ConfigValue("RayCastHeight", "Ray cast height", "The distance above grass that must be free. 200 is slightly more than player height.")]
        public float RayCastHeight
        {
            get;
            set;
        } = 150.0f;

        [ConfigValue("RayCastDepth", "Ray cast depth", "The distance below grass that must be free.")]
        public float RayCastDepth
        {
            get;
            set;
        } = 5.0f;

        [ConfigValue("RayCastCollisionLayers", "Ray cast collision layers", "Which collision layers to check when raycasting. Not recommended to change unless you know what you're doing. These are collision layer index from CK separated by space.")]
        public string RayCastCollisionLayers
        {
            get;
            set;
        }

        [ConfigValue("RayCastIgnoreForms", "Ray cast ignore forms", "Which objects will raycast ignore. This can be useful if you want grass to still grow from some objects (such as roads). The format is formid:formfile separated by ; for example \"1812A:Skyrim.esm;1812D:Skyrim.esm\" would add RoadChunkL01 and RoadChunkL02 forms to ignore list. Base forms go here not object references!")]
        public string RayCastIgnoreForms
        {
            get;
            set;
        } = "";
        
        [ConfigValue("SuperDenseGrass", "Super dense grass", "Enable much more grass without having to change mod files.")]
        public bool SuperDenseGrass
        {
            get;
            set;
        } = false;

        [ConfigValue("SuperDenseMode", "Super dense mode", "How the super dense mode is achieved. Not recommended to change for normal play. This does nothing unless you enable SuperDenseGrass setting. 7 is normal game (meaning nothing is actually changed), 6 or less would be much less grass, 8 is dense, 9 is ?, 10+ is probably crash city.")]
        public int SuperDenseMode
        {
            get;
            set;
        } = 8;

        [ConfigValue("ProfilerReport", "Profiler report", "Whether to track how much time is taken to generate grass. Whenever you open console the result is reported. Try disabling all settings except profiler, go to game and in main menu 'coc riverwood', after loading open console to see normal game time. Then enable settings and check again how it changed. Remember to coc from main menu instead of loading save because it might not be accurate otherwise.")]
        public bool ProfilerReport
        {
            get;
            set;
        } = false;

        [ConfigValue("UseGrassCache", "Use grass cache", "This will generate cache files in /Data/Grass/ and use those when we have them. Any time you change anything with your mod setup you must delete the contents of that directory so new cache can be generated or you will have bugs like floating grass or still grass in objects (that were changed by mods).")]
        public bool UseGrassCache
        {
            get;
            set;
        } = false;

        [ConfigValue("ExtendGrassDistance", "Extend grass distance", "Set true if you want to enable extended grass distance mode. This will allow grass to appear outside of loaded cells. Relevant ini settings:\nSkyrimPrefs.ini [Grass] fGrassStartFadeDistance")]
        public bool ExtendGrassDistance
        {
            get;
            set;
        } = false;

        [ConfigValue("ExtendGrassCount", "Extend grass count", "Allow more grass to be made in total. This is needed if you use very dense grass or have large draw distance. Otherwise it will reach a limit and just stop making grass leaving weird empty squares.")]
        public bool ExtendGrassCount
        {
            get;
            set;
        } = true;

        [ConfigValue("WriteDebugMessages", "Write debug messages", "Write debug messages to NetScriptFramework.log.txt, this should be disabled unless you are actually debugging something!", ConfigEntryFlags.Hidden)]
        public bool WriteDebugMessages
        {
            get;
            set;
        } = false;

        [ConfigValue("EnsureMaxGrassTypesPerTextureSetting", "Ensure max grass types setting", "Makes sure that the max grass types per texture setting is set to at least this much. Can be useful to make sure your INI isn't being overwritten by anything. Most grass replacer mods require this. Set 0 to disable any verification.")]
        public int EnsureMaxGrassTypesPerTextureSetting
        {
            get;
            set;
        } = 7;

        [ConfigValue("OverwriteGrassDistance", "Overwrite grass distance", "Overwrite fGrassStartFadeDistance from any INI. If this is zero or higher then the grass distance will always be taken from this configuration instead of any INI. This can be useful if you have a million things overwriting your INI files and don't know what to edit, so you can just set it here. For example 7000 is vanilla highest in-game grass slider. If you want to set higher you need to enable ExtendGrassDistance setting as well or it will not look right in-game. What the setting actually means is that grass will start to fade out at this distance. Actual total grass distance is this value + fade range value.")]
        public float OverwriteGrassDistance
        {
            get;
            set;
        } = 6000.0f;

        [ConfigValue("OverwriteGrassFadeRange", "Overwrite grass fade range", "Overwrite fGrassFadeRange from any INI. If this is zero or higher then the grass fade range will always be taken from this configuration instead of any INI. This determines the distance it takes for grass to completely fade out starting from OverwriteGrassDistance (or fGrassStartFadeDistance if you didn't use the overwrite). If you want the grass fade out to not be so sudden then increase this value. Probably recommended to keep this at least half of the other setting.")]
        public float OverwriteGrassFadeRange
        {
            get;
            set;
        } = 3000.0f;

        [ConfigValue("OverwriteMinGrassSize", "Overwrite min grass size", "Overwrite iMinGrassSize from any INI. If this is zero or higher then the grass density setting (iMinGrassSize) will be taken from here instead of INI files. Lower values means more dense grass. 50 or 60 is normal mode with somewhat sparse grass (good performance). Lowering the value increases density! 20 is very high density. You should probably not set lower than 20 if you decide to change it. The grass form density setting itself still mostly controls the actual density of grass, think of iMinGrassSize more of as a cap for that setting.")]
        public int OverwriteMinGrassSize
        {
            get;
            set;
        } = -1;

        [ConfigValue("OnlyLoadFromCache", "Only load from cache", "This will change how grass works. It means grass can only ever be loaded from files and not generated during play.")]
        public bool OnlyLoadFromCache
        {
            get;
            set;
        } = false;

        [ConfigValue("SkipPregenerateWorldSpaces", "Skip pregenerate world spaces", "When pre-generating grass then skip worldspaces with this editor IDs. This can greatly speed up generating if we know the worldspace will not need grass at all.")]
        public string SkipPregenerateWorldSpaces
        {
            get;
            set;
        } = "DLC2ApocryphaWorld;DLC01Boneyard;WindhelmPitWorldspace";
        // WindhelmWorld;RiftenWorld;MarkarthWorld;WhiterunWorld;SolitudeWorld;WhiterunDragonsreachWorld - these are supposed to have grass? probably

        [ConfigValue("OnlyPregenerateWorldSpaces", "Only pregenerate world spaces", "If this is not empty then skip every worldspace that isn't in this list.")]
        public string OnlyPregenerateWorldSpaces
        {
            get;
            set;
        } = "";

        internal void Load()
        {
            ConfigFile.LoadFrom(this, "GrassControl", true);
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
                if (!uint.TryParse(idstr, System.Globalization.NumberStyles.HexNumber, null, out id))
                {
                    if (!dontWriteAnythingToLog)
                        NetScriptFramework.Main.Log.AppendLine("Failed to parse " + settingNameForLog + " for " + pluginForLog + "! Invalid form ID: `" + idstr + "`.");
                    return null;
                }

                id &= 0x00FFFFFF;

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
