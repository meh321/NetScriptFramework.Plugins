using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework.SkyrimSE;

namespace GamePlayTweaks.Tools
{
    public sealed class BookTracker
    {
        private BookTracker()
        {
            if(Instance != null)
                throw new InvalidOperationException();
            Instance = this;

            this.type_map = new Dictionary<uint, BookTypes>();
            this.all = new List<TESObjectBOOK>[max_booktypes + 1];
            this.read_count = new int[this.all.Length];
            this.unread_count = new int[this.all.Length];
            for (int i = 0; i < all.Length; i++)
                all[i] = new List<TESObjectBOOK>();

            NetScriptFramework.SkyrimSE.Events.OnMainMenu.Register(e =>
            {
                init();
            }, 0, 1);

            NetScriptFramework.SkyrimSE.Events.OnFrame.Register(e =>
            {
                int now = Environment.TickCount;
                int diff = unchecked(now - _last_update);
                if (diff < 1000)
                    return;

                _last_update = now;
                update();
            });
        }

        public static void EnsureExists()
        {
            if (Instance == null)
                new BookTracker();
        }

        public static BookTracker Instance
        {
            get;
            private set;
        }

        private int _last_update = 0;

        private static readonly int max_booktypes = Enum.GetValues(typeof(BookTypes)).Cast<int>().Max() + 1;
        private readonly List<TESObjectBOOK>[] all;
        private readonly int[] read_count;
        private readonly int[] unread_count;
        private readonly Dictionary<uint, BookTypes> type_map;

        public int UpdateCounter
        {
            get;
            private set;
        }

        public enum BookTypes : int
        {
            Note,
            Journal,
            NormalBook,
            SpellBook,
            SkillBook,
            Special,
        }

        public BookTypes? GetBookType(TESObjectBOOK book)
        {
            if(book != null)
            {
                BookTypes t;
                if (this.type_map.TryGetValue(book.FormId, out t))
                    return t;
            }

            return null;
        }

        public int GetReadCount(BookTypes type)
        {
            int ix = (int)type;
            if (ix < 0 || ix >= max_booktypes)
                return 0;
            return this.read_count[ix];
        }

        public int GetTotalReadCount()
        {
            return this.read_count[max_booktypes];
        }

        public int GetUnreadCount(BookTypes type)
        {
            int ix = (int)type;
            if (ix < 0 || ix >= max_booktypes)
                return 0;
            return this.unread_count[ix];
        }

        public int GetTotalUnreadCount()
        {
            return this.unread_count[max_booktypes];
        }

        public IReadOnlyList<TESObjectBOOK> GetAllBooks(BookTypes type)
        {
            int ix = (int)type;
            if (ix < 0 || ix >= max_booktypes)
                return null;
            return all[ix];
        }

        public IReadOnlyList<TESObjectBOOK> GetAllBooks()
        {
            return all[max_booktypes];
        }

        private BookTypes? DetermineBookType(TESObjectBOOK book)
        {
            // If we can't pick it up, must be special.
            if ((book.BookData.BookFlags & TESObjectBOOK.DataFlags.CantTake) != TESObjectBOOK.DataFlags.None)
                return BookTypes.Special;

            // Teaches spell.
            if (book.IsSpellBook)
                return BookTypes.SpellBook;

            // Advances skill.
            if (book.IsSkillBook)
                return BookTypes.SkillBook;

            // Some special items don't have this keyword.
            if (!book.HasKeywordText("VendorItemBook"))
                return BookTypes.Special;

            // Model path.
            string modelPath = book.ModelName.Text ?? "";
            modelPath = modelPath.ToLowerInvariant().Replace("\\", "/").Trim();
            if (modelPath.StartsWith("clutter/books/"))
                modelPath = modelPath.Substring("clutter/books/".Length);

            // Elder scroll looks like a book to all other conditions.
            if (modelPath.Contains("elderscroll"))
                return BookTypes.Special;

            // This doesn't actually work because books don't have editor ID in game :(
            /*string editorName = book.EditorId;
            if (!string.IsNullOrEmpty(editorName))
            {
                if (editorName.StartsWith("dun")) // Some dungeon related journals or diaries are impossible to determine any other way.
                    return BookTypes.Special;
                if (editorName.StartsWith("MGR")) // Mage questline books, like the ritual quest books or shalidor's insight, they are useless arcane images and duplicates of each other.
                    return BookTypes.Special;
            }*/

            // Pickup sound of a note or single page.
            {
                var sound = book.PickupSound;
                if (sound != null && sound.FormId == 0xC7A54)
                    return BookTypes.Note;
            }

            // Modelpath says note.
            if (modelPath.Contains("note"))
                return BookTypes.Note;

            // This usually catches all journals but some journals still look like books.
            if (modelPath.Contains("journal"))
                return BookTypes.Journal;

            // Looks like normal book, although some journals or quest related dungeon stuff still fall here as well.
            return BookTypes.NormalBook;
        }

        private bool _is_inited = false;

        private void init()
        {
            _is_inited = true;

            TESForm.ForEachForm(form =>
            {
                var book = form as TESObjectBOOK;
                if(book != null)
                {
                    var type = this.DetermineBookType(book);
                    if(type.HasValue)
                    {
                        this.all[(int)type.Value].Add(book);
                        this.all[max_booktypes].Add(book);
                        this.type_map[book.FormId] = type.Value;
                    }
                }
                return true;
            });
        }

        private void update()
        {
            if (!_is_inited)
                return;

            this.UpdateCounter++;

            int tyes = 0;
            int tno = 0;
            for (int i = 0; i < this.all.Length - 1; i++)
            {
                var ls = all[i];
                int yes = 0;
                int no = 0;
                foreach (var b in ls)
                {
                    if (b.IsRead)
                    {
                        yes++;
                        tyes++;
                    }
                    else
                    {
                        no++;
                        tno++;
                    }
                }

                this.read_count[i] = yes;
                this.unread_count[i] = no;
            }
            this.read_count[this.all.Length - 1] = tyes;
            this.unread_count[this.all.Length - 1] = tno;
        }
    }
}
