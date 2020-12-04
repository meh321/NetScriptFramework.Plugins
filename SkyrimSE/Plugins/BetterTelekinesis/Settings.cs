using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework.Tools;

namespace BetterTelekinesis
{
    public sealed class Settings
    {
        [ConfigValue("BaseDistanceMultiplier", "Distance multiplier", "A multiplier to max base distance where telekinesis can pick stuff from. For example 2.0 would mean 2x base distance. This value may be further modified by perks.")]
        public float BaseDistanceMultiplier
        {
            get;
            set;
        } = 2.0f;

        [ConfigValue("BaseDamageMultiplier", "Damage multiplier", "A multiplier to base damage of telekinesis without any perks. Vanilla game value is way too pathetic to be of any use so it's increased here by default.")]
        public float BaseDamageMultiplier
        {
            get;
            set;
        } = 5.0f;

        [ConfigValue("ObjectPullSpeedBase", "Object pull base speed", "The initial speed when pulling objects to caster.")]
        public float ObjectPullSpeedBase
        {
            get;
            set;
        } = 200.0f;

        [ConfigValue("ObjectPullSpeedAccel", "Object pull acceleration speed", "The speed gain per second of pulling objects to caster.")]
        public float ObjectPullSpeedAccel
        {
            get;
            set;
        } = 10000.0f;

        [ConfigValue("ObjectPullSpeedMax", "Object pull max speed", "The max speed of pulling objects to caster.")]
        public float ObjectPullSpeedMax
        {
            get;
            set;
        } = 8000.0f;

        [ConfigValue("ObjectThrowForce", "Object throw force", "The force at which objects are thrown, compared to vanilla. For example 4.0 means 4x force of default vanilla value.")]
        public float ObjectThrowForce
        {
            get;
            set;
        } = 4.0f;

        [ConfigValue("ObjectHoldDistance", "Object hold distance", "The distance multiplier at which to hold objects in front of you.")]
        public float ObjectHoldDistance
        {
            get;
            set;
        } = 1.0f;

        [ConfigValue("ActorPullSpeed", "Actor pull speed", "The speed at which actors are pulled to caster when using the grab actor effect archetype.")]
        public float ActorPullSpeed
        {
            get;
            set;
        } = 8000.0f;

        [ConfigValue("ActorThrowForce", "Actor throw force", "The force at which actors are thrown, compared to vanilla.")]
        public float ActorThrowForce
        {
            get;
            set;
        } = 2.0f;

        [ConfigValue("ActorHoldDistance", "Actor hold distance", "The distance multiplier at which to hold actors in front of you.")]
        public float ActorHoldDistance
        {
            get;
            set;
        } = 1.5f;

        [ConfigValue("ResponsiveHold", "Responsive hold", "Make the way objects are held in place more responsive. Vanilla method lags behind and preserves momentum a lot. If you enable this method it will make objects snap immediately to where you are aiming.")]
        public bool ResponsiveHold
        {
            get;
            set;
        } = true;

        [ConfigValue("ResponsiveHoldParams", "Responsive hold (parameters)", "Parameters on how responsive hold is achieved, you shouldn't change this.", ConfigEntryFlags.Hidden)]
        public string ResponsiveHoldParams
        {
            get;
            set;
        } = BetterTelekinesisPlugin.DefaultResponsiveHoldParameters;

        [ConfigValue("ThrowActorDamage", "Throw actor damage", "This setting will cause throwing an actor to cause telekinesis damage to the same actor you just threw. The game has a bug where if you pick up a ragdoll and throw it very far it might not take almost any damage. This will make grabbing actors with telekinesis type effects much more effective way to combat them since you deal the same damage to actor by throwing them as if you would have by throwing an object at them with telekinesis. The value here says the ratio of the telekinesis damage that is done, if you set 0.5 for example then deal half of telekinesis damage to thrown actor. 0 will disable this setting.")]
        public float ThrowActorDamage
        {
            get;
            set;
        } = 0.0f;

        [ConfigValue("HoldActorDamage", "Hold actor damage", "This setting will cause holding an actor to cause telekinesis damage per second. The amount of damage done is multiplied by this value so 0.1 would deal 10% of telekinesis damage per second to held actor.")]
        public float HoldActorDamage
        {
            get;
            set;
        } = 0.0f;

