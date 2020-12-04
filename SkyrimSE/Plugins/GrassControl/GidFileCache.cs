using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework;

namespace GrassControl
{
    internal static class GidFileCache
    {
        internal static void FixFileFormat()
        {
            try
            {
                var dir = new System.IO.DirectoryInfo("data/grass");
                if (!dir.Exists)
                    dir.Create();
            }
            catch
            {

            }

            // Fix saving the GID files (because bethesda broke it in SE).
            {
                var addrHook = Main.GameInfo.GetAddressOf(74601, 0xB90 - 0xAE0, 0, "49 8D 48 08");
                var addrCall = Main.GameInfo.GetAddressOf(74621);
                Memory.WriteHook(new HookParameters()
                {
                    Address = addrHook,
                    IncludeLength = 0,
                    ReplaceLength = 0x13,
                    After = ctx =>
                    {
                        Memory.InvokeCdecl(addrCall, ctx.R8 + 8, ctx.BP - 0x30, 0x24);
                        var ptrThing = Memory.ReadPointer(ctx.CX + 0x40);
                        var ptrBuf = Memory.ReadPointer(ptrThing + 8);
                        int ptrSize = Memory.ReadInt32(ptrThing + 0x10);
                        Memory.InvokeCdecl(addrCall, ctx.R8 + 8, ptrBuf, ptrSize);
                    },
                });
            }

            // Use a different file extension because we don't want to load the broken .gid files from BSA.
            {
                CustomGrassFileName = AllocateString("Grass\\\\%sx%04dy%04d.cgid");
                CustomGrassLodFileName = AllocateString("Grass\\\\%sx%04dy%04d.dgid");

                // Saving.
                var addr = Main.GameInfo.GetAddressOf(15204, 0x5357 - 0x4D10, 0, "4C 8D 05");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 7,
                    Before = ctx =>
                    {
                        // SI is cell

                        if (DistantGrass.IsLodCell(ctx.SI))
                            ctx.R8 = CustomGrassLodFileName;
                        else
                            ctx.R8 = CustomGrassFileName;
                    }
                });

