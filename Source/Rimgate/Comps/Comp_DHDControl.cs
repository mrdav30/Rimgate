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

    public CompPowerTrader PowerTrader
    => !Props.requiresPower
        ? null
        : _powerComp ?? parent.GetComp<CompPowerTrader>();

    public Graphic ActiveGraphic => _activeGraphic ??= Props.activeGraphicData.Graphic;

    public bool WantsIrisToggled => _wantIrisToggled;

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

    private Graphic _activeGraphic;

    private CompFacility _facilityComp;

    private CompPowerTrader _powerComp;

    private bool _wantGateClosed;

    private bool _wantIrisToggled;

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            yield return gizmo;

        Comp_StargateControl stargate = GetLinkedStargate();
        if (stargate == null)
            yield break;

        Command_Toggle closeGateCmd = new Command_Toggle
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
            closeGateCmd.Disable("RG_GateIsNotActive".Translate());
        else if (stargate.IsReceivingGate)
            closeGateCmd.Disable("RG_CannotCloseIncoming".Translate());
        else if (Props.requiresPower && !(PowerTrader?.PowerOn ?? false))
            closeGateCmd.Disable("PowerNotConnected".Translate());

        yield return closeGateCmd;

        if (!Props.canToggleIris || !stargate.HasIris)
            yield break;

        var actionLabel = stargate.IsIrisActivated
            ? "RG_OpenIris".Translate()
            : "RG_CloseIris".Translate();

        var toggleIrisCmd = new Command_Toggle
        {
            defaultLabel = "RG_ToggleIris".Translate(actionLabel),
            defaultDesc = "RG_ToggleIrisDesc".Translate(actionLabel),
            icon = stargate.ToggleIrisIcon,
            isActive = () => _wantIrisToggled,
            toggleAction = delegate
            {
                _wantIrisToggled = !_wantIrisToggled;

                var dm = parent.Map.designationManager;
                var des = dm.DesignationOn(parent, RimgateDefOf.Rimgate_DesignationToggleIris);
                if (des == null)
                    dm.AddDesignation(new Designation(parent, RimgateDefOf.Rimgate_DesignationToggleIris));
                else
                    des.Delete();
            }
        };

        if (!stargate.HasPower 
            || (Props.requiresPower && !(PowerTrader?.PowerOn ?? false)))
        {
            toggleIrisCmd.Disable("PowerNotConnected".Translate());
        }

        yield return toggleIrisCmd;
    }

    public override void PostDraw()
    {
        base.PostDraw();

        Comp_StargateControl stargate = GetLinkedStargate();
        if (stargate == null || !stargate.IsActive)
            return;

        if (Props.requiresPower && !(PowerTrader?.PowerOn ?? false))
            return;

        var rot = parent.Rotation;
        var drawOffset = parent.def.graphicData.DrawOffsetForRot(rot);

        var posAbove = parent.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.BuildingOnTop) + drawOffset;

        // highlight floats above the dhd.
        ActiveGraphic.Draw(Utils.AddY(posAbove, +0.01f), rot, parent);
    }

    public void DoCloseGate()
    {
        Comp_StargateControl stargate = GetLinkedStargate();
        if (stargate == null)
            return;

        _wantGateClosed = false;
        stargate.CloseStargate(true);
    }

    public void DoToggleIrisRemote()
    {
        var stargate = GetLinkedStargate();
        if (stargate == null) return;

        _wantIrisToggled = false;
        stargate.DoToggleIris();
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
