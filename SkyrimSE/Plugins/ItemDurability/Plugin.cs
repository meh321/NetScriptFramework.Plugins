using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework;
using NetScriptFramework.SkyrimSE;
using main = NetScriptFramework.Main;
using gamemain = NetScriptFramework.SkyrimSE.Main;

namespace ItemDurability
{
    public class ItemDurabilityPlugin : Plugin
    {
        public override string Key
        {
            get
            {
                return "item_durability";
            }
        }

        public override string Name
        {
            get
            {
                return "Item Durability";
            }
        }

        public override int Version
        {
            get
            {
                return 3;
            }
        }

        public override string Author
        {
            get
            {
                return "meh321";
            }
        }

        public static Settings Settings
        {
            get;
            private set;
        }

        internal const float DefaultHealth = 1.0f;
        internal const float MinHealth = 0.01f;

        private static IntPtr addr_GetHealth;

        private static List<Tuple<float, float, string>> _armorNames = null;
        private static List<Tuple<float, float, string>> _weaponNames = null;

        private static bool _did_g = false;

        private static void Error(string message)
        {
            var l = main.Log;
            if (l != null)
                l.AppendLine("ItemDurability: " + message);

            main.WriteDebugMessage("ItemDurability: " + message);
        }

        private static List<Tuple<float, float, string>> ParseNames(string settingName, string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            var ls = new List<Tuple<float, float, string>>();
            var spl = value.Split(new[] { ';' }, StringSplitOptions.None);
            if ((spl.Length % 3) != 0)
            {
                Error("Failed to parse " + settingName + "!");
                return null;
            }

            for(int i = 0; i < spl.Length; i += 3)
            {
                float min;
                float max;
                if(!float.TryParse(spl[i], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out min))
                {
                    Error("Failed to parse " + settingName + "! Invalid value: " + spl[i]);
                    return null;
                }

                if (!float.TryParse(spl[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out max))
                {
                    Error("Failed to parse " + settingName + "! Invalid value: " + spl[i + 1]);
                    return null;
                }

                ls.Add(new Tuple<float, float, string>(min, max, spl[i + 2]));
            }

            return ls;
        }

        private static Dictionary<ulong, MemoryAllocation> _cachedNameMap = new Dictionary<ulong, MemoryAllocation>();

        private static readonly object _cachedNameLocker = new object();

        private static ulong BuildCachedNameKey(bool weap, int index, int pfx)
        {
            ulong mask = 0;
            if (weap)
                mask |= 1;

            mask <<= 24;
            mask |= unchecked((uint)index);
            mask <<= 32;
            mask |= unchecked((uint)pfx);
            return mask;
        }

        private static float GetHealth(ExtraContainerChanges.ItemEntry item, bool oneIfMissing = true)
        {
            if (item == null)
                return 1.0f;

            var ed = item.ExtraData;
            if(ed != null)
            {
                var itm = ed.Item;
                if (itm != null)
                    return GetHealth(itm, oneIfMissing);
            }

            return 1.0f;
        }

        private static float GetHealth(BSExtraDataList ed, bool oneIfMissing = true)
        {
            if (ed == null)
                return 1.0f;

            float health = Memory.InvokeCdeclF(addr_GetHealth, ed.Cast<BSExtraDataList>());
            if (oneIfMissing && health < 0.0f)
                return 1.0f;
            return health;
        }

        private static void SetHealth(Actor owner, ExtraContainerChanges.ItemEntry item, float health)
        {
            if (item == null)
                return;

            var ed = item.ExtraData;
            if(ed != null)
            {
                var itm = ed.Item;
                if(itm != null)
                {
                    SetHealth(owner, itm, health);
                    return;
                }
            }

            if (health == 1.0f)
                return;

            var edPtr = MemoryManager.Allocate(24, 0);
            Memory.InvokeCdecl(addr_EDctor, edPtr);

            SetHealth(null, MemoryObject.FromAddress<BSExtraDataList>(edPtr), health);

            Memory.InvokeCdecl(addr_AddEDList, item.Cast<ExtraContainerChanges.ItemEntry>(), edPtr, 1);

            if (owner != null)
            {
                owner.InvokeVTableThisCall<Actor>(0x50, 0x8000420); // AddChange
                owner.InvokeVTableThisCall<Actor>(0x650); // OnArmorActorValueChanged
            }
        }

        private static void SetHealth(Actor owner, BSExtraDataList data, float health)
        {
            if (data != null)
            {
                if (health != 1.0f)
                    Memory.InvokeCdecl(addr_BSSetHealth, data.Cast<BSExtraDataList>(), health);
                else
                    Memory.InvokeCdecl(addr_BSRemoveHealth, data.Cast<BSExtraDataList>());

                if(owner != null)
                {
                    owner.InvokeVTableThisCall<Actor>(0x50, 0x8000420); // AddChange
                    owner.InvokeVTableThisCall<Actor>(0x650); // OnArmorActorValueChanged
                }
            }
        }

        internal static void DebugMsg(string msg)
        {
            var l = main.Log;
            if (l != null)
                l.AppendLine("ItemDurability: " + msg);

            main.WriteDebugMessage(msg);

            MenuManager.ShowHUDMessage(msg, null, true);
        }

        private static void RestoreItems_Passive(Actor actor, float time)
        {
            bool all = Settings.DebugAutoRestoreAllItemDurability;
            bool non = Settings.AutoRestoreNonRepairableItemDurability;

            if (!all && !non)
                return;

            var cont = actor.Inventory;
            if (cont == null)
                return;

            int did = 0;
            foreach(var o in cont.Objects)
            {
                var ed = o.ExtraData;
                if (ed == null)
                    continue;

                foreach(var e in ed)
                {
                    float health = GetHealth(e);
                    if (health >= 1.0f)
                        continue;

                    if (all)
                    {
                        SetHealth(null, e, 1.0f);
                        did++;
                    }
                    else if (non && !CanDegrade(o.Template))
                    {
                        SetHealth(null, e, 1.0f);
                        did++;
                    }
                }
            }

            if (Settings.DebugMessages && did > 0 && actor.IsPlayer)
                DebugMsg("Restored durability of " + did + " items back to full.");
        }

        private static void DegradeItems_Passive(Actor actor, float time)
        {
            var cont = actor.Inventory;
            if (cont == null)
                return;

            foreach(var o in cont.Objects)
            {
                var ed = o.ExtraData;
                if (ed == null)
                    continue;

                foreach (var e in ed)
                {
                    if (!Memory.InvokeCdecl(addr_IsWorn, e.Cast<BSExtraDataList>(), 1, 0).ToBool())
                        continue;

                    if (!CanDegrade(o.Template))
                        break;

                    if (Settings.EnableArmorDegradation && ChooseForArmorDegrade(o.Template, false) > 0.0)
                    {
                        float amt = time * Settings.ArmorDegradePassive;
                        if ((((TESObjectARMO)o.Template).ModelData.Slots & BGSBipedObjectForm.BipedObjectSlots.Feet) != BGSBipedObjectForm.BipedObjectSlots.None)
                            amt = Math.Max(amt, time * Settings.BootsDegradePassive);

                        if (amt > 0.0f)
                        {
                            amt *= GetConfigBreakSpeed(o.Template);
                            if (amt > 0.0f)
                            {
                                float prev = GetHealth(e);
                                float now = Math.Max(MinHealth, prev - amt);
                                if (now != prev)
                                {
                                    SetHealth(actor, e, now);

                                    if (Settings.DebugMessages && actor.IsPlayer)
                                        DebugMsg("SetHealth(" + o.Template.ToString() + ", " + now + ")");
                                }
                            }
                        }
                        continue;
                    }

                    if (!Settings.EnableWeaponDegradation)
                        continue;

                    var weap = o.Template as TESObjectWEAP;
                    if (weap == null)
                        continue;

                    {
                        float amt = time * Settings.WeaponDegradePassive;
                        if (actor.IsWeaponDrawn)
                            amt = Math.Max(amt, time * Settings.WeaponDegradePassiveOut);

                        if (amt > 0.0f)
                        {
                            amt *= GetConfigBreakSpeed(weap);
                            if (amt > 0.0f)
                            {
                                float prev = GetHealth(e);
                                float now = Math.Max(MinHealth, prev - amt);
                                if (prev != now)
                                {
                                    SetHealth(actor, e, now);

                                    if (Settings.DebugMessages && actor.IsPlayer)
                                        DebugMsg("SetHealth(" + o.Template.ToString() + ", " + now + ")");
                                }
                            }
                        }
                    }
                }
            }
        }

        private static double ChooseForArmorDegrade(TESForm template, bool isHit)
        {
            if (!(template is TESObjectARMO))
                return 0.0;

            var armor = (TESObjectARMO)template;
            var bp = armor.ModelData.Slots;

            if (Settings.ArmorDegradeRestrictNotBoots && isHit && (bp & BGSBipedObjectForm.BipedObjectSlots.Feet) != BGSBipedObjectForm.BipedObjectSlots.None)
                return 0.0;

            /*if ((bp & BGSBipedObjectForm.BipedObjectSlots.Body) != BGSBipedObjectForm.BipedObjectSlots.None)
                return 500.0;
            if ((bp & BGSBipedObjectForm.BipedObjectSlots.Head) != BGSBipedObjectForm.BipedObjectSlots.None)
                return 300.0;
            if ((bp & BGSBipedObjectForm.BipedObjectSlots.Hands) != BGSBipedObjectForm.BipedObjectSlots.None)
                return 300.0;*/

            return 100.0;
        }

        private static void DegradeArmor_Use(Actor victim)
        {
            var cont = victim.Inventory;
            if (cont == null)
                return;

            bool block = (Settings.ArmorDegradeShieldOnlyIfBlocking || Settings.ArmorDegradeWeaponOnlyIfParrying) && Memory.InvokeCdecl(addr_IsBlock, victim.Cast<Actor>()).ToBool();

            var ok = new List<KeyValuePair<KeyValuePair<BSExtraDataList, TESForm>, double>>();
            bool hadWeap = false;
            bool hadShield = false;
            double sum = 0.0f;
            foreach(var o in cont.Objects)
            {
                var ed = o.ExtraData;
                if(ed != null)
                {
                    foreach(var e in ed)
                    {
                        if (Memory.InvokeCdecl(addr_IsWorn, e.Cast<BSExtraDataList>(), 1, 0).ToBool())
                        {
                            if (!CanDegrade(o.Template))
                                break;

                            double weight = ChooseForArmorDegrade(o.Template, true);
                            if (weight > 0.0)
                            {
                                if(block && Settings.ArmorDegradeShieldOnlyIfBlocking)
                                {
                                    var armor = ((TESObjectARMO)o.Template);
                                    if((armor.ModelData.Slots & BGSBipedObjectForm.BipedObjectSlots.Shield) != BGSBipedObjectForm.BipedObjectSlots.None)
                                    {
                                        hadShield = true;
                                        ok.Clear();
                                        ok.Add(new KeyValuePair<KeyValuePair<BSExtraDataList, TESForm>, double>(new KeyValuePair<BSExtraDataList, TESForm>(e, o.Template), weight));
                                        sum = weight;
                                    }
                                }

                                if (!hadShield && !hadWeap)
                                {
                                    ok.Add(new KeyValuePair<KeyValuePair<BSExtraDataList, TESForm>, double>(new KeyValuePair<BSExtraDataList, TESForm>(e, o.Template), weight));
                                    sum += weight;
                                }
                            }
                            else if (block && !hadShield && Settings.ArmorDegradeWeaponOnlyIfParrying && o.Template is TESObjectWEAP && ((TESObjectWEAP)o.Template).WeaponData.AnimationType != WeaponTypes8.Staff)
                            {
                                if(!hadWeap)
                                {
                                    hadWeap = true;
                                    ok.Clear();
                                    sum = 0.0;
                                }

                                ok.Add(new KeyValuePair<KeyValuePair<BSExtraDataList, TESForm>, double>(new KeyValuePair<BSExtraDataList, TESForm>(e, o.Template), 100.0));
                                sum += 100.0;
                            }
                        }
                    }
                }
            }

            if (ok.Count == 0)
                return;
            
            if (Settings.ArmorDegradeRandom)
            {
                var c = NetScriptFramework.Tools.Randomizer.NextEntry(ok, ref sum, false);

                if (c.Key != null)
                {
                    float prev = GetHealth(c.Key);
                    float cur = Math.Max(MinHealth, prev - Settings.ArmorDegradeOnHit * GetConfigBreakSpeed(c.Value));

                    if (cur != prev)
                    {
                        SetHealth(victim, c.Key, cur);

                        if (Settings.DebugMessages && victim.IsPlayer)
                            DebugMsg("SetHealth(" + c.Value.ToString() + ", " + cur + ")");
                    }
                }
            }
            else
            {
                foreach(var t in ok)
                {
                    float prev = GetHealth(t.Key.Key);
                    float cur = Math.Max(MinHealth, prev - Settings.ArmorDegradeOnHit * GetConfigBreakSpeed(t.Key.Value));

                    if (cur != prev)
                    {
                        SetHealth(victim, t.Key.Key, cur);

                        if (Settings.DebugMessages && victim.IsPlayer)
                            DebugMsg("SetHealth(" + t.Key.Value.ToString() + ", " + cur + ")");
                    }
                }
            }
        }

        private static void DegradeWeapon_Use(Actor owner, ExtraContainerChanges.ItemEntry item, float mult)
        {
            float prev = GetHealth(item);
            float now = Math.Max(MinHealth, prev - Settings.WeaponDegradeOnHit * GetConfigBreakSpeed(item.Template) * mult);

            if (now != prev)
            {
                SetHealth(owner, item, now);

                if (Settings.DebugMessages && owner.IsPlayer)
                    DebugMsg("SetHealth(" + item.Template.ToString() + ", " + now + ")");
            }
        }

        internal static bool CanDegrade(TESForm template)
        {
            if (template == null)
                return false;

            var armor = template as TESObjectARMO;
            if (armor != null)
            {
                if (!Settings.EnableArmorDegradation)
                    return false;

                int tries = 0;
                while (tries++ < 10)
                {
                    var a = armor.TemplateArmor;
                    if (a == null)
                        break;

                    armor = a;
                }

                if (Settings.OnlyDegradeTemperableItems)
                {
                    if (!_temperableCache.IsTemperable(armor))
                        return false;
                }

                if(!Settings.ArmorDegradeNonArmor)
                {
                    switch(armor.ModelData.ArmorType)
                    {
                        case BGSBipedObjectForm.ArmorTypes.HeavyArmor:
                        case BGSBipedObjectForm.ArmorTypes.LightArmor:
                            break;

                        default:
                            return false;
                    }
                }

                if(Settings.ArmorDegradeRestrictSlot)
                {
                    if ((armor.ModelData.Slots & (BGSBipedObjectForm.BipedObjectSlots.Head | BGSBipedObjectForm.BipedObjectSlots.Circlet | BGSBipedObjectForm.BipedObjectSlots.Hair | BGSBipedObjectForm.BipedObjectSlots.Body | BGSBipedObjectForm.BipedObjectSlots.Feet | BGSBipedObjectForm.BipedObjectSlots.Hands | BGSBipedObjectForm.BipedObjectSlots.Shield)) == BGSBipedObjectForm.BipedObjectSlots.None)
                        return false;
                }

                return true;
            }

            var weap = template as TESObjectWEAP;
            if(weap != null)
            {
                if (!Settings.EnableWeaponDegradation)
                    return false;

                int tries = 0;
                while(tries++ < 10)
                {
                    var w = weap.TemplateWeapon;
                    if (w == null)
                        break;

                    weap = w;
                }

                if(Settings.OnlyDegradeTemperableItems)
                {
                    if (!_temperableCache.IsTemperable(weap))
                        return false;
                }

                switch(weap.WeaponData.AnimationType)
                {
                    case WeaponTypes8.Staff:
                        return false;
                }

                return true;
            }

            return false;
        }

        private static float accumulated_Self;
        private static float accumulated_NPC;
        private static bool has_passive_degrade;

        private static void update_frame()
        {
            if (gamemain.Instance.IsGamePaused)
                return;

            float diff = Memory.ReadFloat(addr_TimeSinceFrame);
            if (diff <= 0.0f)
                return;

            if(Settings.DebugOption)
            {
                if (NetScriptFramework.Tools.Input.IsPressed(NetScriptFramework.Tools.VirtualKeys.G))
                {
                    if (!_did_g)
                    {
                        _did_g = true;

                        var plr = PlayerCharacter.Instance;
                        var inv = plr.Inventory;
                        if (inv != null)
                        {
                            foreach (var o in inv.Objects)
                            {
                                var bsl = o.ExtraData;
                                if (bsl == null)
                                    continue;

                                foreach (var el in bsl)
                                {
                                    if (!Memory.InvokeCdecl(addr_IsWorn, el.Cast<BSExtraDataList>(), 1, 0).ToBool())
                                        continue;

                                    var form = o.Template;
                                    if (form is TESObjectARMO)
                                    {
                                        var at = ((TESObjectARMO)form).ModelData.ArmorType;
                                        switch (at)
                                        {
                                            case BGSBipedObjectForm.ArmorTypes.HeavyArmor:
                                            case BGSBipedObjectForm.ArmorTypes.LightArmor:
                                                break;

                                            default:
                                                continue;
                                        }
                                    }
                                    else if (form is TESObjectWEAP)
                                    {
                                        if (((TESObjectWEAP)form).WeaponData.AnimationType == WeaponTypes8.Staff)
                                            continue;
                                    }
                                    else
                                        continue;

                                    float amt = Math.Max(MinHealth, (float)NetScriptFramework.Tools.Randomizer.NextDouble());
                                    SetHealth(plr, el, amt);

                                    if(Settings.DebugMessages)
                                        DebugMsg("Set " + form.ToString() + " durability to " + amt);
                                }
                            }
                        }
                    }
                }
                else
                    _did_g = false;
            }

            accumulated_Self += diff;
            accumulated_NPC += diff;

            if (accumulated_Self >= 1.0f)
            {
                var plr = PlayerCharacter.Instance;
                if (plr != null)
                {
                    if(has_passive_degrade)
                        DegradeItems_Passive(plr, accumulated_Self);
                    if (Settings.AutoRestoreNonRepairableItemDurability || Settings.DebugAutoRestoreAllItemDurability)
                        RestoreItems_Passive(plr, accumulated_Self);
                    accumulated_Self = 0.0f;
                }
            }

            if(accumulated_NPC >= 5.0f && Settings.ApplyToNPC)
            {
                if (has_passive_degrade)
                {
                    var cells = TES.Instance.GetLoadedCells();
                    foreach (var c in cells)
                    {
                        c.CellLock.Lock();
                        try
                        {
                            var set = c.References;
                            if (set != null)
                            {
                                foreach (var o in set)
                                {
                                    var actor = o as Actor;
                                    if (actor == null)
                                        continue;

                                    if (actor.IsPlayerTeammate && !actor.IsPlayer)
                                        DegradeItems_Passive(actor, accumulated_NPC);
                                }
                            }
                        }
                        finally
                        {
                            c.CellLock.Unlock();
                        }
                    }
                }

                accumulated_NPC = 0.0f;
            }
        }

        private static bool ShouldShowDurability(IntPtr itemPtr, float health)
        {
            if (itemPtr == IntPtr.Zero)
                return false;

            if (Settings.ShowItemDurabilityInName >= 2)
            {
                return health != 1.0f;
            }
            else
            {
                var item = MemoryObject.FromAddress<TESForm>(itemPtr);
                if (CanDegrade(item))
                    return true;
                
                return false;
            }
        }

        private static ExtraContainerChanges.ItemEntry FindShield(Actor actor)
        {
            var cont = actor.Inventory;
            if (cont == null)
                return null;

            foreach(var o in cont.Objects)
            {
                var ed = o.ExtraData;
                if (ed == null)
                    continue;

                var temp = o.Template as TESObjectARMO;
                if (temp == null)
                    continue;

                if (temp.TemplateArmor != null)
                    temp = temp.TemplateArmor;

                if ((temp.ModelData.Slots & BGSBipedObjectForm.BipedObjectSlots.Shield) == BGSBipedObjectForm.BipedObjectSlots.None)
                    continue;

                foreach(var e in ed)
                {
                    if (!Memory.InvokeCdecl(addr_IsWorn, e.Cast<BSExtraDataList>(), 1, 0).ToBool())
                        continue;

                    return o;
                }
            }

            return null;
        }
        
        private static IntPtr addr_HealthDataSetting;
        private static IntPtr addr_BSSetHealth;
        private static IntPtr addr_BSGetHealth;
        private static IntPtr addr_BSRemoveHealth;
        private static IntPtr addr_SetHealthOnOne;
        private static IntPtr addr_GetEquippedWeap;
        private static IntPtr addr_AddEDList;
        private static IntPtr addr_EDctor;
        private static IntPtr addr_IsWorn;
        private static IntPtr addr_TimeSinceFrame;
        private static IntPtr addr_fsnprintf;
        private static IntPtr addr_UpdateExtraTextDisplayData;
        private static IntPtr addr_otherprintf;
        private static IntPtr addr_IsBlock;
        private static IntPtr addr_GetCurrentAttackData;

        private static IntPtr alloc_DurFmt;
        private static IntPtr alloc_DurFmt2;

        internal static float GetConfigBreakSpeed(TESForm template)
        {
            return _kwModCache.Calculate(template) * 0.01f;
        }

        protected override bool Initialize(bool loadedAny)
        {
            Settings = new Settings();
            Settings.Load();

            _kwModCache.LoadFrom(Settings.KeywordBreakSpeedMult);

            _armorNames = ParseNames("ArmorHealthNames", Settings.ArmorHealthNames);
            _weaponNames = ParseNames("WeaponHealthNames", Settings.WeaponHealthNames);

            addr_GetHealth = main.GameInfo.GetAddressOf(11557);
            addr_HealthDataSetting = main.GameInfo.GetAddressOf(500763);
            addr_BSSetHealth = main.GameInfo.GetAddressOf(11470);
            addr_BSRemoveHealth = main.GameInfo.GetAddressOf(11790);
            addr_SetHealthOnOne = main.GameInfo.GetAddressOf(15905);
            addr_GetEquippedWeap = main.GameInfo.GetAddressOf(38781);
            addr_BSGetHealth = main.GameInfo.GetAddressOf(11557);
            addr_AddEDList = main.GameInfo.GetAddressOf(15748);
            addr_EDctor = main.GameInfo.GetAddressOf(11437);
            addr_IsWorn = main.GameInfo.GetAddressOf(11550);
            addr_TimeSinceFrame = main.GameInfo.GetAddressOf(516940);
            addr_fsnprintf = main.GameInfo.GetAddressOf(12778);
            addr_UpdateExtraTextDisplayData = main.GameInfo.GetAddressOf(15780);
            addr_otherprintf = main.GameInfo.GetAddressOf(10978);
            addr_IsBlock = main.GameInfo.GetAddressOf(36927);
            addr_GetCurrentAttackData = main.GameInfo.GetAddressOf(38530);

            // Enable health of item can go below 1
            {
                // ExtraContainerChanges.ItemEntry::GetHighestHealth
                var addr = main.GameInfo.GetAddressOf(15752, 0, 0, "40 53 48 83 EC 40");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 6,
                    Before = ctx =>
                    {
                        float health = MinHealth;
                        var item = MemoryObject.FromAddress<ExtraContainerChanges.ItemEntry>(ctx.CX);
                        if (item != null)
                        {
                            bool hadAny = false;

                            var ls = item.ExtraData;
                            BSExtraDataList itm;
                            while (ls != null && (itm = ls.Item) != null)
                            {
                                float cur = Memory.InvokeCdeclF(addr_GetHealth, itm.Cast<BSExtraDataList>());
                                if (cur < 0.0f)
                                {
                                    health = Math.Max(health, 1.0f);
                                    hadAny = true;
                                }
                                else
                                {
                                    health = Math.Max(health, cur);
                                    hadAny = true;
                                }

                                ls = ls.Next;
                            }

                            if (!hadAny)
                                health = DefaultHealth;
                        }
                        else
                            health = DefaultHealth;

                        ctx.XMM0f = health;
                    }
                });
                Memory.WriteUInt8(addr + 6, 0xC3, true);

                // TESObjectREFR::GetHealth
                addr = main.GameInfo.GetAddressOf(19777, 0xD, 0, "F3 0F 10 0D");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 8,
                    Before = ctx =>
                    {
                        ctx.XMM1f = MinHealth;
                    }
                });

