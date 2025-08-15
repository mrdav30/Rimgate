using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VEF.Utils;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Rimgate;

public enum CloningStatus
{
    Idle = 0,
    CloningStarted,
    CloningFinished,
    HostDischarged,
    Error
}

public enum CloneType
{
    None = 0,
    Genome,
    Full,
    Enhanced,
    Reconstruct,
}

public class Building_WraithCloningPod : Building, IThingHolder, IThingHolderWithDrawnPawn, ISearchableContents, IOpenable
{
    protected ThingOwner innerContainer;

    protected bool contentsKnown;

    public CompRefuelable Refuelable
    {
        get
        {
            _cachedRefuelable ??= GetComp<CompRefuelable>();
            return _cachedRefuelable;
        }
    }

    public CompPowerTrader Power
    {
        get
        {
            _cachedPowerTrader ??= GetComp<CompPowerTrader>();
            return _cachedPowerTrader;
        }
    }

    public float HeldPawnDrawPos_Y => DrawPos.y - 0.03658537f;

    public float HeldPawnBodyAngle => Rotation.Opposite.AsAngle;

    public PawnPosture HeldPawnPosture => PawnPosture.LayingOnGroundFaceUp;

    public CloningStatus Status = CloningStatus.Idle;

    public bool ContainsCorpse => innerContainer.Count > 0 && innerContainer[0] is Corpse;

    public string openedSignal;

    public virtual int OpenTicks => 300;

    public bool HasAnyContents => innerContainer != null && innerContainer.Count > 0;

    public virtual bool CanOpen => HasAnyContents;

    public ThingOwner SearchableContents => innerContainer;

    public Thing ContainedThing
    {
        get
        {
            if (innerContainer.Count != 0)
            {
                return innerContainer[0];
            }

            return null;
        }
    }

    public Pawn InnerPawn
    {
        get
        {
            if (!HasAnyContents)
                return null;

            foreach (Thing thing in innerContainer)
            {
                if (thing is Pawn innerPawn)
                    return innerPawn;

                if (thing is Corpse corpse)
                    return corpse.InnerPawn;
            }

            return null;
        }
    }

    public float RemainingWork => _workRemaining;

    public CloneType CurrentJob => _currentJob;

    public bool IsWorking => Status == CloningStatus.CloningStarted;

    private static float _hostSavedFoodNeed;

    private static float _hostSavedDbhThirstNeed;

    private CompRefuelable _cachedRefuelable;

    private CompPowerTrader _cachedPowerTrader;

    private static List<ThingDef> _cachedPods;

    private float _workRemaining;

    private CloneType _currentJob;

    public Building_WraithCloningPod()
    {
        innerContainer = new ThingOwner<Thing>(this);
    }

