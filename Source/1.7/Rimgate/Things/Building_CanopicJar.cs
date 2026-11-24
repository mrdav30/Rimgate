using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace Rimgate;

public class Building_CanopicJar : Building_Art, IThingHolder, IThingHolderEvents<Thing>, ISearchableContents, IHaulDestination
{
    public ThingOwner<Thing> InnerContainer => _innerContainer;

    public ThingOwner SearchableContents => _innerContainer;

    public IReadOnlyList<Thing> HeldItems => _innerContainer.InnerListForReading;

    public Thing ContainedThing => HeldItems.FirstOrDefault();

    public bool StorageTabVisible => Faction.IsOfPlayerFaction();

    public bool HaulDestinationEnabled => !HasAnyContents;

    public int HeldItemsCount => _innerContainer?.Count ?? 0;

    public bool HasAnyContents => _innerContainer?.Count > 0;

    public int MaxHeldItems => def.building.maxItemsInCell;

    private StorageSettings _storageSettings;

    private ThingOwner<Thing> _innerContainer;

    public Building_CanopicJar()
    {
        _innerContainer = new ThingOwner<Thing>(this);
    }

    public override void PostMake()
    {
        base.PostMake();
        _innerContainer.dontTickContents = true;
        _storageSettings = new StorageSettings(this);
        if (def.building.defaultStorageSettings != null)
            _storageSettings.CopyFrom(def.building.defaultStorageSettings);
    }

    public void Notify_ItemAdded(Thing item)
    {
        if (item.TryGetComp(out CompRottable comp))
            comp.disabled = true;
    }

    public void Notify_ItemRemoved(Thing item)
    {
        if (item.TryGetComp(out CompRottable comp))
            comp.disabled = false;
    }

    public void Notify_SettingsChanged()
    {
        for (int i = 0; i < _innerContainer.Count; i++)
        {
            Thing item = _innerContainer[i];
            if (!Accepts(item))
            {
                _innerContainer.TryDrop(item, ThingPlaceMode.Near, out var dropped);
                i--;
            }
        }
    }

    public ThingOwner GetDirectlyHeldThings() => _innerContainer;

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, _innerContainer);
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        if (mode == DestroyMode.KillFinalize)
            InnerContainer.ClearAndDestroyContents();
        else
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

    public bool Accepts(Thing thing)
    {
        if (!_storageSettings.AllowedToAccept(thing))
            return false;
        return _innerContainer.CanAcceptAnyOf(thing);
    }

    public StorageSettings GetStoreSettings() => _storageSettings;

    public StorageSettings GetParentStoreSettings() => def.building.fixedStorageSettings;

    public override string GetInspectString()
    {
        StringBuilder sb = new();
        sb.Append(base.GetInspectString());
        if (!Spawned) return sb.ToString();

        if (ContainedThing != null)
        {
            sb.AppendLineIfNotEmpty();
            sb.Append($"{"Contains".Translate()}: ");
            sb.Append(ContainedThing.LabelShortCap);
        }

        return sb.ToString();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref _innerContainer, "innerContainer", this);
        Scribe_Deep.Look(ref _storageSettings, "storageSettings", this);
    }
}
