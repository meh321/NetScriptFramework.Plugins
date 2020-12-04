using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework;
using NetScriptFramework.SkyrimSE;

namespace BlinkSpell
{
    internal sealed class BlinkState
    {
        internal BlinkState(BlinkSpellPlugin plugin)
        {
            this.Plugin = plugin;

            this.SetupRaycastMask(new CollisionLayers[] {
                CollisionLayers.Unidentified,
                CollisionLayers.Static,
                CollisionLayers.AnimStatic,
                CollisionLayers.Transparent,
                CollisionLayers.Clutter,
                CollisionLayers.Weapon,
                CollisionLayers.Projectile,
                CollisionLayers.Trees,
                CollisionLayers.Props,
                CollisionLayers.Water,
                CollisionLayers.Terrain,
                CollisionLayers.Ground,
                CollisionLayers.DebrisLarge,
                CollisionLayers.TransparentSmall,
                CollisionLayers.TransparentSmallAnim,
                CollisionLayers.InvisibleWall,
                CollisionLayers.CharController,
                CollisionLayers.StairHelper,
                CollisionLayers.BipedNoCC,
                CollisionLayers.CollisionBox,
                CollisionLayers.LivingAndDeadActors
            });
        }

        internal readonly BlinkSpellPlugin Plugin;

        private void SetupRaycastMask(CollisionLayers[] layers)
        {
            ulong m = 0;
            foreach(var l in layers)
            {
                ulong fl = (ulong)1 << (int)l;
                m |= fl;
            }
            this.RaycastMask = m;
        }
        
        private ulong RaycastMask = 0;

        private NiPoint3 CurrentTeleportPoint = null;
        private NiPoint3 SourceTeleportPoint = null;
        private NiPoint3 SourceMovePoint = null;
        private NiPoint3 TargetTeleportPoint = null;
        private NiPoint3 TargetMarkerPoint = null;
        private NiPoint3 AimVectorPoint = null;
        private NiPoint3 AimVectorPointDoubled = null;
        private NiTransform ThirdPersonTempTransform = null;
        private List<TimedValue>[] TimedValues = new List<TimedValue>[(int)TimedValueTypes.Max];

        private InternalStates State = InternalStates.None;
        private IntPtr fn_Actor_SetPosition;
        private IntPtr fn_BGSSoundDescriptor_PlaySound;
        private IntPtr fn_TESImageSpaceModifier_Apply;
        private IntPtr fn_FlashHudMeter;
        private IntPtr fn_GetMagicFailedMessage;
        
        private List<RayCastResult> DoRayCast(TESObjectCELL cell, float[] source, float[] target)
        {
            return TESObjectCELL.RayCast(new RayCastParameters()
            {
                Cell = cell,
                Begin = source,
                End = target
            });
        }

        private enum TimedValueTypes : int
        {
            TeleportDoneRatio = 0,

            Max
        }

        private enum InternalStates : int
        {
            None = 0,

            Aiming,
            Fire,
            Teleporting,
        }

        internal sealed class MarkerData
        {
            internal MarkerData(string markerNif, float markerScale, float fadeInTime, float fadeOutTime)
            {
                this.MarkerNif = markerNif;
                this.MarkerScale = markerScale;
                this.FadeInTime = fadeInTime;
                this.FadeOutTime = fadeOutTime;
            }

            private readonly string MarkerNif;
            private readonly float MarkerScale;
            private readonly float FadeInTime;
            private readonly float FadeOutTime;
            internal NiAVObject Object;
            private float CurrentFade;
            private float WantFade;
            internal int WantState;
            private bool TriedToLoadMarker;
            private bool DidLoadMarker;
            
            internal bool IsInWorld()
            {
                return this.Object != null && this.Object.Parent != null;
            }

            private void UpdateFade(float diff)
            {
                if (diff <= 0.0f || this.CurrentFade == this.WantFade)
                    return;

                if (this.CurrentFade < this.WantFade)
                {
                    float time = this.FadeInTime;
                    if (time <= 0.0f)
                    {
                        this.CurrentFade = this.WantFade;
                        return;
                    }
                    float change = diff / time;
                    this.CurrentFade += change;
                    if (this.CurrentFade > this.WantFade)
                        this.CurrentFade = this.WantFade;
                }
                else
                {
                    float time = this.FadeOutTime;
                    if (time <= 0.0f)
                    {
                        this.CurrentFade = this.WantFade;
                        return;
                    }
                    float change = diff / time;
                    this.CurrentFade -= change;
                    if (this.CurrentFade < this.WantFade)
                        this.CurrentFade = this.WantFade;
                }
            }

            private void SetCurrentFade(float now)
            {
                var m = this.Object;
                if (m != null && m is BSFadeNode)
                {
                    var mptr = m.Cast<BSFadeNode>();
                    if (mptr != IntPtr.Zero)
                    {
                        Memory.WriteFloat(mptr + 0x130, now);
                        Memory.WriteFloat(mptr + 0x140, now);
                    }
                }
            }

            internal void Free()
            {
                if (this.Object != null)
                {
                    this.RemoveFromWorld();
                    this.Object.DecRef();
                    this.Object = null;
                }
            }

