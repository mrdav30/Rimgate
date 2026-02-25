using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Rimgate;

public class Building_MobileContainer_Ext : DefModExtension
{
    public float massCapacity = 150f;

    public bool canChangeAssignedThingsAfterStarting = true;

    public float frontOffset = 1.0f;  // how far in front of pusher to draw/spawn

    public float slowdownSeverity = 0.1f; // pusher slowdown

    // only items within this radius are eligible
    public float loadRadius = 10f;

    public bool sealPreventsContentTick = false; // if true, sealing the container (e.g. by fueling) will prevent contents from ticking (e.g. rotting)

    public bool showMassInInspectString = true;

    public int stallFinalizeDelayTicks = 600;  // ~10s at 60 tps

    public bool notifyOnFinalize = true;  // show one message when auto-finalizing

    // if true, colonists can use items inside this cart directly for jobs/searches
    public bool allowColonistsUseContents = true;

    public float SqrdLoadRadius => loadRadius * loadRadius;
}

public class Building_MobileContainer : Building, ILoadReferenceable, IHaulSource, ISearchableContents, IThingHolder, IThingHolderEvents<Thing>
{
    public ThingOwner_MobileContainer InnerContainer;

    public List<TransferableOneWay> LeftToLoad;

    public Building_MobileContainer_Ext Ext => _ext ??= def.GetModExtension<Building_MobileContainer_Ext>() ?? new();

    public CompRefuelable Refuelable => _cachedRefuelable ??= GetComp<CompRefuelable>();

    public bool HaulSourceEnabled => _allowColonistsUseContents;

    public bool StorageTabVisible => false;

    public ThingOwner SearchableContents => InnerContainer;

    public bool AnythingToSearch => InnerContainer != null && InnerContainer.Any;

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
            if (!LoadingInProgress || !Spawned) return false;

            IReadOnlyList<Pawn> pawns = Map.mapPawns.AllPawnsSpawned;

            // 1) Only count CURRENT jobs that still have a valid, selected thing
            for (int i = 0; i < pawns.Count; i++)
            {
                var cur = pawns[i];
                if (cur.CurJobDef != RimgateDefOf.Rimgate_HaulToContainer) continue;

                if (cur.jobs.curDriver is JobDriver_HaulToMobileContainer jd
                    && jd.MobileContainer?.ThingID == ThingID)
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

    public float MassCapacity => Ext.massCapacity;

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

    public bool OverMassCapacity => MassUsage > MassCapacity;

    public bool FuelOK => Refuelable == null || Refuelable.HasFuel;

    public bool UsesFuelWhilePushing => Refuelable != null;

    public float ConsumptionRatePerTick => Refuelable?.Props?.fuelConsumptionRate / GenDate.TicksPerDay ?? 0f;

    public bool AllowColonistsUseContents
    {
        get => _allowColonistsUseContents;
        set => _allowColonistsUseContents = value;
    }

    private List<Thing> _tmpThings = [];

    private bool _massDirty = true;

    private float _cachedMassUsage;

    private int _stalledSinceTick = -1;

    private bool _isSealed;

    private bool _wantsToBePushed;

    private bool _wantsToBeDumped;

    private bool _allowColonistsUseContents = true;

    private LocalTargetInfo _cachedDesignationTarget;

    private Building_MobileContainer_Ext _ext;

    private CompRefuelable _cachedRefuelable;

    public Building_MobileContainer()
    {
        InnerContainer ??= new ThingOwner_MobileContainer(this);
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        InnerContainer.dontTickContents = !Ext.sealPreventsContentTick;
        _isSealed = !Ext.sealPreventsContentTick && FuelOK;
    }

    protected override void Tick()
    {
        base.TickLong();

        InnerContainer.DoTick();

        // If we're set to prevent ticking when sealed, toggle sealing based on fuel state. 
        // This ensures that rot and other comps that rely on ticking are properly paused while sealed, and resume when unsealed.
        if (Ext.sealPreventsContentTick && this.IsHashIntervalTick(GenTicks.TickRareInterval))
        {
            if (!FuelOK)
            {
                InnerContainer.dontTickContents = false;

                foreach (var t in InnerContainer.InnerListForReading)
                {
                    if (t.TryGetComp(out CompRottable comp))
                        comp.disabled = false;
                }
                _isSealed = false;
            }
            else if (!_isSealed)
            {
                InnerContainer.dontTickContents = true;
                foreach (var t in InnerContainer.InnerListForReading)
                {
                    if (t.TryGetComp(out CompRottable comp))
                        comp.disabled = true;
                }
                _isSealed = true;
            }
        }

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
        if (elapsed >= Ext.stallFinalizeDelayTicks)
            FinalizeShortfall();
    }

    public override void DrawExtraSelectionOverlays()
    {
        if (!Spawned || Ext == null) return;
        GenDraw.DrawRadiusRing(Position, Ext.loadRadius);
    }

    public override string GetInspectString()
    {
        StringBuilder sb = new();
        sb.Append(base.GetInspectString());

        if (sb.Length > 0)
            sb.AppendLine();

        sb.Append("Contents".Translate() + ": " + InnerContainer.ContentsString.CapitalizeFirst());
        if (Ext.showMassInInspectString)
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

        sb.AppendLine().Append("RG_LoadOutRange".Translate(Ext.loadRadius));

        return sb.ToString();
    }

    public override void ExposeData()
    {
        base.ExposeData();

        bool flag = !SpawnedOrAnyParentSpawned;
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
                        LogUtil.Error("Trying to save a non-world pawn (" + pawn?.ToString() + ") as a reference in a mobile container.");
                }
            }

