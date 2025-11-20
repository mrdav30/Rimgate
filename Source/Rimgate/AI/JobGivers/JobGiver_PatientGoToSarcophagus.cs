using Rimgate;
using RimWorld;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobGiver_PatientGoToSarcophagus : ThinkNode_JobGiver
{
    protected override Job TryGiveJob(Pawn pawn)
    {
        if (pawn.Downed)
        {
            if (pawn.GetPosture().InBed() || !pawn.health.CanCrawl)
                return null;
        }

        Building_Sarcophagus sarcophagus = SarcophagusUtility.FindBestSarcophagus(pawn, pawn);
        if (sarcophagus != null)
        {
            Job job = JobMaker.MakeJob(RimgateDefOf.Rimgate_PatientGoToSarcophagus, sarcophagus);
            job.count = 1;
            return job;
        }

        return null;
    }
}
