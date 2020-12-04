using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework.SkyrimSE;

namespace GamePlayTweaks.Mods
{
    class PlayerHeadTracking : Mod
    {
        public PlayerHeadTracking() : base()
        {
            settings.onlyRegularThird = this.CreateSettingBool("OnlyRegularThirdPerson", true, "If set to true then don't enable head track during crafting tables or other similar actions.");
            settings.horizontalMinAngle = this.CreateSettingFloat("HorizontalMinAngle", 0);
            settings.horizontalMaxAngle = this.CreateSettingFloat("HorizontalMaxAngle", 95);
            settings.verticalMinAngle = this.CreateSettingFloat("VerticalMinAngle", 0);
            settings.verticalMaxAngle = this.CreateSettingFloat("VerticalMaxAngle", 80);
            settings.forceDisableGlobal = this.CreateSettingForm<TESGlobal>("DisableGlobal", "", "If this points to a global variable and the variable is set to 1 or higher then head tracking is disabled.");
            settings.uninstallMode = this.CreateSettingBool("DisableAndFixMode", false, "If something goes really wrong and buggy with head tracking then enable this option. It will disable head tracking but also attempt to fix the issues.");
            settings.IFPVdist = this.CreateSettingFloat("IFPVCompatibilityDistance", 50, "If camera is closer than this many units to head then stop headtracking (compatibility with IFPV).");
        }

        internal override string Description
        {
            get
            {
                return "Makes player character look in the direction of camera. May have incompatibility with some other headtracking mods.";
            }
        }

        private static class settings
        {
            internal static SettingValue<TESGlobal> forceDisableGlobal;
            internal static SettingValue<bool> onlyRegularThird;
            internal static SettingValue<bool> uninstallMode;
            internal static SettingValue<double> horizontalMinAngle;
            internal static SettingValue<double> horizontalMaxAngle;
            internal static SettingValue<double> verticalMinAngle;
            internal static SettingValue<double> verticalMaxAngle;
            internal static SettingValue<double> IFPVdist;
        }

        [Flags]
        private enum states : int
        {
            none = 0,

            enabled = 1,

            paused = 2,

            need_new_target = 4,
        }
        
        private states State = states.none;
        private NiPoint3 TargetHeadTrack = null;
        private NiPoint3 TranslateHeadTrack = null;
        private IntPtr addr_MenuTopicManager;
        private IntPtr addr_PickData;
        private bool debug_msg = false;
        private bool debug_msg2 = false;

        internal override void Apply()
        {
            var alloc = NetScriptFramework.Memory.Allocate(0x20);
            alloc.Pin();

            this.addr_MenuTopicManager = NetScriptFramework.Main.GameInfo.GetAddressOf(514959);
            addr_PickData = NetScriptFramework.Main.GameInfo.GetAddressOf(515446);

            this.TargetHeadTrack = NetScriptFramework.MemoryObject.FromAddress<NiPoint3>(alloc.Address);
            this.TranslateHeadTrack = NetScriptFramework.MemoryObject.FromAddress<NiPoint3>(alloc.Address + 0x10);
            this.TranslateHeadTrack.X = 0.0f;
            this.TranslateHeadTrack.Y = 2000.0f;
            this.TranslateHeadTrack.Z = 0.0f;
            
            Events.OnFrame.Register(Event_Frame);
            Events.OnUpdatedPlayerHeadtrack.Register(Event_UpdatedHeadtrack);
            Events.OnUpdateCamera.Register(Event_UpdatedCamera, 100000);
        }