        [ConfigValue("AbortTelekinesisHotkey", "Abort telekinesis hotkey", "When you are holding objects you can press this hotkey to abort and drop the objects to the ground where they are instead of launching them.")]
        public string AbortTelekinesisHotkey
        {
            get;
            set;
        } = "shift";

        [ConfigValue("LaunchIsHotkeyInstead", "Launch is hotkey instead", "If set to true then you need to press the hotkey to launch objects instead, otherwise they will be dropped on the ground.")]
        public bool LaunchIsHotkeyInstead
        {
            get;
            set;
        } = false;

        [ConfigValue("DontLaunchIfRunningOutOfMagicka", "Don't launch if empty magicka", "When the spell ends and it's time to launch objects check if you are out of magicka and if yes then don't launch?")]
        public bool DontLaunchIfRunningOutOfMagicka
        {
            get;
            set;
        } = true;

        [ConfigValue("OverwriteTargetPicker", "Overwrite target picker", "This will overwrite the method for how targets for telekinesis and grab actor are picked. Vanilla method will raycast your crosshair but this is very inconvenient in combat as you have to precisely always aim on the object and it's quite difficult or even impossible if the object is small or very far. This will overwrite the method to allow much more freedom in how we pick the targets.")]
        public bool OverwriteTargetPicker
        {
            get;
            set;
        } = true;

        [ConfigValue("ObjectTargetPickerRange", "Object target picker range", "The maximum distance from the line where you are aiming for objects to be picked to be telekinesis targets.")]
        public float ObjectTargetPickerRange
        {
            get;
            set;
        } = 500.0f;

        [ConfigValue("ActorTargetPickerRange", "Actor target picker range", "The maximum distance from the line where you are aiming for actors to be picked to be telekinesis targets.")]
        public float ActorTargetPickerRange
        {
            get;
            set;
        } = 200.0f;

        [ConfigValue("DontPickFriendlyTargets", "Don't pick friendly targets", "If set to 2 then don't pick any actor targets who aren't hostile to you (wouldn't attack you on detection). If set to 1 then don't pick followers marked as team-mates. If set to 0 then pick any actor.")]
        public int DontPickFriendlyTargets
        {
            get;
            set;
        } = 1;
        
        [ConfigValue("TelekinesisMaxObjects", "Telekinesis max objects count", "How many objects you can hold with telekinesis at once. These are objects and not actors.")]
        public int TelekinesisMaxObjects
        {
            get;
            set;
        } = 10;

        [ConfigValue("TelekinesisObjectSpread", "Telekinesis object spread", "When you have multiple objects telekinesised, they will spread out by this much. This is degrees rotation not actual in-game units.")]
        public float TelekinesisObjectSpread
        {
            get;
            set;
        } = 15.0f;

        [ConfigValue("TelekinesisSpells", "Telekinesis spell forms", "The telekinesis spell forms. These are all the spells affected by cost overwrite or the AutoLearnTelekinesisSpell settings.")]
        public string TelekinesisSpells
        {
            get;
            set;
        } = "1A4CC:Skyrim.esm;873:BetterTelekinesis.esp;874:BetterTelekinesis.esp;876:BetterTelekinesis.esp";

        [ConfigValue("TelekinesisPrimary", "Primary telekinesis spell form", "The telekinesis spell that is learned from book.")]
        public string TelekinesisPrimary
        {
            get;
            set;
        } = "1A4CC:Skyrim.esm";

        [ConfigValue("TelekinesisSecondary", "Secondary telekinesis spell forms", "The telekinesis spells that are variants of the primary spell.")]
        public string TelekinesisSecondary
        {
            get;
            set;
        } = "873:BetterTelekinesis.esp;874:BetterTelekinesis.esp;876:BetterTelekinesis.esp";

        [ConfigValue("AutoLearnTelekinesisVariants", "Auto-learn telekinesis variants", "This will learn the secondary telekinesis spells when you have the primary spell.")]
        public bool AutoLearnTelekinesisVariants
        {
            get;
            set;
        } = true;
        
        [ConfigValue("TelekinesisGrabObjectSound", "Telekinesis grab object sound", "If you set this false it will disable playing the sound that happens when you grab an object with telekinesis.")]
        public bool TelekinesisGrabObjectSound
        {
            get;
            set;
        } = true;

