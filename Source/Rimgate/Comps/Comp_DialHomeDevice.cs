using System;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Rimgate;

public class Comp_DialHomeDevice : ThingComp
{
    public PlanetTile LastDialledAddress;

    public CompProperties_DialHomeDevice Props => (CompProperties_DialHomeDevice)props;

    private CompFacility _facilityComp;

    private CompPowerTrader _powerComp;

    public bool IsConnectedToStargate
    {
        get
        {
            if (Props.selfDialler)
                return true;

            return _facilityComp != null
                && _facilityComp.LinkedBuildings.Count > 0;
        }
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        _facilityComp = parent.GetComp<CompFacility>();

        if (Props.requiresPower)
            _powerComp = parent.TryGetComp<CompPowerTrader>();
    }

    public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
    {
        if (!IsConnectedToStargate)
        {
            yield return new FloatMenuOption(
                "Rimgate_CannotDialNoGate".Translate(),
                null);
            yield break;
        }

        bool canReach = selPawn.CanReach(
            parent.InteractionCell,
            PathEndMode.Touch,
            Danger.Deadly,
            false,
            false,
            TraverseMode.ByPawn);
        if (!canReach)
        {
            yield return new FloatMenuOption(
                "Rimgate_CannotDialNoReach".Translate(),
                null);
            yield break;
        }

        if (Props.requiresPower
            && _powerComp != null
            && !_powerComp.PowerOn)
        {
            yield return new FloatMenuOption(
                "Rimgate_CannotDialNoPower".Translate(),
                null);
            yield break;
        }

        Comp_Stargate stargate = GetLinkedStargate();

        if (stargate == null)
        {
            yield return new FloatMenuOption(
                "Rimgate_CannotDialNoGate".Translate(),
                null);
            yield break;
        }

        if (stargate.StargateIsActive)
        {
            yield return new FloatMenuOption(
                "Rimgate_CannotDialGateIsActive".Translate(),
                null);

            yield break;
        }

        WorldComp_StargateAddresses addressComp = Find.World.GetComponent<WorldComp_StargateAddresses>();

        addressComp.CleanupAddresses();
        if (addressComp.AddressCount < 2) // home + another site
        {
            yield return new FloatMenuOption(
                "Rimgate_CannotDialNoDestinations".Translate(),
                null);
            yield break;
        }

        if (stargate.TicksUntilOpen > -1)
        {
            yield return new FloatMenuOption(
                "Rimgate_CannotDialIncoming".Translate(),
                null);
            yield break;
        }

        foreach (PlanetTile tile in addressComp.AddressList)
        {
            if (tile == stargate.GateAddress)
                continue;

            MapParent sgMap = Find.WorldObjects.MapParentAt(tile);
            string designation = Comp_Stargate.GetStargateDesignation(tile);

            yield return new FloatMenuOption(
                "Rimgate_DialGate".Translate(designation, sgMap.Label),
                () => {
                    LastDialledAddress = tile;
                    Job job = JobMaker.MakeJob(Rimgate_DefOf.Rimgate_DialStargate, parent);
                    selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                });
        }
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            yield return gizmo;

        Comp_Stargate stargate = GetLinkedStargate();
        if (stargate == null)
            yield break;

        Command_Action command = new Command_Action
        {
            defaultLabel = "Rimgate_CloseStargate".Translate(),
            defaultDesc = "Rimgate_CloseStargateDesc".Translate(),
            icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true)
        };
        command.action = delegate ()
        {
            stargate.CloseStargate(true);
        };
        if (!stargate.StargateIsActive)
            command.Disable("Rimgate_GateIsNotActive".Translate());
        else if (stargate.IsReceivingGate)
            command.Disable("Rimgate_CannotCloseIncoming".Translate());

        yield return command;
    }
    public Comp_Stargate GetLinkedStargate()
    {
        if (Props.selfDialler)
            return parent.TryGetComp<Comp_Stargate>();

        if (_facilityComp == null || _facilityComp.LinkedBuildings.Count == 0)
            return null;

        return _facilityComp.LinkedBuildings[0].TryGetComp<Comp_Stargate>();
    }

    public static Thing GetDHDOnMap(Map map)
    {
        Thing dhdOnMap = null;
        foreach (Thing thing in map.listerThings.AllThings)
        {
            if (thing.TryGetComp<Comp_DialHomeDevice>() != null
                && thing.def.thingClass != typeof(Building_Stargate))
            {
                dhdOnMap = thing;
                break;
            }
        }

        return dhdOnMap;
    }
}
