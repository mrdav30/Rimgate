using RimWorld;
using UnityEngine;
using Verse;

namespace Rimgate;

public class Comp_SarcophagusControl : ThingComp
{
    public CompProperties_SarcophagusControl Props => (CompProperties_SarcophagusControl)props;

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

        patient.drugs?.Notify_DrugIngested(parent);
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
}