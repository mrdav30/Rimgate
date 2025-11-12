using System.Collections.Generic;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace Rimgate;

public class Comp_MobileContainer : ThingComp, IThingHolder, ISearchableContents
{
    public ThingOwner_Container InnerContainer;

    public List<TransferableOneWay> LeftToLoad;

    public float MassCapacityOverride = -1f;

    public Pawn Pusher;

    public float PushingFuel;  // current fuel while proxy is carried

    public float FuelPerTick; // cached per-tick rate during push

    public ThingDef SavedStuff; // original stuff

    public int SavedHitPoints;

    public Color SavedDrawColor, SavedDrawColorTwo;

    public bool SavedHasPaint;

    public bool IsAttached => Pusher != null;

    private List<Thing> _tmpThings = new();

    private bool _massDirty = true;

    private float _cachedMassUsage;

    private int _stalledSinceTick = -1;

    private const int StallFinalizeDelayTicks = 600; // ~10 sec at 60 ticks/sec

    private CompRefuelable _cachedRefuelable;

    public Map Map => parent.MapHeld;

    public Thing FirstThingLeftToLoad
    {
        get
        {
            if (LeftToLoad == null)
                return null;

            for (int i = 0; i < LeftToLoad.Count; i++)
            {
                if (LeftToLoad[i].CountToTransfer != 0 && LeftToLoad[i].HasAnyThing)
                {
                    for (int j = 0; j < LeftToLoad[i].things.Count; j++)
                    {
                        Thing thing = LeftToLoad[i].things[j];
                        if (thing != null && thing.Spawned)
                            return thing;
                    }
                }
            }

            return null;
        }
    }

    public bool LoadingInProgress => LeftToLoad != null && LeftToLoad.Any(t => t.CountToTransfer > 0);

    public bool AnyPawnCanLoadAnythingNow
    {
        get
        {
            if (!LoadingInProgress || !parent.Spawned) return false;

            IReadOnlyList<Pawn> pawns = parent.Map.mapPawns.AllPawnsSpawned;

            // 1) Only count CURRENT jobs that still have a valid, selected thing
            for (int i = 0; i < pawns.Count; i++)
            {
                var cur = pawns[i];
                if (cur.CurJobDef != RimgateDefOf.Rimgate_HaulToContainer) continue;

                if (cur.jobs.curDriver is JobDriver_HaulToMobileContainer jd
                    && jd.Mobile?.parent.ThingID == parent.ThingID)
                {
                    var t = jd.ThingToCarry; // JobDriver_HaulToContainer exposes this
                    if (t != null && t.Spawned && LeftToLoadContains(t))
                        return true;
                }
            }

            // 2) Otherwise, would any pawn be able to pick a new target *now*?
            for (int k = 0; k < pawns.Count; k++)
            {
                var colonist = pawns[k];
                if (!colonist.IsColonist) continue;

                if (MobileContainerUtility.HasJobOnContainer(colonist, this))
                    return true;
            }

            return false;
        }
    }

    public float MassCapacity => !(MassCapacityOverride <= 0f) ? MassCapacityOverride : Props.massCapacity;

    public ThingOwner SearchableContents => InnerContainer;

    public bool OverMassCapacity => MassUsage > MassCapacity;

    public float MassUsage
    {
        get
        {
            if (_massDirty)
            {
                _massDirty = false;
                _cachedMassUsage = CollectionsMassCalculator.MassUsage(InnerContainer, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, includePawnsMass: false);
            }

            return _cachedMassUsage;
        }
    }

    // Slowdown severity (0..1)
    public float SlowdownSeverity => Props.slowdownSeverity;

    public bool UsesFuelWhilePushing => parent.GetComp<CompRefuelable>() != null;

    public CompRefuelable Refuelable
    {
        get
        {
            _cachedRefuelable ??= parent.GetComp<CompRefuelable>();
            return _cachedRefuelable;
        }
    }

    public float ConsumptionRatePerTick => Refuelable?.Props?.fuelConsumptionRate / GenDate.TicksPerDay ?? 0f;

    public bool FuelOK => Refuelable == null || Refuelable.HasFuel;

    public bool IsProxyRefuelable => FuelPerTick > 0f;

    public bool ProxyFuelOk => !IsProxyRefuelable || PushingFuel > 0f;

    public CompProperties_MobileContainer Props => (CompProperties_MobileContainer)props;

