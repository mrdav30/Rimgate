using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Rimgate
{
    public class Comp_AlarmOpenCaskets : ThingComp, IThingGlower
    {
        // Per-instance unique tags
        private string _triggerTag;
        private string _completedTag;

        private int _ticksUntilEnabled;
        private bool _sent;
        private bool _resolved;

        public CompProperties_ProximityAlarm Props => (CompProperties_ProximityAlarm)props;
        public bool Sent => _sent;
        public bool Enabled => _ticksUntilEnabled <= 0;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            _ticksUntilEnabled = Props.enableAfterTicks;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            // Create per-instance unique tags (persist across save)
            if (string.IsNullOrEmpty(_triggerTag))
                _triggerTag = $"RG.OpenCaskets:{parent.ThingID}:{Find.UniqueIDsManager.GetNextSignalTagID()}";
            if (string.IsNullOrEmpty(_completedTag))
                _completedTag = $"RG.OpenCasketsDone:{parent.ThingID}:{Find.UniqueIDsManager.GetNextSignalTagID()}";
        }

        public override void CompTick()
        {
            base.CompTick();
            if (_sent || !parent.Spawned) return;

            if (!_resolved) Resolve(); // one-time setup

            if (Enabled && parent.IsHashIntervalTick(250))
                CompTickRare();

            if (_ticksUntilEnabled > 0) _ticksUntilEnabled--;
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            if (_sent || !Enabled) return;

            Predicate<Thing> pred = null;
            if (Props.onlyHumanlike)
                pred = t => t is Pawn p && p.RaceProps.Humanlike && p.Faction == Faction.OfPlayer;

            Thing found = null;
            if (Props.triggerOnPawnInRoom)
            {
                var room = parent.GetRoom();
                if (room != null)
                {
                    foreach (var t in room.ContainedAndAdjacentThings)
                    {
                        if (pred == null ? (t is Pawn p && p.Faction == Faction.OfPlayer) : pred(t))
                        { found = t; break; }
                    }
                }
            }

            if (found == null && Props.radius > 0f)
            {
                found = GenClosest.ClosestThingReachable(
                    parent.Position, parent.Map,
                    ThingRequest.ForGroup(ThingRequestGroup.Pawn),
                    PathEndMode.OnCell,
                    TraverseParms.For(TraverseMode.NoPassClosedDoors),
                    Props.radius,
                    pred);
            }

            if (found != null)
                Trigger(found);
        }

        private void Resolve()
        {
            _resolved = true; // IMPORTANT: prevent repeated setup

            var room = parent.GetRoom();
            if (room == null) return;

            var caskets = new List<Thing>();
            foreach (var t in room.ContainedAndAdjacentThings)
                if (t is Building_CryptosleepCasket) caskets.Add(t);

            if (caskets.Count == 0) return;

            // Open action
            var open = (SignalAction_OpenCasket)ThingMaker.MakeThing(ThingDefOf.SignalAction_OpenCasket);
            open.signalTag = _triggerTag; // unique per alarm
            open.caskets.AddRange(caskets);
            open.completedSignalTag = _completedTag;

            if (Props.enableAfterTicks > 0)
            {
                open.delayTicks = Props.enableAfterTicks;

                var msgDelay = (SignalAction_Message)ThingMaker.MakeThing(ThingDefOf.SignalAction_Message);
                msgDelay.signalTag = _triggerTag;
                msgDelay.lookTargets = caskets;
                msgDelay.messageType = MessageTypeDefOf.ThreatBig;
                msgDelay.message = "RG_MessageSleepingThreatDelayActivated".Translate(
                    open.delayTicks.ToStringTicksToPeriod());
                GenSpawn.Spawn(msgDelay, room.Cells.RandomElement(), parent.Map);
            }

            // Place the actions in/near the room, not map center
            GenSpawn.Spawn(open, room.Cells.RandomElement(), parent.Map);

            var msgDone = (SignalAction_Message)ThingMaker.MakeThing(ThingDefOf.SignalAction_Message);
            msgDone.signalTag = _completedTag;
            msgDone.lookTargets = caskets;
            msgDone.messageType = MessageTypeDefOf.ThreatBig;
            msgDone.message = "RG_MessageSleepingPawnsAlerted".Translate();
            GenSpawn.Spawn(msgDone, room.Cells.RandomElement(), parent.Map);
        }

        private void Trigger(Thing initiator)
        {
            if (_sent) return;          // guard re-entry
            _sent = true;               // set BEFORE sending the signal

            var fx = new Effecter(EffecterDefOf.ActivatorProximityTriggered);
            fx.Trigger(parent, TargetInfo.Invalid);
            fx.Cleanup();

            Messages.Message("MessageActivatorProximityTriggered".Translate(initiator),
                             parent, MessageTypeDefOf.ThreatBig);

            // Send ONLY our unique tag so other rooms don’t fire.
            Find.SignalManager.SendSignal(new Signal(_triggerTag, parent.Named("SUBJECT")));
            SoundDefOf.MechanoidsWakeUp.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
        }

        public void Expire() => _sent = true;

        public bool ShouldBeLitNow() => !_sent;

        public override void Notify_SignalReceived(Signal signal)
        {
            // With unique tags, we don’t need cross-alarm sync anymore.
            // If you *do* want to listen, ensure we only react to our tag:
            if (signal.tag == _triggerTag)
            {
                _sent = true;
            }

            if (!Props.triggeredBySkipPsycasts) return;

            // Optional: react to skip within 40 tiles by sending *our* trigger
            if (!_sent
                && signal.tag == CompAbilityEffect_Teleport.SkipUsedSignalTag
                && signal.args.TryGetArg("POSITION", out LocalTargetInfo pos)
                && signal.args.TryGetArg("SUBJECT", out Thing sk)
                && sk?.Map == parent.Map
                && parent.Position.DistanceTo(pos.Cell) <= 40f)
            {
                Trigger(sk);
            }
        }

        public override string CompInspectStringExtra()
        {
            if (!Enabled)
                return "SendSignalOnCountdownCompTime".Translate(
                    _ticksUntilEnabled.ToStringTicksToPeriod(allowSeconds: true, canUseDecimals: true));

            if (!_sent && Props.radius > 0)
                return "radius".Translate().CapitalizeFirst() + ": " + Props.radius.ToString("F0");

            return _sent
                ? "Active".Translate().CapitalizeFirst()
                : "expired".Translate().CapitalizeFirst();
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref _sent, "_sent", false);
            Scribe_Values.Look(ref _ticksUntilEnabled, "_ticksUntilEnabled", 0);
            Scribe_Values.Look(ref _resolved, "_resolved", false);
            Scribe_Values.Look(ref _triggerTag, "_triggerTag");
            Scribe_Values.Look(ref _completedTag, "_completedTag");
        }
    }
}
