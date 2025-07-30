using RimWorld;
using UnityEngine;
using Verse;

namespace Rimgate;

public class Comp_Sarcophagus : ThingComp
{
    public CompProperties_Sarcophagus Props => (CompProperties_Sarcophagus)props;

    // only apply addiction if the patient received full treatment
    public void HandleAfterEffects(Pawn patient, bool isPostTreatment = true)
    {
        if (!Props.applyAddictionHediff || !patient.RaceProps.IsFlesh)
            return;

        Hediff_Addiction hediff_Addiction = AddictionUtility.FindAddictionHediff(patient, Rimgate_DefOf.Rimgate_SarcophagusChemical);

        if (hediff_Addiction != null)
            hediff_Addiction.Severity += Props.existingAddictionSeverityOffset;
        else if (isPostTreatment)
            ApplyAddiction(patient); 

        AdjustChemicalNeed(patient);
        if (isPostTreatment)
            ApplyHigh(patient);

        patient.drugs?.Notify_DrugIngested(parent);
        Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.IngestedDrug, patient.Named(HistoryEventArgsNames.Doer)));
    }

    public void ApplyAddiction(Pawn patient)
    {
        if (Rand.Value >= Props.addictiveness) return;

        patient.health.AddHediff(Rimgate_DefOf.Rimgate_SarcophagusAddiction);
        if (PawnUtility.ShouldSendNotificationAbout(patient))
        {
            Find.LetterStack.ReceiveLetter(
                "LetterLabelNewlyAddicted"
                    .Translate(Rimgate_DefOf.Rimgate_SarcophagusChemical.label)
                    .CapitalizeFirst(),
                "LetterNewlyAddicted"
                    .Translate(
                        patient.LabelShort,
                        Rimgate_DefOf.Rimgate_SarcophagusChemical.label,
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
        if (!patient.needs.TryGetNeed(Rimgate_DefOf.Rimgate_SarcophagusChemicalNeed, out var need))
            return;

        float effect = Props.needLevelOffset;
        AddictionUtility.ModifyChemicalEffectForToleranceAndBodySize(
            patient,
            Rimgate_DefOf.Rimgate_SarcophagusChemical,
            ref effect,
            applyGeneToleranceFactor: false);
        need.CurLevel += effect;
    }

    public void ApplyHigh(Pawn patient)
    {
        if (patient.health.hediffSet.HasHediff(Rimgate_DefOf.Rimgate_SarcophagusHigh))
            return;

        Pawn_HealthTracker pht = patient.health;
        Hediff hediff = HediffMaker.MakeHediff(Rimgate_DefOf.Rimgate_SarcophagusHigh, patient);
        float effect = Props.severity <= 0f
            ? Rimgate_DefOf.Rimgate_SarcophagusHigh.initialSeverity
            : Props.severity;
        AddictionUtility.ModifyChemicalEffectForToleranceAndBodySize(
            patient,
            null,
            ref effect,
            false,
            false);
        hediff.Severity = effect;
        patient.health.AddHediff(hediff);
    }
}