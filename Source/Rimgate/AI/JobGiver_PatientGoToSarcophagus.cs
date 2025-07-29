using Rimgate;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobGiver_PatientGoToSarcophagus : ThinkNode_JobGiver
{
    protected override Job TryGiveJob(Pawn pawn)
    {
        Building_Bed_Sarcophagus bed = SarcophagusRestUtility.FindBestSarcophagus(pawn, pawn);
        return bed != null && !bed.HasAnyContents
            ? JobMaker.MakeJob(Rimgate_DefOf.Rimgate_PatientGoToSarcophagus, bed) 
            : null;
    }
}
