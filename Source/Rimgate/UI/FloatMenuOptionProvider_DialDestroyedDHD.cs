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

        var control = Building_Stargate.GetStargateOnMap(clickedThing.Map);
        if (control == null)
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

        StargateUtil.CleanupAddresses();
        var addressList = StargateUtil.WorldComp.AddressList.Where(t => t != control.GateAddress).ToList();
        if (addressList == null || addressList.Count == 0)
        {
            yield return new FloatMenuOption(
                "RG_CannotDial".Translate("RG_CannotDialNoDestinations".Translate()),
                null);
            yield break;
        }

        if (control.TicksUntilOpen > -1)
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
                Job job = JobMaker.MakeJob(RimgateDefOf.Rimgate_DialStargate, clickedThing);
                job.count = 1;
                job.playerForced = true;
                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            });
    }

    private static bool CanEnterGate(Pawn pawn, Building_Stargate gate)
    {
        return pawn.CanReach(gate, PathEndMode.ClosestTouch, Danger.Deadly);
    }
}
