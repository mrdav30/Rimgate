using RimBridgeServer.Sdk;
using RimWorld;
using Verse;
using Verse.AI;

namespace Rimgate.AssetProbe;

// Only cart/cargo/approach inputs; the ordinary load and gate jobs own every outcome.
public sealed class CartGateProbe
{
    [Tool("rimgate/prepare_cart_gate_fixture", Description = "Stage an empty steel cart, loose unique pistol and two operators on the owned transit source. Does not load, push, dial, close or transfer anything.")]
    public static Task<object> Prepare(IRimBridgeContext context, string expectedConfigPath,
        string expectedModRoot, string sourceGateId, int destinationParentId, string pawnId, string closerId)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        Guard(expectedConfigPath, expectedModRoot);
        var map = Find.CurrentMap;
        var gate = map.listerThings.AllThings.OfType<Building_Gate>().Single(g => Id(g) == sourceGateId);
        if (gate.Position != new IntVec3(110, 0, 130) || gate.IsOpeningQueued || gate.IsIrisActivated
            || gate.ConnectedDHD?.Position != new IntVec3(110, 0, 134)
            || gate.GetDirectlyHeldThings().Count != 0
            || gate.IsActive && (gate.IsReceivingGate || gate.TicksSinceOpened < 150 || gate.ConnectedGate == null
                || gate.ConnectedGate.IsIrisActivated || gate.ConnectedGate.GetDirectlyHeldThings().Count != 0))
            throw new InvalidOperationException("Use the owned empty open-iris source, either inactive or mature outgoing with an open empty receiver.");
        var destination = Find.WorldObjects.MapParents.OfType<WorldObject_GateTransitSite>()
            .Single(s => s.ID == destinationParentId && s.SiteName == "Rimgate transit destination");
        if (map.listerThings.AllThings.Any(t => t is Building_MobileContainer or Thing_MobileCartProxy))
            throw new InvalidOperationException("Reload the owned transit checkpoint before recreating the cart fixture.");
        var pawn = map.mapPawns.FreeColonistsSpawned.Single(p => Id(p) == pawnId);
        var closer = map.mapPawns.FreeColonistsSpawned.Single(p => Id(p) == closerId);
        if (pawn == closer || new[] { pawn, closer }.Any(p => p.Dead || p.Downed || p.InMentalState)
            || pawn.WorkTagIsDisabled(WorkTags.Hauling))
            throw new InvalidOperationException("Use separate healthy source colonists and a hauling-capable operator.");
        // Same known smoke-fixture prerequisite as UtilityProbe: leave colony actors intact.
        var hostiles = map.listerThings.AllThings.OfType<Pawn>().Where(p => p.Spawned && p.Map == map && !p.Dead
            && p.Faction != Faction.OfPlayer && !p.IsColonist && !p.IsPrisonerOfColony && !p.IsSlaveOfColony
            && (p.Faction?.HostileTo(Faction.OfPlayer) == true || p.HostileTo(Faction.OfPlayer))).ToArray();
        var removedHostileIds = hostiles.Select(Id).ToArray();
        foreach (var hostile in hostiles) hostile.Destroy(DestroyMode.Vanish);

