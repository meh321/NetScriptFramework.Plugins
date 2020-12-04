using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework;
using NetScriptFramework.SkyrimSE;

namespace GamePlayTweaks.Mods
{
    class StaminaChanges : Mod
    {
        public StaminaChanges()
        {
            settings.runStuffEnabled = this.CreateSettingBool("RunStuffEnabled", true, "Easy to way to enable or disable all the stamina run stuff.");
            settings.runStamina = this.CreateSettingFloat("RunningStamina", 1.0f, "Running (not sprinting) uses this much stamina per second. While stamina is being used the stamina regeneration is also blocked. If 0 then running does not use any stamina.");
            settings.runBlock = this.CreateSettingBool("BlockRunningWhenNoStamina", true, "Don't allow running to work if completely out of stamina. This will work same way as if you were over-encumbered.");
            settings.runResume = this.CreateSettingFloat("RunningResumePercent", 50, "Resume running again if stamina per cent has regenerated above this amount. Set 0 to disable.");
            settings.runDelayMin = this.CreateSettingFloat("RunningRegenDelayMin", 1, "Minimum amount of time to delay stamina regeneration when running. Actual delay depends on how long player ran, max is used when running for max-delay seconds or longer.");
            settings.runDelayMax = this.CreateSettingFloat("RunningRegenDelayMax", 10, "Maximum amount of time to delay stamina regeneration when running. Actual delay depends on how long player ran, max is used when running for max-delay seconds or longer.");
            settings.runMaxTime = this.CreateSettingFloat("RunningRegenDelayMaxTime", 20, "The time you must spend running to reach the max delay.");

            settings.jumpStuffEnabled = this.CreateSettingBool("JumpStuffEnabled", true, "Easy way to enable or disable all the jump stuff.");
            settings.jumpStamina = this.CreateSettingFloat("JumpingStamina", 20, "Jumping uses this much stamina.");
            settings.jumpBlock = this.CreateSettingBool("BlockJumpingWhenNoStamina", true, "Don't allow jumping to work when completely out of stamina.");
            settings.jumpDelay = this.CreateSettingFloat("JumpingRegenDelay", 10, "How long to delay stamina regeneration when jumping.");

            settings.sneakStuffEnabled = this.CreateSettingBool("SneakStuffEnabled", true, "Easy way to enable or disable all the sneak stuff.");
            settings.sneakIdleStamina = this.CreateSettingFloat("SneakIdleStamina", 0.2, "How much stamina is used per second when you are sneaking and not moving.");
            settings.sneakWalkStamina = this.CreateSettingFloat("SneakWalkStamina", 0.5, "How much stamina is used per second when you are sneaking and walking slowly.");
            settings.sneakRunStamina = this.CreateSettingFloat("SneakRunStamina", 1, "How much stamina is used per second when you are sneaking and moving quickly.");
            settings.sneakBlock = this.CreateSettingBool("BlockSneakingWhenNoStamina", true, "Don't allow sneaking at all when out of stamina.");

            settings.combatRegenBlock = this.CreateSettingBool("BlockStaminaRegenInCombat", true, "Don't allow stamina to regenerate in combat.");
            settings.combatRegenBlock2 = this.CreateSettingBool("BlockMagickaRegenInCombat", true, "Don't allow magicka to regenerate in combat.");

            settings.invisStuffEnabled = this.CreateSettingBool("InvisStuffEnabled", true, "Easy way to enable or disable all the invis stuff.");
            settings.invisIdleMagicka = this.CreateSettingFloat("InvisIdleMagicka", 1, "How much magicka is used per second while invisibility spell is on.");
            settings.invisWalkMagicka = this.CreateSettingFloat("InvisWalkMagicka", 2, "How much magicka is used per second while invisible and moving slowly.");
            settings.invisRunMagicka = this.CreateSettingFloat("InvisRunMagicka", 5, "How much magicka is used per second while invisible and moving quickly.");
            settings.invisBlock = this.CreateSettingBool("InvisRemoveWhenOutOfMagicka", true, "Remove invisibility effects when out of magicka.");

            settings.customEffectStuffEnabled = this.CreateSettingBool("CustomEffectStuffEnabled", true, "Easy way to enable or disable all the custom effect stuff.");
            settings.customEffectKeywords = this.CreateSettingString("CustomEffectKeywords", "", "Keywords for custom magicka drain effect. These are keywords that must be on the magic effect itself.");
            settings.customIdleMagicka = this.CreateSettingFloat("CustomEffectIdleMagicka", 1, "How much magicka is used per second while custom effect is on.");
            settings.customWalkMagicka = this.CreateSettingFloat("CustomEffectWalkMagicka", 2, "How much magicka is used per second while custom effect is on and moving slowly.");
            settings.customRunMagicka = this.CreateSettingFloat("CustomEffectRunMagicka", 5, "How much magicka is used per second while custom effect is on and running.");
            settings.customBlockMagicka = this.CreateSettingBool("CustomEffectBlock", true, "Remove the custom effects with specified keywords when out of magicka.");

            settings.fleshStuffEnabled = this.CreateSettingBool("FleshEffectStuffEnabled", true, "Easy way to enable or disable all the stoneflesh spell type effects.");
            settings.fleshEffectKeywords = this.CreateSettingString("FleshEffectKeywords", "MagicArmorSpell", "The keywords for stoneflesh type spells.");
            settings.fleshHurtMagickaPerDamage = this.CreateSettingFloat("FleshEffectMagickaPerHurt", 1, "How much magicka to drain per damage taken. For example 0.5 means take 10 magicka for every 20 points of damage taken.");
            settings.blockFleshMagicka = this.CreateSettingBool("FleshEffectBlock", true, "Remove all stoneflesh type spells when out of magicka and taking damage?");

            settings.disableStaminaStuff = this.CreateSettingBool("DisableStaminaStuff", false, "Disable all the stamina stuff from here.");
            settings.disableMagickaStuff = this.CreateSettingBool("DisableMagickaStuff", false, "Disable all the magicka stuff from here.");
        }

