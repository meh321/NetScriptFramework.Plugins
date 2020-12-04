using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework;

namespace DebugConsole
{
    public class DebugConsolePlugin : Plugin
    {
        public override string Key
        {
            get
            {
                return "debug_console";
            }
        }

        public override string Name
        {
            get
            {
                return "Debug Console";
            }
        }

        public override int Version
        {
            get
            {
                return 1;
            }
        }

        protected override bool Initialize(bool loadedAny)
        {
            GUI._Start();

            Main.AddDebugMessageListener(new _debugListen());

            return true;
        }

        internal static void ExecuteCommand(string cmd, string args)
        {
            switch((cmd ?? "").ToLowerInvariant())
            {
                case "quitgame":
                    {
                        NetScriptFramework.SkyrimSE.Main.Instance.QuitGame = true;
                    }
                    break;

                default:
                    {
                        GUI.WriteLine("Unknown command: " + (cmd ?? ""));
                    }
                    break;
            }
        }

        private sealed class _debugListen : DebugMessageListener
        {
            public override void OnMessage(Plugin sender, string message)
            {
                string name = sender != null ? sender.GetType().Name : "Unknown";
                GUI.WriteLine(name + ": " + message);
            }
        }
    }
}