        [ConfigValue("TelekinesisLaunchObjectSound", "Telekinesis launch object sound", "If you set this false it will disable playing the sound that happens when you launch an object with telekinesis.")]
        public bool TelekinesisLaunchObjectSound
        {
            get;
            set;
        } = true;

        [ConfigValue("OverwriteTelekinesisSpellBaseCost", "Overwrite telekinesis spell base cost", "If this is not negative then the base spell cost will be set to this value.")]
        public float OverwriteTelekinesisSpellBaseCost
        {
            get;
            set;
        } = -1.0f;

        [ConfigValue("GrabActorNodeNearest", "Grab actor by nearest node", "When grabbing actor, select the nearest node to crosshair. That means if you aim at head and grab actor it will grab by its head. This may be inaccurate if you have any mods that modify the placement of crosshair.")]
        public bool GrabActorNodeNearest
        {
            get;
            set;
        } = true;

        [ConfigValue("GrabActorNodeExclude", "Exclude actor nodes", "Exclude these actor nodes. This can be useful to make sure we get more accurate results.")]
        public string GrabActorNodeExclude
        {
            get;
            set;
        } = "";

        [ConfigValue("GrabActorNodePriority", "Grab actor node priority", "Decide by which node an actor is grabbed by. This setting does nothing if you have enabled GrabActorNodeNearest!!! If a node is not available on actor it will try the next node in list. For example\nGrabActorNodePriority = \"NPC Neck [Neck];NPC Spine2 [Spn2]\"\nThe above would make NPCs be held by their necks if they have one, and if not then spine.")]
        public string GrabActorNodePriority
        {
            get;
            set;
        } = "NPC Spine2 [Spn2]";

        [ConfigValue("AutoLearnTelekinesisSpell", "Auto learn telekinesis", "If set to true it will automatically add the telekinesis spell to player if they don't have it.")]
        public bool AutoLearnTelekinesisSpell
        {
            get;
            set;
        } = false;

        [ConfigValue("TelekinesisLabelMode", "Telekinesis label mode", "Change how the label works when telekinesis is equipped. Due to the way the label works this option only does anything if you also enabled OverwriteTargetPicker. If you don't know what I mean, it's the thing where if you have telekinesis spell equipped (not necessarily hands out) it shows distant objects labels without the E - Activate part. That's to help you show what you are aiming at with telekinesis. So here's some choices on how that works:\n0 = don't change anything, leave vanilla, objects exactly below your crosshair will show label\n1 = show nearest object's label, this is the first object that would fly to you if you cast telekinesis now\n2 = disable telekinesis label completely, don't show the thing")]
        public int TelekinesisLabelMode
        {
            get;
            set;
        } = 1;

        [ConfigValue("FixDragonsNotBeingTelekinesisable", "Fix dragons not being telekinesisable", "WARNING: Don't enable, because once the ragdoll of dragon ends it loses all collision and gets stuck instead of getting up!")]
        public bool FixDragonsNotBeingTelekinesisable
        {
            get;
            set;
        } = false;

        [ConfigValue("FixGrabActorHoldHostility", "Fix grab actor hold hostility", "Make it so that when you grab actor it's immediately considered a hostile action.")]
        public bool FixGrabActorHoldHostility
        {
            get;
            set;
        } = true;

        [ConfigValue("DontDeactivateHavokHoldSpring", "Don't deactivate havok hold spring", "If camera is completely steady the hold object spring will be deactivated, this will not look good when you're holding objects that want to make small movements or rotations.")]
        public bool DontDeactivateHavokHoldSpring
        {
            get;
            set;
        } = true;

        [ConfigValue("MultiObjectHoverAmount", "Object hover amount", "When enabling multi object telekinesis the objects will hover around a bit to make it look cooler. This is the amount of distance they can hover from origin.")]
        public float MultiObjectHoverAmount
        {
            get;
            set;
        } = 4.0f;

        [ConfigValue("FixSuperHugeTelekinesisDistanceBug", "Fix super huge telekinesis distance bug", "There is a vanilla bug where telekinesis distance is really big instead of what it's supposed to be. Fix that.")]
        public bool FixSuperHugeTelekinesisDistanceBug
        {
            get;
            set;
        } = true;

        [ConfigValue("TelekinesisTargetUpdateInterval", "Telekinesis target update interval", "The interval in seconds between telekinesis target updates. This is only used if overwriting the telekinesis target picker.")]
        public float TelekinesisTargetUpdateInterval
        {
            get;
            set;
        } = 0.2f;

