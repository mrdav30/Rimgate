using Rimgate;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobGiver_PatientGoToSarcophagus : ThinkNode_JobGiver
{
    protected override Job TryGiveJob(Pawn pawn)
    {
        Building_Bed_Sarcophagus bedSarcophagus = SarcophagusRestUtility.FindBestSarcophagus(pawn, pawn);
        return bedSarcophagus != null 
            ? JobMaker.MakeJob(Rimgate_DefOf.Rimgate_PatientGoToSarcophagus, bedSarcophagus) 
            : null;
    }
}
