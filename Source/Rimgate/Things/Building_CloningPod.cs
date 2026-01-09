using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VEF.Maps;
using VEF.Utils;
using Verse;
using Verse.AI;
using Verse.Noise;
using Verse.Sound;
using static UnityEngine.Networking.UnityWebRequest;

namespace Rimgate;

public enum CloningStatus
{
    Idle = 0,
    CloningStarted,
    CalibrationFinished,
    HostDischarged,
    Paused,
    Incubating,
    Error
}

public enum CloneType
{
    None = 0,
    Genome,
    Full,
    Enhanced,
    Reconstruct,
}

public class Building_CloningPod : Building, IThingHolder, IThingHolderWithDrawnPawn, ISearchableContents, IOpenable
{
    private static List<ThingDef> _cachedPodDefs;

    public const int BiomassCostPerCycle = 100;

    public CompRefuelable Refuelable => _cachedRefuelable ??= GetComp<CompRefuelable>();

    public CompPowerTrader Power => _cachedPowerTrader ??= GetComp<CompPowerTrader>();

    public bool Powered => Power == null || Power.PowerOn == true;

    public Comp_AnalyzableResearchWhen Analyzable => _cachedAnalyzable ??= GetComp<Comp_AnalyzableResearchWhen>();

    public CompAffectedByFacilities Facilities => _cachedFacilities ??= GetComp<CompAffectedByFacilities>();

    public float HeldPawnDrawPos_Y => DrawPos.y - 0.06f;

    public float HeldPawnBodyAngle => Rotation.Opposite.AsAngle;

    public PawnPosture HeldPawnPosture => PawnPosture.LayingOnGroundFaceUp;

    public CloningStatus Status = CloningStatus.Idle;

    public bool ContainsCorpse => _innerContainer?.Count > 0 && _innerContainer[0] is Corpse;

    public string OpenedSignal;

    public virtual int OpenTicks => 300;

    public virtual bool CanOpen => HasHostPawn;

    public ThingOwner SearchableContents => _innerContainer;

    public Thing ContainedThing
    {
        get
        {
            if (_innerContainer.Count != 0)
            {
                return _innerContainer[0];
            }

            return null;
        }
    }

    public bool HasHostPawn => _innerContainer?.Count > 0;

    public Pawn HostPawn
    {
        get
        {
            if (!HasHostPawn)
                return null;

            foreach (Thing thing in _innerContainer)
            {
                if (thing is Pawn innerPawn)
                    return innerPawn;

                if (thing is Corpse corpse)
                    return corpse.InnerPawn;
            }

            return null;
        }
    }

    public bool HasClonePawn => _pendingCloneContainer?.Count > 0;

    public Pawn ClonePawn
    {
        get
        {
            if (_pendingCloneContainer?.Count == 0)
                return null;

            foreach (Thing thing in _pendingCloneContainer)
            {
                if (thing is Pawn innerPawn)
                    return innerPawn;
            }

            return null;
        }
    }

    public float RemainingCalibrationWork => _calibrationWorkRemaining;

    public float RemainingIncubationTicks => _incubationTicksRemaining;

    public bool FuelForCurrentCycleConsumed => _fuelConsumedForCurrentCycle;

    public CloneType CloningType => _cloningType;

    public bool IsWorking => Status == CloningStatus.CloningStarted || Status == CloningStatus.Incubating;

    public bool HasPendingClone => _pendingCloneContainer != null && _pendingCloneContainer.Count > 0;

    public Pawn PendingClonePawn => HasPendingClone ? _pendingCloneContainer[0] as Pawn : null;

    private ThingOwner _innerContainer;

    private ThingOwner _pendingCloneContainer; // holds 0 or 1 Pawn

    private bool _contentsKnown;

    private float _hostSavedFoodNeed;

    private float _hostSavedDbhThirstNeed;

    private bool _hostNeedsSnapshotTaken;

    private Comp_CloningPodAnimation _cachedControl;

    private CompRefuelable _cachedRefuelable;

    private CompPowerTrader _cachedPowerTrader;

    private Comp_AnalyzableResearchWhen _cachedAnalyzable;

    private CompAffectedByFacilities _cachedFacilities;

    private int _calibrationTicksTotal;

    private float _calibrationWorkRemaining;

    private CloneType _cloningType;

    private int _incubationTicksTotal;

    private int _incubationTicksRemaining;

    private bool _fuelConsumedForCurrentCycle;

    private CalibrationOutcome _calibrationOutcome;

    private bool _hadActiveInducer;

    public Building_CloningPod()
    {
        _innerContainer = new ThingOwner<Thing>(this);
        _pendingCloneContainer = new ThingOwner<Thing>(this);
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        if (base.Faction != null && base.Faction.IsPlayer)
            _contentsKnown = true;

        _hadActiveInducer = HasActiveInducer();
    }

    public override AcceptanceReport ClaimableBy(Faction fac)
    {
        if (_innerContainer.Any && !_contentsKnown)
            return false;

        return base.ClaimableBy(fac);
    }

