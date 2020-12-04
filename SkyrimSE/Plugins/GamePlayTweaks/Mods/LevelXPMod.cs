using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework;

namespace GamePlayTweaks.Mods
{
    /*class LevelXPMod : Mod
    {
        public LevelXPMod() : base()
        {
            settings.GainedXPMessage = this.CreateSettingString("GainedXPMessage", "You receive %s experience.");
            settings.XPRate = this.CreateSettingForm<NetScriptFramework.SkyrimSE.TESGlobal>("XPRate", "", "You can enter the form id and form file name here to set optional global that controls the XP rate of player. If the global is float then it's multiplier, if it's integer type then it's per cent.");
        }

        internal override string Description
        {
            get
            {
                return "";
            }
        }

        private static class settings
        {
            internal static SettingValue<string> GainedXPMessage;
            internal static SettingValue<NetScriptFramework.SkyrimSE.TESGlobal> XPRate;
        }

        private IntPtr Func_ShowHudMessage;

        private void GiveLevelXP(double amount)
        {
            if (amount <= 0.0)
                return;

            var plr = NetScriptFramework.SkyrimSE.PlayerCharacter.Instance;
            if (plr == null)
                return;

            var plrSkills = plr.Skills;
            if (plrSkills == null)
                return;

            var skills = plrSkills.Skills;
            if (skills == null)
                return;

            {
                var rateGlobal = settings.XPRate.Value;
                if(rateGlobal != null)
                {
                    amount *= rateGlobal.FloatValue;
                    if (amount <= 0.0)
                        return;
                }
            }

            float prevAmount = skills.PlayerLevelXp;
            float threshold = skills.PlayerLevelThreshold;
            long prevAmount_i = (long)prevAmount;
            float newAmount = prevAmount + (float)amount;
            long newAmount_i = (long)newAmount;
            skills.PlayerLevelXp = newAmount;

            long diff_i = newAmount_i - prevAmount_i;
            if (diff_i == 0 && prevAmount < threshold && newAmount >= threshold)
                diff_i = 1;

            // No need to show if gained amount is too small.
            if (diff_i == 0)
                return;

            string msg = settings.GainedXPMessage.Value.Replace("%s", diff_i.ToString());
            if(msg.Length != 0)
            {
                var buf = Encoding.UTF8.GetBytes(msg);
                using (var alloc = Memory.Allocate(buf.Length + 4))
                {
                    Memory.WriteBytes(alloc.Address, buf);
                    Memory.WriteUInt8(alloc.Address + buf.Length, 0);

                    Memory.InvokeCdecl(this.Func_ShowHudMessage, 20, alloc.Address, 0, 0);
                }
            }
        }

        private void OnGiveLevelXP(NetScriptFramework.SkyrimSE.GainLevelXPEventArgs e)
        {
            e.Amount = 0.0f;
        }

        internal override void Apply()
        {
            this.Func_ShowHudMessage = NetScriptFramework.Main.GameInfo.GetAddressOf(50751);

            NetScriptFramework.SkyrimSE.Events.OnGainLevelXP.Register(this.OnGiveLevelXP, 1000);
        }
    }*/
}
