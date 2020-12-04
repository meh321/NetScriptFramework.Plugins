using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework;
using NetScriptFramework.SkyrimSE;

namespace GrassControl
{
    public sealed class GrassControlPlugin : Plugin
    {
        public override string Key
        {
            get
            {
                return "grasscontrol";
            }
        }

        public override string Name
        {
            get
            {
                return "Grass Control";
            }
        }

        public override string Author
        {
            get
            {
                return "meh321";
            }
        }

        public override int Version
        {
            get
            {
                return 5;
            }
        }

        internal Settings Settings
        {
            get;
            private set;
        }
        
        internal RaycastHelper Cache
        {
            get;
            private set;
        }

        private static Profiler profiler;
        
        private static IntPtr addr_MaxGrassPerTexture;

        internal static Settings _settingsInstance;

        private static int _did_mainMenu = 0;

        protected override bool Initialize(bool loadedAny)
        {
            this.init();

            return true;
        }
        
        private void init()
        {
            this.Settings = new Settings();
            _settingsInstance = this.Settings;
            this.Settings.RayCastCollisionLayers = string.Join(" ", new CollisionLayers[]
            {
                //CollisionLayers.Unidentified,
                CollisionLayers.Static, // most world objects like house, stairs, crates
                CollisionLayers.AnimStatic,
                //CollisionLayers.Transparent, // very thin static? projectiles do not collide
                //CollisionLayers.Clutter, // misc objects like brooms, pots
                //CollisionLayers.Trees, // all flora! not only trees but bushes and plants too
                //CollisionLayers.Props, // signs, chains, flags
                //CollisionLayers.Water,
                CollisionLayers.Terrain, // terrain object like rock, boulder and mountain
                //CollisionLayers.Ground, // actual terrain height map, we do NOT want this
                CollisionLayers.DebrisLarge,
                //CollisionLayers.TransparentSmall,
                //CollisionLayers.TransparentSmallAnim,
                //CollisionLayers.InvisibleWall,
                CollisionLayers.StairHelper,
            }.Select(q => ((int)q).ToString()));
            this.Settings.Load();
            
            if (this.Settings.UseGrassCache && new System.IO.FileInfo(GidFileGenerationTask.ProgressFilePath).Exists)
            {
                GidFileGenerationTask.apply();

                Events.OnMainMenu.Register(e =>
                {
                    _did_mainMenu = 1;
                }, 5000, 1);

                Events.OnFrame.Register(e =>
                {
                    if (_did_mainMenu == 0)
                        return;

                    if (_did_mainMenu == 1)
                        _did_mainMenu++;
                    else if(_did_mainMenu == 2)
                    {
                        _did_mainMenu++;

                        GidFileGenerationTask.cur_state = 1;

                        GidFileGenerationTask._lastDidSomething = Environment.TickCount;
                        var t = new System.Threading.Thread(GidFileGenerationTask.run_freeze_check);
                        t.Start();

                        var gf = new GidFileGenerationTask();
                    }
                });

                this.Settings.ExtendGrassDistance = false;
            }

            if (this.Settings.ProfilerReport)
            {
                profiler = new Profiler();

                // Track when console is opened.
                {
                    var addr = NetScriptFramework.Main.GameInfo.GetAddressOf(50177, 0, 0, "40 53 48 83 EC 20");
                    Memory.WriteHook(new HookParameters()
                    {
                        Address = addr,
                        IncludeLength = 6,
                        ReplaceLength = 6,
                        Before = ctx =>
                        {
                            var p = profiler;
                            if (p != null)
                                p.Report();
                        }
                    });
                }

                // Track the time taken in create grass function.
                {
                    // 4D10
                    var addr = NetScriptFramework.Main.GameInfo.GetAddressOf(15204, 0, 0, "48 8B C4 4C 89 40 18");
                    Memory.WriteHook(new HookParameters()
                    {
                        Address = addr,
                        IncludeLength = 7,
                        ReplaceLength = 7,
                        Before = ctx =>
                        {
                            var p = profiler;
                            if (p != null)
                                p.Begin();
                        }
                    });

                    // 5923
                    addr = NetScriptFramework.Main.GameInfo.GetAddressOf(15204, 0x5923 - 0x4D10, 0, "48 81 C4 F0 08 00 00");
                    Memory.WriteHook(new HookParameters()
                    {
                        Address = addr,
                        IncludeLength = 7,
                        ReplaceLength = 7,
                        Before = ctx =>
                        {
                            var p = profiler;
                            if (p != null)
                                p.End();
                        }
                    });
                }
            }

            if (this.Settings.RayCast)
            {
                Events.OnMainMenu.Register(e =>
                {
                    string formsStr = this.Settings.RayCastIgnoreForms;
                    var cachedList = CachedFormList.TryParse(formsStr, "GrassControl", "RayCastIgnoreForms", false, true);
                    if (cachedList != null && cachedList.All.Count == 0)
                        cachedList = null;

                    this.Cache = new RaycastHelper(this.Version, this.Settings.RayCastHeight, this.Settings.RayCastDepth, this.Settings.RayCastCollisionLayers, cachedList);
                }, 0, 1);

                var addr = NetScriptFramework.Main.GameInfo.GetAddressOf(15212, 0x723A - 0x6CE0, 0, "F3 0F 10 75 B8");

                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 5,
                    ReplaceLength = 5,
                    Before = ctx =>
                    {
                        var land = MemoryObject.FromAddress<TESObjectLAND>(ctx.SI);
                        float x = Memory.ReadFloat(ctx.SP + 0x40);
                        float y = Memory.ReadFloat(ctx.SP + 0x44);
                        float z = ctx.XMM7f;

                        if (land != null)
                        {
                            var cache = this.Cache;
                            if (cache != null && !cache.CanPlaceGrass(land.ParentCell, land, x, y, z))
                            {
                                ctx.Skip();
                                ctx.IP = ctx.IP + (0x661 - 0x23F);
                            }
                        }
                    },
                });
            }