                // Loading.
                addr = Main.GameInfo.GetAddressOf(15206, 0xC4, 0, "4C 8D 05");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 7,
                    Before = ctx =>
                    {
                        // R13 is cell

                        if (DistantGrass.IsLodCell(ctx.R13))
                            ctx.R8 = CustomGrassLodFileName;
                        else
                            ctx.R8 = CustomGrassFileName;
                    }
                });
            }

            // Set the ini stuff.
            {
                var addr = Main.GameInfo.GetAddressOf(15204, 0xAC - 0x10, 0, "44 38 3D");
                Memory.WriteNop(addr, 0xB5 - 0xAC);

                addr = Main.GameInfo.GetAddressOf(15202, 0xBE7 - 0x890, 0, "0F B6 05");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 7,
                    Before = ctx =>
                    {
                        ctx.AX = new IntPtr(1);
                    }
                });
            }

            // Disable grass console.
            {
                var addr = Main.GameInfo.GetAddressOf(15204, 0x55A6 - 0x4D10, 0, "48 8D 05");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 7,
                    Before = ctx =>
                    {
                        ctx.IP = ctx.IP + (0x5882 - 0x55AD);
                        ctx.R15 = IntPtr.Zero;
                    }
                });
            }
        }
        
        private static IntPtr CustomGrassFileName;

        private static IntPtr CustomGrassLodFileName;

        private static IntPtr AllocateString(string text)
        {
            text = text ?? "";

            IntPtr addr;
            using (var alloc = Memory.Allocate(text.Length + 2))
            {
                alloc.Pin();
                addr = alloc.Address;

                Memory.WriteString(alloc.Address, text, false);
                Memory.WriteUInt8(alloc.Address + text.Length, 0);
            }

            return addr;
        }
    }

    internal sealed class GidFileGenerationTask
    {
        static GidFileGenerationTask()
        {
            addr_EnterLock = Main.GameInfo.GetAddressOf(13188);
            addr_ExitLock = Main.GameInfo.GetAddressOf(13189);
            addr_WSLoadCellByCoordinates = Main.GameInfo.GetAddressOf(20026);
            addr_Load_impl = Main.GameInfo.GetAddressOf(18159);
            addr_unk_CellRefs = Main.GameInfo.GetAddressOf(18563);
            addr_UnloadCell = Main.GameInfo.GetAddressOf(13623);
            addr_unk_load6 = Main.GameInfo.GetAddressOf(18518);
            addr_unk_load7 = Main.GameInfo.GetAddressOf(18594);
            addr_AddGrassNow = Main.GameInfo.GetAddressOf(15204);
            addr_GrassMgr = Main.GameInfo.GetAddressOf(514292);
            addr_ChangeWS = Main.GameInfo.GetAddressOf(13170);
            addr_ShowMessageBox = NetScriptFramework.Main.GameInfo.GetAddressOf(54737);
            addr_UpdateGridLoad = Main.GameInfo.GetAddressOf(13148);
            addr_SetPlrTo = Main.GameInfo.GetAddressOf(39657);
            addr_uGrids = Main.GameInfo.GetAddressOf(501244);
            addr_PrintConsole = Main.GameInfo.GetAddressOf(50179);
            addr_ConsoleSingleton = Main.GameInfo.GetAddressOf(515064);
        }

        internal GidFileGenerationTask()
        {
            if (cur_instance != null)
                throw new InvalidOperationException();
            cur_instance = this;
        }

        internal static IntPtr addr_EnterLock;
        internal static IntPtr addr_ExitLock;
        internal static IntPtr addr_WSLoadCellByCoordinates;
        internal static IntPtr addr_Load_impl;
        internal static IntPtr addr_unk_CellRefs;
        internal static IntPtr addr_UnloadCell;
        internal static IntPtr addr_unk_load6;
        internal static IntPtr addr_unk_load7;
        internal static IntPtr addr_AddGrassNow;
        internal static IntPtr addr_GrassMgr;
        internal static IntPtr addr_ChangeWS;
        internal static IntPtr addr_ShowMessageBox;
        internal static IntPtr addr_UpdateGridLoad;
        internal static IntPtr addr_SetPlrTo;
        internal static IntPtr addr_uGrids;
        internal static IntPtr addr_PrintConsole;
        internal static IntPtr addr_ConsoleSingleton;

        internal static int ChosenGrassGridRadius
        {
            get
            {
                int r = _chosenGrassGridRadius;
                if (r < 0)
                {
                    int ugrid = Memory.ReadInt32(addr_uGrids + 8);
                    r = (ugrid - 1) / 2;
                    
                    _chosenGrassGridRadius = r;
                }

                return r;
            }
        }
        private static int _chosenGrassGridRadius = -1;

        internal static void run_freeze_check()
        {
            while(true)
            {
                if (cur_state != 1)
                    break;

                int last = System.Threading.Interlocked.CompareExchange(ref _lastDidSomething, 0, 0);
                int now = Environment.TickCount;

                if(unchecked(now - last) < 60000)
                {
                    System.Threading.Thread.Sleep(1000);
                    continue;
                }

                //NetScriptFramework.Main.CriticalException("Grass generation appears to have frozen! Restart the game.", false);
                GidFileGenerationTask.KillProcess();
                return;
            }
        }

        internal static void write_all_message(string msg)
        {
            NetScriptFramework.Main.WriteDebugMessage(msg);
            {
                var l = NetScriptFramework.Main.Log;
                if (l != null)
                    l.AppendLine(msg);
            }
            {
                byte[] buf = Encoding.UTF8.GetBytes(msg.Replace("%", "%%"));
                using (var alloc = Memory.Allocate(buf.Length + 2))
                {
                    Memory.WriteBytes(alloc.Address, buf);
                    Memory.WriteUInt8(alloc.Address + buf.Length, 0);

                    Memory.InvokeCdecl(GidFileGenerationTask.addr_PrintConsole, Memory.ReadPointer(GidFileGenerationTask.addr_ConsoleSingleton), alloc.Address);
                }
            }
        }

        internal static void apply()
        {
            var addr = Main.GameInfo.GetAddressOf(13148, 0x2B25 - 0x2220, 0, "E8");
            Memory.WriteNop(addr, 5);

            addr = Main.GameInfo.GetAddressOf(13190, 0xD40 - 0xC70, 0, "E8");
            Memory.WriteHook(new HookParameters()
            {
                Address = addr,
                IncludeLength = 5,
                ReplaceLength = 5,
                Before = ctx =>
                {
                    System.Threading.Interlocked.Increment(ref queued_grass_counter);
                }
            });

            addr = Main.GameInfo.GetAddressOf(13190, 0xD71 - 0xC70, 0, "48 8B 74 24 48");
            Memory.WriteHook(new HookParameters()
            {
                Address = addr,
                IncludeLength = 5,
                ReplaceLength = 5,
                Before = ctx =>
                {
                    System.Threading.Interlocked.Exchange(ref queued_grass_mode, 0);
                }
            });

            addr = Main.GameInfo.GetAddressOf(15202, 0xA0E - 0x890, 0, "8B 05");
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

            addr = Main.GameInfo.GetAddressOf(13138, 0, 0, "48 8B 51 38");
            Memory.WriteHook(new HookParameters()
            {
                Address = addr,
                IncludeLength = 0,
                ReplaceLength = 8,
                Before = ctx =>
                {
                    var cellPtr = Memory.ReadPointer(ctx.CX + 0x38);
                    Memory.InvokeCdecl(addr_AddGrassNow, Memory.ReadPointer(addr_GrassMgr), cellPtr, ctx.CX + 0x48);

                    var cell = MemoryObject.FromAddress<NetScriptFramework.SkyrimSE.TESObjectCELL>(cellPtr);
                    if(cell != null)
                    {
                        var ws = cell.WorldSpace;
                        if (ws != null)
                        {
                            string wsn = ws.EditorId;
                            int x = cell.CoordinateX;
                            int y = cell.CoordinateY;

                            cur_instance.WriteProgressFile(GidFileGenerationTask.KeyCell, wsn, x, y);
                        }
                    }

                    System.Threading.Interlocked.Decrement(ref queued_grass_counter);
                }
            });
            Memory.WriteUInt8(addr + 8, 0xC3, true);

            NetScriptFramework.SkyrimSE.Events.OnFrame.Register(e =>
            {
                if(cur_state == 1)
                {
                    if (System.Threading.Interlocked.CompareExchange(ref queued_grass_counter, 0, 0) != 0)
                        return;

                    if (System.Threading.Interlocked.Exchange(ref queued_grass_mode, 1) != 0)
                        return;

                    System.Threading.Interlocked.Exchange(ref _lastDidSomething, Environment.TickCount);

                    if (!cur_instance.RunOne())
                    {
                        if (GidFileGenerationTask.Crashed)
                        {
                            GidFileGenerationTask.KillProcess();
                            NetScriptFramework.SkyrimSE.Main.Instance.QuitGame = true;
                            return;
                        }

                        cur_state = 2;

                        write_all_message("Grass generation finished successfully!");
                        NetScriptFramework.Main.CriticalException("Grass generation finished successfully!", false);
                        NetScriptFramework.SkyrimSE.Main.Instance.QuitGame = true;
                    }
                }
            }, 200, 0);

            // Allow game to be alt-tabbed and make sure it's processing in the background correctly.
            addr = Main.GameInfo.GetAddressOf(35565, 0x216 - 0x1E0, 0, "74 14");
            Memory.WriteUInt8(addr, 0xEB, true);

            NetScriptFramework.SkyrimSE.Events.OnMainMenu.Register(e =>
            {
                Memory.WriteUInt8(Main.GameInfo.GetAddressOf(508798) + 8, 1); // Skyrim.ini [General] bAlwaysActive=1
                Memory.WriteUInt8(Main.GameInfo.GetAddressOf(501125) + 8, 0); // Skyrim.ini [Grass] bAllowLoadGrass=0
            });
        }

        internal static int DoneWS = 0;
        internal static int TotalWS = 0;

        internal static void KillProcess()
        {
            Crashed = true;

            var p = System.Diagnostics.Process.GetCurrentProcess();
            if (p != null)
                p.Kill();

            Memory.WritePointer(IntPtr.Zero, IntPtr.Zero);
        }

        internal static string ProgressFilePath
        {
            get
            {
                string n = _ovFilePath;
                if (n != null)
                    return n;

                // Dumb user mode for .txt.txt file name.
                try
                {
                    var fi = new System.IO.FileInfo(_progressFilePath);
                    if (fi.Exists)
                        _ovFilePath = _progressFilePath;
                    else
                    {
                        string fpath = _progressFilePath + ".txt";
                        fi = new System.IO.FileInfo(fpath);
                        if (fi.Exists)
                            _ovFilePath = fpath;
                    }
                }
                catch
                {

                }

                if (_ovFilePath == null)
                    _ovFilePath = _progressFilePath;

                return _ovFilePath;
            }
        }
        private static string _ovFilePath = null;
        private const string _progressFilePath = "PrecacheGrass.txt";

        private System.IO.StreamWriter FileStream;

        internal static int queued_grass_counter = 0;

        internal static int queued_grass_mode = 0;

        internal static int cur_state = 0;

        private static GidFileGenerationTask cur_instance;

        private HashSet<string> ProgressDone = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private readonly object ProgressLocker = new object();

        private bool IsResuming
        {
            get;
            set;
        }

        internal const string KeyWS = "ws";

        internal const string KeyCell = "cell";

        internal static int _lastDidSomething = -1;

        internal static void ShowMessageBox(string text)
        {
            byte[] buf = Encoding.UTF8.GetBytes(text);
            using (var alloc = NetScriptFramework.Memory.Allocate(buf.Length + 0x20))
            {
                NetScriptFramework.Memory.WritePointer(alloc.Address, alloc.Address + 0x10);
                NetScriptFramework.Memory.WriteBytes(alloc.Address + 0x10, buf);
                NetScriptFramework.Memory.WriteUInt8(alloc.Address + 0x10 + buf.Length, 0);

                NetScriptFramework.Memory.InvokeCdecl(addr_ShowMessageBox, 0, 0, 0, alloc.Address);
            }
        }

        private void Init()
        {
            var fi = new System.IO.FileInfo(ProgressFilePath);
            if(fi.Exists)
            {
                lock(ProgressLocker)
                {
                    using (var fs = new System.IO.StreamReader(ProgressFilePath))
                    {
                        string l;
                        while ((l = fs.ReadLine()) != null)
                        {
                            l = l.Trim();

                            if (string.IsNullOrEmpty(l))
                                continue;

                            this.ProgressDone.Add(l);
                        }
                    }
                }

                IsResuming = ProgressDone.Count != 0;
            }

            if(this.ProgressDone.Count == 0)
            {
                var dir = new System.IO.DirectoryInfo("Data/Grass");
                if (!dir.Exists)
                    dir.Create();

                var files = dir.GetFiles();
                foreach(var x in files)
                {
                    if (x.Name.EndsWith(".dgid", StringComparison.OrdinalIgnoreCase) || x.Name.EndsWith(".cgid", StringComparison.OrdinalIgnoreCase))
                        x.Delete();
                }
            }

            this.FileStream = new System.IO.StreamWriter(ProgressFilePath, true);

            string tx;
            if (IsResuming)
                tx = "Resuming grass cache generation now.\n\nThis will take a while!\n\nIf the game crashes you can run it again to resume.\n\nWhen all is finished the game will say.\n\nOpen console to see progress.";
            else
                tx = "Generating new grass cache now.\n\nThis will take a while!\n\nIf the game crashes you can run it again to resume.\n\nWhen all is finished the game will say.\n\nOpen console to see progress.";
            GidFileGenerationTask.ShowMessageBox(tx);
        }

        private void Free()
        {
            if(this.FileStream != null)
            {
                this.FileStream.Dispose();
                this.FileStream = null;
            }
        }

        internal bool HasDone(string key, string wsName, int x = int.MinValue, int y = int.MinValue)
        {
            string text = GenerateProgressKey(key, wsName, x, y);
            lock (this.ProgressLocker)
            {
                return this.ProgressDone.Contains(text);
            }
        }

        internal string GenerateProgressKey(string key, string wsName, int x = int.MinValue, int y = int.MinValue)
        {
            var bld = new StringBuilder(64);
            if (!string.IsNullOrEmpty(key))
                bld.Append(key);
            if (!string.IsNullOrEmpty(wsName))
            {
                if (bld.Length != 0)
                    bld.Append('_');
                bld.Append(wsName);
            }
            if (x != int.MinValue)
            {
                if (bld.Length != 0)
                    bld.Append('_');
                bld.Append(x.ToString());
            }
            if (y != int.MinValue)
            {
                if (bld.Length != 0)
                    bld.Append('_');
                bld.Append(y.ToString());
            }

            return bld.ToString();
        }

        internal void WriteProgressFile(string key, string wsName, int x = int.MinValue, int y = int.MinValue)
        {
            string text = GenerateProgressKey(key, wsName, x, y);
            if (string.IsNullOrEmpty(text))
                return;

            lock(this.ProgressLocker)
            {
                if (!this.ProgressDone.Add(text))
                    return;

                if (this.FileStream != null)
                {
                    this.FileStream.WriteLine(text);
                    this.FileStream.Flush();
                }
            }
        }

        private readonly List<GidFileWorldGenerateTask> WorldTodo = new List<GidFileWorldGenerateTask>();

        private void Begin()
        {
            var skip = (GrassControlPlugin._settingsInstance.SkipPregenerateWorldSpaces ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var skipSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var x in skip)
                skipSet.Add(x);

            var only = (GrassControlPlugin._settingsInstance.OnlyPregenerateWorldSpaces ?? "").Trim().Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var onlySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var x in only)
            {
                string sy = x.Trim();
                if(sy.Length != 0)
                    onlySet.Add(x);
            }

            var all = NetScriptFramework.SkyrimSE.DataHandler.Instance.GetAllFormsByType(NetScriptFramework.SkyrimSE.FormTypes.WorldSpace);
            foreach(var f in all)
            {
                var ws = (NetScriptFramework.SkyrimSE.TESWorldSpace)f;

                string name = ws.EditorId;

                if (onlySet.Count != 0)
                {
                    if (name == null || !onlySet.Contains(name))
                        continue;
                }
                else if ((name != null && skipSet.Contains(name)))
                    continue;

                TotalWS++;

                if (HasDone(KeyWS, name))
                {
                    DoneWS++;
                    continue;
                }
                
                var t = new GidFileWorldGenerateTask(this, ws, name);
                this.WorldTodo.Add(t);
            }
        }
        
        private void End()
        {
            if(this.FileStream != null)
            {
                this.FileStream.Dispose();
                this.FileStream = null;
            }

            var fi = new System.IO.FileInfo(ProgressFilePath);
            if (fi.Exists)
                fi.Delete();
        }
        
        private int _istate = 0;

        internal static bool Crashed = false;

        private bool RunOne()
        {
            if (_istate == 0)
            {
                _istate = 1;

                this.Init();

                this.Begin();
            }

            if (this.WorldTodo.Count != 0)
            {
                var t = this.WorldTodo[this.WorldTodo.Count - 1];

                if (!t.RunOne())
                {
                    if (GidFileGenerationTask.Crashed)
                        return false;

                    this.WorldTodo.RemoveAt(this.WorldTodo.Count - 1);
                    DoneWS++;

                    System.Threading.Interlocked.Exchange(ref queued_grass_mode, 0);
                }

                return true;
            }

            if (_istate == 1)
            {
                _istate = 2;

                this.End();

                this.Free();
            }

            return false;
        }
    }

    internal sealed class GidFileWorldGenerateTask
    {
        internal GidFileWorldGenerateTask(GidFileGenerationTask parent, NetScriptFramework.SkyrimSE.TESWorldSpace ws, string wsName)
        {
            this.Parent = parent;
            this.WorldSpace = ws;
            this.Name = wsName;
            _grid = new GidFileCellGenerateTask[129 * 129];

            ugrid = Memory.ReadInt32(GidFileGenerationTask.addr_uGrids + 8);
            uhalf = ugrid / 2;
        }

        internal readonly GidFileGenerationTask Parent;

        internal readonly NetScriptFramework.SkyrimSE.TESWorldSpace WorldSpace;

        internal readonly string Name;

        private readonly LinkedList<GidFileCellGenerateTask> CellTodo = new LinkedList<GidFileCellGenerateTask>();

        private GidFileCellGenerateTask[] _grid;

        internal int TotalCellDo;

        internal int DidCellDo;

        private void Init()
        {
            
        }

        private void Free()
        {
            _grid = null;
        }

        private void Begin()
        {
            int min = -64;
            int max = 64;

            this.TotalCellDo = 129 * 129;

            for(int x = min; x <= max; x++)
            {
                for(int y = min; y <= max; y++)
                {
                    if (this.Parent.HasDone(GidFileGenerationTask.KeyCell, this.Name, x, y))
                    {
                        this.DidCellDo++;
                        continue;
                    }

                    var cg = new GidFileCellGenerateTask(this, x, y);
                    cg.Node = this.CellTodo.AddLast(cg);
                    _grid[(x + 64) * 129 + (y + 64)] = cg;
                }
            }
        }

        private void End()
        {
            this.Parent.WriteProgressFile(GidFileGenerationTask.KeyWS, this.Name);
        }
        
        private int _istate = 0;

        private GidFileCellGenerateTask GetGrid(int x, int y)
        {
            x += 64;
            y += 64;
            if (x < 0 || x > 128 || y < 0 || y > 128)
                return null;
            return _grid[x * 129 + y];
        }

        private int uhalf;
        private int ugrid;

        private GidFileCellGenerateTask FindBestTodo()
        {
            GidFileCellGenerateTask best = null;
            int bestCount = 0;

            int ufull = ugrid * ugrid;

            foreach (var n in this.CellTodo)
            {
                int minx = n.X - uhalf;
                int maxx = n.X + uhalf;
                int miny = n.Y - uhalf;
                int maxy = n.Y + uhalf;
                int cur = 0;

                for(int x = minx; x <= maxx; x++)
                {
                    for(int y = miny; y <= maxy; y++)
                    {
                        var t = GetGrid(x, y);
                        if (t != null)
                            cur++;
                    }
                }

                if (cur == 0)
                    throw new InvalidOperationException();

                if(best == null || cur > bestCount)
                {
                    best = n;
                    bestCount = cur;

                    if (bestCount >= ufull)
                        break;
                }
            }

            return best;
        }

        internal bool RunOne()
        {
            if (_istate == 0)
            {
                _istate = 1;

                this.Init();

                this.Begin();
            }

            while (this.CellTodo.Count != 0)
            {
                var t = this.FindBestTodo();
                if (t == null)
                    throw new InvalidOperationException();

                if (!t.RunOne())
                {
                    if (GidFileGenerationTask.Crashed)
                        return false;

                    this.Remove(t);
                    this.Parent.WriteProgressFile(GidFileGenerationTask.KeyCell, this.Name, t.X, t.Y);
                    this.DidCellDo++;
                    continue;
                }

                int minx = t.X - uhalf;
                int maxx = t.X + uhalf;
                int miny = t.Y - uhalf;
                int maxy = t.Y + uhalf;
                for(int x = minx; x <= maxx; x++)
                {
                    for(int y = miny; y <= maxy; y++)
                    {
                        var tx = this.GetGrid(x, y);
                        if (tx != null)
                        {
                            this.Remove(tx);
                            this.Parent.WriteProgressFile(GidFileGenerationTask.KeyCell, this.Name, x, y);
                            this.DidCellDo++;
                        }
                    }
                }

                return true;
            }

            if (_istate == 1)
            {
                _istate = 2;

                this.End();

                this.Free();
            }

            return false;
        }

        internal void Remove(GidFileCellGenerateTask t)
        {
            if (t.Node == null)
                return;

            t.Node.List.Remove(t.Node);
            t.Node = null;

            int x = t.X + 64;
            int y = t.Y + 64;
            int ix = x * 129 + y;
            if (_grid[ix] == t)
                _grid[ix] = null;
            else
                throw new InvalidOperationException();
        }
    }

    internal sealed class GidFileCellGenerateTask
    {
        internal GidFileCellGenerateTask(GidFileWorldGenerateTask parent, int x, int y)
        {
            this.Parent = parent;
            this.X = x;
            this.Y = y;
        }

        internal readonly GidFileWorldGenerateTask Parent;

        internal readonly int X;

        internal readonly int Y;

        internal LinkedListNode<GidFileCellGenerateTask> Node;

        private NetScriptFramework.SkyrimSE.TESObjectCELL Cell;

        private void Init()
        {

        }

        private void Free()
        {

        }

        private bool Begin()
        {
            IntPtr cellPtr;

            Memory.InvokeCdecl(GidFileGenerationTask.addr_EnterLock);
            try
            {
                cellPtr = Memory.InvokeCdecl(GidFileGenerationTask.addr_WSLoadCellByCoordinates, this.Parent.WorldSpace.Cast<NetScriptFramework.SkyrimSE.TESWorldSpace>(), this.X, this.Y);
            }
            catch
            {
                GidFileGenerationTask.KillProcess();
                return false;
            }
            finally
            {
                Memory.InvokeCdecl(GidFileGenerationTask.addr_ExitLock);
            }

            if (cellPtr == IntPtr.Zero)
                return false;

            this.Cell = MemoryObject.FromAddress<NetScriptFramework.SkyrimSE.TESObjectCELL>(cellPtr);
            if (this.Cell == null)
                throw new NullReferenceException("this.Cell");
            
            return true;
        }

        private void End()
        {
            this.Cell = null;
        }
        
        private bool Process()
        {
            if (this.Cell == null)
                return false;

            double pct = 0.0;
            if (this.Parent.TotalCellDo > 0)
                pct = Math.Max(0.0, Math.Min((double)this.Parent.DidCellDo / (double)this.Parent.TotalCellDo, 1.0)) * 100.0;

            string msg = "Generating grass for " + this.Parent.Name + "(" + this.X + ", " + this.Y + ") " + pct.ToString("0.##") + " pct, world " + (GidFileGenerationTask.DoneWS + 1) + " out of " + GidFileGenerationTask.TotalWS;
            GidFileGenerationTask.write_all_message(msg);

            using (var alloc = Memory.Allocate(0x20))
            {
                Memory.WriteZero(alloc.Address, 0x20);

                Memory.WriteFloat(alloc.Address, this.Cell.CoordinateX * 4096.0f + 2048.0f);
                Memory.WriteFloat(alloc.Address + 4, this.Cell.CoordinateY * 4096.0f + 2048.0f);
                Memory.WriteFloat(alloc.Address + 8, 0.0f);

                try
                {
                    Memory.InvokeCdecl(GidFileGenerationTask.addr_SetPlrTo, NetScriptFramework.SkyrimSE.PlayerCharacter.Instance.Cast<NetScriptFramework.SkyrimSE.PlayerCharacter>(), alloc.Address, alloc.Address + 0x10, this.Cell.Cast<NetScriptFramework.SkyrimSE.TESObjectCELL>(), 0);
                }
                catch
                {
                    GidFileGenerationTask.KillProcess();
                    return false;
                }
            }

            return true;
        }

        internal bool RunOne()
        {
            this.Init();

            bool did = this.Begin();

            if (did && !this.Process())
                did = false;

            this.End();

            this.Free();

            return did;
        }
    }
}