            private void AddToWorld()
            {
                if (this.Object == null)
                    return;

                var plr = PlayerCharacter.Instance;
                if (plr == null)
                    return;

                var node = plr.Node;
                if (node == null)
                    return;

                node = node.Parent;
                if (node == null)
                    return;

                this.CurrentFade = 0.0f;
                this.WantFade = 1.0f;
                this.SetCurrentFade(this.CurrentFade);

                node.AttachObject(this.Object);
            }

            private void RemoveFromWorld()
            {
                if (this.Object == null)
                    return;

                this.Object.Detach();
            }

            private bool FirstUpdate = true;

            internal void UpdateObject(float totalTime)
            {
                if (this.Object == null)
                    return;

                this.Object.Update(totalTime);

                if(this.FirstUpdate)
                {
                    this.FirstUpdate = false;
                    this.SetCurrentFade(this.CurrentFade);
                    this.Object.Update(totalTime);
                }
            }
            
            internal bool Update(float diff, float totalTime)
            {
                if(this.WantState > 0 && !this.TriedToLoadMarker)
                {
                    this.TriedToLoadMarker = true;
                    NiObject.LoadFromFileAsync(new NiObjectLoadParameters()
                    {
                        Callback = p =>
                        {
                            if (p.Success)
                            {
                                var obj = p.Result[0] as NiAVObject;
                                if (obj != null)
                                {
                                    this.Object = obj;
                                    this.Object.IncRef();
                                    this.Object.LocalTransform.Scale = this.MarkerScale;
                                }
                            }
                            this.DidLoadMarker = true;
                        },
                        Count = 1,
                        FileName = this.MarkerNif
                    });
                }

                if (!this.DidLoadMarker)
                    return true;

                if (this.WantState > 0 && this.Object != null)
                {
                    this.WantState = 0;
                    this.CurrentFade = 0.0f;
                    this.WantFade = 1.0f;
                    this.SetCurrentFade(0.0f);

                    this.AddToWorld();
                }
                else if (this.WantState < 0)
                    this.WantFade = 0.0f;

                if (this.Object != null)
                {
                    this.UpdateFade(diff);
                    this.SetCurrentFade(this.CurrentFade);
                }

                if (this.Object != null && this.IsInWorld() && this.WantState < 0 && this.CurrentFade > 0.0f)
                    this.UpdateObject(totalTime);

                if(this.WantState < 0 && this.CurrentFade <= 0.0f)
                {
                    this.Free();
                    return false;
                }

                return true;
            }
        }

        internal readonly List<MarkerData> Markers = new List<MarkerData>();

        internal void Reset()
        {
            this.CurrentDistortionTime = 1000.0f;
            this.State = InternalStates.None;
            this.HotkeyState = 0;
            foreach (var m in this.Markers)
                m.WantState = -1;

            for (int i = 0; i < (int)TimedValueTypes.Max; i++)
                this.TimedValues[i] = null;
        }

        internal void Fire(bool fromSpell)
        {
            if (this.State != InternalStates.Aiming)
                return;

            this.LastFireIsFromSpell = fromSpell;
            this.State = InternalStates.Fire;
        }

        private bool LastFireIsFromSpell = false;
        private const float MaxTargetDistort = 0.6f;
        private const float DistortionDurationAfter = 1.3f;
        private float CurrentTeleportTime = 0.0f;
        private float CurrentDistortionTime = 1000.0f;
        private float CurrentMaxDistort = 0.0f;
        private int HotkeyState = 0;

        private float Curve(float ratio, bool rising)
        {
            ratio = Math.Max(0.0f, Math.Min(1.0f, ratio));

            double amt = 0.3 / (ratio + 0.25) - 0.25;
            amt = Math.Max(0.0, Math.Min(1.0, amt));
            if (rising)
                amt = 1.0 - amt;
            return (float)amt;
        }

        internal float GetDistortionEffect()
        {
            if (this.CurrentDistortionTime >= DistortionDurationAfter)
                return 0.0f;

            float mult = this.Plugin.Settings.ScreenDistortion;
            if (mult == 0.0f)
                return 0.0f;

            if(this.CurrentDistortionTime < 0.0f)
            {
                float timeTotal = this.CurrentTeleportTime;
                float curTime = timeTotal + this.CurrentDistortionTime;
                if (timeTotal <= 0.0f)
                    return this.CurrentMaxDistort * mult;
                float ratio = curTime / timeTotal;
                return Curve(ratio, true) * this.CurrentMaxDistort * mult;
            }

            float ratioDone = this.CurrentDistortionTime / DistortionDurationAfter;
            return this.CurrentMaxDistort * mult * Curve(ratioDone, false);
        }

        private void UpdateHotkey()
        {
            bool isPressed = this.Plugin.Settings.Hotkey > 0 && NetScriptFramework.Tools.Input.IsPressed((NetScriptFramework.Tools.VirtualKeys)this.Plugin.Settings.Hotkey);
            bool isAbort = this.Plugin.Settings.AbortHotkey > 0 && NetScriptFramework.Tools.Input.IsPressed((NetScriptFramework.Tools.VirtualKeys)this.Plugin.Settings.AbortHotkey);

            if(this.HotkeyState > 0)
            {
                if (isAbort)
                {
                    this.HotkeyState = -1;
                }
                else if (!isPressed)
                {
                    this.Fire(false);
                    this.HotkeyState = 0;
                }
                return;
            }

            if(this.HotkeyState < 0)
            {
                if (!isPressed)
                    this.HotkeyState = 0;
                return;
            }

            if (!isPressed || isAbort)
                return;

            this.HotkeyState = 1;
        }

