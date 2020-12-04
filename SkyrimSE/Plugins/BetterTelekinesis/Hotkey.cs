using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework.Tools;

namespace BetterTelekinesis
{
    /// <summary>
    /// Base hotkey.
    /// </summary>
    public abstract class HotkeyBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HotkeyBase"/> class.
        /// </summary>
        /// <param name="keys">The keys.</param>
        protected HotkeyBase(params VirtualKeys[] keys)
        {
            if (keys == null || keys.Length == 0)
                Keys = EmptyKeys;
            else
                Keys = keys.ToArray();
        }

        /// <summary>
        /// The keys.
        /// </summary>
        public readonly IReadOnlyList<VirtualKeys> Keys;

        /// <summary>
        /// The empty keys.
        /// </summary>
        private static readonly VirtualKeys[] EmptyKeys = new VirtualKeys[0];

        /// <summary>
        /// The last update.
        /// </summary>
        private int _lastUpdate;

        /// <summary>
        /// The last state.
        /// </summary>
        private bool _lastState;

        /// <summary>
        /// The register state.
        /// </summary>
        private bool _regState;

        /// <summary>
        /// Determines whether this instance is pressed as of last update.
        /// </summary>
        /// <returns></returns>
        public bool IsPressed()
        {
            return this._lastState;
        }

        /// <summary>
        /// Updates this instance. It will recalculate if the key is pressed and send any events.
        /// </summary>
        public void Update()
        {
            int now = Environment.TickCount;
            if (_lastUpdate == now)
                return;
            this._lastUpdate = now;

            bool yes = true;
            if (this.Keys.Count == 0)
                yes = false;
            else
            {
                foreach (var k in this.Keys)
                {
                    if (!Input.IsPressed(k))
                    {
                        yes = false;
                        break;
                    }
                }
            }

            if(this._lastState != yes)
            {
                this._lastState = yes;
                this.OnStateChanged(yes);
            }
        }

        /// <summary>
        /// Called when state changed.
        /// </summary>
        /// <param name="pressed">if set to <c>true</c> [pressed].</param>
        protected abstract void OnStateChanged(bool pressed);

        /// <summary>
        /// Parses the specified input.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        /// <exception cref="System.FormatException">Unknown keycode specified for hotkey:  + spl[i]</exception>
        public static VirtualKeys[] Parse(string input)
        {
            if (!string.IsNullOrEmpty(input))
            {
                List<VirtualKeys> ls = new List<VirtualKeys>();
                var spl = input.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < spl.Length; i++)
                {
                    string t = spl[i].Trim().ToLowerInvariant();
                    if (t.Length == 0)
                        continue;

                    var k = ParseOne(t);
                    if (!k.HasValue)
                        throw new FormatException("Unknown keycode specified for hotkey: " + spl[i]);

                    if (!ls.Contains(k.Value))
                        ls.Add(k.Value);
                }

                if (ls.Count != 0)
                    return ls.ToArray();
            }

