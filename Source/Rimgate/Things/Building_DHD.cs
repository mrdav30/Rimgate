using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Noise;
using static HarmonyLib.Code;

namespace Rimgate;

public class Building_DHD_Ext : DefModExtension
{
    public bool canToggleIris;

    public bool canFastDial;

    public GraphicData activeGraphicData;
}

public class Building_DHD : Building
{
    public PlanetTile LastDialledAddress;

    public Building_DHD_Ext Props => _cachedProps ??= def.GetModExtension<Building_DHD_Ext>();

    public CompFacility Facility => _cachedFacility ??= GetComp<CompFacility>();

    public CompPowerTrader PowerTrader => _cachedPowerTrader ??= GetComp<CompPowerTrader>();

    public Building_Gate LinkedGate
    {
        get
        {
            if (_cachedGateControl == null)
                TryGetLinkedGate(out _cachedGateControl);
            return _cachedGateControl;
        }
    }

    public bool WantsIrisToggled => _wantIrisToggled;

    public bool WantsGateClosed => _wantGateClosed;

    public bool Powered => PowerTrader == null || PowerTrader.PowerOn;

    public Graphic ActiveGraphic => Props.activeGraphicData?.Graphic;

    private bool _hadActiveModificationEquipment;

    private CompPowerTrader _cachedPowerTrader;

    private Building_DHD_Ext _cachedProps;

    private CompFacility _cachedFacility;

    private bool _wantGateClosed;

    private bool _wantIrisToggled;

    private Building_Gate _cachedGateControl;

    public override void TickRare()
    {
        if (!Spawned || this.IsMinified())
            return;

        base.TickRare();

        var linked = Facility?.LinkedBuildings;
        if (!linked.NullOrEmpty())
            foreach (var t in linked)
            {
                if (t is null) continue;
                if (t.def != RimgateDefOf.Rimgate_WraithModificationEquipment) continue;
                var power = t.TryGetComp<CompPowerTrader>();
                _hadActiveModificationEquipment = power?.PowerOn == true;
                GateUtil.SetModificationEquipmentActive(_hadActiveModificationEquipment);
                return;
            }

        if (_hadActiveModificationEquipment)
        {
            GateUtil.SetModificationEquipmentActive(false);
            _hadActiveModificationEquipment = false;
        }

    }

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        base.DrawAt(drawLoc, flip);

        var active = ActiveGraphic;
        if (active == null)
            return;

        var control = LinkedGate;
        if (control == null)
            return;

        if (!control.IsActive || !Powered) return;

        var rot = Rotation;
        var drawOffset = def.graphicData.DrawOffsetForRot(rot);

        var posAbove = Position.ToVector3ShiftedWithAltitude(AltitudeLayer.BuildingOnTop) + drawOffset;

