using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework.SkyrimSE;

namespace GamePlayTweaks.Tools
{
    internal static class BlockEquipHook
    {
        private static bool Installed = false;

        private static readonly List<Func<Actor, TESForm, string>> Funcs = new List<Func<Actor, TESForm, string>>();
        private static IntPtr addr_ShowHudMessage;

        internal static void Install(Func<Actor, TESForm, string> func)
        {
            if(!Installed)
            {
                Installed = true;
                addr_ShowHudMessage = NetScriptFramework.Main.GameInfo.GetAddressOf(52050);
                var addr = NetScriptFramework.Main.GameInfo.GetAddressOf(36976, 0, 0, "40 55 53 56 57");
                if (!NetScriptFramework.Memory.VerifyBytes(addr + (0xC4D6 - 0xBBD0), "C3"))
                    throw new ArgumentException("Unable to verify bytes for equip hook!");

                NetScriptFramework.Memory.WriteHook(new NetScriptFramework.HookParameters()
                {
                    Address = addr,
                    IncludeLength = 5,
                    ReplaceLength = 5,
                    Before = ctx =>
                    {
                        try
                        {
                            var actor = NetScriptFramework.MemoryObject.FromAddress<Actor>(ctx.CX);
                            var obj = NetScriptFramework.MemoryObject.FromAddress<TESForm>(ctx.DX);

                            if(actor != null && obj != null)
                            {
                                foreach(var f in Funcs)
                                {
                                    string error = f(actor, obj);
                                    if(error != null)
                                    {
                                        // BBD5
                                        // C4D6
                                        ctx.Skip();
                                        ctx.IP = ctx.IP + (0xC4D6 - 0xBBD5);

                                        if(error.Length != 0)
                                        {
                                            byte[] buf = Encoding.UTF8.GetBytes(error);
                                            using (var alloc = NetScriptFramework.Memory.Allocate(buf.Length + 0x10))
                                            {
                                                NetScriptFramework.Memory.WriteBytes(alloc.Address, buf);
                                                NetScriptFramework.Memory.WriteUInt8(alloc.Address + buf.Length, 0);

                                                NetScriptFramework.Memory.InvokeCdecl(addr_ShowHudMessage, alloc.Address, 0, 1);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {

                        }
                    },
                });
            }

            Funcs.Add(func);
        }
    }
}