        private void SetEnabled(bool enabled, bool forceDisableFull)
        {
            if (enabled && !forceDisableFull)
            {
                if ((this.State & states.enabled) == states.none)
                {
                    this.State |= states.enabled;

                    if(this.debug_msg)
                        NetScriptFramework.Main.WriteDebugMessage("headtrack: enabled");
                }
            }
            else
            {
                if ((this.State & states.enabled) != states.none)
                {
                    this.State &= ~states.enabled;

                    if(this.debug_msg)
                        NetScriptFramework.Main.WriteDebugMessage("headtrack: disabled");
                }
            }

            var plr = PlayerCharacter.Instance;
            if (plr != null)
            {
                if (enabled && !forceDisableFull)
                    plr.IsHeadTrackingEnabled = enabled;
                else if (forceDisableFull)
                    plr.IsHeadTrackingEnabled = false;
                else
                {
                    // Disable like this so that there is no weird jerking motion of snapping back to not headtracking.
                    var selfptr = plr.Cast<PlayerCharacter>();
                    if (selfptr != IntPtr.Zero)
                    {
                        var flags = NetScriptFramework.Memory.ReadUInt32(selfptr + 0xC4);
                        flags &= ~(uint)8;
                        NetScriptFramework.Memory.WriteUInt32(selfptr + 0xC4, flags);
                    }
                }
            }
        }

        private void SetPaused(bool paused)
        {
            if (paused)
            {
                if ((this.State & states.paused) == states.none)
                {
                    this.State |= states.paused;

                    if(this.debug_msg)
                        NetScriptFramework.Main.WriteDebugMessage("headtrack: paused");
                }
            }
            else
            {
                if ((this.State & states.paused) != states.none)
                {
                    this.State &= ~states.paused;
                    this.State |= states.need_new_target;

                    if(this.debug_msg)
                        NetScriptFramework.Main.WriteDebugMessage("headtrack: resumed");
                }
            }
        }

        private NetScriptFramework.Tools.Timer _timer = new NetScriptFramework.Tools.Timer();

        private bool IsIFPVMaybe(PlayerCharacter plr, NiPoint3 camPos)
        {
            if (plr == null)
                return false;
            
            var root = plr.Node;
            if (root != null)
            {
                var head = root.LookupNodeByName("NPCEyeBone") ?? root.LookupNodeByName("NPC Head [Head]");
                if (head == null || head.WorldTransform.Position.GetDistance(camPos) >= settings.IFPVdist.Value)
                    return false;
            }
            else
                return false;

            return true;
        }
        
        private void Event_Frame(FrameEventArgs e)
        {   
            var main = NetScriptFramework.SkyrimSE.Main.Instance;
            if (main == null || main.IsGamePaused)
            {
                this.SetPaused(true);
                return;
            }

            // Other conditions can go here.
            
            this.SetPaused(false);
        }

        private bool IsSpecialFurniture
        {
            get
            {
                var actor = PlayerCharacter.Instance;
                if (actor == null)
                    return false;

                var process = actor.Process;
                if (process == null)
                    return false;

                var middleHigh = process.MiddleHigh;
                if (middleHigh == null)
                    return false;

                uint handle = middleHigh.CurrentFurnitureRefHandle;
                if (handle == 0)
                    return false;

                TESObjectREFR obj = null;
                using (var objHandle = new ObjectRefHolder(handle))
                {
                    if (objHandle.IsValid)
                        obj = objHandle.Object;
                }

                if (obj == null)
                    return false;

                var baseObj = obj.BaseForm;
                if (baseObj == null)
                    return false;

                foreach (var x in SpecialFurnitureKeywords)
                {
                    if (baseObj.HasKeywordText(x))
                        return true;
                }

                return false;
            }
        }
        
        private static readonly string[] SpecialFurnitureKeywords = new string[]
        {
            // Mining
            "isPickaxeTable",
            "isPickaxeWall",
            "isPickaxeFloor",

            // Other objects
            "FurnitureWoodChoppingBlock",
            "FurnitureResourceObjectSawmill",
            "isCartTravelPlayer",
        };