                // ExtraContainerChanges__ItemEntry::CalculateCost
                addr = main.GameInfo.GetAddressOf(15757, 0x925 - 0x8D0, 0, "F3 0F 10 35");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 8,
                    Before = ctx =>
                    {
                        ctx.XMM6f = 1.0f;

                        float health = ctx.XMM0f;
                        if (health < 0.0f)
                            health = DefaultHealth;
                        else if (health < MinHealth)
                            health = MinHealth;

                        ctx.XMM7f = health;

                        ctx.IP = ctx.IP + (0x42 - 0x2D);
                    }
                });

                // Weapon health modifier calculation?
                addr = main.GameInfo.GetAddressOf(25915, 0, 0, "48 8B 0D");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 7,
                    Before = ctx =>
                    {
                        float health = ctx.XMM0f;
                        if (health < 1.0f)
                        {
                            ctx.XMM0f = health - 2.0f;
                            ctx.IP = ctx.IP + (0x229 - 0x1C7);
                            return;
                        }

                        ctx.CX = Memory.ReadPointer(addr_HealthDataSetting);
                    }
                });

                // Weapon health apply.
                addr = main.GameInfo.GetAddressOf(25847, 0x2E4 - 0x110, 0, "F3 41 0F 58 F1");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 5,
                    Before = ctx =>
                    {
                        float health = ctx.XMM6f;
                        if (health < 0.0f)
                            ctx.XMM6f = (health + 2.0f) * ctx.XMM9f;
                        else
                            ctx.XMM6f = health + ctx.XMM9f;
                    }
                });

                // Armor health modifier calculation?
                addr = main.GameInfo.GetAddressOf(25916, 0, 0, "40 53 48 83 EC 40");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 6,
                    ReplaceLength = 6,
                    Before = ctx =>
                    {
                        float health = ctx.XMM0f;
                        if (health < 1.0f)
                        {
                            ctx.XMM0f = health - 2.0f;
                            ctx.Skip();
                            ctx.IP = ctx.IP + (0x302 - 0x236);
                            return;
                        }
                    }
                });

                // Armor health apply.
                addr = main.GameInfo.GetAddressOf(15779, 0x56E - 0x520, 0, "F3 0F 58 D8 F3 0F 59 DE");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 8,
                    Before = ctx =>
                    {
                        float health = ctx.XMM0f;
                        if (health < 0.0f)
                            ctx.XMM3f = ctx.XMM3f * (health + 2.0f) * ctx.XMM6f;
                        else
                            ctx.XMM3f = (ctx.XMM3f + health) * ctx.XMM6f;
                    }
                });

                // Temper up by increments if broken.
                addr = main.GameInfo.GetAddressOf(50296, 0x411E - 0x3F80, 0, "48 8B 05");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 7,
                    Before = ctx =>
                    {
                        var plr = PlayerCharacter.Instance;
                        ctx.AX = plr != null ? plr.Cast<PlayerCharacter>() : IntPtr.Zero;

                        var bx = ctx.BX;

                        float cur = Memory.ReadFloat(bx + 0x1C);
                        float prev = Memory.ReadFloat(bx + 0x18);

                        if(prev < 1.0f && cur > prev)
                        {
                            float ncur = cur - (1.0f - prev);
                            if (ncur >= prev)
                                Memory.WriteFloat(bx + 0x1C, ncur);
                        }
                    }
                });

                // Temper up allow anyway even if no gain. Due to durability up should be allowed always.
                addr = main.GameInfo.GetAddressOf(50556, 0x3551 - 0x3460, 0, "F3 0F 10 43 24 F3 0F 5C 43 20");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 10,
                    Before = ctx =>
                    {
                        var bx = ctx.BX;
                        float prev = Memory.ReadFloat(bx + 0x18);
                        float after = Memory.ReadFloat(bx + 0x1C);
                        if (after > prev)
                            ctx.XMM0f = 2.0f;
                        else
                            ctx.XMM0f = 0.0f;
                    }
                });
                addr = main.GameInfo.GetAddressOf(50477, 0x6BF - 0x680, 0, "F3 0F 10 43 24 F3 0F 5C 43 20");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 10,
                    Before = ctx =>
                    {
                        var bx = ctx.BX;
                        float prev = Memory.ReadFloat(bx + 0x18);
                        float after = Memory.ReadFloat(bx + 0x1C);
                        if (after > prev)
                            ctx.XMM0f = 2.0f;
                        else
                            ctx.XMM0f = 0.0f;
                    }
                });
                addr = main.GameInfo.GetAddressOf(50529, 0x21A3 - 0x20E0, 0, "F3 0F 10 44 D1 24 F3 0F 5C 44 D1 20");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 12,
                    Before = ctx =>
                    {
                        var bf = ctx.CX + ctx.DX.ToInt32Safe() * 8;
                        float prev = Memory.ReadFloat(bf + 0x18);
                        float after = Memory.ReadFloat(bf + 0x1C);

                        if (after > prev)
                            ctx.XMM0f = 2.0f;
                        else
                            ctx.XMM0f = 0.0f;
                    }
                });
                addr = main.GameInfo.GetAddressOf(50451, 0xCC42 - 0xCC00, 0, "F3 0F 10 41 24 F3 0F 5C 41 20");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 10,
                    Before = ctx =>
                    {
                        var cx = ctx.CX;
                        float prev = Memory.ReadFloat(cx + 0x18);
                        float after = Memory.ReadFloat(cx + 0x1C);
                        if (after > prev)
                            ctx.XMM0f = 2.0f;
                        else
                            ctx.XMM0f = 0.0f;
                    }
                });

                // Item gold value calculation.
                addr = main.GameInfo.GetAddressOf(25901, 0, 0, "48 8B 0D");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 7,
                    Before = ctx =>
                    {
                        float originalValue = ctx.XMM0f;
                        float health = ctx.XMM1f;

                        if (health < 1.0f)
                        {
                            ctx.XMM0f = originalValue * health;
                            ctx.IP = ctx.IP + (0xCAD - 0xC57);
                            return;
                        }

                        ctx.CX = Memory.ReadPointer(addr_HealthDataSetting);
                    }
                });
            }

            // Show item durability.
            if(Settings.ShowItemDurabilityInName > 0 && !string.IsNullOrEmpty(Settings.ItemDurabilityFormat))
            {
                byte[] buf = Encoding.UTF8.GetBytes(" (%s)" + Settings.ItemDurabilityFormat);
                var mem = Memory.Allocate(buf.Length + 2);
                mem.Pin();
                alloc_DurFmt = mem.Address;
                Memory.WriteBytes(alloc_DurFmt, buf);
                Memory.WriteUInt8(alloc_DurFmt + buf.Length, 0);

                buf = Encoding.UTF8.GetBytes("%s (%s)" + Settings.ItemDurabilityFormat);
                mem = Memory.Allocate(buf.Length + 2);
                mem.Pin();
                alloc_DurFmt2 = mem.Address;
                Memory.WriteBytes(alloc_DurFmt2, buf);
                Memory.WriteUInt8(alloc_DurFmt2 + buf.Length, 0);

                var addr = main.GameInfo.GetAddressOf(12633, 0xCF4D - 0xCE10, 0, "E8");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 5,
                    Before = ctx =>
                    {
                        float amt = ctx.XMM6f;
                        if(!ShouldShowDurability(ctx.R14, amt))
                        {
                            Memory.InvokeCdecl(addr_fsnprintf, ctx.CX, ctx.DX, ctx.R8, ctx.R9);
                            return;
                        }
                        
                        ctx.R8 = alloc_DurFmt;

                        if (amt <= MinHealth)
                            amt = 0.0f;

                        byte[] conv = BitConverter.GetBytes((double)(amt * 100.0f));
                        Memory.InvokeCdecl(addr_fsnprintf, ctx.CX, ctx.DX, ctx.R8, ctx.R9, BitConverter.ToInt64(conv, 0));
                    }
                });
                addr = main.GameInfo.GetAddressOf(12633, 0xCFB6 - 0xCE10, 0, "E8");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 5,
                    Before = ctx =>
                    {
                        float amt = ctx.XMM6f;
                        if (!ShouldShowDurability(ctx.R14, amt))
                        {
                            ctx.AX = Memory.InvokeCdecl(addr_otherprintf, ctx.CX, ctx.DX, ctx.R8, ctx.R9);
                            return;
                        }

                        ctx.DX = alloc_DurFmt2;

                        if (amt <= MinHealth)
                            amt = 0.0f;

                        byte[] conv = BitConverter.GetBytes((double)(amt * 100.0f));
                        ctx.AX = Memory.InvokeCdecl(addr_otherprintf, ctx.CX, ctx.DX, ctx.R8, ctx.R9, BitConverter.ToInt64(conv, 0));
                    }
                });
                addr = main.GameInfo.GetAddressOf(12633, 0xCF87 - 0xCE10, 0, "E8");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 5,
                    Before = ctx =>
                    {
                        float amt = ctx.XMM6f;
                        if (!ShouldShowDurability(ctx.R14, amt))
                        {
                            ctx.AX = Memory.InvokeCdecl(addr_otherprintf, ctx.CX, ctx.DX, ctx.R8, ctx.R9);
                            return;
                        }

                        ctx.DX = alloc_DurFmt2;

                        if (amt <= MinHealth)
                            amt = 0.0f;

                        byte[] conv = BitConverter.GetBytes((double)(amt * 100.0f));
                        ctx.AX = Memory.InvokeCdecl(addr_otherprintf, ctx.CX, ctx.DX, ctx.R8, ctx.R9, BitConverter.ToInt64(conv, 0));
                    }
                });
            }

            // Overwrite item health name.
            if(!string.IsNullOrEmpty(Settings.ArmorHealthNames) || !string.IsNullOrEmpty(Settings.WeaponHealthNames))
            {
                var addr = main.GameInfo.GetAddressOf(12786, 0, 0, "33 C9 0F 57 C9");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 5,
                    ReplaceLength = 5,
                    Before = ctx =>
                    {
                        float amt = ctx.XMM0f;
                        bool weap = ctx.DX.ToUInt8() != 0;

                        var ls = weap ? _weaponNames : _armorNames;
                        if (ls == null || ls.Count == 0)
                            return;

                        string msg = null;
                        int chosenIndex = -1;
                        for(int i = 0; i < ls.Count; i++)
                        {
                            var t = ls[i];
                            if(amt >= t.Item1 && amt < t.Item2)
                            {
                                msg = t.Item3;
                                chosenIndex = i;
                                break;
                            }
                        }

                        if (msg == null)
                            return;

                        int pfx = (int)Math.Round(amt * 100.0f);
                        if (msg.Contains("%i"))
                            msg = msg.Replace("%i", pfx.ToString());
                        else
                            pfx = 0;

                        ulong key = BuildCachedNameKey(weap, chosenIndex, pfx);
                        IntPtr res = IntPtr.Zero;
                        lock(_cachedNameLocker)
                        {
                            MemoryAllocation mem;
                            if(!_cachedNameMap.TryGetValue(key, out mem))
                            {
                                byte[] buf = Encoding.UTF8.GetBytes(msg);
                                mem = Memory.Allocate(buf.Length + 1);
                                Memory.WriteBytes(mem.Address, buf);
                                Memory.WriteUInt8(mem.Address + buf.Length, 0);
                                mem.Pin();
                                _cachedNameMap[key] = mem;
                            }

                            res = mem.Address;
                        }

                        ctx.AX = res;
                        ctx.Skip();
                        ctx.IP = ctx.IP + (0x966 - 0x875);
                    }
                });
            }
            
            if(Settings.EnableWeaponDegradation && Settings.WeaponDegradeOnHit > 0.0f)
            {
                if (!Settings.WeaponDegradeRequiresImpact)
                {
                    var addr = main.GameInfo.GetAddressOf(37650, 0xBF5 - 0xB20, 0, "48 8B 8F F0 00 00 00");
                    Memory.WriteHook(new HookParameters()
                    {
                        Address = addr,
                        IncludeLength = 7,
                        ReplaceLength = 7,
                        Before = ctx =>
                        {
                            var actor = MemoryObject.FromAddress<Actor>(ctx.DI);
                            if (actor == null)
                                return;

                            var process = actor.Process;
                            if (process == null)
                                return;

                            if (!Settings.ApplyToNPC && !actor.IsPlayer)
                                return;

                            var ad = Memory.InvokeCdecl(addr_GetCurrentAttackData, process.Cast<ActorProcess>());
                            if (ad == IntPtr.Zero)
                                return;

                            ad = Memory.ReadPointer(ad);
                            if (ad == IntPtr.Zero)
                                return;

                            int flags = Memory.ReadUInt8(ad + 0x28);
                            int arg = (flags >> 3) & 1; // left = 1, right = 0
                            bool powerAttack = ((flags >> 2) & 1) != 0;
                            bool powerBash = ((flags >> 1) & 1) != 0;

                            var item = MemoryObject.FromAddress<ExtraContainerChanges.ItemEntry>(Memory.InvokeCdecl(addr_GetEquippedWeap, process.Cast<ActorProcess>(), arg));

                            if (powerBash && arg == 0)
                            {
                                var shield = FindShield(actor);
                                if (shield != null)
                                    item = shield;
                            }

                            if (item == null)
                                return;

                            if (!CanDegrade(item.Template))
                                return;

                            float mult = 1.0f;
                            if (powerBash)
                                mult = Settings.WeaponDegradePowerBashMult;
                            else if (powerAttack)
                                mult = Settings.WeaponDegradePowerAttackMult;

                            if (Settings.DebugMessages && actor.IsPlayer)
                                DebugMsg("WeaponHit: " + (item.Template != null ? item.Template.ToString() : "null") + "; powerAttack=" + powerAttack + "; powerBash=" + powerBash);

                            DegradeWeapon_Use(actor, item, mult);
                        }
                    });
                }
                else
                {
                    var addr = main.GameInfo.GetAddressOf(37674, 0xA03C - 0x9280, 0, "44 8B 6D B8 41 F6 C5 0E");
                    Memory.WriteHook(new HookParameters()
                    {
                        Address = addr,
                        IncludeLength = 8,
                        ReplaceLength = 8,
                        Before = ctx =>
                        {
                            var victimPtr = ctx.BX;
                            if (victimPtr != IntPtr.Zero && Memory.ReadUInt8(victimPtr + 0x1A) == 62)
                                return;

                            int zfl = Memory.ReadInt32(ctx.BP - 0x48);
                            if ((zfl & 0xE) != 0 && (zfl & 9) != 0)
                                return;

                            var actor = MemoryObject.FromAddress<Actor>(ctx.R15);

                            if (actor == null)
                                return;

                            var process = actor.Process;
                            if (process == null)
                                return;

                            if (!Settings.ApplyToNPC && !actor.IsPlayer)
                                return;

                            var ad = Memory.InvokeCdecl(addr_GetCurrentAttackData, process.Cast<ActorProcess>());
                            if (ad == IntPtr.Zero)
                                return;

                            ad = Memory.ReadPointer(ad);
                            if (ad == IntPtr.Zero)
                                return;

                            int flags = Memory.ReadUInt8(ad + 0x28);
                            int arg = (flags >> 3) & 1; // left = 1, right = 0
                            bool powerAttack = ((flags >> 2) & 1) != 0;
                            bool powerBash = ((flags >> 1) & 1) != 0;

                            var item = MemoryObject.FromAddress<ExtraContainerChanges.ItemEntry>(Memory.InvokeCdecl(addr_GetEquippedWeap, process.Cast<ActorProcess>(), arg));
                            
                            if (powerBash && arg == 0)
                            {
                                var shield = FindShield(actor);
                                if (shield != null)
                                    item = shield;
                            }

                            if (item == null)
                                return;

                            if (!CanDegrade(item.Template))
                                return;

                            float mult = 1.0f;
                            if (powerBash)
                                mult = Settings.WeaponDegradePowerBashMult;
                            else if (powerAttack)
                                mult = Settings.WeaponDegradePowerAttackMult;

                            if (Settings.DebugMessages && actor.IsPlayer)
                                DebugMsg("WeaponHit: " + (item.Template != null ? item.Template.ToString() : "null") + "; powerAttack=" + powerAttack + "; powerBash=" + powerBash);

                            DegradeWeapon_Use(actor, item, mult);
                        }
                    });

                    addr = main.GameInfo.GetAddressOf(37650, 0x7E9E - 0x7B20, 0, "45 0F B6 CF 45 33 C0");
                    Memory.WriteHook(new HookParameters()
                    {
                        Address = addr,
                        IncludeLength = 7,
                        ReplaceLength = 7,
                        Before = ctx =>
                        {
                            var actor = MemoryObject.FromAddress<Actor>(ctx.DI);

                            if (actor == null)
                                return;

                            var process = actor.Process;
                            if (process == null)
                                return;

                            if (!Settings.ApplyToNPC && !actor.IsPlayer)
                                return;

                            var ad = Memory.InvokeCdecl(addr_GetCurrentAttackData, process.Cast<ActorProcess>());
                            if (ad == IntPtr.Zero)
                                return;

                            ad = Memory.ReadPointer(ad);
                            if (ad == IntPtr.Zero)
                                return;

                            int flags = Memory.ReadUInt8(ad + 0x28);
                            int arg = (flags >> 3) & 1; // left = 1, right = 0
                            bool powerAttack = ((flags >> 2) & 1) != 0;
                            bool powerBash = ((flags >> 1) & 1) != 0;

                            var item = MemoryObject.FromAddress<ExtraContainerChanges.ItemEntry>(Memory.InvokeCdecl(addr_GetEquippedWeap, process.Cast<ActorProcess>(), arg));

                            if (powerBash && arg == 0)
                            {
                                var shield = FindShield(actor);
                                if (shield != null)
                                    item = shield;
                            }

                            if (item == null)
                                return;

                            if (!CanDegrade(item.Template))
                                return;

                            float mult = 1.0f;
                            if (powerBash)
                                mult = Settings.WeaponDegradePowerBashMult;
                            else if (powerAttack)
                                mult = Settings.WeaponDegradePowerAttackMult;

                            if (Settings.DebugMessages && actor.IsPlayer)
                                DebugMsg("WeaponHit: " + (item.Template != null ? item.Template.ToString() : "null") + "; powerAttack=" + powerAttack + "; powerBash=" + powerBash);

                            DegradeWeapon_Use(actor, item, mult);
                        }
                    });
                }

                Events.OnWeaponFireProjectilePosition.Register(e =>
                {
                    var actor = e.Attacker as Actor;
                    if (actor == null)
                        return;

                    var process = actor.Process;
                    if (process == null)
                        return;

                    if (!Settings.ApplyToNPC && !actor.IsPlayer)
                        return;

                    var item = MemoryObject.FromAddress<ExtraContainerChanges.ItemEntry>(Memory.InvokeCdecl(addr_GetEquippedWeap, process.Cast<ActorProcess>(), 0));
                    if (item == null)
                    {
                        item = MemoryObject.FromAddress<ExtraContainerChanges.ItemEntry>(Memory.InvokeCdecl(addr_GetEquippedWeap, process.Cast<ActorProcess>(), 1));
                        if (item == null)
                            return;
                    }

                    if (!CanDegrade(item.Template))
                        return;

                    if (Settings.DebugMessages && actor.IsPlayer)
                        DebugMsg("WeaponFire: " + (item.Template != null ? item.Template.ToString() : "null"));

                    DegradeWeapon_Use(actor, item, 1.0f);
                });
            }

            if(Settings.EnableArmorDegradation && Settings.ArmorDegradeOnHit > 0.0f)
            {
                var addr = main.GameInfo.GetAddressOf(37673, 0xEB7 - 0xE10, 0, "48 8B 8B F0 00 00 00");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 7,
                    ReplaceLength = 7,
                    Before = ctx =>
                    {
                        var victim = MemoryObject.FromAddress<Actor>(ctx.DI);
                        //var attacker = MemoryObject.FromAddress<Actor>(ctx.BX);
                        //byte hand = ctx.BP.ToUInt8();

                        if (victim != null)
                        {
                            if (!Settings.ApplyToNPC && !victim.IsPlayer)
                                return;

                            if (Settings.DebugMessages && victim.IsPlayer)
                                DebugMsg("ArmorHit");
                            
                            DegradeArmor_Use(victim);
                        }
                    }
                });
            }

            bool hasUpdate = false;

            if(Settings.EnableArmorDegradation)
            {
                if (Settings.ArmorDegradePassive > 0.0f)
                    hasUpdate = true;
            }

            if(!hasUpdate && Settings.EnableWeaponDegradation)
            {
                if (Settings.WeaponDegradePassive > 0.0f || Settings.WeaponDegradePassiveOut > 0.0f)
                    hasUpdate = true;
            }

            if (!hasUpdate && (Settings.DebugOption || Settings.AutoRestoreNonRepairableItemDurability || Settings.DebugAutoRestoreAllItemDurability))
                hasUpdate = true;

            if(hasUpdate)
            {
                has_passive_degrade = Settings.ArmorDegradePassive > 0.0f || Settings.BootsDegradePassive > 0.0f || Settings.WeaponDegradePassive > 0.0f || Settings.WeaponDegradePassiveOut > 0.0f;
                Events.OnFrame.Register(e =>
                {
                    update_frame();
                });
            }

            if(Settings.OnlyDegradeTemperableItems)
            {
                Events.OnMainMenu.Register(e =>
                {
                    _temperableCache.Rebuild();
                }, 10000, 1);
            }

            return true;
        }
    }
}
