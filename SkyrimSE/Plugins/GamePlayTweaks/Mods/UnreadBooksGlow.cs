//#define DEBUG_MSG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework;
using NetScriptFramework.SkyrimSE;

namespace GamePlayTweaks.Mods
{
    class UnreadBooksGlow : Mod
    {
        public UnreadBooksGlow()
        {
            this.BoolParameters["NotesGlow"] = false;
            this.BoolParameters["JournalsGlow"] = false;
            this.BoolParameters["NormalBooksGlow"] = true;
            this.BoolParameters["SpellBooksGlow"] = true;
            this.BoolParameters["SkillBooksGlow"] = true;
            this.BoolParameters["SpecialBooksGlow"] = false;

            this.StringParameters["NotesEffect"] = "69CE8:Skyrim.esm";
            this.StringParameters["JournalsEffect"] = "69CE8:Skyrim.esm";
            this.StringParameters["NormalBooksEffect"] = "69CE8:Skyrim.esm";
            this.StringParameters["SpellBooksEffect"] = "69CE8:Skyrim.esm";
            this.StringParameters["SkillBooksEffect"] = "69CE8:Skyrim.esm";
            this.StringParameters["SpecialBooksEffect"] = "69CE8:Skyrim.esm";
        }

        internal override string Description
        {
            get
            {
                return "Make unread books glow so you can see them better. You can enter multiple effect shader forms in the effect fields separated by ; symbol. If you do that then a random one will be chosen each time.";
            }
        }

        private void LoadEffect(string typeStr, Tools.BookTracker.BookTypes bookType)
        {
            if (!this.BoolParameters[typeStr + "Glow"])
                return;

            string input = this.StringParameters[typeStr + "Effect"] ?? "";
            var ls = CachedFormList.TryParse(input, "GamePlayTweaks", "UnreadBooksGlow." + typeStr + "Effect", true);
            if (ls == null)
                return;
            var all = ls.All.Where(q => q is TESEffectShader).Cast<TESEffectShader>().ToList();
            this._shaders[(int)bookType] = all;
        }

        internal override void Apply()
        {
            _is_cell_loaded = NetScriptFramework.Main.GameInfo.GetAddressOf(13163);

            int max = Enum.GetValues(typeof(Tools.BookTracker.BookTypes)).Cast<int>().Max() + 1;
            this._shaders = new List<TESEffectShader>[max];
            for (int i = 0; i < this._shaders.Length; i++)
                this._shaders[i] = new List<TESEffectShader>();
            
            Tools.BookTracker.EnsureExists();

            Events.OnMainMenu.Register(e =>
            {
                init();
            }, 0, 1);

            Events.OnFrame.Register(e =>
            {
                if (!_is_init)
                    return;

                int nc = Tools.BookTracker.Instance.UpdateCounter;
                if (_last_update_counter != nc)
                {
                    _last_update_counter = nc;
                    this.update();
                }
            }, 50);
        }

        private void init()
        {
            this._care_mask = 0;
            this.LoadEffect("Notes", Tools.BookTracker.BookTypes.Note);
            this.LoadEffect("Journals", Tools.BookTracker.BookTypes.Journal);
            this.LoadEffect("NormalBooks", Tools.BookTracker.BookTypes.NormalBook);
            this.LoadEffect("SkillBooks", Tools.BookTracker.BookTypes.SkillBook);
            this.LoadEffect("SpellBooks", Tools.BookTracker.BookTypes.SpellBook);
            this.LoadEffect("SpecialBooks", Tools.BookTracker.BookTypes.Special);
            this._shaderIds = new List<uint>(8);
            for (int i = 0; i < this._shaders.Length; i++)
            {
                if (this._shaders[i].Count != 0)
                {
                    this._care_mask |= 1 << i;
                    foreach (var x in this._shaders[i])
                    {
                        uint fid = x.FormId;
                        if (!this._shaderIds.Contains(fid))
                            this._shaderIds.Add(fid);
                    }
                }
            }

            _is_init = true;
        }

        private bool _is_init = false;
        private IntPtr _is_cell_loaded = IntPtr.Zero;
        private int _last_update_counter = 0;
        private int _care_mask = 0;
        private List<TESEffectShader>[] _shaders;
        private List<uint> _shaderIds;

