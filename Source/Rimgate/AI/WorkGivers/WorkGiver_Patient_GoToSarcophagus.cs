using RimWorld;
using Verse;
using Verse.AI;

namespace Rimgate;

public class WorkGiver_Patient_GoToSarcophagus : WorkGiver
{
    public static JobGiver_PatientGoToSarcophagus Giver = new JobGiver_PatientGoToSarcophagus();

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        return pawn.IsHashIntervalTick(GenTicks.TickRareInterval);
    }

    public override Job NonScanJob(Pawn pawn)
    {
        ThinkResult thinkResult = Giver.TryIssueJobPackage(pawn, default(JobIssueParams));
        if (thinkResult.IsValid)
            return thinkResult.Job;

        return null;
    }
}