        [ConfigValue("TelekinesisTargetOnlyUpdateIfWeaponOut", "Telekinesis target picker needs weapon out", "Don't update telekinesis targets if we don't have hands out. This is here to avoid unnecessarily updating the telekinesis targets. Only used if overwriting the telekinesis target picker.")]
        public bool TelekinesisTargetOnlyUpdateIfWeaponOut
        {
            get;
            set;
        } = true;

        [ConfigValue("DebugMessageMode", "Debug message mode", "Nothing.", ConfigEntryFlags.Hidden)]
        public int DebugMessageMode
        {
            get;
            set;
        } = 0;

        [ConfigValue("PointWeaponsAndProjectilesForward", "Point weapons and projectiles forward", "When holding objects in telekinesis then if they are weapon or projectile point them forward, for maximum coolness.")]
        public bool PointWeaponsAndProjectilesForward
        {
            get;
            set;
        } = true;

        [ConfigValue("AddSwordSpellsToLeveledLists", "Add sword spell books to leveled lists", "Add the sword spell books to leveled lists of NPC vendors.")]
        public bool AddSwordSpellsToLeveledLists
        {
            get;
            set;
        } = true;

        [ConfigValue("MakeSwordSpellsAlterationInstead", "Make sword spells alteration", "Make the new sword spells alteration instead of conjuration.")]
        public bool MakeSwordSpellsAlterationInstead
        {
            get;
            set;
        } = false;

        [ConfigValue("TelekinesisDisarmsEnemies", "Telekinesis disarms", "Sometimes telekinesis disarms enemies. Some NPCs can not be disarmed. If you are already holding the maximum amount of objects with telekinesis it will not disarm. If there are multiple NPCs around you need to aim at the NPC you want to disarm or it may choose wrong target. Enemies need to have their weapon out to be able to disarm them.")]
        public bool TelekinesisDisarmsEnemies
        {
            get;
            set;
        } = false;

        // book spell effect item_assoc
        [ConfigValue("SpellInfo_Reach", "reach", "", ConfigEntryFlags.Hidden)]
        public string SpellInfo_Reach
        {
            get;
            set;
        } = ";876:BetterTelekinesis.esp;875:BetterTelekinesis.esp;";

        [ConfigValue("SpellInfo_One", "one", "", ConfigEntryFlags.Hidden)]
        public string SpellInfo_One
        {
            get;
            set;
        } = ";873:BetterTelekinesis.esp;806:BetterTelekinesis.esp;";

        [ConfigValue("SpellInfo_NPC", "npc", "", ConfigEntryFlags.Hidden)]
        public string SpellInfo_NPC
        {
            get;
            set;
        } = ";874:BetterTelekinesis.esp;809:BetterTelekinesis.esp;";

        [ConfigValue("SpellInfo_Barr", "barr", "", ConfigEntryFlags.Hidden)]
        public string SpellInfo_Barr
        {
            get;
            set;
        } = "879:BetterTelekinesis.esp;871:BetterTelekinesis.esp;807:BetterTelekinesis.esp;";

        [ConfigValue("SpellInfo_Blast", "blast", "", ConfigEntryFlags.Hidden)]
        public string SpellInfo_Blast
        {
            get;
            set;
        } = "87A:BetterTelekinesis.esp;872:BetterTelekinesis.esp;808:BetterTelekinesis.esp;";

        [ConfigValue("SpellInfo_Normal", "normal", "", ConfigEntryFlags.Hidden)]
        public string SpellInfo_Normal
        {
            get;
            set;
        } = "A26E5:Skyrim.esm;1A4CC:Skyrim.esm;800:BetterTelekinesis.esp;";

        [ConfigValue("EffectInfo_Forms", "forms", "", ConfigEntryFlags.Hidden)]
        public string EffectInfo_Forms
        {
            get;
            set;
        } = "877:BetterTelekinesis.esp;878:BetterTelekinesis.esp";

        [ConfigValue("MagicSword_RemoveDelay", "Magic sword remove delay", "How long can magic sword exist in world without doing anything with it (in seconds).")]
        public float MagicSword_RemoveDelay
        {
            get;
            set;
        } = 6.0f;

