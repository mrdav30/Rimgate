using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Rimgate;

public enum SarcophagusStatus
{
    Idle = 0,
    DiagnosisStarted,
    DiagnosisFinished,
    HealingStarted,
    HealingFinished,
    PatientDischarged,
    Error
}

public class Building_Sarcophagus_Ext : DefModExtension
{
    public float maxDiagnosisTime = 5f;

    public float maxPerHediffHealingTime = 10f;

    public float diagnosisModePowerConsumption = 4000f;

    public float healingModePowerConsumption = 16000f;

    public float powerConsumptionReductionFactor = 0.65f;

    public bool applyAddictionHediff;

    public float addictiveness;

    public float severity = -1f;

    public float existingAddictionSeverityOffset = 0.1f;

    public float needLevelOffset = 1f;

    public GraphicData sarchophagusGlowGraphicData;

    public List<HediffDef> alwaysTreatableHediffs;

    public List<HediffDef> neverTreatableHediffs;

    public List<HediffDef> nonCriticalTreatableHediffs;

    public List<TraitDef> alwaysTreatableTraits;

    public List<HediffDef> usageBlockingHediffs;

    public List<TraitDef> usageBlockingTraits;

    public List<string> disallowedRaces;

    [MayRequireBiotech]
    public List<XenotypeDef> disallowedXenotypes;

    public override IEnumerable<string> ConfigErrors()
    {
        if (maxDiagnosisTime > 30f)
        {
            yield return $"{nameof(maxDiagnosisTime)} above allowed maximum; value capped at 30 seconds";
            maxDiagnosisTime = 30f;
        }

        if (maxPerHediffHealingTime > 30f)
        {
            yield return $"{nameof(maxPerHediffHealingTime)} above allowed maximum; value capped at 30 seconds";
            maxPerHediffHealingTime = 30f;
        }
    }
}

public class Building_Sarcophagus : Building, IThingHolder, IOpenable, ISearchableContents
{
    public Building_Sarcophagus_Ext Props => _cachedProps ??= def.GetModExtension<Building_Sarcophagus_Ext>();

    public CompPowerTrader Power => _powerTrader ??= GetComp<CompPowerTrader>();

    public CompAssignableToPawn_Sarcophagus Assignable => _assignable ??= GetComp<CompAssignableToPawn_Sarcophagus>();

    public Comp_AnalyzableResearchWhen Analyzable => _analyzable ??= GetComp<Comp_AnalyzableResearchWhen>();

    public int DiagnosingTicks = 0;

    public int MaxDiagnosingTicks;

    public int PatientBodySizeScaledMaxDiagnosingTicks;

    public int HealingTicks = 0;

    public int MaxHealingTicks;

    public int PatientBodySizeScaledMaxHealingTicks;

    public float DefaultIdlePower => Power?.Props.PowerConsumption ?? 0;

    public float DiagnosingPowerConsumption => Props.diagnosisModePowerConsumption * CurrentPowerMultiplier;

    public float HealingPowerConsumption => Props.healingModePowerConsumption * CurrentPowerMultiplier;

    public float CurrentPowerMultiplier => ResearchUtil.SarcophagusOptimizationComplete
            ? Props.powerConsumptionReductionFactor
            : 1f;

    public List<HediffDef> AlwaysTreatableHediffs => Props.alwaysTreatableHediffs;

    public List<HediffDef> NeverTreatableHediffs => Props.neverTreatableHediffs;

    public List<HediffDef> NonCriticalTreatableHediffs => Props.nonCriticalTreatableHediffs;

    public List<HediffDef> UsageBlockingHediffs => Props.usageBlockingHediffs;

    public List<TraitDef> UsageBlockingTraits => Props.usageBlockingTraits;

    public List<TraitDef> AlwaysTreatableTraits => Props.alwaysTreatableTraits;

    public List<string> DisallowedRaces => Props.disallowedRaces;

    public List<XenotypeDef> DisallowedXenotypes => Props.disallowedXenotypes;

    public int ProgressHealingTicks = 0;

    public int TotalHealingTicks = 0;

    public bool AllowGuests = false;

    public bool AllowSlaves = false;

    public bool AllowPrisoners = false;

    public SarcophagusStatus Status = SarcophagusStatus.Idle;

    public string OpenedSignal;

    private Sustainer _wickSustainer;

    protected ThingOwner<Thing> _innerContainer;

    protected bool _contentsKnown;

    public int OpenTicks => 250;

    public const float MaxBodySize = 9999f;

    public Pawn PatientPawn
    {
        get
        {
            if (_innerContainer == null || _innerContainer.Count == 0)
                return null;

            var pawn = _innerContainer.OfType<Pawn>().FirstOrDefault();
            if (pawn != null)
                return pawn;

            Log.Error($"{ThingID} _innerContainer is not empty but contains no Pawn.");
            return null;
        }
    }

    public bool CanOpen => HasAnyContents;

    public bool HasAnyContents => _innerContainer.Count > 0;

    private Building_Sarcophagus_Ext _cachedProps;

    public ThingOwner SearchableContents => _innerContainer;

    private Mesh GlowMesh => _cachedGlowMesh ??= Props.sarchophagusGlowGraphicData?.Graphic.MeshAt(Rotation);