        private void ApplyIMod(PlayerCharacter plr)
        {
            if (this.fn_TESImageSpaceModifier_Apply == IntPtr.Zero)
                return;

            var imod = this.Plugin.Settings.IModForm;
            if (imod == null)
                return;

            Memory.InvokeCdecl(this.fn_TESImageSpaceModifier_Apply, imod.Address, 1.0f, 0);
        }
        
        private void ShowLowMagicka()
        {
            if (this.fn_FlashHudMeter != IntPtr.Zero)
                Memory.InvokeCdecl(this.fn_FlashHudMeter, 25);

            if (this.fn_GetMagicFailedMessage != IntPtr.Zero)
            {
                var msg = Memory.InvokeCdecl(this.fn_GetMagicFailedMessage, 1);
                if (msg != IntPtr.Zero)
                {
                    string str = Memory.ReadString(msg, false);
                    if(!string.IsNullOrEmpty(str))
                        MenuManager.ShowHUDMessage(str, null, true);
                }
            }
        }

        private void ShowLowStamina()
        {
            if (this.fn_FlashHudMeter != IntPtr.Zero)
                Memory.InvokeCdecl(this.fn_FlashHudMeter, 26);
        }

        private void ShowRecoveryTime()
        {
            if (this.fn_FlashHudMeter != IntPtr.Zero)
                Memory.InvokeCdecl(this.fn_FlashHudMeter, 38);
        }

        private void PlaySound(PlayerCharacter plr)
        {
            if (this.fn_BGSSoundDescriptor_PlaySound == IntPtr.Zero)
                return;

            var snd = this.Plugin.Settings.SoundForm;
            if (snd == null)
                return;

            var node = plr.Node;
            if (node == null)
                return;

            Memory.InvokeCdecl(this.fn_BGSSoundDescriptor_PlaySound, snd.Address, 0, plr.Position.Address, node.Address);
        }

        private bool CheckCosts(PlayerCharacter plr, bool notify, bool take)
        {
            float costMagicka = this.Plugin.Settings.MagickaCost;
            float costStamina = this.Plugin.Settings.StaminaCost;
            float recoveryTime = this.Plugin.Settings.RecoveryTime;

            if (costMagicka > 0.0f)
            {
                float has = plr.GetActorValue(ActorValueIndices.Magicka);
                if (has < costMagicka)
                {
                    if(notify)
                        this.ShowLowMagicka();
                    return false;
                }
            }

            if (costStamina > 0.0f)
            {
                float has = plr.GetActorValue(ActorValueIndices.Stamina);
                if (has < costStamina)
                {
                    if(notify)
                        this.ShowLowStamina();
                    return false;
                }
            }

            if(recoveryTime > 0.0f)
            {
                float has = plr.VoiceRecoveryTime;
                if(has > 0.0f)
                {
                    if (notify)
                        this.ShowRecoveryTime();
                    return false;
                }
            }

            if(take)
            {
                if (costMagicka != 0.0f)
                    plr.DamageActorValue(ActorValueIndices.Magicka, -costMagicka);
                if (costStamina != 0.0f)
                    plr.DamageActorValue(ActorValueIndices.Stamina, -costStamina);
                if (recoveryTime > 0.0f)
                    plr.VoiceRecoveryTime = recoveryTime;
            }

            return true;
        }
        