        internal override string Description
        {
            get
            {
                return "Adds custom options to how stamina is handled.";
            }
        }

        private static class settings
        {
            internal static SettingValue<bool> runStuffEnabled;
            internal static SettingValue<double> runStamina;
            internal static SettingValue<double> runResume;
            internal static SettingValue<double> runMaxTime;
            internal static SettingValue<bool> runBlock;
            internal static SettingValue<double> runDelayMin;
            internal static SettingValue<double> runDelayMax;

            internal static SettingValue<bool> jumpStuffEnabled;
            internal static SettingValue<double> jumpStamina;
            internal static SettingValue<bool> jumpBlock;
            internal static SettingValue<double> jumpDelay;

            internal static SettingValue<bool> combatRegenBlock;
            internal static SettingValue<bool> combatRegenBlock2;

            internal static SettingValue<bool> sneakStuffEnabled;
            internal static SettingValue<double> sneakIdleStamina;
            internal static SettingValue<double> sneakWalkStamina;
            internal static SettingValue<double> sneakRunStamina;
            internal static SettingValue<bool> sneakBlock;

            internal static SettingValue<bool> invisStuffEnabled;
            internal static SettingValue<double> invisIdleMagicka;
            internal static SettingValue<double> invisWalkMagicka;
            internal static SettingValue<double> invisRunMagicka;
            internal static SettingValue<bool> invisBlock;

            internal static SettingValue<bool> customEffectStuffEnabled;
            internal static SettingValue<string> customEffectKeywords;
            internal static SettingValue<double> customIdleMagicka;
            internal static SettingValue<double> customWalkMagicka;
            internal static SettingValue<double> customRunMagicka;
            internal static SettingValue<bool> customBlockMagicka;

            internal static SettingValue<bool> fleshStuffEnabled;
            internal static SettingValue<string> fleshEffectKeywords;
            internal static SettingValue<double> fleshHurtMagickaPerDamage;
            internal static SettingValue<bool> blockFleshMagicka;

            internal static SettingValue<bool> disableMagickaStuff;
            internal static SettingValue<bool> disableStaminaStuff;
        }

        private static IntPtr Addr_FlashHudMeter;
        private static IntPtr Addr_SetAVRegenDelay;
        private static IntPtr Addr_TimeSinceFrame;
        private static IntPtr Addr_StaminaRegenDelayMax;
        private static IntPtr Addr_MagickaRegenDelayMax;

        private static readonly float EmptyValue = 0.01f;

        private float _lastStamina = 99999.0f;
        private long _nextFlashBarTime = 0;
        private long _nextFlashBarTime2 = 0;
        private NetScriptFramework.Tools.Timer _timer = new NetScriptFramework.Tools.Timer();
        private float _runTimer = 0.0f;
        private bool _wantResumeRunning = false;
        private float _lastDamagedHp = 0.0f;

