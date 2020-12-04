using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework.Tools;

namespace ItemDurability
{
    public sealed class Settings
    {
        [ConfigValue("ShowItemDurabilityInName", "Show item durability", "Show item durability in name of armor and weapons.\n0 means disabled, 1 means show always, 2 means show if not 100%.\nRecommended to use 0 or 2 because 1 might also show for items that won't lose durability.")]
        public int ShowItemDurabilityInName
        {
            get;
            set;
        } = 2;

        [ConfigValue("ItemDurabilityFormat", "Item durability format", "The format to show item durability with. This is the C printf format. Don't change the %.0f part unless you know what you're doing or it may crash the game.")]
        public string ItemDurabilityFormat
        {
            get;
            set;
        } = " [%.0f]";
        
        [ConfigValue("WeaponHealthNames", "Overwrite health names (weapon)", "Overwrite health names for these ranges. You can write %i anywhere in the name to show the durability number.")]
        public string WeaponHealthNames
        {
            get;
            set;
        } = "0;0.2;Ruined;0.2;0.4;Broken;0.4;0.6;Damaged;0.6;0.8;Chipped;0.8;0.95;Blemished";

        [ConfigValue("ArmorHealthNames", "Overwrite health names (armor)", "Overwrite health names for these ranges. You can write %i anywhere in the name to show the durability number.")]
        public string ArmorHealthNames
        {
            get;
            set;
        } = "0;0.2;Ruined;0.2;0.4;Broken;0.4;0.6;Damaged;0.6;0.8;Chipped;0.8;0.95;Blemished";

        [ConfigValue("EnableWeaponDegradation", "Enable weapon degradation", "If set to true then weapon quality will degrade.")]
        public bool EnableWeaponDegradation
        {
            get;
            set;
        } = true;

        [ConfigValue("WeaponDegradeOnHit", "Weapon degrade on hit", "How much weapon degrades when you attack with it. 1 means 1% (100% is normal item quality).")]
        public float WeaponDegradeOnHit
        {
            get;
            set;
        } = 0.05f;

        [ConfigValue("WeaponDegradePassive", "Weapon degrade passive", "How much weapon degrades per second.")]
        public float WeaponDegradePassive
        {
            get;
            set;
        } = 0.0f;

        [ConfigValue("WeaponDegradePassiveOut", "Weapon degrade passive (out)", "How much weapon degrades per second while having it out (wielded).")]
        public float WeaponDegradePassiveOut
        {
            get;
            set;
        } = 0.0f;

        [ConfigValue("WeaponDegradePowerAttackMult", "Power attack degrade multiplier", "If you attack with power attack then weapon will lose durability this much faster, set 1.0 to disable extra loss on power attack.")]
        public float WeaponDegradePowerAttackMult
        {
            get;
            set;
        } = 3.0f;

        [ConfigValue("WeaponDegradePowerBashMult", "Power bash degrade multiplier", "If you attack with power bash then weapon will lose durability this much faster, set 1.0 to disable extra loss on power bash.")]
        public float WeaponDegradePowerBashMult
        {
            get;
            set;
        } = 3.0f;

        [ConfigValue("WeaponDegradeRequiresImpact", "Weapon degrade requires impact", "Melee weapons require you to actually hit something to lose durability. If true and you miss with melee weapons it does not reduce durability.")]
        public bool WeaponDegradeRequiresImpact
        {
            get;
            set;
        } = true;

        [ConfigValue("EnableArmorDegradation", "Enable armor degradation", "If set to true then armor quality will degrade.")]
        public bool EnableArmorDegradation
        {
            get;
            set;
        } = true;

        [ConfigValue("ArmorDegradeOnHit", "Armor degrade on hit", "How much armor degrades when you get hit. 1 means 1% (100% is normal item quality).")]
        public float ArmorDegradeOnHit
        {
            get;
            set;
        } = 0.2f;

        [ConfigValue("ArmorDegradePassive", "Armor degrade passive", "How much armor degrades per second while having it equipped.")]
        public float ArmorDegradePassive
        {
            get;
            set;
        } = 0.0f;

        [ConfigValue("BootsDegradePassive", "Boots degrade passive", "How much boots degrade per second while having it equipped.")]
        public float BootsDegradePassive
        {
            get;
            set;
        } = 0.0f;

