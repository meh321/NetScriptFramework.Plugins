using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamePlayTweaks.Mods
{
    class FasterSleepwaitMenu : Mod
    {
        public FasterSleepwaitMenu() : base()
        {
            settings.SecondsPerHour = this.CreateSettingFloat("SecondsPerHour", 0.5, "How many seconds does it take to rest or wait 1 hour, default vanilla value is 1");
        }

        internal override string Description
        {
            get
            {
                return "Make the wait/rest menu pass hours faster.";
            }
        }

        private static class settings
        {
            internal static SettingValue<double> SecondsPerHour;
        }

        internal override void Apply()
        {
            NetScriptFramework.Memory.WriteHook(new NetScriptFramework.HookParameters()
            {
                Address = NetScriptFramework.Main.GameInfo.GetAddressOf(51614, 0xC7E - 0xAB0, 0, "0F 2F 05 ?? ?? ?? ?? 0F 82 90 01 00 00"),
                IncludeLength = 0,
                ReplaceLength = 13,
                Before = ctx =>
                {
                    if (ctx.XMM0f < settings.SecondsPerHour.Value)
                        ctx.IP = ctx.IP + 0x190;
                }
            });
        }
    }
}
