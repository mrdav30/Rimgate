using Rimgate;
using RimWorld;
using Verse;
using Verse.AI;

namespace Rimgate;

public class WorkGiver_PatientGoToSarcophagus : WorkGiver
{
    public static JobGiver_PatientGoToSarcophagus jgpgtmp = new JobGiver_PatientGoToSarcophagus();

    public override Job NonScanJob(Pawn pawn)
    {
        ThinkResult thinkResult = jgpgtmp.TryIssueJobPackage(pawn, default(JobIssueParams));
        if (thinkResult.IsValid)
            return thinkResult.Job;

        return null;
    }
}