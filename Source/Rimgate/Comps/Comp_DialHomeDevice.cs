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

    private CompFacility _compFacility;

    public Comp_Stargate GetLinkedStargate()
    {
        if (Props.selfDialler)
            return parent.TryGetComp<Comp_Stargate>();

        if (_compFacility.LinkedBuildings.Count == 0)
            return null;

        return _compFacility.LinkedBuildings[0].TryGetComp<Comp_Stargate>();
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

    private bool isConnectedToStargate
    {
        get
        {
            if (Props.selfDialler)
                return true;

            if (_compFacility.LinkedBuildings.Count == 0)
                return false;

            return true;
        }
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        _compFacility = parent.GetComp<CompFacility>();
    }

    public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
    {
        if (!isConnectedToStargate)
            yield break;

        bool canReach = selPawn.CanReach(
            parent.InteractionCell,
            PathEndMode.Touch,
            Danger.Deadly,
            false,
            false,
            TraverseMode.ByPawn);
        if (!canReach)
            yield break;

        if (Props.requiresPower)
        {
            CompPowerTrader compPowerTrader = parent.TryGetComp<CompPowerTrader>();
            if (compPowerTrader != null && !compPowerTrader.PowerOn)
            {
                yield return new FloatMenuOption("Rimgate_CannotDialNoPower".Translate(), null);
                yield break;
            }
        }

        Comp_Stargate stargate = GetLinkedStargate();
        if (stargate == null)
            yield break;

        if (stargate.StargateIsActive)
        {
            yield return new FloatMenuOption(
                "Rimgate_CannotDialGateIsActive".Translate(),
                null);

            yield break;
        }

        WorldComp_StargateAddresses addressComp = Find.World.GetComponent<WorldComp_StargateAddresses>();
        addressComp.CleanupAddresses();
        if (addressComp.AddressList.Count < 2)
        {
            yield return new FloatMenuOption("Rimgate_CannotDialNoDestinations".Translate(), null);
            yield break;
        }

        if (stargate.TicksUntilOpen > -1)
        {
            yield return new FloatMenuOption("Rimgate_CannotDialIncoming".Translate(), null);
            yield break;
        }

        foreach (int i in addressComp.AddressList)
        {
            if (i != stargate.GateAddress)
            {
                MapParent sgMap = Find.WorldObjects.MapParentAt(i);
                yield return new FloatMenuOption(
                    "DialGate".Translate(Comp_Stargate.GetStargateDesignation(i), sgMap.Label),
                    () =>
                        {
                            LastDialledAddress = i;
                            Job job = JobMaker.MakeJob(
                                DefDatabase<JobDef>.GetNamed("Rimgate_DialStargate"),
                                parent);
                            selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                        });
            }
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
        else if (stargate.IsRecievingGate)
            command.Disable("Rimgate_CannotCloseIncoming".Translate());

        yield return command;
    }
}
