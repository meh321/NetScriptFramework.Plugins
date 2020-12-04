using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework;
using NetScriptFramework.SkyrimSE;

namespace GamePlayTweaks.Mods
{
    class ArrowStuff : Mod
    {
        public ArrowStuff() : base()
        {
            settings.OnlyPlayer = this.CreateSettingBool("OnlyPlayer", true, "Only apply this for the player character. Otherwise NPCs will also use this.");
            settings.ArrowCount = this.CreateSettingInt("ArrowCount", 1, "How many arrows to shoot at once. Setting this very high may screw up your savegame if you leave thousands of arrows in the world.");
            settings.ArrowCountGlobal = this.CreateSettingForm<TESGlobal>("ArrowCountGlobal", "", "If this is set then the count is instead taken from the global value and not from above setting.");
            settings.ForceDrawTime = this.CreateSettingFloat("ForceDrawTime", -1, "If this is zero or higher then force this timer every time you fire an arrow. That means if you set here 3.5 it will treat arrow fired as if you had drawn the bow for 3.5 seconds.");
        }

        private static class settings
        {
            internal static SettingValue<bool> OnlyPlayer;
            internal static SettingValue<long> ArrowCount;
            internal static SettingValue<TESGlobal> ArrowCountGlobal;
            internal static SettingValue<double> ForceDrawTime;
        }

        internal override string Description
        {
            get
            {
                return "Some stuff related to shooting arrows.";
            }
        }

        private NiNode _curPlaceNode;
        private NiPoint3 _curPlacePos;
        private bool _curPlaceHadNode;
        private float? _projStrength;
        private int _projTrack;
        
        private static void _mod(TESObjectWEAP weap, Actor attacker, ref int count)
        {
            if (count <= 0)
                return;

            if(settings.OnlyPlayer.Value)
            {
                if (attacker == null || !attacker.IsPlayer)
                    return;
            }

            long mult;
            if (settings.ArrowCountGlobal.Value != null)
                mult = (long)Math.Round(settings.ArrowCountGlobal.Value.FloatValue);
            else
                mult = settings.ArrowCount.Value;

            if (mult <= 1)
                return;

            mult *= count;
            if (mult > int.MaxValue)
                count = int.MaxValue;
            else
                count = (int)mult;
        }

        private IntPtr NiNode_ctor;

