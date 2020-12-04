using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework;

namespace GamePlayTweaks.Mods
{
    class CumulativeTrainingTimesPerLevel : Mod
    {
        public CumulativeTrainingTimesPerLevel()
        {
            settings.OverwriteTimesPerLevel = this.CreateSettingInt("OverwriteTimesPerLevel", -1);
        }

        internal override string Description
        {
            get
            {
                return "Allow unused training times per level to accumulate. For example if you can train 5 times per level and you didn't train at all the last 3 levels you can now train 15 times at trainer.";
            }
        }

        private static class settings
        {
            internal static SettingValue<long> OverwriteTimesPerLevel;
        }

        private int OverwriteTimes = -1;
        private IntPtr GameSettingPtr;

        private int CalculateMaxTrainingTimes()
        {
            int mult = 1;
            {
                var plr = NetScriptFramework.SkyrimSE.PlayerCharacter.Instance;
                int lvl = 0;
                if (plr != null)
                    lvl = Math.Max(0, plr.Level - 1);
                mult = lvl;
            }

            int times;
            if (this.OverwriteTimes >= 0)
                times = this.OverwriteTimes;
            else
                times = NetScriptFramework.Memory.ReadInt32(GameSettingPtr);
            return times * mult;
        }

        internal override void Apply()
        {
            this.OverwriteTimes = (int)settings.OverwriteTimesPerLevel.Value;
            if (this.OverwriteTimes < 0)
                this.GameSettingPtr = NetScriptFramework.Main.GameInfo.GetAddressOf(505390, 8);

            Memory.WriteHook(new HookParameters()
            {
                Address = NetScriptFramework.Main.GameInfo.GetAddressOf(51793, 0x34, 0, "8B 15"),
                IncludeLength = 0,
                ReplaceLength = 6,
                Before = ctx =>
                {
                    ctx.DX = new IntPtr((long)CalculateMaxTrainingTimes());
                }
            });

            Memory.WriteHook(new HookParameters()
            {
                Address = NetScriptFramework.Main.GameInfo.GetAddressOf(51794, 0x1D6, 0, "8B 05"),
                IncludeLength = 0,
                ReplaceLength = 6,
                Before = ctx =>
                {
                    ctx.AX = new IntPtr((long)CalculateMaxTrainingTimes());
                }
            });

            // Prevent game from resetting trained times.
            Memory.WriteNop(NetScriptFramework.Main.GameInfo.GetAddressOf(40560, 0x17D, 0, "C7 80 30 09 00 00 00 00 00 00"), 10);
        }
    }
}
