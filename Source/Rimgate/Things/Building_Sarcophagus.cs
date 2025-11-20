using System.Collections.Generic;
using System.Text;
using Verse;
using RimWorld;
using UnityEngine;
using System.Linq;
using System;
using Verse.Sound;
using Verse.AI;
using Rimgate;

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

public class Building_Sarcophagus : Building, IThingHolder, IOpenable, ISearchableContents
{
    public CompPowerTrader Power => _powerTrader ??= GetComp<CompPowerTrader>();

    public Comp_SarcophagusControl Sarcophagus => _sarcophagus ??= GetComp<Comp_SarcophagusControl>();

    public Comp_TreatmentRestrictions Restrictions => _treatmentRestrictions ??= GetComp<Comp_TreatmentRestrictions>();

    public CompAssignableToPawn_Sarcophagus Assignable => _assignable ??= GetComp<CompAssignableToPawn_Sarcophagus>();

    public Comp_AnalyzableResearchWhen Analyzable => _analyzable ??= GetComp<Comp_AnalyzableResearchWhen>();

    public int DiagnosingTicks = 0;

    public int MaxDiagnosingTicks;

    public int PatientBodySizeScaledMaxDiagnosingTicks;

    public int HealingTicks = 0;

    public int MaxHealingTicks;

    public int PatientBodySizeScaledMaxHealingTicks;

    public float DiagnosingPowerConsumption;

    public float HealingPowerConsumption;

    public List<HediffDef> AlwaysTreatableHediffs;

    public List<HediffDef> NeverTreatableHediffs;

    public List<HediffDef> NonCriticalTreatableHediffs;

    public List<HediffDef> UsageBlockingHediffs;

    public List<TraitDef> UsageBlockingTraits;

    public List<TraitDef> AlwaysTreatableTraits;

    public List<string> DisallowedRaces;

    [MayRequireBiotech]
    public List<XenotypeDef> DisallowedXenotypes;

    public int ProgressHealingTicks = 0;

    public int TotalHealingTicks = 0;

    public bool AllowGuests = false;

    public bool AllowSlaves = false;

    public bool AllowPrisoners = false;

    public SarcophagusStatus Status = SarcophagusStatus.Idle;

    public string OpenedSignal;

    private Sustainer _wickSustainer;

    protected ThingOwner _innerContainer;

    protected bool _contentsKnown;

    public int OpenTicks => 250;

    public const float MaxBodySize = 9999f;

    public Pawn PatientPawn => _innerContainer.Count > 0
        ? (Pawn)_innerContainer[0]
        : null;

    public bool CanOpen => HasAnyContents;

    public bool HasAnyContents => _innerContainer.Count > 0;

    public ThingOwner SearchableContents => _innerContainer;

    private List<Hediff> _patientTreatableHediffs = new();

    private static float _patientSavedFoodNeed;

    private static float _patientSavedDbhThirstNeed;

    private static List<Trait> _patientTraitsToRemove = new();

    private float _totalNormalizedSeverities = 0;

    private static string[] gizmosToDisableWhileInUse;

    private CompPowerTrader _powerTrader;

    private Comp_SarcophagusControl _sarcophagus;

    private Comp_TreatmentRestrictions _treatmentRestrictions;

    public CompAssignableToPawn_Sarcophagus _assignable;

    public Comp_AnalyzableResearchWhen _analyzable;

    public Building_Sarcophagus()
    {
        _innerContainer = new ThingOwner<Thing>(this, oneStackOnly: true);
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);

        MaxDiagnosingTicks = GenTicks.SecondsToTicks(Sarcophagus.Props.maxDiagnosisTime);
        MaxHealingTicks = GenTicks.SecondsToTicks(Sarcophagus.Props.maxPerHediffHealingTime);
        DiagnosingPowerConsumption = Sarcophagus.Props.diagnosisModePowerConsumption;
        HealingPowerConsumption = Sarcophagus.Props.healingModePowerConsumption;

