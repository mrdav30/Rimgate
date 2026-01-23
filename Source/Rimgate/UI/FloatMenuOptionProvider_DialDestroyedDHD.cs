using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace Rimgate;

public class FloatMenuOptionProvider_DialDestroyedDHD : FloatMenuOptionProvider
{
    protected override bool Drafted => true;

    protected override bool Undrafted => true;

    protected override bool Multiselect => false;

    protected override bool MechanoidCanDo => true;

    public override bool TargetThingValid(Thing thing, FloatMenuContext context)
    {
        return thing.def == RimgateDefOf.Rimgate_DialHomeDeviceDestroyed
            && ResearchUtil.DHDLogicComplete;
    }

    public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
    {
        var dhd = clickedThing.def == RimgateDefOf.Rimgate_DialHomeDeviceDestroyed;
        if (dhd == null || !ResearchUtil.DHDLogicComplete)
            yield break;

        if (!Building_Gate.TryGetSpawnedGateOnMap(clickedThing.Map, out Building_Gate control))
            yield break;

        Pawn pawn = context.FirstSelectedPawn;
        bool canReach = pawn.CanReach(
            clickedThing.Position,
            PathEndMode.ClosestTouch,
            Danger.Deadly,
            false,
            false,
            TraverseMode.ByPawn);
        if (!canReach)
        {
            yield return new FloatMenuOption(
                "RG_CannotDial".Translate("CannotReach".Translate()),
                null);
            yield break;
        }

        if (control.IsActive)
        {
            yield return new FloatMenuOption(
                "RG_CannotDial".Translate("RG_CannotDialGateIsActive".Translate()),
                null);

            yield break;
        }


        var addressList = GateUtil.GetAddressList(control.GateAddress);
        if (addressList == null || addressList.Count == 0)
        {
            yield return new FloatMenuOption(
                "RG_CannotDial".Translate("RG_CannotDialNoDestinations".Translate()),
                null);
            yield break;
        }

        if (control.IsOpeningQueued)
        {
            yield return new FloatMenuOption(
                "RG_CannotDial".Translate("RG_CannotDialIncoming".Translate()),
                null);
            yield break;
        }

        yield return new FloatMenuOption(
            "RG_DialGate".Translate(),
            () =>
            {
                Job job = JobMaker.MakeJob(RimgateDefOf.Rimgate_DialGate, clickedThing);
                job.count = 1;
                job.playerForced = true;
                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            });
    }

    private static bool CanEnterGate(Pawn pawn, Building_Gate gate)
    {
        return pawn.CanReach(gate, PathEndMode.ClosestTouch, Danger.Deadly);
    }
}
