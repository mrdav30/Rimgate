using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

#nullable disable
namespace Rimgate;

public class LordJob_MaraudColony : LordJob
{
    private Faction _assaulterFaction;

    private bool _canKidnap = true;

    private bool _canTimeoutOrFlee = true;

    private bool _sappers;

    private bool _useAvoidGridSmart;

    private bool _canSteal = true;

    // high-priority theft target
    private Thing _priorityTarget;

    private static readonly IntRange _assaultTimeBeforeGiveUp = new IntRange(26000, 38000);

    private static readonly IntRange _sapTimeBeforeGiveUp = new IntRange(33000, 38000);

    public override bool GuiltyOnDowned => true;

    public LordJob_MaraudColony() { }

    public LordJob_MaraudColony(SpawnedPawnParams parms)
    {
        _assaulterFaction = parms.spawnerThing.Faction;
        _canKidnap = false;
        _canTimeoutOrFlee = false;
        _canSteal = false;
    }

    public LordJob_MaraudColony(
      Faction assaulterFaction,
      bool canKidnap = true,
      bool canTimeoutOrFlee = true,
      bool sappers = false,
      bool useAvoidGridSmart = false,
      bool canSteal = true,
      Thing priorityTarget = null)
    {
        _assaulterFaction = assaulterFaction;
        _canKidnap = canKidnap;
        _canTimeoutOrFlee = canTimeoutOrFlee;
        _sappers = sappers;
        _useAvoidGridSmart = useAvoidGridSmart;
        _canSteal = canSteal;
        _priorityTarget = priorityTarget;
    }

