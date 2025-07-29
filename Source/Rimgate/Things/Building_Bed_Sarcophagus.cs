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

public class Building_Bed_Sarcophagus : Building_Bed, IThingHolder, IOpenable, ISearchableContents
{
    public CompPowerTrader powerComp;

    public Comp_Sarcophagus sarcophagusSettings;

    public Comp_TreatmentRestrictions treatmentRestrictions;

    private List<Hediff> patientTreatableHediffs = new();

    private static float patientSavedFoodNeed;

    private static float patientSavedDbhThirstNeed;

    private static List<Trait> patientTraitsToRemove = new();

    private float totalNormalizedSeverities = 0;

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

    public bool allowGuests = false;

    public bool Aborted = false;

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

    public SarcophagusStatus status = SarcophagusStatus.Idle;

    private Sustainer wickSustainer;

    protected ThingOwner innerContainer;

    protected bool contentsKnown;

    public string openedSignal;

    public virtual int OpenTicks => 250;

    public Pawn PatientPawn => innerContainer.Count > 0
        ? (Pawn)innerContainer[0]
        : null;
    
    public virtual bool CanOpen => HasAnyContents;
    
    public bool HasAnyContents => innerContainer.Count > 0;
    
    public ThingOwner SearchableContents => innerContainer;

    public Building_Bed_Sarcophagus()
    {
        innerContainer = new ThingOwner<Thing>(this, oneStackOnly: true);
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);

        Medical = true; // Always ensure sarcophagus is medical bed (compat fix for SOS2)
        powerComp = GetComp<CompPowerTrader>();
        sarcophagusSettings = GetComp<Comp_Sarcophagus>();
        treatmentRestrictions = GetComp<Comp_TreatmentRestrictions>();

        MaxDiagnosingTicks = GenTicks.SecondsToTicks(sarcophagusSettings.Props.maxDiagnosisTime);
        MaxHealingTicks = GenTicks.SecondsToTicks(sarcophagusSettings.Props.maxPerHediffHealingTime);
        DiagnosingPowerConsumption = sarcophagusSettings.Props.diagnosisModePowerConsumption;
        HealingPowerConsumption = sarcophagusSettings.Props.healingModePowerConsumption;

        AlwaysTreatableHediffs = treatmentRestrictions.Props.alwaysTreatableHediffs;
        NeverTreatableHediffs = treatmentRestrictions.Props.neverTreatableHediffs;
        NonCriticalTreatableHediffs = treatmentRestrictions.Props.nonCriticalTreatableHediffs;
        UsageBlockingHediffs = treatmentRestrictions.Props.usageBlockingHediffs;
        UsageBlockingTraits = treatmentRestrictions.Props.usageBlockingTraits;
        AlwaysTreatableTraits = treatmentRestrictions.Props.alwaysTreatableTraits;
        DisallowedRaces = treatmentRestrictions.Props.disallowedRaces;
        DisallowedXenotypes = treatmentRestrictions.Props.disallowedXenotypes;

        if (Faction?.IsPlayer == true)
            contentsKnown = true;
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        if (powerComp.PowerOn
            && (status == SarcophagusStatus.DiagnosisFinished
                || status == SarcophagusStatus.HealingStarted
                || status == SarcophagusStatus.HealingFinished))
        {
            DischargePatient(PatientPawn, false);
            EjectContents();
        }