    public Comp_MobileContainer()
    {
        InnerContainer = new ThingOwner_Container(this);
    }

    public override void CompTick()
    {
        if (!parent.Spawned) return;

        if (Props.shouldTickContents)
            InnerContainer.DoTick();

        if (!parent.IsHashIntervalTick(60)) return; // ~1s

        // If we’re not actively loading, clear stall state and bail.
        if (!LoadingInProgress)
        {
            _stalledSinceTick = -1;
            return;
        }

        // If anyone can actually load something right now, we’re not stalled.
        if (AnyPawnCanLoadAnythingNow)
        {
            _stalledSinceTick = -1;
            return;
        }

        // We are stalled: start or check the timer.
        if (_stalledSinceTick < 0)
        {
            _stalledSinceTick = Find.TickManager.TicksGame;
            return;
        }

        int elapsed = Find.TickManager.TicksGame - _stalledSinceTick;
        if (elapsed >= (Props?.stallFinalizeDelayTicks ?? StallFinalizeDelayTicks))
            FinalizeShortfall();
    }

    public void Attach(Pawn pawn)
    {
        if (Utils.PawnIncapableOfHauling(pawn, out string reason))
        {
            Messages.Message(reason, parent, MessageTypeDefOf.RejectInput);
            return;
        }
        Pusher = pawn;
    }

    public void Detach() => Pusher = null;

    public override void PostExposeData()
    {
        base.PostExposeData();

        bool flag = !parent.SpawnedOrAnyParentSpawned;
        if (flag && Scribe.mode == LoadSaveMode.Saving)
        {
            _tmpThings.Clear();
            _tmpThings.AddRange(InnerContainer);
            for (int i = 0; i < _tmpThings.Count; i++)
            {
                if (_tmpThings[i] is Pawn pawn)
                {
                    InnerContainer.Remove(pawn);
                    if (!pawn.IsWorldPawn())
                    {
                        Log.Error("Trying to save a non-world pawn (" + pawn?.ToString() + ") as a reference in a mobile container.");
                    }
                }
            }

            _tmpThings.Clear();
        }

        Scribe_Deep.Look(ref InnerContainer, "InnerContainer", this);
        Scribe_Collections.Look(ref LeftToLoad, "LeftToLoad", LookMode.Deep);
        Scribe_Values.Look(ref _stalledSinceTick, "_stalledSinceTick", defaultValue: -1);
        Scribe_Values.Look(ref MassCapacityOverride, "MassCapacityOverride", 0f);
        Scribe_References.Look(ref Pusher, "Pusher");
        Scribe_Values.Look(ref PushingFuel, "PushingFuel", 0f);
        Scribe_Values.Look(ref FuelPerTick, "FuelPerTick", 0f);
        Scribe_Defs.Look(ref SavedStuff, "SavedStuff");
        Scribe_Values.Look(ref SavedHitPoints, "SavedHitPoints", 0);
        Scribe_Values.Look(ref SavedDrawColor, "SavedDrawColor", default);
        Scribe_Values.Look(ref SavedDrawColorTwo, "SavedDrawColorTwo", default);
        Scribe_Values.Look(ref SavedHasPaint, "SavedHasPaint", false);
    }

