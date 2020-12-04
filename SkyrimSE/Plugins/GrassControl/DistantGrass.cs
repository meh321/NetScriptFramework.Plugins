using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework;

namespace GrassControl
{
    internal static class DistantGrass
    {
        //private static bool _debug_g = false;

        internal static void ReplaceGrassGrid(bool dbgMsg, bool _loadOnly)
        {
            DidApply = true;
            debug_msg = dbgMsg;
            load_only = _loadOnly;

            if (load_only)
                LOMap = new LoadOnlyCellInfoContainer();
            else
                Map = new CellInfoContainer();

            addr_GetCurrentWorldspaceCell = Main.GameInfo.GetAddressOf(13174);
            addr_RemoveGrassFromCell = Main.GameInfo.GetAddressOf(15207);
            addr_GrassManager = Main.GameInfo.GetAddressOf(514292);
            addr_uGrids = Main.GameInfo.GetAddressOf(501244);
            addr_AddGrassInCellDirect = Main.GameInfo.GetAddressOf(15204);
            addr_QueueAddGrassInCell = Main.GameInfo.GetAddressOf(13137);
            addr_GetCellInWorldspace = Main.GameInfo.GetAddressOf(13620);
            addr_DataHandler = Main.GameInfo.GetAddressOf(514141);
            addr_PrepareLandNode = Main.GameInfo.GetAddressOf(18331);
            addr_GetOrCreateLand = Main.GameInfo.GetAddressOf(18513);
            addr_WSGetCell = Main.GameInfo.GetAddressOf(20026);
            addr_DHGetCell = Main.GameInfo.GetAddressOf(13612);
            addr_TESLock = Main.GameInfo.GetAddressOf(13188);
            addr_TESUnlock = Main.GameInfo.GetAddressOf(13189);
            addr_GetCellByCoordMask = Main.GameInfo.GetAddressOf(13549);
            addr_AllowLoadFile = Main.GameInfo.GetAddressOf(501125);
            addr_ClearGrassHandles = Main.GameInfo.GetAddressOf(11931);
            //addr_LoadCellTempData = Main.GameInfo.GetAddressOf(18637);
            addr_EnsureLandData = Main.GameInfo.GetAddressOf(18331);
            addr_QueueLoadCellByCoordinates = Main.GameInfo.GetAddressOf(18150);
            addr_QueueLoadCellUnkGlobal = Main.GameInfo.GetAddressOf(514741);
            addr_GetAddGrassTask = Main.GameInfo.GetAddressOf(11933);
            addr_SetAddGrassTask = Main.GameInfo.GetAddressOf(11932);
            addr_LoadGrassFromGID = Main.GameInfo.GetAddressOf(15206);
            addr_GetOrCreateExtraData = Main.GameInfo.GetAddressOf(11930);
            
            // Disable grass fade.
            if(!load_only)
            {
                var addr = Main.GameInfo.GetAddressOf(38141);
                Memory.WriteUInt8(addr, 0xC3, true);

                // There's a chance this below isn't actually used so if it fails no problem.
                try
                {
                    addr = Main.GameInfo.GetAddressOf(13240, 0x55E - 0x480, 0, "E8");
                    Memory.WriteNop(addr, 5);
                }
                catch
                {

                }
            }

            // Auto use correct iGrassCellRadius.
            if(!load_only)
            {
                var addr = Main.GameInfo.GetAddressOf(15202, 0xA0E - 0x890, 0, "8B 05");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 6,
                    Before = ctx =>
                    {
                        ctx.AX = new IntPtr(ChosenGrassGridRadius);
                    }
                });
            }