    public ThingOwner GetDirectlyHeldThings()
    {
        return innerContainer;
    }

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        if (base.Faction != null && base.Faction.IsPlayer)
            contentsKnown = true;
    }

    public override AcceptanceReport ClaimableBy(Faction fac)
    {
        if (innerContainer.Any && !contentsKnown)
            return false;

        return base.ClaimableBy(fac);
    }

    protected override void Tick()
    {
        base.Tick();

        // State-dependent power consumption
        if (Status == CloningStatus.CloningStarted || Status == CloningStatus.CloningFinished)
            Power.PowerOutput = -PowerComp.Props.PowerConsumption;
        else
            Power.PowerOutput = -PowerComp.Props.idlePowerDraw;

        Pawn pawn = InnerPawn;
        if (pawn == null)
            return;

        if (!Power.PowerOn || !Refuelable.IsFull)
        {
            // Interrupt cloning on power loss
            if (!Power.PowerOn)
                Log.Message(this + $" :: Lost power while running (state: {Status})");

            EjectContents();
            Reset();
            return;
        }

        bool containsCorpse = ContainsCorpse;
        switch (Status)
        {
            case CloningStatus.Idle:
                {
                    if (containsCorpse)
                        break;

                    // Save initial patient food need level
                    if (pawn.needs.food != null)
                        _hostSavedFoodNeed = pawn.needs.food.CurLevelPercentage;

                    // Save initial patient DBH thirst and reset DBH bladder/hygiene need levels
                    if (ModCompatibility.DbhIsActive)
                    {
                        _hostSavedDbhThirstNeed = DbhCompatibility.GetThirstNeedCurLevelPercentage(pawn);
                        DbhCompatibility.SetBladderNeedCurLevelPercentage(pawn, 1f);
                        DbhCompatibility.SetHygieneNeedCurLevelPercentage(pawn, 1f);
                    }

                    break;
                }
            case CloningStatus.CloningFinished:
                {

                    if (!containsCorpse)
                    {
                        // Restore previously saved patient food need level
                        if (pawn.needs.food != null)
                            pawn.needs.food.CurLevelPercentage = _hostSavedFoodNeed;

                        // Restore previously saved patient DBH thirst and reset DBH bladder/hygiene need levels

                        if (ModCompatibility.DbhIsActive)
                        {
                            DbhCompatibility.SetThirstNeedCurLevelPercentage(pawn, _hostSavedDbhThirstNeed);
                            DbhCompatibility.SetBladderNeedCurLevelPercentage(pawn, 1f);
                            DbhCompatibility.SetHygieneNeedCurLevelPercentage(pawn, 1f);
                        }
                    }

                    EjectContents();
                    SwitchState();

                    break;
                }
            case CloningStatus.HostDischarged:
                {
                    Reset();
                    break;
                }
        }

        if (containsCorpse)
            return;

        // Suspend patient needs during cloning
        bool suspendNeeds = Status == CloningStatus.CloningStarted
            || Status == CloningStatus.CloningFinished;
        if (suspendNeeds)
        {
            // Food
            if (pawn.needs.food != null)
                pawn.needs.food.CurLevelPercentage = 1f;

            // Dubs Bad Hygiene thirst, bladder and hygiene
            if (ModCompatibility.DbhIsActive)
            {
                DbhCompatibility.SetThirstNeedCurLevelPercentage(pawn, 1f);
                DbhCompatibility.SetBladderNeedCurLevelPercentage(pawn, 1f);
                DbhCompatibility.SetHygieneNeedCurLevelPercentage(pawn, 1f);
            }
        }
    }

    public virtual void Open()
    {
        if (HasAnyContents)
        {
            EjectContents();
            Reset();
            if (!openedSignal.NullOrEmpty())
            {
                Find.SignalManager.SendSignal(new Signal(openedSignal, this.Named("SUBJECT")));
            }

            DirtyMapMesh(base.Map);
        }
    }

    public virtual bool Accepts(Thing thing)
    {
        return innerContainer.CanAcceptAnyOf(thing);
    }

    public virtual bool TryAcceptThing(Thing thing, bool allowSpecialEffects = true)
    {
        if (!Accepts(thing))
            return false;

        bool flag;
        if (thing.holdingOwner != null)
        {
            thing.holdingOwner.TryTransferToContainer(thing, innerContainer, thing.stackCount);
            flag = true;
        }
        else
            flag = innerContainer.TryAdd(thing);

        if (flag)
        {
            if (thing.Faction != null && thing.Faction.IsPlayer)
                contentsKnown = true;

            if (allowSpecialEffects)
                SoundStarter.PlayOneShot(
                    SoundDefOf.CryptosleepCasket_Accept,
                    new TargetInfo(Position, Map, false));

            return true;
        }

        return false;
    }

    public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn pawn)
    {
        Building_WraithCloningPod cloningPod = this;

        foreach (FloatMenuOption floatMenuOption in base.GetFloatMenuOptions(pawn))
            yield return floatMenuOption;

        bool canReach = ReachabilityUtility.CanReach(
            pawn,
            cloningPod,
            PathEndMode.InteractionCell,
            Danger.Deadly,
            false,
            false,
            TraverseMode.ByPawn);
        if (!canReach)
        {
            yield return new FloatMenuOption(Translator.Translate("CannotUseNoPath"), null);
            yield break;
        };

        if (!Power.PowerOn)
        {
            yield return new FloatMenuOption("RG_CannotUseNoPower".Translate(), null);
            yield break;
        }

        if (!Refuelable.IsFull)
        {
            yield return new FloatMenuOption(Translator.Translate("RG_CannotUseFuelLow"), null);
            yield break;
        }

        // initial cloning
        if (cloningPod.innerContainer.Count == 0)
        {
            if (!pawn.RaceProps.IsFlesh)
            {
                yield return new FloatMenuOption("RG_CannotUseNotBiologic".Translate(), null);
                yield break;
            }

            if (QuestUtility.IsQuestLodger(pawn))
            {
                yield return new FloatMenuOption("CryptosleepCasketGuestsNotAllowed".Translate(), null);
                yield break;
            }

            JobDef jobDef = Rimgate_DefOf.Rimgate_EnterCloningPod;
            yield return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(
                    "RG_EnterCloningPod".Translate(),
                    () => pawn.jobs.TryTakeOrderedJob(
                            new Job(jobDef, this),
                            JobTag.Misc,
                            false)
                    ),
                pawn,
                cloningPod,
                "ReservedBy",
                null);

            yield break;
        }

        if (pawn.skills.GetSkill(SkillDefOf.Medicine).levelInt < 10)
        {
            yield return new FloatMenuOption("RG_CannotUseMedicineTooLow".Translate(), null);
            yield break;
        }

        if (!cloningPod.InnerPawn.Dead)
        {
            JobDef jobDefGenome = Rimgate_DefOf.Rimgate_CloneOccupantGenes;
            yield return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(
                    "RG_CloneOccupantGenes".Translate(cloningPod.InnerPawn.LabelCap),
                    () => pawn.jobs.TryTakeOrderedJob(
                        new Job(jobDefGenome, this),
                        JobTag.Misc,
                        false)
                    ),
                pawn,
                cloningPod,
                "ReservedBy",
                null);

            if (!Rimgate_DefOf.Rimgate_WraithCloneFull.IsFinished)
                yield break;

            JobDef jobDefFull = Rimgate_DefOf.Rimgate_CloneOccupantFull;
            yield return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(
                    "RG_CloneOccupantFull".Translate(cloningPod.InnerPawn.LabelCap),
                    () => pawn.jobs.TryTakeOrderedJob(
                        new Job(jobDefFull, this),
                        JobTag.Misc,
                        false)
                    ),
                pawn,
                cloningPod,
                "ReservedBy",
                null);

            if (!Rimgate_DefOf.Rimgate_WraithCloneEnhancement.IsFinished)
                yield break;

            JobDef jobDefSoldier = Rimgate_DefOf.Rimgate_CloneOccupantSoldier;
            yield return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(
                    "RG_CloneOccupantSoldier".Translate(cloningPod.InnerPawn.LabelCap),
                    () => pawn.jobs.TryTakeOrderedJob(
                        new Job(jobDefSoldier, this),
                        JobTag.Misc,
                        false)
                    ),
                pawn,
                cloningPod,
                "ReservedBy",
                null);
        }
        else
        {
            if (!Rimgate_DefOf.Rimgate_WraithCloneCorpse.IsFinished)
                yield break;

            JobDef jobDefReconstruct = Rimgate_DefOf.Rimgate_CloneReconstructDead;
            yield return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(
                    "RG_CloneReconstructDead".Translate(cloningPod.InnerPawn.LabelCap),
                    () => pawn.jobs.TryTakeOrderedJob(
                        new Job(jobDefReconstruct, this),
                        JobTag.Misc, false)
                   ),
                pawn,
                cloningPod,
                "ReservedBy",
                null);
        }
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos())
            yield return gizmo;

        Gizmo select = Building.SelectContainedItemGizmo(this, ContainedThing);
        if (select != null)
            yield return select;

        if (Faction == Faction.OfPlayer
            && innerContainer.Count > 0
            && def.building.isPlayerEjectable)
        {
            Command_Action commandAction = new()
            {
                action = () =>
                {
                    EjectContents();
                    Reset();
                },
                defaultLabel = "RG_CommandCloningPodEjectLabel".Translate(),
                defaultDesc = "RG_CommandCloningPodEjectDesc".Translate(),
                hotKey = KeyBindingDefOf.Misc1,
                icon = ContentFinder<Texture2D>.Get("UI/Icon/Button/RGWraithCloningPodEjectIcon", true)
            };

            if (innerContainer.Count == 0)
                commandAction.Disable("CommandPodEjectFailEmpty".Translate());

            yield return commandAction;
        }
    }

    public virtual void EjectContents()
    {
        if (!HasAnyContents)
            return;

        ThingDef filthSlime = ThingDefOf.Filth_Slime;
        foreach (Thing thing in innerContainer)
        {
            if (thing is not Pawn pawn)
                continue;

            PawnComponentsUtility.AddComponentsForSpawn(pawn);
            pawn.filth.GainFilth(filthSlime);
            if (pawn.RaceProps.IsFlesh)
                pawn.health.AddHediff(Rimgate_DefOf.Rimgate_ClonePodSickness, null, null, null);

        }
        if (!Destroyed)
            SoundStarter.PlayOneShot(
                SoundDefOf.CryptosleepCasket_Eject,
                SoundInfo.InMap(new TargetInfo(Position, Map, false), MaintenanceType.None));

        innerContainer.TryDropAll(InteractionCell, base.Map, ThingPlaceMode.Near);
        contentsKnown = true;
    }

    public override void DeSpawn(DestroyMode mode = 0)
    {
        EjectContents();
        Reset();
        base.DeSpawn(mode);
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        Map map = base.Map;
        base.Destroy(mode);
        if (innerContainer.Count > 0 && (mode == DestroyMode.Deconstruct || mode == DestroyMode.KillFinalize))
        {
            if (mode != DestroyMode.Deconstruct)
            {
                List<Pawn> list = new List<Pawn>();
                foreach (Thing item2 in (IEnumerable<Thing>)innerContainer)
                {
                    if (item2 is Pawn item)
                        list.Add(item);
                }

                foreach (Pawn item3 in list)
                    HealthUtility.DamageUntilDowned(item3);
            }

            innerContainer.TryDropAll(base.Position, map, ThingPlaceMode.Near);
        }

        innerContainer.ClearAndDestroyContents();
    }

    public override string GetInspectString()
    {
        string text = base.GetInspectString();
        string str = (contentsKnown ? innerContainer.ContentsString : ((string)"UnknownLower".Translate()));
        if (!text.NullOrEmpty())
            text += "\n";

        return text + ("CasketContains".Translate() + ": " + str.CapitalizeFirst());
    }

    public static Building_WraithCloningPod FindCloningPodFor(
      Thing rescuee,
      Pawn traveler,
      bool ignoreOtherReservations = false)
    {
        _cachedPods ??= DefDatabase<ThingDef>.AllDefs
            .Where<ThingDef>(def => typeof(Building_WraithCloningPod).IsAssignableFrom(def.thingClass))
            .ToList();
        foreach (ThingDef thingDef in _cachedPods)
        {
            bool queuing = KeyBindingDefOf.QueueOrder.IsDownEvent;
            Building_WraithCloningPod cloningCasketFor = GenClosest.ClosestThingReachable(
                rescuee.Position,
                rescuee.Map,
                ThingRequest.ForDef(thingDef),
                PathEndMode.InteractionCell,
                TraverseParms.For(
                    traveler,
                    Danger.Deadly,
                    TraverseMode.ByPawn,
                    false,
                    false,
                    false,
                    true),
                9999f,
                Validator) as Building_WraithCloningPod;

            if (cloningCasketFor != null)
                return cloningCasketFor;

            bool Validator(Thing x)
            {
                if (((Building_WraithCloningPod)x).HasAnyContents)
                    return false;

                if (!queuing || !traveler.HasReserved(x))
                    return traveler.CanReserve(x, 1, -1, null, ignoreOtherReservations);

                return false;
            }
        }

        return null;
    }

    public void InitiateWork(CloneType jobType, float workAmount)
    {
        _currentJob = jobType;
        _workRemaining = workAmount;
    }

    public void SetWorkAmount(float amount)
    {
        _workRemaining = amount;
    }

    public void TickWork(float amount)
    {
        _workRemaining -= amount;
    }

    public void SwitchState()
    {
        CloningStatus oldStatus = Status;
        switch (Status)
        {
            case CloningStatus.Idle:
                Status = CloningStatus.CloningStarted;
                break;

            case CloningStatus.CloningStarted:
                Status = CloningStatus.CloningFinished;
                break;

            case CloningStatus.CloningFinished:
                Status = CloningStatus.HostDischarged;
                break;

            case CloningStatus.HostDischarged:
                Status = CloningStatus.Idle;
                break;

            default:
                Status = CloningStatus.Error;
                break;
        }

        if (RimgateMod.Debug)
            Log.Message(this + $" :: state change from {oldStatus.ToStringSafe().Colorize(Color.yellow)}"
                + $" to {Status.ToStringSafe().Colorize(Color.yellow)}");
    }

    public void Reset()
    {
        _currentJob = CloneType.None;
        Status = CloningStatus.Idle;
        _workRemaining = 0;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        Scribe_Values.Look(ref contentsKnown, "contentsKnown", defaultValue: false);
        Scribe_Values.Look(ref openedSignal, "openedSignal");
        Scribe_Values.Look(ref _workRemaining, "_remainingWork", 0f);
    }
}
