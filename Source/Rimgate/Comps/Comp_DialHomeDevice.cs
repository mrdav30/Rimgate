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
    CompFacility compFacility;
    public int lastDialledAddress;

    public CompProperties_DialHomeDevice Props => (CompProperties_DialHomeDevice)this.props;

    public Comp_Stargate GetLinkedStargate()
    {
        if (Props.selfDialler)
            return this.parent.TryGetComp<Comp_Stargate>();

        if (compFacility.LinkedBuildings.Count == 0)
            return null;

        return compFacility.LinkedBuildings[0].TryGetComp<Comp_Stargate>();
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

            if (compFacility.LinkedBuildings.Count == 0)
                return false;

            return true;
        }
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        this.compFacility = this.parent.GetComp<CompFacility>();
    }

    public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
    {
        if (!isConnectedToStargate)
            yield break;

        bool canReach = selPawn.CanReach(
            this.parent.InteractionCell,
            PathEndMode.Touch,
            Danger.Deadly,
            false,
            false,
            TraverseMode.ByPawn);
        if (!canReach)
            yield break;

        if (Props.requiresPower)
        {
            CompPowerTrader compPowerTrader = this.parent.TryGetComp<CompPowerTrader>();
            if (compPowerTrader != null && !compPowerTrader.PowerOn)
            {
                yield return new FloatMenuOption("Rimgate_CannotDialNoPower".Translate(), null);
                yield break;
            }
        }

        Comp_Stargate stargate = GetLinkedStargate();
        if (stargate == null)
            yield break;

        if (stargate.stargateIsActive)
        {
            yield return new FloatMenuOption(
                "Rimgate_CannotDialGateIsActive".Translate(),
                null);

            yield break;
        }

        WorldComp_StargateAddresses addressComp = Find.World.GetComponent<WorldComp_StargateAddresses>();
        addressComp.CleanupAddresses();
        if (addressComp.addressList.Count < 2)
        {
            yield return new FloatMenuOption("Rimgate_CannotDialNoDestinations".Translate(), null);
            yield break;
        }

        if (stargate.ticksUntilOpen > -1)
        {
            yield return new FloatMenuOption("Rimgate_CannotDialIncoming".Translate(), null);
            yield break;
        }

        foreach (int i in addressComp.addressList)
        {
            if (i != stargate.gateAddress)
            {
                MapParent sgMap = Find.WorldObjects.MapParentAt(i);
                yield return new FloatMenuOption(
                    "DialGate".Translate(Comp_Stargate.GetStargateDesignation(i), sgMap.Label),
                    () =>
                        {
                            lastDialledAddress = i;
                            Job job = JobMaker.MakeJob(
                                DefDatabase<JobDef>.GetNamed("Rimgate_DialStargate"),
                                this.parent);
                            selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                        });
            }
        }
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            yield return gizmo;

        Comp_Stargate stargate = this.GetLinkedStargate();
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
        if (!stargate.stargateIsActive) 
            command.Disable("Rimgate_GateIsNotActive".Translate());
        else if (stargate.isRecievingGate)
            command.Disable("Rimgate_CannotCloseIncoming".Translate());

        yield return command;
    }
}
