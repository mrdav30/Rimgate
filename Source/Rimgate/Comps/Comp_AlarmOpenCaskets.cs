using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private bool _received;
        private bool _resolved;

        public CompProperties_ProximityAlarm Props => (CompProperties_ProximityAlarm)props;
        public bool Enabled => _ticksUntilEnabled <= 0;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            _ticksUntilEnabled = Props.enableAfterTicks;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            // Create per-instance unique tags (persist across save)
            if (string.IsNullOrEmpty(_triggerTag))
                _triggerTag = $"RG.OpenCaskets:{parent.ThingID}:{Find.UniqueIDsManager.GetNextSignalTagID()}";
            if (string.IsNullOrEmpty(_completedTag))
                _completedTag = $"RG.OpenCasketsDone:{parent.ThingID}:{Find.UniqueIDsManager.GetNextSignalTagID()}";
        }

        public override void CompTick()
        {
            if (_sent)
                return;

            if (!_resolved)
                Resolve(); // one-time setup

            if (Enabled && parent.IsHashIntervalTick(250))
                CheckProximity();

            if (_ticksUntilEnabled > 0)
                _ticksUntilEnabled--;
        }

        private void CheckProximity()
        {
            Predicate<Thing> pred = t => t is Pawn p
                    && (Props.onlyHumanlike
                        ? p.RaceProps.Humanlike
                        : true)
                    && p.Faction.IsOfPlayerFaction();

            Thing found = null;

            if (Props.triggerOnPawnOnMap)
            {
                found = parent.Map.mapPawns.AllPawns
                    .Where(p => pred(p))
                    .FirstOrDefault();
            }
            else if (Props.triggerOnPawnInRoom)
            {
                var room = parent.GetRoom();
                if (room == null)
                    return;

                foreach (var t in room.ContainedAndAdjacentThings)
                {
                    if (pred(t))
                    {
                        found = t;
                        break;
                    }
                }
            }
            else if (Props.radius > 0f)
            {
                found = GenClosest.ClosestThingReachable(
                    parent.Position,
                    parent.Map,
                    ThingRequest.ForGroup(ThingRequestGroup.Pawn),
                    PathEndMode.OnCell,
                    TraverseParms.For(TraverseMode.NoPassClosedDoors),
                    Props.radius,
                    pred);
            }

            if (found == null)
                return;

            Trigger(found);
        }

        private void Trigger(Thing initiator)
        {
            if (_sent) return; // guard re-entry
            _sent = true; // set BEFORE sending the signal

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
                _received = true;
                return;
            }

            if (_received || !Props.triggeredBySkipPsycasts)
                return;

            // Optional: react to skip within 40 tiles by sending *our* trigger
            if (signal.tag == CompAbilityEffect_Teleport.SkipUsedSignalTag
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
            if (_received)
                return "expired".Translate().CapitalizeFirst();

            var sb = new StringBuilder();
            if (!Enabled)
            {
                sb.AppendLine("SendSignalOnCountdownCompTime".Translate(
                    _ticksUntilEnabled.ToStringTicksToPeriod(allowSeconds: true, canUseDecimals: true)));
            }

            if (Props.radius > 0)
                sb.AppendLine("radius".Translate().CapitalizeFirst() + ": " + Props.radius.ToString("F0"));

            sb.AppendLine("Active".Translate().CapitalizeFirst());

            return sb.ToString().TrimEnd();
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref _sent, "_sent", false);
            Scribe_Values.Look(ref _received, "_received", false);
            Scribe_Values.Look(ref _ticksUntilEnabled, "_ticksUntilEnabled", 0);
            Scribe_Values.Look(ref _resolved, "_resolved", false);
            Scribe_Values.Look(ref _triggerTag, "_triggerTag");
            Scribe_Values.Look(ref _completedTag, "_completedTag");
        }

        private void Resolve()
        {
            _resolved = true; // IMPORTANT: prevent repeated setup

            var room = parent.GetRoom();
            if (room == null) return;

            var caskets = new List<Thing>();
            foreach (var t in room.ContainedAndAdjacentThings)
            {
                if (t is Building_CryptosleepCasket
                    && (!Props.ignoreCasketDefs?.Contains(t.def) ?? true))
                {
                    caskets.Add(t);
                }
            }

            if (caskets.Count == 0) return;

            // Open action
            var open = ThingMaker.MakeThing(ThingDefOf.SignalAction_OpenCasket) as SignalAction_OpenCasket;
            open.signalTag = _triggerTag; // unique per alarm
            open.caskets.AddRange(caskets);
            open.completedSignalTag = _completedTag;
            open.delayTicks = Props.enableAfterTicks;

            if (Props.triggerOnPawnOnMap)
            {
                var msgDelay = ThingMaker.MakeThing(ThingDefOf.SignalAction_Message) as SignalAction_Message;
                msgDelay.signalTag = _triggerTag;
                msgDelay.lookTargets = null;
                msgDelay.messageType = MessageTypeDefOf.ThreatBig;
                msgDelay.message = "RG_MessageSleepingThreatDelayActivated".Translate(
                    Props.enableAfterTicks.ToStringTicksToPeriod());
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
    }
}