        // A distant cart leaves time for the separate controller job before its arrival.
        foreach (var rect in new[] { new CellRect(112, 118, 43, 5), new CellRect(108, 118, 9, 19) })
            foreach (var cell in rect.ClipInsideMap(map))
            {
                map.roofGrid.SetRoof(cell, null);
                foreach (var thing in cell.GetThingList(map).ToArray())
                    if (thing is not Pawn && thing is not Building_Gate && thing is not Building_DHD)
                        thing.Destroy(DestroyMode.Vanish);
                map.terrainGrid.SetTerrain(cell, TerrainDefOf.Concrete);
            }
        foreach (var colonist in map.mapPawns.FreeColonistsSpawned.ToArray())
        {
            StagePawn(colonist, colonist == pawn ? new IntVec3(140, 0, 120)
                : colonist == closer ? new IntVec3(111, 0, 134) : new IntVec3(119, 0, 108));
            // The damaged cart is an identity/property oracle, not a repair task.
            colonist.workSettings.SetPriority(WorkTypeDefOf.Construction, 0);
        }
        pawn.workSettings.SetPriority(WorkTypeDefOf.Hauling, 1);
        var cart = (Building_MobileContainer)ThingMaker.MakeThing(RimgateDefOf.Rimgate_Wheelbarrow, ThingDefOf.Steel);
        cart.SetFaction(Faction.OfPlayer);
        cart.AllowColonistsUseContents = false;
        cart.HitPoints = cart.MaxHitPoints - 13;
        GenSpawn.Spawn(cart, new IntVec3(138, 0, 120), map);
        var cargo = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Gun_Autopistol"));
        GenSpawn.Spawn(cargo, new IntVec3(136, 0, 120), map);
        cargo.SetForbidden(false, false);
        map.fogGrid.ClearAllFog();
        return new { success = true, removedHostileIds, sourceGateId, sourceDhdId = Id(gate.ConnectedDHD), sourceMapId = map.uniqueID,
            destinationParentId = destination.ID, pawnId, closerId, cartId = Id(cart), cargoId = Id(cargo),
            hitPoints = cart.HitPoints, cartX = cart.Position.x, cartZ = cart.Position.z,
            cargoX = cargo.Position.x, cargoZ = cargo.Position.z,
            limits = "Prepared empty cart, loose cargo and distant same-map approaches; removed initially hostile non-colony guards as the known smoke-fixture prerequisite. Normal manifest UI/hauling must load it; real gate push and separate closer jobs must perform handoff/cancellation. Inactive preparation requires ordinary dialing before starting gate push. Later job wandering is not corrected by teleportation." };
    });

    [Tool("rimgate/cart_gate_status", Description = "Read cart/proxy/cargo ownership using existing utility/transit observers plus source gate survival. Remains observable after a missing gate; does not mutate jobs, buffers, links or contents.")]
    public static async Task<object> Status(IRimBridgeContext context, string expectedConfigPath,
        string expectedModRoot, string sourceGateId, int destinationParentId, string pawnId, string cargoId)
    {
        var utility = await UtilityProbe.Status(context, expectedConfigPath, expectedModRoot, pawnId, "", cargoId, "");
        var transit = await TransitProbe.Status(context, sourceGateId, destinationParentId, pawnId, cargoId);
        return await context.MainThread.InvokeAsync<object>(() =>
        {
            Guard(expectedConfigPath, expectedModRoot);
            var gate = Find.Maps.SelectMany(m => m.listerThings.AllThings.OfType<Building_Gate>())
                .SingleOrDefault(g => Id(g) == sourceGateId);
            return new { success = true, tick = Find.TickManager.TicksGame, sourceGateExists = gate?.Spawned == true,
                utility, transit, limits = "Existing deep-owner observers plus gate identity. Nested observations are sequential main-thread samples; pause for boundary assertions. Cart identity changes on conversion; cargo identity and preserved cart properties prove continuity." };
        });
    }

    private static void StagePawn(Pawn pawn, IntVec3 cell)
    {
        pawn.jobs.StopAll();
        pawn.drafter.Drafted = false;
        pawn.pather.StopDead();
        pawn.Position = cell;
        var wait = JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture);
        wait.expiryInterval = 60000;
        wait.playerForced = true;
        pawn.jobs.TryTakeOrderedJob(wait, JobTag.Misc);
    }
    private static string Id(Thing thing) => thing.GetUniqueLoadID();
    private static void Guard(string expectedConfigPath, string expectedModRoot)
    {
        var mod = LoadedModManager.RunningModsListForReading.Single(m => m.PackageId == "mrdav30.rimgate");
        if (VersionControl.CurrentVersion.Major != 1 || VersionControl.CurrentVersion.Minor != 7
            || !Prefs.DevMode || Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null
            || Path.GetFileName(GenFilePaths.SaveDataFolderPath) != "Profile-1.7"
            || !Path.GetFullPath(GenFilePaths.ModsConfigFilePath).Equals(Path.GetFullPath(expectedConfigPath), StringComparison.OrdinalIgnoreCase)
            || !Path.GetFullPath(mod.RootDir).Equals(Path.GetFullPath(expectedModRoot), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Use the expected isolated 1.7 profile and private mod deployment.");
    }
}