        [ConfigValue("ArmorDegradeRandom", "Armor degrade random", "When you get hit choose randomly one piece of armor that degrades. If false then all armor will degrade on hit.")]
        public bool ArmorDegradeRandom
        {
            get;
            set;
        } = true;

        [ConfigValue("ArmorDegradeNonArmor", "Allow non-armor to degrade?", "If false then only light armor and heavy armor can degrade.")]
        public bool ArmorDegradeNonArmor
        {
            get;
            set;
        } = false;

        [ConfigValue("ArmorDegradeRestrictSlot", "Restrict slots of armor degrade", "If set to true then only helm, chest, boots, shield and gloves can degrade. This may have some issues detecting certain type of items.")]
        public bool ArmorDegradeRestrictSlot
        {
            get;
            set;
        } = false;

        [ConfigValue("ArmorDegradeRestrictNotBoots", "Restrict slots of armor degrade (boots)", "If set to true then boots don't degrade when getting hit. This can be helpful if you want to set the passive degrade on boots.")]
        public bool ArmorDegradeRestrictNotBoots
        {
            get;
            set;
        } = false;

        [ConfigValue("ArmorDegradeShieldOnlyIfBlocking", "Shield only (if blocking)", "When degrading armor and you are currently blocking with a shield then only degrade shield durability.")]
        public bool ArmorDegradeShieldOnlyIfBlocking
        {
            get;
            set;
        } = true;

        [ConfigValue("ArmorDegradeWeaponOnlyIfParrying", "Weapon only (if parrying)", "When degrading armor and you are currently parrying with a weapon then only degrade the weapon instead.")]
        public bool ArmorDegradeWeaponOnlyIfParrying
        {
            get;
            set;
        } = true;

        [ConfigValue("OnlyDegradeTemperableItems", "Only degrade temperable items", "Only allow to degrade weapons or armor that can be tempered.")]
        public bool OnlyDegradeTemperableItems
        {
            get;
            set;
        } = true;
        
        [ConfigValue("KeywordBreakSpeedMult", "Keyword break speed mult", "How quickly things degrade based on keywords. Each keyword modifier is a multiplier. These would be the keywords on the weapon or armor base form. If you don't want this at all then set the whole setting to just = \"\" and it will disable any modifiers on different types of items.")]
        public string KeywordBreakSpeedMult
        {
            get;
            set;
        } =
            "_DefaultMissingMaterial=2.5"

            + ";ArmorLight=1"
            + ";ArmorMaterialForsworn=2.5"
            + ";VendorItemAnimalHide=2.5" // Fur=2.5
            + ";ArmorMaterialHide=2.5"
            + ";ArmorMaterialStudded=2.5"
            // Thalmor=2.4 - is clothing
            + ";ArmorMaterialImperialStudded=2.3"
            + ";ArmorMaterialImperialLight=2.3"
            + ";DLC1ArmorMaterialDawnguard=2.1"
            + ";DLC1ArmorMaterialVampire=2.1"
            + ";DLC1ArmorMaterialFalmerHardened=1.85"
            + ";ArmorMaterialLeather=1.8"
            + ";ArmorMaterialBearStormcloak=1.8"
            + ";ArmorMaterialThievesGuild=1.6"
            + ";DLC2ArmorMaterialChitinLight=1.6"
            + ";ArmorMaterialElven=1.6"
            + ";ArmorMaterialScaled=1.3"
            + ";ArmorMaterialElvenGilded=1.1"
            + ";ArmorMaterialThievesGuildLeader=1"
            + ";DLC2ArmorMaterialStalhrimLight=0.9"
            + ";ArmorMaterialGlass=0.85"
            + ";ArmorMaterialDragonscale=0.8"
            + ";ArmorNightingale=0.8"

