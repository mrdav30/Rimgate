using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Rimgate;

public class Comp_ToggleIris : Comp_Toggle
{
    public CompPowerTrader PowerTrader => _cachedPowerTrader ??= parent.GetComp<CompPowerTrader>();

    public CompFacility Facility => _cachedFacility ??= parent.GetComp<CompFacility>();

    public bool Powered => PowerTrader == null || PowerTrader.PowerOn;

    private CompPowerTrader _cachedPowerTrader;

    private CompFacility _cachedFacility;

    public override void CompTick()
    {
        if (!parent.IsHashIntervalTick(200)) return;

        CheckState();
    }

    public override void CompTickRare()
    {
        CheckState();
    }

    public void CheckState()
    {
        if (!TryGetLinkedGate(out Building_Gate linked) || !linked.HasIris) return;

        // Ensure switch state matches iris state
        if (!linked.Powered && linked.IsIrisActivated)
        {
            if (RimgateMod.Debug)
                Log.Message("Rimgate :: Iris deactivated due to lack of power.");

            SwitchIsOn = false;
            _wantToggle = false;
            parent.Map?.designationManager?.RemoveAllDesignationsOfDef(RimgateDefOf.Rimgate_DesignationToggle);
            return;
        }

        if (SwitchIsOn != linked.IsIrisActivated)
        {
            if (RimgateMod.Debug)
                Log.Message($"Rimgate :: Syncing iris state with switch state");

            _switchOnInt = linked.IsIrisActivated;
            _wantToggle = false;
            parent.Map?.designationManager?.RemoveAllDesignationsOfDef(RimgateDefOf.Rimgate_DesignationToggle);
        }
    }

    public override void ReceiveCompSignal(string signal)
    {
        bool? status = signal == Props.onSignal
            ? true
            : signal == Props.offSignal
                ? false
                : null;
        if (status == null || !TryGetLinkedGate(out Building_Gate linked)) return;
        linked.SetIrisActive(status.Value);
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (!TryGetLinkedGate(out Building_Gate linked) || !linked.HasIris) yield break;

        var actionLabel = linked.IsIrisActivated
            ? "RG_OpenIris".Translate()
            : "RG_CloseIris".Translate();

        var toggleIrisCmd = new Command_Toggle
        {
            defaultLabel = "RG_ToggleIris".Translate(actionLabel),
            defaultDesc = "RG_ToggleIrisDesc".Translate(actionLabel, linked.LabelCap),
            icon = _switchOnInt && CommandOffTex != null ? CommandOffTex : CommandOnTex,
            isActive = () => _wantToggle,
            toggleAction = delegate
            {
                _wantToggle = !_wantToggle;

                var dm = parent.Map.designationManager;
                Designation des = dm.DesignationOn(parent, RimgateDefOf.Rimgate_DesignationToggle);

                if (_wantToggle && des == null)
                    dm.AddDesignation(new Designation(parent, RimgateDefOf.Rimgate_DesignationToggle));
                else
                    des?.Delete();
            }
        };

        if (!linked.Powered || !Powered)
            toggleIrisCmd.Disable("PowerNotConnected".Translate());

        yield return toggleIrisCmd;
    }

    private bool TryGetLinkedGate(out Building_Gate gate)
    {
        gate = null;
        if (parent is Building_Gate)
        {
            gate = (Building_Gate)parent;
            return true;
        }

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
}
