using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;
using static HarmonyLib.Code;

namespace Rimgate;

public class Building_DHD : Building
{
    public PlanetTile LastDialledAddress;

    public Comp_DHDControl DHDControl => _cachedDialHomeDevice ??= GetComp<Comp_DHDControl>();

    public CompFacility Facility => _cachedFacility ??= GetComp<CompFacility>();

    public CompPowerTrader PowerTrader => _cachedPowerTrader ??= GetComp<CompPowerTrader>();

    public Comp_StargateControl StargateControl
    {
        get
        {
            if (_cachedStargateControl == null)
                TryGetLinkedStargate(out _cachedStargateControl);
            return _cachedStargateControl;
        }
    }

    public bool WantsIrisToggled => _wantIrisToggled;

    public bool WantsGateClosed => _wantGateClosed;

    public bool Powered => PowerTrader == null || PowerTrader.PowerOn;

    private bool _hadActiveModificationEquipment;

    private CompPowerTrader _cachedPowerTrader;

    private Comp_DHDControl _cachedDialHomeDevice;

    private CompFacility _cachedFacility;

    private bool _wantGateClosed;

    private bool _wantIrisToggled;

    private Comp_StargateControl _cachedStargateControl;

    public override void TickRare()
    {
        base.TickRare();

        var linked = Facility?.LinkedBuildings;
        if (!linked.NullOrEmpty())
            foreach (var t in linked)
            {
                if (t is null) continue;
                if (t.def != RimgateDefOf.Rimgate_WraithModificationEquipment) continue;
                var power = t.TryGetComp<CompPowerTrader>();
                _hadActiveModificationEquipment = power?.PowerOn == true;
                StargateUtil.SetModificationEquipmentActive(_hadActiveModificationEquipment);
                return;
            }

        if (_hadActiveModificationEquipment)
        {
            StargateUtil.SetModificationEquipmentActive(false);
            _hadActiveModificationEquipment = false;
        }

    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        if (_hadActiveModificationEquipment)
            StargateUtil.SetModificationEquipmentActive(false);
        base.DeSpawn(mode);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref _hadActiveModificationEquipment, "hadActiveModificationEquipment", false);
        Scribe_Values.Look(ref LastDialledAddress, "lastDialledAddress");
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos())
            yield return gizmo;

        var control = StargateControl;
        if (control == null)
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

                Designation designation = Map.designationManager.DesignationOn(this, RimgateDefOf.Rimgate_DesignationCloseStargate);

                if (designation == null)
                    Map.designationManager.AddDesignation(new Designation(this, RimgateDefOf.Rimgate_DesignationCloseStargate));
                else
                    designation?.Delete();
            }
        };

        if (!control.IsActive)
            closeGateCmd.Disable("RG_GateIsNotActive".Translate());
        else if (control.IsReceivingGate)
            closeGateCmd.Disable("RG_CannotCloseIncoming".Translate());
        else if (!Powered)
            closeGateCmd.Disable("PowerNotConnected".Translate());

        yield return closeGateCmd;

        if (!DHDControl.Props.canToggleIris || !control.HasIris)
            yield break;

        var actionLabel = control.IsIrisActivated
            ? "RG_OpenIris".Translate()
            : "RG_CloseIris".Translate();

        var toggleIrisCmd = new Command_Toggle
        {
            defaultLabel = "RG_ToggleIris".Translate(actionLabel),
            defaultDesc = "RG_ToggleIrisDesc".Translate(actionLabel),
            icon = control.ToggleIrisIcon,
            isActive = () => _wantIrisToggled,
            toggleAction = delegate
            {
                _wantIrisToggled = !_wantIrisToggled;

                var dm = Map.designationManager;
                var des = dm.DesignationOn(this, RimgateDefOf.Rimgate_DesignationToggleIris);
                if (des == null)
                    dm.AddDesignation(new Designation(this, RimgateDefOf.Rimgate_DesignationToggleIris));
                else
                    des.Delete();
            }
        };

        if (!control.HasPower || !Powered)
            toggleIrisCmd.Disable("PowerNotConnected".Translate());

        yield return toggleIrisCmd;
    }

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        base.DrawAt(drawLoc, flip);

        var control = StargateControl;
        if (control == null)
            return;

        if (!control.IsActive || !Powered) return;

        var rot = Rotation;
        var drawOffset = def.graphicData.DrawOffsetForRot(rot);

        var posAbove = Position.ToVector3ShiftedWithAltitude(AltitudeLayer.BuildingOnTop) + drawOffset;

        // highlight floats above the dhd.
        DHDControl.ActiveGraphic.Draw(Utils.AddY(posAbove, +0.01f), rot, this);
    }

    private bool TryGetLinkedStargate(out Comp_StargateControl gate)
    {
        gate = null;

        var linked = Facility?.LinkedBuildings;
        if (linked == null || linked.Count <= 0)
            return false;

        foreach (var t in linked)
        {
            if (t is null) continue;
            if (t.def != RimgateDefOf.Rimgate_Stargate) continue;
            gate = t.TryGetComp<Comp_StargateControl>();
            return gate != null;
        }

        return false;
    }

    public void DoCloseGate()
    {
        var control = StargateControl;
        if (control == null)
            return;

        _wantGateClosed = false;
        control.CloseStargate(true);
    }

    public void DoToggleIrisRemote()
    {
        var control = StargateControl;
        if (control == null)
            return;

        _wantIrisToggled = false;
        control.DoToggleIris();
    }

    public static Building_DHD GetDhdOnMap(Map map)
    {
        Building_DHD dhdOnMap = null;
        foreach (Thing thing in map.listerThings.AllThings)
        {
            if (thing is Building_DHD bdhd)
            {
                dhdOnMap = bdhd;
                break;
            }
        }

        return dhdOnMap;
    }

    public static Building_DHD GetDhdOfOnMap(Map map, ThingDef def)
    {
        Building_DHD dhdOnMap = null;
        foreach (Thing thing in map.listerThings.AllThings)
        {
            if (thing is Building_DHD bdhd
                && bdhd.def == def)
            {
                dhdOnMap = bdhd;
                break;
            }
        }

        return dhdOnMap;
    }
}