        private bool IsOurEffect(TESEffectShader effect)
        {
            if (effect == null)
                return false;

            uint fid = effect.FormId;
            int c = this._shaderIds.Count;
            for(int i = 0; i < c; i++)
            {
                if (this._shaderIds[i] == fid)
                    return true;
            }

            return false;
        }
        
        private void update()
        {
            HashSet<uint> glowing = new HashSet<uint>();

            var plist = ProcessLists.Instance;
            if (plist == null)
                return;

            var plr = PlayerCharacter.Instance;
            if (plr == null)
                return;

            var pos = plr.Position;

#if DEBUG_MSG
            int did_update = 0;
#endif
            plist.MagicEffectsLock.Lock();
            try
            {
                foreach(var effectPtr in plist.MagicEffects)
                {
#if DEBUG_MSG
                    did_update++;
#endif

                    var effectInstance = effectPtr.Value as ShaderReferenceEffect;
                    if (effectInstance == null || effectInstance.Finished || !this.IsOurEffect(effectInstance.EffectData))
                        continue;

                    uint objHandle = effectInstance.TargetObjRefHandle;
                    using (var objHolder = new ObjectRefHolder(objHandle))
                    {
                        var obj = objHolder.Object;
                        if (this.ShouldGlow(obj, pos) == null)
                        {
                            effectInstance.Finished = true;
#if DEBUG_MSG
                            NetScriptFramework.Main.WriteDebugMessage("Removing glow from " + (obj != null ? obj.ToString() : "null"));
#endif
                        }
                        else if(obj != null)
                            glowing.Add(obj.FormId);
                    }
                }
            }
            finally
            {
                plist.MagicEffectsLock.Unlock();
            }
#if DEBUG_MSG
            //NetScriptFramework.Main.WriteDebugMessage("Iterated and updated " + did_update + " effects in ProcessLists");
#endif

            var tes = TES.Instance;
            if (tes == null)
                return;

            var cells = tes.GetLoadedCells();
            foreach(var cell in cells)
            {
                cell.CellLock.Lock();
                try
                {
                    foreach(var objPtr in cell.References)
                    {
                        var obj = objPtr.Value;
                        TESEffectShader shader = this.ShouldGlow(obj, pos);
                        if(shader != null && glowing.Add(obj.FormId))
                        {
#if DEBUG_MSG
                            NetScriptFramework.Main.WriteDebugMessage("Adding glow to " + obj.ToString());
#endif
                            obj.PlayEffect(shader, -1.0f);
                        }
                    }
                }
                finally
                {
                    cell.CellLock.Unlock();
                }
            }
        }

        private TESEffectShader ShouldGlow(TESObjectREFR obj, NiPoint3 pos)
        {
            if (obj == null)
                return null;

            var form = obj.BaseForm;
            TESObjectBOOK book = null;
            if (form == null || (book = form as TESObjectBOOK) == null || book.IsRead)
                return null;

            TESObjectCELL cell = null;
            if (obj.Node == null || (cell = obj.ParentCell) == null)
                return null;

            var tes = TES.Instance;
            if (tes == null)
                return null;

            if (!Memory.InvokeCdecl(_is_cell_loaded, tes.Cast<TES>(), cell.Cast<TESObjectCELL>(), 0).ToBool())
                return null;

            var tracker = Tools.BookTracker.Instance;
            var type = tracker.GetBookType(book);
            if (!type.HasValue)
            {
#if DEBUG_MSG
                NetScriptFramework.Main.WriteDebugMessage("Warning: " + book.ToString() + " does not have a type in tracker!");
#endif
                return null;
            }

            int mask = 1 << (int)type.Value;
            if ((this._care_mask & mask) == 0)
                return null;

            if (obj.Position.GetDistance(pos) > 8000.0f)
                return null;

            var ls = this._shaders[(int)type.Value];
            if (ls.Count == 0)
                return null;

            if (ls.Count == 1)
                return ls[0];

            return ls[rnd.Next(0, ls.Count)];
        }

        private Random rnd = new Random();
    }
}