        // highlight floats above the dhd.
        active.Draw(Utils.AddY(posAbove, +0.01f), rot, this);
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos())
            yield return gizmo;

        var linked = LinkedGate;
        if (linked == null)
            yield break;

        Command_Toggle closeGateCmd = new Command_Toggle
        {
            defaultLabel = "RG_CloseGate".Translate(linked.LabelCap),
            defaultDesc = "RG_CloseGateDesc".Translate(linked.LabelCap),
            icon = RimgateTex.CancelCommandTex,
            isActive = () => _wantGateClosed,
            toggleAction = delegate
            {
                _wantGateClosed = !_wantGateClosed;

                Designation designation = Map.designationManager.DesignationOn(this, RimgateDefOf.Rimgate_DesignationCloseGate);

                if (designation == null)
                    Map.designationManager.AddDesignation(new Designation(this, RimgateDefOf.Rimgate_DesignationCloseGate));
                else
                    designation?.Delete();
            }
        };

        if (!linked.IsActive)
            closeGateCmd.Disable("RG_GateIsNotActive".Translate(linked.LabelCap));
        else if (linked.IsReceivingGate)
            closeGateCmd.Disable("RG_CannotCloseIncoming".Translate());
        else if (!Powered)
            closeGateCmd.Disable("PowerNotConnected".Translate());

        yield return closeGateCmd;

        if (Props.canToggleIris && linked.HasIris)
        {
            var actionLabel = linked.IsIrisActivated
                ? "RG_OpenIris".Translate()
                : "RG_CloseIris".Translate();

            var toggleIrisCmd = new Command_Toggle
            {
                defaultLabel = "RG_ToggleIris".Translate(actionLabel),
                defaultDesc = "RG_ToggleIrisDesc".Translate(actionLabel, linked.LabelCap),
                icon = linked.ToggleIrisIcon,
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

            if (!linked.Powered || !Powered)
                toggleIrisCmd.Disable("PowerNotConnected".Translate());

            yield return toggleIrisCmd;
        }

        var options = new List<FloatMenuOption>();
        var addressList = GateUtil.GetAddressList(Tile);
        foreach (var address in addressList)
        {
            MapParent gwo = Find.WorldObjects.MapParentAt(address);
            if (gwo == null || gwo is WorldObject_GateTransitSite) // transit sites can only be abandoned via their own UI
                continue;

            string designation = GateUtil.GetGateDesignation(address);
            string label = $"{designation} ({gwo.Label})";

            if (gwo.Map?.mapPawns?.AnyPawnBlockingMapRemoval == true)
            {
                string reason = "RG_AbandonExplorationDisabled".Translate();
                options.Add(new FloatMenuOption(label + ": " + reason, null));
                continue;
            }

            Action action = delegate
            {
                if (gwo is WorldObject_GateQuestSite wso)
                    wso.Destroy();
                else
                    GateUtil.RemoveGateAddress(address);

                Messages.Message("RG_AbandonExplorationSuccess".Translate(designation), MessageTypeDefOf.PositiveEvent);
            };

            options.Add(new FloatMenuOption(label, action));
        }

        Command_Action abandonExploration = new Command_Action
        {
            defaultLabel = "RG_AbandonExplorationLabel".Translate(),
            defaultDesc = "RG_AbandonExplorationDesc".Translate(),
            icon = RimgateTex.AbandonExploration,
            action = () => Find.WindowStack.Add(new FloatMenu(options)),
            activateSound = SoundDefOf.Tick_Tiny
        };

        if (options.Count <= 0)
            abandonExploration.Disable("RG_CannotDialNoDestinations".Translate());

        yield return abandonExploration;
    }

    public override string GetInspectString()
    {
        if (this.IsMinified())
            return null;

        StringBuilder sb = new StringBuilder(base.GetInspectString());
        if (sb.Length > 0)
            sb.AppendLine();

        // Address list and active quest counters, minus 1 to exclude the current gate's own address.
        var count = GateUtil.GetAddressList(Tile).Count;
        sb.AppendLine("RG_DHD_AddressListCounter".Translate(count, GateUtil.MaxAddresses - 1));
        sb.AppendLine("RG_DHD_ActiveQuestCounter".Translate(GateUtil.ActiveQuestSiteCount, GateUtil.MaxActiveQuestSiteCount));

        return sb.ToString().TrimEndNewlines();
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        if (_hadActiveModificationEquipment)
            GateUtil.SetModificationEquipmentActive(false);
        base.DeSpawn(mode);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref _hadActiveModificationEquipment, "hadActiveModificationEquipment", false);
        Scribe_Values.Look(ref LastDialledAddress, "lastDialledAddress");
    }

    private bool TryGetLinkedGate(out Building_Gate gate)
    {
        gate = null;

        var linked = Facility?.LinkedBuildings;
        if (linked == null || linked.Count <= 0)
            return false;

        foreach (var t in linked)
        {
            if (t is null) continue;
            if (t is not Building_Gate found) continue;
            gate = found;
            return true;
        }

        return false;
    }

    public void DoCloseGate()
    {
        var control = LinkedGate;
        if (control == null)
            return;

        _wantGateClosed = false;
        control.CloseGate(true);
    }

    public void DoToggleIrisRemote()
    {
        var control = LinkedGate;
        if (control == null)
            return;

        _wantIrisToggled = false;
        control.DoToggleIris();
    }

    public static bool TryGetDhdOnMap(Map map, out Building_DHD dhd, ThingDef def = null)
    {
        dhd = null;
        foreach (Thing thing in map.listerThings.AllThings)
        {
            if (thing is Building_DHD bdhd
                && (def == null || bdhd.def == def))
            {
                dhd = bdhd;
                break;
            }
        }

        return dhd != null;
    }
}