        AlwaysTreatableHediffs = Restrictions.Props.alwaysTreatableHediffs;
        NeverTreatableHediffs = Restrictions.Props.neverTreatableHediffs;
        NonCriticalTreatableHediffs = Restrictions.Props.nonCriticalTreatableHediffs;
        UsageBlockingHediffs = Restrictions.Props.usageBlockingHediffs;
        UsageBlockingTraits = Restrictions.Props.usageBlockingTraits;
        AlwaysTreatableTraits = Restrictions.Props.alwaysTreatableTraits;
        DisallowedRaces = Restrictions.Props.disallowedRaces;
        DisallowedXenotypes = Restrictions.Props.disallowedXenotypes;

        if (Faction?.IsPlayer == true)
            _contentsKnown = true;
    }

    protected override void Tick()
    {
        base.Tick();

        // State-dependent power consumption
        if (Status == SarcophagusStatus.DiagnosisStarted
            || Status == SarcophagusStatus.DiagnosisFinished)
        {
            Power.PowerOutput = -DiagnosingPowerConsumption;
        }
        else if (Status == SarcophagusStatus.HealingStarted
            || Status == SarcophagusStatus.HealingFinished)
        {
            Power.PowerOutput = -HealingPowerConsumption;
        }
        else
            Power.PowerOutput = -Power.Props.PowerConsumption;

        // Main patient treatment cycle logic
        if (PatientPawn != null)
        {
            PatientBodySizeScaledMaxDiagnosingTicks = (int)(MaxDiagnosingTicks * PatientPawn.BodySize);
            PatientBodySizeScaledMaxHealingTicks = (int)(MaxHealingTicks * PatientPawn.BodySize);

            // Interrupt treatment on power loss
            if (!Power.PowerOn)
            {
                if (RimgateMod.Debug)
                    Log.Message(this + $" :: Lost power while running (state: {Status})");

                DischargePatient(PatientPawn, false);
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
                        // Save initial patient food need level
                        if (PatientPawn.needs.food != null)
                            _patientSavedFoodNeed = PatientPawn.needs.food.CurLevelPercentage;

                        // Save initial patient DBH thirst and reset DBH bladder/hygiene need levels
                        if (ModCompatibility.DbhIsActive)
                        {
                            _patientSavedDbhThirstNeed = DbhCompatibility.GetThirstNeedCurLevelPercentage(PatientPawn);
                            DbhCompatibility.SetBladderNeedCurLevelPercentage(PatientPawn, 1f);
                            DbhCompatibility.SetHygieneNeedCurLevelPercentage(PatientPawn, 1f);
                        }

                        if (RimgateMod.Debug)
                            Log.Message($"\t{PatientPawn} :: initial DiagnosingTicks = {DiagnosingTicks}");

                        SwitchState();
                        break;
                    }
                case SarcophagusStatus.DiagnosisStarted:
                    {
                        DiagnosingTicks--;
                        if (DiagnosingTicks == 0)
                            SwitchState();
                        break;
                    }
                case SarcophagusStatus.DiagnosisFinished:
                    {
                        DiagnosePatient(PatientPawn);
                        // Skip treatment if no treatable hediffs are found
                        if (_patientTreatableHediffs.NullOrEmpty())
                        {
                            if (RimgateMod.Debug)
                                Log.Message(this + $" :: Ejecting patient as they have nothing to treat");
                            // if we're only using the sarcophagus for an addiction, adjust need
                            Sarcophagus.HandleAfterEffects(PatientPawn, false);
                            EjectContents();
                            Status = SarcophagusStatus.PatientDischarged;
                        }
                        else
                        {
                            // Scale healing time for the first hediff
                            // according to its (normalized) severity and
                            // patient body size
                            // i.e. More severe hediffs take longer,
                            // bigger pawns also take longer
                            HealingTicks = (int)Math.Ceiling(GetHediffNormalizedSeverity()
                                * PatientBodySizeScaledMaxHealingTicks);
                            if (RimgateMod.Debug)
                            {
                                Log.Message($"\t{PatientPawn} :: first hediff HealingTicks = {HealingTicks}"
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
                        if (HealingTicks == 0)
                            SwitchState();
                        break;
                    }
                case SarcophagusStatus.HealingFinished:
                    {
                        // Don't remove 'good' treatable Hediffs
                        // but instead treat them with 100% quality
                        // (unless the 'good' Hediff is whitelisted as always treatable)
                        bool hasTendable = !_patientTreatableHediffs.First().def.isBad
                            && !AlwaysTreatableHediffs.Contains(_patientTreatableHediffs.First().def)
                            && !NonCriticalTreatableHediffs.Contains(_patientTreatableHediffs.First().def);
                        if (hasTendable)
                            _patientTreatableHediffs.First().Tended(1, 1);
                        else
                            PatientPawn.health.hediffSet.hediffs.Remove(_patientTreatableHediffs.First());

                        _patientTreatableHediffs.RemoveAt(0);
                        if (!_patientTreatableHediffs.NullOrEmpty())
                        {
                            // Scale healing time for the next hediff according
                            // to its (normalized) severity and patient body size
                            // i.e. More severe hediffs take longer, bigger pawns also take longer
                            HealingTicks = (int)Math.Ceiling(GetHediffNormalizedSeverity()
                                * PatientBodySizeScaledMaxHealingTicks);

                            if (RimgateMod.Debug)
                            {
                                Log.Message($"\t{PatientPawn} :: next hediff HealingTicks = {HealingTicks}"
                                    + $" (hediff count: {_patientTreatableHediffs.Count()})");
                            }

                            // Jump back to the previous state to start healing the next hediff
                            Status = SarcophagusStatus.HealingStarted;
                        }
                        else
                        {
                            DischargePatient(PatientPawn);
                            // apply addiction and adjust needs
                            Sarcophagus.HandleAfterEffects(PatientPawn);
                            EjectContents();
                            SwitchState();
                        }
                        break;
                    }
                case SarcophagusStatus.PatientDischarged:
                    {
                        SwitchState();
                        Reset();
                        break;
                    }
            }

            // Suspend patient needs during diagnosis and treatment
            bool suspendNeeds = Status == SarcophagusStatus.DiagnosisStarted
                || Status == SarcophagusStatus.DiagnosisFinished
                || Status == SarcophagusStatus.HealingStarted
                || Status == SarcophagusStatus.HealingFinished;
            if (suspendNeeds)
            {
                // Food
                if (PatientPawn.needs.food != null)
                    PatientPawn.needs.food.CurLevelPercentage = 1f;

                // Dubs Bad Hygiene thirst, bladder and hygiene
                if (ModCompatibility.DbhIsActive)
                {
                    DbhCompatibility.SetThirstNeedCurLevelPercentage(PatientPawn, 1f);
                    DbhCompatibility.SetBladderNeedCurLevelPercentage(PatientPawn, 1f);
                    DbhCompatibility.SetHygieneNeedCurLevelPercentage(PatientPawn, 1f);
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

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
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

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref _innerContainer, "_innerContainer", this);
        Scribe_Values.Look(ref _contentsKnown, "_contentsKnown", false);
        Scribe_Values.Look(ref OpenedSignal, "OpenedSignal");
        Scribe_Values.Look(ref AllowGuests, "AllowGuests", false);
        Scribe_Values.Look(ref AllowSlaves, "AllowSlaves", false);
        Scribe_Values.Look(ref AllowPrisoners, "AllowPrisoners", false);
        BackCompatibility.PostExposeData(this);
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        if (Faction != Faction.OfPlayer) yield break;

        string flickablePowerToggleStr = "CommandDesignateTogglePowerLabel".Translate();

        foreach (Gizmo g in base.GetGizmos())
        {
            bool shouldDisable = (g is Command_Toggle act2
                    && act2.defaultLabel == flickablePowerToggleStr);

            if (shouldDisable && PatientPawn != null)
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

                    foreach(var pawn in assigned)
                    {
                        if(pawn.IsSlave)
                            Assignable.TryUnassignPawn(pawn);
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
                            Assignable.TryUnassignPawn(pawn);
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
            Disabled = PatientPawn == null,
            action = () =>
            {
                if (PatientPawn != null)
                    Abort();
            },
            icon = RimgateTex.TreatmentCommandTex,
            activateSound = SoundDefOf.Click
        };

        Gizmo gizmo = Building.SelectContainedItemGizmo(this, PatientPawn);
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
        if (Faction != Faction.OfPlayer) yield break;

        if (!SarcophagusUtility.IsValidSarcophagusFor(this, pawn, pawn))
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

        // TODO: assignable comp here

        if (Power.PowerOn)
        {
            var powerReqs = "PowerNeeded".Translate()
                + $": {(0f - Power.PowerOutput).ToString("#####0")} W";

            stringBuilder.AppendInNewLine(powerReqs);

            var diagReq = Sarcophagus?.Props?.diagnosisModePowerConsumption ?? 0;
            var healReq = Sarcophagus?.Props?.healingModePowerConsumption ?? 0;
            var useReqs = $" ({diagReq.ToString("#####0")} W for diagnosis"
                + $"  / {healReq.ToString("#####0")} W for healing)";

            stringBuilder.AppendInNewLine(useReqs);

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
                    int healingProgress = (int)(ProgressHealingTicks / (float)TotalHealingTicks * 100);
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

    public ThingOwner GetDirectlyHeldThings() => _innerContainer;

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    public bool Accepts(Thing pawn)
    {
        return _innerContainer.CanAcceptAnyOf(pawn, false);
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
                Status = SarcophagusStatus.Idle;
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

    private void DiagnosePatient(Pawn patientPawn)
    {
        // List all of the patient's hediffs/injuries, sorted by body part hierarchy then severity
        // Hediffs with no body part defined (i.e. "Whole Body" hediffs) are moved to the bottom of the list)
        _patientTreatableHediffs = patientPawn.health.hediffSet.hediffs
            .OrderBy((Hediff x) =>
                x.Part == null
                ? 9999
                : x.Part.Index)
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

        // Only ignore Missing part Hediffs from body parts that have been replaced
        _patientTreatableHediffs.RemoveAll(x =>
            childPartsToSkip.Any(p => x.Part == p)
            && x.def.hediffClass == typeof(Hediff_MissingPart));

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
        _patientTreatableHediffs.RemoveAll(x => x.def == HediffDefOf.BloodLoss);
        PatientPawn.health.hediffSet.hediffs.RemoveAll(x => x.def == HediffDefOf.BloodLoss);

        // Calculate individual and total cumulative treatment time for each hediff/injury
        foreach (Hediff currentHediff in _patientTreatableHediffs)
        {
            float currentSeverity = currentHediff.Severity;

            // currentHediff.Part will throw an error if a hediff
            // is applied to the whole body (e.g. malnutrition), as part == null
            float currentBodyPartMaxHealth = currentHediff.Part != null
                ? EbfCompatibilityWrapper.GetMaxHealth(
                    currentHediff.Part.def,
                    patientPawn,
                    currentHediff.Part)
                : 1;

            float currentNormalizedSeverity = currentSeverity < 1
                ? currentSeverity
                : currentSeverity / currentBodyPartMaxHealth;

            _totalNormalizedSeverities += currentNormalizedSeverity;

            TotalHealingTicks += (int)Math.Ceiling(GetHediffNormalizedSeverity(currentHediff)
                * PatientBodySizeScaledMaxHealingTicks);

            // Tend all bleeding hediffs immediately so the pawn doesn't bleed out while on sarcophagus
            // The Hediff will be completely removed once the sarcophagus is done with the Healing process
            if (currentHediff.Bleeding)
                currentHediff.Tended(1, 1);
        }

        // Identify treatable traits for removal
        _patientTraitsToRemove = patientPawn.story?.traits.allTraits.FindAll(x =>
            AlwaysTreatableTraits.Contains(x.def));
    }

    private float GetHediffNormalizedSeverity(Hediff specificHediff = null)
    {
        Hediff currentHediff = specificHediff == null
            ? _patientTreatableHediffs.First()
            : specificHediff;

        float currentHediffSeverity = currentHediff.Severity;

        float currentHediffBodyPartMaxHealth = currentHediff.Part != null
            ? EbfCompatibilityWrapper.GetMaxHealth(
                currentHediff.Part.def,
                PatientPawn,
                currentHediff.Part)
            : 1;

        float currentHediffNormalizedSeverity = currentHediffSeverity < 1
            ? currentHediffSeverity
            : currentHediffSeverity / currentHediffBodyPartMaxHealth;

        return currentHediffNormalizedSeverity;
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

    public void DischargePatient(Pawn patientPawn, bool finishTreatmentNormally = true)
    {
        // Clear any ongoing mental states (e.g. Manhunter)
        if (patientPawn.InMentalState)
            patientPawn.MentalState.RecoverFromState();

        // Apply the appropriate cortical stimulation hediff, depending on whether the treatment was completed or interrupted            
        string popupMessage = finishTreatmentNormally
            ? "RG_Sarcophagus_Message_TreatmentComplete".Translate(patientPawn.LabelCap, patientPawn)
            : "RG_Sarcophagus_Message_TreatmentInterrupted".Translate(patientPawn.LabelCap, patientPawn);
        MessageTypeDef popupMessageType = finishTreatmentNormally
            ? MessageTypeDefOf.PositiveEvent
            : MessageTypeDefOf.NegativeHealthEvent;

        Messages.Message(popupMessage, patientPawn, popupMessageType, true);

        // Restore previously saved patient food need level
        if (patientPawn.needs.food != null)
            patientPawn.needs.food.CurLevelPercentage = _patientSavedFoodNeed;

        // Restore previously saved patient DBH thirst and reset DBH bladder/hygiene need levels
        if (ModCompatibility.DbhIsActive)
        {
            DbhCompatibility.SetThirstNeedCurLevelPercentage(patientPawn, _patientSavedDbhThirstNeed);
            DbhCompatibility.SetBladderNeedCurLevelPercentage(patientPawn, 1f);
            DbhCompatibility.SetHygieneNeedCurLevelPercentage(patientPawn, 1f);
        }

        // Remove treatable traits only if treatment was completed normally
        if (!_patientTraitsToRemove.NullOrEmpty() && finishTreatmentNormally)
        {
            patientPawn.story?.traits.allTraits
                .RemoveAll(x =>
                    _patientTraitsToRemove.Contains(x));
            string letterLabel = "RG_Sarcophagus_Letter_TraitRemoved_Label".Translate();
            string letterText = "RG_Sarcophagus_Letter_TraitRemoved_Desc"
                .Translate(patientPawn.Named("PAWN")) + string.Join("", (from t in _patientTraitsToRemove select "\n- " + t.def.degreeDatas.FirstOrDefault().LabelCap).ToArray());
            Find.LetterStack.ReceiveLetter(
                letterLabel,
                letterText,
                LetterDefOf.PositiveEvent,
                new TargetInfo(patientPawn));
        }

        // If the patient is a Sanguophage, top up their Hemogen
        if (ModsConfig.BiotechActive && patientPawn.RaceProps.Humanlike)
        {
            Gene_Hemogen gene_Hemogen = patientPawn.genes?.GetFirstGeneOfType<Gene_Hemogen>();
            if (gene_Hemogen != null)
                gene_Hemogen.Value = gene_Hemogen.InitialResourceMax;
        }

        // Refresh pawn renderer (especially important for Anomaly DLC not updating visuals from removed hediffs)
        patientPawn.Drawer.renderer.SetAllGraphicsDirty();

        // Refreshed pawn disabled work tags (especially important for Anomaly DLC not updating work tags from removed hediffs)
        patientPawn.Notify_DisabledWorkTypesChanged();

        // Clear pawn hediff cache and try to get them off the sarcophagus
        patientPawn.health.hediffSet.DirtyCache();
        patientPawn.health.CheckForStateChange(null, null);

        if (RimgateMod.Debug)
        {
            string message = finishTreatmentNormally
                ? "NORMAL".Colorize(Color.green)
                : "ABORTED".Colorize(Color.red);
            Log.Message(this + $" :: Discharged patient {patientPawn} ({message})");
        }
    }

    public void StartWickSustainer()
    {
        SoundInfo info = SoundInfo.InMap(this, MaintenanceType.PerTick);
        _wickSustainer = def.building.soundDispense.TrySpawnSustainer(info);
    }

    public override AcceptanceReport ClaimableBy(Faction fac)
    {
        if (!_contentsKnown && _innerContainer.Any)
            return false;

        return base.ClaimableBy(fac);
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

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        var map = Map;
        base.Destroy(mode);

        if (mode == DestroyMode.Deconstruct || mode == DestroyMode.KillFinalize)
        {
            if (mode != DestroyMode.Deconstruct)
            {
                foreach (Pawn p in _innerContainer)
                    Verse.HealthUtility.DamageUntilDowned(p);
            }

            if (PatientPawn != null)
                DischargePatient(PatientPawn);

            _innerContainer.TryDropAll(Position, map, ThingPlaceMode.Near);
        }

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
    }
}