        internal override void Apply()
        {
            this.NiNode_ctor = NetScriptFramework.Main.GameInfo.GetAddressOf(68936);
            _cachedOffsets = new int[256];

            ulong vid = 17693;
            int baseOffset = 0x5430;

            Events.OnMainMenu.Register(e =>
            {
                if(this._curPlaceNode == null)
                {
                    var alloc = MemoryManager.Allocate(0x130, 0);
                    Memory.InvokeCdecl(this.NiNode_ctor, alloc, 0);
                    this._curPlaceNode = MemoryObject.FromAddress<NiNode>(alloc);
                    this._curPlaceNode.IncRef();

                    alloc = MemoryManager.Allocate(0x10, 0);
                    this._curPlacePos = MemoryObject.FromAddress<NiPoint3>(alloc);
                    Memory.WriteZero(this._curPlacePos.Address, 0xC);
                }
            }, 0, 1);
            
            Memory.WriteHook(new HookParameters()
            {
                Address = NetScriptFramework.Main.GameInfo.GetAddressOf(vid, 0x5CF2 - baseOffset, 0, "0F B6 D9 0F BE C2"),
                IncludeLength = 6,
                ReplaceLength = 6,
                Before = ctx =>
                {
                    TESObjectWEAP weap = null;
                    Actor actor = null;

                    int count = ctx.CX.ToUInt8();
                    try
                    {
                        weap = MemoryObject.FromAddress<TESObjectWEAP>(ctx.R12);
                    }
                    catch
                    {

                    }
                    try
                    {
                        actor = MemoryObject.FromAddress<Actor>(ctx.R15);
                    }
                    catch
                    {

                    }

                    int now = count;
                    _mod(weap, actor, ref now);

                    if (now > 255)
                        now = 255;

                    if (now != count)
                        ctx.CX = new IntPtr(now);
                },
            });

            Memory.WriteHook(new HookParameters()
            {
                Address = NetScriptFramework.Main.GameInfo.GetAddressOf(vid, 0x603D - baseOffset, 0, "E8"),
                IncludeLength = 5,
                ReplaceLength = 5,
                After = ctx =>
                {
                    TESObjectWEAP weap = null;
                    Actor actor = null;
                    int count = ctx.AX.ToUInt8();

                    try
                    {
                        weap = MemoryObject.FromAddress<TESObjectWEAP>(ctx.R12);
                    }
                    catch
                    {

                    }
                    try
                    {
                        actor = MemoryObject.FromAddress<Actor>(ctx.R15);
                    }
                    catch
                    {

                    }

                    int now = count;
                    _mod(weap, actor, ref now);

                    if (now > 255)
                        now = 255;

                    if (now != count)
                        ctx.AX = new IntPtr(now);
                },
            });

            Memory.WriteHook(new HookParameters()
            {
                Address = NetScriptFramework.Main.GameInfo.GetAddressOf(42928, 0xB91B - 0xB360, 0, "E8"),
                IncludeLength = 5,
                ReplaceLength = 5,
                After = ctx =>
                {
                    if (settings.ForceDrawTime.Value >= 0.0)
                        ctx.XMM0f = (float)settings.ForceDrawTime.Value;
                    else
                    {
                        int track = _projTrack;
                        if (track > 0)
                        {
                            track--;
                            _projTrack = track;

                            if (_projStrength.HasValue)
                            {
                                ctx.XMM0f = _projStrength.Value;
                                if (track == 0)
                                    _projStrength = null;
                            }
                            else
                                _projStrength = ctx.XMM0f;
                        }
                    }
                },
            });

            Memory.WriteHook(new HookParameters()
            {
                Address = NetScriptFramework.Main.GameInfo.GetAddressOf(vid, 0x621C - baseOffset, 0, "F3 0F 10 44 24 48"),
                IncludeLength = 0, //0x3D - 0x1C,
                ReplaceLength = 0x3D - 0x1C,
                Before = ctx =>
                {
                    var pos = MemoryObject.FromAddress<NiPoint3>(ctx.BP + 0x68);

                    int index = ctx.SI.ToUInt8();
                    if(index <= 1)
                    {
                        pos.X = Memory.ReadFloat(ctx.SP + 0x48);
                        pos.Y = Memory.ReadFloat(ctx.SP + 0x4C);
                        pos.Z = Memory.ReadFloat(ctx.SP + 0x50);
                        return;
                    }

                    var plr = PlayerCharacter.Instance;
                    bool isPlayer = plr != null && plr.Cast<PlayerCharacter>() == ctx.R15;
                    if (isPlayer)
                        _projTrack = index;
                    else
                        _projTrack = 0;
                    
                    float x = 0.0f;
                    float y = 0.0f;
                    _calculate_projectile_offset(index - 1, ref x, ref y);
                    
                    if(this._curPlaceHadNode)
                    {
                        var npos = this._curPlaceNode.WorldTransform.Position;
                        npos.X = Memory.ReadFloat(ctx.SP + 0x48);
                        npos.Y = Memory.ReadFloat(ctx.SP + 0x4C);
                        npos.Z = Memory.ReadFloat(ctx.SP + 0x50);

                        this._curPlacePos.X = x;
                        this._curPlacePos.Y = 0.0f;
                        this._curPlacePos.Z = y;
                        this._curPlaceNode.WorldTransform.Translate(this._curPlacePos, npos);

                        pos.X = npos.X;
                        pos.Y = npos.Y;
                        pos.Z = npos.Z;
                    }
                    else
                    {
                        bool didGet = false;
                        if (isPlayer)
                        {
                            var pcam = PlayerCamera.Instance;
                            if(pcam != null)
                            {
                                var pnode = pcam.Node;
                                if(pnode != null)
                                {
                                    byte[] buf = Memory.ReadBytes(pnode.WorldTransform.Address, 0x34);
                                    Memory.WriteBytes(this._curPlaceNode.WorldTransform.Address, buf);

                                    var tpos = this._curPlaceNode.WorldTransform.Position;
                                    tpos.X = Memory.ReadFloat(ctx.SP + 0x48);
                                    tpos.Y = Memory.ReadFloat(ctx.SP + 0x4C);
                                    tpos.Z = Memory.ReadFloat(ctx.SP + 0x50);
                                    this._curPlacePos.X = x;
                                    this._curPlacePos.Y = 0.0f;
                                    this._curPlacePos.Z = y;
                                    this._curPlaceNode.WorldTransform.Translate(this._curPlacePos, pos);
                                    didGet = true;
                                }
                            }
                        }
                        
                        if(!didGet)
                        {
                            pos.X = Memory.ReadFloat(ctx.SP + 0x48) + x;
                            pos.Y = Memory.ReadFloat(ctx.SP + 0x4C);
                            pos.Z = Memory.ReadFloat(ctx.SP + 0x50) + y;
                        }
                    }
                }
            });

            Events.OnWeaponFireProjectilePosition.Register(e =>
            {
                if(e.Node != null)
                {
                    byte[] buf = Memory.ReadBytes(e.Node.WorldTransform.Address, 0x34);
                    Memory.WriteBytes(this._curPlaceNode.WorldTransform.Address, buf);
                    this._curPlaceHadNode = true;
                }
                else
                    this._curPlaceHadNode = false;
            }, 50);
        }

        private static void _calculate_projectile_offset(int index, ref float x, ref float y)
        {
            if (index <= 0 || index >= 256)
                return;

            float step = 10.0f;

            if(_cachedOffsets[index] != 0)
            {
                int val = _cachedOffsets[index] >> 1;
                x = unchecked((sbyte)(val & 0xFF)) * step;
                val >>= 8;
                y = unchecked((sbyte)(val & 0xFF)) * step;
                return;
            }

            int jump = 1;
            int xpos = -jump;
            int ypos = -jump;

            for(int i = 1; i < index; i++)
            {
                _advance(ref jump, ref xpos, ref ypos);
            }

            sbyte y_sb = (sbyte)ypos;
            sbyte x_sb = (sbyte)xpos;
            byte y_b = unchecked((byte)y_sb);
            byte x_b = unchecked((byte)x_sb);
            int oval = y_b;
            oval <<= 8;
            oval |= x_b;
            oval <<= 1;
            oval |= 1;
            _cachedOffsets[index] = oval;

            x = xpos * step;
            y = ypos * step;
        }

        private static void _advance(ref int jump, ref int xpos, ref int ypos)
        {
            if(ypos == -jump)
            {
                if(xpos == jump)
                {
                    xpos = -jump;
                    ypos++;
                    return;
                }

                xpos++;
                return;
            }

            if(ypos == jump)
            {
                if(xpos == jump)
                {
                    jump++;
                    xpos = -jump;
                    ypos = -jump;
                    return;
                }

                xpos++;
                return;
            }

            if(xpos == -jump)
            {
                xpos = jump;
                return;
            }

            xpos = -jump;
            ypos++;
        }

        private static int[] _cachedOffsets;
    }
}
