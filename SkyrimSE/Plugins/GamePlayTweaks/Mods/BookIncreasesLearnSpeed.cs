using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework;
using NetScriptFramework.SkyrimSE;

namespace GamePlayTweaks.Mods
{
    class BookIncreasesLearnSpeed : Mod
    {
        public BookIncreasesLearnSpeed()
        {
            settings.Base = this.CreateSettingFloat("Base", 100);
            settings.AmountFlat = this.CreateSettingFloat("AmountFlat", 1);
            settings.AmountExponent = this.CreateSettingFloat("AmountExponent", 1);
            settings.Notify = this.CreateSettingBool("Notify", true);
            settings.NotifyMessage = this.CreateSettingString("NotifyMessage", "Your learning speed is now %s.");
            settings.IncludeNotes = this.CreateSettingBool("IncludeNotes", false);
            settings.IncludeJournals = this.CreateSettingBool("IncludeJournals", false);
            settings.IncludeNormalBooks = this.CreateSettingBool("IncludeNormalBooks", true);
            settings.IncludeSkillBooks = this.CreateSettingBool("IncludeSkillBooks", true);
            settings.IncludeSpellBooks = this.CreateSettingBool("IncludeSpellBooks", false);
            settings.IncludeSpecialBooks = this.CreateSettingBool("IncludeSpecialBooks", false);
        }

        internal override string Description
        {
            get
            {
                return "Every unread book you read will increase learning speed of all skills. The multiplier is <Base + AmountFlat*Count + (AmountExponent^Count - 1) * 100>%. Learning speed starts at <Base>% speed (if you have not read any books yet). Base can be lower than 100 if you want. If <Notify> is set then display message when learning speed changes due to having read a book.";
            }
        }

        private static class settings
        {
            internal static SettingValue<double> Base;
            internal static SettingValue<double> AmountFlat;
            internal static SettingValue<double> AmountExponent;
            internal static SettingValue<bool> Notify;
            internal static SettingValue<string> NotifyMessage;
            internal static SettingValue<bool> IncludeNotes;
            internal static SettingValue<bool> IncludeJournals;
            internal static SettingValue<bool> IncludeNormalBooks;
            internal static SettingValue<bool> IncludeSkillBooks;
            internal static SettingValue<bool> IncludeSpellBooks;
            internal static SettingValue<bool> IncludeSpecialBooks;
        }

        private List<Tools.BookTracker.BookTypes> types;
        private int _last_update_counter;

        internal override void Apply()
        {
            this.types = new List<Tools.BookTracker.BookTypes>(8);
            if (settings.IncludeNotes.Value)
                this.types.Add(Tools.BookTracker.BookTypes.Note);
            if (settings.IncludeJournals.Value)
                this.types.Add(Tools.BookTracker.BookTypes.Journal);
            if (settings.IncludeNormalBooks.Value)
                this.types.Add(Tools.BookTracker.BookTypes.NormalBook);
            if (settings.IncludeSkillBooks.Value)
                this.types.Add(Tools.BookTracker.BookTypes.SkillBook);
            if (settings.IncludeSpellBooks.Value)
                this.types.Add(Tools.BookTracker.BookTypes.SpellBook);
            if (settings.IncludeSpecialBooks.Value)
                this.types.Add(Tools.BookTracker.BookTypes.Special);
            
            Tools.BookTracker.EnsureExists();

            Events.OnFrame.Register(e =>
            {
                int nc = Tools.BookTracker.Instance.UpdateCounter;
                if(nc != _last_update_counter)
                {
                    this.update();
                    this._last_update_counter = nc;
                }
            }, 50);

            Events.OnGainSkillXP.Register(e =>
            {
                if (e.IsFromTrainingOrBook)
                    return;

                double newAmount = e.Amount * this.CalculateMultiplier(this.LastReadCount);
                e.Amount = (float)Math.Max(0.0, newAmount);
            }, 50);
        }

        private double CalculateMultiplier(int count)
        {
            double extra = settings.AmountFlat.Value * count * 0.01;
            if (settings.AmountExponent.Value != 1.0 && settings.AmountExponent.Value > 0.0 && count > 0)
                extra += Math.Pow(settings.AmountExponent.Value, count) - 1.0;
            return settings.Base.Value * 0.01 + extra;
        }

        private int LastReadCount = 0;
        
        private void SetReadNow(int count)
        {
            if (this.LastReadCount == count)
                return;

            if (settings.Notify.Value/* && count > this.LastReadCount*/)
            {
                string nmsg = settings.NotifyMessage.Value.Replace("%s", (this.CalculateMultiplier(count) * 100.0).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "%").Trim();
                if(!string.IsNullOrEmpty(nmsg))
                    MenuManager.ShowHUDMessage(nmsg, null, true);
            }

            this.LastReadCount = count;
        }

        private void update()
        {
            int did = 0;
            foreach(var t in this.types)
                did += Tools.BookTracker.Instance.GetReadCount(t);
            
            this.SetReadNow(did);
        }
    }
}