        [ConfigValue("MagicSwordBlast_PlaceDistance", "Magic sword placement distance", "How far ahead magic sword to put.")]
        public float MagicSwordBlast_PlaceDistance
        {
            get;
            set;
        } = 300.0f;

        [ConfigValue("MagicSwordBarrage_PlaceDistance", "Magic sword placement distance", "How far ahead magic sword to put.")]
        public float MagicSwordBarrage_PlaceDistance
        {
            get;
            set;
        } = 200.0f;

        [ConfigValue("SwordBarrage_FireDelay", "Sword barrage fire delay", "Delay in seconds before barrage will fire the sword after grabbing it.")]
        public float SwordBarrage_FireDelay
        {
            get;
            set;
        } = 0.5f;

        [ConfigValue("SwordBarrage_SpawnDelay", "Sword barrage spawn delay", "Delay in seconds before barrage will spawn the next sword.")]
        public float SwordBarrage_SpawnDelay
        {
            get;
            set;
        } = 0.15f;

        [ConfigValue("SwordReturn_Marker", "Sword return marker", "", ConfigEntryFlags.Hidden)]
        public string SwordReturn_Marker
        {
            get;
            set;
        } = "80B:BetterTelekinesis.esp";

        [ConfigValue("AlwaysLaunchObjectsEvenWhenNotFinishedPulling", "Always launch objects", "There's a mechanic where if you pull object to you with telekinesis but release the spell before the object is finished being pulled to you it gets dropped instead of launched. Here you can overwrite the behavior and force it always to be launched even when not finished pulling yet.")]
        public bool AlwaysLaunchObjectsEvenWhenNotFinishedPulling
        {
            get;
            set;
        } = false;

        [ConfigValue("Barrage_SwordModel", "Barrage spell sword models", "Make the barrage spell use these nifs as models. Separate with ;")]
        public string Barrage_SwordModel
        {
            get;
            set;
        } = @"Weapons\Iron\LongSword.nif;" +
@"Weapons\Iron\LongSword.nif;" +
@"Weapons\Iron\LongSword.nif;" +
@"Weapons\Iron\LongSword.nif;" +
@"Weapons\Akaviri\BladesSword.nif;" +
@"Weapons\Akaviri\BladesSword.nif;" +
@"Clutter\DummyItems\DummySword01.nif;" +
@"Weapons\Daedric\DaedricSword.nif;" +
@"DLC01\Weapons\Dragonbone\Sword.nif;" +
@"Weapons\Draugr\DraugrSword.nif;" +
@"Weapons\Draugr\DraugrSword.nif;" +
@"Weapons\Draugr\DraugrSword.nif;" +
@"Weapons\Draugr\DraugrSword.nif;" +
@"Weapons\Dwarven\DwarvenSword.nif;" +
@"Weapons\Dwarven\DwarvenSword.nif;" +
@"Weapons\Dwarven\DwarvenSword.nif;" +
@"Weapons\Dwarven\DwarvenSword.nif;" +
@"Weapons\Ebony\EbonySword.nif;" +
@"Weapons\Elven\ElvenSword.nif;" +
@"Weapons\Elven\ElvenSword.nif;" +
@"Weapons\Elven\ElvenSword.nif;" +
@"Weapons\Elven\ElvenSword.nif;" +
@"Weapons\Falmer\FalmerLongSword.nif;" +
@"Weapons\Falmer\FalmerLongSword.nif;" +
@"Weapons\Falmer\FalmerLongSword.nif;" +
@"Weapons\Forsworn\ForswornSword.nif;" +
@"Weapons\Forsworn\ForswornSword.nif;" +
@"Weapons\Forsworn\ForswornSword.nif;" +
@"Weapons\Glass\GlassSword.nif;" +
@"Weapons\Glass\GlassSword.nif;" +
@"Weapons\Imperial\ImperialSword.nif;" +
@"Weapons\Imperial\ImperialSword.nif;" +
@"Weapons\Imperial\ImperialSword.nif;" +
@"Weapons\Imperial\ImperialSword.nif;" +
@"Weapons\Orcish\OrcishSword.nif;" +
@"Weapons\Orcish\OrcishSword.nif;" +
@"Weapons\Orcish\OrcishSword.nif;" +
@"Weapons\Orcish\OrcishSword.nif;" +
@"Weapons\NordHero\NordHeroSword.nif;" +
@"Weapons\NordHero\NordHeroSword.nif;" +
@"Weapons\NordHero\NordHeroSword.nif;" +
@"Weapons\Scimitar\Scimitar.nif;" +
@"Weapons\Scimitar\Scimitar.nif;" +
@"Weapons\Silver\SilverSword.nif;" +
@"Weapons\Silver\SilverSword.nif;" +
@"Weapons\Silver\SilverSword.nif;" +
@"Weapons\Steel\SteelSword.nif;" +
@"Weapons\Steel\SteelSword.nif;" +
@"Weapons\Steel\SteelSword.nif;" +
@"Weapons\Steel\SteelSword.nif";

