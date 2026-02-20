using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Rimgate;

public class WorkGiver_CalibrateCloningPod : WorkGiver_Scanner
{
    public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

    public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        var list = pawn.Map.listerThings.ThingsOfDef(RimgateDefOf.Rimgate_WraithCloningPod);
        for (int i = 0; i < list.Count; i++)
        {
            var t = list[i];
            if (t is not Building_CloningPod pod) continue;
            if (!pod.Power.PowerOn || !pod.HasHostPawn || pod.Status != CloningStatus.Idle) continue;
            if (!pawn.CanReserveAndReach(t, PathEndMode.Touch, Danger.Deadly)) continue;
            yield return t;
        }
    }

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        if (pawn.IncapableOfGivingAid(out _)) return true;
        if (pawn.skills.GetSkill(SkillDefOf.Medicine).levelInt < RimgateModSettings.MedicineSkillReq)
            return true;
        return false;
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t is not Building_CloningPod pod || !pod.Power.PowerOn || !pod.HasHostPawn || pod.Status != CloningStatus.Idle)
            return false;
        if (!pawn.CanReserveAndReach(t, PathEndMode.InteractionCell, Danger.Deadly))
            return false;
        return true;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t is not Building_CloningPod pod)
            return null;

        Job job = JobMaker.MakeJob(RimgateDefOf.Rimgate_CalibrateClonePodForPawn, pod);
        job.count = 1;
        job.playerForced = forced;

        return job;
    }
}