        internal void Update(float diff, float totalTime)
        {
            var main = NetScriptFramework.SkyrimSE.Main.Instance;
            PlayerCharacter plr = null;
            TESObjectCELL cell = null;
            SpellItem spell = this.Plugin.Settings.SpellForm;

            if(main == null || main.IsGamePaused || (plr = PlayerCharacter.Instance) == null || (cell = plr.ParentCell) == null)
            {
                this.Reset();
                return;
            }

            this.UpdateHotkey();

            if (this.CurrentDistortionTime < DistortionDurationAfter)
                this.CurrentDistortionTime += diff;

            if (this.Plugin.Settings.AutoLearnSpell && spell != null && !plr.HasSpell(spell))
                plr.AddSpell(spell, true);

            for(int i = this.Markers.Count - 1; i >= 0; i--)
            {
                if (!this.Markers[i].Update(diff, totalTime))
                    this.Markers.RemoveAt(i);
            }

            switch(this.State)
            {
                case InternalStates.None:
                    {
                        var castState = this.GetCurrentCastingState(plr, spell);
                        if(castState == MagicCastingStates.Charged)
                        {
                            if (!this.CheckCosts(plr, true, false))
                            {
                                if (spell != null)
                                    this.InterruptCast(plr, spell);
                                if (this.HotkeyState != 0)
                                    this.HotkeyState = -1;
                                return;
                            }

                            this.State = InternalStates.Aiming;
                            var m = new MarkerData(Plugin.Settings.MarkerNif, Plugin.Settings.MarkerScale, Plugin.Settings.MarkerFadeInTime, Plugin.Settings.MarkerFadeOutTime);
                            m.WantState = 1;
                            this.Markers.Add(m);
                            this.UpdateAiming(diff, totalTime, plr, cell);
                        }
                    }
                    break;

                case InternalStates.Aiming:
                    {
                        var castState = this.GetCurrentCastingState(plr, spell);
                        if (castState != MagicCastingStates.Charged && castState != MagicCastingStates.Released)
                        {
                            this.State = InternalStates.None;
                            foreach (var m in this.Markers)
                                m.WantState = -1;
                        }
                        else
                        {
                            if(!this.CheckCosts(plr, true, false))
                            {
                                this.State = InternalStates.None;
                                foreach (var m in this.Markers)
                                    m.WantState = -1;
                                if (spell != null)
                                    this.InterruptCast(plr, spell);
                                if (this.HotkeyState != 0)
                                    this.HotkeyState = -1;
                                return;
                            }

                            this.UpdateAiming(diff, totalTime, plr, cell);
                        }
                    }
                    break;

                case InternalStates.Fire:
                    {
                        foreach (var m in this.Markers)
                            m.WantState = -1;

                        if (!this.CheckCosts(plr, true, true))
                        {
                            this.State = InternalStates.None;
                            if (this.HotkeyState != 0)
                                this.HotkeyState = -1;
                            return;
                        }

                        this.ApplyIMod(plr);
                        this.PlaySound(plr);

                        float distance = this.TargetTeleportPoint.GetDistance(this.SourceTeleportPoint);
                        float speed = this.Plugin.Settings.TeleportSpeed;
                        this.CurrentDistortionTime = 0.0f;
                        this.CurrentTeleportTime = 0.0f;
                        float maxDistance = Math.Max(100.0f, this.Plugin.Settings.MaxDistance);
                        this.CurrentMaxDistort = MaxTargetDistort * (distance / maxDistance);
                        if (speed > 0.0f)
                        {
                            if (speed < 1.0f)
                                speed = 1.0f;

                            float time = distance / speed;
                            if (time > 0.0f)
                            {
                                this.TimedValues[(int)TimedValueTypes.TeleportDoneRatio] = new List<TimedValue>() { new TimedValue(time, 0, 1) };
                                this.CurrentDistortionTime = -time;
                                this.CurrentTeleportTime = maxDistance / speed; // time;
                            }
                        }

                        this.State = InternalStates.Teleporting;
                    }
                    break;

                case InternalStates.Teleporting:
                    {
                        var tm = this.TimedValues[(int)TimedValueTypes.TeleportDoneRatio];
                        float ratio = 1.0f;
                        if (tm != null && tm.Count != 0)
                        {
                            tm[0].Update(diff);
                            ratio = tm[0].CurrentValue;
                            if (tm[0].IsFinished)
                                this.TimedValues[(int)TimedValueTypes.TeleportDoneRatio] = null;
                        }

                        this.CurrentTeleportPoint.X = (this.TargetTeleportPoint.X - this.SourceTeleportPoint.X) * ratio + this.SourceTeleportPoint.X;
                        this.CurrentTeleportPoint.Y = (this.TargetTeleportPoint.Y - this.SourceTeleportPoint.Y) * ratio + this.SourceTeleportPoint.Y;
                        this.CurrentTeleportPoint.Z = (this.TargetTeleportPoint.Z - this.SourceTeleportPoint.Z) * ratio + this.SourceTeleportPoint.Z;

                        Memory.InvokeCdecl(this.fn_Actor_SetPosition, plr.Address, this.CurrentTeleportPoint.Address, 1);
                        if (ratio >= 1.0f)
                            this.State = InternalStates.None;
                    }
                    break;
            }
        }

        private float[] GetCollisionPointFromCamera(NiNode cameraNode, TESObjectCELL cell, NiAVObject[] ignore)
        {
            float[] source = new float[3];
            float[] target = new float[3];

            var nodePos = cameraNode.WorldTransform.Position;
            source[0] = nodePos.X;
            source[1] = nodePos.Y;
            source[2] = nodePos.Z;

            cameraNode.WorldTransform.Translate(this.AimVectorPointDoubled, this.TargetTeleportPoint);

            target[0] = this.TargetTeleportPoint.X;
            target[1] = this.TargetTeleportPoint.Y;
            target[2] = this.TargetTeleportPoint.Z;

            var ray = this.DoRayCast(cell, source, target);
            var best = this.GetBestResult(source, ray, ignore, false);
            if (best == null)
                return target;
            return best.Position;
        }
        
