using RimWorld;
using Verse;
using System.Linq;

namespace Rimgate;

public abstract class GameCondition_Gate : GameCondition
{
    private Building_Gate _heldGate;

    private bool _restoreLegacyHold;

    public override void ExposeData()
    {
        base.ExposeData();
        if (Scribe.mode == LoadSaveMode.LoadingVars)
            _restoreLegacyHold = Scribe.loader.curXmlParent["heldGate"] == null;
        if (Scribe.mode == LoadSaveMode.PostLoadInit && _restoreLegacyHold)
            _restoreLegacyHold = SingleMap?.GameConditionManager.ActiveConditions.OfType<GameCondition_Gate>().Count() == 1;
        if (Scribe.mode == LoadSaveMode.Saving)
            TryRestoreLegacyHold();
        Scribe_References.Look(ref _heldGate, "heldGate");
    }

    public override void Init()
    {
        base.Init();
        TryHoldLocalGate(SingleMap);
    }

    public override void End()
    {
        TryRestoreLegacyHold();
        ReleaseHold();
        base.End();
    }

    public override void GameConditionTick()
    {
        TryRestoreLegacyHold();
        base.GameConditionTick();
        if (_heldGate == null || _heldGate.Destroyed == true)
            End();
        if (_heldGate != null)
            _heldGate.TicksSinceBufferUnloaded = 0;
    }

    private void TryRestoreLegacyHold()
    {
        // Old saves omitted the reference. Adopt only one unambiguous existing local hold, never push another.
        if (_restoreLegacyHold)
        {
            _restoreLegacyHold = false;
            var map = SingleMap;
            if (_heldGate == null && map != null
                && map.GameConditionManager.ActiveConditions.OfType<GameCondition_Gate>().Count() == 1
                && Building_Gate.TryGetSpawnedGateOnMap(map, out var gate)
                && gate.IsActive && gate.IsReceivingGate && gate.ExternalHoldCount == 1
                && gate.ConnectedGate == null && !gate.ConnectedAddress.Valid)
                _heldGate = gate;
        }

    }

    private void TryHoldLocalGate(Map map)
    {
        if (map == null) return;
        if (!Building_Gate.TryGetSpawnedGateOnMap(map, out Building_Gate gate) || gate.IsActive) return;
        _heldGate = gate;

        _heldGate.PushExternalHold();
        _heldGate.ForceLocalOpenAsReceiver();
    }

    private void ReleaseHold()
    {
        if (_heldGate == null) return;
        _heldGate.PopExternalHold();
        if (_heldGate.ExternalHoldCount == 0 && _heldGate.IsReceivingGate)
            _heldGate.CloseGate();
        _heldGate = null;
    }
}
