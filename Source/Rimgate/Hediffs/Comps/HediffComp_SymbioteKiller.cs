using Rimgate;
using RimWorld;
using Verse;

namespace Rimgate;

public class HediffComp_SymbioteKiller : HediffComp
{
    public HediffCompProperties_SymbioteKiller Props => (HediffCompProperties_SymbioteKiller)props;

    public override void CompPostTick(ref float severityAdjustment)
    {
        Pawn pawn = parent.pawn;

        // Try to find the symbiote hediff
        Hediff symbiote = pawn.health.hediffSet.GetFirstHediffOfDef(RimgateDefOf.Rimgate_SymbioteImplant);
        if (symbiote == null)
            return;

        // Only trigger when we hit the critical threshold
        if (parent.Severity < Props.killThreshold)
            return;

        // If immunity has clearly outpaced severity, we consider it "treated in time"
        var immunityRecord = pawn.health.immunity.GetImmunityRecord(parent.def);
        if (immunityRecord != null && immunityRecord.immunity >= parent.Severity)
            return;

        if (!Rand.Chance(Props.killChance))
            return;

        // Kill / remove the symbiote
        pawn.health.RemoveHediff(symbiote);

        // send letter
        Messages.Message(
            "RG_Message_SymbioteKilledByPathogen".Translate(pawn.Named("PAWN")),
            pawn,
            MessageTypeDefOf.NegativeHealthEvent);
    }
}