        ForOwnerType = BedOwnerType.Colonist;
        Medical = false;
        allowGuests = false;

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
        Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        Scribe_Values.Look(ref contentsKnown, "contentsKnown", false);
        Scribe_Values.Look(ref openedSignal, "openedSignal");
        Scribe_Values.Look(ref allowGuests, "allowGuests", false);
        BackCompatibility.PostExposeData(this);
    }

    private string[] gizmosToDisableWhileInUse;
    public override IEnumerable<Gizmo> GetGizmos()
    {
        string medicalToggleStr = "CommandBedSetAsMedicalLabel".Translate();
        string flickablePowerToggleStr = "CommandDesignateTogglePowerLabel".Translate();
        string allowToggleStr = "CommandAllow".Translate();
        string forPrisonersToggleStr = "CommandBedSetForPrisonersLabel".Translate();

        gizmosToDisableWhileInUse = new string[]
        {
            flickablePowerToggleStr,
            allowToggleStr,
            forPrisonersToggleStr
        };

        foreach (Gizmo g in base.GetGizmos())
        {
            // Hide the Medical bed toggle, as sarcophagi are always Medical beds
            if (g is Command_Toggle act
                && (act.defaultLabel == medicalToggleStr)) continue;

            bool shouldDisable = (g is Command_Toggle act2
                    && gizmosToDisableWhileInUse.Contains(act2.defaultLabel)
                ) || g is Command_SetBedOwnerType;

            if (shouldDisable && PatientPawn != null)
            {
                // Disable various gizmos while sarcophagi is in use
                g.Disable("RG_Sarcophagus_CommandGizmoDisabled_SarcophagusInUse".Translate(def.LabelCap));
            }

            yield return g;
        }

        // Allow guests gizmo - only available on sarcophagi for humanlike colonists
        if (def.building.bed_humanlike && ForColonists)
        {
            yield return new Command_Toggle
            {
                defaultLabel = "RG_Sarcophagus_CommandGizmoAllowGuests_Label".Translate(),
                defaultDesc = "RG_Sarcophagus_CommandGizmoAllowGuests_Desc".Translate(this.LabelCap),
                isActive = () => allowGuests,
                toggleAction = delegate
                {
                    allowGuests = !allowGuests;
                },
                icon = ContentFinder<Texture2D>.Get("UI/Icon/Button/RGSarcophagusAllowGuestsIcon", true),
                activateSound = SoundDefOf.Tick_Tiny
            };
        }

        // Abort gizmo - kick out patient if treatment is aborted
        yield return new Command_Action
        {
            defaultLabel = "RG_Sarcophagus_CommandGizmoAbortTreatment_Label".Translate(),
            defaultDesc = "RG_Sarcophagus_CommandGizmoAbortTreatment_Desc".Translate(),
            Disabled = PatientPawn == null,
            action = delegate
            {
                if (PatientPawn != null)
                    Abort();
            },
            icon = ContentFinder<Texture2D>.Get("UI/Icon/Button/RGAbortSarcophagusTreatmentIcon", true),
            activateSound = SoundDefOf.Click
        };

        Gizmo gizmo = Building.SelectContainedItemGizmo(this, PatientPawn);
        if (gizmo != null)
            yield return gizmo;

        if (RimgateMod.debug && CanOpen)
        {
            yield return new Command_Action
            {
                defaultLabel = "DEV: Open",
                action = Open
            };
        }
    }

    public override string GetInspectString()
    {
        StringBuilder stringBuilder = new StringBuilder();

        string inspectorStatus = null;

        // If minified, don't show computer and feedstock check Inspector messages
        if (ParentHolder != null && !(ParentHolder is Map))
            return string.Empty;

        stringBuilder.AppendInNewLine(powerComp.CompInspectStringExtra());

        if (def.building.bed_humanlike)
        {
            switch (ForOwnerType)
            {
                case BedOwnerType.Prisoner:
                    stringBuilder.AppendInNewLine("ForPrisonerUse"
                        .Translate());
                    break;
                case BedOwnerType.Slave:
                    stringBuilder.AppendInNewLine("ForSlaveUse"
                        .Translate());
                    break;
                case BedOwnerType.Colonist:
                    stringBuilder.AppendInNewLine("ForColonistUse"
                        .Translate());
                    break;
                default:
                    Log.Error($"Unknown bed owner type: {ForOwnerType}");
                    break;
            }
        }

        if (!powerComp.PowerOn)
            inspectorStatus = "RG_Sarcophagus_InspectorStatus_NoPower"
                .Translate();
        else
        {
            switch (status)
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
        }

        stringBuilder.AppendInNewLine(inspectorStatus);

        if (RimgateMod.debug)
        {
            stringBuilder.AppendInNewLine("DEBUG: "
                + status.ToStringSafe()
                + " / aborted = "
                + Aborted.ToStringYesNo());
        }

        return stringBuilder.ToString();
    }

    public ThingOwner GetDirectlyHeldThings() => innerContainer;

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    public virtual bool Accepts(Thing pawn)
    {
        return innerContainer.CanAcceptAnyOf(pawn, false);
    }

    public virtual bool TryAcceptPawn(Thing pawn, bool allowSpecialEffects = true)
    {
        if (!Accepts(pawn))
            return false;

        //((Entity)pawn).DeSpawn((DestroyMode)0);

        bool success;
        if (pawn.holdingOwner != null)
        {
            pawn.holdingOwner.TryTransferToContainer(pawn, innerContainer, pawn.stackCount);
            success = true;
        }
        else
            success = innerContainer.TryAdd(pawn);

        if (success)
        {
            if (pawn.Faction?.IsPlayer == true)
                contentsKnown = true;

            SoundDefOf.CryptosleepCasket_Accept.PlayOneShot(new TargetInfo(base.Position, base.Map));

            return true;
        }

        return false;
    }

    public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
    {
        bool canShowOptions = myPawn.RaceProps.Humanlike
            && !ForPrisoners
            && Medical
            && !myPawn.Drafted
            && Faction == Faction.OfPlayer
            && RestUtility.CanUseBedEver(myPawn, def);
        if (!canShowOptions) yield break;

        if (SarcophagusHealthAIUtility.HasUsageBlockingHediffs(myPawn, UsageBlockingHediffs))
        {
            List<Hediff> blockedHediffs = new();
            myPawn.health.hediffSet.GetHediffs(ref blockedHediffs);

            yield return new FloatMenuOption("UseMedicalBed".Translate() + " (" + "RG_Sarcophagus_FloatMenu_PatientWithHediffNotAllowed".Translate(blockedHediffs.First(h => UsageBlockingHediffs.Contains(h.def)).LabelCap) + ")", null);
            yield break;
        }

        if (SarcophagusHealthAIUtility.HasUsageBlockingTraits(myPawn, UsageBlockingTraits))
        {
            yield return new FloatMenuOption("UseMedicalBed".Translate() + " blocking traits (" + "RG_Sarcophagus_FloatMenu_PatientWithTraitNotAllowed".Translate(myPawn.story?.traits.allTraits.First(t => UsageBlockingTraits.Contains(t.def)).LabelCap) + ")", null);
            yield break;
        }

        if (!SarcophagusHealthAIUtility.IsValidXenotypeForSarcophagus(myPawn, DisallowedXenotypes))
        {
            yield return new FloatMenuOption("UseMedicalBed".Translate() + " (" + "RG_Sarcophagus_FloatMenu_RaceNotAllowed".Translate(myPawn.genes?.Xenotype.label.CapitalizeFirst()) + ")", null);
            yield break;
        }

        if (!SarcophagusHealthAIUtility.IsValidRaceForSarcophagus(myPawn, DisallowedRaces))
        {
            yield return new FloatMenuOption("UseMedicalBed".Translate() + " (" + "RG_Sarcophagus_FloatMenu_RaceNotAllowed".Translate(myPawn.def.label.CapitalizeFirst()) + ")", null);
            yield break;
        }

        if (SarcophagusHealthAIUtility.ShouldSeekSarcophagusRest(myPawn, this))
        {
            if (!powerComp.PowerOn)
            {
                yield return new FloatMenuOption("UseMedicalBed".Translate() + " (" + "RG_Sarcophagus_FloatMenu_Unpowered".Translate() + ")", null);
                yield break;
            }

            if (this.IsForbidden(myPawn))
            {
                yield return new FloatMenuOption("UseMedicalBed".Translate() + " (" + "ForbiddenLower".Translate() + ")", null);
                yield break;
            }

            if (!SarcophagusHealthAIUtility.HasAllowedMedicalCareCategory(myPawn))
            {
                yield return new FloatMenuOption("UseMedicalBed".Translate() + " (" + "RG_Sarcophagus_FloatMenu_MedicalCareCategoryTooLow".Translate() + ")", null);
                yield break;
            }
        }
        else
        {
            yield return new FloatMenuOption("UseMedicalBed".Translate() + " (" + "NotInjured".Translate() + ")", null);
            yield break;
        }

        Action action = delegate
        {
            if (!ForPrisoners
                && Medical
                && myPawn.CanReserveAndReach(
                    this,
                    PathEndMode.ClosestTouch,
                    Danger.Deadly,
                    SleepingSlotsCount,
                    -1,
                    null,
                    ignoreOtherReservations: true))
            {
                if (myPawn.CurJobDef == JobDefOf.LayDown
                    && myPawn.CurJob.GetTarget(TargetIndex.A).Thing == this)
                {
                    myPawn.CurJob.restUntilHealed = true;
                }
                else
                {
                    Job job = JobMaker.MakeJob(JobDefOf.LayDown, this);
                    job.restUntilHealed = true;
                    myPawn.jobs.TryTakeOrderedJob(job);
                }

                myPawn.mindState.ResetLastDisturbanceTick();
            }
        };

        string reservedText = (AnyUnoccupiedSleepingSlot
                ? "ReservedBy"
                : "SomeoneElseSleeping"
                ).CapitalizeFirst();
        yield return FloatMenuUtility.DecoratePrioritizedTask(
            new FloatMenuOption("UseMedicalBed".Translate(), action),
            myPawn,
            this,
            reservedText);
    }

    private void SwitchState()
    {
        SarcophagusStatus oldStatus = status;
        switch (status)
        {
            case SarcophagusStatus.Idle:
                status = SarcophagusStatus.DiagnosisStarted;
                break;

            case SarcophagusStatus.DiagnosisStarted:
                status = SarcophagusStatus.DiagnosisFinished;
                break;

            case SarcophagusStatus.DiagnosisFinished:
                status = SarcophagusStatus.HealingStarted;
                break;

            case SarcophagusStatus.HealingStarted:
                status = SarcophagusStatus.HealingFinished;
                break;

            case SarcophagusStatus.HealingFinished:
                status = SarcophagusStatus.PatientDischarged;
                break;

            case SarcophagusStatus.PatientDischarged:
                status = SarcophagusStatus.Idle;
                break;

            default:
                status = SarcophagusStatus.Error;
                break;
        }

        if (RimgateMod.debug)
        {
            Log.Message(this + " :: state change from " + oldStatus.ToStringSafe().Colorize(Color.yellow) + " to " + status.ToStringSafe().Colorize(Color.yellow));
        }
    }

    public bool IsSarcophagusInUse()
    {
        bool result;
        switch (status)
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
        patientTreatableHediffs = patientPawn.health.hediffSet.hediffs
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

        List<Hediff> artificialPartHediffs = patientTreatableHediffs.FindAll((Hediff x) =>
            x.def.hediffClass.Equals(typeof(Hediff_AddedPart)));

        List<BodyPartRecord> childPartsToSkip = new List<BodyPartRecord>();

        foreach (Hediff currentArtificialPartHediff in artificialPartHediffs)
            childPartsToSkip.AddRange(GetBodyPartDescendants(currentArtificialPartHediff.Part));

        // Only ignore Missing part Hediffs from body parts that have been replaced
        patientTreatableHediffs.RemoveAll((Hediff x) => childPartsToSkip.Any(p => x.Part == p) && x.def.hediffClass == typeof(Hediff_MissingPart));

        // Ignore hediffs/injuries that are:
        // - Not explicitly whitelisted as always treatable
        // - Blacklisted as never treatable
        // - Not explicitly greylisted as non-critical but treatable
        // - Not bad (i.e isBad = false) and not treatable
        patientTreatableHediffs.RemoveAll((Hediff x) =>
            !AlwaysTreatableHediffs.Contains(x.def)
            && (
                NeverTreatableHediffs.Contains(x.def)
                || (
                    !NonCriticalTreatableHediffs.Contains(x.def)
                    && !x.def.isBad
                    && !x.TendableNow()
                   )
               ));

        // Immediately treat blood loss hediff
        patientTreatableHediffs.RemoveAll(x => x.def == HediffDefOf.BloodLoss);
        PatientPawn.health.hediffSet.hediffs.RemoveAll(x => x.def == HediffDefOf.BloodLoss);

        // Calculate individual and total cumulative treatment time for each hediff/injury
        foreach (Hediff currentHediff in patientTreatableHediffs)
        {
            float currentSeverity = currentHediff.Severity;

            // currentHediff.Part will throw an error if a hediff is applied to the whole body (e.g. malnutrition), as part == null
            float currentBodyPartMaxHealth = currentHediff.Part != null
                ? EbfCompatibilityWrapper.GetMaxHealth(
                    currentHediff.Part.def,
                    patientPawn,
                    currentHediff.Part)
                : 1;

            float currentNormalizedSeverity = currentSeverity < 1
                ? currentSeverity
                : currentSeverity / currentBodyPartMaxHealth;

            totalNormalizedSeverities += currentNormalizedSeverity;

            TotalHealingTicks += (int)Math.Ceiling(GetHediffNormalizedSeverity(currentHediff) * PatientBodySizeScaledMaxHealingTicks);

            // Tend all bleeding hediffs immediately so the pawn doesn't bleed out while on sarcophagus
            // The Hediff will be completely removed once the sarcophagus is done with the Healing process
            if (currentHediff.Bleeding)
                currentHediff.Tended(1, 1);
        }

        // Identify treatable traits for removal
        patientTraitsToRemove = patientPawn.story?.traits.allTraits.FindAll(x => AlwaysTreatableTraits.Contains(x.def));
    }

    private float GetHediffNormalizedSeverity(Hediff specificHediff = null)
    {
        Hediff currentHediff = specificHediff == null
            ? patientTreatableHediffs.First()
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
            patientPawn.needs.food.CurLevelPercentage = patientSavedFoodNeed;

        // Restore previously saved patient DBH thirst and reset DBH bladder/hygiene need levels
        if (ModCompatibility.DbhIsActive)
        {
            DbhCompatibility.SetThirstNeedCurLevelPercentage(patientPawn, patientSavedDbhThirstNeed);
            DbhCompatibility.SetBladderNeedCurLevelPercentage(patientPawn, 1f);
            DbhCompatibility.SetHygieneNeedCurLevelPercentage(patientPawn, 1f);
        }

        // Remove treatable traits only if treatment was completed normally
        if (!patientTraitsToRemove.NullOrEmpty() && finishTreatmentNormally)
        {
            patientPawn.story?.traits.allTraits
                .RemoveAll(x =>
                    patientTraitsToRemove.Contains(x));
            string letterLabel = "RG_Sarcophagus_Letter_TraitRemoved_Label".Translate();
            string letterText = "RG_Sarcophagus_Letter_TraitRemoved_Desc"
                .Translate(patientPawn.Named("PAWN")) + string.Join("", (from t in patientTraitsToRemove select "\n- " + t.def.degreeDatas.FirstOrDefault().LabelCap).ToArray());
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

        if (RimgateMod.debug)
        {
            Log.Message(this + " :: Discharged patient " + patientPawn + " (" + (finishTreatmentNormally ? "NORMAL".Colorize(Color.green) : "ABORTED".Colorize(Color.red)) + ")");
        }
    }

    public void StartWickSustainer()
    {
        SoundInfo info = SoundInfo.InMap(this, MaintenanceType.PerTick);
        wickSustainer = def.building.soundDispense.TrySpawnSustainer(info);
    }

    protected override void Tick()
    {
        base.Tick();

        // State-dependent power consumption
        if (status == SarcophagusStatus.DiagnosisStarted
            || status == SarcophagusStatus.DiagnosisFinished)
        {
            powerComp.PowerOutput = -DiagnosingPowerConsumption;
        }
        else if (status == SarcophagusStatus.HealingStarted
            || status == SarcophagusStatus.HealingFinished)
        {
            powerComp.PowerOutput = -HealingPowerConsumption;
        }
        else
            powerComp.PowerOutput = -powerComp.Props.PowerConsumption;

        // Main patient treatment cycle logic
        if (PatientPawn != null)
        {
            PatientBodySizeScaledMaxDiagnosingTicks = (int)(MaxDiagnosingTicks * PatientPawn.BodySize);
            PatientBodySizeScaledMaxHealingTicks = (int)(MaxHealingTicks * PatientPawn.BodySize);

            // Interrupt treatment on power loss
            if (!powerComp.PowerOn)
            {
                DischargePatient(PatientPawn, false);
                EjectContents();
            }

            // Main logic
            switch (status)
            {
                case SarcophagusStatus.Idle:
                    {
                        DiagnosingTicks = PatientBodySizeScaledMaxDiagnosingTicks;
                        // Save initial patient food need level
                        if (PatientPawn.needs.food != null)
                            patientSavedFoodNeed = PatientPawn.needs.food.CurLevelPercentage;

                        // Save initial patient DBH thirst and reset DBH bladder/hygiene need levels
                        if (ModCompatibility.DbhIsActive)
                        {
                            patientSavedDbhThirstNeed = DbhCompatibility.GetThirstNeedCurLevelPercentage(PatientPawn);
                            DbhCompatibility.SetBladderNeedCurLevelPercentage(PatientPawn, 1f);
                            DbhCompatibility.SetHygieneNeedCurLevelPercentage(PatientPawn, 1f);
                        }

                        if (RimgateMod.debug)
                        {
                            Log.Message("\t"
                                + PatientPawn
                                + " :: initial DiagnosingTicks = "
                                + DiagnosingTicks);
                        }

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
                        if (patientTreatableHediffs.NullOrEmpty())
                            status = SarcophagusStatus.PatientDischarged;
                        else
                        {
                            // Scale healing time for the first hediff
                            // according to its (normalized) severity and
                            // patient body size
                            // i.e. More severe hediffs take longer,
                            // bigger pawns also take longer
                            HealingTicks = (int)Math.Ceiling(GetHediffNormalizedSeverity() * PatientBodySizeScaledMaxHealingTicks);
                            if (RimgateMod.debug)
                            {
                                Log.Message("\t" + PatientPawn + " :: first hediff HealingTicks = " + HealingTicks + " (hediff count: " + patientTreatableHediffs.Count() + ")");
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
                        // Don't remove 'good' treatable Hediffs but instead treat them with 100% quality (unless the 'good' Hediff is whitelisted as always treatable)
                        if (!patientTreatableHediffs.First().def.isBad
                            && !AlwaysTreatableHediffs.Contains(patientTreatableHediffs.First().def)
                            && !NonCriticalTreatableHediffs.Contains(patientTreatableHediffs.First().def))
                        {
                            patientTreatableHediffs.First().Tended(1, 1);
                        }
                        else
                            PatientPawn.health.hediffSet.hediffs.Remove(patientTreatableHediffs.First());

                        patientTreatableHediffs.RemoveAt(0);
                        if (!patientTreatableHediffs.NullOrEmpty())
                        {
                            // Scale healing time for the next hediff according to its (normalized) severity and patient body size
                            // i.e. More severe hediffs take longer, bigger pawns also take longer
                            HealingTicks = (int)Math.Ceiling(GetHediffNormalizedSeverity() * PatientBodySizeScaledMaxHealingTicks);

                            if (RimgateMod.debug)
                            {
                                Log.Message("\t" + PatientPawn + " :: next hediff HealingTicks = " + HealingTicks + " (hediff count: " + patientTreatableHediffs.Count() + ")");
                            }

                            // Jump back to the previous state to start healing the next hediff
                            status = SarcophagusStatus.HealingStarted;
                        }
                        else
                        {
                            DischargePatient(PatientPawn);
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
            if (status == SarcophagusStatus.DiagnosisStarted
                || status == SarcophagusStatus.DiagnosisFinished
                || status == SarcophagusStatus.HealingStarted
                || status == SarcophagusStatus.HealingFinished)
            {
                // Food
                if (PatientPawn.needs.food != null) PatientPawn.needs.food.CurLevelPercentage = 1f;

                // Dubs Bad Hygiene thirst, bladder and hygiene
                if (ModCompatibility.DbhIsActive)
                {
                    DbhCompatibility.SetThirstNeedCurLevelPercentage(PatientPawn, 1f);
                    DbhCompatibility.SetBladderNeedCurLevelPercentage(PatientPawn, 1f);
                    DbhCompatibility.SetHygieneNeedCurLevelPercentage(PatientPawn, 1f);
                }
            }
        }
        else
        {
            Reset();
            Aborted = false;
        }

        // sarcophagus glow animation
        if (this.IsHashIntervalTick(2))
        {
            if (IsSarcophagusInUse())
            {
                if (wickSustainer == null)
                    StartWickSustainer();
                else if (wickSustainer.Ended)
                    StartWickSustainer();
                else
                    wickSustainer.Maintain();
            }
        }
    }

    public override AcceptanceReport ClaimableBy(Faction fac)
    {
        if (!contentsKnown && innerContainer.Any)
            return false;

        return base.ClaimableBy(fac);
    }

    public virtual void Open()
    {
        if (!HasAnyContents)
            return;

        Abort();

        if (!openedSignal.NullOrEmpty())
            Find.SignalManager.SendSignal(new Signal(openedSignal, this.Named("SUBJECT")));

        DirtyMapMesh(Map);
    }

    private void Abort()
    {
        DischargePatient(PatientPawn, false);
        EjectContents();
        Aborted = true;
        Reset();
    }

    public virtual void EjectContents()
    {
        innerContainer.TryDropAll(
            InteractionCell,
            Map,
            ThingPlaceMode.Near);
        contentsKnown = true;
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        var map = Map;
        base.Destroy(mode);

        if (mode == DestroyMode.Deconstruct || mode == DestroyMode.KillFinalize)
        {
            if (mode != DestroyMode.Deconstruct)
            {
                foreach (Pawn p in innerContainer)
                    HealthUtility.DamageUntilDowned(p);
            }

            if (PatientPawn != null)
                DischargePatient(PatientPawn);

            innerContainer.TryDropAll(Position, map, ThingPlaceMode.Near);
        }

        innerContainer.ClearAndDestroyContents();
    }

    public void Reset()
    {
        status = SarcophagusStatus.Idle;
        if (!patientTreatableHediffs.NullOrEmpty())
            patientTreatableHediffs.Clear();

        if (!patientTraitsToRemove.NullOrEmpty())
            patientTraitsToRemove.Clear();

        DiagnosingTicks = 0;
        HealingTicks = 0;
        ProgressHealingTicks = 0;
        TotalHealingTicks = 0;
    }
}