            return EmptyKeys;
        }

        /// <summary>
        /// Tries to parse input.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="result">The result.</param>
        /// <returns></returns>
        public static bool TryParse(string input, out VirtualKeys[] result)
        {
            if (!string.IsNullOrEmpty(input))
            {
                List<VirtualKeys> ls = new List<VirtualKeys>();
                var spl = input.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < spl.Length; i++)
                {
                    string t = spl[i].Trim().ToLowerInvariant();
                    if (t.Length == 0)
                        continue;

                    var k = ParseOne(t);
                    if (!k.HasValue)
                    {
                        result = null;
                        return false;
                    }

                    if (!ls.Contains(k.Value))
                        ls.Add(k.Value);
                }

                if (ls.Count != 0)
                {
                    result = ls.ToArray();
                    return true;
                }
            }

            result = EmptyKeys;
            return true;
        }

        /// <summary>
        /// Parses one key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        private static VirtualKeys? ParseOne(string key)
        {
            if (key.Length == 0)
                return null;

            if(key.Length == 1)
            {
                char ch = key[0];
                if (ch >= '0' && ch <= '9')
                    return (VirtualKeys)((int)VirtualKeys.N0 + (ch - '0'));
                if (ch >= 'a' && ch <= 'z')
                    return (VirtualKeys)((int)VirtualKeys.A + (ch - 'a'));
            }
            else
            {
                if(key[0] == 'f' && key.Length <= 3)
                {
                    bool allOk = true;
                    for(int i = 1; i < key.Length; i++)
                    {
                        if(!char.IsDigit(key[i]))
                        {
                            allOk = false;
                            break;
                        }
                    }

                    if(allOk)
                    {
                        try
                        {
                            int num = int.Parse(key.Substring(1), System.Globalization.NumberStyles.None);
                            if (num >= 1 && num <= 24)
                                return (VirtualKeys)((int)VirtualKeys.F1 + (num - 1));
                        }
                        catch
                        {

                        }
                    }
                }
            }

            switch(key)
            {
                case "mouse1":
                case "leftmouse":
                case "mouseleft":
                case "lmouse":
                case "mousel":
                    return VirtualKeys.LeftButton;
                case "mouse2":
                case "rightmouse":
                case "mouseright":
                case "rmouse":
                case "mouser":
                    return VirtualKeys.RightButton;
                case "mouse3":
                case "middlemouse":
                case "mousemiddle":
                case "mmouse":
                case "mousem":
                    return VirtualKeys.MiddleButton;
                case "mouse4":
                case "xbutton1":
                case "extrabutton1":
                    return VirtualKeys.ExtraButton1;
                case "mouse5":
                case "xbutton2":
                case "extrabutton2":
                    return VirtualKeys.ExtraButton2;

                case "back":
                case "backspace":
                    return VirtualKeys.Back;

                case "tab":
                    return VirtualKeys.Tab;

                case "enter":
                    return VirtualKeys.Return;

                case "shift":
                case "shft":
                    return VirtualKeys.Shift;

                case "control":
                case "ctrl":
                    return VirtualKeys.Control;

                case "alt":
                    return VirtualKeys.Menu;

                case "lshift":
                case "leftshift":
                case "shiftl":
                case "shiftleft":
                    return VirtualKeys.LeftShift;

                case "rshift":
                case "rightshift":
                case "shiftr":
                case "shiftright":
                    return VirtualKeys.RightShift;

                case "lcontrol":
                case "lctrl":
                case "leftcontrol":
                case "leftctrl":
                case "controll":
                case "ctrll":
                case "controlleft":
                case "ctrlleft":
                    return VirtualKeys.LeftControl;

                case "rcontrol":
                case "rctrl":
                case "rightcontrol":
                case "rightctrl":
                case "controlr":
                case "ctrlr":
                case "controlright":
                case "ctrlright":
                    return VirtualKeys.RightControl;

                case "lalt":
                case "leftalt":
                case "altl":
                case "altleft":
                    return VirtualKeys.LeftMenu;

                case "ralt":
                case "rightalt":
                case "altr":
                case "altright":
                    return VirtualKeys.RightMenu;

                case "pause":
                    return VirtualKeys.Pause;

                case "caps":
                case "capslock":
                    return VirtualKeys.CapsLock;

                case "esc":
                case "escape":
                    return VirtualKeys.Escape;

                case "space":
                case "spacebar":
                    return VirtualKeys.Space;

                case "pageup":
                case "pgup":
                    return VirtualKeys.Prior;

                case "pagedown":
                case "pgdown":
                case "pagedn":
                    return VirtualKeys.Next;

                case "end":
                    return VirtualKeys.End;

                case "home":
                    return VirtualKeys.Home;

                case "right":
                case "rightarrow":
                case "arrowright":
                    return VirtualKeys.Right;

                case "left":
                case "leftarrow":
                case "arrowleft":
                    return VirtualKeys.Left;

                case "up":
                case "uparrow":
                case "arrowup":
                    return VirtualKeys.Up;

                case "down":
                case "downarrow":
                case "arrowdown":
                    return VirtualKeys.Down;

                case "select":
                    return VirtualKeys.Select;

                case "print":
                case "printscreen":
                case "printscrn":
                case "printscr":
                    return VirtualKeys.Snapshot;

                case "ins":
                case "insert":
                    return VirtualKeys.Insert;

                case "del":
                case "delete":
                    return VirtualKeys.Delete;

                case "one":
                case "n1":
                case "number1":
                case "numberone":
                    return VirtualKeys.N1;

                case "two":
                case "n2":
                case "number2":
                case "numbertwo":
                    return VirtualKeys.N2;

                case "three":
                case "n3":
                case "number3":
                case "numberthree":
                    return VirtualKeys.N3;

                case "four":
                case "n4":
                case "number4":
                case "numberfour":
                    return VirtualKeys.N4;

                case "five":
                case "n5":
                case "number5":
                case "numberfive":
                    return VirtualKeys.N5;

                case "six":
                case "n6":
                case "number6":
                case "numbersix":
                    return VirtualKeys.N6;

                case "seven":
                case "n7":
                case "number7":
                case "numberseven":
                    return VirtualKeys.N7;

                case "eight":
                case "n8":
                case "number8":
                case "numbereight":
                    return VirtualKeys.N8;

                case "nine":
                case "n9":
                case "number9":
                case "numbernine":
                    return VirtualKeys.N9;

                case "zero":
                case "n0":
                case "number0":
                case "numberzero":
                    return VirtualKeys.N0;

                case "windows":
                case "win":
                case "lwindows":
                case "lwin":
                case "leftwindows":
                case "leftwin":
                case "windowsl":
                case "winl":
                case "windowsleft":
                case "winleft":
                    return VirtualKeys.LeftWindows;

                case "rwindows":
                case "rwin":
                case "rightwindows":
                case "rightwin":
                case "windowsr":
                case "winr":
                case "windowsright":
                case "winright":
                    return VirtualKeys.RightWindows;

                case "numpad0":
                case "numpad1":
                case "numpad2":
                case "numpad3":
                case "numpad4":
                case "numpad5":
                case "numpad6":
                case "numpad7":
                case "numpad8":
                case "numpad9":
                    return (VirtualKeys)((int)VirtualKeys.Numpad0 + (key[6] - '0'));

                case "mul":
                case "mult":
                case "multiply":
                case "multiplication":
                case "*":
                    return VirtualKeys.Multiply;

                case "div":
                case "divide":
                case "division":
                case "/":
                    return VirtualKeys.Divide;

                case "add":
                case "addition":
                    return VirtualKeys.Add;

                case "sub":
                case "subtract":
                case "subtraction":
                case "-":
                    return VirtualKeys.Subtract;

                case "separator":
                case "sep":
                case "_":
                    return VirtualKeys.Separator;

                case "decimal":
                case "point":
                case ".":
                    return VirtualKeys.Decimal;

                case "comma":
                case ",":
                    return VirtualKeys.OEMComma;

                case "numlock":
                    return VirtualKeys.NumLock;

                case "scrolllock":
                case "scrollock":
                case "scrllock":
                    return VirtualKeys.ScrollLock;

                case ":":
                case ";":
                    return VirtualKeys.OEM1;

                case "?":
                    return VirtualKeys.OEM2;

                case "`":
                case "~":
                case "tilde":
                case "^":
                    return VirtualKeys.OEM3;

                case "[":
                case "{":
                    return VirtualKeys.OEM4;

                case "\\":
                case "|":
                    return VirtualKeys.OEM5;

                case "]":
                case "}":
                    return VirtualKeys.OEM6;

                case "'":
                case "\"":
                    return VirtualKeys.OEM7;

                case "<":
                case ">":
                    return VirtualKeys.OEM8; // ?
            }

            if(key.StartsWith("0x"))
            {
                int nx = 0;
                if (int.TryParse(key.Substring(2), System.Globalization.NumberStyles.AllowHexSpecifier, null, out nx) && nx > 0 && nx < 32768)
                    return (VirtualKeys)nx;
            }
            else
            {
                int nx = 0;
                if (int.TryParse(key, System.Globalization.NumberStyles.None, null, out nx) && nx > 0 && nx < 32768)
                    return (VirtualKeys)nx;
            }

            return null;
        }

        /// <summary>
        /// The locker for global list.
        /// </summary>
        private static readonly object Locker = new object();

        /// <summary>
        /// The global list of registered hotkeys.
        /// </summary>
        private static readonly List<HotkeyBase> All = new List<HotkeyBase>();

        /// <summary>
        /// Updates all registered hotkeys now.
        /// </summary>
        public static void UpdateAll()
        {
            lock(Locker)
            {
                for(int i = All.Count - 1; i >= 0; i--)
                {
                    if (i >= All.Count)
                        continue;

                    All[i].Update();
                }
            }
        }

        /// <summary>
        /// Registers this instance. This is not necessary if you plan to manually update the hotkey.
        /// </summary>
        /// <returns></returns>
        public bool Register()
        {
            lock(Locker)
            {
                if (this._regState)
                    return false;

                this._regState = true;
                All.Add(this);
                return true;
            }
        }

        /// <summary>
        /// Unregisters this instance.
        /// </summary>
        /// <returns></returns>
        public bool Unregister()
        {
            lock(Locker)
            {
                if (!this._regState)
                    return false;

                this._regState = false;
                All.Remove(this);
                return true;
            }
        }
    }

    /// <summary>
    /// A hotkey that will raise event when it's pressed state changes.
    /// </summary>
    /// <seealso cref="BetterTelekinesis.HotkeyBase" />
    public sealed class HotkeyHeld : HotkeyBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HotkeyHeld"/> class.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <param name="keys">The keys.</param>
        public HotkeyHeld(OnHeldStateChangedDelegate function, params VirtualKeys[] keys) : base(keys)
        {
            this.Function = function;
        }

        /// <summary>
        /// The function.
        /// </summary>
        private readonly OnHeldStateChangedDelegate Function;

        /// <summary>
        /// Delegate for hotkey.
        /// </summary>
        /// <param name="pressed">if set to <c>true</c> [pressed].</param>
        public delegate void OnHeldStateChangedDelegate(bool pressed);

        /// <summary>
        /// Called when state changed.
        /// </summary>
        /// <param name="pressed">if set to <c>true</c> [pressed].</param>
        protected override void OnStateChanged(bool pressed)
        {
            if(this.Function != null)
                this.Function(pressed);
        }

        /// <summary>
        /// Tries to create hotkey. This will return null if something goes wrong.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="function">The function.</param>
        /// <returns></returns>
        public static HotkeyHeld TryCreateHotkey(string input, OnHeldStateChangedDelegate function)
        {
            VirtualKeys[] vk;
            if (HotkeyBase.TryParse(input, out vk) && vk != null && vk.Length != 0)
                return new HotkeyHeld(function, vk);

            return null;
        }
    }

    /// <summary>
    /// A hotkey that will raise an event only when it's pressed state turns on.
    /// </summary>
    /// <seealso cref="BetterTelekinesis.HotkeyBase" />
    public sealed class HotkeyPress : HotkeyBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HotkeyPress"/> class.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <param name="keys">The keys.</param>
        public HotkeyPress(OnPressDelegate function, params VirtualKeys[] keys) : base(keys)
        {
            this.Function = function;
        }

        /// <summary>
        /// The function.
        /// </summary>
        private readonly OnPressDelegate Function;

        /// <summary>
        /// The delegate for hotkey.
        /// </summary>
        public delegate void OnPressDelegate();

        /// <summary>
        /// Called when state changed.
        /// </summary>
        /// <param name="pressed">if set to <c>true</c> [pressed].</param>
        protected override void OnStateChanged(bool pressed)
        {
            if (pressed && this.Function != null)
                this.Function();
        }

        /// <summary>
        /// Tries to create hotkey. This will return null if something goes wrong.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="function">The function.</param>
        /// <returns></returns>
        public static HotkeyPress TryCreateHotkey(string input, OnPressDelegate function)
        {
            VirtualKeys[] vk;
            if (HotkeyBase.TryParse(input, out vk) && vk != null && vk.Length != 0)
                return new HotkeyPress(function, vk);

            return null;
        }
    }
}
