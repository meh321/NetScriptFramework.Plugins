using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NetScriptFramework;

namespace DebugConsole
{
    public partial class GUI : Form
    {
        public GUI()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Writes the line to gui.
        /// </summary>
        /// <param name="text">The text.</param>
        public static void WriteLine(string text)
        {
            lock (Locker)
            {
                var now = _sw.ElapsedTicks;
                MessageQueue.Add(new Tuple<string, long>(text, now));

                var time = _sw_start + new TimeSpan(now * 1000 / System.Diagnostics.Stopwatch.Frequency * TimeSpan.TicksPerMillisecond);
                string fmt = "[" + string.Format("{0:00}:{1:00}:{2:00}.{3:000}", time.Hour, time.Minute, time.Second, time.Millisecond) + "] " + (text ?? string.Empty);
                fmt = Environment.NewLine + fmt;

                if (_gui_debug_file != null)
                {
                    _gui_debug_file.Write(fmt);
                    _gui_debug_file.Flush();
                }
            }
        }
        
        /// <summary>
        /// Clears the text from gui.
        /// </summary>
        public static void Clear()
        {
            lock (Locker)
            {
                CloseFile();

                MessageQueue.Add(new Tuple<string, long>("", -1));
            }
        }

        /// <summary>
        /// Closes the file.
        /// </summary>
        public static void CloseFile()
        {
            lock (Locker)
            {
                if (_gui_debug_file != null)
                {
                    _gui_debug_file.Dispose();
                    _gui_debug_file = null;
                }
            }
        }
        
        internal static void _Start()
        {
            System.Threading.Thread t = new System.Threading.Thread(_Run);
            t.SetApartmentState(System.Threading.ApartmentState.STA);
            t.IsBackground = true;
            t.Start(new GUI());

            System.Threading.Thread.Sleep(50);
        }

        private static void _Run(object arg)
        {
            Application.Run(arg as Form);
        }

        internal static readonly object Locker = new object();
        private static readonly System.Diagnostics.Stopwatch _sw = new System.Diagnostics.Stopwatch();
        private static DateTime _sw_start;
        private static List<Tuple<string, long>> MessageQueue = new List<Tuple<string, long>>(QueueInitialCapacity);
        private readonly Queue<int> Messages = new Queue<int>(1024);
        private const int QueueInitialCapacity = 8;
        private int TotalLength = 0;
        private static System.IO.StreamWriter _gui_debug_file = null;

        internal static long _shouldStop
        {
            get
            {
                return System.Threading.Interlocked.Read(ref _ss);
            }
            set
            {
                System.Threading.Interlocked.Exchange(ref _ss, value);
            }
        }
        private static long _ss = 0;

        private int _minimize_once = 0;

        private void timer1_Tick(object sender, EventArgs e)
        {
            List<Tuple<string, long>> mq = null;
            lock (Locker)
            {
                if (MessageQueue != null)
                {
                    mq = MessageQueue;
                    MessageQueue = new List<Tuple<string, long>>(QueueInitialCapacity);
                }
            }

            if (_minimize_once >= 0)
            {
                _minimize_once = -1;
                if (NetScriptFramework.PluginManager.GetPlugin("debugger") != null)
                    this.WindowState = FormWindowState.Minimized;
            }

            if (mq != null)
            {
                var str = new StringBuilder(512);
                Queue<int> add = new Queue<int>(mq.Count);
                int maxCapacity = 1000000;
                int remove = 0;
                int remove2 = 0;

                for (int i = 0; i < mq.Count; i++)
                {
                    var t = mq[i];
                    if (t.Item2 < 0)
                    {
                        str.Clear();
                        add.Clear();
                        this.TotalLength = 0;
                        remove = this.textBox2.Text.Length;
                        this.Messages.Clear();
                        continue;
                    }

                    var time = _sw_start + new TimeSpan(t.Item2 * 1000 / System.Diagnostics.Stopwatch.Frequency * TimeSpan.TicksPerMillisecond);
                    string fmt = "[" + string.Format("{0:00}:{1:00}:{2:00}.{3:000}", time.Hour, time.Minute, time.Second, time.Millisecond) + "] " + (t.Item1 ?? string.Empty);
                    fmt = Environment.NewLine + fmt;
                    int len = fmt.Length;
                    add.Enqueue(len);
                    str.Append(fmt);
                    this.TotalLength += len;
                }

                while (this.TotalLength > maxCapacity)
                {
                    if (this.Messages.Count != 0)
                    {
                        int len = this.Messages.Dequeue();
                        this.TotalLength -= len;
                        remove += len;
                    }
                    else
                    {
                        int len = add.Dequeue();
                        this.TotalLength -= len;
                        remove2 += len;
                    }
                }

                //_gui_debug_file.Write(str.ToString());
                //_gui_debug_file.Flush();

                if (remove2 != 0)
                    str.Remove(0, remove2);

                string orig = this.textBox2.Text;

                if (remove != 0)
                {
                    if (remove == orig.Length)
                        orig = "";
                    else
                        orig = orig.Remove(0, remove);

                    orig = orig + str.ToString();
                    this.textBox2.Text = orig;
                    this.textBox2.Select(orig.Length, 0);
                    //this.textBox2.SelectedText = "";
                }
                else
                    this.textBox2.AppendText(str.ToString());
            }

            if (_shouldStop > 0)
                this.Close();
        }

        private void GUI_Load(object sender, EventArgs e)
        {
            _gui_debug_file = new System.IO.StreamWriter("SkyrimSE_gui.txt", false);
            _sw_start = DateTime.Now;
            _sw.Start();
            timer1.Start();
        }

        private void GUI_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_gui_debug_file != null)
            {
                _gui_debug_file.Dispose();
                _gui_debug_file = null;
            }
        }
        
        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                string tx = this.textBox1.Text.TrimStart();
                if (string.IsNullOrEmpty(tx))
                    return;

                int ix = tx.IndexOf(' ');
                string cmd = "";
                string arg = "";
                if (ix >= 0)
                {
                    cmd = tx.Substring(0, ix).Trim();
                    arg = tx.Substring(ix + 1).Trim();
                }
                else
                    cmd = tx.Trim();

                this.textBox1.SelectAll();
                DebugConsolePlugin.ExecuteCommand(cmd, arg);
            }
        }
    }
}
