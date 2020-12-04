using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomSkills
{
    public class StringAlloc
    {
        public string Value
        {
            get
            {
                return this._value;
            }
            set
            {
                if (this._value != null)
                    this.Allocation.Dispose();

                this._value = value;

                if (this._value != null)
                {
                    byte[] buf = Encoding.UTF8.GetBytes(this._value);
                    this.Allocation = NetScriptFramework.Memory.Allocate(buf.Length + 16);
                    NetScriptFramework.Memory.WriteZero(this.Allocation.Address, this.Allocation.Size);
                    NetScriptFramework.Memory.WriteBytes(this.Allocation.Address, buf);
                }
                else
                    this.Allocation = null;
            }
        }

        private string _value;

        public NetScriptFramework.MemoryAllocation Allocation
        {
            get;
            private set;
        }
    }

    /// <summary>
    /// Information about the skill.
    /// </summary>
    public sealed class Skill
    {
        public readonly StringAlloc Name = new StringAlloc();
        public readonly StringAlloc Description = new StringAlloc();
        public readonly StringAlloc Skydome = new StringAlloc();
        public GValueShort Level;
        public GValueFloat Ratio;
        public GValueShort ShowLevelup;
        public GValueShort OpenMenu;
        public GValueShort PerkPoints;
        public GValueShort Legendary;
        public GValueInt Color;
        public GValueShort DebugReload;
        public bool NormalNif;
        public NetScriptFramework.SkyrimSE.BGSSkillPerkTreeNode SkillTree;
        public readonly StringAlloc ColorStr = new StringAlloc();
        public int ColorLast = -1;

        public bool UpdateColor()
        {
            if (this.Color == null)
                return false;

            int cv = (this.Color.Value & 0xFFFFFF);
            if (cv == this.ColorLast)
                return true;

            this.ColorLast = cv;
            this.ColorStr.Value = "#" + cv.ToString("X6");
            return true;
        }
    }

    internal sealed class TreeNode
    {
        internal int Index;
        internal List<int> Links; // CNAM to index
        internal string PerkFile;
        internal uint PerkId;
        internal int GridX;
        internal int GridY;
        internal float X;
        internal float Y;

        internal static NetScriptFramework.SkyrimSE.BGSSkillPerkTreeNode Create(IReadOnlyList<TreeNode> nodes)
        {
            var itoNode = new Dictionary<int, TreeNode>();
            var itoMem = new Dictionary<int, NetScriptFramework.SkyrimSE.BGSSkillPerkTreeNode>();

            if(nodes != null)
            {
                foreach(var n in nodes)
                {
                    int ix = n.Index;
                    itoNode.Add(ix, n);
                    itoMem.Add(ix, CreateNode(ix));
                }

                foreach(var n in nodes)
                {
                    var mem = itoMem[n.Index];
                    var addr = mem.Cast<NetScriptFramework.SkyrimSE.BGSSkillPerkTreeNode>();

                    if(!string.IsNullOrEmpty(n.PerkFile) && n.PerkId != 0)
                    {
                        var perk = NetScriptFramework.SkyrimSE.TESForm.LookupFormFromFile(n.PerkId, n.PerkFile) as NetScriptFramework.SkyrimSE.BGSPerk;
                        if (perk != null)
                            NetScriptFramework.Memory.WritePointer(addr + 0x40, perk.Cast<NetScriptFramework.SkyrimSE.BGSPerk>());
                    }

                    NetScriptFramework.Memory.WriteInt32(addr + 0x48, 1); // fnam, unknown what this is
                    NetScriptFramework.Memory.WriteInt32(addr + 0x4C, n.GridX);
                    NetScriptFramework.Memory.WriteInt32(addr + 0x50, n.GridY);
                    NetScriptFramework.Memory.WriteFloat(addr + 0x60, n.X);
                    NetScriptFramework.Memory.WriteFloat(addr + 0x64, n.Y);

                    if(n.Links != null)
                    {
                        foreach(var ix in n.Links)
                            LinkNode(addr, itoMem[ix].Cast<NetScriptFramework.SkyrimSE.BGSSkillPerkTreeNode>());
                    }
                }
            }

            return itoMem[0];
        }

        private static void LinkNode(IntPtr source, IntPtr target)
        {
            AddList(source + 0x10, target);
            AddList(target + 0x28, source);
        }

        private static void AddList(IntPtr ptr, IntPtr value)
        {
            using (var alloc = NetScriptFramework.Memory.Allocate(0x10))
            {
                NetScriptFramework.Memory.WritePointer(alloc.Address, NetScriptFramework.Main.GameInfo.GetAddressOf(228548));
                NetScriptFramework.Memory.WritePointer(alloc.Address + 8, ptr);

                var func = CustomSkillsPlugin.addr_ListAlloc;
                int index = NetScriptFramework._IntPtrExtensions.ToInt32Safe(NetScriptFramework.Memory.InvokeCdecl(func, ptr + 0x10, alloc.Address, NetScriptFramework.Memory.ReadInt32(ptr + 8), 8));
                var data = NetScriptFramework.Memory.ReadPointer(ptr);
                NetScriptFramework.Memory.WritePointer(data + 8 * index, value);
            }
        }

        private static NetScriptFramework.SkyrimSE.BGSSkillPerkTreeNode CreateNode(int index)
        {
            var avInfo = NetScriptFramework.SkyrimSE.TESForm.LookupFormById(0x45D); // enchanting
            if (avInfo == null)
                throw new NullReferenceException("avInfo");

            var mem = NetScriptFramework.SkyrimSE.MemoryManager.Allocate(0x68, 0);
            NetScriptFramework.Memory.WriteZero(mem, 0x68);

            var ctor = CustomSkillsPlugin.addr_PerkNodeCtor;
            NetScriptFramework.Memory.InvokeCdecl(ctor, mem, index, avInfo.Cast<NetScriptFramework.SkyrimSE.ActorValueInfo>());

            return NetScriptFramework.MemoryObject.FromAddress<NetScriptFramework.SkyrimSE.BGSSkillPerkTreeNode>(mem);
        }
    }
}
