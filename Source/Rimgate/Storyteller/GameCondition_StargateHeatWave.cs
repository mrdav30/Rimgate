using RimWorld;
using Verse;
using System.Linq;
using Verse.Noise;

namespace Rimgate;

public class GameCondition_StargateHeatWave : GameCondition_HeatWave
{
    private Building_Stargate _heldGate;

    public override void Init()
    {
        base.Init();
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
        var gate = Building_Stargate.GetStargateOnMap(map);
        if (gate == null) return;
        _heldGate = gate;

        _heldGate.PushExternalHold();
        _heldGate.ForceLocalOpenAsReceiver();
    }

    private void ReleaseHold()
    {
        if (_heldGate == null) return;
        _heldGate.PopExternalHold();
        if (_heldGate.ExternalHoldCount == 0 && _heldGate.IsReceivingGate)
            _heldGate.CloseStargate();
        _heldGate = null;
    }
}