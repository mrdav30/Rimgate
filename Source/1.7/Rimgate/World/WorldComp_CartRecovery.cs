using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Rimgate;

// A last-resort owner for carts that cannot be placed without destroying another thing.
// Carry/inventory cleanup is unsuitable: native drop fallback may wipe an existing building.
public class WorldComp_CartRecovery : WorldComponent, IThingHolder
{
    public sealed class PendingCart : IExposable
    {
        private Building_MobileContainer _cart;
        private MapParent _mapParent;
        private IntVec3 _anchor;

        public Building_MobileContainer Cart => _cart;
        public MapParent MapParent => _mapParent;
        public IntVec3 Anchor => _anchor;

        public PendingCart() { }
        public PendingCart(Building_MobileContainer cart, MapParent mapParent, IntVec3 anchor)
        {
            _cart = cart;
            _mapParent = mapParent;
            _anchor = anchor;
        }

        public void ExposeData()
        {
            // A removed destination cannot be referenced in a later save. The cart stays owned.
            if (Scribe.mode == LoadSaveMode.Saving && _mapParent?.Destroyed == true)
                _mapParent = null;
            Scribe_References.Look(ref _cart, "cart");
            Scribe_References.Look(ref _mapParent, "mapParent");
            Scribe_Values.Look(ref _anchor, "anchor");
        }
    }

    private ThingOwner<Building_MobileContainer> _carts;
    private List<PendingCart> _pending = new();

    public IReadOnlyList<PendingCart> Pending => _pending;
    public IThingHolder ParentHolder => world;

    public WorldComp_CartRecovery(World world) : base(world)
    {
        _carts = new ThingOwner<Building_MobileContainer>(this, false, LookMode.Deep);
    }

    public ThingOwner GetDirectlyHeldThings() => _carts;
    public void GetChildHolders(List<IThingHolder> outChildren) =>
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, _carts);

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref _carts, "carts", this);
        Scribe_Collections.Look(ref _pending, "pending", LookMode.Deep);
        _carts ??= new ThingOwner<Building_MobileContainer>(this, false, LookMode.Deep);
        _pending ??= new List<PendingCart>();
    }

    public override void WorldComponentTick()
    {
        if (Find.TickManager.TicksGame % 250 != 0) return;
        for (int i = _pending.Count - 1; i >= 0; i--)
        {
            var pending = _pending[i];
            var map = pending.MapParent?.Map;
            if (map == null || !_carts.Contains(pending.Cart)) continue;
            if (TryPlace(pending.Cart, pending.Anchor, map))
            {
                _carts.Remove(pending.Cart);
                _pending.RemoveAt(i);
            }
        }
    }

    public static bool TryRestore(Thing_MobileCartProxy proxy, Pawn pawn)
    {
        var map = proxy.Spawned ? proxy.Map : pawn.MapHeld ?? proxy.MapHeld;
        if (map == null) return false;
        var anchor = proxy.Spawned ? proxy.Position : pawn.PositionHeld;
        var cart = proxy.ConvertProxyToCart(pawn.Faction);
        bool placed = TryPlace(cart, anchor, map);
        if (!placed)
        {
            var recovery = Find.World.GetComponent<WorldComp_CartRecovery>();
            if (!recovery._carts.TryAdd(cart, false))
            {
                cart.Destroy(); // The original proxy still owns every cargo item.
                return false;
            }
            recovery._pending.Add(new PendingCart(cart, map.Parent, anchor));
        }

        // Placement or the durable world owner is secured before releasing the proxy's cargo.
        proxy.MoveContentsToContainer(cart);
        proxy.Destroy();
        if (!placed)
            Messages.Message("RG_CartRecoveryBlocked".Translate(),
                new TargetInfo(anchor, map), MessageTypeDefOf.RejectInput);
        return true;
    }

    private static bool TryPlace(Building_MobileContainer cart, IntVec3 anchor, Map map)
    {
        bool placed = GenPlace.TryPlaceThing(cart, anchor, map, ThingPlaceMode.Near,
            extraValidator: cell => CanRestoreAt(cart.def, cart.Rotation, cell, map), rot: cart.Rotation);
        if (placed) cart.Rotation = Utils.RotationFacingFor(anchor, cart.Position);
        return placed;
    }

    public static bool CanRestoreAt(ThingDef def, Rot4 rotation, IntVec3 cell, Map map) =>
        GenSpawn.CanSpawnAt(def, cell, map, rotation, true)
        && GenAdj.OccupiedRect(cell, rotation, def.size)
            .All(c => c.InBounds(map) && !c.GetThingList(map)
                .Any(t => GenSpawn.SpawningWipes(def, t.def, true)));
}
