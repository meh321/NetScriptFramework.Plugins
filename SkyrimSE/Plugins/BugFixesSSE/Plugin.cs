using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework;

namespace BugFixesSSE
{
    public sealed class BugFixesPlugin : Plugin
    {
        public override string Key
        {
            get
            {
                return "bug_fixes";
            }
        }

        public override string Name
        {
            get
            {
                return "Bug Fixes SSE";
            }
        }

        public override string Author
        {
            get
            {
                return "meh321";
            }
        }

        public override int Version
        {
            get
            {
                return 2;
            }
        }
        
        protected override bool Initialize(bool loadedAny)
        {
            FixHandler.init(this, "BugFixesSSE");

            return true;
        }
    }
}