    protected override void Tick()
    {
        base.Tick();

        // State-dependent power consumption
        if (Power != null)
            Power.PowerOutput = IsWorking ? -Power.Props.PowerConsumption : -Power.Props.idlePowerDraw;

        switch (Status)
        {
            case CloningStatus.Idle:
                {
                    if (!Powered)
                        return;

                    if (!HasHostPawn || ContainsCorpse)
                        break;

                    if (!_hostNeedsSnapshotTaken)
                    {
                        var pawn = HostPawn;
                        // Save initial patient food need level
                        if (pawn.needs.food != null)
                            _hostSavedFoodNeed = pawn.needs.food.CurLevelPercentage;

                        // Save initial patient DBH thirst and reset DBH bladder/hygiene need levels
                        if (ModCompatibility.DbhIsActive)
                        {
                            _hostSavedDbhThirstNeed = DbhCompatibility.GetThirstNeedCurLevelPercentage(pawn);
                            DbhCompatibility.SetBladderNeedCurLevelPercentage(pawn, 1f);
                            DbhCompatibility.SetHygieneNeedCurLevelPercentage(pawn, 1f);
                        }
                        _hostNeedsSnapshotTaken = true;
                    }

                    break;
                }
            case CloningStatus.CloningStarted:
                {
                    if (!HasHostPawn)
                    {
                        Status = CloningStatus.Error;
                        Messages.Message(
                            "RG_CloningPodCalibrationFailed".Translate("RG_CloningPodCalibrationFailed_NoHost".Translate()),
                            this,
                            MessageTypeDefOf.NegativeEvent);
                        return;
                    }

                    if (!Powered)
                    {
                        Status = CloningStatus.Error;
                        Messages.Message(
                            "RG_CloningPodCalibrationFailed".Translate("NoPower".Translate()),
                            this,
                            MessageTypeDefOf.NegativeEvent);
                        return;
                    }

                    if (!this.IsHashIntervalTick(GenTicks.TicksPerRealSecond))
                    {
                        bool has = HasActiveInducer();
                        if (has != _hadActiveInducer)
                        {
                            float modifier = has
                                ? (1f / RimgateModSettings.InducerCalibrationSpeedFactor)
                                : RimgateModSettings.InducerCalibrationSpeedFactor;

                            int newTotal = Mathf.Max(1, Mathf.RoundToInt(_calibrationTicksTotal * modifier));
                            _calibrationWorkRemaining = RescaleRemainingFloat(_calibrationTicksTotal, _calibrationWorkRemaining, newTotal);
                            _calibrationTicksTotal = newTotal;
                            _hadActiveInducer = has;
                        }
                    }

                    break;
                }
            case CloningStatus.CalibrationFinished:
                {
                    if (!ContainsCorpse)
                    {
                        var pawn = HostPawn;
                        // Restore previously saved patient food need level
                        if (pawn.needs.food != null)
                            pawn.needs.food.CurLevelPercentage = _hostSavedFoodNeed;

                        // Restore previously saved patient DBH thirst and reset DBH bladder/hygiene need levels

                        if (ModCompatibility.DbhIsActive)
                        {
                            DbhCompatibility.SetThirstNeedCurLevelPercentage(pawn, _hostSavedDbhThirstNeed);
                            DbhCompatibility.SetBladderNeedCurLevelPercentage(pawn, 1f);
                            DbhCompatibility.SetHygieneNeedCurLevelPercentage(pawn, 1f);
                        }
                    }

                    EjectContents();
                    SwitchState(); // CalibrationFinished -> HostDischarged

                    break;
                }
            case CloningStatus.HostDischarged:
                {
                    if (!HasPendingClone)
                    {
                        Status = CloningStatus.Error;
                        if (RimgateMod.Debug)
                            Log.Message(this + " :: No pending clone found while paused, switching to Error state");
                        return;
                    }

                    if (TryGetCostForCurrentCycle(out int cost))
                    {
                        Log.Message($"consuming {cost} biomass to start incubation");

                        // Consume initial biomass cost prior to incubation to simulate starting the process
                        Refuelable.ConsumeFuel(cost);
                        DecayActiveBiomassStabilizers();
                        _fuelConsumedForCurrentCycle = true;
                        SwitchState(); // HostDischarged -> Incubating
                    }
                    else
                        Status = CloningStatus.Paused;

                    break;
                }
            case CloningStatus.Paused:
                {
                    if (!HasPendingClone)
                    {
                        Status = CloningStatus.Error;
                        if (RimgateMod.Debug)
                            Log.Message(this + " :: No pending clone found while paused, switching to Error state");
                        return;
                    }

                    // Resume if power is back on and we have enough biomass
                    if (Powered)
                    {
                        if (!_fuelConsumedForCurrentCycle && TryGetCostForCurrentCycle(out int cost))
                        {
                            Refuelable.ConsumeFuel(cost);
                            DecayActiveBiomassStabilizers();
                            _fuelConsumedForCurrentCycle = true;
                        }

                        if (_fuelConsumedForCurrentCycle)
                            SwitchState(); // Paused -> Incubating
                    }

                    break;
                }
            case CloningStatus.Incubating:
                {
                    if (!HasPendingClone)
                    {
                        Status = CloningStatus.Error;
                        return;
                    }

                    // stop if power is off, but don't put in paused state
                    // TODO: the longer power is off, start decrementing _incubationTicksRemaining to simulate deterioration?
                    if (!Powered) return;

                    if (!this.IsHashIntervalTick(GenTicks.TicksPerRealSecond))
                    {
                        bool has = HasActiveInducer();
                        if (has != _hadActiveInducer)
                        {
                            float modifier = has
                                ? (1f / RimgateModSettings.InducerIncubationSpeedFactor)
                                : RimgateModSettings.InducerIncubationSpeedFactor;

                            int newTotal = Mathf.Max(1, Mathf.RoundToInt(_incubationTicksTotal * modifier));
                            _incubationTicksRemaining = RescaleRemainingInt(_incubationTicksTotal, _incubationTicksRemaining, newTotal);
                            _incubationTicksTotal = newTotal;
                            _hadActiveInducer = has;
                        }
                    }

                    if (_incubationTicksRemaining > 0)
                        _incubationTicksRemaining--;
                    else
                    {
                        _incubationTicksRemaining = 0;
                        _fuelConsumedForCurrentCycle = false;
                        CloneUtility.FinalizeSpawn(this, PendingClonePawn, _calibrationOutcome);
                        SwitchState(); // Incubating -> Idle (via FinalizeSpawn)
                    }

                    break;
                }
            case CloningStatus.Error:
                {
                    SwitchState(); // Error -> Idle
                    break;
                }
        }
    }