        + ";ArmorHeavy=0.7"
        + ";ArmorMaterialIron=2.5"
        // AncientNord=2.5 - is daedric?
        + ";ArmorMaterialIronBanded=2.5"
        + ";ArmorMaterialImperialHeavy=2.3"
        // Dawnguard=2.1 - already set up
        + ";ArmorMaterialSteel=2"
        + ";DLC2ArmorMaterialBonemoldHeavy=1.9"
        + ";DLC1ArmorMaterialFalmerHeavy=1.85"
        + ";ArmorMaterialDwarven=1.75"
        + ";ArmorMaterialSteelPlate=1.6"
        + ";DLC2ArmorMaterialNordicHeavy=1.6"
        + ";ArmorMaterialBlades=1.6"
        + ";ArmorMaterialOrcish=1.3"
        + ";ArmorMaterialEbony=1"
        + ";DLC2ArmorMaterialStalhrimHeavy=0.9"
        + ";ArmorMaterialDragonplate=0.85"
        + ";ArmorMaterialDaedric=0.8"

            + ";WeapMaterialDraugr=2.5"
            + ";WeapMaterialIron=2.5"
            + ";WeapMaterialFalmer=2.5"
            + ";WeapMaterialImperial=2.2"
            + ";WeapMaterialSteel=2"
            + ";WeapMaterialElven=1.9"
            + ";WeapMaterialDwarven=1.75"
            + ";DLC2WeaponMaterialNordic=1.6"
            + ";WeapMaterialOrcish=1.3"
            + ";WeapMaterialEbony=1"
            + ";DLC2WeaponMaterialStalhrim=0.9"
            + ";WeapMaterialGlass=0.85"
            + ";WeapMaterialDaedric=0.8"
            + ";DLC1WeapMaterialDragonbone=0.8"

            + ";DaedricArtifact=0"
        ;

        [ConfigValue("ApplyToNPC", "Apply to NPC", "Apply weapon and armor degradation to NPCs? If set to true then passive degradation will only be applied to team-mates and not normal NPCs. On hit degradation is applied to any NPC.")]
        public bool ApplyToNPC
        {
            get;
            set;
        } = false;

        [ConfigValue("DebugMessages", "Debug messages", "Show debug messages when player equipment degrades?")]
        public bool DebugMessages
        {
            get;
            set;
        } = false;

        [ConfigValue("DebugOption", "Debug option", "A hotkey to set worn equipment to random durabilities.", ConfigEntryFlags.Hidden)]
        public bool DebugOption
        {
            get;
            set;
        } = false;

        [ConfigValue("AutoRestoreNonRepairableItemDurability", "Auto restore non-repairable", "Auto restore the durability of items in inventory that can't be repaired? This option is only necessary if you were using v1 and some items that can't be repaired lost durability.")]
        public bool AutoRestoreNonRepairableItemDurability
        {
            get;
            set;
        } = false;

        [ConfigValue("DebugAutoRestoreAllItemDurability", "Auto restore all items", "Auto restore the durability of all items in inventory. This can be helpful if you are planning to uninstall and want items to go back to normal.")]
        public bool DebugAutoRestoreAllItemDurability
        {
            get;
            set;
        } = false;

        internal void Load()
        {
            ConfigFile.LoadFrom(this, "ItemDurability", true);
        }
    }

