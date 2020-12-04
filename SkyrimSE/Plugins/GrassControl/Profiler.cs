using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrassControl
{
    internal sealed class Profiler
    {
        internal Profiler()
        {
            this.Timer = new System.Diagnostics.Stopwatch();
            this.Timer.Start();

            this.Divide = System.Diagnostics.Stopwatch.Frequency / 1000;
        }

        private readonly List<long> Times = new List<long>();

        private readonly List<KeyValuePair<int, long>> Progress = new List<KeyValuePair<int, long>>();

        private readonly object Locker = new object();

        private readonly System.Diagnostics.Stopwatch Timer;

        private readonly long Divide;

        internal void Begin()
        {
            int who = NetScriptFramework.Memory.GetCurrentNativeThreadId();
            long when = this.Timer.ElapsedTicks;

            lock(this.Locker)
            {
                for(int i = 0; i < this.Progress.Count; i++)
                {
                    var t = this.Progress[i];
                    if(t.Key == who)
                    {
                        this.Progress[i] = new KeyValuePair<int, long>(who, when);
                        return;
                    }
                }

                this.Progress.Add(new KeyValuePair<int, long>(who, when));
            }
        }

        internal void End()
        {
            long when = this.Timer.ElapsedTicks;
            int who = NetScriptFramework.Memory.GetCurrentNativeThreadId();

            lock(this.Locker)
            {
                for(int i = 0; i < this.Progress.Count; i++)
                {
                    var t = this.Progress[i];
                    if(t.Key == who)
                    {
                        long diff = when - t.Value;
                        this.Progress.RemoveAt(i);

                        this.Times.Add(diff);
                        if (this.Times.Count > 1000)
                            this.Times.RemoveAt(0);
                        return;
                    }
                }
            }
        }

        internal void Report()
        {
            List<double> all = new List<double>();

            lock(this.Locker)
            {
                foreach(var diff in this.Times)
                {
                    double w = diff;
                    w /= (double)this.Divide;
                    all.Add(w);
                }
            }

            string message;
            if(all.Count == 0)
                message = "No samples have been gathered yet.";
            else
            {
                if (all.Count > 1)
                    all.Sort();

                double sum = all.Sum();
                double avg = sum / (double)all.Count;
                double med = all[all.Count / 2];
                double min = all[0];
                double max = all[all.Count - 1];

                Func<double, string> fmt = v =>
                {
                    return v.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
                };

                string[] things = new string[]
                {
                    "Samples = " + all.Count,
                    "Average = " + fmt(avg),
                    "Median = " + fmt(med),
                    "Min = " + fmt(min),
                    "Max = " + fmt(max),
                };
                message = string.Join("; ", things);
            }

            NetScriptFramework.SkyrimSE.MenuManager.ShowHUDMessage(message, null, true);
            NetScriptFramework.Main.WriteDebugMessage(message);
            var l = NetScriptFramework.Main.Log;
            if (l != null)
                l.AppendLine(message);
        }
    }
}
