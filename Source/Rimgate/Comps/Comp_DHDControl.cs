using System;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Rimgate;

public class Comp_DHDControl : ThingComp
{
    public PlanetTile LastDialledAddress;

    public CompProperties_DHDControl Props => (CompProperties_DHDControl)props;

    private CompFacility _facilityComp;

    private CompPowerTrader _powerComp;

    private bool _wantGateClosed;

    public bool WantsGateClosed => _wantGateClosed;

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
                "RG_CannotDialNoGate".Translate(),
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
                "RG_CannotDialNoReach".Translate(),
                null);
            yield break;
        }

        if (Props.requiresPower
            && _powerComp != null
            && !_powerComp.PowerOn)
        {
            yield return new FloatMenuOption(
                "RG_CannotDialNoPower".Translate(),
                null);
            yield break;
        }

        Comp_StargateControl stargate = GetLinkedStargate();
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
                    LastDialledAddress = tile;
                    Job job = JobMaker.MakeJob(RimgateDefOf.Rimgate_DialStargate, parent);
                    selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                });
        }
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            yield return gizmo;

        Comp_StargateControl stargate = GetLinkedStargate();
        if (stargate == null)
            yield break;

        Command_Toggle command = new Command_Toggle
        {
            defaultLabel = "RG_CloseStargate".Translate(),
            defaultDesc = "RG_CloseStargateDesc".Translate(),
            icon = RimgateTex.CancelCommandTex,
            isActive = () => _wantGateClosed,
            toggleAction = delegate
            {
                _wantGateClosed = !_wantGateClosed;

                Designation designation = parent.Map.designationManager.DesignationOn(parent, RimgateDefOf.Rimgate_DesignationCloseStargate);

                if (designation == null)
                   parent.Map.designationManager.AddDesignation(new Designation(parent, RimgateDefOf.Rimgate_DesignationCloseStargate));
                else
                    designation?.Delete();
            }
        };

        if (!stargate.IsActive)
            command.Disable("RG_GateIsNotActive".Translate());
        else if (stargate.IsReceivingGate)
            command.Disable("RG_CannotCloseIncoming".Translate());

        yield return command;
    }

    public void DoCloseGate()
    {
        Comp_StargateControl stargate = GetLinkedStargate();
        if (stargate == null)
            return;

        _wantGateClosed = false;
        stargate.CloseStargate(true);
    }

    public Comp_StargateControl GetLinkedStargate()
    {
        if (Props.selfDialler)
            return parent.TryGetComp<Comp_StargateControl>();

        if (_facilityComp == null || _facilityComp.LinkedBuildings.Count == 0)
            return null;

        return _facilityComp.LinkedBuildings[0].TryGetComp<Comp_StargateControl>();
    }
}
