using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using static HarmonyLib.Code;

namespace Rimgate;

public class FloatMenuOptionProvider_DialAddress : FloatMenuOptionProvider
{
    private WorldComp_StargateAddresses _cachedComp;

    protected override bool Drafted => true;

    protected override bool Undrafted => true;

    protected override bool Multiselect => false;

    protected override bool MechanoidCanDo => true;

    public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
    {
        var dhd = clickedThing as Building_DHD;

        if (dhd == null)
            yield break;

        var control = dhd.StargateControl;
        if (control == null)
        {
            yield return new FloatMenuOption(
                "RG_CannotDialNoGate".Translate(),
                null);
            yield break;
        }

        Pawn pawn = context.FirstSelectedPawn;
        bool canReach = pawn.CanReach(
            clickedThing.InteractionCell,
            PathEndMode.Touch,
            Danger.Deadly,
            false,
            false,
            TraverseMode.ByPawn);
        if (!canReach)
        {
            yield return new FloatMenuOption(
                "RG_CannotDialNoReach".Translate(),
                null);
            yield break;
        }

        if (!dhd.Powered)
        {
            yield return new FloatMenuOption(
                "RG_CannotDialNoPower".Translate(),
                null);
            yield break;
        }

        if (control.IsActive)
        {
            yield return new FloatMenuOption(
                "RG_CannotDialGateIsActive".Translate(),
                null);

            yield break;
        }

        _cachedComp ??= Find.World.GetComponent<WorldComp_StargateAddresses>();

        _cachedComp.CleanupAddresses();
        if (_cachedComp.AddressCount < 2) // home + another site
        {
            yield return new FloatMenuOption(
                "RG_CannotDialNoDestinations".Translate(),
                null);
            yield break;
        }

        if (control.TicksUntilOpen > -1)
        {
            yield return new FloatMenuOption(
                "RG_CannotDialIncoming".Translate(),
                null);
            yield break;
        }

        foreach (PlanetTile tile in _cachedComp.AddressList)
        {
            if (tile == control.GateAddress)
                continue;

            MapParent sgMap = Find.WorldObjects.MapParentAt(tile);
            string designation = StargateUtil.GetStargateDesignation(tile);

            yield return new FloatMenuOption(
                "RG_DialGate".Translate(designation, sgMap.Label),
                () =>
                {
                    dhd.LastDialledAddress = tile;
                    Job job = JobMaker.MakeJob(RimgateDefOf.Rimgate_DialStargate, clickedThing);
                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                });
        }
    }

    private static bool CanEnterGate(Pawn pawn, Building_Stargate gate)
    {
        return pawn.CanReach(gate, PathEndMode.ClosestTouch, Danger.Deadly);
    }
}