            _tmpThings.Clear();
        }

        Scribe_Deep.Look(ref InnerContainer, "InnerContainer", this);
        Scribe_Collections.Look(ref LeftToLoad, "LeftToLoad", LookMode.Deep);
        Scribe_Values.Look(ref _stalledSinceTick, "_stalledSinceTick", defaultValue: -1);
        Scribe_Values.Look(ref _isSealed, "_isSealed", false);
        Scribe_Values.Look(ref _wantsToBePushed, "_wantsToBePushed", false);
        Scribe_Values.Look(ref _wantsToBeDumped, "_wantsToBeDumped", false);
        Scribe_Values.Look(ref _allowColonistsUseContents, "_allowColonistsUseContents", true);
        Scribe_TargetInfo.Look(ref _cachedDesignationTarget, "_cachedDesignationTarget");
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        base.DeSpawn(mode);

        if (BeingTransportedOnGravship)
            return;

        CancelLoad(Map);
        InnerContainer.TryDropAll(Position, Map, ThingPlaceMode.Near);
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos())
            yield return gizmo;

        yield return new Command_Toggle
        {
            defaultLabel = "RG_CommandAllowUseCartContents_Label".Translate(),
            defaultDesc = "RG_CommandAllowUseCartContents_Desc".Translate(LabelShort),
            icon = _allowColonistsUseContents ? TexCommand.ForbidOff : TexCommand.ForbidOn,
            isActive = () => _allowColonistsUseContents,
            toggleAction = () => _allowColonistsUseContents = !_allowColonistsUseContents
        };

        if (LoadingInProgress)
        {
            Command_Action cancelLoadCommand = new()
            {
                defaultLabel = "CommandCancelLoad".Translate(),
                defaultDesc = "CommandCancelLoadDesc".Translate(),
                icon = RimgateTex.CancelCommandTex,
                action = delegate
                    {
                        SoundDefOf.Designate_Cancel.PlayOneShotOnCamera();
                        CancelLoad(Map);
                    }
            };
            yield return cancelLoadCommand;

            if (Ext.canChangeAssignedThingsAfterStarting)
            {
                Command_LoadToContainer loadAfterCommand = new()
                {
                    defaultLabel = "CommandLoadTransporterSingle".Translate(),
                    defaultDesc = "CommandSetToLoadTransporterDesc".Translate(),
                    icon = RimgateTex.LoadCommandTex,
                    Container = this
                };
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
                defaultDesc = "CommandUnloadDesc".Translate(LabelShort),
                icon = TexCommand.ClearPrioritizedWork, // vanilla drop icon
                action = () =>
                {
                    InnerContainer?.TryDropAll(Position, Map, ThingPlaceMode.Near);
                }
            };
        }

        // Load — when idle
        Command_LoadToContainer loadCommand = new()
        {
            defaultLabel = "CommandLoadTransporterSingle".Translate(),
            defaultDesc = "CommandSetToLoadTransporterDesc".Translate(),
            icon = RimgateTex.LoadCommandTex,
            Container = this
        };
        yield return loadCommand;

        // Push to…
        yield return new Command_Toggle
        {
            defaultLabel = "RG_CommandPushLabel".Translate(),
            defaultDesc = "RG_CommandPushDesc".Translate(LabelShort),
            icon = RimgateTex.PushCommandTex,
            Disabled = LoadingInProgress,
            disabledReason = "RG_CommandCartDisabled".Translate(),
            isActive = () => _wantsToBePushed,
            toggleAction = () => SetGizmoDesignation(false)
        };

        if (!InnerContainer.Any) yield break;

        // Push & dump…
        yield return new Command_Toggle
        {
            defaultLabel = "RG_CommandDumpLabel".Translate(),
            defaultDesc = "RG_CommandDumpDesc".Translate(LabelShort),
            icon = RimgateTex.PushAndDumpCommandTex,
            Disabled = LoadingInProgress,
            disabledReason = "RG_CommandCartDisabled".Translate(),
            isActive = () => _wantsToBeDumped,
            toggleAction = () => SetGizmoDesignation(true)
        };
    }

    public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
    {
        if (selPawn == null || selPawn.Downed || selPawn.Dead) yield break;
        if (!selPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly)) yield break;
        if (LoadingInProgress) yield break; // Don’t show push options while loading

        // If the pawn can’t haul/manual-dumb, show disabled entries
        if (selPawn.IncapableOfHauling(out var reason))
        {
            yield return new FloatMenuOption("RG_CommandPushLabel".Translate() + " (" + reason + ")",
                null,
                MenuOptionPriority.DisabledOption);
            if (InnerContainer.Any)
                yield return new FloatMenuOption("RG_CommandDumpLabel".Translate() + " (" + reason + ")",
                    null,
                    MenuOptionPriority.DisabledOption);
            yield break;
        }

        // Prioritize push to…
        yield return FloatMenuUtility.DecoratePrioritizedTask(
            new FloatMenuOption("RG_CommandPushLabel".Translate(), () => BeginFloatTargeting(selPawn, false)),
            selPawn, this);

        if (!InnerContainer.Any) yield break;

        // Prioritize push & dump…
        yield return FloatMenuUtility.DecoratePrioritizedTask(
            new FloatMenuOption("RG_CommandDumpLabel".Translate(), () => BeginFloatTargeting(selPawn, true)),
            selPawn, this);
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
    {
        foreach (StatDrawEntry stat in base.SpecialDisplayStats())
            yield return stat;

        yield return new StatDrawEntry(
            StatCategoryDefOf.Building,
            "RG_Stat_Cart_LoadoutRange_Label".Translate(),
            Ext.loadRadius.ToString("F0") + " W",
            "RG_Stat_Cart_LoadoutRange_Desc".Translate(),
            4994);
    }

    private void SetGizmoDesignation(bool dump)
    {
        if (dump)
            _wantsToBeDumped = !_wantsToBeDumped;
        else
            _wantsToBePushed = !_wantsToBePushed;

        var dm = Map?.designationManager;
        if (dm == null) return;

        Designation designation = dm.DesignationOn(this, RimgateDefOf.Rimgate_DesignationPushCart);
        if (designation == null)
        {
            var tp = new TargetingParameters { canTargetLocations = true, canTargetBuildings = !dump };
            Find.Targeter.BeginTargeting(tp, target =>
            {
                if (!target.Cell.IsValid) return;
                if (!Utils.FindStandCellFor(InteractionCell, target.Cell, Map, out _))
                {
                    Messages.Message("RG_CannotReachDestination".Translate(),
                        this,
                        MessageTypeDefOf.RejectInput);
                    return;
                }

                _cachedDesignationTarget = target;
                dm.AddDesignation(new Designation(this, RimgateDefOf.Rimgate_DesignationPushCart));
            });
        }
        else
        {
            _cachedDesignationTarget = null;
            designation?.Delete();
        }
    }

    private void BeginFloatTargeting(Pawn pawn, bool dump)
    {
        var tp = new TargetingParameters { canTargetLocations = true, canTargetBuildings = !dump };
        Find.Targeter.BeginTargeting(tp, target =>
        {
            if (!target.Cell.IsValid) return;
            if (!Utils.FindStandCellFor(InteractionCell, target.Cell, Map, out _))
            {
                Messages.Message("RG_CannotReachDestination".Translate(),
                    this,
                    MessageTypeDefOf.RejectInput);
                return;
            }
            ClearDesignations();
            var job = GetPushJob(pawn, target);
            if (job == null) return;
            job.playerForced = true;
            if (dump)
                job.haulMode = HaulMode.ToCellNonStorage;

            pawn.jobs.TryTakeOrderedJob(job, JobTag.MiscWork);
        });
    }

    public void ClearDesignations()
    {
        _wantsToBePushed = false;
        _wantsToBeDumped = false;

        var dm = Map?.designationManager;
        if (dm == null) return;

        dm.RemoveAllDesignationsOn(this);
    }

    public Job GetDesignatedPushJob(Pawn pawn)
    {
        if (!_cachedDesignationTarget.IsValid)
            return null;

        var job = GetPushJob(pawn, _cachedDesignationTarget);
        if (job == null) return null;
        _cachedDesignationTarget = null;
        if (_wantsToBeDumped)
            job.haulMode = HaulMode.ToCellNonStorage;
        return job;
    }

    public Job GetPushJob(Pawn pawn, LocalTargetInfo dest)
    {
        if (!Spawned) return null;

        if (pawn == null)
        {
            Messages.Message("RG_MessageNoPawnToPush".Translate(LabelShort),
                this,
                MessageTypeDefOf.RejectInput);
            return null;
        }

        if (dest == null)
        {
            Messages.Message("RG_CannotReachDestination".Translate(), this, MessageTypeDefOf.RejectInput);
            return null;
        }

        if (!FuelOK)
        {
            Messages.Message("RG_CartNoFuel".Translate(LabelShort), this, MessageTypeDefOf.RejectInput);
            return null;
        }

        LocalTargetInfo targetInfo;
        var def = RimgateDefOf.Rimgate_PushContainer;
        // If a gate was clicked, store it directly in targetC; otherwise use the cell
        if (dest.HasThing)
        {
            if (dest.Thing is not Building_Gate sg || !sg.IsActive)
            {
                Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), this, MessageTypeDefOf.RejectInput);
                return null;
            }

            if (!pawn.CanReach(sg.InteractionCell, PathEndMode.OnCell, Danger.Deadly))
            {
                Messages.Message("RG_CannotReachDestination".Translate(), this, MessageTypeDefOf.RejectInput);
                return null;
            }

            targetInfo = sg;
            def = RimgateDefOf.Rimgate_EnterGateWithContainer;
        }
        else
        {
            if (!pawn.CanReach(dest.Cell, PathEndMode.OnCell, Danger.Deadly))
            {
                Messages.Message("RG_CannotReachDestination".Translate(), this, MessageTypeDefOf.RejectInput);
                return null;
            }

            targetInfo = dest.Cell;
        }

        var job = JobMaker.MakeJob(def, this);
        job.ignoreForbidden = true;
        job.targetC = targetInfo;

        return job;
    }

    public ThingOwner GetDirectlyHeldThings() => InnerContainer;

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    public StorageSettings GetStoreSettings() => def.building.fixedStorageSettings;

    public StorageSettings GetParentStoreSettings() => def.building.fixedStorageSettings;

    public void Notify_SettingsChanged() { }

    // cart -> proxy, caching state for the proxy to use when converting back or just for reference while pushing
    public Thing_MobileCartProxy GetProxyForCart()
    {
        // Create proxy
        if (ThingMaker.MakeThing(RimgateDefOf.Rimgate_MobileCartProxy) is not Thing_MobileCartProxy proxy)
        {
            LogUtil.Error("Proxy missing.");
            return null;
        }

        proxy.SavedDef = def;
        proxy.SavedStuff = Stuff;
        proxy.SavedHitPoints = HitPoints;
        proxy.SavedDrawColor = DrawColor;
        proxy.SavedDrawColorTwo = DrawColorTwo;

        var paint = this.TryGetComp<CompColorable>();
        proxy.SavedHasPaint = paint != null && paint.Active;

        proxy.SavedUseContentsSetting = AllowColonistsUseContents;

        var refuelable = Refuelable;
        if (refuelable != null)
        {
            // copy current fuel
            proxy.PushingFuel = refuelable.Fuel;
            // TODO: lerp with more weight
            proxy.FuelPerTick = ConsumptionRatePerTick;
        }
        else
        {
            proxy.PushingFuel = 0f;
            proxy.FuelPerTick = 0f;
        }

        return proxy;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MoveContentsToProxy(Thing_MobileCartProxy proxy)
    {
        if (proxy == null) return;

        InnerContainer.TryTransferAllToContainer(proxy.InnerContainer);
    }

    public void AddToTheToLoadList(TransferableOneWay t, int count)
    {
        if (!t.HasAnyThing || count <= 0)
            return;

        LeftToLoad ??= [];

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
            Messages.Message("RG_MessageFinishedLoadingCart".Translate(LabelCap), this, MessageTypeDefOf.TaskCompletion);
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

    public void Notify_ItemAdded(Thing t)
    {
        _massDirty = true;

        if (_isSealed && t.TryGetComp(out CompRottable comp))
            comp.disabled = true;

        // decrement by full placed amount for NEW stacks
        SubtractFromToLoadList(t, t.stackCount);
    }

    public void Notify_ItemRemoved(Thing t)
    {
        if (t.TryGetComp(out CompRottable comp) && comp.disabled)
            comp.disabled = false;

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

        if (Ext.notifyOnFinalize)
        {
            if (shortfall > 0)
            {
                Messages.Message(
                    "RG_MessageCantLoadMoreIntoCart".Translate(
                        Faction.OfPlayer.def.pawnsPlural,
                        Label,
                        FirstThingLeftToLoad?.LabelShort),
                    this,
                    MessageTypeDefOf.NeutralEvent);
            }
            else
            {
                Messages.Message(
                    "RG_MessageFinishedLoadingCart".Translate(LabelCap),
                    this,
                    MessageTypeDefOf.TaskCompletion);
            }
        }
    }
}
