using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework;
using NetScriptFramework.SkyrimSE;

namespace ActorLimitPlugin
{
    public class ActorLimitPlugin : Plugin
    {
        public override string Key
        {
            get
            {
                return "actor_limit";
            }
        }

        public override string Name
        {
            get
            {
                return "Actor Limit Plugin";
            }
        }

        public override int Version
        {
            get
            {
                return 2;
            }
        }

        internal Settings Settings;

        protected override bool Initialize(bool loadedAny)
        {
            this.Settings = new Settings();
            this.Settings.Load();

            // Maximum actor movement updates.
            if(this.Settings.MoverLimit != 128)
            {
                var funcAssign = NetScriptFramework.Main.GameInfo.GetAddressOf(40296, 0x90, 0, "BE");
                var funcComp = NetScriptFramework.Main.GameInfo.GetAddressOf(40296, 0x9C, 0, "3D");

                int defaultCap = 128;
                int prev = NetScriptFramework.Memory.ReadInt32(funcAssign + 1);
                int prev2 = NetScriptFramework.Memory.ReadInt32(funcComp + 1);
                if (prev != 0x80 || prev2 != 0x80)
                {
                    if (prev == prev2)
                        NetScriptFramework.Main.Log.AppendLine("ActorLimitPlugin: Warning! Another mod may have already changed the mover limit. Expected to find " + defaultCap + " but it's " + prev + " instead. Skipping.");
                    else
                        NetScriptFramework.Main.Log.AppendLine("ActorLimitPlugin: Warning! Another mod may have already incorrectly changed the mover limit. Expected to find " + defaultCap + " but it's " + prev + " / " + prev2 + " instead. Skipping.");
                    return true;
                }

                Memory.WriteInt32(funcAssign + 1, this.Settings.MoverLimit, true);
                Memory.WriteInt32(funcComp + 1, this.Settings.MoverLimit, true);
            }

            // Replace static buffer.
            if(this.Settings.ReplaceStaticBuffer)
            {
                var alloc = Memory.Allocate(0x2000);
                alloc.Pin();

                Memory.WriteZero(alloc.Address, alloc.Size);

                var addr = NetScriptFramework.Main.GameInfo.GetAddressOf(40435, 0x21, 0, "48 8D 0D");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 7,
                    Before = ctx =>
                    {
                        ctx.CX = alloc.Address;
                    },
                });

                addr = NetScriptFramework.Main.GameInfo.GetAddressOf(40435, 0x31, 0, "48 C1 E3 09");
                Memory.WriteUInt8(addr + 3, 12, true);

                addr = NetScriptFramework.Main.GameInfo.GetAddressOf(40277, 0x26, 0, "48 8D 0D");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 7,
                    Before = ctx =>
                    {
                        ctx.CX = alloc.Address;
                    }
                });

                addr = NetScriptFramework.Main.GameInfo.GetAddressOf(40277, 0x22, 0, "48 C1 E3 09");
                Memory.WriteUInt8(addr + 3, 12, true);

                addr = NetScriptFramework.Main.GameInfo.GetAddressOf(40427, 0x1C1, 0, "48 8D 0D");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 7,
                    Before = ctx =>
                    {
                        ctx.CX = alloc.Address;
                    }
                });

                addr = NetScriptFramework.Main.GameInfo.GetAddressOf(40427, 0x1BD, 0, "48 C1 E7 09");
                Memory.WriteUInt8(addr + 3, 12, true);

                addr = NetScriptFramework.Main.GameInfo.GetAddressOf(40434, 0x61, 0, "48 8D 05");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 7,
                    Before = ctx =>
                    {
                        ctx.AX = alloc.Address;
                    }
                });

                addr = NetScriptFramework.Main.GameInfo.GetAddressOf(40434, 0x70, 0, "48 C1 E3 09");
                Memory.WriteUInt8(addr + 3, 12, true);

                addr = NetScriptFramework.Main.GameInfo.GetAddressOf(40434, 0x41, 0, "83 FF 40");
                var addr2 = NetScriptFramework.Main.GameInfo.GetAddressOf(40434, 0x137, 0, "48 8B 6C 24 60");
                Memory.WriteHook(new HookParameters()
                {
                    Address = addr,
                    IncludeLength = 0,
                    ReplaceLength = 9,
                    Before = ctx =>
                    {
                        if(ctx.DI.ToUInt32() >= 512)
                            ctx.IP = addr2;
                    }
                });

                /*Memory.WriteHook(new HookParameters()
                {
                    Address = new IntPtr(0x1406D2E30).FromBase(),
                    IncludeLength = 5,
                    ReplaceLength = 5,
                    Before = ctx =>
                    {
                        NetScriptFramework.Main.WriteDebugMessage("DEBUG FUNC CALLED!");
                    }
                });*/
            }

            // Maximum morph updates limit.
            if(this.Settings.MorphLimit != 0xA)
            {
                int want = !this.Settings.ReplaceStaticBuffer ? Math.Min(62, this.Settings.MorphLimit) : Math.Min(510, this.Settings.MorphLimit);
                int defaultCap = 0xA;
                var cap1 = NetScriptFramework.Main.GameInfo.GetAddressOf(40427, 0x1D9, 0, "BE");
                int read1 = Memory.ReadInt32(cap1 + 1);
                var cap2 = NetScriptFramework.Main.GameInfo.GetAddressOf(507579, 8);
                int read2 = Memory.ReadInt32(cap2);
                if (read1 != defaultCap || read2 != defaultCap)
                {
                    NetScriptFramework.Main.Log.AppendLine("ActorLimitPlugin: Warning! Another mod may have already changed the morph limit. Expected to find " + defaultCap + " but it's " + read1 + " / " + read2 + " instead. Skipping.");
                }
                else
                {
                    Memory.WriteInt32(cap1 + 1, want, true);
                    Memory.WriteInt32(cap2, want);
                }
            }

            return true;
        }
    }
}
