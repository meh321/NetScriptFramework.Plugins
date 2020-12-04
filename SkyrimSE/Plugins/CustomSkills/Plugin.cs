using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomSkills
{
    public sealed class CustomSkillsPlugin : NetScriptFramework.Plugin
    {
        public override string Key
        {
            get
            {
                return "custom_skills";
            }
        }

        public override string Name
        {
            get
            {
                return "Custom Skills";
            }
        }

        public override int Version
        {
            get
            {
                return 1;
            }
        }

        public override string Author
        {
            get
            {
                return "meh321";
            }
        }

        public override int RequiredLibraryVersion
        {
            get
            {
                return 13;
            }
        }

        private NetScriptFramework.SkyrimSE.BGSSkillPerkTreeNode OriginalSkillTree;
        private readonly List<Skill> Skills = new List<Skill>();
        private Skill MenuSkill = null;
        private const string MenuName = "StatsMenu";
        private const NetScriptFramework.SkyrimSE.ActorValueIndices MenuAV = NetScriptFramework.SkyrimSE.ActorValueIndices.Enchanting;
        private byte MenuState; // 0 nothing, 1 waiting to open, 2 is open, 3 waiting to close

        private void ShowLevelup(string name, int level)
        {
            var ptr = NetScriptFramework.Memory.ReadPointer(this.addr_GameSettingSkillIncreased + 8);
            if (ptr == IntPtr.Zero)
                return;

            var text = NetScriptFramework.Memory.ReadString(ptr, false);
            if (string.IsNullOrEmpty(text))
                return;

            text = text.Replace("%s", name);
            text = text.Replace("%i", level.ToString());

            byte[] buf = Encoding.UTF8.GetBytes(text);
            using (var alloc = NetScriptFramework.Memory.Allocate(buf.Length + 16))
            {
                NetScriptFramework.Memory.WriteZero(alloc.Address, alloc.Size);
                NetScriptFramework.Memory.WriteBytes(alloc.Address, buf);

                NetScriptFramework.Memory.InvokeCdecl(this.addr_ShowSpecialHUDMessage, 20, alloc.Address, 0, 0);
            }
        }

        private void CloseStatsMenu()
        {
            this.MenuState = 3;

            using (var str = new NetScriptFramework.SkyrimSE.StringRefHolder(MenuName))
            {
                var mgr = NetScriptFramework.Memory.ReadPointer(this.addr_MenuManager);
                if (mgr != IntPtr.Zero)
                    NetScriptFramework.Memory.InvokeCdecl(this.addr_SendMenuCommand, mgr, str.AddressOf, 3, 0);
            }
        }

        private void OpenStatsMenu()
        {
            var sk = this.MenuSkill;
            if (sk == null || sk.SkillTree == null)
                return;

            var av = NetScriptFramework.Memory.InvokeCdecl(this.addr_GetActorValueInfo, (int)MenuAV);
            if (av == IntPtr.Zero)
                return;

            var pt = NetScriptFramework.Memory.ReadPointer(av + 0x118);
            if (pt == IntPtr.Zero)
                return;

            if (this.OriginalSkillTree == null)
                this.OriginalSkillTree = NetScriptFramework.MemoryObject.FromAddress<NetScriptFramework.SkyrimSE.BGSSkillPerkTreeNode>(pt);

            var nt = sk.SkillTree.Cast<NetScriptFramework.SkyrimSE.BGSSkillPerkTreeNode>();
            if (nt != pt)
                NetScriptFramework.Memory.WritePointer(av + 0x118, nt);

            this.MenuState = 1;

            NetScriptFramework.Memory.InvokeCdecl(this.addr_FadeFn, 1, 1, 1.0f, 1, 0); // fade to black? otherwise the stats menu is transparent in current game world
            NetScriptFramework.Memory.InvokeCdecl(this.addr_OpenStatsMenu, 0);
        }

        private bool IsMenuControlsEnabled
        {
            get
            {
                return NetScriptFramework._IntPtrExtensions.ToBool(NetScriptFramework.Memory.InvokeCdecl(this.addr_IsMenuControlsEnabled));
            }
        }

        private bool IsStatsMenuOpen
        {
            get
            {
                var mgr = NetScriptFramework.SkyrimSE.MenuManager.Instance;
                return mgr != null && mgr.IsMenuOpen(MenuName);
            }
        }

        private byte CurrentPerkPoints
        {
            get
            {
                if (this.IsOurMenuMode)
                {
                    var sk = this.MenuSkill;
                    if (sk != null && sk.PerkPoints != null)
                    {
                        short v = sk.PerkPoints.Value;
                        if (v <= 0)
                            return 0;
                        if (v > 255)
                            return 255;
                        return (byte)v;
                    }
                }

                var plr = NetScriptFramework.SkyrimSE.PlayerCharacter.Instance;
                if (plr != null)
                    return NetScriptFramework.Memory.ReadUInt8(plr.Cast<NetScriptFramework.SkyrimSE.PlayerCharacter>() + 0xB01);

                return 0;
            }
            set
            {
                if (this.IsOurMenuMode)
                {
                    var sk = this.MenuSkill;
                    if (sk != null && sk.PerkPoints != null)
                    {
                        sk.PerkPoints.Value = value;
                        return;
                    }
                }

                var plr = NetScriptFramework.SkyrimSE.PlayerCharacter.Instance;
                if (plr != null)
                    NetScriptFramework.Memory.WriteUInt8(plr.Cast<NetScriptFramework.SkyrimSE.PlayerCharacter>() + 0xB01, value);
            }
        }

        private bool IsBeastMode
        {
            get
            {
                return NetScriptFramework.Memory.ReadUInt8(this.addr_IsBeastMode) != 0;
            }
        }

        private bool IsOurMenuMode
        {
            get
            {
                return this.MenuState != 0 && this.MenuSkill != null;
            }
        }

        private Skill FindSkillFromGlobalLevel(IntPtr gb_ptr)
        {
            foreach(var sk in this.Skills)
            {
                if(sk.Level != null)
                {
                    var gb = sk.Level.EnsureGlobalWithType(NetScriptFramework.SkyrimSE.TESGlobal.GlobalValueTypes.Int16);
                    if (gb != null && gb.Cast<NetScriptFramework.SkyrimSE.TESGlobal>() == gb_ptr)
                        return sk;
                }
            }

            return null;
        }

        private void UpdateSkills()
        {
            if (NetScriptFramework.SkyrimSE.Main.Instance.IsGamePaused)
                return;

            // Don't update anything if we are already in menu or the menu controls are disabled. This is usually only when some special game event is taking place.
            if (this.IsStatsMenuOpen || !this.IsMenuControlsEnabled)
                return;

            bool reload = false;
            foreach (var sk in this.Skills)
            {
                {
                    var g = sk.OpenMenu;
                    short amt;
                    if (g != null && (amt = g.Value) > 0)
                    {
                        g.Value = 0;
                        
                        this.MenuSkill = sk;
                        this.OpenStatsMenu();
                        return;
                    }
                }

                {
                    var g = sk.ShowLevelup;
                    short amt;
                    if (g != null && (amt = g.Value) > 0)
                    {
                        g.Value = 0;
                        
                        this.ShowLevelup(sk.Name.Value ?? "", amt);
                        return;
                    }
                }

                {
                    var g = sk.DebugReload;
                    if(g != null && g.Value > 0)
                    {
                        g.Value = 0;

                        reload = true;
                        break;
                    }
                }
            }

            if(reload && !this.IsStatsMenuOpen)
            {
                this.Skills.Clear();
                this.Skills.AddRange(Settings.ReadSkills());
            }
        }

        private void UpdateMenu()
        {
            byte s = this.MenuState;
            bool closed = false;
            switch(s)
            {
                case 0: break;

                case 1:
                    if (this.IsStatsMenuOpen)
                        this.MenuState = 2;
                    break;

                case 2:
                    if (!this.IsStatsMenuOpen)
                    {
                        closed = true;
                        this.MenuState = 0;
                    }
                    break;

                case 3:
                    if (!this.IsStatsMenuOpen)
                    {
                        closed = true;
                        this.MenuState = 0;
                    }
                    break;
            }

            if (closed)
            {
                if (this.OriginalSkillTree != null)
                {
                    var av = NetScriptFramework.Memory.InvokeCdecl(this.addr_GetActorValueInfo, (int)MenuAV);
                    if (av != IntPtr.Zero)
                        NetScriptFramework.Memory.WritePointer(av + 0x118, this.OriginalSkillTree.Cast<NetScriptFramework.SkyrimSE.BGSSkillPerkTreeNode>());
                }
            }
        }

        private IntPtr addr_IsMenuControlsEnabled;
        private IntPtr addr_ShowSpecialHUDMessage;
        private IntPtr addr_GameSettingSkillIncreased;
        private IntPtr addr_SendMenuCommand;
        private IntPtr addr_MenuManager;
        private IntPtr addr_OpenStatsMenu;
        private IntPtr addr_IsBeastMode;
        private IntPtr addr_GetActorValueInfo;
        private IntPtr addr_FadeFn;
        private IntPtr addr_LastChosenMenuIndex;
        private IntPtr addr_RequirementBegin;
        private IntPtr addr_GetActorValueName;
        private IntPtr addr_GetComparisonValue;
        private IntPtr addr_CurrentDifficulty;
        private IntPtr addr_ShowMessageBox;
        internal static IntPtr addr_PerkNodeCtor;
        internal static IntPtr addr_ListAlloc;

        protected override bool Initialize(bool loadedAny)
        {
            this.addr_IsMenuControlsEnabled = NetScriptFramework.Main.GameInfo.GetAddressOf(54851);
            this.addr_ShowSpecialHUDMessage = NetScriptFramework.Main.GameInfo.GetAddressOf(50751);
            this.addr_GameSettingSkillIncreased = NetScriptFramework.Main.GameInfo.GetAddressOf(506575);
            this.addr_SendMenuCommand = NetScriptFramework.Main.GameInfo.GetAddressOf(13530);
            this.addr_MenuManager = NetScriptFramework.Main.GameInfo.GetAddressOf(514285);
            this.addr_OpenStatsMenu = NetScriptFramework.Main.GameInfo.GetAddressOf(51643);
            this.addr_IsBeastMode = NetScriptFramework.Main.GameInfo.GetAddressOf(519908);
            this.addr_GetActorValueInfo = NetScriptFramework.Main.GameInfo.GetAddressOf(26569);
            this.addr_FadeFn = NetScriptFramework.Main.GameInfo.GetAddressOf(51909);
            this.addr_LastChosenMenuIndex = NetScriptFramework.Main.GameInfo.GetAddressOf(510254);
            this.addr_RequirementBegin = NetScriptFramework.Main.GameInfo.GetAddressOf(507152);
            this.addr_GetActorValueName = NetScriptFramework.Main.GameInfo.GetAddressOf(26561);
            this.addr_GetComparisonValue = NetScriptFramework.Main.GameInfo.GetAddressOf(29088);
            this.addr_CurrentDifficulty = NetScriptFramework.Main.GameInfo.GetAddressOf(507644);
            this.addr_ShowMessageBox = NetScriptFramework.Main.GameInfo.GetAddressOf(54737);
            addr_PerkNodeCtor = NetScriptFramework.Main.GameInfo.GetAddressOf(26592);
            addr_ListAlloc = NetScriptFramework.Main.GameInfo.GetAddressOf(66908);

            this.WriteHooks();

            NetScriptFramework.SkyrimSE.Events.OnMainMenu.Register(e =>
            {
                this.ApplyHooks();

                _ColorOfSkillNormal = new StringAlloc();
                _ColorOfSkillNormal.Value = "#FFFFFF";
                this.Skills.AddRange(Settings.ReadSkills());
            }, 0, 1);

            NetScriptFramework.SkyrimSE.Events.OnFrame.Register(e =>
            {
                this.UpdateMenu();

                this.UpdateSkills();
            });

            return true;
        }

        private static StringAlloc _ColorOfSkillNormal;

        private void WriteHooks()
        {
            // The NIF background file.
            this.WriteHook(51636, 0xEA3 - 0xB80, "44 89 64 24 48", 5, 5, ctx =>
            {
                if (this.IsOurMenuMode && NetScriptFramework._IntPtrExtensions.ToInt32Safe(ctx.BP) == 3 && this.MenuSkill.Skydome.Allocation != null)
                    ctx.CX = this.MenuSkill.Skydome.Allocation.Address;
            }, null);

            // Make sure we always open at correct skill index.
            this.WriteHook(51636, 0xD94 - 0xB80, "89 87 C0 01 00 00", 6, 6, ctx =>
            {
                if (this.IsOurMenuMode)
                    ctx.AX = IntPtr.Zero; // enchanting
            }, null);

            // Max skill trees is 2, just here for compatibility with the nif, probably it's not necessary.
            this.WriteHook(51636, 0xD34 - 0xB80, "C7 87 10 03 00 00 12 00 00 00", 10, 10, null, ctx =>
            {
                if (this.IsOurMenuMode)
                    NetScriptFramework.Memory.WriteInt32(ctx.DI + 0x310, 2);
            });

            // Only send array of 1 skill to the swf.
            this.WriteHook(51652, 0x296A - 0x22B0, "C7 44 24 28 5A 00 00 00", 8, 8, null, ctx =>
            {
                if (this.IsOurMenuMode)
                    NetScriptFramework.Memory.WriteInt32(ctx.SP + 0x28, 5);
            });
            
            Action<ulong, int> replaceCompCheck = (vid, offset) =>
            {
                this.WriteHook(vid, offset, "80 3D ?? ?? ?? ?? 00", 0, 7, ctx =>
                {
                    long flags = ctx.FLAGS.ToInt64();
                    if (this.IsOurMenuMode)
                        flags &= ~((long)0x40);
                    else
                    {
                        if (NetScriptFramework.Memory.ReadUInt8(this.addr_IsBeastMode) == 0)
                            flags |= 0x40;
                        else
                            flags &= ~((long)0x40);
                    }
                    ctx.FLAGS = new IntPtr(flags);
                }, null);
            };

            replaceCompCheck(51659, 0x4FA - 0x3A0); // Don't allow left right tree switching?
            replaceCompCheck(51637, 0xD9 - 0xA0); // Don't remember last selected tree.
            replaceCompCheck(51661, 0x96 - 0x50); // No horizontal velocity?
            replaceCompCheck(51669, 0xD9A - 0xB10); // More changing of tree possibly.

            // Unknown, possibly changing selected tree.
            this.WriteHook(51666, 0x64, "44 38 3D ?? ?? ?? ?? 0F 85", 0, 13, ctx =>
            {
                if (this.IsOurMenuMode || this.IsBeastMode)
                    ctx.IP = ctx.IP + (0x7E0 - 0x371);
            }, null);

            // Don't update data of other trees.
            this.WriteHook(51652, 0x340 - 0x2B0, "80 3D ?? ?? ?? ?? 00 0F 85 8F 03 00 00", 0, 13, ctx =>
            {
                if ((this.IsOurMenuMode && NetScriptFramework._IntPtrExtensions.ToInt32Safe(ctx.R14) > 2) || this.IsBeastMode)
                    ctx.IP = ctx.IP + (0x6DC - 0x34D);
            }, null);

            // This will only create 1 perk tree lines and nodes.
            this.WriteHook(51667, 0xD6 - 0x40, "4C 8B B8 18 01 00 00", 7, 7, null, ctx =>
            {
                if (this.IsOurMenuMode)
                {
                    int avid = NetScriptFramework._IntPtrExtensions.ToInt32Safe(ctx.R14);
                    if (avid != (int)MenuAV)
                        ctx.R15 = IntPtr.Zero;
                }
            });

            // Get name of skill.
            this.WriteHook(51652, 0x49E - 0x2B0, "E8", 5, 5, null, ctx =>
            {
                if (this.IsOurMenuMode && this.MenuSkill.Name.Allocation != null)
                    ctx.AX = this.MenuSkill.Name.Allocation.Address;
            });

            // Get color of skill name.
            this.WriteHook(51652, 0x5A1 - 0x2B0, "E8", 5, 5, null, ctx =>
            {
                if (this.IsOurMenuMode)
                {
                    if (this.MenuSkill.UpdateColor())
                        ctx.AX = this.MenuSkill.ColorStr.Allocation.Address;
                    else if (_ColorOfSkillNormal != null && _ColorOfSkillNormal.Allocation != null)
                        ctx.AX = _ColorOfSkillNormal.Allocation.Address;
                }
            });

            // Name of skill in perk description.
            this.WriteHook(51654, 0x3E65 - 0x2D90, "FF 50 28 4C 8B C8", 6, 6, null, ctx =>
            {
                if (this.IsOurMenuMode && this.MenuSkill.Name.Allocation != null)
                    ctx.R9 = this.MenuSkill.Name.Allocation.Address;
            });

            // Get description of skill.
            this.WriteHook(51654, 0x4235 - 0x2D90, "4D 8B D5 4C 89 6C 24 30", 8, 8, ctx =>
            {
                if (this.IsOurMenuMode && this.MenuSkill.Description.Allocation != null)
                    ctx.R9 = this.MenuSkill.Description.Allocation.Address;
            }, null);

            // Get current level for legendary reset check.
            this.WriteHook(51638, 0xA1E - 0x550, "48 8D 8F B0 00 00 00 FF 53 18", 10, 10, null, ctx =>
            {
                if (this.IsOurMenuMode && this.MenuSkill.Level != null)
                    ctx.XMM0f = this.MenuSkill.Level.Value;
            });

            // One additional legendary skill check.
            this.WriteHook(51714, 0xB070 - 0xAE40, "8B 56 1C FF 50 18", 6, 6, null, ctx =>
            {
                if (this.IsOurMenuMode && this.MenuSkill.Level != null)
                    ctx.XMM0f = this.MenuSkill.Level.Value;
            });

            // Don't allow resetting skill value (we do this on our own).
            this.WriteHook(51714, 0xB09C - 0xAE40, "8B 56 1C FF 50 20", 6, 6, ctx =>
            {
                if (this.IsOurMenuMode)
                {
                    ctx.Skip();
                    if (this.MenuSkill.Level != null)
                        this.MenuSkill.Level.Value = (short)Math.Round(ctx.XMM2f);
                    if (this.MenuSkill.Ratio != null)
                        this.MenuSkill.Ratio.Value = 0.0f;
                    if (this.MenuSkill.Legendary != null)
                        this.MenuSkill.Legendary.Value++;
                }
            }, null);

            // Reset in player skills struct.
            this.WriteHook(51714, 0xB0B3 - 0xAE40, "E8", 5, 5, ctx =>
            {
                if (this.IsOurMenuMode)
                    ctx.Skip();
            }, null);

            // Overwrite allowing legendary reset to work.
            this.WriteHook(15642, 0, "83 3D", 0, 0xA, ctx =>
            {
                byte now = NetScriptFramework.Memory.ReadUInt8(this.addr_CurrentDifficulty + 8);
                if (now >= 5)
                {
                    if (this.IsOurMenuMode && this.MenuSkill.Legendary == null)
                        ctx.AX = new IntPtr(0);
                    else
                        ctx.AX = new IntPtr(1);
                }
                else
                    ctx.AX = new IntPtr(0);
            }, null);

            // Make sure if we legendary and use custom perk points it gets refunded in the custom global variable instead.
            this.WriteHook(51716, 0x212 - 0x170, "40 00 B0 01 0B 00 00", 0, 7, ctx =>
            {
                byte add = NetScriptFramework._IntPtrExtensions.ToUInt8(ctx.SI);
                int prev = this.CurrentPerkPoints;
                int cur = prev + add;
                if (cur > 255)
                    cur = 255;
                if (cur > prev)
                    this.CurrentPerkPoints = (byte)cur;
            }, null);

            // Get current perk points.
            this.WriteHook(51664, 0x226 - 0x110, "48 8B 5C 24 70", 5, 5, ctx =>
            {
                if (this.IsOurMenuMode)
                    ctx.AX = new IntPtr((int)this.CurrentPerkPoints);
            }, null);

            // Spend or add perk points.
            this.WriteHook(51665, 0, "40 53 48 83 EC 20", 6, 6, ctx =>
            {
                if (this.IsOurMenuMode)
                {
                    int changed = NetScriptFramework._IntPtrExtensions.ToInt32Safe(ctx.DX);
                    ctx.DX = IntPtr.Zero;

                    if (changed > 0)
                    {
                        // Level up has different logic.
                        var plr = NetScriptFramework.SkyrimSE.PlayerCharacter.Instance;
                        if (plr != null)
                        {
                            int oldAmt = NetScriptFramework.Memory.ReadUInt8(plr.Cast<NetScriptFramework.SkyrimSE.PlayerCharacter>() + 0xB01);
                            int newAmt = unchecked(oldAmt + changed);
                            if (newAmt > oldAmt && oldAmt != 255)
                                NetScriptFramework.Memory.WriteUInt8(plr.Cast<NetScriptFramework.SkyrimSE.PlayerCharacter>() + 0xB01, (byte)Math.Min(255, newAmt));
                        }
                        return;
                    }

                    long prev = this.CurrentPerkPoints;
                    long cur = prev + changed;

                    if (cur < 0)
                        cur = 0;
                    else if (cur > 255)
                        cur = 255;
                    this.CurrentPerkPoints = (byte)cur;
                    return;
                }
            }, null);

            // Replace current skill level and xp.
            this.WriteHook(40552, 0, "4C 8B 11 8D 42 FA", 6, 6, ctx =>
            {
                if (this.IsOurMenuMode)
                {
                    var a3 = ctx.R8;
                    if (a3 != IntPtr.Zero)
                    {
                        if (this.MenuSkill.Level != null)
                            NetScriptFramework.Memory.WriteFloat(a3, this.MenuSkill.Level.Value);
                        else
                            NetScriptFramework.Memory.WriteFloat(a3, 1.0f);
                    }
                    var a4 = ctx.R9;
                    if (a4 != IntPtr.Zero)
                    {
                        if (this.MenuSkill.Ratio != null)
                            NetScriptFramework.Memory.WriteFloat(a4, Math.Max(0.0f, Math.Min(1.0f, this.MenuSkill.Ratio.Value)));
                        else
                            NetScriptFramework.Memory.WriteFloat(a4, 0.0f);
                    }
                    var a5 = NetScriptFramework.Memory.ReadPointer(ctx.SP + 0x28);
                    if (a5 != IntPtr.Zero)
                        NetScriptFramework.Memory.WriteFloat(a5, 1.0f);
                    var a6 = NetScriptFramework.Memory.ReadPointer(ctx.SP + 0x30);
                    if (a6 != IntPtr.Zero)
                    {
                        if (this.MenuSkill.Legendary != null)
                            NetScriptFramework.Memory.WriteInt32(a6, this.MenuSkill.Legendary.Value);
                        else
                            NetScriptFramework.Memory.WriteInt32(a6, 0);
                    }

                    ctx.Skip();
                    ctx.IP = ctx.IP + 0x3C;
                }
            }, null);

            // Replace current skill level.
            this.WriteHook(51652, 0x3C9 - 0x2B0, "8B D6 FF 50 08", 5, 5, null, ctx =>
            {
                if (this.IsOurMenuMode && this.MenuSkill.Level != null)
                    ctx.XMM0f = this.MenuSkill.Level.Value;
            });

            // Replace current skill level.
            this.WriteHook(51654, 0xEE7 - 0xD90, "8B D6 FF 50 18", 5, 5, null, ctx =>
            {
                if (this.IsOurMenuMode && this.MenuSkill.Level != null)
                    ctx.XMM0f = this.MenuSkill.Level.Value;
            });

            // Replace current skill level.
            this.WriteHook(51654, 0x43C2 - 0x2D90, "8B D6 FF 50 08", 5, 5, null, ctx =>
            {
                if (this.IsOurMenuMode && this.MenuSkill.Level != null)
                    ctx.XMM0f = this.MenuSkill.Level.Value;
            });

            // Replace current skill level.
            this.WriteHook(51654, 0x3E4D - 0x2D90, "8B 95 70 05 00 00 FF 50 18", 9, 9, null, ctx =>
            {
                if (this.IsOurMenuMode && this.MenuSkill.Level != null)
                    ctx.XMM0f = this.MenuSkill.Level.Value;
            });

            // Replace current skill level.
            /*this.WriteHook(51647, 0x10D7 - 0xF80, "48 8D 8F B0 00 00 00 FF 53 18", 10, 10, null, ctx =>
            {
                if (this.IsOurMenuMode && this.MenuSkill.Level != null)
                    ctx.XMM0f = this.MenuSkill.Level.Value;
            });*/
            // This has to be hooked in a special way due to uncapper.
            this.WriteHook(51647, 0x10D5 - 0xF80, "8B D0 48 8D 8F B0 00 00 00 FF 53 18 0F 2F 05 ?? ?? ?? ?? 0F 82 10 0A 00 00", 0, 25, ctx =>
            {
                float skillValue = 0.0f;
                if (this.IsOurMenuMode)
                {
                    if (this.MenuSkill.Level != null && this.MenuSkill.Legendary != null)
                        skillValue = this.MenuSkill.Level.Value;
                }
                else
                {
                    var plr = NetScriptFramework.MemoryObject.FromAddress<NetScriptFramework.SkyrimSE.PlayerCharacter>(ctx.DI);
                    if (plr != null)
                        skillValue = plr.GetBaseActorValue((NetScriptFramework.SkyrimSE.ActorValueIndices)NetScriptFramework._IntPtrExtensions.ToUInt8(ctx.AX));
                }

                if (skillValue < 100.0f)
                    ctx.IP = ctx.IP + 0xA10;
            }, null, "uncapper_compatibility");

            // Make sure we can't select other tree.
            this.WriteHook(51647, 0x14F3 - 0xF80, "0F B6 05 ?? ?? ?? ??", 0, 7, null, ctx =>
            {
                if (this.IsOurMenuMode)
                    ctx.AX = new IntPtr(1);
                else
                    ctx.AX = new IntPtr(this.IsBeastMode ? 1 : 0);
            });

            // Something to do with background nif. This selects where camera should look based on the PointXX in the nif.
            // Adding a compatibility option here in case someone wants to copy from normal nif instead of werewolf nif.
            this.WriteHook(51657, 0x988 - 0x720, "80 3D ?? ?? ?? ?? 00", 0, 7, ctx =>
            {
                long flags = ctx.FLAGS.ToInt64();
                if (this.IsOurMenuMode && !this.MenuSkill.NormalNif)
                    flags &= ~((long)0x40);
                else
                {
                    if (NetScriptFramework.Memory.ReadUInt8(this.addr_IsBeastMode) == 0)
                        flags |= 0x40;
                    else
                        flags &= ~((long)0x40);
                }
                ctx.FLAGS = new IntPtr(flags);
            }, null);

            // Perk conditions fix. This fixes the "REQUIRES X" below a perk is not shown unless it uses specifically GetBaseActorValue condition but we want to use GetGlobalValue condition instead.
            this.WriteHook(23356, 0, "48 8B C4 53 56", 0, 5, ctx =>
            {
                ctx.Skip();
                ctx.IP = ctx.IP + (0xB85 - 0xA95);

                var perk = NetScriptFramework.MemoryObject.FromAddress<NetScriptFramework.SkyrimSE.BGSPerk>(ctx.CX);
                var buf = ctx.DX;

                NetScriptFramework.Memory.WriteUInt8(buf, 0);

                NetScriptFramework.SkyrimSE.TESCondition.ConditionNode node;
                if (perk != null && (node = perk.PerkConditions.First) != null)
                {
                    int bufLen = NetScriptFramework._IntPtrExtensions.ToInt32Safe(ctx.R8);
                    string prefix = NetScriptFramework.Memory.ReadString(ctx.R9, false);
                    var builder = new StringBuilder();
                    int wrote = 0;

                    var req_ptr = NetScriptFramework.Memory.ReadPointer(this.addr_RequirementBegin + 8);
                    if (req_ptr != IntPtr.Zero)
                    {
                        string req_str = NetScriptFramework.Memory.ReadString(req_ptr, false);
                        if (!string.IsNullOrEmpty(req_str))
                            builder.Append(req_str);
                    }

                    int maxTry = 0;
                    while (node != null && maxTry++ < 100)
                    {
                        if (node.Function == NetScriptFramework.SkyrimSE.TESCondition.Functions.GetBaseActorValue)
                        {
                            var name_ptr = NetScriptFramework.Memory.InvokeCdecl(this.addr_GetActorValueName, node.Param1);
                            if (name_ptr == IntPtr.Zero)
                                continue;

                            if (wrote > 0)
                                builder.Append(',');
                            builder.Append(' ');
                            builder.Append(prefix);

                            float comp_value = NetScriptFramework.Memory.InvokeCdeclF(this.addr_GetComparisonValue, node.Cast<NetScriptFramework.SkyrimSE.TESCondition.ConditionNode>());
                            int i_value = (int)Math.Round(comp_value);
                            builder.Append(i_value.ToString());

                            wrote++;
                        }
                        else if (node.Function == NetScriptFramework.SkyrimSE.TESCondition.Functions.GetGlobalValue)
                        {
                            var gb_ptr = node.Param1;
                            if (gb_ptr != IntPtr.Zero)
                            {
                                var sk = this.FindSkillFromGlobalLevel(gb_ptr);
                                if (sk != null)
                                {
                                    if (wrote > 0)
                                        builder.Append(',');
                                    builder.Append(' ');
                                    builder.Append(prefix);

                                    float comp_value = NetScriptFramework.Memory.InvokeCdeclF(this.addr_GetComparisonValue, node.Cast<NetScriptFramework.SkyrimSE.TESCondition.ConditionNode>());
                                    int i_value = (int)Math.Round(comp_value);
                                    builder.Append(i_value.ToString());

                                    wrote++;
                                }
                            }
                        }

                        node = node.Next;
                    }

                    if (builder.Length != 0)
                    {
                        while (true)
                        {
                            byte[] tbuf = Encoding.UTF8.GetBytes(builder.ToString());
                            if (tbuf.Length + 1 > bufLen)
                            {
                                if (builder.Length > 8)
                                {
                                    builder.Remove(builder.Length - 8, 8);
                                    builder.Append("...");
                                    continue;
                                }

                                break;
                            }

                            NetScriptFramework.Memory.WriteBytes(buf, tbuf);
                            NetScriptFramework.Memory.WriteUInt8(buf + tbuf.Length, 0);
                            break;
                        }
                    }
                }
            }, null);
        }

        private void WriteHook(ulong vid, int offset, string pattern, int includeLen, int replaceLen, Action<NetScriptFramework.CPURegisters> before, Action<NetScriptFramework.CPURegisters> after, string resolveCompatibility = null)
        {
            var addr = NetScriptFramework.Main.GameInfo.GetAddressOf(vid, offset, 0, pattern);

            var data = new hook_data();
            this._hooks.Add(data);
            data.Address = addr;
            data.Length = replaceLen;
            data.Original = NetScriptFramework.Memory.ReadBytes(addr, replaceLen);
            data.Name = vid + "+" + offset.ToString("X");
            data.Compatibility = resolveCompatibility;

            NetScriptFramework.Memory.WriteHook(new NetScriptFramework.HookParameters()
            {
                Address = addr,
                IncludeLength = includeLen,
                ReplaceLength = replaceLen,
                Before = before,
                After = after
            });

            data.Hooked = NetScriptFramework.Memory.ReadBytes(addr, replaceLen);

            NetScriptFramework.Memory.WriteBytes(addr, data.Original, true);
        }

        private readonly List<hook_data> _hooks = new List<hook_data>();

        private void ApplyHooks()
        {
            List<string> incomp = null;
            foreach(var h in this._hooks)
            {
                if (NetScriptFramework.Memory.VerifyBytes(h.Address, string.Join(" ", h.Original.Select(q => q.ToString("X2")))))
                    continue;

                if(h.Compatibility != null)
                {
                    if(h.Compatibility == "uncapper_compatibility")
                    {
                        if(NetScriptFramework.Memory.VerifyBytes(h.Address, "FF 25 ?? ?? ?? ?? 00 00 00 FF 53 18 0F 2F 05 ?? ?? ?? ?? 0F 82 10 0A 00 00"))
                            continue;
                    }
                }

                if (incomp == null)
                    incomp = new List<string>();
                incomp.Add(h.Name);
            }

            if(incomp != null)
            {
                this.ShowMessageBox("CustomSkills.dll plugin failed to load due to incompability with an SKSE plugin:\n" + string.Join("\n", incomp));
                return;
            }

            foreach(var h in this._hooks)
                NetScriptFramework.Memory.WriteBytes(h.Address, h.Hooked, true);
        }

        private void ShowMessageBox(string text)
        {
            byte[] buf = Encoding.UTF8.GetBytes(text);
            using (var alloc = NetScriptFramework.Memory.Allocate(buf.Length + 0x20))
            {
                NetScriptFramework.Memory.WritePointer(alloc.Address, alloc.Address + 0x10);
                NetScriptFramework.Memory.WriteBytes(alloc.Address + 0x10, buf);
                NetScriptFramework.Memory.WriteUInt8(alloc.Address + 0x10 + buf.Length, 0);

                NetScriptFramework.Memory.InvokeCdecl(this.addr_ShowMessageBox, 0, 0, 0, alloc.Address);
            }
        }

        private sealed class hook_data
        {
            internal string Name;
            internal IntPtr Address;
            internal int Length;
            internal byte[] Original;
            internal byte[] Hooked;
            internal string Compatibility;
        }
    }
}