    internal static class _kwModCache
    {
        private static readonly Dictionary<string, float> AllMap = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        private static bool IsMaterial(string text)
        {
            if (text.IndexOf("material", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (text.Equals("VendorItemAnimalHide", StringComparison.OrdinalIgnoreCase))
                return true;

            if (text.Equals("ArmorNightingale", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private static void ReportError(string thing, string message)
        {
            string tx = message;
            if (!string.IsNullOrEmpty(thing))
                tx = thing + " <- " + tx;
            tx = "ItemDurability loading KeywordBreakSpeedMult: " + tx;

            var l = NetScriptFramework.Main.Log;
            if (l != null)
                l.AppendLine(tx);

            NetScriptFramework.Main.WriteDebugMessage(tx);
        }

        internal static void LoadFrom(string input)
        {
            AllMap.Clear();

            if (string.IsNullOrEmpty(input))
                return;

            var spl = input.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach(var _x in spl)
            {
                int ix = _x.IndexOf('=');
                if(ix < 0)
                {
                    ReportError(_x, "Expected '='!");
                    AllMap.Clear();
                    return;
                }

                string key = _x.Substring(0, ix).Trim();
                if(string.IsNullOrEmpty(key))
                {
                    ReportError(_x, "Expected keyword on left side of '='!");
                    AllMap.Clear();
                    return;
                }

                string vl = _x.Substring(ix + 1).Trim();
                float tmp;
                if(string.IsNullOrEmpty(vl) || !float.TryParse(vl, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out tmp) || tmp < 0.0f)
                {
                    ReportError(_x, "Expected a valid value on right side of '='!");
                    AllMap.Clear();
                    return;
                }

                if (AllMap.ContainsKey(key))
                    ReportError(_x, "Warning duplicate key '" + key + "'! Continuing anyway.");

                AllMap[key] = tmp;
            }
        }

        internal static float Calculate(NetScriptFramework.SkyrimSE.TESForm form)
        {
            if (form == null || AllMap.Count == 0)
                return 1.0f;

            float amt = 1.0f;
            float? mat = null;

            var kwForm = form as NetScriptFramework.SkyrimSE.BGSKeywordForm;
            if(kwForm != null)
            {
                int count = kwForm.Count;
                if(count != 0)
                {
                    var buf = kwForm.Keywords;
                    if (buf != IntPtr.Zero)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            var kwPtr = NetScriptFramework.Memory.ReadPointer(buf + 8 * i);
                            if (kwPtr == IntPtr.Zero)
                                continue;

                            var kw = NetScriptFramework.MemoryObject.FromAddress<NetScriptFramework.SkyrimSE.BGSKeyword>(kwPtr);
                            if (kw != null)
                            {
                                var kwt = kw.KeywordText;
                                if (kwt != null)
                                {
                                    string tx = kw.KeywordText.Text;
                                    if(!string.IsNullOrEmpty(tx))
                                    {
                                        float tmp;
                                        if(AllMap.TryGetValue(tx, out tmp))
                                        {
                                            if (ItemDurabilityPlugin.Settings.DebugMessages)
                                                ItemDurabilityPlugin.DebugMsg("Calculating keyword modifier " + tx + "=" + tmp + " for item " + form.ToString());

                                            if (IsMaterial(tx))
                                            {
                                                if (mat.HasValue)
                                                    mat = Math.Min(mat.Value, tmp);
                                                else
                                                    mat = tmp;
                                            }
                                            else
                                                amt *= tmp;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if(!mat.HasValue)
            {
                float tmp;
                if (AllMap.TryGetValue("_DefaultMissingMaterial", out tmp))
                    mat = tmp;
                else
                    mat = 1.0f;
            }

            return amt * mat.Value;
        }
    }

    internal static class _temperableCache
    {
        private static HashSet<uint> _formIds = null;

        internal static void Rebuild()
        {
            _formIds = new HashSet<uint>();

            var data = NetScriptFramework.SkyrimSE.DataHandler.Instance;
            if (data == null)
                return;

            var recipes = data.GetAllFormsByType(NetScriptFramework.SkyrimSE.FormTypes.ConstructibleObject);
            foreach(var f in recipes)
            {
                var co = f as NetScriptFramework.SkyrimSE.BGSConstructibleObject;
                if (co == null)
                    continue;

                var kw = co.CraftingStationKeyword;
                if (kw == null)
                    continue;

                var kws = kw.KeywordText;
                if (kws == null)
                    continue;

                var kwt = kws.Text;
                if (string.IsNullOrEmpty(kwt))
                    continue;

                if (!kwt.Equals("CraftingSmithingArmorTable", StringComparison.OrdinalIgnoreCase) &&
                    !kwt.Equals("CraftingSmithingSharpeningWheel", StringComparison.OrdinalIgnoreCase))
                    continue;

                var obj = co.CreatedItem;
                if (obj != null)
                    _formIds.Add(obj.FormId);
            }
        }

        internal static bool IsTemperable(NetScriptFramework.SkyrimSE.TESForm form)
        {
            if (form == null)
                return false;

            var armor = form as NetScriptFramework.SkyrimSE.TESObjectARMO;
            if(armor != null)
            {
                int tries = 0;
                while(armor != null && tries++ < 10)
                {
                    if (_formIds.Contains(armor.FormId))
                        return true;

                    armor = armor.TemplateArmor;
                }

                return false;
            }

            var weap = form as NetScriptFramework.SkyrimSE.TESObjectWEAP;
            if(weap != null)
            {
                int tries = 0;
                while(weap != null && tries++ < 10)
                {
                    if (_formIds.Contains(weap.FormId))
                        return true;

                    weap = weap.TemplateWeapon;
                }

                return false;
            }

            return false;
        }
    }
}
