using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomSkills
{
    public class Settings
    {
        internal static IEnumerable<Skill> ReadSkills()
        {
            var skills = new List<Skill>();
            var dir = new System.IO.DirectoryInfo("Data/NetScriptFramework/Plugins");
            var files = dir.GetFiles();
            foreach(var x in files)
            {
                string n = x.Name;
                if (!n.StartsWith("CustomSkill.", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!n.EndsWith(".config.txt", StringComparison.OrdinalIgnoreCase))
                    continue;

                string key = n.Substring(12);
                key = key.Substring(0, key.Length - 11);

                if (key.Length == 0)
                    continue;

                var sk = ReadSkill(key, x);
                if (sk != null)
                    skills.Add(sk);
            }

            return skills;
        }

        private const int MaxNodes = 128;

        private static Skill ReadSkill(string key, System.IO.FileInfo info)
        {
            var cv = new NetScriptFramework.Tools.ConfigFile("CustomSkill." + key);
            cv.AddSetting("Name", new NetScriptFramework.Tools.Value("Vampire Lord"));
            cv.AddSetting("Description", new NetScriptFramework.Tools.Value("Kill enemies with the Drain Life or bite power attack to earn perks. Each new perk requires a few more feedings."));
            cv.AddSetting("Skydome", new NetScriptFramework.Tools.Value("DLC01/Interface/INTVampirePerkSkydome.nif"));
            cv.AddSetting("SkydomeNormalNif", new NetScriptFramework.Tools.Value(false));
            cv.AddSetting("LevelFile", new NetScriptFramework.Tools.Value("Skyrim.esm"));
            cv.AddSetting("LevelId", new NetScriptFramework.Tools.Value((uint)0x646));
            cv.AddSetting("RatioFile", new NetScriptFramework.Tools.Value(""));
            cv.AddSetting("RatioId", new NetScriptFramework.Tools.Value((uint)0x0));
            cv.AddSetting("ShowLevelupFile", new NetScriptFramework.Tools.Value(""));
            cv.AddSetting("ShowLevelupId", new NetScriptFramework.Tools.Value((uint)0x0));
            cv.AddSetting("ShowMenuFile", new NetScriptFramework.Tools.Value(""));
            cv.AddSetting("ShowMenuId", new NetScriptFramework.Tools.Value((uint)0x0));
            cv.AddSetting("PerkPointsFile", new NetScriptFramework.Tools.Value(""));
            cv.AddSetting("PerkPointsId", new NetScriptFramework.Tools.Value((uint)0x0));
            cv.AddSetting("LegendaryFile", new NetScriptFramework.Tools.Value(""));
            cv.AddSetting("LegendaryId", new NetScriptFramework.Tools.Value((uint)0x0));
            cv.AddSetting("ColorFile", new NetScriptFramework.Tools.Value(""));
            cv.AddSetting("ColorId", new NetScriptFramework.Tools.Value((uint)0x0));
            cv.AddSetting("DebugReloadFile", new NetScriptFramework.Tools.Value(""));
            cv.AddSetting("DebugReloadId", new NetScriptFramework.Tools.Value((uint)0x0));
            for (int i = 0; i <= MaxNodes; i++)
            {
                string nk = "Node" + i + ".";
                cv.AddSetting(nk + "Enable", new NetScriptFramework.Tools.Value(false));
                cv.AddSetting(nk + "PerkFile", new NetScriptFramework.Tools.Value(""));
                cv.AddSetting(nk + "PerkId", new NetScriptFramework.Tools.Value((uint)0x0));
                cv.AddSetting(nk + "X", new NetScriptFramework.Tools.Value((float)0));
                cv.AddSetting(nk + "Y", new NetScriptFramework.Tools.Value((float)0));
                cv.AddSetting(nk + "GridX", new NetScriptFramework.Tools.Value((int)0));
                cv.AddSetting(nk + "GridY", new NetScriptFramework.Tools.Value((int)0));
                cv.AddSetting(nk + "Links", new NetScriptFramework.Tools.Value(""));
            }
            cv.Load();

            var sk = new Skill();
            var v = cv.GetValue("Name");
            if (v != null)
                sk.Name.Value = v.ToString();
            v = cv.GetValue("Description");
            if (v != null)
                sk.Description.Value = v.ToString();
            v = cv.GetValue("Skydome");
            if (v != null)
                sk.Skydome.Value = v.ToString();
            v = cv.GetValue("SkydomeNormalNif");
            bool ub;
            if (v != null && v.TryToBoolean(out ub) && ub)
                sk.NormalNif = true;

            sk.Level = LoadGValueShort("Level", cv);
            sk.Ratio = LoadGValueFloat("Ratio", cv);
            sk.ShowLevelup = LoadGValueShort("ShowLevelup", cv);
            sk.OpenMenu = LoadGValueShort("ShowMenu", cv);
            sk.PerkPoints = LoadGValueShort("PerkPoints", cv);
            sk.Legendary = LoadGValueShort("Legendary", cv);
            sk.Color = LoadGValueInt("Color", cv);
            sk.DebugReload = LoadGValueShort("DebugReload", cv);

            if(sk.Level == null)
            {
                Error(info, "Failed to load form for Level global variable!");
                return null;
            }

            if(sk.Ratio == null)
            {
                Error(info, "Failed to load form for Level ratio global variable!");
                return null;
            }

            if(sk.ShowLevelup == null)
            {
                Error(info, "Failed to load form for showing Level up global variable!");
                return null;
            }

            if(sk.OpenMenu == null)
            {
                Error(info, "Failed to load form for showing skill menu global variable!");
                return null;
            }

            var nodes = new List<TreeNode>();
            for(int i = 0; i <= MaxNodes; i++)
            {
                string nk = "Node" + i + ".";
                v = cv.GetValue(nk + "Enable");
                bool r;
                if (v == null || !v.TryToBoolean(out r) || !r)
                    continue;

                var tn = new TreeNode();
                tn.Index = i;
                int ri;
                uint ru;
                float rf;
                v = cv.GetValue(nk + "PerkFile");
                if (v != null)
                    tn.PerkFile = v.ToString();
                v = cv.GetValue(nk + "PerkId");
                if (v != null && v.TryToUInt32(out ru) && ru != 0)
                    tn.PerkId = ru;
                v = cv.GetValue(nk + "X");
                if (v != null && v.TryToSingle(out rf))
                    tn.X = rf;
                v = cv.GetValue(nk + "Y");
                if (v != null && v.TryToSingle(out rf))
                    tn.Y = rf;
                v = cv.GetValue(nk + "GridX");
                if (v != null && v.TryToInt32(out ri))
                    tn.GridX = ri;
                v = cv.GetValue(nk + "GridY");
                if (v != null && v.TryToInt32(out ri))
                    tn.GridY = ri;
                v = cv.GetValue(nk + "Links");
                if(v != null)
                {
                    string lns = v.ToString();
                    var spl = lns.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    List<int> lnks = new List<int>();
                    foreach(var x in spl)
                    {
                        if (int.TryParse(x, System.Globalization.NumberStyles.None, null, out ri))
                            lnks.Add(ri);
                        else
                        {
                            Error(info, "Format error in " + nk + "Links! Unable to parse integer from " + x + "!");
                            return null;
                        }
                    }

                    if (lnks.Count != 0)
                        tn.Links = lnks;
                }

                nodes.Add(tn);
            }

            try
            {
                sk.SkillTree = TreeNode.Create(nodes);
            }
            catch
            {
                Error(info, "Something went wrong when creating skill perk tree! Make sure node 0 exists and no missing nodes are referenced in links.");
                return null;
            }

            return sk;
        }

        private static GValueShort LoadGValueShort(string key, NetScriptFramework.Tools.ConfigFile file)
        {
            var v = file.GetValue(key + "File");
            if (v == null)
                return null;

            string fname = v.ToString();
            if (string.IsNullOrEmpty(fname))
                return null;

            v = file.GetValue(key + "Id");
            if (v == null)
                return null;

            uint id;
            if (!v.TryToUInt32(out id) || id == 0)
                return null;

            var g = NetScriptFramework.SkyrimSE.TESForm.LookupFormFromFile(id, fname) as NetScriptFramework.SkyrimSE.TESGlobal;
            if (g == null)
                return null;

            return new GValueShort(g);
        }

        private static GValueFloat LoadGValueFloat(string key, NetScriptFramework.Tools.ConfigFile file)
        {
            var v = file.GetValue(key + "File");
            if (v == null)
                return null;

            string fname = v.ToString();
            if (string.IsNullOrEmpty(fname))
                return null;

            v = file.GetValue(key + "Id");
            if (v == null)
                return null;

            uint id;
            if (!v.TryToUInt32(out id) || id == 0)
                return null;

            var g = NetScriptFramework.SkyrimSE.TESForm.LookupFormFromFile(id, fname) as NetScriptFramework.SkyrimSE.TESGlobal;
            if (g == null)
                return null;

            return new GValueFloat(g);
        }

        private static GValueInt LoadGValueInt(string key, NetScriptFramework.Tools.ConfigFile file)
        {
            var v = file.GetValue(key + "File");
            if (v == null)
                return null;

            string fname = v.ToString();
            if (string.IsNullOrEmpty(fname))
                return null;

            v = file.GetValue(key + "Id");
            if (v == null)
                return null;

            uint id;
            if (!v.TryToUInt32(out id) || id == 0)
                return null;

            var g = NetScriptFramework.SkyrimSE.TESForm.LookupFormFromFile(id, fname) as NetScriptFramework.SkyrimSE.TESGlobal;
            if (g == null)
                return null;

            return new GValueInt(g);
        }

        private static NetScriptFramework.SkyrimSE.TESForm LoadForm(string key, NetScriptFramework.Tools.ConfigFile file)
        {
            var v = file.GetValue(key + "File");
            if (v == null)
                return null;

            string fname = v.ToString();
            if (string.IsNullOrEmpty(fname))
                return null;

            v = file.GetValue(key + "Id");
            if (v == null)
                return null;

            uint id;
            if (!v.TryToUInt32(out id) || id == 0)
                return null;

            return NetScriptFramework.SkyrimSE.TESForm.LookupFormFromFile(id, fname);
        }

        private static void Error(System.IO.FileInfo info, string message)
        {
            string ln = "CustomSkills plugin: Failed to read skill from file `" + info.Name + "`: " + (message ?? "");

            var l = NetScriptFramework.Main.Log;
            if (l != null)
                l.AppendLine(ln);

            NetScriptFramework.Main.WriteDebugMessage(ln);
        }
    }
}
