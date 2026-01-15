using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Verse;
using Verse.AI;

namespace Rimgate;

public class FloatMenuOptionProvider_DialAddress : FloatMenuOptionProvider
{
    protected override bool Drafted => true;

    protected override bool Undrafted => true;

    protected override bool Multiselect => false;

    protected override bool MechanoidCanDo => true;

    public override bool TargetThingValid(Thing thing, FloatMenuContext context)
    {
        return thing is Building_DHD;
    }

    public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
    {
        var dhd = clickedThing as Building_DHD;
        if (dhd == null)
            yield break;

        var control = dhd.LinkedStargate;
        if (control == null)
        {
            yield return new FloatMenuOption(
                "RG_CannotDial".Translate("RG_CannotDialNoGate".Translate()),
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
                "RG_CannotDial".Translate("CannotReach".Translate()),
                null);
            yield break;
        }

        if (!dhd.Powered)
        {
            yield return new FloatMenuOption(
                "RG_CannotDial".Translate("NoPower".Translate()),
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

        foreach (PlanetTile tile in addressList)
        {
            string designation = StargateUtil.GetStargateDesignation(tile);
            MapParent gwo = Find.WorldObjects.MapParentAt(tile);

            if (StargateUtil.ActiveQuestSitesAtLimit && !gwo.HasMap)
            {
                bool isQuestSite = Find.WorldObjects.MapParentAt(tile) is WorldObject_GateQuestSite;
                if (isQuestSite)
                {
                    yield return new FloatMenuOption(
                        $"{"RG_DialGate".Translate()} {designation}"
                        + $" ({"RG_CannotDial".Translate("RG_CannotDialQuestSiteLimitReached".Translate())})",
                        null);
                    continue;
                }
            }

            if (tile.LayerDef.isSpace && !StargateUtil.ModificationEquipmentActive)
            {
                yield return new FloatMenuOption(
                    $"{"RG_DialGate".Translate()} {designation}"
                    + $" ({"RG_CannotDial".Translate("RG_CannotDialNoModEquipment".Translate())})",
                    null);
                continue;
            }

            yield return new FloatMenuOption(
                $"{"RG_DialGate".Translate()} {designation} ({gwo.Label})",
                () =>
                {
                    dhd.LastDialledAddress = tile;
                    Job job = JobMaker.MakeJob(RimgateDefOf.Rimgate_DialStargate, clickedThing);
                    job.count = 1;
                    job.playerForced = true;
                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                });
        }
    }

    private static bool CanEnterGate(Pawn pawn, Building_Stargate gate)
    {
        return pawn.CanReach(gate, PathEndMode.ClosestTouch, Danger.Deadly);
    }
}
