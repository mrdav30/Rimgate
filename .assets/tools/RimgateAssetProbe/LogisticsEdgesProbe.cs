using RimBridgeServer.Sdk;
using RimWorld;
using Verse;

namespace Rimgate.AssetProbe;

// Loose fuel, empty carts and ordinary placement obstructions are inputs only.
public sealed class LogisticsEdgesProbe
{
    [Tool("rimgate/prepare_fuel_cart", Description = "Replace the owned empty utility wheelbarrow with an empty fuelled-variant input and loose compatible fuel. Never loads, pushes or refuels by job; initialFuel is an explicit starvation input, default zero.")]
    public static Task<object> PrepareFuelCart(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot,
        string cartId, string variant, float initialFuel = 0)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        Guard(expectedConfigPath, expectedModRoot);
        string def = variant switch
        {
            "Tracked" => "Rimgate_MobileTrackedCart",
            "RefrigeratedTracked" => "Rimgate_RefrigeratedMobileTrackedCart",
            "Hover" => "Rimgate_MobileHoverSkid",
            "RefrigeratedHover" => "Rimgate_RefrigeratedMobileHoverSkid",
            _ => throw new ArgumentException("Use an authored fuelled cart variant.")
        };
        var map = Find.CurrentMap;
        var original = map.listerThings.AllThings.OfType<Building_MobileContainer>().Single(c => c.GetUniqueLoadID() == cartId);
        if (original.Position != new IntVec3(133, 0, 111) || original.InnerContainer.Count != 0 || original.LoadingInProgress
            || initialFuel < 0 || initialFuel > 0.05f)
            throw new InvalidOperationException("Use the empty owned utility input; a nonzero seed is only for bounded fuel starvation.");
        var cart = (Building_MobileContainer)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(def), ThingDefOf.Steel);
        var fuelComp = cart.Refuelable;
        var fuel = ThingMaker.MakeThing(fuelComp.Props.fuelFilter.AllowedThingDefs.Single());
        fuel.stackCount = (int)Math.Ceiling(fuelComp.Props.fuelCapacity);
        cart.SetFaction(Faction.OfPlayer);
        cart.AllowColonistsUseContents = false;
        cart.HitPoints = cart.MaxHitPoints - 13;
        fuelComp.allowAutoRefuel = false;
        if (initialFuel > 0) fuelComp.Refuel(initialFuel);
        var cell = original.Position;
        original.Destroy(DestroyMode.Vanish);
        GenSpawn.Spawn(cart, cell, map);
        GenSpawn.Spawn(fuel, new IntVec3(135, 0, 112), map);
        fuel.SetForbidden(false, false);
        return new { success = true, variant, cartId = cart.GetUniqueLoadID(), cartLabel = cart.Label, fuelId = fuel.GetUniqueLoadID(),
            fuelDef = fuel.def.defName, fuelCount = fuel.stackCount, initialFuel, hitPoints = cart.HitPoints,
            fuelCapacity = fuelComp.Props.fuelCapacity, baseFuelPerTick = fuelComp.Props.fuelConsumptionRate / 60000f,
            limits = "Only an empty input cart and loose fuel are replaced/staged. Real contextual refuelling, manifest hauling, proxy conversion and fuel consumption remain production outcomes." };
    });

    [Tool("rimgate/prepare_cart_obstructions", Description = "Stage ordinary walls around an actual carried cart proxy, optionally a passable sandbag on the pusher's cell. Preserves preexisting things and reports native cart spawn predicates; never interrupts, converts or drops the proxy.")]
    public static Task<object> PrepareObstructions(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot,
        string pawnId, bool blockCenter = false, int radius = 2)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        Guard(expectedConfigPath, expectedModRoot);
        var map = Find.CurrentMap;
        var pawn = map.mapPawns.FreeColonistsSpawned.Single(p => p.GetUniqueLoadID() == pawnId);
        if (pawn.carryTracker.CarriedThing is not Thing_MobileCartProxy proxy || !pawn.CurJobDef.defName.Contains("Container"))
            throw new InvalidOperationException("Pause at an actual carried push boundary.");
        if (radius is < 2 or > 13)
            throw new ArgumentException("Use radius 2..13; 13 covers the native 12.9-cell Near search.");
        var cells = GenRadial.RadialCellsAround(pawn.Position, radius, true).ToArray();
        var inputs = new List<(IntVec3 Cell, ThingDef Def, ThingDef Stuff)>();
        foreach (var cell in cells)
        {
            if (cell == pawn.Position && !blockCenter) continue;
            // Original obstacles already rejected by the actual production predicate stay untouched.
            if (!WorldComp_CartRecovery.CanRestoreAt(proxy.SavedDef, Rot4.North, cell, map)) continue;
            var def = cell == pawn.Position ? DefDatabase<ThingDef>.GetNamed("Sandbags") : ThingDefOf.Wall;
            if (!cell.InBounds(map) || cell.GetThingList(map).Any(t => t is Pawn other && other != pawn)
                || !GenSpawn.CanSpawnAt(def, cell, map, Rot4.North, true)
                || cell.GetThingList(map).Any(t => GenSpawn.SpawningWipes(def, t.def, true)))
                throw new InvalidOperationException("Every still-legal cart cell must accept an obstruction without changing any original object; choose another real approach boundary.");
            inputs.Add((cell, def, cell == pawn.Position ? ThingDefOf.Cloth : ThingDefOf.Steel));
        }
        var protectedThings = map.listerThings.AllThings.Select(t => (Thing: t, t.Position, t.HitPoints)).ToArray();
        var made = new List<Thing>();
        try
        {
            foreach (var input in inputs)
            {
                var blocker = ThingMaker.MakeThing(input.Def, input.Stuff);
                blocker.SetFaction(Faction.OfPlayer);
                GenSpawn.Spawn(blocker, input.Cell, map);
                made.Add(blocker);
            }
            if (protectedThings.Any(p => p.Thing.Destroyed || !p.Thing.Spawned || p.Thing.Position != p.Position || p.Thing.HitPoints != p.HitPoints))
                throw new InvalidOperationException("Obstructions changed an original fixture object.");
            return new { success = true, pawnId, proxyId = proxy.GetUniqueLoadID(), center = new { x = pawn.Position.x, z = pawn.Position.z },
                blockCenter, radius, pawnCellStandable = pawn.Position.Standable(map), blockerIds = string.Join(",", made.Select(t => t.GetUniqueLoadID())),
                nativeNearLegalCellCount = GenRadial.RadialCellsAround(pawn.Position, 12.9f, true)
                    .Count(c => WorldComp_CartRecovery.CanRestoreAt(proxy.SavedDef, Rot4.North, c, map)),
                blockers = made.Select(t => new { id = t.GetUniqueLoadID(), def = t.def.defName, x = t.Position.x, z = t.Position.z, hitPoints = t.HitPoints }).ToArray(),
                cells = cells.Select(c => new { x = c.x, z = c.z, standable = c.Standable(map), edifice = c.GetEdifice(map)?.def.defName,
                    canSpawn = GenSpawn.CanSpawnAt(proxy.SavedDef, c, map, Rot4.North, true),
                    wouldWipeIds = c.GetThingList(map).Where(t => GenSpawn.SpawningWipes(proxy.SavedDef, t.def, true)).Select(t => t.GetUniqueLoadID()).ToArray() }).ToArray(),
                limits = "Read native predicates before actual draft interruption; walls/sandbags are fixture inputs. No carry/drop/job/proxy state is written. The unobstructed pawn cell is a distinct legal fallback when blockCenter is false." };
        }
        catch
        {
            foreach (var t in made) if (!t.Destroyed) t.Destroy(DestroyMode.Vanish);
            throw;
        }
    });

    [Tool("rimgate/cart_obstruction_status", Description = "Read original obstruction IDs after real cart cleanup; never restores missing blockers or cargo.")]
    public static Task<object> ObstructionStatus(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot, string blockerIds)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        Guard(expectedConfigPath, expectedModRoot);
        var all = Find.Maps.SelectMany(m => m.listerThings.AllThings).ToArray();
        return new { success = true, blockers = blockerIds.Split(',').Select(id => new { id, found = all.Any(t => t.GetUniqueLoadID() == id),
            state = all.Where(t => t.GetUniqueLoadID() == id).Select(t => new { spawned = t.Spawned, hitPoints = t.HitPoints, x = t.Position.x, z = t.Position.z }).ToArray() }).ToArray() };
    });

    [Tool("rimgate/release_cart_obstructions", Description = "Remove only the recorded private fixture's player wall/sandbag IDs after an actual failed cart placement. Does not restore, transfer or drop pending carts; normal world ticks must recover them.")]
    public static Task<object> ReleaseObstructions(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot, string blockerIds)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        Guard(expectedConfigPath, expectedModRoot);
        var map = Find.CurrentMap;
        if (!map.listerThings.AllThings.OfType<Building_Gate>().Any(g => g.Position == new IntVec3(110, 0, 130)
            && g.ConnectedDHD?.Position == new IntVec3(110, 0, 134)))
            throw new InvalidOperationException("Use the owned utility/transit map.");
        var ids = blockerIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();
        var things = ids.Select(id => map.listerThings.AllThings.Single(t => t.GetUniqueLoadID() == id)).ToArray();
        if (things.Any(t => !t.Spawned || t.Faction != Faction.OfPlayer || t.def != ThingDefOf.Wall && t.def.defName != "Sandbags"))
            throw new InvalidOperationException("Release only the recorded player wall/sandbag inputs on this owned map.");
        foreach (var thing in things) thing.Destroy(DestroyMode.Vanish);
        return new { success = true, releasedIds = ids, limits = "Only the exact supplied obstruction IDs are removed; no cart recovery outcome is invoked." };
    });

    [Tool("rimgate/cart_recovery_status", Description = "Read the actual durable recovery owner, cart metadata/cargo and original map-parent/anchor after blocked restoration or reload. Never retries or places a cart.")]
    public static Task<object> RecoveryStatus(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        Guard(expectedConfigPath, expectedModRoot);
        var recovery = Find.World.GetComponent<WorldComp_CartRecovery>();
        var owner = recovery.GetDirectlyHeldThings();
        return new { success = true, tick = Find.TickManager.TicksGame, ownedCount = owner.Count,
            pending = recovery.Pending.Select(p => new { cartId = p.Cart.GetUniqueLoadID(),
                ownerIsRecovery = p.Cart.ParentHolder == recovery, spawned = p.Cart.Spawned,
                defName = p.Cart.def.defName, stuff = p.Cart.Stuff?.defName, hitPoints = p.Cart.HitPoints,
                allowUseContents = p.Cart.AllowColonistsUseContents, fuel = p.Cart.Refuelable?.Fuel,
                cargo = p.Cart.InnerContainer.Select(t => new { id = t.GetUniqueLoadID(), defName = t.def.defName, count = t.stackCount }).ToArray(),
                destinationId = p.MapParent?.ID, hasMap = p.MapParent?.HasMap == true,
                anchor = new { x = p.Anchor.x, z = p.Anchor.z } }).ToArray() };
    });

    private static void Guard(string expectedConfigPath, string expectedModRoot)
    {
        var mod = LoadedModManager.RunningModsListForReading.Single(m => m.PackageId == "mrdav30.rimgate");
        if (!Prefs.DevMode || Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null
            || Path.GetFileName(GenFilePaths.SaveDataFolderPath) != "Profile-1.7"
            || !Path.GetFullPath(GenFilePaths.ModsConfigFilePath).Equals(Path.GetFullPath(expectedConfigPath), StringComparison.OrdinalIgnoreCase)
            || !Path.GetFullPath(mod.RootDir).Equals(Path.GetFullPath(expectedModRoot), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Use the expected private 1.7 profile/deployment.");
    }
}