        private bool IsEmptyStamina(Actor actor)
        {
            if (actor == null)
                return false;

            return actor.GetActorValue(ActorValueIndices.Stamina) < EmptyValue;
        }

        private bool IsEmptyMagicka(Actor actor)
        {
            if (actor == null)
                return false;

            return actor.GetActorValue(ActorValueIndices.Magicka) < EmptyValue;
        }

        private void FlashStaminaBar()
        {
            long now = _timer.Time;
            if (now < _nextFlashBarTime)
                return;

            _nextFlashBarTime = now + 2000;
            Memory.InvokeCdecl(Addr_FlashHudMeter, (int)ActorValueIndices.Stamina);
        }

        private void FlashMagickaBar()
        {
            long now = _timer.Time;
            if (now < _nextFlashBarTime2)
                return;

            _nextFlashBarTime2 = now + 2000;
            Memory.InvokeCdecl(Addr_FlashHudMeter, (int)ActorValueIndices.Magicka);
        }

        private void SetStaminaRegenDelay(Actor actor, float amount)
        {
            if (actor == null)
                return;

            var process = actor.Process;
            if (process == null)
                return;

            Memory.InvokeCdecl(Addr_SetAVRegenDelay, process.Cast<ActorProcess>(), (int)ActorValueIndices.Stamina, amount);
        }

        private void SetMagickaRegenDelay(Actor actor, float amount)
        {
            if (actor == null)
                return;

            var process = actor.Process;
            if (process == null)
                return;

            Memory.InvokeCdecl(Addr_SetAVRegenDelay, process.Cast<ActorProcess>(), (int)ActorValueIndices.Magicka, amount);
        }

        private void SetRunningEnabled(bool enabled)
        {
            var pcont = PlayerControls.Instance.Cast<PlayerControls>();
            byte wantMode = enabled ? (byte)1 : (byte)0;
            byte runMode = Memory.ReadUInt8(pcont + 73);
            if (runMode != wantMode)
                Memory.WriteUInt8(pcont + 73, wantMode);

            this._wantResumeRunning = !enabled;
        }

        private void UpdateResumeRunning(Actor actor)
        {
            if (actor == null)
                return;

            float nowStamina = actor.GetActorValue(ActorValueIndices.Stamina);
            float maxStamina = actor.GetActorValueMax(ActorValueIndices.Stamina);

            if (maxStamina <= 0.0f)
                return;

            float ratio = nowStamina * 100.0f / maxStamina;
            float resume = (float)settings.runResume.Value;
            if(resume > 0.0f && resume <= 100.0f)
            {
                if (_lastStamina < resume && ratio >= resume && this._wantResumeRunning)
                    this.SetRunningEnabled(true);
            }

            _lastStamina = ratio;
        }

        private bool SpendStamina(Actor actor, float amount, float delay, bool allowNotEnough, modes m)
        {
            if (actor == null)
                return false;

            if (delay > 1.0f)
            {
                float curMaxDelay = Memory.ReadFloat(Addr_StaminaRegenDelayMax);
                if (delay > curMaxDelay)
                    Memory.WriteFloat(Addr_StaminaRegenDelayMax, delay);
            }
            
            if(!allowNotEnough)
            {
                float current = actor.GetActorValue(ActorValueIndices.Stamina);
                if (current < amount)
                {
                    this.OnDepletedStamina(actor, m);
                    return false;
                }

                if (delay > 0.0f)
                    this.SetStaminaRegenDelay(actor, delay);
                actor.DamageActorValue(ActorValueIndices.Stamina, -amount);
            }
            else
            {
                if(IsEmptyStamina(actor))
                {
                    this.OnDepletedStamina(actor, m);
                    return false;
                }

                float current = actor.GetActorValue(ActorValueIndices.Stamina);
                if (delay > 0.0f)
                    this.SetStaminaRegenDelay(actor, delay);
                if (current < amount)
                    actor.DamageActorValue(ActorValueIndices.Stamina, -current);
                else
                    actor.DamageActorValue(ActorValueIndices.Stamina, -amount);

                if(IsEmptyStamina(actor))
                    this.OnDepletedStamina(actor, m);
            }

            return true;
        }