    public override StateGraph CreateGraph()
    {
        StateGraph graph = new StateGraph();

        LordToil sappersToil = null;
        if (_sappers)
        {
            sappersToil = new LordToil_AssaultColonySappers { useAvoidGrid = _useAvoidGridSmart };
            graph.AddToil(sappersToil);
            Transition loop = new Transition(sappersToil, sappersToil, true, true);
            loop.AddTrigger(new Trigger_PawnLost(PawnLostCondition.Undefined));
            graph.AddTransition(loop);
        }

        // feed the target into the maraud toil
        LordToil maraudToil = new LordToil_MaraudColony(priorityTarget: _priorityTarget)
        {
            useAvoidGrid = _useAvoidGridSmart
        };
        graph.AddToil(maraudToil);

        LordToil_ExitMap exitToil = new LordToil_ExitMap(LocomotionUrgency.Jog, false, true);
        exitToil.useAvoidGrid = true;
        graph.AddToil(exitToil);

        if (_sappers)
        {
            Transition toMaraud = new Transition(sappersToil, maraudToil);
            toMaraud.AddTrigger((Trigger)new Trigger_NoFightingSappers());
            graph.AddTransition(toMaraud, false);
        }

        if (_assaulterFaction.def.humanlikeFaction)
        {
            if (_canTimeoutOrFlee)
            {
                Transition toGiveUp = new Transition(maraudToil, exitToil);
                if (sappersToil != null)
                    toGiveUp.AddSource(sappersToil);

                int randomInRange1;
                if (!_sappers)
                {
                    IntRange timeBeforeGiveUp = _assaultTimeBeforeGiveUp;
                    randomInRange1 = timeBeforeGiveUp.RandomInRange;
                }
                else
                {
                    IntRange timeBeforeGiveUp = _sapTimeBeforeGiveUp;
                    randomInRange1 = timeBeforeGiveUp.RandomInRange;
                }

                toGiveUp.AddTrigger(new Trigger_TicksPassed(randomInRange1));
                toGiveUp.AddPreAction(new TransitionAction_Message(
                        "MessageRaidersGivenUpLeaving".Translate(
                            _assaulterFaction.def.pawnsPlural.CapitalizeFirst(),
                            _assaulterFaction.Name)));
                graph.AddTransition(toGiveUp, false);
                Transition toSatisfiedExit = new Transition(maraudToil, exitToil);
                if (sappersToil != null)
                    toSatisfiedExit.AddSource(sappersToil);
                FloatRange floatRange = new FloatRange(0.25f, 0.35f);
                float randomInRange2 = floatRange.RandomInRange;
                toSatisfiedExit.AddTrigger(new Trigger_FractionColonyDamageTaken(randomInRange2, 900f));
                toSatisfiedExit.AddPreAction(new TransitionAction_Message(
                        "MessageRaidersSatisfiedLeaving".Translate(
                            _assaulterFaction.def.pawnsPlural.CapitalizeFirst(),
                            _assaulterFaction.Name)));
                graph.AddTransition(toSatisfiedExit, false);
            }

            if (_canKidnap)
            {
                LordToil kidnapToil = graph.AttachSubgraph(new LordJob_Kidnap().CreateGraph()).StartingToil;
                Transition toKidnap = new Transition(maraudToil, kidnapToil);
                if (sappersToil != null)
                    toKidnap.AddSource(sappersToil);

                toKidnap.AddPreAction(new TransitionAction_Message(
                    "MessageRaidersKidnapping".Translate(
                        _assaulterFaction.def.pawnsPlural.CapitalizeFirst(),
                        _assaulterFaction.Name
                    )));
                toKidnap.AddTrigger((Trigger)new Trigger_KidnapVictimPresent());
                graph.AddTransition(toKidnap, false);
            }

            if (_canSteal)
            {
                LordToil stealToil = graph.AttachSubgraph(new LordJob_Steal().CreateGraph()).StartingToil;
                Transition toSteal = new Transition(maraudToil, stealToil);
                if (sappersToil != null)
                    toSteal.AddSource(sappersToil);

                toSteal.AddPreAction(
                    new TransitionAction_Message(
                    "MessageRaidersStealing".Translate(
                        _assaulterFaction.def.pawnsPlural.CapitalizeFirst(),
                        _assaulterFaction.Name
                    )));
                toSteal.AddTrigger(new Trigger_HighValueThingsAround());
                graph.AddTransition(toSteal, false);
            }

            // if the priority target despawns or is destroyed, switch behavior
            if (_priorityTarget != null)
            {
                LordToil transitionToil = _canSteal
                    ? graph.AttachSubgraph(new LordJob_Steal().CreateGraph()).StartingToil
                    : exitToil;

                Transition switchIfGone = new Transition(maraudToil, transitionToil);
                switchIfGone.AddTrigger(new Trigger_Custom((signal) => _priorityTarget.DestroyedOrNull()));
                switchIfGone.AddPreAction(new TransitionAction_Message(
                    "MessageRaidersLeaving".Translate(
                        _assaulterFaction.def.pawnsPlural.CapitalizeFirst(),
                        _assaulterFaction.Name)));
                graph.AddTransition(switchIfGone);
            }
        }

        Transition toExit = new Transition(maraudToil, exitToil);
        if (sappersToil != null)
            toExit.AddSource(sappersToil);

        toExit.AddTrigger(new Trigger_BecameNonHostileToPlayer());
        toExit.AddPreAction(new TransitionAction_Message(
                    "MessageRaidersLeaving".Translate(
                        _assaulterFaction.def.pawnsPlural.CapitalizeFirst(),
                        _assaulterFaction.Name
                    )));
        graph.AddTransition(toExit, false);

        return graph;
    }

    public override void ExposeData()
    {
        Scribe_References.Look(ref _assaulterFaction, "_assaulterFaction");
        Scribe_Values.Look(ref _canKidnap, "_canKidnap", true);
        Scribe_Values.Look(ref _canTimeoutOrFlee, "_canTimeoutOrFlee", true);
        Scribe_Values.Look(ref _sappers, "_sappers");
        Scribe_Values.Look(ref _useAvoidGridSmart, "_useAvoidGridSmart");
        Scribe_Values.Look(ref _canSteal, "_canSteal", true);
        Scribe_References.Look(ref _priorityTarget, "_priorityTarget");
    }
}