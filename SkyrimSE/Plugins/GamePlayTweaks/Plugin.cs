using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework;

namespace GamePlayTweaks
{
    public sealed class GamePlayTweaksPlugin : Plugin
    {
        public override string Key
        {
            get
            {
                return "gameplaytweaks";
            }
        }

        public override string Name
        {
            get
            {
                return "Gameplay Tweaks";
            }
        }

        public override string Author
        {
            get
            {
                return "meh321";
            }
        }

        public override int RequiredLibraryVersion
        {
            get
            {
                return 14;
            }
        }

        public override int Version
        {
            get
            {
                return 6;
            }
        }

        protected override bool Initialize(bool loadedAny)
        {
            ModHandler.init(this, "GamePlayTweaks");

            NetScriptFramework.SkyrimSE.Events.OnMainMenu.Register(e => ModHandler.HadMainMenu = true, 0, 1);

            return true;
        }
    }
}
