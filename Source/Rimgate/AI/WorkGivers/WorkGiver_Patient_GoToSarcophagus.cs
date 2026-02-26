using RimWorld;
using Verse;
using Verse.AI;

namespace Rimgate;

public class WorkGiver_Patient_GoToSarcophagus : WorkGiver
{
    public static JobGiver_PatientGoToSarcophagus Giver = new();

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        // only check every few ticks since this is a non-urgent job
        if (pawn?.Map == null
            || !pawn.IsHashIntervalTick(GenTicks.TickRareInterval)
            || !pawn.Map.listerBuildings.ColonistsHaveBuilding(building => building is Building_Sarcophagus)) return true;

        return false;
    }

    public override Job NonScanJob(Pawn pawn)
    {
        ThinkResult thinkResult = Giver.TryIssueJobPackage(pawn, default);
        if (thinkResult.IsValid)
            return thinkResult.Job;

        return null;
    }
}
