using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Rimgate;

public class MapComponent_MobileContainerPushPreview(Map map) : MapComponent(map)
{
    private static readonly Color PushGhostColorValid = new(1f, 1f, 1f, 1f);

    public override void MapComponentUpdate()
    {
        if (Find.CurrentMap != map)
            return;

        DesignationManager designationManager = map?.designationManager;
        if (designationManager == null)
            return;

        if (designationManager.AnySpawnedDesignationOfDef(RimgateDefOf.Rimgate_DesignationPushCart))
            DrawDesignatedContainerDestinations(designationManager);

        DrawActiveProxyDestinations();
    }

    private void DrawDesignatedContainerDestinations(DesignationManager designationManager)
    {
        foreach (Designation designation in designationManager.SpawnedDesignationsOfDef(RimgateDefOf.Rimgate_DesignationPushCart))
        {
            if (designation?.target.Thing is not Building_MobileContainer cart)
                continue;

            if (!cart.TryGetPushDestination(out IntVec3 destinationCell))
                continue;

            DrawGhost(destinationCell,
                Utils.RotationFacingFor(cart.Position, destinationCell),
                cart.def,
                cart.Graphic,
                cart);
        }
    }

    private void DrawActiveProxyDestinations()
    {
        List<Pawn> pawns = map?.mapPawns?.FreeColonistsSpawned;
        if (pawns == null || pawns.Count == 0)
            return;

        for (int i = 0; i < pawns.Count; i++)
        {
            Pawn pawn = pawns[i];
            if (pawn?.carryTracker?.CarriedThing is not Thing_MobileCartProxy proxy)
                continue;

            if (!proxy.HasPushDestination || !proxy.PushDestination.InBounds(map))
                continue;

            Graphic ghostGraphic = proxy.GetPushGhostGraphic();
            if (ghostGraphic == null)
                continue;

            DrawGhost(proxy.PushDestination,
                Utils.RotationFacingFor(pawn.Position, proxy.PushDestination),
                proxy.SavedDef,
                ghostGraphic,
                pawn);
        }
    }

    private static void DrawGhost(IntVec3 destinationCell, Rot4 rotation, ThingDef thingDef, Graphic graphic, Thing source)
    {
        if (!destinationCell.IsValid || thingDef == null || graphic == null)
            return;

        GhostDrawer.DrawGhostThing(destinationCell,
            rotation,
            thingDef,
            graphic,
            PushGhostColorValid,
            AltitudeLayer.Blueprint,
            source,
            drawPlaceWorkers: false);
    }
}
