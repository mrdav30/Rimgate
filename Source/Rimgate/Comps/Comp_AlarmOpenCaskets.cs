using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Rimgate;

public class Comp_AlarmOpenCaskets : ThingComp, IThingGlower
{
    public const float MaxDistActivationByOther = 40f;

    public const string TriggerOpenAction = "RimgateTriggerOpenAction";

    public const string CompletedOpenAction = "RimgateCompletedOpenAction";

    public CompProperties_ProximityAlarm Props => (CompProperties_ProximityAlarm)props;

    public bool Sent => _sent;

    public bool Enabled => _ticksUntilEnabled <= 0;

    private int _ticksUntilEnabled;

    private bool _sent;

    private bool _resolved;

    public override void Initialize(CompProperties props)
    {
        base.Initialize(props);
        _ticksUntilEnabled = Props.enableAfterTicks;
    }

    public override void CompTick()
    {
        base.CompTick();
        if (!_sent && parent.Spawned)
        {
            if (!_resolved)
                Resolve();

            if (Enabled && Find.TickManager.TicksGame % 250 == 0)
                CompTickRare();

            if (_ticksUntilEnabled > 0)
                _ticksUntilEnabled--;
        }
    }

    public override void CompTickRare()
    {
        base.CompTickRare();
        Predicate<Thing> predicate = null;
        if (Props.onlyHumanlike)
            predicate = (Thing t) => t is Pawn pawn && pawn.RaceProps.Humanlike && pawn.Faction == Faction.OfPlayer;

        Thing thing = null;
        if (Props.triggerOnPawnInRoom)
        {
            foreach (Thing containedAndAdjacentThing in parent.GetRoom().ContainedAndAdjacentThings)
            {
                if (predicate(containedAndAdjacentThing))
                {
                    thing = containedAndAdjacentThing;
                    break;
                }
            }
        }

        if (thing == null && Props.radius > 0f)
        {
            thing = GenClosest.ClosestThingReachable(
                parent.Position,
                parent.Map,
                ThingRequest.ForGroup(ThingRequestGroup.Pawn),
                PathEndMode.OnCell,
                TraverseParms.For(TraverseMode.NoPassClosedDoors),
                Props.radius,
                predicate);
        }

        if (thing != null)
            Trigger(thing);
    }

    public void Resolve()
    {
        List<Thing> caskets = new();
        Room room = parent.GetRoom();
        foreach (Thing thing in room.ContainedAndAdjacentThings)
        {
            if (thing is Building_CryptosleepCasket)
                caskets.Add(thing);
        }

        SignalAction_OpenCasket signalAction_OpenCasket = (SignalAction_OpenCasket)ThingMaker.MakeThing(ThingDefOf.SignalAction_OpenCasket);
        signalAction_OpenCasket.signalTag = TriggerOpenAction;
        signalAction_OpenCasket.caskets.AddRange(caskets);
        signalAction_OpenCasket.completedSignalTag = CompletedOpenAction + Find.UniqueIDsManager.GetNextSignalTagID();
        if (Props.enableAfterTicks > 0)
        {
            signalAction_OpenCasket.delayTicks = Props.enableAfterTicks;
            SignalAction_Message obj = (SignalAction_Message)ThingMaker.MakeThing(ThingDefOf.SignalAction_Message);
            obj.signalTag = TriggerOpenAction;
            obj.lookTargets = caskets;
            obj.messageType = MessageTypeDefOf.ThreatBig;
            obj.message = "RG_MessageSleepingThreatDelayActivated".Translate(signalAction_OpenCasket.delayTicks.ToStringTicksToPeriod());
            GenSpawn.Spawn(obj, room.Cells.RandomElement(), parent.Map);
        }

        GenSpawn.Spawn(signalAction_OpenCasket, parent.Map.Center, parent.Map);

        SignalAction_Message obj2 = (SignalAction_Message)ThingMaker.MakeThing(ThingDefOf.SignalAction_Message);
        obj2.signalTag = signalAction_OpenCasket.completedSignalTag;
        obj2.lookTargets = caskets;
        obj2.messageType = MessageTypeDefOf.ThreatBig;
        obj2.message = "RG_MessageSleepingPawnsAlerted".Translate();
        GenSpawn.Spawn(obj2, room.Cells.RandomElement(), parent.Map);
    }

    protected void Trigger(Thing initiator)
    {
        Effecter effecter = new Effecter(EffecterDefOf.ActivatorProximityTriggered);
        effecter.Trigger(parent, TargetInfo.Invalid);
        effecter.Cleanup();
        Messages.Message(
            "MessageActivatorProximityTriggered".Translate(initiator),
            parent,
            MessageTypeDefOf.ThreatBig);
        Find.SignalManager.SendSignal(new Signal(TriggerOpenAction, parent.Named("SUBJECT")));
        SoundDefOf.MechanoidsWakeUp.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
        _sent = true;
    }

    public void Expire()
    {
        _sent = true;
    }

    public bool ShouldBeLitNow()
    {
        return !Sent;
    }

    public override void Notify_SignalReceived(Signal signal)
    {
        bool wasSent = signal.tag == TriggerOpenAction
            && signal.args.TryGetArg("SUBJECT", out Thing arg)
            && arg != parent
            && arg != null
            && arg.Map == parent.Map
            && parent.Position.DistanceTo(arg.Position) <= 40f;
        if (wasSent)
            _sent = true;

        if (!Props.triggeredBySkipPsycasts)
            return;

        Thing pawn = null;
        bool skipDetected = !_sent
            && signal.tag == CompAbilityEffect_Teleport.SkipUsedSignalTag
            && signal.args.TryGetArg("SUBJECT", out pawn)
            && signal.args.TryGetArg("POSITION", out LocalTargetInfo arg2)
            && pawn != null
            && pawn.Map == parent.Map
            && parent.Position.DistanceTo(arg2.Cell) <= 40f;
        if (skipDetected)
            Trigger(pawn);
    }

    public override string CompInspectStringExtra()
    {
        if (!Enabled)
        {
            return "SendSignalOnCountdownCompTime".Translate(
                _ticksUntilEnabled.ToStringTicksToPeriod(
                    allowSeconds: true,
                    shortForm: false,
                    canUseDecimals: true,
                    allowYears: false)).Resolve();
        }

        if (!_sent && Props.radius > 0)
            return "radius".Translate().CapitalizeFirst() + ": " + Props.radius.ToString("F0");

        return "expired".Translate().CapitalizeFirst();
    }

    public override void PostExposeData()
    {
        Scribe_Values.Look(ref _sent, "_sent", defaultValue: false);
        Scribe_Values.Look(ref _ticksUntilEnabled, "_ticksUntilEnabled", 0);
        Scribe_Values.Look(ref _resolved, "_resolved", defaultValue: false);
    }
}