    public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn pawn)
    {
        foreach (FloatMenuOption floatMenuOption in base.GetFloatMenuOptions(pawn))
            yield return floatMenuOption;

        bool canReach = ReachabilityUtility.CanReach(
            pawn,
            this,
            PathEndMode.InteractionCell,
            Danger.Deadly,
            false,
            false,
            TraverseMode.ByPawn);
        if (!canReach)
        {
            yield return new FloatMenuOption(Translator.Translate("CannotUseNoPath"), null);
            yield break;
        }

        if (!Powered)
        {
            string req = "not powered";
            yield return new FloatMenuOption("RG_CannotUse".Translate("NoPower".Translate()), null);
            yield break;
        }

        if (!ResearchUtil.WraithCloneGenomeComplete)
        {
            string req = $"{Analyzable?.Props?.requiresResearchDef?.label} knowledge required";
            yield return new FloatMenuOption("RG_CannotUse".Translate(req), null);
            yield break;
        }

        if (Status != CloningStatus.Idle) yield break;

        // initial pawn entering phase
        if (!HasHostPawn)
        {
            if (!pawn.RaceProps.IsFlesh)
            {
                string req = "not biologic";
                yield return new FloatMenuOption("RG_CannotUse".Translate(req), null);
                yield break;
            }

            if (QuestUtility.IsQuestLodger(pawn))
            {
                yield return new FloatMenuOption("RG_CannotUse".Translate("CryptosleepCasketGuestsNotAllowed".Translate()), null);
                yield break;
            }

            yield return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(
                    "RG_EnterCloningPod".Translate("RG_BeginCloneGenome".Translate()),
                    () => AssignJob(CloneType.Genome)),
                pawn,
                this,
                "ReservedBy",
                null);

            if (ResearchUtil.WraithCloneFullComplete)
                yield return FloatMenuUtility.DecoratePrioritizedTask(
                    new FloatMenuOption(
                        "RG_EnterCloningPod".Translate("RG_BeginCloneFull".Translate()),
                        () => AssignJob(CloneType.Full)),
                    pawn,
                    this,
                    "ReservedBy",
                    null);

            if (ResearchUtil.WraithCloneEnhancementComplete)
                yield return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(
                    "RG_EnterCloningPod".Translate("RG_BeginCloneSoldier".Translate()),
                    () => AssignJob(CloneType.Enhanced)),
                pawn,
                this,
                "ReservedBy",
                null);


            void AssignJob(CloneType jobType)
            {
                Job job = JobMaker.MakeJob(RimgateDefOf.Rimgate_EnterCloningPod, this);
                job.count = 1;
                if (pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc, false))
                    SetCloningType(jobType);
            }
            yield break;
        }

        // if we have a pawn, show what the user can do

        if (pawn.skills.GetSkill(SkillDefOf.Medicine).levelInt < RimgateModSettings.MedicineSkillReq)
        {
            string req = $"calibration requires {SkillDefOf.Medicine.label} >= {RimgateModSettings.MedicineSkillReq}";
            yield return new FloatMenuOption("RG_CannotUse".Translate(req), null);
            yield break;
        }

        string label = string.Empty;
        switch (_cloningType)
        {
            case CloneType.None:
                break;
            case CloneType.Genome:
                label = "RG_CloneOccupantGenes".Translate(HostPawn.LabelCap);
                break;
            case CloneType.Full:
                label = "RG_CloneOccupantFull".Translate(HostPawn.LabelCap);
                break;
            case CloneType.Enhanced:
                label = "RG_CloneOccupantSoldier".Translate(HostPawn.LabelCap);
                break;
            case CloneType.Reconstruct:
                label = "RG_CloneReconstructDead".Translate(HostPawn.LabelCap);
                break;
            default:
                break;
        }

        if (label.NullOrEmpty()) // something went wrong then...
            yield break;

        yield return FloatMenuUtility.DecoratePrioritizedTask(
            new FloatMenuOption(
                label,
                () =>
                {
                    Job job = JobMaker.MakeJob(RimgateDefOf.Rimgate_CalibrateClonePodForPawn, this);
                    job.count = 1;
                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc, false);
                }),
            pawn,
            this,
            "ReservedBy",
            null);
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos())
            yield return gizmo;

        Gizmo select = Building.SelectContainedItemGizmo(this, ContainedThing);
        if (select != null)
            yield return select;

        if (!Faction.IsOfPlayerFaction()) yield break;

        if (HasHostPawn && def.building.isPlayerEjectable)
        {
            Command_Action commandAction = new()
            {
                action = () =>
                {
                    EjectContents();
                    Reset();
                },
                defaultLabel = "RG_CommandCloningPodEjectLabel".Translate(),
                defaultDesc = "RG_CommandCloningPodEjectDesc".Translate(),
                hotKey = KeyBindingDefOf.Misc1,
                icon = RimgateTex.CloneEjectCommandTex
            };

            if (_innerContainer.Count == 0)
                commandAction.Disable("CommandPodEjectFailEmpty".Translate());

            yield return commandAction;
        }

        if (Status == CloningStatus.Incubating)
        {
            Command_Action commandAction = new()
            {
                action = () =>
                {
                    Reset();
                },
                defaultLabel = "RG_CommandCloningPodAbortIncubationLabel".Translate(),
                defaultDesc = "RG_CommandCloningPodAbortIncubationDesc".Translate(),
                hotKey = KeyBindingDefOf.Misc2,
                icon = RimgateTex.CancelCommandTex
            };
            yield return commandAction;
        }

        if (!ResearchUtil.WraithCloneGenomeComplete) yield break;

        bool disableActions = !Powered || Status != CloningStatus.Idle;
        string disabledReason = "RG_CannotUse".Translate(!Powered ? "NoPower".Translate() : "in use");

        Command_Action genomeAction = new Command_Action
        {
            defaultLabel = "RG_BeginCloneGenome".Translate(),
            defaultDesc = "RG_CloneOccupantGenes".Translate("a colonist"),
            icon = RimgateTex.CloneGenomeCommandTex,
            action = () => SetPawnAndJobOptions(CloneType.Genome),
            activateSound = SoundDefOf.Tick_Tiny
        };

        if (disableActions)
            genomeAction.Disable(disabledReason);

        yield return genomeAction;

        if (ResearchUtil.WraithCloneFullComplete)
        {
            Command_Action fullAction = new Command_Action
            {
                defaultLabel = "RG_BeginCloneFull".Translate(),
                defaultDesc = "RG_CloneOccupantFull".Translate("a colonist"),
                icon = RimgateTex.CloneFullCommandTex,
                action = () => SetPawnAndJobOptions(CloneType.Full),
                activateSound = SoundDefOf.Tick_Tiny
            };

            if (disableActions)
                fullAction.Disable(disabledReason);

            yield return fullAction;
        }

        if (ResearchUtil.WraithCloneEnhancementComplete)
        {
            Command_Action enhanceAction = new Command_Action
            {
                defaultLabel = "RG_BeginCloneSoldier".Translate(),
                defaultDesc = "RG_CloneOccupantSoldier".Translate("a colonist"),
                icon = RimgateTex.CloneEnhancedCommandTex,
                action = () => SetPawnAndJobOptions(CloneType.Enhanced),
                activateSound = SoundDefOf.Tick_Tiny
            };

            if (disableActions)
                enhanceAction.Disable(disabledReason);

            yield return enhanceAction;
        }

        if (ResearchUtil.WraithCloneCorpseComplete)
        {
            Command_Action corpseAction = new Command_Action
            {
                defaultLabel = "RG_BeginCloneCorpse".Translate(),
                defaultDesc = "RG_CloneReconstructDead".Translate("the deceased"),
                icon = RimgateTex.CloneReconstructCommandTex,
                action = () => SetPawnAndJobOptions(CloneType.Reconstruct, true),
                activateSound = SoundDefOf.Tick_Tiny
            };

            if (disableActions)
                corpseAction.Disable(disabledReason);

            yield return corpseAction;
        }

        void SetPawnAndJobOptions(CloneType cloneType, bool forCorpse = false)
        {
            var options = new List<FloatMenuOption>();
            foreach (Pawn pawn in Map.mapPawns.FreeColonistsSpawned)
            {
                if (pawn.Downed || pawn.InMentalState) continue;

                string invalidReason = string.Empty;
                if (!pawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly))
                    invalidReason = "NoPath".Translate().CapitalizeFirst();

                string label = pawn.Label + (invalidReason.NullOrEmpty() ? "" : ": " + invalidReason);
                Action action = null;
                if (invalidReason.NullOrEmpty())
                {
                    action = delegate
                    {
                        if (forCorpse)
                        {
                            var tp = new TargetingParameters
                            {
                                canTargetMechs = false,
                                canTargetAnimals = false,
                                canTargetSubhumans = false,
                                canTargetHumans = true,
                                canTargetCorpses = true,
                                onlyTargetCorpses = true
                            };
                            Find.Targeter.BeginTargeting(tp, target =>
                            {
                                Corpse corpse = target.Thing as Corpse;
                                if (corpse == null)
                                    return;

                                Job job = JobMaker.MakeJob(RimgateDefOf.Rimgate_CarryCorpseToCloningPod, corpse, this);
                                job.count = 1;
                                if (pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc, false))
                                    SetCloningType(cloneType);
                            });
                        }
                        else
                        {
                            Job job = JobMaker.MakeJob(RimgateDefOf.Rimgate_EnterCloningPod, this);
                            job.count = 1;
                            if (pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc, false))
                                SetCloningType(cloneType);
                        }
                    };
                }

                options.Add(new FloatMenuOption(label, action));
            }

            if (options.Count > 0)
                Find.WindowStack.Add(new FloatMenu(options));
        }

        if (DebugSettings.ShowDevGizmos && Status == CloningStatus.Incubating)
        {
            yield return new Command_Action
            {
                defaultLabel = "DEV: Finish Incubation",
                action = () => _incubationTicksRemaining = 0
            };
        }
    }

    public override void DeSpawn(DestroyMode mode = 0)
    {
        EjectContents();
        Reset();
        base.DeSpawn(mode);
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        Map map = base.Map;
        base.Destroy(mode);
        if (_innerContainer.Count > 0 && (mode == DestroyMode.Deconstruct || mode == DestroyMode.KillFinalize))
        {
            if (mode != DestroyMode.Deconstruct)
            {
                List<Pawn> list = new List<Pawn>();
                foreach (Thing item2 in (IEnumerable<Thing>)_innerContainer)
                {
                    if (item2 is Pawn item)
                        list.Add(item);
                }

                foreach (Pawn item3 in list)
                    HealthUtility.DamageUntilDowned(item3);
            }

            _innerContainer.TryDropAll(base.Position, map, ThingPlaceMode.Near);
        }

        _innerContainer.ClearAndDestroyContents();
        _pendingCloneContainer.ClearAndDestroyContents();
    }

    public override string GetInspectString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(base.GetInspectString());
        string str = (_contentsKnown ? _innerContainer.ContentsString : ((string)"UnknownLower".Translate()));
        if (sb.Length > 0)
            sb.AppendLine();
        sb.Append("CasketContains".Translate() + ": " + str.CapitalizeFirst());

        if (Status == CloningStatus.Incubating)
        {
            sb.AppendLine();
            if (!Powered)
                sb.Append("RG_IncubationPaused".Translate("NoPower".Translate()));
            else
                sb.Append("RG_IncubationInProgress".Translate(GetIncubationProgress().ToStringPercent()));
        }
        else if (Status == CloningStatus.Paused && !TryGetCostForCurrentCycle(out int cost))
        {
            sb.AppendLine();
            sb.Append("RG_IncubationPaused".Translate($"need x{cost} biomass loaded"));
        }

        return sb.ToString().TrimEndNewlines();
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
    {
        foreach (StatDrawEntry state in base.SpecialDisplayStats())
            yield return state;

        var baseCost = BiomassCostPerCycle;
        int baseCalibrationTicks = RimgateModSettings.BaseCalibrationTicks;
        int baseIncubationTicks = RimgateModSettings.BaseIncubationTicks;

        float modifierCost = 1f;
        int stabilizerCount = ActiveBiomassStabilizerCount();
        if (stabilizerCount > 0)
        {
            float reduction = RimgateModSettings.StabilizerBiomassCostReduction * stabilizerCount;
            reduction = Mathf.Clamp(reduction, 0f, 0.5f); // max 50% reduction
            modifierCost *= (1f - reduction);
        }
        float modifierCal = 1f;
        float modifierInc = 1f;
        if (HasActiveInducer())
        {
            modifierCal /= RimgateModSettings.InducerCalibrationSpeedFactor;
            modifierInc /= RimgateModSettings.InducerIncubationSpeedFactor;
        }

        yield return GetCostStatFor("RG_BeginCloneGenome".Translate(), baseCost, modifierCost, 4494);

        yield return GetCalibrationTicksFor("RG_BeginCloneGenome".Translate(), baseCalibrationTicks, modifierCal, 4494);

        int genomeIncTicks = Mathf.Max(0, Mathf.RoundToInt(baseIncubationTicks * modifierInc));
        yield return GetIncubationTicksFor("RG_BeginCloneGenome".Translate(), genomeIncTicks, modifierInc, 4494);

        if (ResearchUtil.WraithCloneFullComplete)
        {
            int fullCost = Mathf.Max(0, Mathf.RoundToInt(baseCost * RimgateModSettings.FullCloneFactor));
            yield return GetCostStatFor("RG_BeginCloneFull".Translate(), fullCost, modifierCost, 4493);

            int fullCalTicks = Mathf.Max(0, Mathf.RoundToInt(baseCalibrationTicks * RimgateModSettings.FullCloneFactor));
            yield return GetCalibrationTicksFor("RG_BeginCloneFull".Translate(), fullCalTicks, modifierCal, 4493);

            int fullIncTicks = Mathf.Max(0, Mathf.RoundToInt((baseIncubationTicks * RimgateModSettings.FullCloneFactor) * modifierInc));
            yield return GetIncubationTicksFor("RG_BeginCloneFull".Translate(), fullIncTicks, modifierInc, 4493);
        }

        if (ResearchUtil.WraithCloneEnhancementComplete)
        {
            int enhancedCost = Mathf.Max(0, Mathf.RoundToInt(baseCost * RimgateModSettings.EnhancedCloneFactor));
            yield return GetCostStatFor("RG_BeginCloneSoldier".Translate(), enhancedCost, modifierCost, 4492);

            int enhancedCalTicks = Mathf.Max(0, Mathf.RoundToInt(baseCalibrationTicks * RimgateModSettings.EnhancedCloneFactor));
            yield return GetCalibrationTicksFor("RG_BeginCloneSoldier".Translate(), enhancedCalTicks, modifierCal, 4492);

            int enhancedIncTicks = Mathf.Max(0, Mathf.RoundToInt((baseIncubationTicks * RimgateModSettings.EnhancedCloneFactor) * modifierInc));
            yield return GetIncubationTicksFor("RG_BeginCloneSoldier".Translate(), enhancedIncTicks, modifierInc, 4492);
        }

        if (ResearchUtil.WraithCloneCorpseComplete)
        {
            int reconstructCost = Mathf.Max(0, Mathf.RoundToInt(baseCost * RimgateModSettings.ReconstructionCloneFactor));
            yield return GetCostStatFor("RG_BeginCloneCorpse".Translate(), reconstructCost, modifierCost, 4491);

            int reconstructCalTicks = Mathf.Max(0, Mathf.RoundToInt(baseCalibrationTicks * RimgateModSettings.ReconstructionCloneFactor));
            yield return GetCalibrationTicksFor("RG_BeginCloneCorpse".Translate(), reconstructCalTicks, modifierCal, 4491);

            int reconstructIncTicks = Mathf.Max(0, Mathf.RoundToInt(baseIncubationTicks * RimgateModSettings.ReconstructionCloneFactor));
            yield return GetIncubationTicksFor("RG_BeginCloneCorpse".Translate(), reconstructIncTicks, modifierInc, 4491);
        }

        static StatDrawEntry GetCostStatFor(string labelKey, int baseCost, float modifier, int priority)
        {
            int modifiedCost = Mathf.Max(0, Mathf.RoundToInt(baseCost * modifier));
            StringBuilder sb = new StringBuilder("RG_CloningPod_Stat_BiomassConsumption_Desc".Translate(labelKey));
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("StatsReport_BaseValue".Translate() + $": {baseCost}");
            sb.AppendLine();
            if (modifier != 1f)
            {
                sb.AppendLine("RG_StatsReport_BiomassStabilizerMultiplier".Translate() + $": x{modifier.ToStringPercent()}");
                sb.AppendLine();
            }
            sb.Append("StatsReport_FinalValue".Translate() + $": {modifiedCost.ToString("F0")}");
            return new StatDrawEntry(
                StatCategoryDefOf.Building,
                "RG_CloningPod_Stat_BiomassConsumption_Label".Translate(labelKey),
                modifiedCost.ToString("F0"),
                sb.ToString().TrimEndNewlines(),
                priority);
        }

        static StatDrawEntry GetCalibrationTicksFor(string labelKey, int baseTicks, float modifier, int priority)
        {
            int modifiedTicks = Mathf.Max(0, Mathf.RoundToInt(baseTicks * modifier));
            StringBuilder sb = new StringBuilder("RG_CloningPod_Stat_CalibrationTicks_Desc".Translate(labelKey));
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("StatsReport_BaseValue".Translate() + $": {baseTicks.ToStringTicksToDays()}");
            sb.AppendLine();
            if (modifier != 1f)
            {
                sb.AppendLine("RG_StatsReport_InducerMultiplier".Translate() + $": x{modifier.ToStringPercent()}");
                sb.AppendLine();
            }
            sb.Append("StatsReport_FinalValue".Translate() + $": {modifiedTicks.ToStringTicksToDays()}");
            return new StatDrawEntry(
                StatCategoryDefOf.Building,
                "RG_CloningPod_Stat_CalibrationTicks_Label".Translate(labelKey),
                modifiedTicks.ToStringTicksToDays(),
                sb.ToString().TrimEndNewlines(),
                priority);
        }

        static StatDrawEntry GetIncubationTicksFor(string labelKey, int baseTicks, float modifier, int priority)
        {
            int modifiedTicks = Mathf.Max(0, Mathf.RoundToInt(baseTicks * modifier));
            StringBuilder sb = new StringBuilder("RG_CloningPod_Stat_IncubationTicks_Desc".Translate(labelKey));
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("StatsReport_BaseValue".Translate() + $": {baseTicks.ToStringTicksToDays()}");
            sb.AppendLine();
            if (modifier != 1f)
            {
                sb.AppendLine("RG_StatsReport_InducerMultiplier".Translate() + $": x{modifier.ToStringPercent()}");
                sb.AppendLine();
            }
            sb.Append("StatsReport_FinalValue".Translate() + $": {modifiedTicks.ToStringTicksToDays()}");
            return new StatDrawEntry(
                StatCategoryDefOf.Building,
                "RG_CloningPod_Stat_IncubationTicks_Label".Translate(labelKey),
                modifiedTicks.ToStringTicksToDays(),
                sb.ToString().TrimEndNewlines(),
                priority);
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref _innerContainer, "_innerContainer", this);
        Scribe_Deep.Look(ref _pendingCloneContainer, "_pendingCloneContainer", this);
        Scribe_Values.Look(ref _contentsKnown, "_contentsKnown", false);
        Scribe_Values.Look(ref OpenedSignal, "OpenedSignal");
        Scribe_Values.Look(ref Status, "Status", CloningStatus.Idle);
        Scribe_Values.Look(ref _cloningType, "_cloningType", CloneType.None);
        Scribe_Values.Look(ref _calibrationTicksTotal, "_calibrationTicksTotal", 0);
        Scribe_Values.Look(ref _calibrationWorkRemaining, "_calibrationWorkRemaining", 0f);
        Scribe_Values.Look(ref _incubationTicksTotal, "_incubationTicksTotal", 0);
        Scribe_Values.Look(ref _incubationTicksRemaining, "_incubationTicksRemaining", 0);
        Scribe_Values.Look(ref _hadActiveInducer, "_hadActiveInducer", false);
        Scribe_Values.Look(ref _fuelConsumedForCurrentCycle, "_fuelConsumedForCurrentCycle", false);
        Scribe_Deep.Look(ref _calibrationOutcome, "_calibrationOutcome");
        Scribe_Values.Look(ref _hostSavedFoodNeed, "_hostSavedFoodNeed", 0f);
        Scribe_Values.Look(ref _hostSavedDbhThirstNeed, "_hostSavedDbhThirstNeed", 0f);
        Scribe_Values.Look(ref _hostNeedsSnapshotTaken, "_hostNeedsSnapshotTaken", false);
    }

    public ThingOwner GetDirectlyHeldThings()
    {
        return _innerContainer;
    }

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    public void Open()
    {
        if (HasHostPawn)
        {
            EjectContents();
            Reset();
            if (!OpenedSignal.NullOrEmpty())
            {
                Find.SignalManager.SendSignal(new Signal(OpenedSignal, this.Named("SUBJECT")));
            }

            DirtyMapMesh(base.Map);
        }
    }

    public bool Accepts(Thing thing)
    {
        return _innerContainer.CanAcceptAnyOf(thing);
    }

    public bool TryAcceptThing(Thing thing, bool allowSpecialEffects = true)
    {
        if (!Accepts(thing))
            return false;

        bool flag;
        if (thing.holdingOwner != null)
        {
            thing.holdingOwner.TryTransferToContainer(thing, _innerContainer, thing.stackCount);
            flag = true;
        }
        else
            flag = _innerContainer.TryAdd(thing);

        if (flag)
        {
            if (thing.Faction != null && thing.Faction.IsPlayer)
                _contentsKnown = true;

            if (allowSpecialEffects)
                SoundStarter.PlayOneShot(
                    SoundDefOf.CryptosleepCasket_Accept,
                    new TargetInfo(Position, Map, false));

            return true;
        }

        return false;
    }

    public void SetCloningType(CloneType type)
    {
        _cloningType = type;
    }

    public void InitiateCalibrationProcess()
    {
        _calibrationTicksTotal = GetTotalCalibrationTicks();
        _calibrationWorkRemaining = _calibrationTicksTotal;
        SwitchState(); // Idle -> CloningStarted
    }

    public bool TryGetCostForCurrentCycle(out int result)
    {
        float cost = BiomassCostPerCycle;

        if (_cloningType == CloneType.Full)
            cost *= RimgateModSettings.FullCloneFactor;
        else if (_cloningType == CloneType.Enhanced)
            cost *= RimgateModSettings.EnhancedCloneFactor;
        else if (_cloningType == CloneType.Reconstruct)
            cost *= RimgateModSettings.ReconstructionCloneFactor;

        int stabilizerCount = ActiveBiomassStabilizerCount();
        if (stabilizerCount > 0)
        {
            float reduction = RimgateModSettings.StabilizerBiomassCostReduction * stabilizerCount;
            reduction = Mathf.Clamp(reduction, 0f, 0.5f); // max 50% reduction
            cost *= 1f - reduction;
        }

        result = Mathf.Max(0, Mathf.RoundToInt(cost));

        return Refuelable?.Fuel >= result;
    }

    private int GetTotalCalibrationTicks()
    {
        int result = RimgateModSettings.BaseCalibrationTicks;

        if (_cloningType == CloneType.Full)
            result = (int)(result * RimgateModSettings.FullCloneFactor);
        else if (_cloningType == CloneType.Enhanced)
            result = (int)(result * RimgateModSettings.EnhancedCloneFactor);
        else if (_cloningType == CloneType.Reconstruct)
            result = (int)(result * RimgateModSettings.ReconstructionCloneFactor);

        if (HasActiveInducer())
            result = (int)(result / RimgateModSettings.InducerCalibrationSpeedFactor);

        result = Mathf.Clamp(result, 0, int.MaxValue);

        return result;
    }

    // Calibration work tick handled by JobDriver_CalibrateClonePod
    public void TickCalibrationWork(Pawn actor)
    {
        float workDone = StatExtension.GetStatValue(actor, RimgateDefOf.MedicalOperationSpeed, true, -1);
        _calibrationWorkRemaining -= workDone;

        actor.skills.Learn(SkillDefOf.Intellectual, 0.11f, false, false);

        if (_calibrationWorkRemaining > 0) return;

        _calibrationWorkRemaining = 0f;
        if (!TryStageClone())
        {
            Status = CloningStatus.Error;
            Messages.Message(
                "RG_CloningPodCalibrationFailed".Translate("RG_CloningPodCalibrationFailed_NotStaged".Translate()),
                this,
                MessageTypeDefOf.NegativeEvent);
            return;
        }

        SwitchState(); // CloningStarted -> CalibrationFinished
    }

    public float GetCalibrationProgress()
    {
        if (Status != CloningStatus.CloningStarted || _calibrationTicksTotal <= 0)
            return 0f;
        float total = _calibrationTicksTotal;
        float done = _calibrationTicksTotal - _calibrationWorkRemaining;
        return Mathf.Clamp01(done / total);
    }

    private int GetTotalIncubationTicks(CalibrationOutcome outcome)
    {
        int result = RimgateModSettings.BaseIncubationTicks;

        if (_cloningType == CloneType.Full)
            result = (int)(result * RimgateModSettings.FullCloneFactor);
        else if (_cloningType == CloneType.Enhanced)
            result = (int)(result * RimgateModSettings.EnhancedCloneFactor);
        else if (_cloningType == CloneType.Reconstruct)
            result = (int)(result * RimgateModSettings.ReconstructionCloneFactor);

        // Note: not displayed in special stats since outcome is unknown at that time
        if (!outcome.AnyIssues)
            result = (int)(result * 0.75f); // faster if no issues
        else if (outcome.MajorIssues)
            result = (int)(result * 1.5f); // slower if major failure occurs
        else if (outcome.MinorIssues)
            result = (int)(result * 1.25f); // slower if minor failure occurs

        if (HasActiveInducer())
            result = (int)(result / RimgateModSettings.InducerIncubationSpeedFactor);

        result = Mathf.Clamp(result, 0, int.MaxValue);

        return result;
    }

    private bool TryStageClone()
    {
        if (HasPendingClone)
            return false;

        if (!CloneUtility.TryCreateClonePawn(this, _cloningType, out Pawn clone, out CalibrationOutcome outcome))
            return false;

        _pendingCloneContainer.TryAdd(clone);
        _calibrationOutcome = outcome;

        // Determine incubation time based on outcome
        _incubationTicksTotal = GetTotalIncubationTicks(outcome);
        _incubationTicksRemaining = _incubationTicksTotal;

        return true;
    }

    public float GetIncubationProgress()
    {
        if (Status != CloningStatus.Incubating || _incubationTicksTotal <= 0)
            return 0f;
        float total = _incubationTicksTotal;
        float done = _incubationTicksTotal - _incubationTicksRemaining;
        return Mathf.Clamp01(done / total);
    }

    private int ActiveBiomassStabilizerCount()
    {
        if (Facilities == null) return 0;
        var list = Facilities.LinkedFacilitiesListForReading;
        if (list == null || list.Count == 0) return 0;

        int n = 0;
        for (int i = 0; i < list.Count; i++)
        {
            var t = list[i];
            if (t?.def != RimgateDefOf.Rimgate_WraithBiomassStabilizer) continue;

            // Must be powered to contribute
            var power = t.TryGetComp<CompPowerTrader>();
            if (power != null && power.PowerOn) n++;
        }
        return n;
    }

    private void DecayActiveBiomassStabilizers()
    {
        if (Facilities == null) return; // no fuel, no stabilizer effect

        var list = Facilities.LinkedFacilitiesListForReading;
        if (list == null || list.Count == 0) return;

        float rate = RimgateModSettings.StabilizerDeteriorationFactor;
        if (rate <= 0f) rate = 0;

        for (int i = 0; i < list.Count; i++)
        {
            var t = list[i];
            if (t == null || t.def != RimgateDefOf.Rimgate_WraithBiomassStabilizer) continue;

            // Must be powered to contribute
            var power = t.TryGetComp<CompPowerTrader>();
            if (power != null && !power.PowerOn) continue;

            if (Rand.Chance(0.72f))
                t.TakeDamage(new DamageInfo(DamageDefOf.Deterioration, t.def.BaseMaxHitPoints * rate));
        }
    }

    private bool HasActiveInducer()
    {
        if (Facilities == null) return false;
        var list = Facilities.LinkedFacilitiesListForReading;
        if (list == null || list.Count == 0) return false;
        for (int i = 0; i < list.Count; i++)
        {
            var t = list[i];
            if (t?.def != RimgateDefOf.Rimgate_WraithCyclicWaveInducer) continue;
            // Must be powered to contribute
            var power = t.TryGetComp<CompPowerTrader>();
            if (power != null && power.PowerOn) return true;
        }
        return false;
    }

    public void SwitchState()
    {
        CloningStatus oldStatus = Status;
        switch (Status)
        {
            case CloningStatus.Idle:
                Status = CloningStatus.CloningStarted;
                break;

            case CloningStatus.CloningStarted:
                Status = CloningStatus.CalibrationFinished;
                break;

            case CloningStatus.CalibrationFinished:
                Status = CloningStatus.HostDischarged;
                break;

            case CloningStatus.HostDischarged:
                Status = CloningStatus.Incubating;
                break;

            case CloningStatus.Paused:
                Status = CloningStatus.Incubating;
                break;

            case CloningStatus.Incubating:
                {
                    Reset(); // sets state to Idle
                    break;
                }

            case CloningStatus.Error:
                {
                    EjectContents();
                    Reset();
                    break;
                }
        }

        if (RimgateMod.Debug)
            Log.Message(this + $" :: state change from {oldStatus.ToStringSafe().Colorize(Color.yellow)}"
                + $" to {Status.ToStringSafe().Colorize(Color.yellow)}");
    }

    public void EjectContents()
    {
        if (!HasHostPawn)
            return;

        ThingDef filthSlime = ThingDefOf.Filth_Slime;
        foreach (Thing thing in _innerContainer)
        {
            if (thing is not Pawn pawn)
                continue;

            PawnComponentsUtility.AddComponentsForSpawn(pawn);
            pawn.filth.GainFilth(filthSlime);
            if (pawn.RaceProps.IsFlesh)
                pawn.health.AddHediff(RimgateDefOf.Rimgate_ClonePodSickness, null, null, null);
        }

        if (!Destroyed)
            SoundStarter.PlayOneShot(
                SoundDefOf.CryptosleepCasket_Eject,
                SoundInfo.InMap(new TargetInfo(Position, Map, false), MaintenanceType.None));

        _innerContainer.TryDropAll(InteractionCell, base.Map, ThingPlaceMode.Near);
        _innerContainer.ClearAndDestroyContents();
        _contentsKnown = true;
    }

    public void Reset()
    {
        _cloningType = CloneType.None;
        Status = CloningStatus.Idle;
        _hadActiveInducer = false;

        _calibrationWorkRemaining = 0f;
        _incubationTicksRemaining = 0;
        _fuelConsumedForCurrentCycle = false;
        _calibrationOutcome = null;

        _hostSavedFoodNeed = 0f;
        _hostSavedDbhThirstNeed = 0f;
        _hostNeedsSnapshotTaken = false;

        _innerContainer.ClearAndDestroyContents();
        _pendingCloneContainer.ClearAndDestroyContents();
    }

    private static int RescaleRemainingInt(int oldTotal, int oldRemaining, int newTotal)
    {
        if (oldTotal <= 0) return newTotal;
        float progress = Mathf.Clamp01(1f - (oldRemaining / (float)oldTotal));
        return Mathf.Clamp(Mathf.RoundToInt(newTotal * (1f - progress)), 0, newTotal);
    }

    private static float RescaleRemainingFloat(int oldTotal, float oldRemaining, int newTotal)
    {
        if (oldTotal <= 0) return newTotal;
        float progress = Mathf.Clamp01(1f - (oldRemaining / oldTotal));
        return Mathf.Clamp(newTotal * (1f - progress), 0f, newTotal);
    }

    public static Building_CloningPod FindCloningPodFor(
      Thing rescuee,
      Pawn traveler,
      bool ignoreOtherReservations = false)
    {
        _cachedPodDefs ??= DefDatabase<ThingDef>.AllDefs
            .Where<ThingDef>(def => typeof(Building_CloningPod).IsAssignableFrom(def.thingClass))
            .ToList();
        foreach (ThingDef thingDef in _cachedPodDefs)
        {
            bool queuing = KeyBindingDefOf.QueueOrder.IsDownEvent;
            Building_CloningPod pod = GenClosest.ClosestThingReachable(
                rescuee.Position,
                rescuee.Map,
                ThingRequest.ForDef(thingDef),
                PathEndMode.InteractionCell,
                TraverseParms.For(
                    traveler,
                    Danger.Deadly,
                    TraverseMode.ByPawn,
                    false,
                    false,
                    false,
                    true),
                9999f,
                Validator) as Building_CloningPod;

            if (pod != null)
                return pod;

            bool Validator(Thing x)
            {
                if (x is not Building_CloningPod pod || pod.HasHostPawn || !pod.Powered)
                    return false;

                if (!queuing || !traveler.HasReserved(x))
                    return traveler.CanReserve(x, 1, -1, null, ignoreOtherReservations);

                return false;
            }
        }

        return null;
    }
}
