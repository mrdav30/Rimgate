using RimWorld;
using Verse;
using System.Linq;
using Verse.Noise;

namespace Rimgate;

public class GameCondition_StargateToxicFallout : GameCondition_ToxicFallout
{
    private Comp_StargateControl _heldComp;

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

        _heldComp = gate.StargateControl;
        if (_heldComp == null) return;

        _heldComp.PushExternalHold();
        _heldComp.ForceLocalOpenAsReceiver();
    }

    private void ReleaseHold()
    {
        if (_heldComp == null) return;
        _heldComp.PopExternalHold();
        if (_heldComp.ExternalHoldCount == 0 && _heldComp.IsReceivingGate)
            _heldComp.CloseStargate(closeOtherGate: false);
        _heldComp = null;
    }
}