        [ConfigValue("Blast_SwordModel", "Blast spell sword models", "Make the blast spell use these nifs as models. Separate with ;")]
        public string Blast_SwordModel
        {
            get;
            set;
        } = @"Weapons\Iron\LongSword.nif;" +
@"Weapons\Iron\LongSword.nif;" +
@"Weapons\Iron\LongSword.nif;" +
@"Weapons\Iron\LongSword.nif;" +
@"Weapons\Akaviri\BladesSword.nif;" +
@"Weapons\Akaviri\BladesSword.nif;" +
@"Clutter\DummyItems\DummySword01.nif;" +
@"Weapons\Daedric\DaedricSword.nif;" +
@"DLC01\Weapons\Dragonbone\Sword.nif;" +
@"Weapons\Draugr\DraugrSword.nif;" +
@"Weapons\Draugr\DraugrSword.nif;" +
@"Weapons\Draugr\DraugrSword.nif;" +
@"Weapons\Draugr\DraugrSword.nif;" +
@"Weapons\Dwarven\DwarvenSword.nif;" +
@"Weapons\Dwarven\DwarvenSword.nif;" +
@"Weapons\Dwarven\DwarvenSword.nif;" +
@"Weapons\Dwarven\DwarvenSword.nif;" +
@"Weapons\Ebony\EbonySword.nif;" +
@"Weapons\Elven\ElvenSword.nif;" +
@"Weapons\Elven\ElvenSword.nif;" +
@"Weapons\Elven\ElvenSword.nif;" +
@"Weapons\Elven\ElvenSword.nif;" +
@"Weapons\Falmer\FalmerLongSword.nif;" +
@"Weapons\Falmer\FalmerLongSword.nif;" +
@"Weapons\Falmer\FalmerLongSword.nif;" +
@"Weapons\Forsworn\ForswornSword.nif;" +
@"Weapons\Forsworn\ForswornSword.nif;" +
@"Weapons\Forsworn\ForswornSword.nif;" +
@"Weapons\Glass\GlassSword.nif;" +
@"Weapons\Glass\GlassSword.nif;" +
@"Weapons\Imperial\ImperialSword.nif;" +
@"Weapons\Imperial\ImperialSword.nif;" +
@"Weapons\Imperial\ImperialSword.nif;" +
@"Weapons\Imperial\ImperialSword.nif;" +
@"Weapons\Orcish\OrcishSword.nif;" +
@"Weapons\Orcish\OrcishSword.nif;" +
@"Weapons\Orcish\OrcishSword.nif;" +
@"Weapons\Orcish\OrcishSword.nif;" +
@"Weapons\NordHero\NordHeroSword.nif;" +
@"Weapons\NordHero\NordHeroSword.nif;" +
@"Weapons\NordHero\NordHeroSword.nif;" +
@"Weapons\Scimitar\Scimitar.nif;" +
@"Weapons\Scimitar\Scimitar.nif;" +
@"Weapons\Silver\SilverSword.nif;" +
@"Weapons\Silver\SilverSword.nif;" +
@"Weapons\Silver\SilverSword.nif;" +
@"Weapons\Steel\SteelSword.nif;" +
@"Weapons\Steel\SteelSword.nif;" +
@"Weapons\Steel\SteelSword.nif;" +
@"Weapons\Steel\SteelSword.nif";

        /*[ConfigValue("AllowProjectileTelekinesis", "Allow projectile telekinesis", "This will allow telekinesising flying projectiles.")]
        public bool AllowProjectileTelekinesis
        {
            get;
            set;
        } = true;*/

        internal void Load()
        {
            ConfigFile.LoadFrom(this, "BetterTelekinesis", true);
        }
    }

