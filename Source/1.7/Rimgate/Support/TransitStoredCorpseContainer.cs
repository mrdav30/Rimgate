using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace Rimgate;

// Keep a corpse-containing cargo root intact instead of rebuilding its nested owners.
public sealed class TransitStoredCorpseContainer : IExposable, IThingHolder
{
    private readonly WorldObject_GateTransitSite _parent;
    private ThingOwner<Thing> _contents;

    public IntVec3 Position;
    public Rot4 Rotation;

    public Thing Root => _contents.Count == 0 ? null : _contents[0];
    public ThingOwner<Thing> Contents => _contents;
    public IThingHolder ParentHolder => _parent;

    // Scribe_Collections.Look(..., LookMode.Deep, site) supplies the owning site
    // before loading this entry or its nested ThingOwner.
    public TransitStoredCorpseContainer(WorldObject_GateTransitSite parent)
    {
        _parent = parent ?? throw new ArgumentNullException(nameof(parent));
        _contents = new ThingOwner<Thing>(this);
    }

    public TransitStoredCorpseContainer(WorldObject_GateTransitSite parent, Thing root)
        : this(parent)
    {
        if (root == null || root.Destroyed || !root.Spawned || root.Map != parent.Map)
            throw new InvalidOperationException("Capture a spawned cargo root on the owning transit-site map.");

        Position = root.Position;
        Rotation = root.Rotation;
        if (root is MinifiedThing { InnerThing: Building_MobileContainer innerCart })
        {
            innerCart.CancelLoad(null);
            innerCart.ClearDesignations();
        }
        if (root is Building_MobileContainer cart) cart.DeSpawnForTransitStorage();
        else root.DeSpawn();
        if (!_contents.TryAdd(root, false))
            throw new InvalidOperationException("Could not retain corpse-containing transit cargo ownership.");
    }

    public ThingOwner GetDirectlyHeldThings() => _contents;

    public void GetChildHolders(List<IThingHolder> outChildren) =>
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, _contents);

    public void DestroyContents() => _contents.ClearAndDestroyContents();

    public void ExposeData()
    {
        Scribe_Values.Look(ref Position, "position");
        Scribe_Values.Look(ref Rotation, "rotation");
        Scribe_Deep.Look(ref _contents, "contents", this);
        _contents ??= new ThingOwner<Thing>(this);
    }
}