            // Unload old grass, load new grass. But we have replaced how the grid works now.
            {
                var addr = Main.GameInfo.GetAddressOf(13148, 0xA06 - 0x220, 0, "8B 3D");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 6,
                    Before = ctx =>
                    {
                        ctx.IP = ctx.IP + (0xB5F - 0xA0C);
                        if (!_canUpdateGridNormal)
                            return;

                        int movedX = ctx.BP.ToInt32Safe();
                        int movedY = ctx.R14.ToInt32Safe();
                        UpdateGrassGridNow(ctx.BX, movedX, movedY, 0);
                    },
                });

                addr = Main.GameInfo.GetAddressOf(13190, 0, 0, "48 89 74 24 10");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 5,
                    Before = ctx =>
                    {
                        UpdateGrassGridNow(ctx.CX, 0, 0, 1);
                        _canUpdateGridNormal = true;
                    },
                });
                Memory.WriteUInt8(addr + 5, 0xC3, true);

                addr = Main.GameInfo.GetAddressOf(13191, 0, 0, "48 89 74 24 10");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 5,
                    Before = ctx =>
                    {
                        _canUpdateGridNormal = false;
                        UpdateGrassGridNow(ctx.CX, 0, 0, -1);
                    },
                });
                Memory.WriteUInt8(addr + 5, 0xC3, true);
                
                addr = Main.GameInfo.GetAddressOf(13138, 0, 0, "48 8B 51 38");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 8,
                    Before = ctx =>
                    {
                        if (load_only)
                            throw new InvalidOperationException("Trying to make grass with queueing!");

                        Call_AddGrassNow(Memory.ReadPointer(ctx.CX + 0x38), ctx.CX + 0x48);
                    },
                });
                Memory.WriteUInt8(addr + 8, 0xC3, true);

                // cell dtor
                if (!load_only)
                {
                    addr = Main.GameInfo.GetAddressOf(18446, 0xC4 - 0x50, 0, "48 85 C0");
                    Memory.WriteHook(new HookParameters()
                    {
                        Address = addr,
                        IncludeLength = 0,
                        ReplaceLength = (0xE9 - 0xC9),
                        Before = ctx =>
                        {
                            var cell = ctx.DI;
                            var grassMgr = Memory.ReadPointer(addr_GrassManager);
                            Handle_RemoveGrassFromCell_Call(grassMgr, cell, "celldtor");

                            if (ctx.AX != IntPtr.Zero)
                                Memory.InvokeCdecl(addr_ClearGrassHandles, cell + 0x48);
                        },
                    });
                }

                // unloading cell
                if (!load_only)
                {
                    addr = Main.GameInfo.GetAddressOf(13623, 0xC0F8 - 0xBF90, 0, "E8");
                    Memory.WriteHook(new HookParameters()
                    {
                        Address = addr,
                        IncludeLength = 0,
                        ReplaceLength = 5,
                        Before = ctx =>
                        {
                        // This is not necessary because we want to keep grass on cell unload.
                        //Handle_RemoveGrassFromCell_Call(ctx.CX, ctx.DX, "unload");
                    },
                    });
                }

                // remove just before readd grass so there's not so noticable fade-in / fade-out, there is still somewhat noticable effect though
                if (!load_only)
                {
                    addr = Main.GameInfo.GetAddressOf(15204, 0x8F - 0x10, 0, "E8");
                    Memory.WriteHook(new HookParameters()
                    {
                        Address = addr,
                        IncludeLength = 5,
                        ReplaceLength = 5,
                        After = ctx =>
                        {
                            var cell = ctx.SI;
                            if (cell != IntPtr.Zero)
                            {
                                var c = Map.FindByCell(cell);
                                if (c != null && (c.self_data & 0xFF) != 0)
                                {
                                    if (debug_msg)
                                        WriteDebugMsg("RemoveGrassDueToAdd", c.x, c.y);

                                    Memory.InvokeCdecl(addr_RemoveGrassFromCell, ctx.R13, ctx.SI);
                                }
                                else if (debug_msg)
                                {
                                    if (c == null)
                                        WriteDebugMsg("RemoveGrassDueToAdd", int.MinValue, 0, "warning: c == null");
                                }
                            }
                            else if (debug_msg)
                                WriteDebugMsg("RemoveGrassDueToAdd", int.MinValue, 0);
                        }
                    });
                }
            }

            // Allow grass to exist without cell.
            if(load_only)
            {
                NetScriptFramework.SkyrimSE.Events.OnMainMenu.Register(e =>
                {
                    DummyCell_Ptr = NetScriptFramework.SkyrimSE.MemoryManager.Allocate(320, 0);
                    Memory.WriteZero(DummyCell_Ptr, 320);

                    var exteriorData = NetScriptFramework.SkyrimSE.MemoryManager.Allocate(8, 0);
                    Memory.WriteZero(exteriorData, 8);

                    Memory.WritePointer(DummyCell_Ptr + 0x60, exteriorData);
                });
            }

            // Fix weird shape selection.
            // Vanilla game groups shape selection by 12 x 12 cells, we want a shape per cell.
            {
                var addr = Main.GameInfo.GetAddressOf(15204, 0x5005 - 0x4D10, 0, "E8");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 5,
                    ReplaceLength = 5,
                    Before = ctx =>
                    {
                        ctx.R8 = Memory.ReadPointer(ctx.SP + 0x40);
                        ctx.R9 = Memory.ReadPointer(ctx.SP + 0x3C);
                    }
                });

                addr = Main.GameInfo.GetAddressOf(15205, 0x5F6B - 0x5940, 0, "E8");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 5,
                    ReplaceLength = 5,
                    Before = ctx =>
                    {
                        ctx.R8 = Memory.ReadPointer(ctx.SP + 0x38);
                        ctx.R9 = Memory.ReadPointer(ctx.SP + 0x3C);
                    }
                });

                addr = Main.GameInfo.GetAddressOf(15206, 0x645C - 0x6200, 0, "E8");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 5,
                    ReplaceLength = 5,
                    Before = ctx =>
                    {
                        ctx.R8 = Memory.ReadPointer(ctx.SP + 0x34);
                        ctx.R9 = Memory.ReadPointer(ctx.SP + 0x30);
                    }
                });

                addr = Main.GameInfo.GetAddressOf(15214, 0x78B7 - 0x7830, 0, "66 44 89 7C 24 4C");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 0xC2 - 0xB7,
                    Before = ctx =>
                    {
                        short x = ctx.R15.ToInt16();
                        short y = ctx.BX.ToInt16();
                        Memory.WriteInt16(ctx.SP + 0x48 + 4, x);
                        Memory.WriteInt16(ctx.SP + 0x48 + 6, y);

                        x /= 12;
                        y /= 12;

                        Memory.WriteInt32(ctx.SP + 0x258 + 0x60, y);
                        Memory.WriteInt32(ctx.SP + 0x258 + 0x58, x);

                        ctx.R15 = new IntPtr(x);
                        ctx.BX = new IntPtr(y);
                    }
                });
            }

            // Exterior cell buffer must be extended if grass radius is outside of ugrids.
            // Reason: cell may get deleted while it still has grass and we can not keep grass there then.
            if(!load_only)
            {
                var addr = Main.GameInfo.GetAddressOf(13233, 0xB2 - 0x60, 0, "8B 05");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 6,
                    Before = ctx =>
                    {
                        int ugrids = Memory.ReadInt32(addr_uGrids + 8);
                        int ggrids = ChosenGrassGridRadius * 2 + 1;

                        ctx.AX = new IntPtr(Math.Max(ugrids, ggrids));
                    }
                });
            }
            
            // Allow grass distance to extend beyond uGrids * 4096 units (20480).
            {
                var addr = Main.GameInfo.GetAddressOf(15202, 0xB06 - 0x890, 0, "C1 E0 0C");
                Memory.WriteUInt8(addr + 2, 16, true);

                addr = Main.GameInfo.GetAddressOf(528751, 0xFE - 0xE0, 0, "C1 E0 0C");
                Memory.WriteUInt8(addr + 2, 16, true);
            }

            // Allow create grass without a land mesh. This is still necessary!
            if(!load_only)
            {
                var addr = Main.GameInfo.GetAddressOf(15204, 0xED2 - 0xD10, 0, "E8");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 5,
                    Before = ctx =>
                    {
                        ctx.AX = new IntPtr(1);
                    },
                });
            }

            // Cell unload should clear queued task. Otherwise it will crash or not allow creating grass again later.
            if(!load_only)
            {
                var addr = Main.GameInfo.GetAddressOf(18655, 0xC888 - 0xC7C0, 0, "E8");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 5,
                    Before = ctx =>
                    {
                        bool did = ClearCellAddGrassTask(ctx.DI);

                        if (debug_msg)
                        {
                            var cellObj = MemoryObject.FromAddress<NetScriptFramework.SkyrimSE.TESObjectCELL>(ctx.DI);
                            WriteDebugMsg("DataHandler::UnloadCell", cellObj.CoordinateX, cellObj.CoordinateY, "did: " + did);
                        }
                    }
                });
            }

            // Create custom way to load cell.
            if(!load_only)
            {
                var addr = Main.GameInfo.GetAddressOf(18137, 0x17, 0, "48 8B 4B 18 48 8D 53 20");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 8,
                    ReplaceLength = 8,
                    Before = ctx =>
                    {
                        byte ourMethod = Memory.ReadUInt8(ctx.BX + 0x3E);
                        if (ourMethod == 1)
                        {
                            ctx.Skip();
                            ctx.IP = ctx.IP + (0xA9 - 0x8F);

                            IntPtr ws = Memory.ReadPointer(ctx.BX + 0x20);
                            int x = Memory.ReadInt32(ctx.BX + 0x30);
                            int y = Memory.ReadInt32(ctx.BX + 0x34);

                            CellLoadNow_Our(ws, x, y);
                        }
                        else if (ourMethod != 0)
                            throw new InvalidOperationException("GrassControl.dll: unexpected ourMethod: " + ourMethod);
                    }
                });

                addr = Main.GameInfo.GetAddressOf(18150, 0xB094 - 0xAF20, 0, "88 43 3D 8B 74 24 30");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 7,
                    ReplaceLength = 7,
                    Before = ctx =>
                    {
                        IntPtr ws = Memory.ReadPointer(ctx.BX + 0x20);
                        int x = Memory.ReadInt32(ctx.BX + 0x30);
                        int y = Memory.ReadInt32(ctx.BX + 0x34);

                        byte isOurMethod = 0;
                        var c = Map.GetFromGrid(x, y);
                        if (c != null && System.Threading.Interlocked.CompareExchange(ref c.furtherLoad, 2, 1) == 1)
                            isOurMethod = 1;
                        Memory.WriteUInt8(ctx.BX + 0x3E, isOurMethod);
                    }
                });

                addr = Main.GameInfo.GetAddressOf(18149, 0xE1B - 0xCC0, 0, "C6 43 3C 01 44 88 7B 3D");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 8,
                    ReplaceLength = 8,
                    Before = ctx =>
                    {
                        Memory.WriteUInt8(ctx.BX + 0x3E, 0);
                    }
                });

                addr = Main.GameInfo.GetAddressOf(13148, 0x2630 - 0x2220, 0, "66 83 BC 24 90 00 00 00 00");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 9,
                    ReplaceLength = 9,
                    Before = ctx =>
                    {
                        int movedX = Memory.ReadInt16(ctx.SP + 0x90);
                        int movedY = ctx.R13.ToInt16();

                        IntPtr ws = Memory.ReadPointer(ctx.BX + 0x140);
                        if (ws == IntPtr.Zero)
                            return;

                        int prevX = Memory.ReadInt32(ctx.BX + 0xB8);
                        int prevY = Memory.ReadInt32(ctx.BX + 0xBC);

                        UpdateGrassGridQueue(ws, prevX, prevY, movedX, movedY);
                    }
                });

                addr = Main.GameInfo.GetAddressOf(13148, 0x29AF - 0x2220, 0, "E8");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 5,
                    ReplaceLength = 5,
                    After = ctx =>
                    {
                        var ws = Memory.ReadPointer(ctx.BX + 0x140);

                        int nowX = Memory.ReadInt32(ctx.BX + 0xB0);
                        int nowY = Memory.ReadInt32(ctx.BX + 0xB4);

                        UpdateGrassGridEnsureLoad(ws, nowX, nowY);
                    }
                });
            }
        }

        private static bool _canUpdateGridNormal = false;

        private static bool load_only;
        
        private static float ChosenGrassFadeRange
        {
            get
            {
                float r = GrassControlPlugin._settingsInstance.OverwriteGrassFadeRange;
                if (r < 0.0f)
                {
                    var addr = Main.GameInfo.GetAddressOf(501110);
                    r = Memory.ReadFloat(addr + 8);
                }

                return r;
            }
        }

        private static int ChosenGrassGridRadius
        {
            get
            {
                int r = _chosenGrassGridRadius;
                if(r < 0)
                {
                    float dist = GrassControlPlugin._settingsInstance.OverwriteGrassDistance;
                    if (dist < 0.0f)
                    {
                        var addr = Main.GameInfo.GetAddressOf(501108);
                        dist = Memory.ReadFloat(addr + 8);
                    }

                    float range = ChosenGrassFadeRange;
                    
                    float total = Math.Max(0.0f, dist) + Math.Max(0.0f, range);
                    float cells = total / 4096.0f;

                    int icells = (int)Math.Ceiling(cells);
                    if (icells < 2)
                        r = 2;
                    else if (icells > 32)
                    {
                        var l = Main.Log;
                        if (l != null)
                            l.AppendLine("GrassControl: calculated iGrassCellRadius is " + icells + "! Using 32 instead. This probably means fGrassFadeRange or fGrassFadeStartDistance is very strange value.");

                        r = 32;
                    }
                    else
                        r = icells;

                    int imin = 0;
                    if (GrassControlPlugin._settingsInstance.OverwriteMinGrassSize >= 0)
                        imin = GrassControlPlugin._settingsInstance.OverwriteMinGrassSize;
                    else
                    {
                        try
                        {
                            imin = Memory.ReadInt32(Main.GameInfo.GetAddressOf(501113) + 8);
                        }
                        catch
                        {

                        }
                    }

                    if (debug_msg)
                        WriteDebugMsg("ChosenGrassGridRadius", int.MinValue, 0, "chose: " + r + ", dist: " + dist + ", range: " + range + ", iMinGrassSize: " + imin);
                    else
                    {
                        var l = Main.Log;
                        if (l != null)
                            l.AppendLine("ChosenGrassSettings: grid: " + r + ", dist: " + dist.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", range: " + range.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", iMinGrassSize: " + imin);
                    }
                    _chosenGrassGridRadius = r;
                }
                
                return r;
            }
        }
        private static int _chosenGrassGridRadius = -1;

        private static IntPtr addr_GetCurrentWorldspaceCell;
        private static IntPtr addr_RemoveGrassFromCell;
        private static IntPtr addr_GrassManager;
        private static IntPtr addr_ClearGrassHandles;
        private static IntPtr addr_uGrids;
        private static IntPtr addr_AddGrassInCellDirect;
        private static IntPtr addr_QueueAddGrassInCell;
        private static IntPtr addr_GetCellInWorldspace;
        private static IntPtr addr_DataHandler;
        private static IntPtr addr_PrepareLandNode;
        private static IntPtr addr_GetOrCreateLand;
        private static IntPtr addr_WSGetCell;
        private static IntPtr addr_DHGetCell;
        private static IntPtr addr_TESLock;
        private static IntPtr addr_TESUnlock;
        private static IntPtr addr_GetCellByCoordMask;
        private static IntPtr addr_AllowLoadFile;
        private static IntPtr addr_EnsureLandData;
        private static IntPtr addr_QueueLoadCellByCoordinates;
        private static IntPtr addr_QueueLoadCellUnkGlobal;
        private static IntPtr addr_GetAddGrassTask;
        private static IntPtr addr_SetAddGrassTask;
        private static IntPtr addr_LoadGrassFromGID;
        private static IntPtr addr_GetOrCreateExtraData;

        private static IntPtr DummyCell_Ptr;

        private static bool DidApply = false;

        private static bool debug_msg = false;

        private static void WriteDebugMsg(string type, int x, int y, string extra = null)
        {
            string middle = x == int.MinValue || y == int.MinValue ? "null" : (x.ToString() + ", " + y);
            string text = type + "(" + middle + ")" + (extra != null ? (" " + extra) : "");
            Main.WriteDebugMessage(text);

            var l = Main.Log;
            if (l != null)
                l.AppendLine(text);
        }

        private static void CellLoadNow_Our(IntPtr ws, int x, int y)
        {
            var c = Map.GetFromGrid(x, y);
            if (c == null || System.Threading.Interlocked.CompareExchange(ref c.furtherLoad, 0, 2) != 2)
                return;

            if (ws == IntPtr.Zero)
                return;

            var cell = Memory.InvokeCdecl(addr_WSGetCell, ws, x, y);
            if (IsValidLoadedCell(cell, false))
            {
                if (debug_msg)
                    WriteDebugMsg("FurtherLoadSuccessFirst", x, y, MemoryObject.FromAddress<NetScriptFramework.SkyrimSE.TESObjectCELL>(cell).ToString());
            }
            else if(cell != IntPtr.Zero)
            {
                var landPtr = Memory.InvokeCdecl(addr_GetOrCreateLand, cell);
                if (landPtr != IntPtr.Zero && (Memory.ReadUInt8(landPtr + 0x28) & 8) == 0)
                {
                    Memory.InvokeCdecl(addr_EnsureLandData, landPtr, 0, 1);

                    if (IsValidLoadedCell(cell, false))
                    {
                        if (debug_msg)
                            WriteDebugMsg("FurtherLoadSuccessSecond", c.x, c.y, MemoryObject.FromAddress<NetScriptFramework.SkyrimSE.TESObjectCELL>(cell).ToString());
                    }
                }
            }
        }
        
        // This is only ever called from when we are adding grass, calling from outside is not valid.
        internal static bool IsLodCell(IntPtr cell)
        {
            if (!DidApply || load_only)
                return false;

            if (cell == IntPtr.Zero)
                return false;
            
            var c = Map.FindByCell(cell);
            if (c == null)
                return false;

            int d = c.self_data;
            if (((d >> 8) & 0xFF) == (int)GrassStates.Lod)
                return true;

            return false;
        }

        private static bool ClearCellAddGrassTask(IntPtr cell)
        {
            if (cell == IntPtr.Zero)
                return false;

            IntPtr task;
            using (var alloc = Memory.Allocate(0x10))
            {
                Memory.WritePointer(alloc.Address, IntPtr.Zero);

                Memory.InvokeCdecl(addr_GetAddGrassTask, cell + 0x48, alloc.Address);

                task = Memory.ReadPointer(alloc.Address);
            }

            if (task == IntPtr.Zero)
                return false;

            Memory.WriteUInt8(task + 0x48, 1);

            if(Memory.InterlockedDecrement32(task + 8) == 0)
            {
                var vtable = Memory.ReadPointer(task);
                var dtor = Memory.ReadPointer(vtable);
                Memory.InvokeCdecl(dtor, task, 1);
            }

            Memory.InvokeCdecl(addr_SetAddGrassTask, cell + 0x48, IntPtr.Zero);

            return true;
        }
        
        private enum GrassStates : byte
        {
            None,

            Lod,

            Active,
        }

        private sealed class cell_info
        {
            internal cell_info(int _x, int _y)
            {
                x = _x;
                y = _y;
            }

            internal readonly int x;
            internal readonly int y;

            internal IntPtr cell;
            internal int self_data;
            internal int furtherLoad;
            
            internal bool checkHasFile(string wsName, bool lod)
            {
                if (string.IsNullOrEmpty(wsName))
                    return false;

                string x_str = (x < 0 ? x.ToString("D3") : x.ToString("D4"));
                string y_str = (y < 0 ? y.ToString("D3") : y.ToString("D4"));
                string fpath = "Data/Grass/" + wsName + "x" + x_str + "y" + y_str;

                if (lod)
                    fpath = fpath + ".dgid";
                else
                    fpath = fpath + ".cgid";

                bool has = false;
                try
                {
                    if (new System.IO.FileInfo(fpath).Exists)
                        has = true;
                }
                catch
                {

                }

                return has;
            }
        }

        private static CellInfoContainer Map;
        private static LoadOnlyCellInfoContainer LOMap;

        private static bool IsValidLoadedCell(IntPtr cell, bool quickLoad)
        {
            if (cell == IntPtr.Zero)
                return false;

            if (quickLoad)
                return true;

            var land = Memory.ReadPointer(cell + 0x68);
            if (land == IntPtr.Zero)
                return false;

            var data = Memory.ReadPointer(land + 0x40);
            if (data == IntPtr.Zero)
                return false;

            return true;
        }

        private static IntPtr GetCurrentWorldspaceCell(IntPtr tes, IntPtr ws, int x, int y, bool quickLoad, bool allowLoadNow)
        {
            var data = Memory.ReadPointer(addr_DataHandler);
            if (data == IntPtr.Zero || ws == IntPtr.Zero)
                return IntPtr.Zero;

            IntPtr cell = Memory.InvokeCdecl(addr_GetCurrentWorldspaceCell, tes, x, y);
            if (IsValidLoadedCell(cell, quickLoad))
            {
                if (debug_msg)
                    WriteDebugMsg("add_GetCell", x, y, "(0): " + MemoryObject.FromAddress<NetScriptFramework.SkyrimSE.TESObjectCELL>(cell).ToString());
                return cell;
            }

            if (allowLoadNow)
            {
                var c = Map.GetFromGrid(x, y);
                if (c != null)
                    System.Threading.Interlocked.CompareExchange(ref c.furtherLoad, 0, 2);

                Memory.InvokeCdecl(addr_TESLock);
                try
                {
                    cell = Memory.InvokeCdecl(addr_WSGetCell, ws, x, y);
                    if (IsValidLoadedCell(cell, quickLoad))
                    {
                        if (debug_msg)
                            WriteDebugMsg("add_GetCell", x, y, "(1): " + MemoryObject.FromAddress<NetScriptFramework.SkyrimSE.TESObjectCELL>(cell).ToString());
                        return cell;
                    }
                }
                finally
                {
                    Memory.InvokeCdecl(addr_TESUnlock);
                }

                if (cell != IntPtr.Zero)
                {
                    var landPtr = Memory.InvokeCdecl(addr_GetOrCreateLand, cell);
                    if (landPtr != IntPtr.Zero && (Memory.ReadUInt8(landPtr + 0x28) & 8) == 0)
                    {
                        Memory.InvokeCdecl(addr_EnsureLandData, landPtr, 0, 1);

                        if (IsValidLoadedCell(cell, false))
                        {
                            if (debug_msg)
                                WriteDebugMsg("add_GetCell", x, y, "(2): " + MemoryObject.FromAddress<NetScriptFramework.SkyrimSE.TESObjectCELL>(cell).ToString());
                            return cell;
                        }
                    }
                }
            }
            
            if (debug_msg)
            {
                string failData = "null";
                if (cell != IntPtr.Zero)
                {
                    var obj = MemoryObject.FromAddress<NetScriptFramework.SkyrimSE.TESObjectCELL>(cell);
                    if (obj == null)
                        failData = "bad_cell";
                    else
                        failData = "loadState: " + ((int)obj.CellState).ToString() + " hasTemp: " + ((Memory.ReadUInt8(cell + 0x40) & 0x10) != 0 ? 1 : 0);
                }
                WriteDebugMsg("add_GetCell", x, y, "FAIL " + failData);
            }

            return IntPtr.Zero;
        }

        private static void Call_AddGrassNow(IntPtr cell, IntPtr customArg)
        {
            if (Memory.ReadUInt8(customArg) != 0)
                return;

            lock(Map.locker)
            {
                cell_info c;
                Map.map.TryGetValue(cell, out c);
                if (c == null)
                {
                    if (debug_msg)
                        WriteDebugMsg("AddedGrass", int.MinValue, 0, "warning: c == null");
                    return;
                }

                int d = c.self_data;
                int tg = (d >> 8) & 0xFF;

                bool quickLoad = ((d >> 16) & 0xFF) != 0;
                if (IsValidLoadedCell(cell, quickLoad))
                {
                    var grassMgr = Memory.ReadPointer(addr_GrassManager);

                    if (debug_msg)
                        WriteDebugMsg("AddingGrass", c.x, c.y, "ca: " + Memory.ReadUInt8(customArg) + ", cell: " + MemoryObject.FromAddress<NetScriptFramework.SkyrimSE.TESObjectCELL>(cell).ToString());

                    Memory.InvokeCdecl(addr_AddGrassInCellDirect, grassMgr, cell, customArg);

                    if (debug_msg)
                        WriteDebugMsg("AddedGrass", c.x, c.y);
                }
                else if (debug_msg)
                    WriteDebugMsg("AddedGrassFAIL", c.x, c.y, "<NotValidLoadedCell>");

                // Set this anyway otherwise we get stuck.
                c.self_data = tg;
            }
        }
        
        private static GrassStates GetWantState(cell_info c, int curX, int curY, int uGrid, int grassRadius, bool canLoadFromFile, string wsName)
        {
            int diffX = Math.Abs(curX - c.x);
            int diffY = Math.Abs(curY - c.y);

            if (diffX > grassRadius || diffY > grassRadius)
                return GrassStates.None;

            if (load_only)
                return GrassStates.Active;

            int uHalf = uGrid / 2;
            if (diffX > uHalf || diffY > uHalf)
            {
                // Special case: if we are loading and not generating anyway and already have active file we can take active instead of lod
                if (canLoadFromFile && c.checkHasFile(wsName, false))
                    return GrassStates.Active;

                return GrassStates.Lod;
            }

            return GrassStates.Active;
        }

        private static void Handle_RemoveGrassFromCell_Call(IntPtr grassMgr, IntPtr cell, string why)
        {
            if (cell == IntPtr.Zero)
            {
                if (debug_msg)
                    WriteDebugMsg("RemoveGrassOther", int.MinValue, 0, "<- " + why);
                return;
            }

            lock(Map.locker)
            {
                cell_info c;
                Map.map.TryGetValue(cell, out c);
                if (c != null)
                {
                    if (debug_msg)
                    {
                        int d = c.self_data;
                        if ((d >> 24) != 0)
                            WriteDebugMsg("RemoveGrassOther", c.x, c.y, "<- " + why + " warning: busy");
                        else
                            WriteDebugMsg("RemoveGrassOther", c.x, c.y, "<- " + why);
                    }

                    c.self_data = 0;
                    c.cell = IntPtr.Zero;
                    Map.map.Remove(cell);
                    ClearCellAddGrassTask(cell);
                }
                else if (debug_msg)
                {
                    var cellObj = MemoryObject.FromAddress<NetScriptFramework.SkyrimSE.TESObjectCELL>(cell);
                    WriteDebugMsg("cell_dtor", cellObj.CoordinateX, cellObj.CoordinateY);
                }
            }
            
            Memory.InvokeCdecl(addr_RemoveGrassFromCell, grassMgr, cell);
        }

        private static byte CalculateLoadState(int nowX, int nowY, int x, int y, int ugrid, int ggrid)
        {
            int diffX = Math.Abs(x - nowX);
            int diffY = Math.Abs(y - nowY);

            if (diffX <= ugrid && diffY <= ugrid)
                return 2;

            if (diffX <= ggrid && diffY <= ggrid)
                return 1;

            return 0;
        }

        private static void UpdateGrassGridEnsureLoad(IntPtr ws, int nowX, int nowY)
        {
            if (ws == IntPtr.Zero)
                return;

            int grassRadius = ChosenGrassGridRadius;
            int uGrids = Memory.ReadInt32(addr_uGrids + 8);
            int uHalf = uGrids / 2;
            int bigSide = Math.Max(grassRadius, uHalf);
            bool canLoadGrass = Memory.ReadUInt8(addr_AllowLoadFile + 8) != 0;

            string wsName = null;
            try
            {
                var wsObj = MemoryObject.FromAddress<NetScriptFramework.SkyrimSE.TESWorldSpace>(ws);
                if (wsObj != null)
                    wsName = wsObj.EditorId;
            }
            catch
            {

            }

            for (int x = nowX - bigSide; x <= nowX + bigSide; x++)
            {
                for (int y = nowY - bigSide; y <= nowY + bigSide; y++)
                {
                    byte wantState = CalculateLoadState(nowX, nowY, x, y, uHalf, grassRadius);
                    if (wantState != 1)
                        continue;

                    var c = Map.GetFromGrid(x, y);
                    if (c == null)
                        continue;

                    short xs = (short)x;
                    short ys = (short)y;

                    ushort xsu = unchecked((ushort)xs);
                    ushort ysu = unchecked((ushort)ys);

                    uint mask = (uint)xsu << 16;
                    mask |= ysu;

                    var cell = Memory.InvokeCdecl(addr_GetCellByCoordMask, ws, mask);
                    if (IsValidLoadedCell(cell, false))
                        continue;

                    bool quickLoad = false;
                    if (canLoadGrass && (c.checkHasFile(wsName, false) || c.checkHasFile(wsName, true)))
                        quickLoad = true;

                    if (quickLoad && IsValidLoadedCell(cell, true))
                        continue;

                    System.Threading.Interlocked.CompareExchange(ref c.furtherLoad, 0, 2);

                    cell = Memory.InvokeCdecl(addr_WSGetCell, ws, x, y);
                    if (IsValidLoadedCell(cell, false))
                    {
                        if (debug_msg)
                            WriteDebugMsg("InstantLoadSuccessFirst", x, y, MemoryObject.FromAddress<NetScriptFramework.SkyrimSE.TESObjectCELL>(cell).ToString());
                    }
                    else if (cell != IntPtr.Zero)
                    {
                        var landPtr = Memory.InvokeCdecl(addr_GetOrCreateLand, cell);
                        if (landPtr != IntPtr.Zero && (Memory.ReadUInt8(landPtr + 0x28) & 8) == 0)
                        {
                            Memory.InvokeCdecl(addr_EnsureLandData, landPtr, 0, 1);

                            if (IsValidLoadedCell(cell, false))
                            {
                                if (debug_msg)
                                    WriteDebugMsg("InstantLoadSuccessSecond", c.x, c.y, MemoryObject.FromAddress<NetScriptFramework.SkyrimSE.TESObjectCELL>(cell).ToString());
                            }
                        }
                    }
                }
            }
        }

        private static void UpdateGrassGridQueue(IntPtr ws, int prevX, int prevY, int movedX, int movedY)
        {
            if (ws == IntPtr.Zero)
                return;

            int grassRadius = ChosenGrassGridRadius;
            int uGrids = Memory.ReadInt32(addr_uGrids + 8);
            int uHalf = uGrids / 2;
            int bigSide = Math.Max(grassRadius, uHalf);
            bool canLoadGrass = Memory.ReadUInt8(addr_AllowLoadFile + 8) != 0;

            string wsName = null;
            try
            {
                var wsObj = MemoryObject.FromAddress<NetScriptFramework.SkyrimSE.TESWorldSpace>(ws);
                if (wsObj != null)
                    wsName = wsObj.EditorId;
            }
            catch
            {

            }

            int nowX = prevX + movedX;
            int nowY = prevY + movedY;

            for(int x = nowX - bigSide; x <= nowX + bigSide; x++)
            {
                for(int y = nowY - bigSide; y <= nowY + bigSide; y++)
                {
                    byte wantState = CalculateLoadState(nowX, nowY, x, y, uHalf, grassRadius);
                    if (wantState != 1)
                        continue;

                    var c = Map.GetFromGrid(x, y);
                    if (c == null)
                        continue;

                    short xs = (short)x;
                    short ys = (short)y;

                    ushort xsu = unchecked((ushort)xs);
                    ushort ysu = unchecked((ushort)ys);

                    uint mask = (uint)xsu << 16;
                    mask |= ysu;

                    var cell = Memory.InvokeCdecl(addr_GetCellByCoordMask, ws, mask);
                    if (IsValidLoadedCell(cell, false))
                        continue;

                    bool quickLoad = false;
                    if (canLoadGrass && (c.checkHasFile(wsName, false) || c.checkHasFile(wsName, true)))
                        quickLoad = true;

                    if (quickLoad && IsValidLoadedCell(cell, true))
                        continue;

                    if(System.Threading.Interlocked.CompareExchange(ref c.furtherLoad, 1, 0) == 0)
                        Memory.InvokeCdecl(addr_QueueLoadCellByCoordinates, Memory.ReadPointer(addr_QueueLoadCellUnkGlobal), ws, x, y, 0);
                }
            }
        }

        private static void UpdateGrassGridNow(IntPtr tes, int movedX, int movedY, int addType)
        {
            if(load_only)
            {
                UpdateGrassGridNow_LoadOnly(tes, movedX, movedY, addType);
                return;
            }

            if (debug_msg)
                WriteDebugMsg("UpdateGrassGridNowBegin", movedX, movedY, "type: " + addType);

            int grassRadius = ChosenGrassGridRadius;
            int uGrids = Memory.ReadInt32(addr_uGrids + 8);
            int uHalf = uGrids / 2;
            int bigSide = Math.Max(grassRadius, uHalf);
            IntPtr ws = Memory.ReadPointer(tes + 0x140);
            IntPtr grassMgr = Memory.ReadPointer(addr_GrassManager);
            bool canLoadGrass = Memory.ReadUInt8(addr_AllowLoadFile + 8) != 0;
            uint wsId = uint.MaxValue;
            NetScriptFramework.SkyrimSE.TESWorldSpace wsObj = null;
            string wsName = "";
            if (ws != IntPtr.Zero)
            {
                wsObj = MemoryObject.FromAddress<NetScriptFramework.SkyrimSE.TESWorldSpace>(ws);
                if (wsObj != null)
                {
                    wsId = wsObj.FormId;
                    wsName = wsObj.EditorId;
                }
            }
            
            int nowX = Memory.ReadInt32(tes + 0xB0);
            int nowY = Memory.ReadInt32(tes + 0xB4);
            
            var invokeList = new List<IntPtr>();
            if (addType <= 0)
            {
                lock (Map.locker)
                {
                    Map.unsafe_ForeachWithState(c =>
                    {
                        var want = addType < 0 ? GrassStates.None : GetWantState(c, nowX, nowY, uGrids, grassRadius, false, null);
                        if (want == GrassStates.None)
                        {
                            var cell = c.cell;
                            c.cell = IntPtr.Zero;
                            c.self_data = 0;
                            ClearCellAddGrassTask(cell);

                            if (debug_msg)
                                WriteDebugMsg("RemoveGrassGrid", c.x, c.y);

                            invokeList.Add(cell);
                            return false;
                        }

                        return true;
                    });
                }

                if (invokeList.Count != 0)
                {
                    foreach (var cell in invokeList)
                        Memory.InvokeCdecl(addr_RemoveGrassFromCell, grassMgr, cell);
                    
                    invokeList.Clear();
                }
            }

            if (addType >= 0)
            {
                lock (Map.locker)
                {
                    int minX = nowX - bigSide;
                    int maxX = nowX + bigSide;
                    int minY = nowY - bigSide;
                    int maxY = nowY + bigSide;

                    // Add grass after.
                    for (int x = minX; x <= maxX; x++)
                    {
                        for (int y = minY; y <= maxY; y++)
                        {
                            var c = Map.GetFromGrid(x, y);
                            if (c == null)
                                continue;

                            int d = c.self_data;
                            bool busy = (d >> 24) != 0;

                            var want = GetWantState(c, nowX, nowY, uGrids, grassRadius, canLoadGrass, wsName);
                            if (want > (GrassStates)(d & 0xFF)) // this check is using > because there's no need to set lod grass if we already have active grass
                            {
                                bool canQuickLoad = want == GrassStates.Active || (canLoadGrass && c.checkHasFile(wsName, true));
                                var cellPtr = GetCurrentWorldspaceCell(tes, ws, x, y, canQuickLoad, addType > 0);
                                
                                if (cellPtr != IntPtr.Zero)
                                {
                                    if (c.cell != cellPtr)
                                    {
                                        if(c.cell != IntPtr.Zero)
                                        {
                                            if (debug_msg)
                                                WriteDebugMsg("c.cell", c.x, c.y, "warning: already had cell!");
                                        }
                                        c.cell = cellPtr;
                                        Map.map.Add(cellPtr, c);
                                    }

                                    // set busy and target
                                    c.self_data = 0x01000000 | (canQuickLoad ? 0x00010000 : 0) | ((int)want << 8) | (d & 0xFF);

                                    if (debug_msg)
                                        WriteDebugMsg("QueueGrass", x, y);

                                    if(!busy)
                                        invokeList.Add(cellPtr);
                                }
                            }
                        }
                    }
                }

                if(invokeList.Count != 0)
                {
                    foreach(var cell in invokeList)
                    {
                        // The way game does it is do it directly if generate grass files is enabled, But we don't want that because it causes a lot of stuttering.
                        Memory.InvokeCdecl(addr_QueueAddGrassInCell, cell);
                    }
                }
            }

            if (debug_msg)
                WriteDebugMsg("UpdateGrassGridNowEnd", movedX, movedY, "type: " + addType);
        }

        private static void UpdateGrassGridNow_LoadOnly(IntPtr tes, int movedX, int movedY, int addType)
        {
            if (debug_msg)
                WriteDebugMsg("UpdateGrassGridNowBegin_LoadOnly", movedX, movedY, "type: " + addType);

            int grassRadius = ChosenGrassGridRadius;
            IntPtr ws = Memory.ReadPointer(tes + 0x140);
            IntPtr grassMgr = Memory.ReadPointer(addr_GrassManager);
            bool canLoadGrass = Memory.ReadUInt8(addr_AllowLoadFile + 8) != 0;
            uint wsId = uint.MaxValue;
            NetScriptFramework.SkyrimSE.TESWorldSpace wsObj = null;
            string wsName = "";
            if (ws != IntPtr.Zero)
            {
                wsObj = MemoryObject.FromAddress<NetScriptFramework.SkyrimSE.TESWorldSpace>(ws);
                if (wsObj != null)
                {
                    wsId = wsObj.FormId;
                    wsName = wsObj.EditorId;
                }
            }

            int nowX = Memory.ReadInt32(tes + 0xB0);
            int nowY = Memory.ReadInt32(tes + 0xB4);

            if (addType <= 0)
            {
                LOMap.ForeachWithRemove(c =>
                {
                    if (addType >= 0 && Math.Abs(c.X - nowX) <= grassRadius && Math.Abs(c.Y - nowY) <= grassRadius)
                        return true;

                    if(c.ExtraDataContent != null)
                    {
                        Memory.WritePointer(DummyCell_Ptr + 0x120, ws);
                        var ext = Memory.ReadPointer(DummyCell_Ptr + 0x60);
                        Memory.WriteInt32(ext, (int)c.X);
                        Memory.WriteInt32(ext + 4, (int)c.Y);

                        var extraDataPtr = Memory.InvokeCdecl(addr_GetOrCreateExtraData, DummyCell_Ptr + 0x48, 1);
                        Memory.WriteBytes(extraDataPtr, c.ExtraDataContent);

                        c.ExtraDataContent = null;

                        if (debug_msg)
                            WriteDebugMsg("RemoveGrassFromCell", c.X, c.Y);

                        Memory.InvokeCdecl(addr_RemoveGrassFromCell, grassMgr, DummyCell_Ptr);
                        Memory.InvokeCdecl(addr_ClearGrassHandles, DummyCell_Ptr + 0x48);
                    }
                    else
                    {
                        //if (debug_msg) WriteDebugMsg("RemoveGrassFromCell", c.X, c.Y, "Didn't have");
                    }

                    c.State = loadonly_cell.lo_cell_states.none;
                    return false;
                });
            }

            if (addType >= 0)
            {
                short minX = (short)(nowX - grassRadius);
                short maxX = (short)(nowX + grassRadius);
                short minY = (short)(nowY - grassRadius);
                short maxY = (short)(nowY + grassRadius);

                // Add grass after.
                for (short x = minX; x <= maxX; x++)
                {
                    for (short y = minY; y <= maxY; y++)
                    {
                        var c = LOMap.Ensure(x, y);

                        // Out of bounds.
                        if (c == null)
                            continue;

                        // Already tried to load or is loaded.
                        if (c.State != loadonly_cell.lo_cell_states.none)
                            continue;

                        c.State = loadonly_cell.lo_cell_states.failed;

                        // No file can't do anything.
                        if (!c.checkHasFile(wsName))
                        {
                            if (debug_msg)
                                WriteDebugMsg("CantAddGrass", x, y, "NoFile - Data/Grass/" + (wsName ?? "") + "x" + (c.X < 0 ? c.X.ToString("D3") : c.X.ToString("D4")) + "y" + (c.Y < 0 ? c.Y.ToString("D3") : c.Y.ToString("D4")) + ".cgid");
                            continue;
                        }

                        Memory.WritePointer(DummyCell_Ptr + 0x120, ws);
                        var ext = Memory.ReadPointer(DummyCell_Ptr + 0x60);
                        Memory.WriteInt32(ext, (int)x);
                        Memory.WriteInt32(ext + 4, (int)y);

                        Memory.InvokeCdecl(addr_LoadGrassFromGID, grassMgr, DummyCell_Ptr);

                        var extraDataPtr = Memory.InvokeCdecl(addr_GetOrCreateExtraData, DummyCell_Ptr + 0x48, 0);
                        if (extraDataPtr != IntPtr.Zero && Memory.ReadInt32(extraDataPtr + 0x10) != 0)
                        {
                            if (debug_msg)
                                WriteDebugMsg("AddedGrass", x, y);

                            c.ExtraDataContent = Memory.ReadBytes(extraDataPtr, 0x20);
                            Memory.WriteZero(extraDataPtr, 0x20);
                            c.State = loadonly_cell.lo_cell_states.ok;
                        }
                        else
                        {
                            if (debug_msg)
                                WriteDebugMsg("DidntAddGrass", x, y, "edc: " + (extraDataPtr == IntPtr.Zero ? "null" : Memory.ReadInt32(extraDataPtr + 0x10).ToString()));
                        }

                        Memory.InvokeCdecl(addr_ClearGrassHandles, DummyCell_Ptr + 0x48);
                    }
                }
            }

            if (debug_msg)
                WriteDebugMsg("UpdateGrassGridNowEnd_LoadOnly", movedX, movedY, "type: " + addType + "; c_count: " + LOMap.GetCount() + "; shapeCount: " + Memory.ReadInt32(grassMgr + 0x58));
        }

        private class loadonly_cell
        {
            internal loadonly_cell(short x, short y)
            {
                this.X = x;
                this.Y = y;
            }

            internal readonly short X;
            internal readonly short Y;
            internal lo_cell_states State;
            // 3 more bytes can fit here
            internal byte[] ExtraDataContent;

            internal enum lo_cell_states : byte
            {
                none = 0,

                failed = 1,

                ok = 2,
            }

            internal bool checkHasFile(string wsName, bool lod = false)
            {
                if (string.IsNullOrEmpty(wsName))
                    return false;

                string x_str = (this.X < 0 ? this.X.ToString("D3") : this.X.ToString("D4"));
                string y_str = (this.Y < 0 ? this.Y.ToString("D3") : this.Y.ToString("D4"));
                string fpath = "Data/Grass/" + wsName + "x" + x_str + "y" + y_str;

                if (lod)
                    fpath = fpath + ".dgid";
                else
                    fpath = fpath + ".cgid";

                bool has = false;
                try
                {
                    if (new System.IO.FileInfo(fpath).Exists)
                        has = true;
                }
                catch
                {

                }

                return has;
            }
        }

        private class LoadOnlyCellInfoContainer
        {
            internal LoadOnlyCellInfoContainer()
            {

            }

            private readonly Dictionary<uint, loadonly_cell> Map = new Dictionary<uint, loadonly_cell>();

            internal int GetCount()
            {
                thread_check();

                return Map.Count;
            }

            private static uint CreateKey(short x, short y)
            {
                ushort x_u = unchecked((ushort)x);
                ushort y_u = unchecked((ushort)y);

                uint m = x_u;
                m <<= 16;
                m |= y_u;
                return m;
            }

            internal loadonly_cell Get(short x, short y)
            {
                thread_check();

                loadonly_cell c;
                uint m = CreateKey(x, y);
                this.Map.TryGetValue(m, out c);
                return c;
            }

            internal loadonly_cell Ensure(short x, short y)
            {
                thread_check();

                if (x < -64 || x > 64 || y < -64 || y > 64)
                    return null;

                loadonly_cell c;
                uint m = CreateKey(x, y);
                if (this.Map.TryGetValue(m, out c))
                    return c;
                c = new loadonly_cell(x, y);
                this.Map[m] = c;
                return c;
            }

            internal bool Remove(short x, short y)
            {
                thread_check();

                loadonly_cell c;
                uint m = CreateKey(x, y);
                if (!this.Map.TryGetValue(m, out c))
                    return false;
                this.Map.Remove(m);
                return true;
            }

            internal void Clear()
            {
                thread_check();

                this.Map.Clear();
            }

            internal void ForeachWithRemove(Func<loadonly_cell, bool> func)
            {
                thread_check();

                List<uint> rem = null;

                foreach(var pair in this.Map)
                {
                    if (func(pair.Value))
                        continue;

                    if (rem == null)
                        rem = new List<uint>();
                    rem.Add(pair.Key);
                }

                if (rem == null)
                    return;

                foreach(var m in rem)
                    this.Map.Remove(m);
            }

            private static void thread_check()
            {
                int now = Memory.GetCurrentNativeThreadId();
                int prev = System.Threading.Interlocked.CompareExchange(ref _orig_thread_id, now, -1);

                if (prev == now || prev == -1)
                    return;

                throw new InvalidOperationException("LOMap not thread safe!");
            }

            private static int _orig_thread_id = -1;
        }

        private class CellInfoContainer
        {
            internal CellInfoContainer()
            {
                grid = new cell_info[129 * 129];
                for(int x = 0; x <= 128; x++)
                {
                    for(int y = 0; y <= 128; y++)
                        grid[x * 129 + y] = new cell_info(x - 64, y - 64);
                }
            }

            internal cell_info GetFromGrid(int x, int y)
            {
                if (x < -64 || y < -64 || x > 64 || y > 64)
                    return null;

                x += 64;
                y += 64;

                return this.grid[x * 129 + y];
            }

            private readonly cell_info[] grid;

            internal readonly Dictionary<IntPtr, cell_info> map = new Dictionary<IntPtr, cell_info>(1024);

            internal readonly object locker = new object();
            
            internal void unsafe_ForeachWithState(Func<cell_info, bool> action)
            {
                var all = map.ToList();
                foreach(var pair in all)
                {
                    if (!action(pair.Value))
                        map.Remove(pair.Key);
                }
            }

            internal cell_info FindByCell(IntPtr cell)
            {
                cell_info c;
                lock(this.locker)
                {
                    this.map.TryGetValue(cell, out c);
                }
                return c;
            }
        }
    }
}
