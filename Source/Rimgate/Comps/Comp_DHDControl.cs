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

    public CompFacility Facility 
        => _facilityComp ?? parent.GetComp<CompFacility>();

    private CompFacility _facilityComp;

    public CompPowerTrader PowerTrader
    => !Props.requiresPower 
        ? null
        : _powerComp ?? parent.GetComp<CompPowerTrader>();

    private CompPowerTrader _powerComp;

    private bool _wantGateClosed;

    public bool WantsGateClosed => _wantGateClosed;

    public bool IsConnectedToStargate
    {
        get
        {
            if (Props.selfDialler)
                return true;

            return Facility?.LinkedBuildings.Count > 0;
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

        if (Facility?.LinkedBuildings.Count == 0)
            return null;

        return Facility.LinkedBuildings[0].TryGetComp<Comp_StargateControl>();
    }
}