            if (this.Settings.SuperDenseGrass)
            {
                // Make amount big.
                {
                    var addr = NetScriptFramework.Main.GameInfo.GetAddressOf(15202, 0xAE5 - 0x890, 0, "C1 E1 07");
                    int mode = Math.Max(0, Math.Min(12, this.Settings.SuperDenseMode));
                    if (mode != 7)
                        Memory.WriteUInt8(addr + 2, (byte)mode, true);
                }
            }

            if(this.Settings.ExtendGrassCount)
            {
                // Create more grass shapes if one becomes full.
                {
                    var addr = NetScriptFramework.Main.GameInfo.GetAddressOf(15220, 0x433 - 0x3C0, 0, "0F 84");
                    Memory.WriteNop(addr, 6);

                    addr = NetScriptFramework.Main.GameInfo.GetAddressOf(15214, 0x960 - 0x830, 0, "48 39 18 74 0A");
                    Memory.WriteHook(new HookParameters()
                    {
                        Address = addr,
                        IncludeLength = 0,
                        ReplaceLength = 5,
                        Before = ctx =>
                        {
                            if (Memory.ReadPointer(ctx.AX) == ctx.BX)
                            {
                                var shapePtr = Memory.ReadPointer(ctx.AX + 8);
                                if (shapePtr != IntPtr.Zero)
                                {
                                    shapePtr = Memory.ReadPointer(shapePtr);
                                    if (shapePtr != IntPtr.Zero)
                                    {
                                        int hasSize = Memory.ReadInt32(shapePtr + 0x190) * Memory.ReadInt32(shapePtr + 0x194) * 2;
                                        if (hasSize < 0x20000)
                                            ctx.IP = ctx.IP + (0xF - 5);
                                    }
                                }
                            }
                        },
                    });
                }

                // Fix bug related to coordinates which causes game to want to put all grass into 1 or 2 shapes.
                // This can't work, there's more places in game where game expects it to be * 12 :(
                /*{
                    // 1401B4D10
                    // 1401B4E0D
                    // 1401B4E3C
                    var addr = NetScriptFramework.Main.GameInfo.GetAddressOf(15204, 0xE0D - 0xD10, 0, "B8 AB AA AA 2A");
                    Memory.WriteHook(new HookParameters()
                    {
                        Address = addr,
                        IncludeLength = 0,
                        ReplaceLength = (0x3C - 0xD),
                        Before = ctx =>
                        {
                            Memory.WriteInt32(ctx.SP + (0x928 - 0x8B8), Memory.ReadInt32(ctx.SP + (0x928 - 0x8E8)));
                            Memory.WriteInt32(ctx.BP - 0x60, Memory.ReadInt32(ctx.SP + (0x928 - 0x8EC)));
                        },
                    });

                    // 1401B5940
                    // 1401B59C8
                    // 1401B59FD
                    addr = NetScriptFramework.Main.GameInfo.GetAddressOf(15205, 0xC8 - 0x40, 0, "B8 AB AA AA 2A");
                    Memory.WriteHook(new HookParameters()
                    {
                        Address = addr,
                        IncludeLength = 0,
                        ReplaceLength = (0xFD - 0xC8),
                        Before = ctx =>
                        {
                            int what = Memory.ReadInt32(ctx.SP + (0x238 - 0x200));
                            Memory.WriteInt32(ctx.SP + (0x238 - 0x1E4), what);
                            ctx.R15 = new IntPtr(what);

                            Memory.WriteInt32(ctx.SP + (0x238 - 0x1E8), Memory.ReadInt32(ctx.SP + (0x238 - 0x1FC)));
                        },
                    });

                    // 1401B6200
                    // 1401B6252
                    // 1401B6282
                    addr = NetScriptFramework.Main.GameInfo.GetAddressOf(15206, 0x52, 0, "B8 AB AA AA 2A");
                    Memory.WriteHook(new HookParameters()
                    {
                        Address = addr,
                        IncludeLength = 0,
                        ReplaceLength = (0x82 - 0x52),
                        Before = ctx =>
                        {
                            int what = Memory.ReadInt32(ctx.SP + (0x318 - 0x2E4));
                            Memory.WriteInt32(ctx.SP + (0x318 - 0x2DC), what);
                            ctx.BX = new IntPtr(what);

                            what = Memory.ReadInt32(ctx.SP + (0x318 - 0x2E8));
                            Memory.WriteInt32(ctx.SP + (0x318 - 0x2D4), what);
                            ctx.SI = new IntPtr(what);
                        },
                    });

                    // Fixup coordinates when it's making the shape later.
                    // 1401B7830
                    // 1401B7F04
                    // 1401B7F17
                    addr = NetScriptFramework.Main.GameInfo.GetAddressOf(15214, 0xF04 - 0x830, 0, "8D 04 40 C1 E0 0E");
                    Memory.WriteHook(new HookParameters()
                    {
                        Address = addr,
                        IncludeLength = 0,
                        ReplaceLength = 0x11 - 4,
                        Before = ctx =>
                        {
                            ctx.XMM1f = (float)((double)ctx.AX.ToInt32Safe() * 4096.0);
                        },
                    });
                    addr = NetScriptFramework.Main.GameInfo.GetAddressOf(15214, 0xF17 - 0x830, 0, "8D 04 40 C1 E0 0E");
                    Memory.WriteHook(new HookParameters()
                    {
                        Address = addr,
                        IncludeLength = 0,
                        ReplaceLength = 0x24 - 0x17,
                        Before = ctx =>
                        {
                            ctx.XMM0f = (float)((double)ctx.AX.ToInt32Safe() * 4096.0);
                        },
                    });
                }*/
            }

