using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework;

namespace GamePlayTweaks.Mods
{
    class DisableAutoFullHeal : Mod
    {
        public DisableAutoFullHeal()
        {
            IntParameters["DisableOnLevelup"] = 7;
            IntParameters["DisableOnRest"] = 7;
        }

        internal override string Description
        {
            get
            {
                return "Game will force restore HP/Magicka/Stamina in certain situations like when you level up or rest in a bed. This option will allow you to skip that behavior and you don't get auto-healed and must heal on your own. Set 7 to disable restoring health, magicka and stamina. Set 0 to allow restore everything. Flags: Health=1, Magicka=2, Stamina=4";
            }
        }

        internal override void Apply()
        {
            int onLevelup = (int)(this.IntParameters["DisableOnLevelup"] & 7);
            int onRest = (int)(this.IntParameters["DisableOnRest"] & 7);

            if(onLevelup != 0)
            {
                int mask = onLevelup;
                ulong vid = 40560; // 1406E6930
                if ((mask & 1) != 0)
                    Memory.WriteNop(Main.GameInfo.GetAddressOf(vid, 0x11B, 0, "E8"), 5);
                if ((mask & 2) != 0)
                    Memory.WriteNop(Main.GameInfo.GetAddressOf(vid, 0x146, 0, "E8"), 5);
                if ((mask & 4) != 0)
                    Memory.WriteNop(Main.GameInfo.GetAddressOf(vid, 0x171, 0, "E8"), 5);
            }

            if(onRest != 0)
            {
                int mask = onRest;
                ulong vid = 39346; // 14069ADE0
                if ((mask & 1) != 0)
                    Memory.WriteNop(Main.GameInfo.GetAddressOf(vid, 0x9A, 0, "E8"), 5);
            }
        }
    }
}