    /// <summary>
    /// Cached form list for lookups later.
    /// </summary>
    public sealed class CachedFormList
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="CachedFormList"/> class from being created.
        /// </summary>
        private CachedFormList()
        {
        }

        /// <summary>
        /// The forms.
        /// </summary>
        private readonly List<NetScriptFramework.SkyrimSE.TESForm> Forms = new List<NetScriptFramework.SkyrimSE.TESForm>();

        /// <summary>
        /// The ids.
        /// </summary>
        private readonly HashSet<uint> Ids = new HashSet<uint>();

        /// <summary>
        /// Tries to parse from input. Returns null if failed.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="pluginForLog">The plugin for log.</param>
        /// <param name="settingNameForLog">The setting name for log.</param>
        /// <param name="warnOnMissingForm">If set to <c>true</c> warn on missing form.</param>
        /// <param name="dontWriteAnythingToLog">Don't write any errors to log if failed to parse.</param>
        /// <returns></returns>
        public static CachedFormList TryParse(string input, string pluginForLog, string settingNameForLog, bool warnOnMissingForm = true, bool dontWriteAnythingToLog = false)
        {
            if (string.IsNullOrEmpty(settingNameForLog))
                settingNameForLog = "unknown form list setting";
            if (string.IsNullOrEmpty(pluginForLog))
                pluginForLog = "unknown plugin";

            var ls = new CachedFormList();
            var spl = input.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var x in spl)
            {
                string idstr;
                string file;

                int ix = x.IndexOf(':');
                if (ix <= 0)
                {
                    if (!dontWriteAnythingToLog)
                        NetScriptFramework.Main.Log.AppendLine("Failed to parse " + settingNameForLog + " for " + pluginForLog + "! Invalid input: `" + x + "`.");
                    return null;
                }

                idstr = x.Substring(0, ix);
                file = x.Substring(ix + 1);

                if (!idstr.All(q => (q >= '0' && q <= '9') || (q >= 'a' && q <= 'f') || (q >= 'A' && q <= 'F')))
                {
                    if (!dontWriteAnythingToLog)
                        NetScriptFramework.Main.Log.AppendLine("Failed to parse " + settingNameForLog + " for " + pluginForLog + "! Invalid form ID: `" + idstr + "`.");
                    return null;
                }

                if (string.IsNullOrEmpty(file))
                {
                    if (!dontWriteAnythingToLog)
                        NetScriptFramework.Main.Log.AppendLine("Failed to parse " + settingNameForLog + " for " + pluginForLog + "! Missing file name.");
                    return null;
                }

                uint id = 0;
                if (!uint.TryParse(idstr, System.Globalization.NumberStyles.HexNumber, null, out id))
                {
                    if (!dontWriteAnythingToLog)
                        NetScriptFramework.Main.Log.AppendLine("Failed to parse " + settingNameForLog + " for " + pluginForLog + "! Invalid form ID: `" + idstr + "`.");
                    return null;
                }

                var form = NetScriptFramework.SkyrimSE.TESForm.LookupFormFromFile(id, file);
                if (form == null)
                {
                    if (!dontWriteAnythingToLog && warnOnMissingForm)
                        NetScriptFramework.Main.Log.AppendLine("Failed to find form " + settingNameForLog + " for " + pluginForLog + "! Form ID was " + id.ToString("X") + " and file was " + file + ".");
                    continue;
                }

                if (ls.Ids.Add(form.FormId))
                    ls.Forms.Add(form);
            }

            return ls;
        }

        /// <summary>
        /// Determines whether this list contains the specified form.
        /// </summary>
        /// <param name="form">The form.</param>
        /// <returns></returns>
        public bool Contains(NetScriptFramework.SkyrimSE.TESForm form)
        {
            if (form == null)
                return false;

            return Contains(form.FormId);
        }

        /// <summary>
        /// Determines whether this list contains the specified form identifier.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <returns></returns>
        public bool Contains(uint formId)
        {
            return this.Ids.Contains(formId);
        }

        /// <summary>
        /// Gets all forms in this list.
        /// </summary>
        /// <value>
        /// All.
        /// </value>
        public IReadOnlyList<NetScriptFramework.SkyrimSE.TESForm> All
        {
            get
            {
                return this.Forms;
            }
        }
    }
}
