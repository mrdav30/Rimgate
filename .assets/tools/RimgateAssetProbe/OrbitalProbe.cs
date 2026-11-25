using RimBridgeServer.Sdk;
using RimWorld;
using Verse;
using Verse.AI;

namespace Rimgate.AssetProbe;

// Orbital discovery inputs only. Decode results and the equipment flag remain production-owned.
public sealed class OrbitalProbe
{
    [Tool("rimgate/prepare_orbital_fixture", Description = "Stage an Orbit parchment stack, skilled decoder, research and ordinary source power in the guarded smoke colony. Does not create quests/sites/addresses, dial or bypass vacuum.")]
    public static Task<object> Prepare(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        var gate = Guard(expectedConfigPath, expectedModRoot);
        var map = gate.Map;
        if (!ModsConfig.OdysseyActive || Find.WorldGrid.Orbit == null || GateUtil.AddressBookFull
            || GateUtil.ModificationEquipmentActive
            || map.listerThings.AllThings.Any(t => t.def.defName == "Rimgate_GlyphParchment_Orbit"
                || t.def == RimgateDefOf.Rimgate_WraithModificationEquipment))
            throw new InvalidOperationException("Reload the owned smoke save with Odyssey/Orbit, a free address slot and no orbital glyph/equipment fixture.");
        var pawn = map.mapPawns.FreeColonistsSpawned.Where(p => !p.Dead && !p.Downed && !p.InMentalState
            && p.skills != null && !p.skills.GetSkill(SkillDefOf.Intellectual).TotallyDisabled)
            .OrderBy(p => p.thingIDNumber).First();
        var powerCell = new IntVec3(105, 0, 130);
        if (powerCell.GetThingList(map).Any(t => t is Building))
            throw new InvalidOperationException("The owned power input cell must be empty.");
        var initial = new {
            sites = Find.WorldObjects.MapParents.OfType<WorldObject_GateQuestSite>().Select(s => new { id = s.ID }).ToArray(),
            quests = Find.QuestManager.QuestsListForReading.Select(q => new { id = q.id }).ToArray()
        };
        foreach (var cell in new CellRect(113, 131, 3, 5).ClipInsideMap(map))
        {
            map.roofGrid.SetRoof(cell, null);
            foreach (var thing in cell.GetThingList(map).ToArray())
                if (thing is not Pawn && thing is not Building_Gate && thing is not Building_DHD)
                    thing.Destroy(DestroyMode.Vanish);
            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Concrete);
        }
        var power = GenSpawn.Spawn(DefDatabase<ThingDef>.GetNamed("VanometricPowerCell"), powerCell, map);
        power.SetFaction(Faction.OfPlayer);
        // This owned input colony contains unrelated ancient guards under fog.
        // Remove only its initially hostile spawned actors; never player/colony pawns.
        var initialMapPawns = map.listerThings.AllThings.OfType<Pawn>().ToArray();
        var hostileCleanupCandidates = initialMapPawns.Select(p => new
        {
            id = p.GetUniqueLoadID(), defName = p.def.defName, spawned = p.Spawned, onMap = p.Map == map,
            factionId = p.Faction?.GetUniqueLoadID(), factionHostile = p.Faction?.HostileTo(Faction.OfPlayer) == true,
            pawnHostile = p.HostileTo(Faction.OfPlayer), colonist = p.IsColonist,
            prisoner = p.IsPrisonerOfColony, slave = p.IsSlaveOfColony
        }).ToArray();
        var removedHostiles = initialMapPawns.Where(p => p.Spawned && p.Map == map && !p.Dead
            && p.Faction != Faction.OfPlayer && !p.IsColonist && !p.IsPrisonerOfColony && !p.IsSlaveOfColony
            && (p.Faction?.HostileTo(Faction.OfPlayer) == true || p.HostileTo(Faction.OfPlayer))).ToArray();
        var removedHostileIds = removedHostiles.Select(p => p.GetUniqueLoadID()).ToArray();
        foreach (var hostile in removedHostiles) hostile.Destroy(DestroyMode.Vanish);
        map.fogGrid.ClearAllFog();
        Find.ResearchManager.FinishProject(RimgateDefOf.Rimgate_GlyphDeciphering, false, pawn, false);
        Find.ResearchManager.FinishProject(RimgateDefOf.Rimgate_GateModification, false, pawn, false);
        pawn.skills.GetSkill(SkillDefOf.Intellectual).Level = 20;
        pawn.jobs.StopAll();
        pawn.drafter.Drafted = false;
        pawn.pather.StopDead();
        pawn.Position = new IntVec3(114, 0, 134);
        var wait = JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture);
        wait.expiryInterval = 60000;
        wait.playerForced = true;
        pawn.jobs.TryTakeOrderedJob(wait, JobTag.Misc);
        var glyph = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Rimgate_GlyphParchment_Orbit"));
        glyph.stackCount = 2;
        GenSpawn.Spawn(glyph, new IntVec3(114, 0, 132), map);
        return new { success = true, removedHostileIds, hostileCleanupCandidates, pawnId = pawn.GetUniqueLoadID(), glyphId = glyph.GetUniqueLoadID(),
            sourceGateId = gate.GetUniqueLoadID(), sourceDhdId = gate.ConnectedDHD.GetUniqueLoadID(),
            useDuration = glyph.TryGetComp<Comp_GlyphParchment>().Props.useDuration,
            decodeLabel = $"{"RG_DecodeSGSymbols".Translate()} {"RG_Glyph_SpaceLocked".Translate()}",
            dialPrefix = "RG_DialGate".Translate().ToString(),
            blockedSuffix = $" ({"RG_CannotDial".Translate("RG_CannotDialNoModEquipment".Translate())})",
            initial, limits = "Only prerequisite inputs. Real decode creates the Orbit quest/site/address. No map generation, arrival, spacesuit or vacuum changes. Initially hostile spawned guards are removed before unfogging; no colony pawn is removed." };
    });

    [Tool("rimgate/stage_orbital_equipment", Description = "Spawn ordinary player-owned Wraith modification equipment next to the owned inactive DHD. Production power/facility/rare-tick logic must enable orbital dialing.")]
    public static Task<object> Equipment(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        var gate = Guard(expectedConfigPath, expectedModRoot);
        var cell = new IntVec3(109, 0, 134);
        if (GateUtil.ModificationEquipmentActive || cell.GetThingList(gate.Map).Any(t => t is Building)
            || gate.Map.listerThings.AllThings.Any(t => t.def == RimgateDefOf.Rimgate_WraithModificationEquipment))
            throw new InvalidOperationException("Use the decoded checkpoint without existing equipment and with an empty input cell.");
        gate.Map.terrainGrid.SetTerrain(cell, TerrainDefOf.Concrete);
        var equipment = GenSpawn.Spawn(RimgateDefOf.Rimgate_WraithModificationEquipment, cell, gate.Map);
        equipment.SetFaction(Faction.OfPlayer);
        return new { success = true, equipmentId = equipment.GetUniqueLoadID(), status = Read(gate) };
    });

    [Tool("rimgate/orbital_equipment_status", Description = "Read actual owned gate/DHD power, linked modification equipment and world flag. Does not tick equipment or set the flag.")]
    public static Task<object> Status(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot)
        => context.MainThread.InvokeAsync<object>(() => Read(Guard(expectedConfigPath, expectedModRoot)));

    private static object Read(Building_Gate gate) => new {
        success = true, tick = Find.TickManager.TicksGame, modificationEquipmentActive = GateUtil.ModificationEquipmentActive,
        gateActive = gate.IsActive, gateQueued = gate.IsOpeningQueued, gatePowered = gate.Powered,
        dhdPowered = gate.ConnectedDHD.Powered,
        equipment = gate.Map.listerThings.AllThings.Where(t => t.def == RimgateDefOf.Rimgate_WraithModificationEquipment)
            .Select(t => new { id = t.GetUniqueLoadID(), powered = t.TryGetComp<CompPowerTrader>()?.PowerOn == true,
                linked = gate.ConnectedDHD.Facility.LinkedBuildings.Contains(t) }).ToArray()
    };

    private static Building_Gate Guard(string expectedConfigPath, string expectedModRoot)
    {
        var mod = LoadedModManager.RunningModsListForReading.Single(m => m.PackageId == "mrdav30.rimgate");
        if (VersionControl.CurrentVersion.Major != 1 || VersionControl.CurrentVersion.Minor != 7
            || !Prefs.DevMode || Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null
            || Path.GetFileName(GenFilePaths.SaveDataFolderPath) != "Profile-1.7"
            || !Path.GetFullPath(GenFilePaths.ModsConfigFilePath).Equals(Path.GetFullPath(expectedConfigPath), StringComparison.OrdinalIgnoreCase)
            || !Path.GetFullPath(mod.RootDir).Equals(Path.GetFullPath(expectedModRoot), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Use the expected private 1.7 profile/deployment.");
        var gate = Find.CurrentMap.listerThings.AllThings.OfType<Building_Gate>()
            .Single(g => g.Position == new IntVec3(110, 0, 130));
        if (gate.IsActive || gate.IsOpeningQueued || gate.IsIrisActivated || gate.GetDirectlyHeldThings().Count != 0
            || gate.ConnectedDHD?.Position != new IntVec3(110, 0, 134))
            throw new InvalidOperationException("Use the inactive, empty, open-iris owned smoke gate and linked DHD.");
        return gate;
    }
}
