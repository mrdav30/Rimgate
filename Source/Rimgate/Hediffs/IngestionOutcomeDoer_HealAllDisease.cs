using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Rimgate;

public class IngestionOutcomeDoer_HealAllDisease : IngestionOutcomeDoer
{
    public List<HediffDef> exclusions;

    public List<HediffDef> inclusions;

    protected override void DoIngestionOutcomeSpecial(
        Pawn pawn,
        Thing ingested,
        int ingestedCount)
    {
        MedicalUtil.FixImmunizableHealthConditions(
            pawn,
            inclusions,
            exclusions);
        if (PawnUtility.ShouldSendNotificationAbout(pawn))
        {
            TaggedString label = "RG_HealedDiseases".Translate(pawn);
            Messages.Message(label, pawn, MessageTypeDefOf.PositiveEvent);
        }
    }
}