        private void UpdateAiming(float diff, float totalTime, PlayerCharacter plr, TESObjectCELL cell)
        {
            MarkerData currentMarker = null;
            if(this.Markers.Count != 0)
                currentMarker = this.Markers[this.Markers.Count - 1];
            if (currentMarker == null || currentMarker.Object == null || currentMarker.WantState != 0)
                return;

            var camera = PlayerCamera.Instance;
            if (camera == null)
                return;

            var cameraNode = camera.Node;
            if (cameraNode == null)
                return;

            NiNode[] playerNode = new NiNode[2]
            {
                plr.GetSkeletonNode(false),
                plr.GetSkeletonNode(true)
            };

            for(int i = 0; i < playerNode.Length; i++)
            {
                if (playerNode[i] == null)
                    return;
            }

            {
                var plrPos = plr.Position;
                this.SourceMovePoint.X = plrPos.X;
                this.SourceMovePoint.Y = plrPos.Y;
                this.SourceMovePoint.Z = plrPos.Z;
            }

            if (camera.State != null && camera.State.Id == TESCameraStates.FirstPerson)
            {
                var plrPos = cameraNode.WorldTransform.Position;
                this.SourceTeleportPoint.X = plrPos.X;
                this.SourceTeleportPoint.Y = plrPos.Y;
                this.SourceTeleportPoint.Z = plrPos.Z;

                cameraNode.WorldTransform.Translate(this.AimVectorPoint, this.TargetTeleportPoint);
            }
            else
            {
                var headNode = playerNode[0].LookupNodeByName("NPC Head [Head]");
                if (headNode != null)
                {
                    var plrPos = headNode.WorldTransform.Position;
                    this.SourceTeleportPoint.X = plrPos.X;
                    this.SourceTeleportPoint.Y = plrPos.Y;
                    this.SourceTeleportPoint.Z = plrPos.Z;
                }
                else
                {
                    var plrPos = plr.Position;
                    this.SourceTeleportPoint.X = plrPos.X;
                    this.SourceTeleportPoint.Y = plrPos.Y;
                    this.SourceTeleportPoint.Z = plrPos.Z + this.Plugin.Settings.PlayerRadius;
                }

                var cameraCol = this.GetCollisionPointFromCamera(cameraNode, cell, playerNode);
                byte[] data = Memory.ReadBytes(cameraNode.WorldTransform.Address, 0x34);
                Memory.WriteBytes(this.ThirdPersonTempTransform.Address, data);
                this.TargetTeleportPoint.X = cameraCol[0];
                this.TargetTeleportPoint.Y = cameraCol[1];
                this.TargetTeleportPoint.Z = cameraCol[2];
                this.ThirdPersonTempTransform.LookAt(this.TargetTeleportPoint);
                this.ThirdPersonTempTransform.Translate(this.AimVectorPoint, this.TargetTeleportPoint);
            }

            bool allowWallClimb = true;
            {
                var source = new[] { this.SourceTeleportPoint.X, this.SourceTeleportPoint.Y, this.SourceTeleportPoint.Z };
                var target = new[] { this.TargetTeleportPoint.X, this.TargetTeleportPoint.Y, this.TargetTeleportPoint.Z };
                var ray = this.DoRayCast(cell, source, target);

                float[] collisionPosition = null;
                float[] collisionNormal = null;

                var best = this.GetBestResult(source, ray, playerNode, false);
                if(best == null)
                {
                    collisionPosition = target;

                    float[] revNormal = new float[3];
                    float rx = target[0] - source[0];
                    float ry = target[1] - source[1];
                    float rz = target[2] - source[2];
                    float rd = (float)Math.Sqrt(rx * rx + ry * ry + rz * rz);
                    if(rd > 0.0f)
                    {
                        rx /= rd;
                        ry /= rd;
                        rz /= rd;
                    }
                    revNormal[0] = rx * -1.0f;
                    revNormal[1] = ry * -1.0f;
                    revNormal[2] = rz * -1.0f;
                    collisionNormal = revNormal;
                    allowWallClimb = false;
                }
                else
                {
                    collisionPosition = best.Position;
                    collisionNormal = best.Normal;

                    float nlen = Length(collisionNormal);
                    if (nlen > 0.0f)
                    {
                        collisionNormal[0] /= nlen;
                        collisionNormal[1] /= nlen;
                        collisionNormal[2] /= nlen;
                    }

                    if (!this.Plugin.Settings.AllowLedgeClimbNPC)
                    {
                        var hkObj = best.HavokObject;
                        if (hkObj != IntPtr.Zero)
                        {
                            var layer = (CollisionLayers)(Memory.ReadUInt32(hkObj + 0x2C) & 0x7F);
                            switch (layer)
                            {
                                case CollisionLayers.LivingAndDeadActors:
                                case CollisionLayers.BipedNoCC:
                                case CollisionLayers.CharController:
                                    allowWallClimb = false;
                                    break;
                            }
                        }
                    }
                }
                
                float fullDist = Distance(collisionPosition, source);
                float ratioInc = 1.0f;
                if (fullDist > 0.0f)
                    ratioInc = Math.Max(10.0f, this.Plugin.Settings.TeleportIncrementalCheck) / fullDist;
                ratioInc = Math.Max(0.02f, ratioInc);

                float ratioNow = 1.0f;
                float[] checkPos = new float[3];
                bool ok = false;
                while(ratioNow > 0.0f)
                {
                    for(int i = 0; i < 3; i++)
                        checkPos[i] = (collisionPosition[i] - source[i]) * ratioNow + source[i];

                    float[] tpPos = null;
                    float[] mrPos = null;
                    if(this.CalculatePositionFromCollision(checkPos, collisionNormal, ref tpPos, ref mrPos, playerNode, cell, allowWallClimb))
                    {
                        this.TargetTeleportPoint.X = tpPos[0];
                        this.TargetTeleportPoint.Y = tpPos[1];
                        this.TargetTeleportPoint.Z = tpPos[2];

                        this.TargetMarkerPoint.X = mrPos[0];
                        this.TargetMarkerPoint.Y = mrPos[1];
                        this.TargetMarkerPoint.Z = mrPos[2];

                        ok = true;
                        break;
                    }

                    ratioNow -= ratioInc;
                }

                if(!ok)
                {
                    this.TargetTeleportPoint.X = this.SourceMovePoint.X;
                    this.TargetTeleportPoint.Y = this.SourceMovePoint.Y;
                    this.TargetTeleportPoint.Z = this.SourceMovePoint.Z;

                    this.TargetMarkerPoint.X = this.SourceMovePoint.X;
                    this.TargetMarkerPoint.Y = this.SourceMovePoint.Y;
                    this.TargetMarkerPoint.Z = this.SourceMovePoint.Z + this.Plugin.Settings.PlayerRadius;
                }
            }

            var mpos = currentMarker.Object.LocalTransform.Position;
            mpos.X = this.TargetMarkerPoint.X;
            mpos.Y = this.TargetMarkerPoint.Y;
            mpos.Z = this.TargetMarkerPoint.Z;

            currentMarker.UpdateObject(totalTime);
        }