        private void Event_UpdatedCamera(UpdateCameraEventArgs e)
        {
            bool enabled = false;
            bool showedDebug = false;
            bool disabled = settings.uninstallMode.Value;
            string extraDebug = null;
            try
            {
                if (disabled)
                    return;

                var camera = e.Camera as PlayerCamera;
                if (camera == null)
                    return;

                var state = camera.State;
                if (state == null)
                    return;

                var id = state.Id;
                if (settings.onlyRegularThird.Value)
                {
                    switch (id)
                    {
                        //case TESCameraStates.Dragon:
                        //case TESCameraStates.Horse:
                        case TESCameraStates.ThirdPerson2:
                            break;

                        case TESCameraStates.ThirdPerson1:
                        case TESCameraStates.Furniture:
                            disabled = true;
                            return;

                        case TESCameraStates.Free:
                            break;

                        default:
                            disabled = true;
                            return;
                    }
                }
                else
                {
                    switch (id)
                    {
                        //case TESCameraStates.Dragon:
                        //case TESCameraStates.Horse:
                        case TESCameraStates.ThirdPerson1:
                        case TESCameraStates.ThirdPerson2:
                            break;

                        case TESCameraStates.Furniture:
                            disabled = true;
                            return;

                        case TESCameraStates.Free:
                            break;

                        default:
                            disabled = true;
                            return;
                    }
                }

                var third = state as ThirdPersonState;
                if (third == null)
                {
                    if (debug_msg)
                        extraDebug = "third == null";
                    return;
                }

                if (settings.forceDisableGlobal.Value != null)
                {
                    float fgval = settings.forceDisableGlobal.Value.FloatValue;
                    if (fgval >= 1.5f)
                    {
                        disabled = true;
                        return;
                    }
                    if (fgval >= 0.5f)
                        return;
                }

                var plr = PlayerCharacter.Instance;
                if (plr == null)
                    return;

                {
                    // Aiming bow or crossbow.
                    uint flags = NetScriptFramework.Memory.ReadUInt32(plr.Cast<PlayerCharacter>() + 0xC0) >> 28;
                    //if (flags == 0xA)
                    if(flags >= 8)
                    {
                        disabled = true;
                        return;
                    }

                    for (int i = 0; i < 3; i++)
                    {
                        var caster = plr.GetMagicCaster((EquippedSpellSlots)i);
                        if (caster == null)
                            continue;

                        var mstate = caster.State;
                        switch (mstate)
                        {
                            case MagicCastingStates.Charged:
                            case MagicCastingStates.Charging:
                            case MagicCastingStates.Concentrating:
                                {
                                    disabled = true;
                                    return;
                                }
                        }
                    }
                }

                if(this.IsSpecialFurniture)
                {
                    disabled = true;
                    return;
                }

                bool hadpos = false;
                bool skipAngle = false;

                try
                {
                    var mm = MenuManager.Instance;
                    if (mm != null)
                    {
                        if (mm.IsMenuOpen("Dialogue Menu"))
                        {
                            var menuMgr = NetScriptFramework.Memory.ReadPointer(this.addr_MenuTopicManager);
                            if (menuMgr != null)
                            {
                                uint refHandleId = NetScriptFramework.Memory.ReadUInt32(menuMgr + 0x68);
                                using (var refObj = new ObjectRefHolder(refHandleId))
                                {
                                    var objPtr = refObj.Object;
                                    if (objPtr != null)
                                    {
                                        var root = objPtr.Node;
                                        if (root != null)
                                        {
                                            var head = root.LookupNodeByName("NPCEyeBone") ?? root.LookupNodeByName("NPC Head [Head]");
                                            if (head != null)
                                            {
                                                var hpos = head.WorldTransform.Position;
                                                this.TargetHeadTrack.X = hpos.X;
                                                this.TargetHeadTrack.Y = hpos.Y;
                                                this.TargetHeadTrack.Z = hpos.Z;
                                                hadpos = true;
                                                skipAngle = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {

                }

                if(!hadpos)
                {
                    try
                    {
                        var pickData = NetScriptFramework.Memory.ReadPointer(addr_PickData);
                        if(pickData != IntPtr.Zero)
                        {
                            uint refHandleId = NetScriptFramework.Memory.ReadUInt32(pickData + 4);
                            using (var refObj = new ObjectRefHolder(refHandleId))
                            {
                                var objPtr = refObj.Object as Actor;
                                if (objPtr != null && !objPtr.IsDead)
                                {
                                    var root = objPtr.Node;
                                    if (root != null)
                                    {
                                        var head = root.LookupNodeByName("NPCEyeBone") ?? root.LookupNodeByName("NPC Head [Head]");
                                        if (head != null)
                                        {
                                            var hpos = head.WorldTransform.Position;
                                            this.TargetHeadTrack.X = hpos.X;
                                            this.TargetHeadTrack.Y = hpos.Y;
                                            this.TargetHeadTrack.Z = hpos.Z;
                                            hadpos = true;
                                            skipAngle = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {

                    }
                }

                double x = 0, y = 0;
                if (!skipAngle)
                {
                    x = (180.0 / Math.PI) * third.XRotationFromLastResetPoint;
                    y = 0.0;
                    
                    {
                        float amount = -plr.Rotation.X;
                        float offset = third.YRotationFromLastResetPoint;
                        y = (180.0 / Math.PI) * (amount + offset);
                    }

                    x = Math.Abs(x);
                    y = Math.Abs(y);

                    if (x < settings.horizontalMinAngle.Value || x > settings.horizontalMaxAngle.Value || y < settings.verticalMinAngle.Value || y > settings.verticalMaxAngle.Value)
                        return;
                }

                var node = camera.Node;
                if (node == null)
                    return;

                if (this.IsIFPVMaybe(plr, node.WorldTransform.Position))
                {
                    disabled = true;
                    if(debug_msg)
                        extraDebug = "ifpv";
                    return;
                }

                if(!hadpos)
                    node.WorldTransform.Translate(this.TranslateHeadTrack, this.TargetHeadTrack);
                enabled = true;

                if (debug_msg)
                {
                    if (!_timer.IsRunning)
                        _timer.Start();
                    if (_timer.Time >= 1000)
                    {
                        NetScriptFramework.Main.WriteDebugMessage("============================================");
                        Action<string, NiPoint3> report = (type, pos) =>
                        {
                            NetScriptFramework.Main.WriteDebugMessage(type + "_x: " + pos.X + "; " + type + "_y: " + pos.Y + "; " + type + "_z: " + pos.Z + "; dist: " + node.WorldTransform.Position.GetDistance(pos));
                        };
                        NetScriptFramework.Main.WriteDebugMessage("rot_x = " + x + "; rot_y = " + y);
                        report("target", this.TargetHeadTrack);
                        report("cam", node.WorldTransform.Position);
                        try
                        {
                            report("head", PlayerCharacter.Instance.GetSkeletonNode(false).LookupNodeByName("NPC Head [Head]").WorldTransform.Position);
                        }
                        catch
                        {

                        }
                        _timer.Restart();
                        debug_msg2 = true;
                    }
                    showedDebug = true;
                }
            }
            finally
            {
                this.SetEnabled(enabled, disabled);

                if (enabled)
                    this.State &= ~states.need_new_target;

                if(!showedDebug && debug_msg)
                {
                    if (!_timer.IsRunning)
                        _timer.Start();
                    if (_timer.Time >= 1000)
                    {
                        string dmsg = !string.IsNullOrEmpty(extraDebug) ? extraDebug : "?";
                        NetScriptFramework.Main.WriteDebugMessage(dmsg);
                        _timer.Restart();
                    }
                }
            }
        }

        private void Event_UpdatedHeadtrack(UpdatedPlayerHeadtrackEventArgs e)
        {
            var s = this.State;
            if (s != states.enabled)
                return;

            if(debug_msg2)
            {
                debug_msg2 = false;
                NetScriptFramework.Main.WriteDebugMessage("UpdatedHeadtrack");
            }

            var plr = PlayerCharacter.Instance;
            if (plr != null)
                plr.SetLookAtPosition(this.TargetHeadTrack);
        }
    }
}
