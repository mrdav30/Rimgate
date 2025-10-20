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
    protected override bool Drafted => true;

    protected override bool Undrafted => true;

    protected override bool Multiselect => false;

    protected override bool MechanoidCanDo => true;

    public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
    {
        Building_DHD dhd = clickedThing as Building_DHD;
        if (dhd == null || dhd.DHDControl == null)
            yield break;

        var control = dhd.DHDControl;
        if (!control.IsConnectedToStargate)
        {
            yield return new FloatMenuOption(
                "RG_CannotDialNoGate".Translate(),
                null);
            yield break;
        }

        Pawn pawn = context.FirstSelectedPawn;
        bool canReach = pawn.CanReach(
            dhd.InteractionCell,
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

        if (control.Props.requiresPower
            && control.PowerTrader?.PowerOn == false)
        {
            yield return new FloatMenuOption(
                "RG_CannotDialNoPower".Translate(),
                null);
            yield break;
        }

        Comp_StargateControl stargate = control.GetLinkedStargate();
        if (stargate.IsActive)
        {
            yield return new FloatMenuOption(
                "RG_CannotDialGateIsActive".Translate(),
                null);

            yield break;
        }

        WorldComp_StargateAddresses addressComp = Find.World.GetComponent<WorldComp_StargateAddresses>();

        addressComp.CleanupAddresses();
        if (addressComp.AddressCount < 2) // home + another site
        {
            yield return new FloatMenuOption(
                "RG_CannotDialNoDestinations".Translate(),
                null);
            yield break;
        }

        if (stargate.TicksUntilOpen > -1)
        {
            yield return new FloatMenuOption(
                "RG_CannotDialIncoming".Translate(),
                null);
            yield break;
        }

        foreach (PlanetTile tile in addressComp.AddressList)
        {
            if (tile == stargate.GateAddress)
                continue;

            MapParent sgMap = Find.WorldObjects.MapParentAt(tile);
            string designation = StargateUtility.GetStargateDesignation(tile);

            yield return new FloatMenuOption(
                "RG_DialGate".Translate(designation, sgMap.Label),
                () =>
                {
                    control.LastDialledAddress = tile;
                    Job job = JobMaker.MakeJob(RimgateDefOf.Rimgate_DialStargate, dhd);
                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                });
        }
    }

    private static bool CanEnterGate(Pawn pawn, Building_Stargate gate)
    {
        return pawn.CanReach(gate, PathEndMode.ClosestTouch, Danger.Deadly);
    }
}