    private Material GlowMaterial => _cachedGlowMaterial ??= Props.sarchophagusGlowGraphicData?.Graphic.MatAt(Rotation, null);

    private List<Hediff> _patientTreatableHediffs = new();

    private float _patientSavedFoodNeed;

    private float _patientSavedDbhThirstNeed;

    private bool _patientNeedsSnapshotTaken;

    private List<Trait> _patientTraitsToRemove = new();

    private CompPowerTrader _powerTrader;

    private CompAssignableToPawn_Sarcophagus _assignable;

    private Comp_AnalyzableResearchWhen _analyzable;

    private Material _cachedGlowMaterial;

    private Mesh _cachedGlowMesh;

    public Building_Sarcophagus()
    {
        _innerContainer = new ThingOwner<Thing>(this, oneStackOnly: true);
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);

        MaxDiagnosingTicks = GenTicks.SecondsToTicks(Props.maxDiagnosisTime);
        MaxHealingTicks = GenTicks.SecondsToTicks(Props.maxPerHediffHealingTime);

        if (Faction?.IsPlayer == true)
            _contentsKnown = true;
    }

    protected override void Tick()
    {
        if (!Spawned || this.IsMinified())
            return;

        base.Tick();

        // State-dependent power consumption
        switch (Status)
        {
            case SarcophagusStatus.Idle:
                Power.PowerOutput = -DefaultIdlePower;
                break;

            case SarcophagusStatus.DiagnosisStarted:
            case SarcophagusStatus.DiagnosisFinished:
                Power.PowerOutput = -DiagnosingPowerConsumption;
                break;

            case SarcophagusStatus.HealingStarted:
            case SarcophagusStatus.HealingFinished:
                Power.PowerOutput = -HealingPowerConsumption;
                break;
        }

        // Main patient treatment cycle logic
        var patient = PatientPawn;
        if (patient != null)
        {
            PatientBodySizeScaledMaxDiagnosingTicks = Mathf.CeilToInt(MaxDiagnosingTicks * patient.BodySize);
            PatientBodySizeScaledMaxHealingTicks = Mathf.CeilToInt(MaxHealingTicks * patient.BodySize);

            // Interrupt treatment on power loss
            if (!Power.PowerOn)
            {
                if (RimgateMod.Debug)
                    Log.Message(this + $" :: Lost power while running (state: {Status})");

                DischargePatient(patient, false);
                EjectContents();
                Reset();
                return;
            }

            // Main logic
            switch (Status)
            {
                case SarcophagusStatus.Idle:
                    {
                        DiagnosingTicks = PatientBodySizeScaledMaxDiagnosingTicks;

                        if (!_patientNeedsSnapshotTaken)
                        {
                            // Save initial patient food need level
                            if (patient.needs.food != null)
                                _patientSavedFoodNeed = patient.needs.food.CurLevelPercentage;

                            // Save initial patient DBH thirst and reset DBH bladder/hygiene need levels
                            if (ModCompatibility.DbhIsActive)
                            {
                                _patientSavedDbhThirstNeed = DbhCompatibility.GetThirstNeedCurLevelPercentage(patient);
                                DbhCompatibility.SetBladderNeedCurLevelPercentage(patient, 1f);
                                DbhCompatibility.SetHygieneNeedCurLevelPercentage(patient, 1f);
                            }

                            _patientNeedsSnapshotTaken = true;
                        }

                        if (RimgateMod.Debug)
                            Log.Message($"\t{patient} :: initial DiagnosingTicks = {DiagnosingTicks}");

                        SwitchState();
                        break;
                    }
                case SarcophagusStatus.DiagnosisStarted:
                    {
                        DiagnosingTicks--;
                        if (DiagnosingTicks <= 0)
                            SwitchState();
                        break;
                    }
                case SarcophagusStatus.DiagnosisFinished:
                    {
                        DiagnosePatient(patient);
                        // Skip treatment if no treatable hediffs are found
                        if (_patientTreatableHediffs.NullOrEmpty())
                        {
                            if (RimgateMod.Debug)
                                Log.Message(this + $" :: Discharging patient since there are no hediffs to treat");

                            // No hediffs to heal, but we still want to cleanly discharge + restore needs.
                            DischargePatient(patient, true);

                            // if we're only using the sarcophagus for an addiction, adjust need
                            HandleAfterEffects(patient, false);

                            EjectContents();
                            Status = SarcophagusStatus.PatientDischarged;
                        }
                        else
                        {
                            HealingTicks = ScaleHealingTicks(patient);
                            if (RimgateMod.Debug)
                            {
                                Log.Message($"\t{patient} :: first hediff HealingTicks = {HealingTicks}"
                                    + $" (hediff count: {_patientTreatableHediffs.Count()})");
                            }

                            SwitchState();
                        }
                        break;
                    }
                case SarcophagusStatus.HealingStarted:
                    {
                        HealingTicks--;
                        ProgressHealingTicks++;
                        if (HealingTicks <= 0)
                            SwitchState();
                        break;
                    }
                case SarcophagusStatus.HealingFinished:
                    {
                        // Don't remove 'good' treatable Hediffs
                        // but instead treat them with 100% quality
                        // (unless the 'good' Hediff is whitelisted as always treatable)
                        var hediff = _patientTreatableHediffs.First();
                        bool hasTendable = !hediff.def.isBad
                            && !AlwaysTreatableHediffs.Contains(hediff.def)
                            && !NonCriticalTreatableHediffs.Contains(hediff.def);
                        if (hasTendable)
                            hediff.Tended(1, 1);
                        else
                            patient.health.hediffSet.hediffs.Remove(hediff);

                        _patientTreatableHediffs.RemoveAt(0);
                        if (!_patientTreatableHediffs.NullOrEmpty())
                        {
                            HealingTicks = ScaleHealingTicks(patient);
                            if (RimgateMod.Debug)
                            {
                                Log.Message($"\t{patient} :: next hediff HealingTicks = {HealingTicks}"
                                    + $" (hediff count: {_patientTreatableHediffs.Count()})");
                            }

                            // Jump back to the previous state to start healing the next hediff
                            Status = SarcophagusStatus.HealingStarted;
                        }
                        else
                        {
                            DischargePatient(patient);
                            // apply addiction and adjust needs
                            HandleAfterEffects(patient);
                            EjectContents();
                            SwitchState();
                        }
                        break;
                    }
                case SarcophagusStatus.PatientDischarged:
                    {
                        SwitchState();
                        break;
                    }
            }

            // Suspend patient needs during diagnosis and treatment
            bool suspendNeeds = Status != SarcophagusStatus.Idle
                && Status != SarcophagusStatus.PatientDischarged
                && Status != SarcophagusStatus.Error;
            if (suspendNeeds)
            {
                // Food
                if (patient.needs.food != null)
                    patient.needs.food.CurLevelPercentage = 1f;

                // Dubs Bad Hygiene thirst, bladder and hygiene
                if (ModCompatibility.DbhIsActive)
                {
                    DbhCompatibility.SetThirstNeedCurLevelPercentage(patient, 1f);
                    DbhCompatibility.SetBladderNeedCurLevelPercentage(patient, 1f);
                    DbhCompatibility.SetHygieneNeedCurLevelPercentage(patient, 1f);
                }
            }
        }
        else if (Status != SarcophagusStatus.Idle)
            Reset();

        // sarcophagus glow animation
        if (!this.IsHashIntervalTick(2) && !IsSarcophagusInUse())
            return;

        if (_wickSustainer == null || _wickSustainer.Ended)
            StartWickSustainer();
        else
            _wickSustainer.Maintain();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref _innerContainer, "_innerContainer", this);
        Scribe_Values.Look(ref _contentsKnown, "_contentsKnown", false);
        Scribe_Values.Look(ref OpenedSignal, "OpenedSignal");
        Scribe_Values.Look(ref AllowGuests, "AllowGuests", false);
        Scribe_Values.Look(ref AllowSlaves, "AllowSlaves", false);
        Scribe_Values.Look(ref AllowPrisoners, "AllowPrisoners", false);
        Scribe_Values.Look(ref Status, "Status");
        Scribe_Values.Look(ref DiagnosingTicks, "DiagnosingTicks", 0);
        Scribe_Values.Look(ref HealingTicks, "HealingTicks", 0);
        Scribe_Values.Look(ref ProgressHealingTicks, "ProgressHealingTicks", 0);
        Scribe_Values.Look(ref TotalHealingTicks, "TotalHealingTicks", 0);
        Scribe_Values.Look(ref _patientSavedFoodNeed, "_patientSavedFoodNeed", 0f);
        Scribe_Values.Look(ref _patientSavedDbhThirstNeed, "_patientSavedDbhThirstNeed", 0f);
        Scribe_Values.Look(ref _patientNeedsSnapshotTaken, "_patientNeedsSnapshotTaken", false);
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        if (!Faction.IsOfPlayerFaction()) yield break;

        string flickablePowerToggleStr = "CommandDesignateTogglePowerLabel".Translate();
        var patient = PatientPawn;

        foreach (Gizmo g in base.GetGizmos())
        {
            bool shouldDisable = (g is Command_Toggle act2
                    && act2.defaultLabel == flickablePowerToggleStr);

            if (shouldDisable && patient != null)
            {
                // Disable various gizmos while sarcophagi is in use
                g.Disable("RG_Sarcophagus_CommandGizmoDisabled_SarcophagusInUse".Translate(def.LabelCap));
            }

            yield return g;
        }

        // Allow guests gizmo
        yield return new Command_Toggle
        {
            defaultLabel = "RG_Sarcophagus_CommandGizmoAllowGuests_Label".Translate(),
            defaultDesc = "RG_Sarcophagus_CommandGizmoAllowGuests_Desc".Translate(LabelCap),
            isActive = () => AllowGuests,
            toggleAction = () =>
            {
                AllowGuests = !AllowGuests;
            },
            icon = RimgateTex.AllowGuestCommandTex,
            activateSound = SoundDefOf.Tick_Tiny
        };

        // Allow Slaves gizmo
        yield return new Command_Toggle
        {
            defaultLabel = "RG_Sarcophagus_CommandGizmoAllowSlaves_Label".Translate(),
            defaultDesc = "RG_Sarcophagus_CommandGizmoAllowSlaves_Desc".Translate(LabelCap),
            isActive = () => AllowSlaves,
            toggleAction = () =>
            {
                AllowSlaves = !AllowSlaves;

                if (!AllowSlaves)
                {
                    var assigned = Assignable?.AssignedPawns.ToList();
                    if (assigned?.Count <= 0) return;

                    foreach (var pawn in assigned)
                    {
                        if (pawn.IsSlave)
                            Assignable.ForceRemovePawn(pawn);
                    }
                }
            },
            icon = RimgateTex.AllowSlaveCommandTex,
            activateSound = SoundDefOf.Tick_Tiny
        };

        // Allow Prisoners gizmo
        yield return new Command_Toggle
        {
            defaultLabel = "RG_Sarcophagus_CommandGizmoAllowPrisoners_Label".Translate(),
            defaultDesc = "RG_Sarcophagus_CommandGizmoAllowPrisoners_Desc".Translate(LabelCap),
            isActive = () => AllowPrisoners,
            toggleAction = () =>
            {
                AllowPrisoners = !AllowPrisoners;

                if (!AllowPrisoners)
                {
                    var assigned = Assignable?.AssignedPawns.ToList();
                    if (assigned?.Count <= 0) return;

                    foreach (var pawn in assigned)
                    {
                        if (pawn.IsPrisoner)
                            Assignable.ForceRemovePawn(pawn);
                    }
                }
            },
            icon = RimgateTex.AllowPrisonerCommandTex,
            activateSound = SoundDefOf.Tick_Tiny
        };

        // Abort gizmo - kick out patient if treatment is aborted
        yield return new Command_Action
        {
            defaultLabel = "RG_Sarcophagus_CommandGizmoAbortTreatment_Label".Translate(),
            defaultDesc = "RG_Sarcophagus_CommandGizmoAbortTreatment_Desc".Translate(),
            Disabled = patient == null,
            action = () =>
            {
                if (patient != null)
                    Abort();
            },
            icon = RimgateTex.TreatmentCommandTex,
            activateSound = SoundDefOf.Click
        };

        Gizmo gizmo = Building.SelectContainedItemGizmo(this, patient);
        if (gizmo != null)
            yield return gizmo;

        if (RimgateMod.Debug && CanOpen)
        {
            yield return new Command_Action
            {
                defaultLabel = "DEV: Open",
                action = Open
            };
        }
    }

    public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn pawn)
    {
        if (!Faction.IsOfPlayerFaction()) yield break;

        if (!SarcophagusUtil.IsValidSarcophagusFor(this, pawn, pawn))
        {
            var reason = JobFailReason.HaveReason
                ? $" ({JobFailReason.Reason})"
                : string.Empty;
            yield return new FloatMenuOption("UseMedicalBed".Translate() + reason, null);
            yield break;
        }

        yield return new FloatMenuOption(
            "UseMedicalBed".Translate(),
            () =>
            {
                Job job = JobMaker.MakeJob(RimgateDefOf.Rimgate_PatientGoToSarcophagus, this);
                job.restUntilHealed = true;
                pawn.jobs.TryTakeOrderedJob(job);
            }
        );
    }

    public override string GetInspectString()
    {
        StringBuilder stringBuilder = new StringBuilder();

        // If minified, don't show computer and feedstock check Inspector messages
        if (ParentHolder != null && !(ParentHolder is Map))
            return string.Empty;

        if (Power.PowerOn)
        {
            var powerReqs = "PowerNeeded".Translate()
                + $": {(0f - Power.PowerOutput).ToString("#####0")} W";

            stringBuilder.AppendInNewLine(powerReqs);

            string inspectorStatus = string.Empty;
            switch (Status)
            {
                case SarcophagusStatus.DiagnosisStarted:
                    float diagnosingProgress = (int)((PatientBodySizeScaledMaxDiagnosingTicks - DiagnosingTicks)
                        / (float)PatientBodySizeScaledMaxDiagnosingTicks * 100);
                    inspectorStatus = "RG_Sarcophagus_InspectorStatus_DiagnosisProgress"
                        .Translate(diagnosingProgress);
                    break;
                case SarcophagusStatus.DiagnosisFinished:
                    inspectorStatus = "RG_Sarcophagus_InspectorStatus_DiagnosisComplete"
                        .Translate();
                    break;
                case SarcophagusStatus.HealingStarted:
                case SarcophagusStatus.HealingFinished:
                    int healingProgress = TotalHealingTicks > 0
                        ? (int)(ProgressHealingTicks / (float)TotalHealingTicks * 100)
                        : 100;
                    inspectorStatus = "RG_Sarcophagus_InspectorStatus_HealingProgress"
                        .Translate(healingProgress);
                    break;
                case SarcophagusStatus.PatientDischarged:
                    inspectorStatus = "RG_Sarcophagus_InspectorStatus_PatientDischarged"
                        .Translate();
                    break;
                case SarcophagusStatus.Idle:
                default:
                    inspectorStatus = "RG_Sarcophagus_InspectorStatus_Idle"
                        .Translate();
                    break;
            }

            if (!inspectorStatus.NullOrEmpty())
                stringBuilder.AppendInNewLine(inspectorStatus);
        }
        else
            stringBuilder.AppendInNewLine("RG_Sarcophagus_InspectorStatus_NoPower"
                .Translate());

        if (Analyzable != null)
            stringBuilder.AppendInNewLine(Analyzable.CompInspectStringExtra());

        return stringBuilder.ToString();
    }

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        base.DrawAt(drawLoc, flip);

        if (!IsSarcophagusInUse()) return;

        Vector3 sarchophagusGlowDrawPos = DrawPos;

        float drawAltitude = AltitudeLayer.Pawn.AltitudeFor();

        sarchophagusGlowDrawPos.y = drawAltitude + 0.06f;

        Graphics.DrawMesh(
            GlowMesh,
            sarchophagusGlowDrawPos,
            Quaternion.identity,
            GlowMaterial,
            0);
    }

    public ThingOwner GetDirectlyHeldThings() => _innerContainer;

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    public bool Accepts(Thing thing)
    {
        if (thing is not Pawn p
            || !p.RaceProps.Humanlike
            || p.IsMutant && !p.mutant.Def.entitledToMedicalCare)
        {
            return false;
        }

        return _innerContainer.CanAcceptAnyOf(thing, false);
    }

    public bool TryAcceptPawn(Thing pawn, bool allowSpecialEffects = true)
    {
        if (!Accepts(pawn))
            return false;

        bool success;
        if (pawn.holdingOwner != null)
        {
            pawn.holdingOwner.TryTransferToContainer(pawn, _innerContainer, pawn.stackCount);
            success = true;
        }
        else
            success = _innerContainer.TryAdd(pawn);

        if (success)
        {
            if (pawn.Faction?.IsPlayer == true)
                _contentsKnown = true;

            SoundDefOf.CryptosleepCasket_Accept.PlayOneShot(new TargetInfo(base.Position, base.Map));

            return true;
        }

        return false;
    }

    private void SwitchState()
    {
        SarcophagusStatus oldStatus = Status;
        switch (Status)
        {
            case SarcophagusStatus.Idle:
                Status = SarcophagusStatus.DiagnosisStarted;
                break;

            case SarcophagusStatus.DiagnosisStarted:
                Status = SarcophagusStatus.DiagnosisFinished;
                break;

            case SarcophagusStatus.DiagnosisFinished:
                Status = SarcophagusStatus.HealingStarted;
                break;

            case SarcophagusStatus.HealingStarted:
                Status = SarcophagusStatus.HealingFinished;
                break;

            case SarcophagusStatus.HealingFinished:
                Status = SarcophagusStatus.PatientDischarged;
                break;

            case SarcophagusStatus.PatientDischarged:
                Reset();
                break;

            default:
                Status = SarcophagusStatus.Error;
                break;
        }

        if (RimgateMod.Debug)
            Log.Message(this + $" :: state change from {oldStatus.ToStringSafe().Colorize(Color.yellow)}"
                + $" to {Status.ToStringSafe().Colorize(Color.yellow)}");
    }

    public bool IsSarcophagusInUse()
    {
        bool result;
        switch (Status)
        {
            case SarcophagusStatus.DiagnosisStarted:
            case SarcophagusStatus.DiagnosisFinished:
            case SarcophagusStatus.HealingStarted:
            case SarcophagusStatus.HealingFinished:
                result = true;
                break;
            default:
                result = false;
                break;
        }
        return result;
    }

    private void DiagnosePatient(Pawn patient)
    {
        // Reset aggregate healing metrics for this diagnosis pass
        HealingTicks = 0;
        TotalHealingTicks = 0;
        ProgressHealingTicks = 0;

        // List all of the patient's hediffs/injuries, sorted by body part hierarchy then severity
        // Hediffs with no body part defined (i.e. "Whole Body" hediffs) are moved to the bottom of the list)
        _patientTreatableHediffs = patient.health.hediffSet.hediffs
            .OrderBy((Hediff x) => x.Part == null ? 9999 : x.Part.Index)
            .ThenByDescending((Hediff x) => x.Severity)
            .ToList();

        // Ignore missing child parts of limbs and other body parts that have been replaced with
        // implants or prosthetics
        // This is a multi-step process:
        // - Find the hediffs (and the associated body parts) corresponding to implants/prosthetics
        // - Identify the child parts affected by the implants/prosthetics
        // - Remove the hediffs from the treatment list by body part

        List<Hediff> artificialPartHediffs = _patientTreatableHediffs.FindAll((Hediff x) =>
            x.def.hediffClass.Equals(typeof(Hediff_AddedPart)));

        List<BodyPartRecord> childPartsToSkip = new List<BodyPartRecord>();

        foreach (Hediff currentArtificialPartHediff in artificialPartHediffs)
            childPartsToSkip.AddRange(GetBodyPartDescendants(currentArtificialPartHediff.Part));

        // Ignore all missing part Hediffs from body parts that have been replaced
        _patientTreatableHediffs.RemoveAll(x =>
            childPartsToSkip.Any(p => x.Part == p)
            && x.def.hediffClass == typeof(Hediff_MissingPart));

        // If the advanced sarcophagus bioregeneration research is NOT complete,
        // do not treat generic MissingBodyPart at all.
        if (!ResearchUtil.SarcophagusBioregenerationComplete)
        {
            int removedMissing = _patientTreatableHediffs.RemoveAll(h => h.def == HediffDefOf.MissingBodyPart);

            if (RimgateMod.Debug && removedMissing > 0)
                Log.Message($"{this} :: Missing body parts removed from treatment list (bioregeneration research not completed).");
        }

        // Ignore hediffs/injuries that are:
        // - Not explicitly whitelisted as always treatable
        // - Blacklisted as never treatable
        // - Not explicitly greylisted as non-critical but treatable
        // - Not bad (i.e isBad = false) and not treatable
        _patientTreatableHediffs.RemoveAll((Hediff x) =>
            !AlwaysTreatableHediffs.Contains(x.def)
            && (NeverTreatableHediffs.Contains(x.def)
                || (!NonCriticalTreatableHediffs.Contains(x.def)
                    && !x.def.isBad
                    && !x.TendableNow())));

        // Immediately treat blood loss hediff
        int bloodLossCount = _patientTreatableHediffs.RemoveAll(x => x.def == HediffDefOf.BloodLoss);
        if (bloodLossCount > 0)
            patient.health.hediffSet.hediffs.RemoveAll(x => x.def == HediffDefOf.BloodLoss);

        // Calculate individual and total cumulative treatment time for each hediff/injury
        foreach (Hediff currentHediff in _patientTreatableHediffs)
        {
            float currentSeverity = currentHediff.Severity;

            // currentHediff.Part will throw an error if a hediff
            // is applied to the whole body (e.g. malnutrition), as part == null
            float currentBodyPartMaxHealth = currentHediff.Part != null
                ? EbfCompatibilityWrapper.GetMaxHealth(
                    currentHediff.Part.def,
                    patient,
                    currentHediff.Part)
                : 1;

            float currentNormalizedSeverity = currentSeverity < 1
                ? currentSeverity
                : currentSeverity / currentBodyPartMaxHealth;

            TotalHealingTicks += ScaleHealingTicks(patient, currentHediff);

            // Tend all bleeding hediffs immediately so the pawn doesn't bleed out while on sarcophagus
            // The Hediff will be completely removed once the sarcophagus is done with the Healing process
            if (currentHediff.Bleeding)
                currentHediff.Tended(1, 1);
        }

        // Identify treatable traits for removal
        _patientTraitsToRemove = patient.story?.traits.allTraits.FindAll(x =>
            AlwaysTreatableTraits.Contains(x.def));
    }

    // Scale healing time for the first hediff
    // according to its (normalized) severity and
    // patient body size
    // i.e. More severe hediffs take longer,
    // bigger pawns also take longer
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ScaleHealingTicks(Pawn patient, Hediff hediff = null) =>
        Mathf.CeilToInt(GetHediffNormalizedSeverity(patient, hediff) * PatientBodySizeScaledMaxHealingTicks);

    private float GetHediffNormalizedSeverity(Pawn patient, Hediff hediff = null)
    {
        Hediff currentHediff = hediff == null
            ? _patientTreatableHediffs.First()
            : hediff;

        float currentHediffSeverity = currentHediff?.Severity ?? 0;

        float currentHediffBodyPartMaxHealth = currentHediff.Part != null
            ? EbfCompatibilityWrapper.GetMaxHealth(
                currentHediff.Part.def,
                patient,
                currentHediff.Part)
            : 1;

        float currentHediffNormalizedSeverity = currentHediffSeverity < 1
            ? currentHediffSeverity
            : currentHediffSeverity / currentHediffBodyPartMaxHealth;

        return Math.Abs(currentHediffNormalizedSeverity);
    }

    private List<BodyPartRecord> GetBodyPartDescendants(BodyPartRecord part)
    {
        List<BodyPartRecord> childParts = new List<BodyPartRecord>();

        if (part.parts.Count > 0)
        {
            foreach (BodyPartRecord currentChildPart in part.parts)
            {
                childParts.Add(currentChildPart);
                childParts.AddRange(GetBodyPartDescendants(currentChildPart));
            }
        }

        return childParts;
    }

    private void DischargePatient(Pawn patient, bool finishTreatmentNormally = true)
    {
        // Restore previously saved patient food need level
        if (patient.needs.food != null)
            patient.needs.food.CurLevelPercentage = _patientSavedFoodNeed;

        // Restore previously saved patient DBH thirst and reset DBH bladder/hygiene need levels
        if (ModCompatibility.DbhIsActive)
        {
            DbhCompatibility.SetThirstNeedCurLevelPercentage(patient, _patientSavedDbhThirstNeed);
            DbhCompatibility.SetBladderNeedCurLevelPercentage(patient, 1f);
            DbhCompatibility.SetHygieneNeedCurLevelPercentage(patient, 1f);
        }

        if (finishTreatmentNormally)
        {
            // Clear any ongoing mental states (e.g. Manhunter)
            if (patient.InMentalState)
                patient.MentalState.RecoverFromState();

            // Remove treatable traits only if treatment was completed normally
            if (!_patientTraitsToRemove.NullOrEmpty())
            {
                patient.story?.traits.allTraits
                    .RemoveAll(x =>
                        _patientTraitsToRemove.Contains(x));
                string letterLabel = "RG_Sarcophagus_Letter_TraitRemoved_Label".Translate();
                string letterText = "RG_Sarcophagus_Letter_TraitRemoved_Desc"
                    .Translate(patient.Named("PAWN"))
                    + string.Join("", (from t in _patientTraitsToRemove
                                       select "\n- "
                                       + t.def.degreeDatas.FirstOrDefault().LabelCap).ToArray());
                Find.LetterStack.ReceiveLetter(
                    letterLabel,
                    letterText,
                    LetterDefOf.PositiveEvent,
                    new TargetInfo(patient));
            }

            // De-aging
            float ageRemoval = 900000f; //3600000f - 1 year
            int num = (int)(ageRemoval * patient.ageTracker.AdultAgingMultiplier);
            long val = (long)(3600000f * patient.ageTracker.AdultMinAge); // minimum biological age in ticks
            patient.ageTracker.AgeBiologicalTicks = Math.Max(val, patient.ageTracker.AgeBiologicalTicks - num);

            if (ModsConfig.BiotechActive)
            {
                patient.ageTracker.ResetAgeReversalDemand(Pawn_AgeTracker.AgeReversalReason.ViaTreatment);
                patient.TryGiveThought(ThoughtDefOf.AgeReversalReceived);

                // If the patient is a Sanguophage or Wraith, top up their gene resource

                Gene_Hemogen gene_Hemogen = patient.genes?.GetFirstGeneOfType<Gene_Hemogen>();
                if (gene_Hemogen != null)
                    gene_Hemogen.Value = gene_Hemogen.InitialResourceMax;

                Gene_WraithEssenceMetabolism gene_Essence = patient.genes?.GetFirstGeneOfType<Gene_WraithEssenceMetabolism>();
                if (gene_Essence != null)
                    gene_Essence.Value = gene_Essence.InitialResourceMax;
            }
        }

        // Refresh pawn renderer,
        // especially important for Anomaly DLC not updating visuals from removed hediffs
        patient.Drawer.renderer.SetAllGraphicsDirty();

        // Refresh pawn disabled work tags,
        // especially important for Anomaly DLC not updating work tags from removed hediffs
        patient.Notify_DisabledWorkTypesChanged();

        // Clear pawn hediff cache and try to get them out of the sarcophagus
        patient.health.hediffSet.DirtyCache();
        patient.health.CheckForStateChange(null, null);

        // Apply the appropriate cortical stimulation hediff,
        // depending on whether the treatment was completed or interrupted            
        string popupMessage = finishTreatmentNormally
            ? "RG_Sarcophagus_Message_TreatmentComplete".Translate(patient.LabelCap, patient)
            : "RG_Sarcophagus_Message_TreatmentInterrupted".Translate(patient.LabelCap, patient);
        MessageTypeDef popupMessageType = finishTreatmentNormally
            ? MessageTypeDefOf.PositiveEvent
            : MessageTypeDefOf.NegativeHealthEvent;

        Messages.Message(popupMessage, patient, popupMessageType, true);

        if (finishTreatmentNormally)
            RimgateEvents.Notify_ColonyOfPawnEvent(patient, RimgateDefOf.Rimgate_UsedSarcophagus);

        if (RimgateMod.Debug)
        {
            string message = finishTreatmentNormally
                ? "NORMAL".Colorize(Color.green)
                : "ABORTED".Colorize(Color.red);
            Log.Message(this + $" :: Discharged patient {patient} ({message})");
        }
    }

    public void StartWickSustainer()
    {
        SoundInfo info = SoundInfo.InMap(this, MaintenanceType.PerTick);
        _wickSustainer = def.building.soundDispense.TrySpawnSustainer(info);
    }

    // only apply addiction if the patient received full treatment
    public void HandleAfterEffects(Pawn patient, bool isPostTreatment = true)
    {
        if (!Props.applyAddictionHediff || !patient.RaceProps.IsFlesh)
            return;

        Hediff_Addiction hediff_Addiction = AddictionUtility.FindAddictionHediff(patient, RimgateDefOf.Rimgate_SarcophagusChemical);

        if (hediff_Addiction != null)
            hediff_Addiction.Severity += Props.existingAddictionSeverityOffset;
        else if (isPostTreatment)
            ApplyAddiction(patient);

        AdjustChemicalNeed(patient);
        if (isPostTreatment)
            ApplyHigh(patient);

        patient.drugs?.Notify_DrugIngested(this);
    }

    public void ApplyAddiction(Pawn patient)
    {
        if (Rand.Value >= Props.addictiveness) return;

        patient.health.AddHediff(RimgateDefOf.Rimgate_SarcophagusAddiction);
        if (PawnUtility.ShouldSendNotificationAbout(patient))
        {
            Find.LetterStack.ReceiveLetter(
                "LetterLabelNewlyAddicted"
                    .Translate(RimgateDefOf.Rimgate_SarcophagusChemical.label)
                    .CapitalizeFirst(),
                "LetterNewlyAddicted"
                    .Translate(
                        patient.LabelShort,
                        RimgateDefOf.Rimgate_SarcophagusChemical.label,
                        patient.Named("PAWN"))
                            .AdjustedFor(patient)
                            .CapitalizeFirst(),
                LetterDefOf.NegativeEvent,
                patient);
        }

        AddictionUtility.CheckDrugAddictionTeachOpportunity(patient);
    }

    public void AdjustChemicalNeed(Pawn patient)
    {
        if (!patient.needs.TryGetNeed(RimgateDefOf.Rimgate_SarcophagusChemicalNeed, out var need))
            return;

        float effect = Props.needLevelOffset;
        AddictionUtility.ModifyChemicalEffectForToleranceAndBodySize(
            patient,
            RimgateDefOf.Rimgate_SarcophagusChemical,
            ref effect,
            applyGeneToleranceFactor: false);
        need.CurLevel += effect;
    }

    public void ApplyHigh(Pawn patient)
    {
        if (patient.health.hediffSet.HasHediff(RimgateDefOf.Rimgate_SarcophagusHigh))
            return;

        Hediff hediff = HediffMaker.MakeHediff(RimgateDefOf.Rimgate_SarcophagusHigh, patient);
        float effect = Props.severity <= 0f
            ? RimgateDefOf.Rimgate_SarcophagusHigh.initialSeverity
            : Props.severity;
        // body-size scaling
        effect /= patient.BodySize;
        hediff.Severity = effect;
        patient.health.AddHediff(hediff);
    }

    public override AcceptanceReport ClaimableBy(Faction fac)
    {
        if (!_contentsKnown && _innerContainer.Any)
            return false;

        return base.ClaimableBy(fac);
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
    {
        foreach (StatDrawEntry state in base.SpecialDisplayStats())
            yield return state;

        // Compute current effective multiplier:
        float multiplier = ResearchUtil.SarcophagusOptimizationComplete
            ? Props.powerConsumptionReductionFactor
            : 1f;

        float diag = Props.diagnosisModePowerConsumption * multiplier;
        float heal = Props.healingModePowerConsumption * multiplier;

        yield return new StatDrawEntry(
            StatCategoryDefOf.Building,
            "RG_Sarcophagus_Stat_PowerConsumptionDiagnosis_Label".Translate(),
            diag.ToString("F0") + " W",
            "RG_Sarcophagus_Stat_PowerConsumptionDiagnosis_Desc".Translate(),
            4994);

        yield return new StatDrawEntry(
            StatCategoryDefOf.Building,
            "RG_Sarcophagus_Stat_PowerConsumptionHealing_Label".Translate(),
            heal.ToString("F0") + " W",
            "RG_Sarcophagus_Stat_PowerConsumptionHealing_Desc".Translate(),
            4993);

        yield return new StatDrawEntry(
            StatCategoryDefOf.Building,
            "RG_Sarcophagus_Stat_DiagnosisTime_Label".Translate(),
            "RG_Sarcophagus_Stat_TimeSeconds".Translate(Props.maxDiagnosisTime),
            "RG_Sarcophagus_Stat_DiagnosisTime_Desc".Translate(),
            4992);

        yield return new StatDrawEntry(
            StatCategoryDefOf.Building,
            "RG_Sarcophagus_Stat_PerHediffHealingTime_Label".Translate(),
            "RG_Sarcophagus_Stat_TimeSeconds".Translate(Props.maxPerHediffHealingTime),
            "RG_Sarcophagus_Stat_PerHediffHealingTime_Desc".Translate(),
            4991);
    }

    public void Open()
    {
        if (!HasAnyContents)
            return;

        Abort();

        if (!OpenedSignal.NullOrEmpty())
            Find.SignalManager.SendSignal(new Signal(OpenedSignal, this.Named("SUBJECT")));

        DirtyMapMesh(Map);
    }

    private void Abort()
    {
        DischargePatient(PatientPawn, false);
        EjectContents();
        Reset();
    }

    public void EjectContents()
    {
        _innerContainer.TryDropAll(
            InteractionCell,
            Map,
            ThingPlaceMode.Near);
        _contentsKnown = true;
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        _cachedGlowMaterial = null;
        _cachedGlowMesh = null;

        if (Power.PowerOn
            && (Status == SarcophagusStatus.DiagnosisFinished
                || Status == SarcophagusStatus.HealingStarted
                || Status == SarcophagusStatus.HealingFinished))
        {
            DischargePatient(PatientPawn, false);
        }

        EjectContents();

        AllowGuests = false;

        District district = this.GetDistrict();
        base.DeSpawn(mode);
        if (district != null)
        {
            district.Notify_RoomShapeOrContainedBedsChanged();
            district.Room.Notify_RoomShapeChanged();
        }
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        var map = Map;

        if (mode == DestroyMode.Deconstruct || mode == DestroyMode.KillFinalize)
        {
            if (mode != DestroyMode.Deconstruct)
            {
                foreach (Pawn p in _innerContainer)
                    Verse.HealthUtility.DamageUntilDowned(p);
            }
        }

        base.Destroy(mode);

        _innerContainer.ClearAndDestroyContents();
    }

    public void Reset()
    {
        Status = SarcophagusStatus.Idle;
        if (!_patientTreatableHediffs.NullOrEmpty())
            _patientTreatableHediffs.Clear();

        if (!_patientTraitsToRemove.NullOrEmpty())
            _patientTraitsToRemove.Clear();

        DiagnosingTicks = 0;
        HealingTicks = 0;
        ProgressHealingTicks = 0;
        TotalHealingTicks = 0;

        _patientSavedFoodNeed = 0;
        _patientSavedDbhThirstNeed = 0;
        _patientNeedsSnapshotTaken = false;
    }
}