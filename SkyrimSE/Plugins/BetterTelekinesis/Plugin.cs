using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework;
using NetScriptFramework.SkyrimSE;
using gamemain = NetScriptFramework.SkyrimSE.Main;

namespace BetterTelekinesis
{
    public class BetterTelekinesisPlugin : Plugin
    {
        public override string Key
        {
            get
            {
                return "better_telekinesis";
            }
        }

        public override string Name
        {
            get
            {
                return "Better Telekinesis";
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

        public static Settings SettingsInstance
        {
            get;
            private set;
        }

        private static int HeldUpdateCounter = 0;

        private static double Time = 0.0;

        //private static int _dbg_counter = 0;

        private static IntPtr addr_TeleDamBase;
        private static IntPtr addr_TeleDamMult;
        private static IntPtr addr_ApplyPerk;
        private static IntPtr addr_LastCanResult;
        private static IntPtr addr_CanBeTelekinesis;
        private static IntPtr addr_ClearGrabbed;
        private static IntPtr addr_IsDualCasting;
        private static IntPtr addr_TimeSinceFrame;
        private static IntPtr addr_GetAggression;
        private static IntPtr addr_OnAttacked;
        private static IntPtr addr_GetCollisionObject;
        internal static IntPtr addr_GetbhkColl;
        internal static IntPtr addr_GetbhkRigid;
        private static IntPtr addr_GetRigidBody;
        private static IntPtr addr_SetAngularVelocity;
        private static IntPtr addr_PickDistance;
        private static IntPtr addr_SetPosButReallyGoodly;
        private static IntPtr addr_TaskPool;
        private static IntPtr addr_DisarmTask;
        
        protected override bool Initialize(bool loadedAny)
        {
            SettingsInstance = new Settings();
            SettingsInstance.Load();

            debug_msg = SettingsInstance.DebugMessageMode;

            addr_TeleDamBase = gi(506190, 8);
            addr_TeleDamMult = gi(506186, 8);
            addr_ApplyPerk = gi(23073);
            addr_LastCanResult = gi(516696);
            addr_CanBeTelekinesis = gi(33822);
            addr_ClearGrabbed = gi(39480);
            addr_IsDualCasting = gi(37815);
            addr_TimeSinceFrame = gi(516940);
            addr_GetAggression = gi(36663);
            addr_OnAttacked = gi(37672);
            addr_GetCollisionObject = gi(25482);
            addr_GetRigidBody = gi(12784);
            addr_SetAngularVelocity = gi(76260);
            addr_PickDistance = gi(502526, 8);
            addr_GetbhkColl = gi(12787);
            addr_GetbhkRigid = gi(12784);
            addr_SetPosButReallyGoodly = gi(56227);
            addr_TaskPool = gi(517228);
            addr_DisarmTask = gi(36010);

            Events.OnMainMenu.Register(e =>
            {
                spellInfos = new spell_info[(int)spell_types.max];
                spellInfos[(int)spell_types.reach] = new spell_info(spell_types.reach).Load(SettingsInstance.SpellInfo_Reach, "SpellInfo_Reach");
                spellInfos[(int)spell_types.normal] = new spell_info(spell_types.normal).Load(";;", "SpellInfo_Normal");
                spellInfos[(int)spell_types.single] = new spell_info(spell_types.single).Load(SettingsInstance.SpellInfo_One, "SpellInfo_One");
                spellInfos[(int)spell_types.enemy] = new spell_info(spell_types.enemy).Load(SettingsInstance.SpellInfo_NPC, "SpellInfo_NPC");
                spellInfos[(int)spell_types.blast] = new spell_info(spell_types.blast).Load(SettingsInstance.SpellInfo_Blast, "SpellInfo_Blast");
                spellInfos[(int)spell_types.barrage] = new spell_info(spell_types.barrage).Load(SettingsInstance.SpellInfo_Barr, "SpellInfo_Barr");

                for(int i = 0; i < spellInfos.Length; i++)
                {
                    var b = spellInfos[i].SpellBook;
                    if (b != null)
                        leveled_list_helper.AddToLeveledList(b);
                }

                var cac = CachedFormList.TryParse(SettingsInstance.EffectInfo_Forms, "BetterTelekinesis", "EffectInfo_Forms", true, false);
                if(cac != null)
                {
                    foreach(var x in cac.All)
                    {
                        var ef = x as TESEffectShader;
                        if (ef != null)
                            EffectInfos.Add(ef);
                    }
                }

                cac = CachedFormList.TryParse(SettingsInstance.SwordReturn_Marker, "BetterTelekinesis", "SwordReturn_Marker", true, false);
                if (cac != null && cac.All.Count != 0)
                    sword_ReturnMarker = cac.All[0] as TESObjectREFR;

                InitSwords();
            }, 0, 1);
            
            Events.OnFrame.Register(e =>
            {
                float diff = Memory.ReadFloat(addr_TimeSinceFrame);
                Time += diff;

                _reach_spell = 0.0f;
                casting_sword_barrage = false;
                casting_normal = false;
                var plr = PlayerCharacter.Instance;
                if(plr != null)
                {
                    float dist_telek = plr.TelekinesisDistance;
                    if(dist_telek > 0.0f)
                    {
                        var efls = plr.ActiveEffects;
                        if(efls != null)
                        {
                            foreach(var x in efls)
                            {
                                var st = IsOurSpell(x.BaseEffect);
                                if (st == OurSpellTypes.TelekReach)
                                {
                                    _reach_spell = dist_telek;
                                }
                                else if (st == OurSpellTypes.SwordBarrage)
                                {
                                    casting_sword_barrage = true;
                                }
                                else if (st == OurSpellTypes.SwordBlast)
                                    continue;
                                else if (x is TelekinesisEffect)
                                    casting_normal = true;
                            }
                        }
                    }

                    var casters = new MagicCaster[] { plr.GetMagicCaster(EquippedSpellSlots.LeftHand), plr.GetMagicCaster(EquippedSpellSlots.RightHand) };
                    if(casters[0] != null && casters[1] != null && casters[0].State != MagicCastingStates.None && casters[1].State != MagicCastingStates.None)
                    {
                        var items = new MagicItem[] { casters[0].CastItem, casters[1].CastItem };
                        if(items[0] != null && items[1] != null)
                        {
                            int ourMode = 0;
                            for(int i = 0; i < 2; i++)
                            {
                                bool has = false;
                                var itm = items[i];
                                var effls = itm.Effects;
                                if(effls != null)
                                {
                                    foreach(var x in effls)
                                    {
                                        var fs = x.Effect;
                                        if (fs == null)
                                            continue;

                                        if(fs.Archetype == Archetypes.Telekinesis || fs.Archetype == Archetypes.GrabActor || IsOurSpell(fs) != OurSpellTypes.None)
                                        {
                                            has = true;
                                            break;
                                        }
                                    }
                                }

                                if (has)
                                    ourMode |= 1 << i;
                            }

                            if(ourMode == 3)
                            {
                                casters[0].InterruptCast();
                                casters[1].InterruptCast();
                            }
                        }
                    }
                }

                lock (CachedHandlesLocker)
                {
                    int counter = unchecked(++HeldUpdateCounter);

                    var ef = GetCurrentRelevantActiveEffects();
                    foreach (var x in ef)
                    {
                        uint handleId = Memory.ReadUInt32(x.Cast<ActiveEffect>() + 0xA0);
                        if (handleId == 0)
                            continue;

                        held_obj_data od = null;
                        if(!CachedHeldHandles.TryGetValue(handleId, out od))
                        {
                            od = new held_obj_data();
                            od.ObjectHandleId = handleId;
                            od.IsActor = x is GrabActorEffect;
                            od.Effect = x.BaseEffect;
                            CachedHeldHandles[handleId] = od;

                            sword_instance sw;
                            if (normal_swords.lookup.TryGetValue(handleId, out sw))
                            {
                                sw.Held = true;
                                sw.HeldTime = Time;
                                if (normal_swords.forced_grab == sw)
                                    normal_swords.forced_grab = null;
                            }
                            else if (ghost_swords.lookup.TryGetValue(handleId, out sw))
                            {
                                sw.Held = true;
                                sw.HeldTime = Time;
                                if (ghost_swords.forced_grab == sw)
                                    ghost_swords.forced_grab = null;
                            }
                        }
                        else
                        {
                            sword_instance sw;
                            if (normal_swords.lookup.TryGetValue(handleId, out sw))
                                sw.HeldTime = Time;
                            else if (ghost_swords.lookup.TryGetValue(handleId, out sw))
                                sw.HeldTime = Time;
                        }
                        od.Elapsed += diff;
                        od.__update_counter = counter;
                    }

                    List<uint> rem = null;
                    foreach(var pair in CachedHeldHandles)
                    {
                        if(pair.Value.__update_counter != counter)
                        {
                            if (rem == null)
                                rem = new List<uint>();
                            rem.Add(pair.Key);
                            continue;
                        }

                        using (var objHolder = new ObjectRefHolder(pair.Value.ObjectHandleId))
                        {
                            if (objHolder.IsValid)
                                update_held_object(objHolder.Object, pair.Value, ef);
                            else
                            {
                                if (rem == null)
                                    rem = new List<uint>();
                                rem.Add(pair.Key);
                            }
                        }
                    }

                    if(rem != null)
                    {
                        foreach (var u in rem)
                        {
                            CachedHeldHandles.Remove(u);

                            sword_instance sw;
                            if (normal_swords.lookup.TryGetValue(u, out sw))
                                sw.Held = false;
                            else if (ghost_swords.lookup.TryGetValue(u, out sw))
                                sw.Held = false;
                        }
                    }
                }

                UpdateSwordEffects();
            }, -100);

            // Allow launch object even if not pulled completely.
            {
                var addr = gi(34250, 0x332 - 0x250, "80 B8 A8 00 00 00 00");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 7,
                    ReplaceLength = 7,
                    Before = ctx =>
                    {
                        var eff = MemoryObject.FromAddress<ActiveEffect>(ctx.AX);
                        int launch = ShouldLaunchObjectNow(eff);
                        if(launch > 0)
                        {
                            ctx.Skip();
                            ctx.IP = ctx.IP + 6;
                        }
                        else if(launch < 0)
                        {
                            ctx.Skip();
                            ctx.IP = ctx.IP + (0x4CB - 0x339);
                        }
                    },
                });
            }

            // Allow reach spell.
            {
                var addr = gi(25591, 0xB2E1 - 0xA6A0, "F3 0F 10 05");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 8,
                    Before = ctx =>
                    {
                        float fpick = Memory.ReadFloat(addr_PickDistance);
                        ctx.XMM0f = Math.Max(fpick, _reach_spell);
                    }
                });
            }

            /*{
                var hkdebug = new HotkeyPress(() =>
                {
                    if (NetScriptFramework.SkyrimSE.Main.Instance.IsGamePaused)
                        return;

                    var plr = PlayerCharacter.Instance;
                    var all = DataHandler.Instance.GetAllFormsByType(FormTypes.EffectShader).ToList();
                    int index = _dbg_counter++;
                    if (index >= all.Count)
                        WriteDebugMsg("No more found");
                    else
                    {
                        plr.PlayEffect((TESEffectShader)all[index], 3.0f);
                        WriteDebugMsg("Now: " + all[index].ToString());
                    }

                }, NetScriptFramework.Tools.VirtualKeys.G);
                hkdebug.Register();
            }*/
            
            /*if ((debug_msg & 8) != 0)
            {
                var hkdebug = new HotkeyPress(() =>
                {
                    uint refh = CrossHairPickData.Instance.TargetRefHandle;
                    if (refh == 0)
                    {
                        WriteDebugMsg("null ref");
                        return;
                    }

                    using (var objRefHold = new ObjectRefHolder(refh))
                    {
                        if (!objRefHold.IsValid)
                        {
                            WriteDebugMsg("bad ref");
                            return;
                        }

                        var root = objRefHold.Object.Node;
                        if (root == null)
                        {
                            WriteDebugMsg("root null");
                            return;
                        }

                        var n = find_nearest_node_helper.FindBestNodeInCrosshair(root);
                        if (n == null)
                        {
                            WriteDebugMsg("best null");
                            return;
                        }

                        WriteDebugMsg(n.ToString());
                    }
                }, NetScriptFramework.Tools.VirtualKeys.G);
                hkdebug.Register();
            }*/

            // Can't work, projectiles are handled too different in havok.
            //if(SettingsInstance.AllowProjectileTelekinesis)
            {
                /*Memory.WriteHook(new HookParameters()
                {
                    Address = new IntPtr(0x140556CA6).FromBase(),
                    IncludeLength = 6,
                    ReplaceLength = 6,
                    Before = ctx =>
                    {
                        if (ctx.AX.ToUInt32() == 4)
                            ctx.AX = new IntPtr(3);
                    }
                });

                // 83 C0 FC 83 F8 01
                Memory.WriteHook(new HookParameters()
                {
                    Address = new IntPtr(0x1406AB9F9).FromBase(),
                    IncludeLength = 6,
                    ReplaceLength = 6,
                    Before = ctx =>
                    {
                        if (ctx.AX.ToUInt32() == 4)
                            ctx.AX = new IntPtr(3);
                    }
                });

                Memory.WriteHook(new HookParameters()
                {
                    Address = new IntPtr(0x1406AB19F).FromBase(),
                    IncludeLength = 6,
                    ReplaceLength = 6,
                    After = ctx =>
                    {
                        ctx.AX = IntPtr.Zero;
                    }
                });*/

                /*var addr = gi(33822, 0xCC6 - 0xC30, "40 0F B6 FF 8D 48 FC B8 01 00 00 00 3B C8 0F 46 F8");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 0x11,
                    Before = ctx =>
                    {
                        CollisionLayers layer = (CollisionLayers)ctx.AX.ToInt32Safe();
                        bool ok = false;
                        switch(layer)
                        {
                            // Default game only allows clutter or weapon.
                            case CollisionLayers.Clutter:
                            case CollisionLayers.Weapon:
                                ok = true;
                                break;

                            case CollisionLayers.Projectile:
                                ok = true;
                                break;
                        }

                        ctx.DI = new IntPtr(ok ? 1 : 0);
                    }
                });*/

                // Since all the ones we want to allow are sequential we can only change the comparison operand.
                //var addr = gi(33822, 0xCCD - 0xC30, "B8 01 00 00 00");
                //Memory.WriteUInt8(addr + 1, 2, true);
            }

            if(SettingsInstance.DontDeactivateHavokHoldSpring)
            {
                // Don't allow spring to deactivate.
                var addr = gi(61571, 0x9AE - 0x980, "74");
                Memory.WriteNop(addr, 2);
            }