    public ThingOwner GetDirectlyHeldThings() => InnerContainer;

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo item in base.CompGetGizmosExtra())
            yield return item;

        int num = 0;
        foreach (object selectedObject in Find.Selector.SelectedObjects)
        {
            if (selectedObject is ThingWithComps thing && thing.HasComp<Comp_MobileContainer>())
                num++;
        }

        if (LoadingInProgress)
        {
            Command_Action cancelLoadCommand = new Command_Action();
            cancelLoadCommand.defaultLabel = "CommandCancelLoad".Translate();
            cancelLoadCommand.defaultDesc = "CommandCancelLoadDesc".Translate();
            cancelLoadCommand.icon = RimgateTex.CancelCommandTex;
            cancelLoadCommand.action = delegate
            {
                SoundDefOf.Designate_Cancel.PlayOneShotOnCamera();
                CancelLoad(Map);
            };
            yield return cancelLoadCommand;

            if (Props.canChangeAssignedThingsAfterStarting)
            {
                Command_LoadToContainer loadAfterCommand = new();
                loadAfterCommand.defaultLabel = "CommandLoadTransporterSingle".Translate();
                loadAfterCommand.defaultDesc = "CommandSetToLoadTransporterDesc".Translate();
                loadAfterCommand.icon = RimgateTex.LoadCommandTex;
                loadAfterCommand.Container = this;
                yield return loadAfterCommand;
            }

            yield break;
        }

        // !LoadingInProgress

        // Unload all — when idle and has contents
        if (InnerContainer.Any)
        {
            yield return new Command_Action
            {
                defaultLabel = "CommandUnload".Translate(),
                defaultDesc = "CommandUnloadDesc".Translate(parent.LabelShort),
                icon = TexCommand.ClearPrioritizedWork, // vanilla drop icon
                action = () =>
                {
                    InnerContainer?.TryDropAll(parent.Position, Map, ThingPlaceMode.Near);
                }
            };
        }

        // Load — when idle
        Command_LoadToContainer loadCommand = new();
        loadCommand.defaultLabel = "CommandLoadTransporterSingle".Translate();
        loadCommand.defaultDesc = "CommandSetToLoadTransporterDesc".Translate();
        loadCommand.icon = RimgateTex.LoadCommandTex;
        loadCommand.Container = this;
        yield return loadCommand;

        // “Push to…” — choose a destination cell and queue the job on the (attached or nearest) pawn
        yield return new Command_Target
        {
            defaultLabel = "RG_CommandPushLabel".Translate(),
            defaultDesc = "RG_CommandPushDesc".Translate(parent.LabelShort),
            icon = RimgateTex.PushCommandTex,
            targetingParams = new TargetingParameters { canTargetLocations = true, canTargetBuildings = true },
            Disabled = LoadingInProgress,
            disabledReason = "RG_CommandCartDisabled".Translate(),
            action = target => AssignPushJob(target, dump: false)
        };

        if (!InnerContainer.Any) yield break;

        // "Push & dump…" — choose a destination cell,
        // queue the job on the (attached or nearest) pawn,
        // and dump any contents
        yield return new Command_Target
        {
            defaultLabel = "RG_CommandPushAndDumpLabel".Translate(),
            defaultDesc = "RG_CommandPushAndDumpDesc".Translate(parent.LabelShort),
            icon = RimgateTex.PushAndDumpCommandTex,
            targetingParams = new TargetingParameters { canTargetLocations = true },
            Disabled = LoadingInProgress,
            disabledReason = "RG_CommandCartDisabled".Translate(),
            action = target => AssignPushJob(target, dump: true)
        };
    }

    public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
    {
        if (selPawn == null || selPawn.Downed || selPawn.Dead) yield break;
        if (!selPawn.CanReach(parent, PathEndMode.Touch, Danger.Deadly)) yield break;
        if (LoadingInProgress) yield break; // Don’t show push options while loading

        // If the pawn can’t haul/manual-dumb, show disabled entries
        if (Utils.PawnIncapableOfHauling(selPawn, out var reason))
        {
            yield return new FloatMenuOption("RG_CommandPushLabel".Translate() + " (" + reason + ")",
                null,
                MenuOptionPriority.DisabledOption);
            if (InnerContainer.Any)
                yield return new FloatMenuOption("RG_CommandPushAndDumpLabel".Translate() + " (" + reason + ")",
                    null,
                    MenuOptionPriority.DisabledOption);
            yield break;
        }

        // Prioritize push to…
        yield return FloatMenuUtility.DecoratePrioritizedTask(
            new FloatMenuOption("RG_CommandPushLabel".Translate(),
                () => BeginPushTargeting(dump: false, forcedPawn: selPawn)),
            selPawn, parent);

        if (!InnerContainer.Any) yield break;

        // Prioritize push & dump…
        yield return FloatMenuUtility.DecoratePrioritizedTask(
            new FloatMenuOption("RG_CommandPushAndDumpLabel".Translate(),
                () => BeginPushTargeting(dump: true, forcedPawn: selPawn)),
            selPawn, parent);
    }

    private void BeginPushTargeting(bool dump, Pawn forcedPawn = null)
    {
        var tp = new TargetingParameters { canTargetLocations = true, canTargetBuildings = true };
        Find.Targeter.BeginTargeting(tp, target =>
        {
            if (!target.Cell.IsValid) return;
            AssignPushJob(target, dump, forcedPawn);
        });
    }

    public void AssignPushJob(LocalTargetInfo dest, bool dump, Pawn forcedPawn = null)
    {
        if (!parent.Spawned) return;

        // choose pawn: explicit (from float menu) or best available
        Pawn pawn = forcedPawn ?? ChoosePusher(dest.Cell.IsValid ? dest.Cell : parent.Position);
        if (pawn == null)
        {
            Messages.Message("RG_MessageNoPawnToPush".Translate(parent.LabelShort),
                parent,
                MessageTypeDefOf.RejectInput);
            return;
        }

        if (!dest.HasThing && !pawn.CanReserveAndReach(dest, PathEndMode.Touch, Danger.Deadly))
        {
            Messages.Message("RG_CannotReachDestination".Translate(), parent, MessageTypeDefOf.RejectInput);
            return;
        }

        if (!FuelOK)
        {
            Messages.Message("RG_CartNoFuel".Translate(parent.LabelShort), parent, MessageTypeDefOf.RejectInput);
            return;
        }

        LocalTargetInfo targetInfo = null;
        var def = RimgateDefOf.Rimgate_PushMobileContainer;
        // If a gate was clicked, store it directly in targetC; otherwise use the cell
        if (dest.HasThing)
        {
            if (dest.Thing is not Building_Stargate sg)
            {
                Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), parent, MessageTypeDefOf.RejectInput);
                return;
            }

            targetInfo = dest.Thing;
            if (!dump) def = RimgateDefOf.Rimgate_EnterStargateWithContainer;
        }
        else
        {
            targetInfo = dest.Cell;
            if(dump) def = RimgateDefOf.Rimgate_PushAndDumpMobileContainer;
        }

        var job = JobMaker.MakeJob(def, parent);
        job.playerForced = forcedPawn != null; // only forced if user prioritized a specific pawn
        job.ignoreForbidden = true;
        job.targetC = targetInfo;

        pawn.jobs.TryTakeOrderedJob(job, JobTag.MiscWork);
    }

    // prefer attached pusher, else nearest viable colonist (simple distance score)
    private Pawn ChoosePusher(IntVec3 dest)
    {
        var map = Map;
        if (Pusher != null && Pusher.Spawned && Pusher.CanReserveAndReach(parent, PathEndMode.Touch, Danger.Deadly))
            return Pusher;

        Pawn best = null; float bestScore = float.MaxValue;
        foreach (var p in map.mapPawns.FreeColonistsSpawned)
        {
            if (p.Dead || p.Downed || p.Drafted) continue;
            if (!p.CanReserveAndReach(parent, PathEndMode.Touch, Danger.Deadly)) continue;
            if (Utils.PawnIncapableOfHauling(p, out _)) continue;

            // favor someone close to cart and not far from destination
            float score = p.Position.DistanceToSquared(parent.Position) + 0.5f * p.Position.DistanceToSquared(dest);
            if (score < bestScore) { best = p; bestScore = score; }
        }
        return best;
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        if (parent.BeingTransportedOnGravship)
            return;

        Detach();
        CancelLoad(map);
        InnerContainer.TryDropAll(parent.Position, map, ThingPlaceMode.Near);
    }

    public override string CompInspectStringExtra()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("Contents".Translate() + ": " + InnerContainer.ContentsString.CapitalizeFirst());
        if (Props.showMassInInspectString)
        {
            TaggedString taggedString = "Mass".Translate() + ": "
                + MassUsage.ToString("F0") + " / "
                + MassCapacity.ToString("F0") + " kg";
            sb.AppendLine().Append(MassUsage > MassCapacity
                ? taggedString.Colorize(ColorLibrary.RedReadable)
                : ((string)taggedString));
        }

        sb.AppendLine().Append(LoadingInProgress
            ? "RG_CartStatusLoading".Translate()
            : "RG_CartStatusIdle".Translate());

        return sb.ToString();
    }

    public void AddToTheToLoadList(TransferableOneWay t, int count)
    {
        if (!t.HasAnyThing || count <= 0)
            return;

        LeftToLoad ??= new List<TransferableOneWay>();

        TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatching(t.AnyThing, LeftToLoad, TransferAsOneMode.PodsOrCaravanPacking);
        if (transferableOneWay != null)
        {
            for (int i = 0; i < t.things.Count; i++)
            {
                if (!transferableOneWay.things.Contains(t.things[i]))
                    transferableOneWay.things.Add(t.things[i]);
            }

            if (transferableOneWay.CanAdjustBy(count).Accepted)
                transferableOneWay.AdjustBy(count);
        }
        else
        {
            TransferableOneWay transferableOneWay2 = new TransferableOneWay();
            LeftToLoad.Add(transferableOneWay2);
            transferableOneWay2.things.AddRange(t.things);
            transferableOneWay2.AdjustTo(count);
        }
    }

    public void RemoveFromLoadList(TransferableOneWay t, bool sendMessageOnFinished = true)
    {
        LeftToLoad.Remove(t);

        // If nothing is left, flip the flag so WorkGiver stops scanning
        if (LeftToLoad.Count == 0 && sendMessageOnFinished)
            Messages.Message("RG_MessageFinishedLoadingCart".Translate(parent.LabelCap), parent, MessageTypeDefOf.TaskCompletion);
    }

    public bool LeftToLoadContains(Thing thing)
    {
        if (LeftToLoad == null)
            return false;

        for (int i = 0; i < LeftToLoad.Count; i++)
        {
            for (int j = 0; j < LeftToLoad[i].things.Count; j++)
            {
                if (LeftToLoad[i].things[j] == thing)
                    return true;
            }
        }

        return false;
    }

    // How many of THIS thing do we still need?
    public int RemainingToLoadFor(Thing t)
    {
        if (LeftToLoad == null || t == null) return 0;

        // If your dialog stores exact Thing refs:
        for (int i = 0; i < LeftToLoad.Count; i++)
        {
            for (int j = 0; j < LeftToLoad[i].things.Count; j++)
            {
                if (LeftToLoad[i].things[j] == t && LeftToLoad[i].things[j].Spawned)
                    return LeftToLoad[i].CountToTransfer;
            }
        }

        return 0;
    }

    public void Notify_ThingAdded(Thing t)
    {
        _massDirty = true;
        // decrement by full placed amount for NEW stacks
        SubtractFromToLoadList(t, t.stackCount);
    }

    public void Notify_ThingRemoved()
    {
        _massDirty = true;
    }

    public void Notify_ThingAddedAndMergedWith(Thing intoStack, int mergedCount)
    {
        _massDirty = true;
        // decrement by the delta merged into an existing stack
        SubtractFromToLoadList(intoStack, mergedCount, sendMessageOnFinished: true);
    }

    public void CancelLoad(Map map)
    {
        if (!LoadingInProgress)
            return;

        LeftToLoad?.Clear();
        _stalledSinceTick = -1;
    }

    public int SubtractFromToLoadList(Thing t, int count, bool sendMessageOnFinished = true)
    {
        if (LeftToLoad == null || t == null || count <= 0) return 0;

        // Match the transferable that corresponds to this stack (including split-offs)
        var tr = TransferableUtility.TransferableMatchingDesperate(
            t, LeftToLoad, TransferAsOneMode.PodsOrCaravanPacking);

        if (tr == null || tr.CountToTransfer <= 0)
            return 0;

        int take = Mathf.Min(tr.CountToTransfer, count);
        tr.AdjustBy(-take);
        if (tr.CountToTransfer <= 0)
            RemoveFromLoadList(tr);

        return take; // how much we actually decremented
    }

    private void FinalizeShortfall()
    {
        // Compute any remainder for UX
        int shortfall = 0;
        if (LeftToLoad != null)
            for (int i = 0; i < LeftToLoad.Count; i++)
                shortfall += LeftToLoad[i].CountToTransfer;

        LeftToLoad?.Clear();
        _stalledSinceTick = -1;

        if (Props?.notifyOnFinalize ?? true)
        {
            if (shortfall > 0)
            {
                Messages.Message(
                    "RG_MessageCantLoadMoreIntoCart".Translate(
                        Faction.OfPlayer.def.pawnsPlural,
                        parent.Label,
                        FirstThingLeftToLoad?.LabelShort),
                    parent,
                    MessageTypeDefOf.NeutralEvent);
            }
            else
            {
                Messages.Message(
                    "RG_MessageFinishedLoadingCart".Translate(parent.LabelCap),
                    parent,
                    MessageTypeDefOf.TaskCompletion);
            }
        }
    }

    // helper to cache from a live cart before despawn
    public void SaveVisualFrom(Building_MobileContainer cart)
    {
        SavedStuff = cart.Stuff;
        SavedHitPoints = cart.HitPoints;
        SavedDrawColor = cart.DrawColor;
        SavedDrawColorTwo = cart.DrawColorTwo;

        var paint = cart.TryGetComp<CompColorable>();
        SavedHasPaint = paint != null && paint.Active;
    }  
}