        private static float Length(float[] vec)
        {
            float dx = vec[0];
            float dy = vec[1];
            float dz = vec[2];
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        private static float Distance(float[] a, float[] b)
        {
            float[] c = new[]
            {
                b[0] - a[0],
                b[1] - a[1],
                b[2] - a[2]
            };
            return Length(c);
        }

        private bool CalculatePositionFromCollision(float[] colPos, float[] colNormal, ref float[] teleportPos, ref float[] markerPos, NiAVObject[] ignore, TESObjectCELL cell, bool allowWallClimb)
        {
            // Calculate the position.
            var ntype = CalculateNormalType(colNormal);
            float tpush = 1.0f;
            bool canMoveDown = true;
            switch (ntype)
            {
                case NormalTypes.Up:
                    canMoveDown = false;
                    break;

                case NormalTypes.Down:
                    //tpush = this.Plugin.Settings.PlayerRadius * 2.0f;
                    break;

                case NormalTypes.Sideways:
                    {
                        tpush = this.Plugin.Settings.PlayerRadius;

                        var pushed = PushAway(colPos, colNormal, this.Plugin.Settings.PlayerRadius * 2.0f + 2.0f);
                        var pushedSource = PushAway(colPos, colNormal, 0.5f);
                        if (!this.CheckRay(pushedSource, pushed, ignore, cell))
                            return false;
                    }
                    break;

                case NormalTypes.Diagonal:
                    /*if (colNormal[2] >= 0.0f)
                        tpush = this.Plugin.Settings.PlayerRadius * 0.66f;
                    else
                        tpush = this.Plugin.Settings.PlayerRadius * 1.33f;*/
                    break;
            }
            var tpos = PushAway(colPos, colNormal, tpush);

            // Try to climb a wall.
            bool skipSnap = false;
            bool skipHeight = false;
            if (allowWallClimb)
            {
                float wallClimb = this.Plugin.Settings.MaxWallClimbHeight;
                if (ntype == NormalTypes.Sideways && wallClimb > 0.0f/* && tpos[2] > (this.SourceMovePoint.X - 40.0f)*/)
                {
                    var srcPos = tpos.ToArray();
                    var dstPos = srcPos.ToArray();
                    dstPos[2] += wallClimb;

                    float dist = QuickRay(srcPos, dstPos, ignore, cell);
                    float pheight = this.Plugin.Settings.PlayerRadius * 3.0f;
                    if (dist < 0.0f || dist > pheight)
                    {
                        var colNormalRev = colNormal.ToArray();
                        for (int i = 0; i < colNormalRev.Length; i++)
                            colNormalRev[i] *= -1.0f;

                        if (dist < 0.0f)
                            srcPos[2] += wallClimb;
                        else
                            srcPos[2] += (dist - 5.0f);

                        float width = this.Plugin.Settings.WallClimbWidth;
                        dstPos = PushAway(srcPos, colNormalRev, width);
                        if (CheckRay(srcPos, dstPos, ignore, cell))
                        {
                            srcPos = PushAway(srcPos, colNormalRev, width - this.Plugin.Settings.PlayerRadius);
                            dstPos = srcPos.ToArray();
                            dstPos[2] -= wallClimb;

                            float dist2 = QuickRay(srcPos, dstPos, ignore, cell);
                            if (dist2 >= 0.0f && (srcPos[2] - dist2) > (tpos[2] - 40.0f))
                            {
                                srcPos[2] -= (dist2 - 1.0f);

                                // Make sure there's enough height for player to exist there.
                                bool climb = true;
                                {
                                    dstPos = srcPos.ToArray();
                                    dstPos[2] += pheight;

                                    float dist3 = QuickRay(srcPos, dstPos, ignore, cell);
                                    if (dist3 >= 0.0f)
                                    {
                                        float moveDown = pheight - dist3;
                                        dstPos[2] = srcPos[2] - moveDown;
                                        if (!CheckRay(srcPos, dstPos, ignore, cell))
                                            climb = false;
                                        else
                                            srcPos[2] -= moveDown;
                                    }
                                }

                                if (climb)
                                {
                                    tpos = srcPos.ToArray();
                                    skipSnap = true;
                                    skipHeight = true;
                                }
                            }
                        }
                    }
                }
            }

            // Make sure there's enough height for player to exist there.
            if(!skipHeight)
            {
                float pheight = this.Plugin.Settings.PlayerRadius * 3.0f;
                var srcPos = tpos.ToArray();
                var dstPos = srcPos.ToArray();
                dstPos[2] += pheight;

                float dist = QuickRay(srcPos, dstPos, ignore, cell);
                if (dist >= 0.0f)
                {
                    if (!canMoveDown)
                        return false;

                    float moveDown = pheight - dist;
                    dstPos[2] = srcPos[2] - moveDown;
                    if (!CheckRay(srcPos, dstPos, ignore, cell))
                        return false;

                    tpos[2] -= moveDown;
                }
            }

            // Try to snap to ground.
            float maxSnap = this.Plugin.Settings.MaxSnapToGroundDistance;
            if(!skipSnap && maxSnap > 0.0f && canMoveDown)
            {
                var srcPos = tpos.ToArray();
                var dstPos = srcPos.ToArray();
                dstPos[2] -= maxSnap;

                float dist = QuickRay(srcPos, dstPos, ignore, cell);
                if(dist >= 0.0f)
                {
                    float reduce = dist - 1.0f;
                    tpos[2] -= reduce;
                }
            }

            teleportPos = tpos;
            float[] mpos = tpos.ToArray();
            mpos[2] += this.Plugin.Settings.PlayerRadius;
            markerPos = mpos;
            return true;
        }

        private enum NormalTypes : int
        {
            Sideways,
            Up,
            Down,
            Diagonal
        }

        private NormalTypes CalculateNormalType(float[] normal)
        {
            float[] two = new[] { Math.Max(normal[0], normal[1]), normal[2] };
            float len = (float)Math.Sqrt(two[0] * two[0] + two[1] * two[1]);
            if (len > 0.0f)
            {
                two[0] /= len;
                two[1] /= len;
            }

            if (Math.Abs(two[0]) == 1.0f)
                return NormalTypes.Sideways;
            if (Math.Abs(two[1]) == 1.0f)
                return two[1] > 0.0f ? NormalTypes.Up : NormalTypes.Down;

            double angle = Math.Atan2(two[1], two[0]) * 180.0 / Math.PI;
            if (angle < 0.0)
                angle += 180.0;
            if (angle <= 30.0 || angle >= 150.0)
                return NormalTypes.Sideways;
            if (angle <= 60.0 || angle >= 120.0)
                return NormalTypes.Diagonal;
            return two[1] >= 0.0f ? NormalTypes.Up : NormalTypes.Down;
        }

        private bool CheckRay(float[] source, float[] target, NiAVObject[] ignore, TESObjectCELL cell)
        {
            var ray = this.DoRayCast(cell, source, target);
            return this.GetBestResult(source, ray, ignore, true) == null;
        }

        private float QuickRay(float[] source, float[] target, NiAVObject[] ignore, TESObjectCELL cell)
        {
            var ray = this.DoRayCast(cell, source, target);
            var best = this.GetBestResult(source, ray, ignore, false);
            if (best == null)
                return -1.0f;

            return Distance(best.Position, source);
        }

        private float[] PushAway(float[] pos, float[] normal, float amount)
        {
            float[] result = new float[3];
            for (int i = 0; i < 3; i++)
                result[i] = pos[i] + normal[i] * amount;
            return result;
        }

        private void InterruptCast(PlayerCharacter plr, SpellItem spell)
        {
            if (plr == null || spell == null)
                return;

            for (int i = 0; i <= 2; i++)
            {
                var caster = plr.GetMagicCaster((EquippedSpellSlots)i);
                if (caster == null)
                    continue;

                var item = caster.CastItem;
                if (item == null || !item.Equals(spell))
                    continue;

                plr.InterruptCast();
                return;
            }
        }
        
        private MagicCastingStates GetCurrentCastingState(PlayerCharacter plr, SpellItem spell)
        {
            if (plr == null)
                return MagicCastingStates.None;

            if (this.HotkeyState > 0)
                return MagicCastingStates.Charged;

            if (spell == null)
                return MagicCastingStates.None;

            for(int i = 0; i <= 2; i++)
            {
                var caster = plr.GetMagicCaster((EquippedSpellSlots)i);
                if (caster == null)
                    continue;

                var item = caster.CastItem;
                if (item == null || !item.Equals(spell))
                    continue;

                return caster.State;
            }

            return MagicCastingStates.None;
        }

        internal void Initialize()
        {
            var alloc = Memory.Allocate(0x110);
            alloc.Pin();

            this.TargetMarkerPoint = MemoryObject.FromAddress<NiPoint3>(alloc.Address);
            this.TargetTeleportPoint = MemoryObject.FromAddress<NiPoint3>(alloc.Address + 0x10);
            this.AimVectorPoint = MemoryObject.FromAddress<NiPoint3>(alloc.Address + 0x20);
            this.SourceTeleportPoint = MemoryObject.FromAddress<NiPoint3>(alloc.Address + 0x30);
            this.CurrentTeleportPoint = MemoryObject.FromAddress<NiPoint3>(alloc.Address + 0x40);
            this.AimVectorPointDoubled = MemoryObject.FromAddress<NiPoint3>(alloc.Address + 0x50);
            this.ThirdPersonTempTransform = MemoryObject.FromAddress<NiTransform>(alloc.Address + 0x60);
            this.SourceMovePoint = MemoryObject.FromAddress<NiPoint3>(alloc.Address + 0x100);

            this.AimVectorPoint.X = 0.0f;
            this.AimVectorPoint.Z = 0.0f;
            this.AimVectorPoint.Y = Math.Max(100.0f, Math.Min(8000.0f, this.Plugin.Settings.MaxDistance));

            this.AimVectorPointDoubled.X = 0.0f;
            this.AimVectorPointDoubled.Z = 0.0f;
            this.AimVectorPointDoubled.Y = 2000.0f + Math.Max(100.0f, Math.Min(8000.0f, this.Plugin.Settings.MaxDistance));
            
            this.fn_Actor_SetPosition = NetScriptFramework.Main.GameInfo.GetAddressOf(36319);
            this.fn_BGSSoundDescriptor_PlaySound = NetScriptFramework.Main.GameInfo.GetAddressOf(32301);
            this.fn_TESImageSpaceModifier_Apply = NetScriptFramework.Main.GameInfo.GetAddressOf(18185);
            this.fn_FlashHudMeter = NetScriptFramework.Main.GameInfo.GetAddressOf(51907);
            this.fn_GetMagicFailedMessage = NetScriptFramework.Main.GameInfo.GetAddressOf(11295);

            /*var cost = this.Plugin.Settings.MagickaCost;
            if (cost > 0.0f)
            {
                var spell = this.Plugin.Settings.SpellForm;
                if (spell != null)
                {
                    var effectItem = Memory.ReadPointer(spell.Address + 0x58);
                    if (effectItem != IntPtr.Zero)
                    {
                        effectItem = Memory.ReadPointer(effectItem);
                        if (effectItem != IntPtr.Zero)
                        {
                            var effect = MemoryObject.FromAddress<EffectSetting>(Memory.ReadPointer(effectItem + 0x10));
                            if (effect != null)
                                Memory.WriteFloat(effect.Address + 0x6C, cost);

                            Memory.WriteFloat(effectItem + 0x18, cost);
                        }
                    }

                    Memory.WriteInt32(spell.Address + 0xC0, (int)(cost + 0.1f));
                }
            }*/
        }
        
        private bool CheckCollidableObject(NiAVObject obj, IntPtr havokObj, NiAVObject[] ignore)
        {
            if(havokObj != IntPtr.Zero)
            {
                uint flags = Memory.ReadUInt32(havokObj + 0x2C) & 0x7F;
                ulong mask = (ulong)1 << (int)flags;
                if ((this.RaycastMask & mask) == 0)
                    return false;
            }

            if (obj == null)
                return true;

            if(ignore != null)
            {
                for(int i = 0; i < ignore.Length; i++)
                {
                    var o = ignore[i];
                    if (o != null && o.Equals(obj))
                        return false;
                }
            }
            
            return true;
        }

        private RayCastResult GetBestResult(float[] source, List<RayCastResult> ls, NiAVObject[] ignore, bool any)
        {
            List<Tuple<RayCastResult, float>> all = new List<Tuple<RayCastResult, float>>();
            if(ls != null)
            {
                foreach(var x in ls)
                {
                    if(any)
                    {
                        if (this.CheckCollidableObject(x.Object, x.HavokObject, ignore))
                            return x;
                        continue;
                    }

                    float[] other = x.Position;
                    float dx = other[0] - source[0];
                    float dy = other[1] - source[1];
                    float dz = other[2] - source[2];
                    float d = dx * dx + dy * dy + dz * dz;
                    all.Add(new Tuple<RayCastResult, float>(x, d));
                }
            }

            if(all.Count > 1)
                all.Sort((u, v) => u.Item2.CompareTo(v.Item2));

            foreach(var t in all)
            {
                var r = t.Item1;
                if (this.CheckCollidableObject(r.Object, r.HavokObject, ignore))
                    return r;
            }

            return null;
        }
    }

    internal class TimedValue
    {
        internal TimedValue(float totalTime, float beginValue, float endValue)
        {
            this.TotalTime = totalTime;
            this.ElapsedTime = 0.0f;
            this.BeginValue = beginValue;
            this.EndValue = endValue;
            this.CurrentValue = this.BeginValue;
        }

        internal readonly float TotalTime;
        internal float ElapsedTime
        {
            get;
            private set;
        }
        internal readonly float BeginValue;
        internal readonly float EndValue;
        internal float CurrentValue
        {
            get;
            private set;
        }

        internal void Update(float diff)
        {
            this.ElapsedTime += diff;

            float ratio = 1.0f;
            if(this.TotalTime > 0.0f)
                ratio = Math.Min(1.0f, this.ElapsedTime / this.TotalTime);

            this.CurrentValue = (this.EndValue - this.BeginValue) * ratio + this.BeginValue;
        }

        internal bool IsFinished
        {
            get
            {
                return this.ElapsedTime >= this.TotalTime;
            }
        }
    }
}
