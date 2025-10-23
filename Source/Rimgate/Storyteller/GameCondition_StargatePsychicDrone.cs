using RimWorld;
using Verse;
using System.Linq;
using Verse.Noise;

namespace Rimgate;

public class GameCondition_StargatePsychicDrone : GameCondition_PsychicEmanation
{
    private Comp_StargateControl _heldComp;

    public override void Init()
    {
        base.Init();
        // keep receiver open during the drone
        TryHoldLocalGate(SingleMap);
    }

    public override void End()
    {
        ReleaseHold();
        base.End();
    }

    public override void GameConditionTick()
    {
        base.GameConditionTick();
        if (_heldComp == null || _heldComp.parent?.Destroyed == true)
            End();
        if (_heldComp != null) 
            _heldComp.TicksSinceBufferUnloaded = 0;
    }

    private void TryHoldLocalGate(Map map)
    {
        if (map == null) return;
        var gate = map.listerThings.ThingsOfDef(RimgateDefOf.Rimgate_Stargate)
                   .OfType<Building_Stargate>()
                   .FirstOrDefault();
        if (gate == null) return;

        var comp = gate.GateControl;
        if (comp == null) return;

        _heldComp = comp;
        _heldComp.PushExternalHold();
        // If the gate isn’t already open,
        // light it up as an “incoming” link
        _heldComp.ForceLocalOpenAsReceiver();
    }

    private void ReleaseHold()
    {
        if (_heldComp == null) return;

        _heldComp.PopExternalHold();
        // If nothing else is holding the gate
        // and it was an “incoming” fake link:
        if (_heldComp.ExternalHoldCount == 0 && _heldComp.IsReceivingGate)
            _heldComp.CloseStargate();

        _heldComp = null;
    }
}