            if(this.Settings.UseGrassCache)
                GidFileCache.FixFileFormat();

            if (this.Settings.ExtendGrassDistance)
                DistantGrass.ReplaceGrassGrid(this.Settings.WriteDebugMessages, this.Settings.OnlyLoadFromCache);

            if(this.Settings.EnsureMaxGrassTypesPerTextureSetting > 0)
            {
                addr_MaxGrassPerTexture = NetScriptFramework.Main.GameInfo.GetAddressOf(501615);

                var addr = NetScriptFramework.Main.GameInfo.GetAddressOf(18342, 0xD63 - 0xCF0, 0, "44 8B 25");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 7,
                    Before = ctx =>
                    {
                        int max = Math.Max(this.Settings.EnsureMaxGrassTypesPerTextureSetting, Memory.ReadInt32(addr_MaxGrassPerTexture + 8));
                        ctx.R12 = new IntPtr(max);
                    },
                });
            }

            if(this.Settings.OverwriteGrassDistance >= 0.0f)
            {
                var addr = NetScriptFramework.Main.GameInfo.GetAddressOf(528751, 0, 0, "F3 0F 10 05");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 8,
                    Before = ctx =>
                    {
                        ctx.XMM0f = this.Settings.OverwriteGrassDistance;
                    }
                });

                addr = NetScriptFramework.Main.GameInfo.GetAddressOf(528751, 0xC10 - 0xBE0, 0, "F3 0F 10 15");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 8,
                    Before = ctx =>
                    {
                        ctx.XMM2f = this.Settings.OverwriteGrassDistance;
                    }
                });

                addr = NetScriptFramework.Main.GameInfo.GetAddressOf(15210, 0xBD - 0xA0, 0, "F3 0F 10 05");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 8,
                    Before = ctx =>
                    {
                        ctx.XMM0f = this.Settings.OverwriteGrassDistance;
                    },
                });

                addr = NetScriptFramework.Main.GameInfo.GetAddressOf(15202, 0x4B1B - 0x4890, 0, "F3 0F 10 15");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 8,
                    Before = ctx =>
                    {
                        ctx.XMM2f = this.Settings.OverwriteGrassDistance;
                    }
                });

                addr = NetScriptFramework.Main.GameInfo.GetAddressOf(15202, 0x4AF3 - 0x4890, 0, "F3 0F 58 05");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 8,
                    Before = ctx =>
                    {
                        ctx.XMM0f += this.Settings.OverwriteGrassDistance;
                    }
                });

                addr = NetScriptFramework.Main.GameInfo.GetAddressOf(15202, 0x49F7 - 0x4890, 0, "F3 0F 10 05");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 8,
                    Before = ctx =>
                    {
                        ctx.XMM0f = this.Settings.OverwriteGrassDistance;
                    }
                });
            }

            if(this.Settings.OverwriteGrassFadeRange >= 0.0f)
            {
                var addr = NetScriptFramework.Main.GameInfo.GetAddressOf(15202, 0xAEB - 0x890, 0, "F3 0F 10 05");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 8,
                    Before = ctx =>
                    {
                        ctx.XMM0f = this.Settings.OverwriteGrassFadeRange;
                    }
                });

                addr = NetScriptFramework.Main.GameInfo.GetAddressOf(528751, 0xB, 0, "F3 0F 58 05");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 8,
                    Before = ctx =>
                    {
                        ctx.XMM0f += this.Settings.OverwriteGrassFadeRange;
                    }
                });
            }

            if(this.Settings.OverwriteMinGrassSize >= 0)
            {
                var addr = NetScriptFramework.Main.GameInfo.GetAddressOf(15202, 0x4B4E - 0x4890, 0, "66 0F 6E 05");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 8 + 3,
                    Before = ctx =>
                    {
                        ctx.XMM0f = Math.Max(1, this.Settings.OverwriteMinGrassSize);
                    }
                });

                addr = NetScriptFramework.Main.GameInfo.GetAddressOf(15212, 0x6DBB - 0x6CE0, 0, "66 0F 6E 0D");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 8 + 3,
                    Before = ctx =>
                    {
                        ctx.XMM1f = Math.Max(1, this.Settings.OverwriteMinGrassSize);
                    }
                });
            }

            if(this.Settings.ExtendGrassDistance)
            {
                if(!this.Settings.UseGrassCache || !this.Settings.OnlyLoadFromCache)
                {
                    NetScriptFramework.SkyrimSE.Events.OnMainMenu.Register(e =>
                    {
                        warn_extend_without_cache();
                    }, 0, 1);
                }
            }
        }

        private void warn_extend_without_cache()
        {
            var ls = new List<string>();
            ls.Add("Warning!! You have enabled ExtendGrassDistance without using pre-generated grass. This could lead to unstable game. Either disable ExtendGrassDistance or pre-generate grass cache files. In order to use pre-generated grass cache you will need UseGrassCache=True and OnlyLoadFromCache=True");
            ls.Add("Check nexus page of 'No Grass In Objects' mod for more information on how to do this.");
            //ls.Add("This warning won't be shown again next time you start game.");

            try
            {
                var fi = new System.IO.FileInfo("Data/NetScriptFramework/Plugins/GrassControl.warned.txt");
                if (fi.Exists)
                    return;

                using (var sw = fi.CreateText())
                {
                    sw.WriteLine("Dummy file to track whether the following warning was shown:");
                    sw.WriteLine();
                    sw.Write(string.Join(Environment.NewLine + Environment.NewLine, ls));
                }
            }
            catch
            {
                
            }

            GidFileGenerationTask.ShowMessageBox(string.Join("\n\n", ls));
        }
    }
}