        private bool SpendMagicka(Actor actor, float amount, float delay, bool allowNotEnough, modes m)
        {
            if (actor == null)
                return false;

            if (delay > 1.0f)
            {
                float curMaxDelay = Memory.ReadFloat(Addr_MagickaRegenDelayMax);
                if (delay > curMaxDelay)
                    Memory.WriteFloat(Addr_MagickaRegenDelayMax, delay);
            }

            if (!allowNotEnough)
            {
                float current = actor.GetActorValue(ActorValueIndices.Magicka);
                if (current < amount)
                {
                    this.OnDepletedMagicka(actor, m);
                    return false;
                }

                if (delay > 0.0f)
                    this.SetMagickaRegenDelay(actor, delay);
                actor.DamageActorValue(ActorValueIndices.Magicka, -amount);
            }
            else
            {
                if (IsEmptyMagicka(actor))
                {
                    this.OnDepletedMagicka(actor, m);
                    return false;
                }

                float current = actor.GetActorValue(ActorValueIndices.Magicka);
                if (delay > 0.0f)
                    this.SetMagickaRegenDelay(actor, delay);
                if (current < amount)
                    actor.DamageActorValue(ActorValueIndices.Magicka, -current);
                else
                    actor.DamageActorValue(ActorValueIndices.Magicka, -amount);

                if (IsEmptyMagicka(actor))
                    this.OnDepletedMagicka(actor, m);
            }

            return true;
        }

        private bool IsStaminaEnabled
        {
            get
            {
                return !settings.disableStaminaStuff.Value;
            }
        }

        private bool IsMagickaEnabled
        {
            get
            {
                return !settings.disableMagickaStuff.Value;
            }
        }

        private string[] flesh_kw;
        private string[] custom_kw;

        [Flags]
        private enum modes : uint
        {
            none = 0,

            combat = 1,

            run = 2,

            walk = 4,

            sneak = 8,

            invis = 0x10,

            custom = 0x20,

            jump = 0x40,

            flesh = 0x80,

            sprint = 0x100,
        }

        private float CalculateTakeStamina(float diff, modes mode)
        {
            if (!this.IsStaminaEnabled)
                return 0.0f;

            double high = 0.0;

            if(settings.sneakStuffEnabled.Value && (mode & modes.sneak) != modes.none)
            {
                if ((mode & (modes.run | modes.sprint)) != modes.none)
                    high = settings.sneakRunStamina.Value;
                else if ((mode & modes.walk) != modes.none)
                    high = settings.sneakWalkStamina.Value;
                else
                    high = settings.sneakIdleStamina.Value;
            }

            if (settings.runStuffEnabled.Value && (mode & modes.run) != modes.none)
                high = Math.Max(high, settings.runStamina.Value);

            return (float)high * diff;
        }

        private float CalculateTakeMagicka(PlayerCharacter plr, float diff, ref modes mode)
        {
            if (!this.IsMagickaEnabled)
                return 0.0f;

            double sum = 0.0;

            if(settings.invisStuffEnabled.Value)
            {
                double take;
                if ((mode & (modes.run | modes.sprint)) != modes.none)
                    take = settings.invisRunMagicka.Value;
                else if ((mode & modes.walk) != modes.none)
                    take = settings.invisWalkMagicka.Value;
                else
                    take = settings.invisIdleMagicka.Value;

                if (take > 0.0 && plr.FindFirstEffectWithArchetype(Archetypes.Invisibility, false) != null)
                {
                    sum += take * diff;
                    mode |= modes.invis;
                }
            }

            if(settings.customEffectStuffEnabled.Value && this.custom_kw != null)
            {
                double take;
                if ((mode & (modes.run | modes.sprint)) != modes.none)
                    take = settings.customRunMagicka.Value;
                else if ((mode & modes.walk) != modes.none)
                    take = settings.customWalkMagicka.Value;
                else
                    take = settings.customIdleMagicka.Value;

                MagicItem tmp = null;
                if(take > 0.0 && this.custom_kw.Any(q => plr.HasMagicEffectWithKeywordText(q, ref tmp)))
                {
                    sum += take * diff;
                    mode |= modes.custom;
                }
            }

            if (settings.fleshStuffEnabled.Value && this.flesh_kw != null)
            {
                double take = settings.fleshHurtMagickaPerDamage.Value;
                if (take > 0.0)
                {
                    float now = plr.GetActorValueMax(ActorValueIndices.Health) - plr.GetActorValue(ActorValueIndices.Health);
                    if (now > _lastDamagedHp)
                        take *= (now - _lastDamagedHp);
                    else
                        take = 0.0;
                    _lastDamagedHp = now;

                    if (take > 0.0)
                    {
                        MagicItem tmp = null;
                        if (this.flesh_kw.Any(q => plr.HasMagicEffectWithKeywordText(q, ref tmp)))
                        {
                            sum += take;
                            mode |= modes.flesh;
                        }
                    }
                }
            }

            return (float)sum;
        }

