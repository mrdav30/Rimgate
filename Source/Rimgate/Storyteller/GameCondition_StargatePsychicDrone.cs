using RimWorld;
using Verse;
using System.Linq;
using Verse.Noise;

namespace Rimgate;

public class GameCondition_StargatePsychicDrone : GameCondition_PsychicEmanation
{
    private Building_Stargate _heldGate;

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
        if (_heldGate == null || _heldGate.Destroyed == true)
            End();
        if (_heldGate != null)
            _heldGate.TicksSinceBufferUnloaded = 0;
    }

    private void TryHoldLocalGate(Map map)
    {
        if (map == null) return;
        var gate = map.listerThings.ThingsOfDef(RimgateDefOf.Rimgate_Dwarfgate)
                   .OfType<Building_Stargate>()
                   .FirstOrDefault();

        if (gate == null) return;
        _heldGate = gate;

        _heldGate.PushExternalHold();
        // If the gate isn’t already open,
        // light it up as an “incoming” link
        _heldGate.ForceLocalOpenAsReceiver();
    }

    private void ReleaseHold()
    {
        if (_heldGate == null) return;

        _heldGate.PopExternalHold();
        // If nothing else is holding the gate
        // and it was an “incoming” fake link:
        if (_heldGate.ExternalHoldCount == 0 && _heldGate.IsReceivingGate)
            _heldGate.CloseStargate();

        _heldGate = null;
    }
}