            // Allow dragons to be grabbed. This will cause issues due to end of ragdoll dragon gets stuck without any collision at all.
            if(SettingsInstance.FixDragonsNotBeingTelekinesisable)
            {
                var addr = gi(39197, 0, "48 89 6C 24 10");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 5,
                    Before = ctx =>
                    {
                        //var actor = MemoryObject.FromAddress<Actor>(ctx.CX);
                        var root = MemoryObject.FromAddress<NiAVObject>(ctx.DX) as NiNode;
                        if (root != null)
                        {
                            if (find_collision_node(root))
                                ctx.AX = new IntPtr(1);
                            else
                                ctx.AX = IntPtr.Zero;
                        }
                        else
                            ctx.AX = IntPtr.Zero;
                    }
                });

                Memory.WriteUInt8(addr + 5, 0xC3, true);
            }

            Events.OnMainMenu.Register(e => apply_good_stuff(), 0, 1);

            if(SettingsInstance.AutoLearnTelekinesisVariants)
            {
                Events.OnFrame.Register(e =>
                {
                    var prim = PrimarySpells;
                    if (prim == null)
                        return;

                    var second = SecondarySpells;
                    if (second == null)
                        return;

                    int now = Environment.TickCount;
                    if (unchecked(now - _last_check_learn2) < 1000)
                        return;

                    _last_check_learn2 = now;

                    var main = NetScriptFramework.SkyrimSE.Main.Instance;
                    if (main == null || main.IsGamePaused)
                        return;

                    var plr = PlayerCharacter.Instance;
                    if (plr == null)
                        return;

                    bool has = false;
                    foreach (var form in prim.All)
                    {
                        var sp = form as SpellItem;
                        if (sp == null)
                            continue;

                        if (plr.HasSpell(sp))
                        {
                            has = true;
                            break;
                        }
                    }

                    if (!has)
                        return;

                    foreach(var form in second.All)
                    {
                        var sp = form as SpellItem;
                        if (sp == null)
                            continue;

                        if (!plr.HasSpell(sp))
                            plr.AddSpell(sp, true);
                    }
                });
            }

            if (SettingsInstance.AutoLearnTelekinesisSpell)
            {
                Events.OnFrame.Register(e =>
                {
                    var spells = Spells;
                    if (spells == null || spells.All.Count == 0)
                        return;

                    int now = Environment.TickCount;
                    if (unchecked(now - _last_check_learn) < 1000)
                        return;

                    _last_check_learn = now;

                    var main = NetScriptFramework.SkyrimSE.Main.Instance;
                    if (main == null || main.IsGamePaused)
                        return;

                    var plr = PlayerCharacter.Instance;
                    if (plr == null)
                        return;

                    foreach (var form in spells.All)
                    {
                        var sp = form as SpellItem;
                        if (sp == null)
                            continue;

                        if (!plr.HasSpell(sp))
                            plr.AddSpell(sp, true);
                    }
                });
            }
            
            if (SettingsInstance.GrabActorNodeNearest || (!string.IsNullOrEmpty(SettingsInstance.GrabActorNodePriority) && !SettingsInstance.GrabActorNodePriority.Equals("NPC Spine2 [Spn2]", StringComparison.OrdinalIgnoreCase)))
            {
                var spl = SettingsInstance.GrabActorNodeNearest ? null : ((SettingsInstance.GrabActorNodePriority ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
                if (spl != null && spl.Length != 0)
                    grabActorNodes = spl.ToList();

                spl = !SettingsInstance.GrabActorNodeNearest ? null : ((SettingsInstance.GrabActorNodeExclude ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
                if(spl != null && spl.Length != 0)
                {
                    ExcludeActorNodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var x in spl)
                        ExcludeActorNodes.Add(x);
                }

                if (SettingsInstance.GrabActorNodeNearest || grabActorNodes != null)
                {
                    var addr = gi(33826, 0, "40 57 48 83 EC 30");
                    Memory.WriteHook(new HookParameters()
                    {
                        Address = addr,
                        IncludeLength = 0,
                        ReplaceLength = 6,
                        Before = ctx =>
                        {
                            var obj = MemoryObject.FromAddress<TESObjectREFR>(ctx.CX);
                            if (obj != null)
                            {
                                var root = obj.Node;
                                if (root != null)
                                {
                                    if(SettingsInstance.GrabActorNodeNearest)
                                    {
                                        var sel = find_nearest_node_helper.FindBestNodeInCrosshair(root);
                                        if(sel != null)
                                        {
                                            if((debug_msg & 8) != 0)
                                                WriteDebugMsg("Picked up by " + sel.ToString());
                                            ctx.AX = sel.Cast<NiAVObject>();
                                            return;
                                        }

                                        ctx.AX = root.Cast<NiAVObject>();
                                        return;
                                    }

                                    foreach (var x in grabActorNodes)
                                    {
                                        var node = root.LookupNodeByName(x);
                                        if (node != null)
                                        {
                                            ctx.AX = node.Cast<NiAVObject>();
                                            return;
                                        }
                                    }

                                    ctx.AX = root.Cast<NiAVObject>();
                                    return;
                                }
                            }

                            ctx.AX = IntPtr.Zero;
                        }
                    });
                    Memory.WriteUInt8(addr + 6, 0xC3, true);
                }
            }

            if(SettingsInstance.FixSuperHugeTelekinesisDistanceBug)
            {
                var addr = gi(39474, 0x414 - 0x3E0, "F3 0F 10 05");
                Memory.WriteNop(addr, 16);

                addr = gi(39464, 0x116 - 0x050, "F3 0F 10 05");
                Memory.WriteNop(addr, 24);
            }

            if(SettingsInstance.HoldActorDamage > 0.0f)
            {
                Events.OnFrame.Register(e =>
                {
                    var main = NetScriptFramework.SkyrimSE.Main.Instance;
                    if (main == null || main.IsGamePaused)
                        return;

                    float diff = Memory.ReadFloat(addr_TimeSinceFrame);
                    if (diff <= 0.0f)
                        return;

                    var plr = PlayerCharacter.Instance;
                    if (plr == null)
                        return;

                    if (plr.TelekinesisDistance <= 0.0f)
                        return;

                    ForeachHeldHandle(dat =>
                    {
                        if (!dat.IsActor)
                            return;

                        using (var obj = new ObjectRefHolder(dat.ObjectHandleId))
                        {
                            if (obj.IsValid)
                            {
                                var actor = obj.Object as Actor;
                                if (actor != null)
                                {
                                    float dam = CalculateCurrentTelekinesisDamage(plr.Cast<PlayerCharacter>(), actor.Cast<Actor>()) * diff * SettingsInstance.HoldActorDamage;
                                    if (dam > 0.0f)
                                        actor.DamageActorValue(ActorValueIndices.Health, -dam);
                                }
                            }
                        }
                    });
                });
            }

            if (SettingsInstance.OverwriteTargetPicker)
                apply_overwrite_target_pick();
            
            //if(SettingsInstance.TelekinesisMaxObjects > 1)
                apply_multi_telekinesis();

            if(SettingsInstance.TelekinesisMaxObjects > 1 || !SettingsInstance.TelekinesisGrabObjectSound)
            {
                // Probably don't need the grab object timer check here since it's spaced out anyway, but..
                var addr = gi(34259, 0xE1C - 0xDC0, "E8");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 5,
                    ReplaceLength = 5,
                    Before = ctx =>
                    {
                        if (!SettingsInstance.TelekinesisGrabObjectSound)
                        {
                            ctx.Skip();
                            return;
                        }
                        int now = Environment.TickCount;
                        if (unchecked(now - _last_tk_sound2) < 100)
                            ctx.Skip();
                        else
                            _last_tk_sound2 = now;
                    }
                });
            }

            if(SettingsInstance.TelekinesisMaxObjects > 1 || !SettingsInstance.TelekinesisLaunchObjectSound)
            {
                // Don't play telekinesis launch sound if we just played it, otherwise it ends up being playd 10 times and becomes super loud.
                var addr = gi(34250, 0x4C4 - 0x250, "E8");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 5,
                    ReplaceLength = 5,
                    Before = ctx =>
                    {
                        if (!SettingsInstance.TelekinesisLaunchObjectSound)
                        {
                            ctx.Skip();
                            return;
                        }
                        int now = Environment.TickCount;
                        if (unchecked(now - _last_tk_sound) < 200)
                            ctx.Skip();
                        else
                            _last_tk_sound = now;
                    }
                });
            }

            if(SettingsInstance.FixGrabActorHoldHostility)
            {
                var addr = gi(33564, 0xC7C - 0xB40, "E8");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 5,
                    ReplaceLength = 5,
                    Before = ctx =>
                    {
                        var victim = MemoryObject.FromAddress<Actor>(ctx.DX);
                        var plr = PlayerCharacter.Instance;

                        if (victim == null || plr == null)
                            return;

                        int aggression = Memory.InvokeCdecl(addr_GetAggression, victim.Cast<Actor>()).ToInt32Safe();
                        var r8 = Memory.ReadPointer(ctx.SI + 0x48);
                        Memory.InvokeCdecl(addr_OnAttacked, victim.Cast<Actor>(), plr.Cast<Actor>(), r8, aggression);
                    }
                });
            }

            var hk = HotkeyPress.TryCreateHotkey(SettingsInstance.AbortTelekinesisHotkey, _try_drop_now);
            if (hk != null || SettingsInstance.DontLaunchIfRunningOutOfMagicka || SettingsInstance.LaunchIsHotkeyInstead || SettingsInstance.ThrowActorDamage > 0.0f)
            {
                reg_hk(hk);

                // Telekinesis launch
                var addr = gi(34256, 0x1C, "E8");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 5,
                    ReplaceLength = 5,
                    Before = ctx =>
                    {
                        // Always launch sword barrage.
                        var dx = ctx.DX;
                        if(dx != IntPtr.Zero)
                        {
                            dx = Memory.ReadPointer(dx);
                            var ef = MemoryObject.FromAddress<ActiveEffect>(dx);
                            if (ef != null && IsOurSpell(ef.BaseEffect) == OurSpellTypes.SwordBarrage)
                                return;
                        }

                        if (drop_timer.HasValue)
                        {
                            int now = Environment.TickCount;
                            if (unchecked(now - drop_timer.Value) < 200)
                            {
                                if(!SettingsInstance.LaunchIsHotkeyInstead)
                                    ctx.Skip();
                                return;
                            }

                            drop_timer = null;
                        }

                        if(SettingsInstance.LaunchIsHotkeyInstead)
                        {
                            ctx.Skip();
                            return;
                        }

                        if(SettingsInstance.DontLaunchIfRunningOutOfMagicka)
                        {
                            var plr = PlayerCharacter.Instance;
                            if(plr != null && plr.GetActorValue(ActorValueIndices.Magicka) <= 0.01f)
                            {
                                ctx.Skip();
                                return;
                            }
                        }
                    }
                });

                // Grab actor launch
                addr = gi(33559, 0x8AD - 0x730, "E8");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 5,
                    ReplaceLength = 5,
                    Before = ctx =>
                    {
                        if (drop_timer.HasValue)
                        {
                            int now = Environment.TickCount;
                            if (unchecked(now - drop_timer.Value) < 200)
                            {
                                if (!SettingsInstance.LaunchIsHotkeyInstead)
                                    ctx.Skip();
                                else
                                    OnLaunchActor(ctx.DI);
                                return;
                            }

                            drop_timer = null;
                        }

                        if (SettingsInstance.LaunchIsHotkeyInstead)
                        {
                            ctx.Skip();
                            return;
                        }

                        if (SettingsInstance.DontLaunchIfRunningOutOfMagicka)
                        {
                            var plr = PlayerCharacter.Instance;
                            if (plr != null && plr.GetActorValue(ActorValueIndices.Magicka) <= 0.01f)
                            {
                                ctx.Skip();
                                return;
                            }
                        }

                        OnLaunchActor(ctx.DI);
                    }
                });
            }

            return true;
        }

        private static void Debug_on_G_Hk(Action func)
        {
            if (func == null)
                return;

            var hk = new HotkeyPress(() => func(), NetScriptFramework.Tools.VirtualKeys.G);
            hk.Register();
        }

        internal static IntPtr gi(ulong vid, int offset = 0, string pattern = null)
        {
            return NetScriptFramework.Main.GameInfo.GetAddressOf(vid, offset, 0, pattern);
        }

        private static float _reach_spell = 0.0f;

        internal sealed class held_obj_data
        {
            internal uint ObjectHandleId;
            internal EffectSetting Effect;
            internal bool IsActor;
            internal float Elapsed;

            internal int __update_counter = 0;
        }

        internal static readonly Dictionary<uint, held_obj_data> CachedHeldHandles = new Dictionary<uint, held_obj_data>();

        internal static readonly object CachedHandlesLocker = new object();

        internal static void ForeachHeldHandle(Action<held_obj_data> func)
        {
            if (func == null)
                return;

            lock(CachedHandlesLocker)
            {
                foreach(var pair in CachedHeldHandles)
                    func(pair.Value);
            }
        }

        private static float CalculateCurrentTelekinesisDamage(IntPtr ptrPlr, IntPtr actorPtr)
        {
            float damBase = 0.0f;
            float damMult = 1.0f;
            using (var alloc = Memory.Allocate(0x10))
            {
                Memory.WriteFloat(alloc.Address, Memory.ReadFloat(addr_TeleDamBase));
                Memory.InvokeCdecl(addr_ApplyPerk, (int)PerkEntryPoints.ModTelekinesisDamage, ptrPlr, actorPtr, alloc.Address);
                damBase = Memory.ReadFloat(alloc.Address);

                Memory.WriteFloat(alloc.Address, Memory.ReadFloat(addr_TeleDamMult));
                Memory.InvokeCdecl(addr_ApplyPerk, (int)PerkEntryPoints.ModTelekinesisDamageMult, ptrPlr, alloc.Address);
                damMult = Memory.ReadFloat(alloc.Address);
            }

            float damTotal = damBase * damMult;
            return damTotal;
        }

        private static void OnLaunchActor(IntPtr actorPtr)
        {
            if (SettingsInstance.ThrowActorDamage <= 0.0f || actorPtr == IntPtr.Zero || Memory.ReadUInt8(actorPtr + 0x1A) != 62)
                return;

            var plr = PlayerCharacter.Instance;
            if (plr == null)
                return;

            var ptrPlr = plr.Cast<PlayerCharacter>();
            if (ptrPlr == IntPtr.Zero)
                return;

            Actor actor = MemoryObject.FromAddress<Actor>(actorPtr);
            if (actorPtr == null)
                return;

            float damTotal = CalculateCurrentTelekinesisDamage(ptrPlr, actorPtr) * SettingsInstance.ThrowActorDamage;
            if (damTotal > 0.0f)
                actor.DamageActorValue(ActorValueIndices.Health, -damTotal);
        }

        private static bool _did_reg_hk = false;

        private static void reg_hk(HotkeyBase hk)
        {
            if(hk != null)
            {
                if(hk.Register())
                {
                    if(!_did_reg_hk)
                    {
                        _did_reg_hk = true;

                        Events.OnFrame.Register(e => HotkeyBase.UpdateAll());
                    }
                }
            }
        }
        
        private static void write_float(ulong vid, float value)
        {
            if (value < 0.0f)
                return;

            Memory.WriteFloat(gi(vid, 8), value);
        }

        private static void write_float_mult(ulong vid, float value)
        {
            if (value == 1.0f)
                return;

            var addr = gi(vid, 8);
            float prev = Memory.ReadFloat(addr);
            Memory.WriteFloat(addr, value * prev);
        }

        private static int? drop_timer;

        internal static string DefaultResponsiveHoldParameters
        {
            get
            {
                List<float> vls = new List<float>();

                vls.Add(0.75f); // complex spring elasticity, default: 0.1
                vls.Add(0.65f); // normal spring elasticity, default: 0.075

                vls.Add(0.7f); // complex spring damping, default: 0.9
                vls.Add(0.5f); // normal spring damping, default: 0.5

                vls.Add(0.5f); // complex object damping, default: 0.7
                vls.Add(0.5f); // normal object damping, default: 0.95

                vls.Add(1000); // complex max force, default: 1000
                vls.Add(1000); // normal max force, default: 1000

                return string.Join(" ", vls.Select(q => q.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture)));
            }
        }

        internal static CachedFormList Spells;

        internal static CachedFormList PrimarySpells;

        internal static CachedFormList SecondarySpells;

        private static List<string> grabActorNodes;

        internal static HashSet<string> ExcludeActorNodes;

        private static int _last_check_learn;
        private static int _last_check_learn2;

        private static bool find_collision_node(NiNode root, int depth = 0)
        {
            if (root == null)
                return false;

            if (root.Collidable.Value != null)
                return true;

            if(depth < 4)
            {
                var chls = root.Children;
                if(chls != null)
                {
                    foreach(var ch in chls)
                    {
                        var cn = ch as NiNode;
                        if (cn != null && find_collision_node(cn, depth + 1))
                            return true;
                    }
                }
            }

            return false;
        }

        private static void apply_good_stuff()
        {
            write_float_mult(506184, SettingsInstance.BaseDistanceMultiplier);
            write_float_mult(506190, SettingsInstance.BaseDamageMultiplier);
            write_float(506149, SettingsInstance.ObjectPullSpeedBase);
            write_float(506151, SettingsInstance.ObjectPullSpeedAccel);
            write_float(506153, SettingsInstance.ObjectPullSpeedMax);
            write_float_mult(506157, SettingsInstance.ObjectThrowForce);
            write_float(506196, SettingsInstance.ActorPullSpeed);
            write_float_mult(506199, SettingsInstance.ActorThrowForce);
            write_float_mult(506155, SettingsInstance.ObjectHoldDistance);
            write_float_mult(506194, SettingsInstance.ActorHoldDistance);

            find_nearest_node_helper.init();

            if(!string.IsNullOrEmpty(SettingsInstance.TelekinesisSpells))
                Spells = CachedFormList.TryParse(SettingsInstance.TelekinesisSpells, "BetterTelekinesis", "TelekinesisSpells", false);
            if(!string.IsNullOrEmpty(SettingsInstance.TelekinesisPrimary))
                PrimarySpells = CachedFormList.TryParse(SettingsInstance.TelekinesisPrimary, "BetterTelekinesis", "TelekinesisPrimary", false);
            if (!string.IsNullOrEmpty(SettingsInstance.TelekinesisSecondary))
                SecondarySpells = CachedFormList.TryParse(SettingsInstance.TelekinesisSecondary, "BetterTelekinesis", "TelekinesisSecondary", false);

            if (SettingsInstance.OverwriteTelekinesisSpellBaseCost >= 0.0f)
            {
                int cost = (int)Math.Round(SettingsInstance.OverwriteTelekinesisSpellBaseCost);
                if (Spells != null)
                {
                    foreach (var x in Spells.All)
                    {
                        var spell = x as SpellItem;
                        if (spell == null)
                            continue;

                        var ptr = spell.Cast<SpellItem>();
                        uint fl = Memory.ReadUInt32(ptr + 0xC4);
                        Memory.WriteUInt32(ptr + 0xC4, fl | 1);

                        Memory.WriteInt32(ptr + 0xC0, cost);
                    }
                }
            }

            if (SettingsInstance.ResponsiveHold)
            {
                List<float> ls = null;
                for (int i = 0; i < 2; i++)
                {
                    string prls = i == 0 ? (SettingsInstance.ResponsiveHoldParams ?? "") : DefaultResponsiveHoldParameters;
                    var spl = prls.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    ls = new List<float>();
                    foreach(var x in spl)
                    {
                        float fx;
                        if (float.TryParse(x, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out fx))
                            ls.Add(fx);
                        else
                        {
                            ls = null;
                            break;
                        }
                    }

                    if (ls != null && ls.Count == 8)
                        break;
                }

                if (ls != null && ls.Count == 8)
                {
                    // elasticity
                    write_float(506169, ls[0]);
                    write_float(506161, ls[1]);

                    // spring damping
                    write_float(506167, ls[2]);
                    write_float(506159, ls[3]);

                    // object damping
                    write_float(506171, ls[4]);
                    write_float(506163, ls[5]);

                    // max force
                    write_float(506173, ls[6]);
                    write_float(506165, ls[7]);
                }
            }
        }

        private static void _try_drop_now()
        {
            var gameMain = NetScriptFramework.SkyrimSE.Main.Instance;
            if (gameMain == null || gameMain.IsGamePaused)
                return;

            var plr = PlayerCharacter.Instance;
            if (plr == null)
                return;

            bool didTimer = false;
            for (int i = 0; i <= 2; i++)
            {
                var caster = plr.GetMagicCaster((EquippedSpellSlots)i);
                if (caster == null)
                    continue;

                var item = caster.CastItem;
                if (item == null)
                    continue;

                var effls = item.Effects;
                if (effls == null)
                    continue;

                bool had = false;
                foreach (var ef in effls)
                {
                    var set = ef.Effect;
                    if (set == null)
                        continue;

                    switch (set.Archetype)
                    {
                        case Archetypes.Telekinesis:
                        case Archetypes.GrabActor:
                            {
                                had = true;
                                break;
                            }
                    }

                    if (had)
                        break;
                }

                if (!had)
                    continue;

                if (!didTimer)
                {
                    drop_timer = Environment.TickCount;
                    didTimer = true;
                }
                caster.InterruptCast();
            }
        }

        private static TESObjectREFR sword_ReturnMarker;

        private static List<ActiveEffect> GetCurrentRelevantActiveEffects(Predicate<ActiveEffect> conditions = null)
        {
            var ls = new List<ActiveEffect>();

            var plr = PlayerCharacter.Instance;
            if (plr != null)
            {
                var efls = plr.ActiveEffects;
                if(efls != null)
                {
                    foreach(var ef in efls)
                    {
                        if(ef is TelekinesisEffect)
                        {
                            if (conditions == null || conditions(ef))
                                ls.Add(ef);
                        }
                        else if(ef is GrabActorEffect)
                        {
                            if (conditions == null || conditions(ef))
                                ls.Add(ef);
                        }
                    }
                }
            }

            return ls;
        }

        private static int _last_updated_telek = 0;
        private static bool _next_update_telek = false;
        private static bool _last_weap_out = false;

        private static long _total_telek_time = 0;
        private static long _times_telek_time = 0;

        private static void ForceUpdateTelekinesis()
        {
            _next_update_telek = true;
        }

        private static bool ShouldUpdateTelekinesis(int now)
        {
            if(_next_update_telek)
            {
                _next_update_telek = false;
                _last_updated_telek = now;
                return true;
            }

            if(SettingsInstance.TelekinesisTargetOnlyUpdateIfWeaponOut)
            {
                var plr = PlayerCharacter.Instance;
                if (plr != null && plr.IsWeaponDrawn)
                {
                    if (!_last_weap_out)
                    {
                        _last_weap_out = true;
                        _last_updated_telek = now;
                        return true;
                    }
                }
                else
                {
                    _last_weap_out = false;
                    return false;
                }
            }

            int delay = (int)(SettingsInstance.TelekinesisTargetUpdateInterval * 1000.0f);
            if(unchecked(now - _last_updated_telek) >= delay)
            {
                _last_updated_telek = now;
                return true;
            }

            return false;
        }
        
        private static void apply_overwrite_target_pick()
        {
            var addr = gi(39534, 0x5E4 - 0x3D0, "E8");
            Memory.WriteHook(new HookParameters()
            {
                Address = addr,
                IncludeLength = 5,
                ReplaceLength = 5,
                After = ctx =>
                {
                    uint origTelekinesis = Memory.ReadUInt32(ctx.R14 + 0xC);
                    uint chosenTelekinesis = origTelekinesis;
                    lock (locker_picked)
                    {
                        if (ShouldUpdateTelekinesis(Environment.TickCount))
                        {
                            telekinesis_picked.Clear();
                            grabactor_picked.Clear();
                            long bgt = 0;
                            if((debug_msg & 0x20) != 0)
                            {
                                var t = _profile_timer;
                                if(t != null)
                                    bgt = t.ElapsedTicks;
                            }
                            OverwriteTelekinesisTargetPick(origTelekinesis);
                            if((debug_msg & 0x20) != 0)
                            {
                                _total_telek_time += _profile_timer.ElapsedTicks - bgt;
                                if (((_times_telek_time++) % 10) == 1)
                                    WriteDebugMsg("profiler: " + ((double)(_total_telek_time / (System.Diagnostics.Stopwatch.Frequency / 1000)) / (double)_times_telek_time).ToString("0.##") + " <- " + _times_telek_time + " ; " + PlayerCharacter.Instance.TelekinesisDistance);
                            }
                        }

                        switch (SettingsInstance.TelekinesisLabelMode)
                        {
                            case 0:
                                if (chosenTelekinesis != 0 && !telekinesis_picked.Contains(chosenTelekinesis))
                                    chosenTelekinesis = 0;
                                if (chosenTelekinesis != 0 && !HasAnyNormalTelekInHand())
                                    chosenTelekinesis = 0;
                                break;

                            case 1:
                                if (telekinesis_picked.Count != 0)
                                {
                                    chosenTelekinesis = telekinesis_picked[0];
                                    using (var objRef = new ObjectRefHolder(chosenTelekinesis))
                                    {
                                        if (!objRef.IsValid || IsOurItem(objRef.Object.BaseForm) != OurItemTypes.None)
                                            chosenTelekinesis = 0;
                                    }

                                    if (chosenTelekinesis != 0 && !HasAnyNormalTelekInHand())
                                        chosenTelekinesis = 0;
                                }
                                else
                                    chosenTelekinesis = 0;
                                break;

                            case 2:
                                chosenTelekinesis = 0;
                                break;
                        }
                    }

                    if (origTelekinesis != chosenTelekinesis)
                        Memory.WriteUInt32(ctx.R14 + 0xC, chosenTelekinesis);
                }
            });

            addr = gi(34259, 0xD3 - 0xC0, "89 91 A0 00 00 00");
            Memory.WriteHook(new HookParameters()
            {
                Address = addr,
                IncludeLength = 6,
                ReplaceLength = 6,
                Before = ctx =>
                {
                    var ef = MemoryObject.FromAddress<ActiveEffect>(ctx.CX);
                    EffectSetting efs = ef.BaseEffect;

                    bool failBecauseMax = false;
                    uint handleId = 0;
                    int hadObjCount = 0;
                    uint actorHandle = 0;
                    lock (locker_picked)
                    {
                        if (telekinesis_picked.Count != 0)
                        {
                            HashSet<uint> alreadyChosen = null;
                            bool hasBad = false;
                            ForeachHeldHandle(dat =>
                            {
                                if (hasBad)
                                    return;

                                if (efs == null || !efs.Equals(dat.Effect))
                                    hasBad = true;
                                else
                                {
                                    if (alreadyChosen == null)
                                        alreadyChosen = new HashSet<uint>();
                                    alreadyChosen.Add(dat.ObjectHandleId);
                                    hadObjCount++;
                                }
                            });

                            if (!hasBad)
                            {
                                foreach (var x in telekinesis_picked)
                                {
                                    if (alreadyChosen != null && alreadyChosen.Contains(x))
                                        continue;
                                    
                                    handleId = x;
                                    break;
                                }

                                if (handleId == 0 && alreadyChosen != null && alreadyChosen.Count >= SettingsInstance.TelekinesisMaxObjects)
                                    failBecauseMax = true;
                            }
                        }

                        if (grabactor_picked.Count != 0)
                            actorHandle = grabactor_picked[0];
                    }

                    if((debug_msg & 0x40) != 0)
                    {
                        if (handleId == 0)
                            WriteDebugMsg("Didn't pick any target");
                        else
                        {
                            using (var objHandler = new ObjectRefHolder(handleId))
                            {
                                if (!objHandler.IsValid)
                                    WriteDebugMsg("Picked invalid handle");
                                else
                                    WriteDebugMsg("Picked " + objHandler.Object.ToString());
                            }
                        }
                    }

                    if(handleId != 0)
                    {
                        using (var objRefHold = new ObjectRefHolder(handleId))
                        {
                            if (!objRefHold.IsValid || !CanPickTelekinesisTarget(objRefHold.Object, new List<EffectSetting>() { efs }, true))
                                handleId = 0;
                        }
                    }
                    
                    ctx.DX = new IntPtr(handleId);
                    ForceUpdateTelekinesis();

                    if(!failBecauseMax && casting_normal && SettingsInstance.TelekinesisDisarmsEnemies)
                    {
                        if(actorHandle != 0)
                        {
                            using (var objRef = new ObjectRefHolder(actorHandle))
                            {
                                if(objRef.IsValid)
                                {
                                    var ac = objRef.Object as Actor;
                                    if (ac != null)
                                        DisarmActor(ac);
                                }
                            }
                        }
                    }

                    if (handleId == 0)
                        OnFailPickTelekinesisTarget(efs, failBecauseMax, hadObjCount);
                    else
                        OnSucceedPickTelekinesisTarget(efs, handleId);
                }
            });

            addr = gi(33677, 0x10A5 - 0x1010, "8B 48 04 89 8D 90 03 00 00");
            Memory.WriteHook(new HookParameters()
            {
                Address = addr,
                IncludeLength = 9,
                ReplaceLength = 9,
                Before = ctx =>
                {
                    if (ctx.R13.ToUInt8() == 2)
                    {
                        ctx.Skip();
                        uint handleId = 0;
                        lock(locker_picked)
                        {
                            if (grabactor_picked.Count != 0)
                            {
                                HashSet<uint> alreadyChosen = null;
                                bool hasBad = false;
                                ForeachHeldHandle(dat =>
                                {
                                    if (hasBad)
                                        return;

                                    if (dat.Effect != null || !dat.IsActor)
                                        hasBad = true;
                                    else
                                    {
                                        if (alreadyChosen == null)
                                            alreadyChosen = new HashSet<uint>();
                                        alreadyChosen.Add(dat.ObjectHandleId);
                                    }
                                });

                                if (!hasBad)
                                {
                                    foreach (var x in grabactor_picked)
                                    {
                                        if(alreadyChosen == null || !alreadyChosen.Contains(x))
                                        {
                                            handleId = x;
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        ctx.CX = new IntPtr(handleId);
                        Memory.WriteUInt32(ctx.BP + 0x390, handleId);
                        ForceUpdateTelekinesis();
                    }
                }
            });

            addr = gi(33669, 0x500D9 - 0x4FFF0, "B2 01");
            Memory.WriteUInt8(addr + 1, 2, true);

            Events.OnMainMenu.Register(e =>
            {
                var alloc = Memory.Allocate(0x50);
                alloc.Pin();
                Memory.WriteZero(alloc.Address, 0x50);
                TempPt1 = MemoryObject.FromAddress<NiPoint3>(alloc.Address);
                TempPt2 = MemoryObject.FromAddress<NiPoint3>(alloc.Address + 0x10);
                TempPt3 = MemoryObject.FromAddress<NiPoint3>(alloc.Address + 0x20);
                TempPtBegin = MemoryObject.FromAddress<NiPoint3>(alloc.Address + 0x30);
                TempPtEnd = MemoryObject.FromAddress<NiPoint3>(alloc.Address + 0x40);

                var effects = DataHandler.Instance.GetAllFormsByType(FormTypes.EffectSetting);
                var eqpab = TESForm.LookupFormById(0x1A4CA);
                
                if (eqpab != null && eqpab is SpellItem)
                {
                    foreach (var form in effects)
                    {
                        var set = form as EffectSetting;
                        if (set == null || set.Archetype != Archetypes.GrabActor)
                            continue;

                        if (set.EquipAbility != null)
                        {
                            if ((debug_msg & 4) != 0)
                                WriteDebugMsg("Couldn't set " + form.ToString() + " equip ability as it already has one! (" + set.EquipAbility.ToString() + ")");
                            continue;
                        }

                        Memory.WritePointer(set.Cast<EffectSetting>() + 0xC8, eqpab.Cast<SpellItem>());
                        if ((debug_msg & 4) != 0)
                            WriteDebugMsg("Set " + form.ToString() + " equip ability to " + eqpab.ToString());
                    }
                }
                else
                {
                    if ((debug_msg & 4) != 0)
                        WriteDebugMsg("Couldn't set any equip ability of grab actor because the telekinesis effect ability is missing!");
                }
            }, 0, 1);

            var col = new CollisionLayers[]
            {
                CollisionLayers.Ground,
                CollisionLayers.Terrain,
                CollisionLayers.Clutter,
                CollisionLayers.Static,
                CollisionLayers.Weapon,
                CollisionLayers.Biped,
                CollisionLayers.LivingAndDeadActors,
                CollisionLayers.BipedNoCC,
            };

            foreach (var x in col)
            {
                ulong fl = (ulong)1 << (int)x;
                RaycastMask |= fl;
            }
        }

        private static ulong RaycastMask;

        private static NiPoint3 TempPt1;
        private static NiPoint3 TempPt2;
        private static NiPoint3 TempPt3;
        private static NiPoint3 TempPtBegin;
        private static NiPoint3 TempPtEnd;

        private static void do_split_raycast(float[] headPos, float[] camPos, float[] endPos, TESObjectCELL cell, List<NiNode> plrNodes)
        {
            var ray = TESObjectCELL.RayCast(new RayCastParameters()
            {
                Begin = camPos,
                End = endPos,
                Cell = cell,
            });

            float frac = 1.0f;

            foreach (var r in ray)
            {
                if (r.Fraction >= frac || r.HavokObject == IntPtr.Zero)
                    continue;

                uint flags = Memory.ReadUInt32(r.HavokObject + 0x2C) & 0x7F;
                ulong mask = (ulong)1 << (int)flags;
                if ((RaycastMask & mask) == 0)
                    continue;

                var obj = r.Object;
                if (obj != null)
                {
                    bool ispl = false;
                    for (int i = 0; i < plrNodes.Count; i++)
                    {
                        var pi = plrNodes[i];
                        if (pi != null && pi.Equals(obj))
                        {
                            ispl = true;
                            break;
                        }
                    }

                    if (ispl)
                        continue;
                }

                frac = r.Fraction;
            }

            if (frac < 1.0f)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (i == 2)
                        endPos[i] = (endPos[i] - camPos[i]) * Math.Max(frac - 0.01f, 0.0f) + camPos[i];
                    else
                        endPos[i] = (endPos[i] - camPos[i]) * frac + camPos[i];
                }
            }

            if (headPos == null)
                return;

            ray = TESObjectCELL.RayCast(new RayCastParameters()
            {
                Begin = headPos,
                End = endPos,
                Cell = cell,
            });

            frac = 1.0f;

            foreach (var r in ray)
            {
                if (r.Fraction >= frac || r.HavokObject == IntPtr.Zero)
                    continue;

                uint flags = Memory.ReadUInt32(r.HavokObject + 0x2C) & 0x7F;
                ulong mask = (ulong)1 << (int)flags;
                if ((RaycastMask & mask) == 0)
                    continue;

                var obj = r.Object;
                if (obj != null)
                {
                    bool ispl = false;
                    for (int i = 0; i < plrNodes.Count; i++)
                    {
                        var pi = plrNodes[i];
                        if (pi != null && pi.Equals(obj))
                        {
                            ispl = true;
                            break;
                        }
                    }

                    if (ispl)
                        continue;
                }

                frac = r.Fraction;
            }

            if (frac < 1.0f)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (i == 2)
                        endPos[i] = (endPos[i] - headPos[i]) * Math.Max(frac - 0.01f, 0.0f) + headPos[i];
                    else
                        endPos[i] = (endPos[i] - headPos[i]) * frac + headPos[i];
                }
            }
        }

        internal sealed class spell_info
        {
            internal spell_info(spell_types t)
            {
                this.type = t;
            }

            internal TESObjectBOOK SpellBook;
            internal SpellItem Spell;
            internal EffectSetting Effect;
            internal spell_types type;
            internal HashSet<uint> Item;

            internal spell_info Load(string str, string setting)
            {
                if (string.IsNullOrEmpty(str))
                    return this;

                var spl = str.Split(new[] { ';' }, StringSplitOptions.None);
                if (spl.Length < 3)
                    return this;

                for(int i = 0; i < spl.Length; i++)
                {
                    if (string.IsNullOrEmpty(spl[i]))
                        continue;

                    var cac = CachedFormList.TryParse(spl[i], "BetterTelekinesis", setting, true, false);
                    if (cac != null && cac.All.Count == 1)
                    {
                        if (i == 0)
                            this.SpellBook = cac.All[0] as TESObjectBOOK;
                        else if (i == 1)
                            this.Spell = cac.All[0] as SpellItem;
                        else if (i == 2)
                            this.Effect = cac.All[0] as EffectSetting;
                        /*else if (i >= 3)
                        {
                            var form = cac.All[0];
                            if (form != null)
                            {
                                uint formId = form.FormId;
                                if (this.Item == null)
                                    this.Item = new HashSet<uint>();
                                this.Item.Add(formId);
                            }
                        }*/
                    }
                    else
                        continue;
                }

                switch(this.type)
                {
                    case spell_types.blast:
                        {
                            var mspl = (SettingsInstance.Blast_SwordModel ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                            if (mspl.Length == 0)
                                mspl = new string[] { @"Weapons\Iron\LongSword.nif" };

                            string fname = "BetterTelekinesis.esp";
                            int mi = 0;

                            ProduceItem(0x805, fname, mspl[(mi++) % mspl.Length]);
                            for (uint u = 0x88C; u <= 0x8BC; u++)
                                ProduceItem(u, fname, mspl[(mi++) % mspl.Length]);
                        }
                        break;

                    case spell_types.barrage:
                        {
                            var mspl = (SettingsInstance.Barrage_SwordModel ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                            if (mspl.Length == 0)
                                mspl = new string[] { @"Weapons\Iron\LongSword.nif" };

                            string fname = "BetterTelekinesis.esp";
                            int mi = 0;

                            ProduceItem(0x804, fname, mspl[(mi++) % mspl.Length]);
                            for (uint u = 0x87B; u <= 0x88B; u++)
                                ProduceItem(u, fname, mspl[(mi++) % mspl.Length]);
                            for (uint u = 0x8BD; u <= 0x8DC; u++)
                                ProduceItem(u, fname, mspl[(mi++) % mspl.Length]);
                        }
                        break;
                }

                return this;
            }

            private void ProduceItem(uint formId, string formFile, string model)
            {
                var item = TESForm.LookupFormFromFile(formId, formFile) as TESObjectMISC;
                if (item == null)
                    return;

                if (this.Item == null)
                    this.Item = new HashSet<uint>();

                this.Item.Add(item.FormId);

                byte[] buf = Encoding.UTF8.GetBytes(model);
                using (var alloc = Memory.Allocate(buf.Length + 2))
                {
                    Memory.WriteBytes(alloc.Address, buf);
                    Memory.WriteUInt8(alloc.Address + buf.Length, 0);
                    item.InvokeVTableThisCall<TESModelTextureSwap>(0x28, alloc.Address);
                }
            }
        }

        internal static spell_info[] spellInfos;

        internal static readonly List<TESEffectShader> EffectInfos = new List<TESEffectShader>();

        internal enum spell_types : int
        {
            normal,

            reach,
            enemy,
            single,
            barrage,
            blast,

            max
        }

        private static readonly List<uint> telekinesis_picked = new List<uint>();
        private static readonly List<uint> grabactor_picked = new List<uint>();
        private static readonly object locker_picked = new object();

        private static int debug_msg = 0;
        private static int last_debug_pick = 0;
        private static bool debug_pick = false;
        
        private static void WriteDebugMsg(string msg)
        {
            var l = NetScriptFramework.Main.Log;
            if (l != null)
                l.AppendLine(msg);
            NetScriptFramework.Main.WriteDebugMessage(msg);
        }

        private static System.Diagnostics.Stopwatch _profile_timer;
        private static long[] _profile_times = new long[32];
        private static long _profile_last;
        private static int _profile_index;
        private static long _profile_counter;
        private static int _profile_report;

        private static void begin_profile()
        {
            if ((debug_msg & 0x20) == 0)
                return;

            var t = _profile_timer;
            if(t == null)
            {
                t = new System.Diagnostics.Stopwatch();
                _profile_timer = t;
                t.Start();
            }

            _profile_last = t.ElapsedTicks;
            _profile_index = 0;
        }

        private static void step_profile()
        {
            if ((debug_msg & 0x20) == 0)
                return;

            long t = _profile_timer.ElapsedTicks;
            long diff = t - _profile_last;
            _profile_last = t;
            _profile_times[_profile_index++] += diff;
        }

        private static void end_profile()
        {
            if ((debug_msg & 0x20) == 0)
                return;

            _profile_counter++;

            int now = Environment.TickCount;
            if (unchecked(now - _profile_report) < 3000)
                return;

            _profile_report = now;
            var bld = new StringBuilder(32);
            for(int i = 0; i < _profile_times.Length; i++)
            {
                long tot = _profile_times[i];
                if (tot == 0)
                    continue;

                double avg = (double)tot / (double)_profile_counter;
                //avg /= (double)(System.Diagnostics.Stopwatch.Frequency / 1000);

                if (bld.Length != 0)
                    bld.Append("  ");
                bld.Append("[" + i + "] = " + avg.ToString("0.###"));
            }

            WriteDebugMsg(bld.ToString() + " <- " + _profile_counter);
        }

        private static bool is_cell_within_dist(float myX, float myY, int coordX, int coordY, float maxDist)
        {
            float minX = coordX * 4096.0f;
            float maxX = (coordX + 1) * 4096.0f;
            float minY = coordY * 4096.0f;
            float maxY = (coordY + 1) * 4096.0f;

            float smallestDist = 999999.0f;
            if (myX < minX)
                smallestDist = minX - myX;
            else if (myX > maxX)
                smallestDist = myX - maxX;
            else
                return true;

            if (myY < minY)
                smallestDist = Math.Min(smallestDist, minY - myY);
            else if (myY > maxY)
                smallestDist = Math.Min(smallestDist, myY - maxY);
            else
                return true;

            return smallestDist < maxDist;
        }

        private static List<EffectSetting> CalculateCasting()
        {
            var ls = new List<EffectSetting>();
            var plr = PlayerCharacter.Instance;
            if (plr != null)
            {
                for (int i = 0; i < 2; i++)
                {
                    var caster = plr.GetMagicCaster((EquippedSpellSlots)i);
                    if (caster != null)
                    {
                        switch (caster.State)
                        {
                            case MagicCastingStates.Charging:
                            case MagicCastingStates.Concentrating:
                                {
                                    var ef = caster.CastItem;
                                    if (ef != null)
                                    {
                                        var efls = ef.Effects;
                                        if (efls != null)
                                        {
                                            foreach (var x in efls)
                                            {
                                                var xe = x.Effect;
                                                if (xe != null)
                                                    ls.Add(xe);
                                            }
                                        }
                                    }
                                }
                                break;
                        }
                    }
                }
            }

            return ls;
        }

        private static bool casting_normal = false;

        private static void DisarmActor(Actor who)
        {
            if (who == null || who.IsPlayer)
                return;

            var plr = PlayerCharacter.Instance;
            if (plr == null)
                return;

            // staggered?
            /*uint data = (Memory.ReadUInt32(who.Cast<Actor>() + 0xC4) >> 13) & 1;
            if (data == 0)
                return;*/
                
            var taskPool = Memory.ReadPointer(addr_TaskPool);
            using (var alloc = Memory.Allocate(0x20))
            {
                using (var objRef = new ObjectRefHolder(who))
                {
                    if (!objRef.IsValid)
                        return;

                    Memory.WriteUInt32(alloc.Address, objRef.Handle);
                }

                using (var objRef = new ObjectRefHolder(plr))
                {
                    if (!objRef.IsValid)
                        return;

                    Memory.WriteUInt32(alloc.Address + 0x10, objRef.Handle);
                }

                Memory.InvokeCdecl(addr_DisarmTask, taskPool, alloc.Address, alloc.Address + 0x10);
            }
        }

        internal static void OverwriteTelekinesisTargetPick(uint originalTarget)
        {
            var plr = PlayerCharacter.Instance;
            if (plr == null)
                return;

            if ((debug_msg & 1) != 0)
            {
                int now = Environment.TickCount;
                if (unchecked(now - last_debug_pick) >= 1000)
                {
                    last_debug_pick = now;
                    debug_pick = true;

                    WriteDebugMsg("================================= (" + GetCurrentRelevantActiveEffects().Count + ")");
                }
                else
                    debug_pick = false;
            }

            // Not doing telekinesis then don't care?
            float maxDistance;
            if ((maxDistance = plr.TelekinesisDistance) <= 0.0f)
            {
                if ((debug_msg & 1) != 0 && debug_pick)
                    WriteDebugMsg("Not doing telekinesis");
                return;
            }
            
            // This will prevent us from keeping to switch targets while effect has already chosen target, stops the activation label from switching all the time (just visual, no real effect).
            /*foreach (var ef in relevantEff)
            {
                if(ef is TelekinesisEffect)
                {
                    uint prevHandle = Memory.ReadUInt32(ef.Cast<TelekinesisEffect>() + 0xA0);
                    if (prevHandle != 0)
                    {
                        if (debug_msg && debug_pick)
                            WriteDebugMsg("Used previous handle for pick");
                        return prevHandle;
                    }
                }
                else if(ef is GrabActorEffect)
                {
                    uint prevHandle = Memory.ReadUInt32(ef.Cast<GrabActorEffect>() + 0xA0);
                    if (prevHandle != 0)
                    {
                        if (debug_msg && debug_pick)
                            WriteDebugMsg("Used previous handle for pick");
                        return prevHandle;
                    }
                }
            }*/

            // If there's already picked a valid one then it's the best choice.
            /*if (originalTarget != 0)
            {
                if (debug_msg && debug_pick)
                    WriteDebugMsg("Used original choice for pick");
                return originalTarget;
            }*/

            var pcam = PlayerCamera.Instance;
            if (pcam == null)
                return;

            var camNode = pcam.Node;
            if (camNode == null)
                return;

            var cell = plr.ParentCell;
            if (cell == null || !cell.IsAttached)
                return;

            var tes = TES.Instance;
            if (tes == null)
                return;

            var plrNodes = new List<NiNode> { plr.GetSkeletonNode(true), plr.GetSkeletonNode(false) };
            
            HashSet<uint> ignoreHandles = new HashSet<uint>();

            ForeachHeldHandle(dat =>
            {
                using (var objHold = new ObjectRefHolder(dat.ObjectHandleId))
                {
                    if (objHold.IsValid)
                    {
                        var rootObj = objHold.Object.Node;
                        if (rootObj != null)
                            plrNodes.Add(rootObj);

                        ignoreHandles.Add(objHold.Handle);
                    }
                }
            });
            
            var camPos = camNode.WorldTransform.Position;

            float[] beginHead = null;
            float[] beginCam = null;

            if (plrNodes[1] != null && pcam.State != null && pcam.State.Id != TESCameraStates.FirstPerson)
            {
                var headNode = plrNodes[1].LookupNodeByName("NPC Head [Head]");
                if (headNode != null)
                {
                    beginHead = new float[3];
                    var headPos = headNode.WorldTransform.Position;
                    beginHead[0] = headPos.X;
                    beginHead[1] = headPos.Y;
                    beginHead[2] = headPos.Z;
                }
            }

            {
                beginCam = new float[3];
                beginCam[0] = camPos.X;
                beginCam[1] = camPos.Y;
                beginCam[2] = camPos.Z;
            }

            float[] end = new float[3];
            using (var alloc = Memory.Allocate(0x20))
            {
                Memory.WriteZero(alloc.Address, 0x20);
                Memory.WriteFloat(alloc.Address + 4, maxDistance);
                var tg = MemoryObject.FromAddress<NiPoint3>(alloc.Address + 0x10);
                camNode.WorldTransform.Translate(MemoryObject.FromAddress<NiPoint3>(alloc.Address), tg);
                end[0] = tg.X;
                end[1] = tg.Y;
                end[2] = tg.Z;
            }
            
            do_split_raycast(beginHead, beginCam, end, cell, plrNodes);
            
            float[] begin = beginHead ?? beginCam;

            int findMask = 3; // 1 = objects, 2 = actors
            // TODO: only find object or actor?

            TempPtBegin.X = begin[0];
            TempPtBegin.Y = begin[1];
            TempPtBegin.Z = begin[2];
            TempPtEnd.X = end[0];
            TempPtEnd.Y = end[1];
            TempPtEnd.Z = end[2];
            
            var data = new telek_calc_data();
            data.begin = begin;
            data.end = end;
            data.chls = new List<telek_obj_data>();
            data.findMask = findMask;
            data.maxDistance = maxDistance;
            data.ignore = new HashSet<NiNode>();
            foreach (var n in plrNodes)
                data.ignore.Add(n);
            data.ignore_handle = ignoreHandles;
            data.casting = CalculateCasting();
            
            find_best_telekinesis(cell, data);
            
            if (!cell.IsInterior)
            {
                var cells = tes.GetLoadedCells();
                foreach (var c in cells)
                {
                    if (cell.Equals(c))
                        continue;

                    /*float minDist = Math.Max(Math.Abs(c.CoordinateX - cell.CoordinateX), Math.Abs(c.CoordinateY - cell.CoordinateY)) * 4096.0f - 4096.0f;
                    if (minDist >= maxDistance)
                        continue;*/

                    if (!is_cell_within_dist(begin[0], begin[1], c.CoordinateX, c.CoordinateY, maxDistance))
                        continue;

                    find_best_telekinesis(c, data);
                }
            }
            
            if (data.chls.Count == 0)
            {
                if ((debug_msg & 1) != 0 && debug_pick)
                    WriteDebugMsg("Didn't find any valid object for ray pick");
                return;
            }

            if (data.chls.Count > 1)
                data.chls.Sort((u, v) => u.distFromRay.CompareTo(v.distFromRay));

            int objLeftTake = 1;
            int actorLeftTake = 1;

            for(int i = 0; i < data.chls.Count && (objLeftTake > 0 || actorLeftTake > 0); i++)
            {
                var odata = data.chls[i];

                bool isActor = false;
                if(odata.obj is Actor)
                {
                    isActor = true;
                    if (actorLeftTake == 0)
                        continue;
                }
                else
                {
                    if (objLeftTake == 0)
                        continue;
                }

                // Make sure it's in line of sight to us.
                var ray = TESObjectCELL.RayCast(new RayCastParameters()
                {
                    Begin = data.begin,

                    End = new float[]
                    {
                        odata.x,
                        odata.y,
                        odata.z
                    },

                    Cell = cell
                });

                var addedNode = odata.obj.Node;
                if (addedNode != null)
                    data.ignore.Add(addedNode);

                bool hasLos = true;
                foreach (var r in ray)
                {
                    if (r.HavokObject == IntPtr.Zero)
                        continue;

                    uint flags = Memory.ReadUInt32(r.HavokObject + 0x2C) & 0x7F;
                    ulong mask = (ulong)1 << (int)flags;
                    if ((RaycastMask & mask) == 0)
                        continue;

                    {
                        var cobj = r.Object;
                        if (cobj != null)
                        {
                            if (data.ignore.Contains(cobj))
                                continue;
                        }
                    }

                    hasLos = false;
                    break;
                }

                if (addedNode != null)
                    data.ignore.Remove(addedNode);

                if (!hasLos)
                {
                    if ((debug_msg & 1) != 0 && debug_pick)
                        WriteDebugMsg("Checked BAD object (no LOS): " + odata.obj.ToString());
                    continue;
                }

                using (var objRefHold = new ObjectRefHolder(odata.obj))
                {
                    if (objRefHold.IsValid)
                    {
                        if (isActor)
                        {
                            grabactor_picked.Add(objRefHold.Handle);
                            if ((debug_msg & 1) != 0 && debug_pick)
                                WriteDebugMsg("Returned actor: " + odata.obj.ToString() + "; dist = " + odata.distFromRay);
                            actorLeftTake--;
                        }
                        else
                        {
                            telekinesis_picked.Add(objRefHold.Handle);
                            if ((debug_msg & 1) != 0 && debug_pick)
                                WriteDebugMsg("Returned object: " + odata.obj.ToString() + "; dist = " + odata.distFromRay);
                            objLeftTake--;
                        }
                    }
                }
            }
        }

        private sealed class telek_obj_data
        {
            internal TESObjectREFR obj;
            internal float distFromRay;
            internal float x;
            internal float y;
            internal float z;
        }

        private sealed class telek_calc_data
        {
            internal float[] begin;
            internal float[] end;
            internal List<telek_obj_data> chls;
            internal int findMask;
            internal float maxDistance;
            internal HashSet<NiNode> ignore;
            internal HashSet<uint> ignore_handle;
            internal List<EffectSetting> casting;
        }
        
        private static void process_one_obj(TESObjectCELL cell, TESObjectREFR obj, telek_calc_data data, float quickMaxDist)
        {
            float objBaseX;
            float objBaseY;
            float objBaseZ;

            /*if ((debug_msg & 1) != 0)
            {
                if (obj.BaseForm != null && obj.BaseForm.FormId == 0x3BE1A)
                {
                    WriteDebugMsg("had ancient nord arrow");
                    arrow_debug = true;
                }
            }*/

            // Very quick check to save resources.
            {
                var opos = obj.Position;
                objBaseX = opos.X;
                objBaseY = opos.Y;
                float dx = objBaseX - data.begin[0];
                float dy = objBaseY - data.begin[1];

                if ((dx * dx + dy * dy) > quickMaxDist)
                    return;

                objBaseZ = opos.Z;
            }

            step_profile(); // end of 0

            uint formFlag = obj.FormFlags;
            if ((formFlag & ((1 << 11) | (1 << 5))) != 0)
                return;

            step_profile(); // end of 1

            uint thisHandle = 0;
            using (var objHolder = new ObjectRefHolder(obj))
            {
                if (!objHolder.IsValid)
                    return;

                thisHandle = objHolder.Handle;
                if(data.ignore_handle.Contains(objHolder.Handle))
                    return;
            }

            step_profile(); // end of 2

            var actor = obj as Actor;

            if ((data.findMask & 2) == 0 && actor != null)
                return;

            if ((data.findMask & 1) == 0 && actor == null)
                return;

            if (actor != null)
            {
                if (actor.IsPlayer)
                    return;

                if (SettingsInstance.DontPickFriendlyTargets == 1)
                {
                    if (actor.IsPlayerTeammate)
                        return;
                }
                else if (SettingsInstance.DontPickFriendlyTargets == 2)
                {
                    if (actor.IsPlayerTeammate || !actor.IsHostileToActor(PlayerCharacter.Instance))
                        return;
                }
            }

            step_profile(); // end of 3

            TempPt1.X = 0.0f;
            TempPt1.Y = 0.0f;
            TempPt1.Z = 0.0f;
            obj.InvokeVTableThisCall<TESObjectREFR>(0x398, TempPt1.Cast<NiPoint3>());

            TempPt2.X = 0.0f;
            TempPt2.Y = 0.0f;
            TempPt2.Z = 0.0f;
            obj.InvokeVTableThisCall<TESObjectREFR>(0x3A0, TempPt2.Cast<NiPoint3>());

            step_profile(); // end of 4

            // This isn't perfect way to do it in case object is rotated strangely but those are not common cases.

            TempPt1.X = objBaseX + ((TempPt2.X - TempPt1.X) * 0.5f + TempPt1.X);
            TempPt1.Y = objBaseY + ((TempPt2.Y - TempPt1.Y) * 0.5f + TempPt1.Y);
            TempPt1.Z = objBaseZ + ((TempPt2.Z - TempPt1.Z) * 0.5f + TempPt1.Z);

            float objTotalDist = TempPtBegin.GetDistance(TempPt1);

            if (objTotalDist > data.maxDistance)
            {
                /*if (debug_msg && debug_pick)
                    WriteDebugMsg("Checked BAD object (too far " + objTotalDist + " vs. " + maxDistance + "): " + obj.ToString());*/
                return;
            }

            step_profile(); // end of 5

            TempPtBegin.Subtract(TempPt1, TempPt2);
            TempPtEnd.Subtract(TempPtBegin, TempPt3);
            float dot = TempPt2.Dot(TempPt3);
            TempPtEnd.Subtract(TempPtBegin, TempPt2);
            float len = TempPt2.Length;
            len *= len;
            float t = -1.0f;
            if (len > 0.0f)
                t = -(dot / len);

            float distResult = 999999.0f;
            if (t > 0.0f && t < 1.0f)
            {
                TempPt1.Subtract(TempPtBegin, TempPt2); // TempPt1 - TempPtBegin -> TempPt2
                TempPt1.Subtract(TempPtEnd, TempPt3); // TempPt1 - TempPtEnd -> TempPt3
                TempPt2.Cross(TempPt3, TempPt2); // TempPt2 X TempPt3 -> TempPt2
                float dist1 = TempPt2.Length;
                TempPtBegin.Subtract(TempPtEnd, TempPt3);
                float dist2 = TempPt3.Length;
                if (dist2 > 0.0f)
                    distResult = dist1 / dist2;
            }
            else
            {
                TempPtBegin.Subtract(TempPt1, TempPt2);
                float dist1 = TempPt2.Length;
                TempPtEnd.Subtract(TempPt1, TempPt2);
                float dist2 = TempPt2.Length;
                distResult = Math.Min(dist1, dist2);
            }

            float maxDistFromRay = actor != null ? SettingsInstance.ActorTargetPickerRange : SettingsInstance.ObjectTargetPickerRange;
            if (distResult > maxDistFromRay)
            {
                /*if (debug_msg && debug_pick)
                    WriteDebugMsg("Checked BAD object (exceed ray " + distResult + " vs. " + maxDistFromRay + "): " + obj.ToString());*/
                return;
            }

            step_profile(); // end of 6

            /*if (bestObj != null && bestDist <= distResult)
            {
                if (debug_msg && debug_pick)
                {
                    if (actor == null && !Memory.InvokeCdecl(addr_CanBeTelekinesis, obj.Cast<TESObjectREFR>()).ToBool())
                        continue;
                    WriteDebugMsg("Checked BAD object (already have better " + distResult + " vs. " + bestDist + "): " + obj.ToString());
                }
                continue;
            }*/

            // Verify object.
            if (actor != null)
            {

            }
            else
            {
                if (!Memory.InvokeCdecl(addr_CanBeTelekinesis, obj.Cast<TESObjectREFR>()).ToBool())
                {
                    /*if (debug_msg && debug_pick)
                        WriteDebugMsg("Checked BAD object (can't be telekinesed): " + obj.ToString());*/
                    return;
                }
            }

            step_profile(); // end of 7

            if (!CanPickTelekinesisTarget(obj, data.casting, false))
                return;

            step_profile(); // end of 8

            var odata = new telek_obj_data();
            odata.obj = obj;
            odata.distFromRay = distResult;
            odata.x = TempPt1.X;
            odata.y = TempPt1.Y;
            odata.z = TempPt1.Z;
            data.chls.Add(odata);

            step_profile(); // end of 9
        }

        private static void find_best_telekinesis(TESObjectCELL cell, telek_calc_data data)
        {
            float quickMaxDist = (data.maxDistance + 500.0f);
            quickMaxDist *= quickMaxDist;

            cell.CellLock.Lock();
            try
            {
                var refs = cell.References;
                if (refs != null)
                {
                    foreach (var objPt in refs)
                    {
                        var obj = objPt.Value;
                        if (obj == null)
                            continue;
                        
                        begin_profile();
                        process_one_obj(cell, obj, data, quickMaxDist);
                        //arrow_debug = false;
                        end_profile();
                    }
                }
            }
            finally
            {
                cell.CellLock.Unlock();
            }
        }

        private static int GetCurrentTelekinesisObjectCount(int valueIfActorGrabbed = int.MaxValue)
        {
            int hasObj = 0;
            bool hadActor = false;
            ForeachHeldHandle(dat =>
            {
                if (hadActor)
                    return;

                if (dat.IsActor)
                    hadActor = true;
                else
                    hasObj++;
            });
            if (hadActor)
                return valueIfActorGrabbed;
            return hasObj;
        }

        private static void apply_multi_telekinesis()
        {
            // Player update func, clears grabbed objects in some cases.
            var addr = gi(39375, 0xEC86 - 0xE770, "83 BF C0 08 00 00 00");
            Memory.WriteHook(new HookParameters()
            {
                Address = addr,
                IncludeLength = 0,
                ReplaceLength = 0x11,
                Before = ctx =>
                {
                    clear_grabindex(true);
                }
            });

            // Player ::Revert
            addr = gi(39466, 0x9837 - 0x9620, "E8");
            Memory.WriteHook(new HookParameters()
            {
                Address = addr,
                IncludeLength = 0,
                ReplaceLength = 5,
                Before = ctx =>
                {
                    clear_grabindex(false);
                }
            });

            // Called from ActivateHandler, probably to drop grabbed objects.
            addr = gi(39476, 0, "83 B9 F4 0A 00 00 00");
            Memory.WriteHook(new HookParameters()
            {
                Address = addr,
                IncludeLength = 0,
                ReplaceLength = 0xD,
                Before = ctx =>
                {
                    if (current_grabindex != IntPtr.Zero || Memory.ReadInt32(ctx.CX + 0xAF4) != 0)
                        clear_grabindex(false);
                }
            });

            // Clear grab objects func itself.
            addr = gi(39480, 0, "4C 8B DC 55 56");
            Memory.WriteHook(new HookParameters()
            {
                Address = addr,
                IncludeLength = 5,
                ReplaceLength = 5,
                Before = ctx =>
                {
                    var cg = current_grabindex;
                    if (cg != IntPtr.Zero)
                    {
                        if(_dont_call_clear == 0)
                            free_grabindex(cg, "unexpected clear grabbed objects");
                    }
                }
            });

            // Telekinesis dtor
            addr = gi(34252, 0, "40 57 48 83 EC 30");
            Memory.WriteHook(new HookParameters()
            {
                Address = addr,
                IncludeLength = 6,
                ReplaceLength = 6,
                Before = ctx =>
                {
                    free_grabindex(ctx.CX, "dtor");
                }
            });

            // Telekinesis apply begin.
            addr = gi(34259, 0, "40 53 48 83 EC 40");
            Memory.WriteHook(new HookParameters()
            {
                Address = addr,
                IncludeLength = 6,
                ReplaceLength = 6,
                Before = ctx =>
                {
                    switch_to_grabindex(ctx.CX, "add effect");
                    _dont_call_clear++;
                }
            });

            addr = gi(34259, 0xE21 - 0xDC0, "C6 83 A9 00 00 00 00");
            Memory.WriteHook(new HookParameters()
            {
                Address = addr,
                IncludeLength = 7,
                ReplaceLength = 7,
                Before = ctx =>
                {
                    switch_to_grabindex(IntPtr.Zero, "add effect finished");
                    _dont_call_clear--;
                }
            });

            addr = gi(34259, 0xE30 - 0xDC0, "48 8B CB 48 83 C4 40");
            Memory.WriteHook(new HookParameters()
            {
                Address = addr,
                IncludeLength = 7,
                ReplaceLength = 7,
                Before = ctx =>
                {
                    switch_to_grabindex(IntPtr.Zero, "add effect finished");
                    _dont_call_clear--;
                }
            });

            addr = gi(34256, 0, "48 83 EC 28 48 89 4C 24 30");
            Memory.WriteHook(new HookParameters()
            {
                Address = addr,
                IncludeLength = 9,
                ReplaceLength = 9,
                Before = ctx =>
                {
                    switch_to_grabindex(ctx.CX, "end of effect launch");
                }
            });

            addr = gi(34256, 0xCA8 - 0xC80, "48 83 C4 28");
            Memory.WriteHook(new HookParameters()
            {
                Address = addr,
                IncludeLength = 4,
                ReplaceLength = 5,
                Before = ctx =>
                {
                    free_grabindex(Memory.ReadPointer(ctx.SP + 0x30), "end of effect launch finished");
                    switch_to_grabindex(IntPtr.Zero, "end of effect launch finished");
                }
            });
            Memory.WriteUInt8(addr + 5, 0xC3, true);
            Memory.WriteNop(addr + 6, 3);

            addr = gi(34260, 0, "40 55 56 57 48 83 EC 50");
            Memory.WriteHook(new HookParameters()
            {
                Address = addr,
                IncludeLength = 8,
                ReplaceLength = 8,
                Before = ctx =>
                {
                    float diff = Memory.ReadFloat(addr_TimeSinceFrame);
                    switch_to_grabindex(ctx.CX, "update begin", diff);
                    _dont_call_clear++;
                }
            });

            addr = gi(34260, 0x70B3 - 0x6E40, "48 83 C4 50 5F");
            Memory.WriteHook(new HookParameters()
            {
                Address = addr,
                IncludeLength = 5,
                ReplaceLength = 5,
                Before = ctx =>
                {
                    switch_to_grabindex(IntPtr.Zero, "update end");
                    _dont_call_clear--;
                }
            });

            if (SettingsInstance.TelekinesisMaxObjects > 1)
            {
                // Allow more than one instance of the telekinesis active effect.
                addr = gi(33781, 0xA29 - 0xA20, "48 39 42 48");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 6,
                    Before = ctx =>
                    {
                        var adding = ctx.AX;
                        var before = Memory.ReadPointer(ctx.DX + 0x48);

                        if (adding == before)
                        {
                            var item = MemoryObject.FromAddress<ActiveEffect.EffectItem>(before);
                            if (item != null)
                            {
                                var ie = item.Effect;
                                if (ie != null && ie.Archetype == Archetypes.Telekinesis)
                                {
                                    if(IsOurSpell(ie) != OurSpellTypes.TelekOne)
                                        ctx.IP = ctx.IP + 7;
                                }
                            }
                            return;
                        }

                        ctx.IP = ctx.IP + 7;
                    }
                });

                // Allow more than one instance of the telekinesis effect (both places must be edited).
                addr = gi(33785, 0xB80 - 0xB70, "C1 E8 12 48 8B F9");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 6,
                    ReplaceLength = 6,
                    Before = ctx =>
                    {
                        var ef = MemoryObject.FromAddress<ActiveEffect>(ctx.BX) as TelekinesisEffect;
                        if (ef != null && IsOurSpell(ef.BaseEffect) != OurSpellTypes.TelekOne)
                        {
                            if (SettingsInstance.TelekinesisMaxObjects < 99)
                            {
                                if (GetCurrentTelekinesisObjectCount() >= SettingsInstance.TelekinesisMaxObjects)
                                    return;
                            }

                            ctx.Skip();
                            ctx.AX = new IntPtr(1);
                            return;
                        }
                    }
                });
            }

            // Rotate the normal vector based on current index of telekinesised item to separate them out a bit.
            addr = gi(39479, 0xC273 - 0xC0F0, "FF 90 10 06 00 00");
            Memory.WriteHook(new HookParameters()
            {
                Address = addr,
                IncludeLength = 6,
                ReplaceLength = 6,
                After = ctx =>
                {
                    var pt = current_grabindex;
                    if (pt == IntPtr.Zero)
                        return;

                    int indexOfMe = -1;
                    int hadCount = 0;
                    float extraX = 0.0f;
                    float extraY = 0.0f;
                    lock(grabindex_locker)
                    {
                        saved_grab_index g;
                        if (saved_grabindex.TryGetValue(pt, out g))
                        {
                            indexOfMe = g.index_of_obj;
                            if(g.rng != null)
                            {
                                extraX = g.rng.CurrentX;
                                extraY = g.rng.CurrentY;
                            }
                        }
                        hadCount = saved_grabindex.Count;
                    }

                    if (indexOfMe < 0 || indexOfMe >= 100)
                        return;

                    int stepX = 0;
                    int stepY = 0;
                    _select_rotation_offset(indexOfMe, ref stepX, ref stepY);

                    if (stepX == 0 && stepY == 0 && extraX == 0.0f && extraY == 0.0f)
                        return;

                    // Formula method isn't good because it's too jarringly noticable when it changes.
                    /*if (hadCount < 2)
                        hadCount = 1;
                    else
                        hadCount--;

                    double stepAmt = 5.0 + Math.Max(-3.0, (1.0 - hadCount / 10.0) * 10.0);*/

                    double stepAmt = SettingsInstance.TelekinesisObjectSpread;
                    double rotX = stepX * stepAmt;
                    double rotY = stepY * stepAmt;

                    rotX += extraX;
                    rotY += extraY;

                    var positionPtr = ctx.SP + (0x6B8 - 0x670);
                    var normalPtr = ctx.SP + (0x6B8 - 0x690);

                    var position = MemoryObject.FromAddress<NiPoint3>(positionPtr);
                    var normal = MemoryObject.FromAddress<NiPoint3>(normalPtr);
                    
                    using (var alloc = Memory.Allocate(0x90))
                    {
                        Memory.WriteZero(alloc.Address, 0x90);

                        var targetPos = MemoryObject.FromAddress<NiPoint3>(alloc.Address + 0x50);
                        targetPos.X = position.X + normal.X;
                        targetPos.Y = position.Y + normal.Y;
                        targetPos.Z = position.Z + normal.Z;

                        var transform = MemoryObject.FromAddress<NiTransform>(alloc.Address);
                        var tpos = transform.Position;
                        tpos.X = position.X;
                        tpos.Y = position.Y;
                        tpos.Z = position.Z;
                        transform.Scale = 1.0f;
                        transform.LookAt(targetPos);

                        transform.Rotation.RotateZ((float)(rotX / 180.0 * Math.PI), transform.Rotation);

                        var srot = MemoryObject.FromAddress<NiMatrix33>(alloc.Address + 0x60);
                        srot.Identity(1.0f);
                        srot.RotateX((float)(rotY / 180.0 * Math.PI), srot);
                        transform.Rotation.Multiply(srot, transform.Rotation);

                        targetPos.X = 0.0f;
                        targetPos.Y = 1.0f;
                        targetPos.Z = 0.0f;

                        transform.Translate(targetPos, tpos);
                        
                        normal.X = tpos.X - position.X;
                        normal.Y = tpos.Y - position.Y;
                        normal.Z = tpos.Z - position.Z;
                    }
                }
            });

            if (SettingsInstance.TelekinesisMaxObjects > 1)
            {
                // Fix telekinesis gaining skill for each instance of the effect.
                addr = gi(33321, 0x2D, "E8");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 5,
                    ReplaceLength = 5,
                    After = ctx =>
                    {
                        if (ctx.XMM1f > 0.0f)
                        {
                            var ef = MemoryObject.FromAddress<ActiveEffect>(ctx.DI);
                            if (ef is TelekinesisEffect)
                            {
                                var plr = PlayerCharacter.Instance;
                                if (plr != null)
                                {
                                    var ef2 = plr.FindFirstEffectWithArchetype(Archetypes.Telekinesis, false);
                                    if (ef2 != null && !ef2.Equals(ef))
                                    {
                                        ctx.Skip();
                                        ctx.AX = IntPtr.Zero;
                                    }
                                }
                            }
                        }
                    }
                });
            }
        }

        private static int _last_tk_sound = 0;
        private static int _last_tk_sound2 = 0;

        private sealed class saved_grab_index
        {
            internal saved_grab_index()
            {

            }

            internal IntPtr addr;
            internal uint handle;
            internal float dist;
            internal float wgt;
            internal int grabtype;
            internal int index_of_obj;
            internal random_move_generator rng;

            internal byte[] spring;
            internal MemoryAllocation spring_alloc;
        }

        private static readonly object grabindex_locker = new object();

        private static readonly Dictionary<IntPtr, saved_grab_index> saved_grabindex = new Dictionary<IntPtr, saved_grab_index>();

        private static bool casting_sword_barrage = false;
        private static int _placement_barrage = 0;

        private static int unsafe_find_free_index()
        {
            byte[] taken_bits = new byte[13];
            foreach(var pair in saved_grabindex)
            {
                if (pair.Key == IntPtr.Zero)
                    continue;

                int ti = pair.Value.index_of_obj;
                if (ti < 0 || ti >= 100)
                    continue;

                int ix = ti / 8;
                int jx = ti % 8;
                taken_bits[ix] |= (byte)(1 << jx);
            }

            if (casting_sword_barrage)
            {
                for(int j = 0; j < 8; j++)
                {
                    int ji = (_placement_barrage + j) % 8;
                    ji++;

                    int ix = ji / 8;
                    int jx = ji % 8;
                    if ((taken_bits[ix] & (byte)(1 << jx)) == 0)
                    {
                        _placement_barrage++;
                        return ji;
                    }
                }
            }

            for (int i = 0; i < 100; i++)
            {
                int ix = i / 8;
                int jx = i % 8;
                if ((taken_bits[ix] & (byte)(1 << jx)) == 0)
                    return i;
            }

            return -1;
        }

        private static IntPtr current_grabindex;

        private static void switch_to_grabindex(IntPtr addr, string reason, float diff = 0.0f)
        {
            var plr = PlayerCharacter.Instance;
            if (plr == null)
                return;

            var pptr = plr.Cast<PlayerCharacter>();
            if (pptr == IntPtr.Zero)
                return;

            lock(grabindex_locker)
            {
                saved_grab_index g;
                if (!saved_grabindex.ContainsKey(IntPtr.Zero))
                {
                    g = new saved_grab_index();
                    g.addr = IntPtr.Zero;
                    g.wgt = Memory.ReadFloat(pptr + 0x8CC);
                    g.dist = Memory.ReadFloat(pptr + 0x8D0);
                    g.handle = Memory.ReadUInt32(pptr + 0x8C8);
                    g.spring_alloc = null;
                    g.spring = Memory.ReadBytes(pptr + 0x898, 0x30);
                    g.grabtype = Memory.ReadInt32(pptr + 0xAF4);
                    g.index_of_obj = -1;

                    saved_grabindex[IntPtr.Zero] = g;
                }

                if (!saved_grabindex.TryGetValue(addr, out g))
                {
                    g = new saved_grab_index();
                    g.addr = addr;
                    g.dist = 0.0f;
                    g.handle = 0;
                    g.spring_alloc = Memory.Allocate(0x30);
                    g.grabtype = 0;
                    Memory.WriteZero(g.spring_alloc.Address, 0x30);
                    Memory.WriteUInt32(g.spring_alloc.Address, 0x80000000);
                    Memory.WriteUInt32(g.spring_alloc.Address + 0x28, 0);
                    g.spring = Memory.ReadBytes(g.spring_alloc.Address, 0x30);
                    g.index_of_obj = unsafe_find_free_index();
                    g.rng = new random_move_generator();

                    saved_grabindex[addr] = g;
                }
                else if (diff > 0.0f && g.rng != null)
                    g.rng.update(diff);

                if ((debug_msg & 2) != 0)
                    WriteDebugMsg("switch " + current_grabindex.ToHexString() + " -> " + addr.ToHexString() + " (" + reason + ")");
                
                if (current_grabindex == addr)
                    return;

                var prev = saved_grabindex[current_grabindex];
                prev.wgt = Memory.ReadFloat(pptr + 0x8CC);
                prev.dist = Memory.ReadFloat(pptr + 0x8D0);
                prev.handle = Memory.ReadUInt32(pptr + 0x8C8);
                prev.spring = Memory.ReadBytes(pptr + 0x898, 0x30);
                prev.grabtype = Memory.ReadInt32(pptr + 0xAF4);

                current_grabindex = addr;

                Memory.WriteFloat(pptr + 0x8CC, g.wgt);
                Memory.WriteFloat(pptr + 0x8D0, g.dist);
                Memory.WriteUInt32(pptr + 0x8C8, g.handle);
                Memory.WriteBytes(pptr + 0x898, g.spring);
                Memory.WriteInt32(pptr + 0xAF4, g.grabtype);
            }
        }

        private static int _dont_call_clear = 0;

        private static void free_grabindex(IntPtr addr, string reason)
        {
            if (addr == IntPtr.Zero)
                return;

            var plr = PlayerCharacter.Instance;
            if (plr == null)
                return;

            var pptr = plr.Cast<PlayerCharacter>();
            if (pptr == IntPtr.Zero)
                return;

            lock (grabindex_locker)
            {
                if ((debug_msg & 2) != 0)
                    WriteDebugMsg("free " + addr.ToHexString() + " (" + reason + ")");

                saved_grab_index g;
                if (!saved_grabindex.TryGetValue(addr, out g))
                    return;
                
                IntPtr cur_ind = current_grabindex;
                if (cur_ind != addr)
                    switch_to_grabindex(addr, "need to free");

                // Call the func that drops the items from havok.
                _dont_call_clear = 1;
                Memory.InvokeCdecl(addr_ClearGrabbed, pptr);
                _dont_call_clear = 0;

                if (cur_ind == addr)
                    switch_to_grabindex(IntPtr.Zero, "returning from free");
                else
                    switch_to_grabindex(cur_ind, "returning from free");

                g.spring_alloc.Dispose();
                saved_grabindex.Remove(addr);
            }
        }

        private static void clear_grabindex(bool onlyIfCount)
        {
            var plr = PlayerCharacter.Instance;
            if (plr == null)
                return;

            var pptr = plr.Cast<PlayerCharacter>();
            if (pptr == IntPtr.Zero)
                return;

            lock(grabindex_locker)
            {
                if((debug_msg & 2) != 0)
                    WriteDebugMsg("clear");

                var all = saved_grabindex.ToList();
                foreach(var x in all)
                {
                    if (x.Key == IntPtr.Zero)
                        continue;

                    free_grabindex(x.Key, "clear");
                }

                // Current must be Zero or uninited, both is ok.
                if (!onlyIfCount || Memory.ReadInt32(pptr + 0x8C0) != 0)
                {
                    _dont_call_clear = 1;
                    Memory.InvokeCdecl(addr_ClearGrabbed, pptr);
                    _dont_call_clear = 0;
                }
            }
        }

        private static void _select_rotation_offset(int index, ref int x, ref int y)
        {
            if (index < 0 || index >= _rot_offsets.Length)
                return;

            var kv = _rot_offsets[index];
            x = kv.Key;
            y = kv.Value;
        }

        private static readonly KeyValuePair<int, int>[] _rot_offsets = new KeyValuePair<int, int>[]
        {
            new KeyValuePair<int, int>(0, 0),
            new KeyValuePair<int, int>(1, 1),
            new KeyValuePair<int, int>(1, -1),
            new KeyValuePair<int, int>(-1, 1),
            new KeyValuePair<int, int>(-1, -1),
            new KeyValuePair<int, int>(1, 0),
            new KeyValuePair<int, int>(-1, 0),
            new KeyValuePair<int, int>(2, 2),
            new KeyValuePair<int, int>(0, -1),
            new KeyValuePair<int, int>(0, 1),
            new KeyValuePair<int, int>(-2, -2),
            new KeyValuePair<int, int>(2, 1),
            new KeyValuePair<int, int>(-2, 2),
            new KeyValuePair<int, int>(2, -2),
            new KeyValuePair<int, int>(-2, -1),
            new KeyValuePair<int, int>(1, 2),
            new KeyValuePair<int, int>(1, -2),
            new KeyValuePair<int, int>(-2, 1),
            new KeyValuePair<int, int>(2, 0),
            new KeyValuePair<int, int>(-1, -2),
            new KeyValuePair<int, int>(-1, 2),
            new KeyValuePair<int, int>(2, -1),
            new KeyValuePair<int, int>(-2, 0),
            new KeyValuePair<int, int>(2, 3),
            new KeyValuePair<int, int>(0, -2),
            new KeyValuePair<int, int>(0, 2),
            new KeyValuePair<int, int>(-3, -2),
            new KeyValuePair<int, int>(3, -2),
            new KeyValuePair<int, int>(-2, 3),
            new KeyValuePair<int, int>(3, 2),
            new KeyValuePair<int, int>(-2, -3),
            new KeyValuePair<int, int>(-3, 2),
            new KeyValuePair<int, int>(2, -3),
            new KeyValuePair<int, int>(1, 3),
            new KeyValuePair<int, int>(-1, -3),
            new KeyValuePair<int, int>(-1, 3),
            new KeyValuePair<int, int>(3, 0),
            new KeyValuePair<int, int>(-3, -1),
            new KeyValuePair<int, int>(3, -1),
            new KeyValuePair<int, int>(-3, 1),
            new KeyValuePair<int, int>(3, 1),
            new KeyValuePair<int, int>(0, -3),
            new KeyValuePair<int, int>(0, 3),
            new KeyValuePair<int, int>(-3, 0),
            new KeyValuePair<int, int>(1, -3),
            new KeyValuePair<int, int>(2, 4),
            new KeyValuePair<int, int>(-4, -2),
            new KeyValuePair<int, int>(4, -2),
            new KeyValuePair<int, int>(-2, 4),
            new KeyValuePair<int, int>(-2, -4),
            new KeyValuePair<int, int>(4, 2),
            new KeyValuePair<int, int>(-4, 2),
            new KeyValuePair<int, int>(2, -4),
            new KeyValuePair<int, int>(1, 4),
            new KeyValuePair<int, int>(-3, -3),
            new KeyValuePair<int, int>(4, 1),
            new KeyValuePair<int, int>(-3, 3),
            new KeyValuePair<int, int>(1, -4),
            new KeyValuePair<int, int>(3, 3),
            new KeyValuePair<int, int>(-4, -1),
            new KeyValuePair<int, int>(4, -1),
            new KeyValuePair<int, int>(-4, 1),
            new KeyValuePair<int, int>(3, -3),
            new KeyValuePair<int, int>(-1, 4),
            new KeyValuePair<int, int>(-1, -4),
            new KeyValuePair<int, int>(5, 3),
            new KeyValuePair<int, int>(-4, 0),
            new KeyValuePair<int, int>(4, 0),
            new KeyValuePair<int, int>(-5, 3),
            new KeyValuePair<int, int>(3, -5),
            new KeyValuePair<int, int>(0, 4),
            new KeyValuePair<int, int>(0, -4),
            new KeyValuePair<int, int>(3, 5),
            new KeyValuePair<int, int>(-5, -3),
            new KeyValuePair<int, int>(5, -3),
            new KeyValuePair<int, int>(-3, 5),
            new KeyValuePair<int, int>(-3, -5),
            new KeyValuePair<int, int>(4, 4),
            new KeyValuePair<int, int>(4, -4),
            new KeyValuePair<int, int>(-4, 4),
            new KeyValuePair<int, int>(-4, -4),
            new KeyValuePair<int, int>(5, 2),
            new KeyValuePair<int, int>(-5, 2),
            new KeyValuePair<int, int>(3, -4),
            new KeyValuePair<int, int>(2, 5),
            new KeyValuePair<int, int>(-4, -3),
            new KeyValuePair<int, int>(5, -2),
            new KeyValuePair<int, int>(-3, 4),
            new KeyValuePair<int, int>(2, -5),
            new KeyValuePair<int, int>(1, 5),
            new KeyValuePair<int, int>(-5, -2),
            new KeyValuePair<int, int>(5, -1),
            new KeyValuePair<int, int>(-5, 1),
            new KeyValuePair<int, int>(4, 3),
            new KeyValuePair<int, int>(-2, -5),
            new KeyValuePair<int, int>(-2, 5),
            new KeyValuePair<int, int>(4, -3),
            new KeyValuePair<int, int>(-5, -1),
            new KeyValuePair<int, int>(5, 1),
            new KeyValuePair<int, int>(-4, 3),
        };

        private static float rotate_speed(float diff)
        {
            float adiff = Math.Abs(diff);

            // Less than 1 degree difference.
            if (adiff < 0.017453f)
                return 0.0f;

            // diff is in radians
            return diff * 20.0f;
        }
        
        private static float adjust_diff(float current, float target)
        {
            const float pi = (float)Math.PI;
            const float pi2 = (float)Math.PI * 2.0f;
            float x = target - current;
            x -= pi;
            return -pi + ((pi2 + (x % pi2)) % pi2);
        }

        private static void activate_node(NiNode node)
        {
            if (node == null)
                return;
            
            var colNode = Memory.InvokeCdecl(addr_GetCollisionObject, node.Cast<NiNode>());
            if (colNode == IntPtr.Zero)
                return;

            var rigidBody = Memory.InvokeCdecl(addr_GetRigidBody, colNode);
            if (rigidBody == IntPtr.Zero)
                return;

            using (var alloc = Memory.Allocate(0x10))
            {
                Memory.WriteZero(alloc.Address, 0x10);

                Memory.InvokeCdecl(addr_SetAngularVelocity, rigidBody, alloc.Address);
            }
        }
        
        private static void update_point_forward(NiNode node)
        {
            if (node == null)
                return;

            var pcam = PlayerCamera.Instance;
            if (pcam == null)
                return;

            var camNode = pcam.Node;
            if (camNode == null)
                return;

            var colNode = Memory.InvokeCdecl(addr_GetCollisionObject, node.Cast<NiNode>());
            if (colNode == IntPtr.Zero)
                return;

            var rigidBody = Memory.InvokeCdecl(addr_GetRigidBody, colNode);
            if (rigidBody == IntPtr.Zero)
                return;

            using (var alloc = Memory.Allocate(0x10))
            {
                var pt = MemoryObject.FromAddress<NiPoint3>(alloc.Address);
                node.WorldTransform.Rotation.GetEulerAngles(pt);
                
                float hasX = pt.X;
                float hasY = pt.Y;
                float hasZ = pt.Z;

                camNode.WorldTransform.Rotation.GetEulerAngles(pt);

                float wantX = pt.X;
                float wantY = pt.Y;
                float wantZ = pt.Z;

                float diffX = adjust_diff(hasX, wantX);
                float diffY = adjust_diff(hasY, wantY);
                float diffZ = adjust_diff(hasZ, wantZ);
                
                Memory.WriteFloat(alloc.Address + 0, rotate_speed(-diffY));
                Memory.WriteFloat(alloc.Address + 4, rotate_speed(-diffX));
                Memory.WriteFloat(alloc.Address + 8, rotate_speed(-diffZ));
                Memory.WriteFloat(alloc.Address + 12, 0.0f);
                
                Memory.InvokeCdecl(addr_SetAngularVelocity, rigidBody, alloc.Address);
            }
        }

        private static void update_held_object(TESObjectREFR obj, held_obj_data data, List<ActiveEffect> effectList)
        {
            if (obj == null)
                return;

            if (SettingsInstance.PointWeaponsAndProjectilesForward)
            {
                if (obj is TESObjectWEAP || obj is Projectile || IsOurItem(obj.BaseForm) != OurItemTypes.None)
                    update_point_forward(obj.Node);
            }

            if(data.Effect != null && data.Elapsed >= SettingsInstance.SwordBarrage_FireDelay && IsOurSpell(data.Effect) == OurSpellTypes.SwordBarrage)
            {
                foreach(var x in effectList)
                {
                    uint handleId = Memory.ReadUInt32(x.Cast<ActiveEffect>() + 0xA0);
                    if(handleId == data.ObjectHandleId)
                    {
                        sword_instance sw;
                        if (normal_swords.lookup.TryGetValue(handleId, out sw))
                            sw.LaunchTime = Time;
                        else if (ghost_swords.lookup.TryGetValue(handleId, out sw))
                            sw.LaunchTime = Time;
                        
                        x.Dispel();
                        break;
                    }
                }
            }
        }

        /*private static void PlayArtObject(TESObjectREFR obj, BGSArtObject art, float duration)
        {
            if (obj == null || art == null)
                return;

            ulong vid = 22289;
            var a1 = obj.Cast<TESObjectREFR>();
            var a2 = art.Cast<BGSArtObject>();
            float a3 = duration; // duration
            var a4 = IntPtr.Zero; // PlayerCharacter.Instance.Cast<Actor>(); // aim at target
            int a5 = 0; // bool of some sort, maybe rotate to face target
            int a6 = 0; // another bool of something, maybe attach to camera
            var a7 = IntPtr.Zero;
            var a8 = IntPtr.Zero;

            Memory.InvokeCdecl(new IntPtr(0x14030FB90).FromBase(), a1, a2, a3, a4, a5, a6, a7, a8);
        }

        private static void StopArtObject(TESObjectREFR obj, BGSArtObject art)
        {
            if (obj == null || art == null)
                return;

            var func = new IntPtr(0x1406DDC20).FromBase();
            var list = Memory.ReadPointer(new IntPtr(0x141EE5AD0).FromBase());

            Memory.InvokeCdecl(func, list, obj.Cast<TESObjectREFR>(), art.Cast<BGSArtObject>());
        }*/

        private static bool _has_init_sword = false;

        private static void InitSwords()
        {
            if (_has_init_sword)
                return;
            _has_init_sword = true;

            var mem = Memory.Allocate(0x50);
            mem.Pin();
            Memory.WriteZero(mem.Address, 0x50);

            sword_data.Temp1 = MemoryObject.FromAddress<NiPoint3>(mem.Address);
            sword_data.Temp2 = MemoryObject.FromAddress<NiPoint3>(mem.Address + 0x10);
            sword_data.Temp3 = MemoryObject.FromAddress<NiPoint3>(mem.Address + 0x20);
            sword_data.Return1 = mem.Address + 0x30;
            sword_data.Return2 = mem.Address + 0x40;
            
            string fileName = "BetterTelekinesis.esp";

            normal_swords.AddSword_FormId(0x80E, fileName, false);
            for(uint u = 0x840; u < 0x870; u++)
                normal_swords.AddSword_FormId(u, fileName, false);

            ghost_swords.AddSword_FormId(0x80D, fileName, true);
            for (uint u = 0x80F; u <= 0x83F; u++)
                ghost_swords.AddSword_FormId(u, fileName, true);
        }

        // 10e296, fire
        // 10f56b, fire
        // 81180, shadow
        // 8CA2F, light
        // 60db7, cool fire but no membrane
        // 7a296, shadow fast
        // 3fa99, big fire
        private static uint ghost_sword_effect = 0;
        private static uint normal_sword_effect = 0;

        private static void PlaySwordEffect(TESObjectREFR obj, bool ghost)
        {
            if (obj.Node == null)
                return;

            if(ghost)
            {
                var form = EffectInfos.Count >= 1 ? EffectInfos[0] : null;
                if (form != null)
                    obj.PlayEffect(form, 1.5f);
                
                if (ghost_sword_effect != 0)
                {
                    var form2 = TESForm.LookupFormById(ghost_sword_effect) as TESEffectShader;
                    if (form2 != null)
                        obj.PlayEffect(form2, -1.0f);
                }
            }
            else
            {
                var form = EffectInfos.Count >= 1 ? EffectInfos[0] : null;
                if (form != null)
                    obj.PlayEffect(form, 1.5f);

                if (normal_sword_effect != 0)
                {
                    var form2 = TESForm.LookupFormById(normal_sword_effect) as TESEffectShader;
                    if (form2 != null)
                        obj.PlayEffect(form2, -1.0f);
                }
            }
        }

        private static void StopSwordEffect(TESObjectREFR obj, bool ghost)
        {
            if (obj.Node == null)
                return;

            if (ghost)
            {
                if (ghost_sword_effect != 0)
                {
                    var form2 = TESForm.LookupFormById(ghost_sword_effect) as TESEffectShader;
                    if (form2 != null)
                        obj.StopEffect(form2);
                }

                var form = EffectInfos.Count >= 2 ? EffectInfos[1] : null;
                if (form != null)
                    obj.PlayEffect(form, 5.0f);
            }
            else
            {
                if (normal_sword_effect != 0)
                {
                    var form2 = TESForm.LookupFormById(normal_sword_effect) as TESEffectShader;
                    if (form2 != null)
                        obj.StopEffect(form2);
                }

                var form = EffectInfos.Count >= 2 ? EffectInfos[1] : null;
                if (form != null)
                    obj.PlayEffect(form, 5.0f);
            }
        }

        internal static void ReturnSwordToPlace(TESObjectREFR obj)
        {
            var marker = sword_ReturnMarker;
            if (marker == null)
                return;

            var cell = marker.ParentCell;
            if (cell == null)
                return;

            using (var markerHold = new ObjectRefHolder(marker))
            {
                if (!markerHold.IsValid)
                    return;

                var cellPtr = cell != null ? cell.Cast<TESObjectCELL>() : IntPtr.Zero;

                var ws = cell != null ? cell.WorldSpace : null;
                var wsPtr = ws != null ? ws.Cast<TESWorldSpace>() : IntPtr.Zero;

                Memory.InvokeCdecl(addr_SetPosButReallyGoodly, obj.Cast<TESObjectREFR>(), markerHold.Handle, cellPtr, wsPtr, sword_data.Return1, sword_data.Return2);
            }
        }

        private static float first_TeleportZOffset = -2000.0f;
        //private static float first_TeleportZOffset = 0.0f;

        private static void UpdateSwordEffects()
        {
            double now = Time;

            /*HashSet<uint> glowing = new HashSet<uint>();

            var plist = ProcessLists.Instance;
            if (plist == null)
                return;

            var plr = PlayerCharacter.Instance;
            if (plr == null)
                return;

            var form2 = TESForm.LookupFormById(0x69CE8) as TESEffectShader;
            if (form2 == null)
                return;

            {
                plist.MagicEffectsLock.Lock();
                try
                {
                    foreach (var effectPtr in plist.MagicEffects)
                    {
                        var effectInstance = effectPtr.Value as ShaderReferenceEffect;
                        if (effectInstance == null || effectInstance.Finished || !form2.Equals(effectInstance.EffectData))
                            continue;

                        uint objHandle = effectInstance.TargetObjRefHandle;
                        glowing.Add(objHandle);
                    }
                }
                finally
                {
                    plist.MagicEffectsLock.Unlock();
                }
            }*/
            
            for (int z = 0; z < 2; z++)
            {
                var dat = z == 0 ? normal_swords : ghost_swords;

                if (dat.forced_grab != null)
                {
                    if (now - dat.forced_grab.CreateTime > 0.5)
                        dat.forced_grab = null;
                }

                for (int i = 0; i < dat.swords.Count; i++)
                {
                    var sw = dat.swords[i];

                    bool isForced = dat.forced_grab != null && dat.forced_grab.Handle == sw.Handle;

                    if (sw.WaitingEffect != 0)
                    {
                        bool waitMore = false;
                        if (sw.IsWaitingEffect(now))
                        {
                            using (var objRef = new ObjectRefHolder(sw.Handle))
                            {
                                if (objRef.IsValid)
                                {
                                    var root = objRef.Object.Node;
                                    if (root == null)
                                        waitMore = true;
                                    else
                                    {
                                        if (sw.WaitEffectCounter == 0)
                                        {
                                            var scb = root.LookupNodeByName("Scb");
                                            if (scb != null)
                                                scb.NiAVFlags |= NiAVObjectFlags.Hidden;

                                            if (sw.WaitingEffect == 2/* && !glowing.Contains(sw.Handle)*/)
                                                PlaySwordEffect(objRef.Object, true);
                                            else if (sw.WaitingEffect == 1)
                                                PlaySwordEffect(objRef.Object, false);

                                            root.LocalTransform.Position.Z -= first_TeleportZOffset;
                                            root.Update(-1.0f);

                                            sw.WaitEffectCounter = 1;
                                            waitMore = true;
                                        }
                                        else if(sw.WaitEffectCounter == 1)
                                        {
                                            activate_node(root);
                                        }
                                    }
                                }
                            }
                        }

                        if (!waitMore)
                        {
                            sw.WaitingEffect = 0;
                            sw.WaitEffectCounter = 0;
                        }
                    }

                    if(sw.IsWaitingInvis(now))
                    {
                        if(now - sw.FadeTime > 3.0)
                        {
                            sw.FadedOut = true;
                            sw.FadingOut = false;

                            using (var objRef = new ObjectRefHolder(sw.Handle))
                            {
                                if (objRef.IsValid)
                                    ReturnSwordToPlace(objRef.Object);
                            }
                        }
                    }
                    else if(!isForced && sw.CanPlayFadeout(now))
                    {
                        sw.FadingOut = true;
                        sw.FadeTime = now;

                        using (var objRef = new ObjectRefHolder(sw.Handle))
                        {
                            if (objRef.IsValid)
                                StopSwordEffect(objRef.Object, z == 1);
                        }
                    }
                }
            }
        }
        
        private static void TryPlaceSwordNow(bool ghost)
        {
            InitSwords();
            
            var plr = PlayerCharacter.Instance;
            if (plr == null)
                return;

            double now = Time;
            uint chosen = 0;
            int ci = 0;
            var data = ghost ? ghost_swords : normal_swords;

            // Barrage rate of fire?
            if(ghost)
            {
                foreach(var sw in data.swords)
                {
                    if (now - sw.CreateTime < SettingsInstance.SwordBarrage_SpawnDelay)
                        return;
                }
            }

            if (data.forced_grab != null)
                return;

            sword_instance inst = null;

            // try select random first.
            if(data.swords.Count != 0)
            {
                int randomTried = 0;
                while(randomTried++ < 2)
                {
                    int chosenIndex = NetScriptFramework.Tools.Randomizer.NextInt(0, data.swords.Count);
                    var sword = data.swords[chosenIndex];

                    if(sword.IsFreeForSummon(now))
                    {
                        chosen = sword.Handle;
                        data.next_index = chosenIndex + 1;
                        inst = sword;
                        ci = chosenIndex;
                        break;
                    }
                }
            }

            if(inst == null)
            {
                int maxTry = data.swords.Count;

                for(int i = 0; i < maxTry; i++)
                {
                    int chosenIndex = (data.next_index + i) % maxTry;
                    var sword = data.swords[chosenIndex];

                    if(sword.IsFreeForSummon(now))
                    {
                        chosen = sword.Handle;
                        data.next_index = i + 1;
                        inst = sword;
                        ci = i;
                        break;
                    }
                }
            }

            if (chosen == 0)
                return;

            var cell = plr.ParentCell;
            if (cell == null || !cell.IsAttached)
                return;

            if (!CalculateSwordPlacePosition(50.0f, false, ghost))
                return;

            using (var objRef = new ObjectRefHolder(chosen))
            {
                if (!objRef.IsValid)
                    return;

                using (var plrHold = new ObjectRefHolder(plr))
                {
                    var cellPtr = cell != null ? cell.Cast<TESObjectCELL>() : IntPtr.Zero;

                    var ws = cell != null ? cell.WorldSpace : null;
                    var wsPtr = ws != null ? ws.Cast<TESWorldSpace>() : IntPtr.Zero;

                    float[] go = new float[6];
                    for (int i = 0; i < 3; i++)
                        go[i] = Memory.ReadFloat(sword_data.Temp2.Cast<NiPoint3>() + 4 * i);
                    for (int i = 0; i < 3; i++)
                        go[i + 3] = Memory.ReadFloat(sword_data.Temp3.Cast<NiPoint3>() + 4 * i);
                    sword_data.Temp2.Z += first_TeleportZOffset;

                    Memory.InvokeCdecl(addr_SetPosButReallyGoodly, objRef.Object.Cast<TESObjectREFR>(), plrHold.Handle, cellPtr, wsPtr, sword_data.Temp2.Cast<NiPoint3>(), sword_data.Temp3.Cast<NiPoint3>());
                    if (inst != null)
                    {
                        inst.WaitingEffect = (byte)(ghost ? 2 : 1);
                        inst.CreateTime = now;
                        inst.FadedOut = false;
                        inst.Goto = go;
                        data.forced_grab = inst;
                    }
                }
            }
        }

        private static bool CalculateSwordPlacePosition(float extraRadiusOfSword, bool forcePlaceInBadPosition, bool ghost)
        {
            var plr = PlayerCharacter.Instance;
            if (plr == null)
                return false;

            var rootPlr = plr.Node;
            if (rootPlr == null)
                return false;

            var head = rootPlr.LookupNodeByName("NPC Head [Head]");
            if (head == null)
                head = rootPlr;

            var cell = plr.ParentCell;
            if (cell == null || !cell.IsAttached)
                return false;

            /*var pcam = PlayerCamera.Instance;
            if (pcam == null)
                return;

            var camNode = pcam.Node;
            if (camNode == null)
                return;*/

            List<NiNode> ignore_ls = new List<NiNode>();
            ignore_ls.Add(rootPlr);
            var fs = plr.GetSkeletonNode(true);
            if (fs != null)
                ignore_ls.Add(fs);

            lock(CachedHandlesLocker)
            {
                foreach(var d in CachedHeldHandles)
                {
                    using (var objRef = new ObjectRefHolder(d.Key))
                    {
                        if(objRef.IsValid)
                        {
                            var onode = objRef.Object.Node;
                            if (onode != null)
                                ignore_ls.Add(onode);
                        }
                    }
                }
            }

            var camWt = rootPlr.WorldTransform;
            float[] begin;
            float[] end;
            using (var alloc = Memory.Allocate(0x50))
            {
                Memory.Copy(camWt.Cast<NiTransform>(), alloc.Address, 0x34);
                camWt = MemoryObject.FromAddress<NiTransform>(alloc.Address);

                var hpos = head.WorldTransform.Position;
                var bpos = camWt.Position;
                bpos.X = hpos.X;
                bpos.Y = hpos.Y;
                bpos.Z = hpos.Z;

                sword_data.Temp1.Y = ghost ? SettingsInstance.MagicSwordBarrage_PlaceDistance : SettingsInstance.MagicSwordBlast_PlaceDistance;
                if(ghost)
                {
                    sword_data.Temp1.X = 0.0f;
                    sword_data.Temp1.Z = 0.0f;
                }
                else
                {
                    // Some offset?
                    sword_data.Temp1.X = 0.0f;
                    sword_data.Temp1.Z = 0.0f;
                }

                camWt.Translate(sword_data.Temp1, sword_data.Temp2);
                camWt.Rotation.GetEulerAngles(sword_data.Temp3);

                begin = new float[] { hpos.X, hpos.Y, hpos.Z };
                end = new float[] { sword_data.Temp2.X, sword_data.Temp2.Y, sword_data.Temp2.Z };
            }

            var rp = TESObjectCELL.RayCast(new RayCastParameters()
            {
                Begin = begin,
                End = end,
                Cell = cell
            });
            float frac = 1.0f;
            foreach (var r in rp)
            {
                if (r.Fraction >= frac || r.HavokObject == IntPtr.Zero)
                    continue;

                uint flags = Memory.ReadUInt32(r.HavokObject + 0x2C) & 0x7F;
                ulong mask = (ulong)1 << (int)flags;
                if ((RaycastMask & mask) == 0)
                    continue;

                var cobj = r.Object;
                if (cobj != null)
                {
                    bool had = false;
                    for (int i = 0; i < ignore_ls.Count; i++)
                    {
                        var co = ignore_ls[i];
                        if (co != null && co.Equals(cobj))
                        {
                            had = true;
                            break;
                        }
                    }

                    if (had)
                        continue;
                }

                frac = r.Fraction;
            }

            float frac_extent = extraRadiusOfSword / Math.Max(1.0f, sword_data.Temp1.Y);
            frac -= frac_extent;

            // Can't fit here.
            if (!forcePlaceInBadPosition && frac < frac_extent)
                return false;

            if (frac < 1.0f)
            {
                for (int i = 0; i < 3; i++)
                    end[i] = (end[i] - begin[i]) * frac + begin[i];
            }

            sword_data.Temp2.X = end[0];
            sword_data.Temp2.Y = end[1];
            sword_data.Temp2.Z = end[2];

            return true;
        }

        private static readonly sword_data normal_swords = new sword_data();
        private static readonly sword_data ghost_swords = new sword_data();

        private static int ShouldLaunchObjectNow(ActiveEffect ef)
        {
            if (SettingsInstance.AlwaysLaunchObjectsEvenWhenNotFinishedPulling)
                return 1;

            if (ef == null)
                return 0;

            var efs = ef.BaseEffect;
            if (efs == null)
                return 0;

            var st = IsOurSpell(efs);
            if (st == OurSpellTypes.SwordBarrage)
                return 1;

            return 0;
        }

        private static bool CanPickTelekinesisTarget(TESObjectREFR obj, List<EffectSetting> casting, bool actuallyTakingNow)
        {
            if (obj == null)
                return false;

            var bform = obj.BaseForm;
            if (bform == null)
                return false;

            bool castingGhost = false;
            bool castingNormal = false;

            foreach(var ef in casting)
            {
                var st = IsOurSpell(ef);
                switch(st)
                {
                    case OurSpellTypes.SwordBarrage: castingGhost = true; break;
                    case OurSpellTypes.SwordBlast: castingNormal = true; break;
                }
            }

            if(castingGhost)
            {
                if (ghost_swords.forced_grab == null)
                    return false;

                if (IsOurItem(bform) != OurItemTypes.GhostSword)
                    return false;

                using (var objHandle = new ObjectRefHolder(obj))
                {
                    uint handleId = objHandle.Handle;
                    if (handleId == 0)
                        return false;

                    if (ghost_swords.forced_grab.Handle != handleId)
                        return false;
                }
            }

            if(castingNormal)
            {
                if (normal_swords.forced_grab == null)
                    return false;

                if(IsOurItem(bform) != OurItemTypes.IronSword)
                    return false;

                using (var objHandle = new ObjectRefHolder(obj))
                {
                    uint handleId = objHandle.Handle;
                    if (handleId == 0)
                        return false;

                    if (normal_swords.forced_grab.Handle != handleId)
                        return false;
                }
            }

            return true;
        }

        private static void OnSucceedPickTelekinesisTarget(EffectSetting efs, uint handleId)
        {
            
        }

        private static void OnFailPickTelekinesisTarget(EffectSetting efs, bool failBecauseAlreadyMax, int hadObjCount)
        {
            if (efs == null || failBecauseAlreadyMax)
                return;

            var st = IsOurSpell(efs);
            switch(st)
            {
                case OurSpellTypes.SwordBarrage:
                    {
                        TryPlaceSwordNow(true);
                    }
                    break;

                case OurSpellTypes.SwordBlast:
                    {
                        TryPlaceSwordNow(false);
                    }
                    break;
            }
        }

        private static OurSpellTypes IsOurSpell(EffectSetting ef)
        {
            if(ef != null)
            {
                for(int i = 0; i < (int)spell_types.max; i++)
                {
                    var inf = spellInfos[i];
                    if(inf.Effect != null && inf.Effect.Equals(ef))
                    {
                        switch((spell_types)i)
                        {
                            case spell_types.normal: return OurSpellTypes.TelekNormal;
                            case spell_types.reach: return OurSpellTypes.TelekReach;
                            case spell_types.single: return OurSpellTypes.TelekOne;
                            case spell_types.enemy: return OurSpellTypes.None;
                            case spell_types.blast: return OurSpellTypes.SwordBlast;
                            case spell_types.barrage: return OurSpellTypes.SwordBarrage;
                        }
                    }
                }
            }

            return OurSpellTypes.None;
        }

        private static OurItemTypes IsOurItem(TESForm baseForm)
        {
            if(baseForm != null)
            {
                for (int i = 0; i < (int)spell_types.max; i++)
                {
                    var inf = spellInfos[i];
                    if (inf.Item != null && inf.Item.Contains(baseForm.FormId))
                    {
                        switch ((spell_types)i)
                        {
                            case spell_types.normal: return OurItemTypes.None;
                            case spell_types.reach: return OurItemTypes.None;
                            case spell_types.single: return OurItemTypes.None;
                            case spell_types.enemy: return OurItemTypes.None;
                            case spell_types.blast: return OurItemTypes.IronSword;
                            case spell_types.barrage: return OurItemTypes.GhostSword;
                        }
                    }
                }
            }

            return OurItemTypes.None;
        }

        private static bool HasAnyNormalTelekInHand()
        {
            var plr = PlayerCharacter.Instance;
            if (plr == null)
                return false;

            for(int i = 0; i < 2; i++)
            {
                var caster = plr.GetMagicCaster((EquippedSpellSlots)i);
                if (caster == null)
                    continue;

                var item = caster.CastItem;
                if (item == null)
                    continue;

                var efls = item.Effects;
                if (efls == null)
                    continue;

                foreach(var x in efls)
                {
                    var ef = x.Effect;
                    if(ef != null)
                    {
                        switch(IsOurSpell(ef))
                        {
                            case OurSpellTypes.TelekNormal:
                            case OurSpellTypes.TelekOne:
                                return true;
                        }
                    }
                }
            }

            return false;
        }

        internal enum OurItemTypes : int
        {
            None = 0,

            GhostSword,
            IronSword,
        }

        internal enum OurSpellTypes : int
        {
            None,

            SwordBarrage,
            SwordBlast,
            TelekOne,
            TelekNormal,
            TelekReach,
        }
    }

    internal sealed class sword_data
    {
        internal List<sword_instance> swords = new List<sword_instance>();
        internal readonly Dictionary<uint, sword_instance> lookup = new Dictionary<uint, sword_instance>();

        internal int next_index = 0;

        internal sword_instance forced_grab;

        internal void AddSword_FormId(uint formId, string fileName, bool ghost)
        {
            var form = TESForm.LookupFormFromFile(formId, fileName) as TESObjectREFR;
            if(form != null)
                this.AddSword_Obj(form, ghost);
        }
        
        internal void AddSword_Obj(TESObjectREFR obj, bool ghost)
        {
            if (obj == null)
                return;

            using (var objRef = new ObjectRefHolder(obj))
            {
                if (objRef.IsValid)
                {
                    var sw = new sword_instance()
                    {
                        Handle = objRef.Handle
                    };
                    swords.Add(sw);
                    lookup[objRef.Handle] = sw;

                    int ix = swords.Count - 1;
                    var allItem = BetterTelekinesisPlugin.spellInfos[ghost ? (int)BetterTelekinesisPlugin.spell_types.barrage : (int)BetterTelekinesisPlugin.spell_types.blast].Item;
                    uint fid = 0;
                    if (allItem != null && allItem.Count != 0)
                        fid = allItem.ElementAt(ix % allItem.Count);

                    var form = TESForm.LookupFormById(fid) as TESObjectMISC;
                    if (form != null)
                        Memory.WritePointer(objRef.Object.Cast<TESObjectREFR>() + 0x40, form.Cast<TESObjectMISC>());
                }
            }
        }

        internal static NiPoint3 Temp1;
        internal static NiPoint3 Temp2;
        internal static NiPoint3 Temp3;
        internal static IntPtr Return1;
        internal static IntPtr Return2;
    }

    internal sealed class sword_instance
    {
        internal uint Handle;
        internal byte WaitingEffect;
        internal bool Held;
        internal bool FadingOut;
        internal bool FadedOut = true;
        internal double FadeTime;
        internal double CreateTime;
        internal double LaunchTime;
        internal double HeldTime;
        internal float[] Goto;
        internal byte WaitEffectCounter;

        internal bool IsFreeForSummon(double now)
        {
            if (!this.FadedOut || this.Held)
                return false;

            if (this.IsWaitingEffect(now))
                return false;

            if (now - this.LaunchTime < 3.0)
                return false;

            if (now - this.CreateTime < 3.0)
                return false;

            return true;
        }

        internal bool IsWaitingEffect(double now)
        {
            return this.WaitingEffect != 0 && now - this.CreateTime < 0.3;
        }

        internal bool CanPlayFadeout(double now)
        {
            if (this.FadedOut || this.Held || this.FadingOut || now - this.HeldTime < Lifetime || now - this.CreateTime < Lifetime)
                return false;

            return true;
        }

        internal bool IsWaitingInvis(double now)
        {
            if (this.FadedOut || !this.FadingOut)
                return false;

            return true;
        }

        internal static double Lifetime
        {
            get
            {
                return BetterTelekinesisPlugin.SettingsInstance.MagicSword_RemoveDelay;
            }
        }
    }

    internal static class find_nearest_node_helper
    {
        private static bool inited = false;

        private static NiPoint3 Begin;
        private static NiPoint3 End;
        private static NiPoint3 Temp1;
        private static NiPoint3 Temp2;
        private static NiPoint3 Temp3;
        private static NiPoint3 Temp4;

        internal static void init()
        {
            inited = true;

            var alloc = Memory.Allocate(0x100);
            alloc.Pin();

            var ptr = alloc.Address;

            Begin = MemoryObject.FromAddress<NiPoint3>(ptr);
            ptr = ptr + 0x10;

            End = MemoryObject.FromAddress<NiPoint3>(ptr);
            ptr = ptr + 0x10;

            Temp1 = MemoryObject.FromAddress<NiPoint3>(ptr);
            ptr = ptr + 0x10;

            Temp2 = MemoryObject.FromAddress<NiPoint3>(ptr);
            ptr = ptr + 0x10;

            Temp3 = MemoryObject.FromAddress<NiPoint3>(ptr);
            ptr = ptr + 0x10;

            Temp4 = MemoryObject.FromAddress<NiPoint3>(ptr);
            ptr = ptr + 0x10;

            Temp1.X = 0.0f;
            Temp1.Y = 5000.0f;
            Temp1.Z = 0.0f;
        }

        internal static NiNode FindBestNodeInCrosshair(NiNode root)
        {
            if (!inited)
                return null;

            var pcam = PlayerCamera.Instance;
            if (pcam == null)
                return null;

            var camNode = pcam.Node;
            if (camNode == null)
                return null;

            var wt = camNode.WorldTransform;
            var wtpos = wt.Position;
            Begin.X = wtpos.X;
            Begin.Y = wtpos.Y;
            Begin.Z = wtpos.Z;

            wt.Translate(Temp1, End);

            var r = new temp_calc();
            r.best = root;
            r.dist = GetDistance(root);

            explore_calc(root, r);

            return r.best;
        }

        private static void explore_calc(NiNode current, temp_calc state)
        {
            var arr = current.Children;
            if (arr == null)
                return;

            foreach(var ch in arr)
            {
                var cn = ch as NiNode;
                if (cn == null)
                    continue;

                // fade node is stuff like weapon, shield, and they don't allow us to move them by it properly.

                bool exclude = cn is BSFadeNode;
                if (!exclude)
                {
                    var cobj = Memory.InvokeCdecl(BetterTelekinesisPlugin.addr_GetbhkColl, cn.Cast<NiNode>());
                    if (cobj == IntPtr.Zero)
                        exclude = true;
                    else
                    {
                        var rigid = Memory.InvokeCdecl(BetterTelekinesisPlugin.addr_GetbhkRigid, cobj);
                        if (rigid == IntPtr.Zero)
                            exclude = true;
                        else
                        {
                            var hk = Memory.ReadPointer(rigid + 0x10);
                            if (hk == IntPtr.Zero)
                                exclude = true;
                            else
                            {
                                CollisionLayers layer = (CollisionLayers)(Memory.ReadUInt32(hk + 0x2C) & 0x7F);
                                if (layer == CollisionLayers.Unidentified)
                                    exclude = true;
                            }
                        }
                    }

                    if (!exclude && BetterTelekinesisPlugin.ExcludeActorNodes != null)
                    {
                        var nmb = cn.Name;
                        if (nmb != null)
                        {
                            string nt = nmb.Text;
                            if (nt != null && BetterTelekinesisPlugin.ExcludeActorNodes.Contains(nt))
                                exclude = true;
                        }
                    }
                }
                
                if (!exclude)
                {
                    float dx = GetDistance(cn);
                    if (dx < state.dist)
                    {
                        state.dist = dx;
                        state.best = cn;
                    }
                }

                explore_calc(cn, state);
            }
        }

        private static float GetDistance(NiNode n)
        {
            var np = n.Parent;
            if (np == null)
                return 999999.0f;

            /*var npos = n.WorldTransform.Position;
            var ppos = np.WorldTransform.Position;
            var qpos = Temp4;
            qpos.X = (npos.X - ppos.X) * 0.5f + ppos.X;
            qpos.Y = (npos.Y - ppos.Y) * 0.5f + ppos.Y;
            qpos.Z = (npos.Z - ppos.Z) * 0.5f + ppos.Z;*/
            var qpos = n.WorldTransform.Position;

            qpos.Subtract(Begin, Temp2);
            qpos.Subtract(End, Temp3);
            Temp2.Cross(Temp3, Temp3);
            float len1 = Temp3.Length;
            End.Subtract(Begin, Temp3);
            float len2 = Temp3.Length;

            if (len2 <= 0.0001f)
                return 999999.0f;

            return len1 / len2;
        }

        private sealed class temp_calc
        {
            internal NiNode best;
            internal float dist;
        }
    }

    internal sealed class random_move_generator
    {
        private float current_x;
        private float current_y;
        private float target_x;
        private float target_y;
        private float speed_x;
        private float speed_y;
        private byte has_target;
        private bool disable;

        private static float speed_change = 0.3f;
        private static float max_speed = 1.0f;
        private static float extent_mult
        {
            get
            {
                return BetterTelekinesisPlugin.SettingsInstance.MultiObjectHoverAmount;
            }
        }

        internal float CurrentX
        {
            get
            {
                return this.current_x;
            }
        }

        internal float CurrentY
        {
            get
            {
                return this.current_y;
            }
        }

        internal void update(float diff)
        {
            if (this.disable || diff <= 0.0f)
                return;

            float ha = extent_mult;
            if(ha <= 0.0f)
            {
                disable = true;
                return;
            }

            if(this.has_target == 0)
                this.select_target();

            this.update_speed(diff);

            this.update_pos(diff);
        }

        private void update_pos(float diff)
        {
            this.current_x += this.speed_x * diff;
            this.current_y += this.speed_y * diff;

            if((this.has_target & 1) != 0)
            {
                if (this.target_x < 0.0f)
                {
                    if (this.current_x <= this.target_x)
                        this.has_target &= 0xFE;
                }
                else
                {
                    if (this.current_x >= this.target_x)
                        this.has_target &= 0xFE;
                }
            }

            if ((this.has_target & 2) != 0)
            {
                if (this.target_y < 0.0f)
                {
                    if (this.current_y <= this.target_y)
                        this.has_target &= 0xFD;
                }
                else
                {
                    if (this.current_y >= this.target_y)
                        this.has_target &= 0xFD;
                }
            }
        }

        private void update_speed(float diff)
        {
            float mod;
            if (this.current_x < this.target_x)
                mod = diff * speed_change;
            else
                mod = -(diff * speed_change);

            this.speed_x += mod;
            if (Math.Abs(this.speed_x) > max_speed)
                this.speed_x = this.speed_x < 0.0f ? (-max_speed) : max_speed;

            if (this.current_y < this.target_y)
                mod = diff * speed_change;
            else
                mod = -(diff * speed_change);

            this.speed_y += mod;
            if (Math.Abs(this.speed_y) > max_speed)
                this.speed_y = this.speed_y < 0.0f ? (-max_speed) : max_speed;
        }

        private void select_target()
        {
            float chosen_x = (float)((NetScriptFramework.Tools.Randomizer.NextDouble() - 0.5) * 2.0 * extent_mult);
            float chosen_y = (float)((NetScriptFramework.Tools.Randomizer.NextDouble() - 0.5) * 2.0 * extent_mult);

            int had_q = GetQuadrant(current_x, current_y);
            int has_q = GetQuadrant(chosen_x, chosen_y);

            if(had_q == has_q)
            {
                if ((has_q & 1) != 0)
                    chosen_x -= extent_mult;
                else
                    chosen_x += extent_mult;

                if ((has_q & 2) != 0)
                    chosen_y -= extent_mult;
                else
                    chosen_y += extent_mult;
            }

            this.target_x = chosen_x;
            this.target_y = chosen_y;
            this.has_target = 3;
        }

        private static int GetQuadrant(float x, float y)
        {
            int q = 0;

            if (x >= 0.0f)
                q |= 1;
            if (y >= 0.0f)
                q |= 2;

            return q;
        }
    }

    internal static class leveled_list_helper
    {
        internal enum schools : int
        {
            alteration,
            conjuration,
            destruction,
            illusion,
            restoration,
        }

        internal enum levels : int
        {
            novice,
            apprentice,
            adept,
            expert,
            master,
        }

        private static void AddLeveledList(List<TESLeveledList> ls, uint id)
        {
            var form = TESForm.LookupFormById(id) as TESLeveledList;
            if(form == null)
            {
                NetScriptFramework.Main.WriteDebugMessage("Warning: leveled list " + id.ToString("X") + " was not found!");
                return;
            }

            ls.Add(form);
        }

        private static void FindLeveledLists(schools school, levels level, List<TESLeveledList> all, List<TESLeveledList> one)
        {
            switch(level)
            {
                case levels.novice:
                    {
                        AddLeveledList(all, 0xA297A);
                        AddLeveledList(one, 0x10FD8C);

                        switch(school)
                        {
                            case schools.alteration:
                                AddLeveledList(all, 0x10F64E);
                                AddLeveledList(one, 0x9E2B0);
                                break;

                            case schools.conjuration:
                                AddLeveledList(all, 0x10F64F);
                                AddLeveledList(one, 0x9E2B1);
                                break;

                            case schools.destruction:
                                AddLeveledList(all, 0x10F650);
                                AddLeveledList(one, 0x9E2B2);
                                break;

                            case schools.illusion:
                                AddLeveledList(all, 0x10F651);
                                AddLeveledList(one, 0x9E2B3);
                                break;

                            case schools.restoration:
                                AddLeveledList(all, 0x10F652);
                                AddLeveledList(one, 0x9E2B4);
                                break;
                        }
                    }
                    break;

                case levels.apprentice:
                    {
                        AddLeveledList(all, 0x10523F);
                        AddLeveledList(one, 0x10FD8D);

                        switch (school)
                        {
                            case schools.alteration:
                                AddLeveledList(all, 0xA297D);
                                AddLeveledList(one, 0xA272A);
                                break;

                            case schools.conjuration:
                                AddLeveledList(all, 0xA297E);
                                AddLeveledList(one, 0xA272B);
                                break;

                            case schools.destruction:
                                AddLeveledList(all, 0xA297F);
                                AddLeveledList(one, 0xA272C);
                                break;

                            case schools.illusion:
                                AddLeveledList(all, 0xA2980);
                                AddLeveledList(one, 0xA272D);
                                break;

                            case schools.restoration:
                                AddLeveledList(all, 0xA2981);
                                AddLeveledList(one, 0xA272E);
                                break;
                        }
                    }
                    break;

                case levels.adept:
                    {
                        AddLeveledList(one, 0x10FCF0);

                        switch (school)
                        {
                            case schools.alteration:
                                AddLeveledList(all, 0xA298C);
                                AddLeveledList(one, 0xA2735);
                                break;

                            case schools.conjuration:
                                AddLeveledList(all, 0xA298D);
                                AddLeveledList(one, 0xA2730);
                                break;

                            case schools.destruction:
                                AddLeveledList(all, 0xA298E);
                                AddLeveledList(one, 0xA2731);
                                break;

                            case schools.illusion:
                                AddLeveledList(all, 0xA298F);
                                AddLeveledList(one, 0xA2732);
                                break;

                            case schools.restoration:
                                AddLeveledList(all, 0xA2990);
                                AddLeveledList(one, 0xA2734);
                                break;
                        }
                    }
                    break;

                case levels.expert:
                case levels.master: // add master to expert because they are treated as special by game and don't show up in normal vendors
                    {
                        AddLeveledList(one, 0x10FCF1);

                        switch (school)
                        {
                            case schools.alteration:
                                AddLeveledList(all, 0xA2982);
                                AddLeveledList(one, 0xA272F);
                                break;

                            case schools.conjuration:
                                AddLeveledList(all, 0xA2983);
                                AddLeveledList(one, 0xA2736);
                                break;

                            case schools.destruction:
                                AddLeveledList(all, 0xA2984);
                                AddLeveledList(one, 0xA2737);
                                break;

                            case schools.illusion:
                                AddLeveledList(all, 0xA2985);
                                AddLeveledList(one, 0xA2738);
                                break;

                            case schools.restoration:
                                AddLeveledList(all, 0xA2986);
                                AddLeveledList(one, 0xA2739);
                                break;
                        }
                    }
                    break;

                /*case levels.master:
                    {
                        AddLeveledList(all, 0x);
                        AddLeveledList(one, 0x);

                        switch (school)
                        {
                            case schools.alteration:
                                AddLeveledList(all, 0x);
                                AddLeveledList(one, 0xDD645);
                                break;

                            case schools.conjuration:
                                AddLeveledList(all, 0x);
                                AddLeveledList(one, 0xA273A);
                                break;

                            case schools.destruction:
                                AddLeveledList(all, 0x);
                                AddLeveledList(one, 0xA273B);
                                break;

                            case schools.illusion:
                                AddLeveledList(all, 0x);
                                AddLeveledList(one, 0xA273C);
                                break;

                            case schools.restoration:
                                AddLeveledList(all, 0x);
                                AddLeveledList(one, 0xDD648);
                                break;
                        }
                    }
                    break;*/
            }
        }

        private static void ChangeSpellSchool(SpellItem spell, TESObjectBOOK book)
        {
            int minSkill = 0;

            var efls = spell.Effects;
            if(efls != null)
            {
                foreach(var x in efls)
                {
                    var ef = x.Effect;
                    if (ef == null)
                        continue;

                    ef.AssociatedSkill = ActorValueIndices.Alteration;
                    minSkill = Math.Max(minSkill, ef.MinimumSkill);
                }
            }

            string str = @"Clutter\Books\SpellTomeAlterationLowPoly.nif";
            byte[] buf = Encoding.UTF8.GetBytes(str);
            using (var alloc = Memory.Allocate(buf.Length + 2))
            {
                Memory.WriteBytes(alloc.Address, buf);
                Memory.WriteUInt8(alloc.Address + buf.Length, 0);
                book.InvokeVTableThisCall<TESModelTextureSwap>(0x28, alloc.Address);
            }

            var form = TESForm.LookupFormById(0x2FBB3) as TESObjectSTAT;
            if (form != null)
                Memory.WritePointer(book.Cast<TESObjectBOOK>() + 0x120, form.Cast<TESObjectSTAT>());

            levels lv;
            if (minSkill >= 100)
                lv = levels.master;
            else if (minSkill >= 75)
                lv = levels.expert;
            else if (minSkill >= 50)
                lv = levels.adept;
            else if (minSkill >= 25)
                lv = levels.apprentice;
            else
                lv = levels.novice;

            uint perkId = 0;
            switch(lv)
            {
                case levels.novice: perkId = 0xF2CA6; break;
                case levels.apprentice: perkId = 0xC44B7; break;
                case levels.adept: perkId = 0xC44B8; break;
                case levels.expert: perkId = 0xC44B9; break;
                case levels.master: perkId = 0xC44BA; break;
            }

            var perk = perkId != 0 ? TESForm.LookupFormById(perkId) as BGSPerk : null;
            if (perk == null)
                Memory.WritePointer(spell.Cast<SpellItem>() + 0xE0, IntPtr.Zero);
            else
                Memory.WritePointer(spell.Cast<SpellItem>() + 0xE0, perk.Cast<BGSPerk>());
        }

        private static void ActualAdd(TESLeveledList list, TESObjectBOOK book, bool all)
        {
            if(list != null && book != null)
                list.AddLeveledListEntry(book, 1, 1, null);
        }

        internal static void AddToLeveledList(TESObjectBOOK spellBook)
        {
            if (spellBook == null || !spellBook.IsSpellBook)
                return;

            SpellItem spell;
            try
            {
                spell = spellBook.BookData.Teaches.SpellToTeach;
            }
            catch
            {
                NetScriptFramework.Main.WriteDebugMessage("AddToLeveledList.getSpell threw exception!");
                return;
            }

            if (spell == null)
                return;

            if (BetterTelekinesisPlugin.SettingsInstance.MakeSwordSpellsAlterationInstead)
                ChangeSpellSchool(spell, spellBook);

            if (!BetterTelekinesisPlugin.SettingsInstance.AddSwordSpellsToLeveledLists)
                return;

            var efls = spell.Effects;
            if (efls == null)
                return;

            int high_skill = 0;
            ActorValueIndices av_choice = ActorValueIndices.Max;
            foreach(var x in efls)
            {
                var ef = x.Effect;
                if (ef == null)
                    continue;

                high_skill = Math.Max(high_skill, ef.MinimumSkill);
                if(av_choice < ActorValueIndices.Alteration || av_choice > ActorValueIndices.Restoration)
                    av_choice = ef.AssociatedSkill;
            }

            schools sc;
            levels lv;

            switch(av_choice)
            {
                case ActorValueIndices.Alteration: sc = schools.alteration; break;
                case ActorValueIndices.Conjuration: sc = schools.conjuration; break;
                case ActorValueIndices.Destruction: sc = schools.destruction; break;
                case ActorValueIndices.Illusion: sc = schools.illusion; break;
                case ActorValueIndices.Restoration: sc = schools.restoration; break;
                default:
                    return;
            }

            if (high_skill >= 100)
                lv = levels.master;
            else if (high_skill >= 75)
                lv = levels.expert;
            else if (high_skill >= 50)
                lv = levels.adept;
            else if (high_skill >= 25)
                lv = levels.apprentice;
            else
                lv = levels.novice;

            List<TESLeveledList> all = new List<TESLeveledList>();
            List<TESLeveledList> one = new List<TESLeveledList>();

            FindLeveledLists(sc, lv, all, one);

            foreach (var x in all)
                ActualAdd(x, spellBook, true);

            foreach (var x in one)
                ActualAdd(x, spellBook, false);
        }
    }
}