        private void UpdateFrame(FrameEventArgs e)
        {
            if (NetScriptFramework.SkyrimSE.Main.Instance.IsGamePaused)
                return;

            float diff = Memory.ReadFloat(Addr_TimeSinceFrame);
            if (diff > 0.0f)
            {
                var plr = PlayerCharacter.Instance;
                if (plr == null)
                    return;

                modes m = modes.none;
                if (plr.IsInCombat)
                    m |= modes.combat;
                if (plr.IsSneaking)
                    m |= modes.sneak;

                float spendStamina = 0.0f;
                float spendMagicka = 0.0f;
                float delayStamina = 0.0f;
                float delayMagicka = 0.0f;

                if ((m & modes.combat) != modes.none)
                {
                    if (this.IsStaminaEnabled && settings.combatRegenBlock.Value)
                        delayStamina = 0.1f;
                    if (this.IsMagickaEnabled && settings.combatRegenBlock2.Value)
                        delayMagicka = 0.1f;
                }

                {
                    uint flags = Memory.ReadUInt32(plr.Cast<Actor>() + 0xC0) & 0x3FFF;
                    bool mounted = plr.IsOnMount || plr.IsOnFlyingMount;

                    if((m & modes.sneak) == modes.none && (flags & 0x100) != 0) // sprint
                    {
                        if (!mounted)
                            m |= modes.sprint;
                    }

                    if ((flags & 0x180) == 0x80) // Running
                    {
                        if (!mounted)
                        {
                            var pcont = PlayerControls.Instance.Cast<PlayerControls>();
                            byte runMode = Memory.ReadUInt8(pcont + 73);
                            if (runMode != 0)
                                m |= modes.run;
                        }
                    }

                    if ((m & modes.run) == modes.none && (flags & 0x1C0) == 0x40) // Walking
                    {
                        if (!mounted)
                            m |= modes.walk;
                    }

                    if((m & (modes.run | modes.sprint)) != modes.none)
                    {
                        this._wantResumeRunning = false;
                        float maxTime = Math.Max(1.0f, (float)settings.runMaxTime.Value);

                        _runTimer += diff;

                        // This is needed when we lower runtimer later.
                        if (_runTimer > maxTime)
                            _runTimer = maxTime;
                    }
                    else
                    {
                        if (_runTimer > 0.0f)
                            _runTimer = Math.Max(0.0f, _runTimer - diff * 4.0f);
                    }
                }

                spendStamina += this.CalculateTakeStamina(diff, m);
                spendMagicka += this.CalculateTakeMagicka(plr, diff, ref m);

                if((m & modes.run) != modes.none && spendStamina > 0.0f)
                {
                    float maxTime = Math.Max(1.0f, (float)settings.runMaxTime.Value);
                    float runRatio = Math.Min(1.0f, _runTimer / maxTime);
                    delayStamina = Math.Max(delayStamina, (float)((settings.runDelayMax.Value - settings.runDelayMin.Value) * runRatio + settings.runDelayMin.Value));
                }

                if (spendMagicka > 0.0f)
                    delayMagicka = Math.Max(delayMagicka, 1.0f);

                if (spendStamina > 0.0f)
                    this.SpendStamina(plr, spendStamina, delayStamina, true, m);
                else if (delayStamina > 0.0f)
                    this.SetStaminaRegenDelay(plr, delayStamina);

                if (spendMagicka > 0.0f)
                    this.SpendMagicka(plr, spendMagicka, delayMagicka, true, m);
                else if (delayMagicka > 0.0f)
                    this.SetMagickaRegenDelay(plr, delayMagicka);

                if (this.IsStaminaEnabled && settings.runStuffEnabled.Value && settings.runBlock.Value)
                    this.UpdateResumeRunning(plr);
            }
        }

        private void OnDepletedStamina(Actor actor, modes m)
        {
            if (settings.runStuffEnabled.Value && settings.runBlock.Value)
                this.SetRunningEnabled(false);
            if (settings.sneakStuffEnabled.Value && settings.sneakBlock.Value && actor.IsSneaking)
                actor.IsSneaking = false;

            this.FlashStaminaBar();
        }

