using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework.Tools;

namespace ActorLimitPlugin
{
    public sealed class Settings
    {
        [ConfigValue("MoverLimit", "New mover limit", "Change maximum amount of actors whose movement can be updated at a time. Default engine value here is 128. Increase if you still find floating NPCs.")]
        public int MoverLimit
        {
            get;
            set;
        } = 0x100;

        [ConfigValue("ReplaceStaticBuffer", "Replaces static buffer of actors", "This is necessary to uncap morph limit properly and a few other things. Usually nearby actors are limited to 64, anything after that gets a bit buggy due to them being excluded from a list that sorts nearby actors by distance. They get put into a static buffer that is limited in size, this setting will replace that buffer with a much larger one.")]
        public bool ReplaceStaticBuffer
        {
            get;
            set;
        } = true;

        [ConfigValue("MorphLimit", "Morph update limit", "Change maximum amount of actors whose facial morphs can be updated at a time. Default engine value here is 10. When you hit that limit actors facial expressions/lips stop moving even when they are talking.")]
        public int MorphLimit
        {
            get;
            set;
        } = 128;

        internal void Load()
        {
            ConfigFile.LoadFrom<Settings>(this, "ActorLimitPlugin", true);
        }
    }
}
