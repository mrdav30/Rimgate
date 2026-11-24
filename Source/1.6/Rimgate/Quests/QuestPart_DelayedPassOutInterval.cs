using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Rimgate;

public class QuestPart_DelayedPassOutInterval : QuestPartActivable
{
    public int InitialDelayTicks;

    public IntRange TicksInterval;

    public List<string> OutSignals = new List<string>();

    public List<string> InSignalsDisable = new List<string>();

    private int _currentInterval;

    protected override void Enable(SignalArgs receivedArgs)
    {
        base.Enable(receivedArgs);
        _currentInterval = InitialDelayTicks;
    }

    public override void QuestPartTick()
    {
        if (OutSignals == null || OutSignals.Count == 0) return;

        if (_currentInterval < 0)
        {
            foreach (string outSignal in OutSignals)
                Find.SignalManager.SendSignal(new Signal(outSignal));

            _currentInterval = TicksInterval.RandomInRange;
        }

        _currentInterval--;
    }

    protected override void ProcessQuestSignal(Signal signal)
    {
        if (InSignalsDisable.Contains(signal.tag))
            Disable();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref OutSignals, "OutSignals", LookMode.Value);
        Scribe_Collections.Look(ref InSignalsDisable, "InSignalsDisable", LookMode.Value);
        Scribe_Values.Look(ref _currentInterval, "_currentInterval", 0);
        Scribe_Values.Look(ref InitialDelayTicks, "InitialDelayTicks", 0);
        Scribe_Values.Look(ref TicksInterval, "TicksInterval");
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            OutSignals = OutSignals ?? new List<string>();
            InSignalsDisable = InSignalsDisable ?? new List<string>();
        }
    }

    public override void DoDebugWindowContents(Rect innerRect, ref float curY)
    {
        if (State == QuestPartState.Enabled)
        {
            Rect rect = new Rect(innerRect.x, curY, 500f, 25f);
            if (Widgets.ButtonText(rect, "Reset Interval " + ToString()))
                _currentInterval = 0;

            curY += rect.height + 4f;
        }
    }
}