        private void OnDepletedMagicka(Actor actor, modes m)
        {
            if (settings.invisStuffEnabled.Value && settings.invisBlock.Value)
            {
                //if((m & modes.invis) != modes.none)
                actor.DispelEffectsWithArchetype(Archetypes.Invisibility, null);
            }

            if(settings.customEffectStuffEnabled.Value && this.custom_kw != null && settings.customBlockMagicka.Value)
            {
                //if((m & modes.custom) != modes.none)
                var all = actor.ActiveEffects.Where(q =>
                {
                    if (q.IsInactive)
                        return false;

                    var itm = q.BaseEffect;
                    if(itm != null)
                    {
                        if (this.custom_kw.Any(kw => itm.HasKeywordText(kw)))
                            return true;
                    }

                    return false;
                }).ToList();

                foreach (var eff in all)
                    eff.Dispel();
            }

            if(settings.fleshStuffEnabled.Value && settings.blockFleshMagicka.Value && this.flesh_kw != null)
            {
                //if((m & modes.flesh) != modes.none)
                var all = actor.ActiveEffects.Where(q =>
                {
                    if (q.IsInactive)
                        return false;

                    var itm = q.BaseEffect;
                    if (itm != null)
                    {
                        if (this.flesh_kw.Any(kw => itm.HasKeywordText(kw)))
                            return true;
                    }

                    return false;
                }).ToList();

                foreach (var eff in all)
                    eff.Dispel();
            }

            this.FlashMagickaBar();
        }

        private bool ShouldBlockJump()
        {
            if (!this.IsStaminaEnabled || !settings.jumpStuffEnabled.Value || !settings.jumpBlock.Value)
                return false;

            var plr = PlayerCharacter.Instance;
            if (plr == null)
                return false;

            if (IsEmptyStamina(plr))
                return true;

            return false;
        }

        private static bool _skippedJumpNow = false;

        internal override void Apply()
        {
            _timer.Start();

            flesh_kw = (settings.fleshEffectKeywords.Value ?? "").Split(new[] { ' ', '\t', ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (flesh_kw.Length == 0)
                flesh_kw = null;

            custom_kw = (settings.customEffectKeywords.Value ?? "").Split(new[] { ' ', '\t', ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (custom_kw.Length == 0)
                custom_kw = null;

            Addr_FlashHudMeter = NetScriptFramework.Main.GameInfo.GetAddressOf(51907);
            Addr_SetAVRegenDelay = NetScriptFramework.Main.GameInfo.GetAddressOf(38526); // 38525 is directly set
            Addr_TimeSinceFrame = NetScriptFramework.Main.GameInfo.GetAddressOf(516940);
            Addr_StaminaRegenDelayMax = NetScriptFramework.Main.GameInfo.GetAddressOf(503351, 8);
            Addr_MagickaRegenDelayMax = NetScriptFramework.Main.GameInfo.GetAddressOf(503353, 8);

            Events.OnFrame.Register(UpdateFrame);

            if (this.IsStaminaEnabled && settings.jumpStuffEnabled.Value && (settings.jumpBlock.Value || settings.jumpStamina.Value > 0.0))
            {
                Memory.WriteHook(new HookParameters()
                {
                    Address = NetScriptFramework.Main.GameInfo.GetAddressOf(36271, 0x96, 0, "E8"),
                    IncludeLength = 5,
                    ReplaceLength = 5,

                    Before = ctx =>
                    {
                        var plr = PlayerCharacter.Instance;
                        if (plr == null || plr.Address != ctx.DI)
                            return;

                        if (this.ShouldBlockJump())
                        {
                            ctx.Skip();
                            ctx.AX = new IntPtr(0);
                            _skippedJumpNow = true;
                        }
                        else
                            _skippedJumpNow = false;
                    },

                    After = ctx =>
                    {
                        if (settings.jumpStamina.Value <= 0.0)
                            return;

                        var plr = PlayerCharacter.Instance;
                        if (plr == null || plr.Cast<PlayerCharacter>() != ctx.DI)
                            return;

                        if (!_skippedJumpNow)
                            this.SpendStamina(plr, (float)settings.jumpStamina.Value, (float)settings.jumpDelay.Value, true, modes.jump);
                    },
                });
            }
        }
    }
}
