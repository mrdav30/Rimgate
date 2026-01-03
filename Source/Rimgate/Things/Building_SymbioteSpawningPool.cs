using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Rimgate;

public class Building_SymbioteSpawningPool : Building, IThingHolder, IThingHolderEvents<Thing>, ISearchableContents, IHaulDestination
{
    public Comp_SymbiotePool SymbiotePool => GetComp<Comp_SymbiotePool>();

    public ThingOwner<Thing> InnerContainer => _innerContainer;

    public ThingOwner SearchableContents => _innerContainer;

    public IReadOnlyList<Thing> HeldItems => _innerContainer.InnerListForReading;

    public bool StorageTabVisible => Faction.IsOfPlayerFaction();

    public bool HaulDestinationEnabled => true;

    public bool HasAnyContents => GetDirectlyHeldThings()?.Count > 0;

    public bool HasQueen => _innerContainer?.Any(t => t.def == SymbiotePool?.Props.symbioteQueenDef) ?? false;

    public int MaxHeldItems => def.building.maxItemsInCell;

    private StorageSettings _storageSettings;

    private ThingOwner<Thing> _innerContainer;

    public Building_SymbioteSpawningPool()
    {
        _innerContainer = new ThingOwner<Thing>(this);
    }

    public override void PostMake()
    {
        base.PostMake();
        _innerContainer.dontTickContents = true;
        _storageSettings = new StorageSettings(this);
        if (def.building.defaultStorageSettings != null)
        {
            _storageSettings.CopyFrom(def.building.defaultStorageSettings);
        }
    }

    public void Notify_ItemAdded(Thing item)
    {
        if (item.TryGetComp<Comp_SymbioteRottable>(out Comp_SymbioteRottable comp))
            comp.disabled = true;
    }

    public void Notify_ItemRemoved(Thing item)
    {
        if (item.TryGetComp<Comp_SymbioteRottable>(out Comp_SymbioteRottable comp))
            comp.disabled = false;
    }

    public ThingOwner GetDirectlyHeldThings() => _innerContainer;

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, _innerContainer);
    }

    public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
    {
        if (!Faction.IsOfPlayerFaction()) yield break;

        foreach (var opt in base.GetFloatMenuOptions(selPawn))
            yield return opt;

        if (SymbiotePool == null || HasQueen)
            yield break;

        if (!selPawn.CanReach(this, PathEndMode.InteractionCell, selPawn.NormalMaxDanger()))
            yield break;

        // Find a queen the pawn can reach & reserve
        if (!TryGetClosestQueen(selPawn, out Thing queen))
        {
            string req = "none available";
            yield return new FloatMenuOption("RG_CannotInsertQueen".Translate(req), null);
            yield break;
        }

        yield return FloatMenuUtility.DecoratePrioritizedTask(
            new FloatMenuOption("RG_InsertQueen".Translate(), () =>
            {
                Job job = JobMaker.MakeJob(RimgateDefOf.Rimgate_InsertSymbioteQueen, queen, this);
                selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }),
            selPawn,
            this);
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        EjectContents();
        base.Destroy(mode);
    }

    public void EjectContents()
    {
        if (!HasAnyContents)
            return;

        var container = GetDirectlyHeldThings();
        var toDrop = container.ToList();
        foreach (Thing thing in toDrop)
            container.TryDrop(thing, ThingPlaceMode.Near, out var dropped);
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        if (!Faction.IsOfPlayerFaction()) yield break;

        foreach (var g in base.GetGizmos())
            yield return g;

        if (SymbiotePool == null || HasQueen)
            yield break;

        yield return new Command_Action
        {
            defaultLabel = "RG_InsertQueen".Translate(),
            defaultDesc = "RG_InsertQueenDesc".Translate(),
            icon = SymbiotePool?.QueenIcon,
            action = () =>
            {
                var parms = new TargetingParameters
                {
                    canTargetPawns = true,
                    canTargetBuildings = false,
                    canTargetAnimals = false,
                    validator = t =>
                    {
                        Pawn p = t.Thing as Pawn;
                        return p != null
                               && p.Faction.IsOfPlayerFaction()
                               && !p.Downed
                               && p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation);
                    }
                };

                Find.Targeter.BeginTargeting(parms, target =>
                {
                    Pawn pawn = target.Thing as Pawn;
                    if (pawn == null) return;

                    // Recheck if pool might have recieved
                    if (HasQueen == true)
                    {
                        Messages.Message("RG_InsertQueenRejectOccupied".Translate(), this, MessageTypeDefOf.RejectInput);
                        return;
                    }

                    if (!TryGetClosestQueen(pawn, out Thing queen))
                    {
                        Messages.Message("RG_InsertQueenRejectNoQueen".Translate(), this, MessageTypeDefOf.RejectInput);
                        return;
                    }

                    Job job = JobMaker.MakeJob(RimgateDefOf.Rimgate_InsertSymbioteQueen, queen, this);
                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                });
            }
        };
    }

    public bool TryGetClosestQueen(Pawn pawn, out Thing queen)
    {
        var def = SymbiotePool?.Props.symbioteQueenDef;
        if (def == null)
        {
            queen = null;
            return false;
        }
        queen = GenClosest.ClosestThingReachable(
            pawn.Position,
            pawn.Map,
            ThingRequest.ForDef(SymbiotePool?.Props.symbioteQueenDef),
            PathEndMode.Touch,
            TraverseParms.For(pawn),
            999f,
            t => !t.IsForbidden(pawn) && pawn.CanReserve(t));
        return queen != null;
    }

    public bool Accepts(Thing thing)
    {
        if (!_storageSettings.AllowedToAccept(thing))
            return false;
        if (thing.def == SymbiotePool?.Props.symbioteQueenDef && HasQueen)
            return false;
        return _innerContainer.CanAcceptAnyOf(thing);
    }

    public StorageSettings GetStoreSettings() => _storageSettings;

    public StorageSettings GetParentStoreSettings() => def.building.fixedStorageSettings;

    public void Notify_SettingsChanged() { }

    public bool TryAcceptQueen(Thing thing, bool canMerge = false)
    {
        if (thing == null || thing.def != SymbiotePool?.Props.symbioteQueenDef)
            return false;

        if (!Faction.IsOfPlayerFaction() || HasQueen)
            return false;

        return _innerContainer.TryAddOrTransfer(thing, canMerge);
    }

    public bool TryDrop(
        Thing thing,
        IntVec3 cell,
        ThingPlaceMode mode,
        int count,
        out Thing dropped)
    {
        if (_innerContainer.TryDrop(thing, cell, base.Map, mode, count, out dropped))
            return true;

        dropped = null;
        return false;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref _innerContainer, "innerContainer", this);
        Scribe_Deep.Look(ref _storageSettings, "storageSettings", this);
    }
}
