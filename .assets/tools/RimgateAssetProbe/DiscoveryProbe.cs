using System.Reflection;
using RimBridgeServer.Sdk;
using RimWorld;
using Verse;
using Verse.AI;

namespace Rimgate.AssetProbe;

// Private fixture inputs and read-only observations. The player menu starts decoding.
public sealed class DiscoveryProbe
{
    private static readonly FieldInfo SiteQuestId = typeof(WorldObject_GateQuestSite)
        .GetField("_questId", BindingFlags.Instance | BindingFlags.NonPublic)!;

    [Tool("rimgate/prepare_discovery_fixture", Description = "Stage a skilled pawn, research and two glyph parchments in the guarded owned smoke colony. Does not decode, create or accept quests/sites, dial or activate gates.")]
    public static Task<object> Prepare(IRimBridgeContext context, string expectedConfigPath,
        string expectedModRoot)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        Guard(expectedConfigPath, expectedModRoot);
        var map = Find.CurrentMap;
        var gate = map.listerThings.AllThings.OfType<Building_Gate>()
            .SingleOrDefault(g => g.Position == new IntVec3(110, 0, 130));
        if (gate == null || gate.IsActive || gate.IsOpeningQueued || gate.IsIrisActivated
            || gate.ConnectedDHD?.Position != new IntVec3(110, 0, 134))
            throw new InvalidOperationException("Load the owned smoke save with its inactive open-iris gate at (110,130) and linked DHD at (110,134).");
        var world = Find.World.GetComponent<WorldComp_GateAddresses>();
        if (world.AddressList.Count >= GateUtil.MaxAddresses)
            throw new InvalidOperationException("The owned fixture needs a free address slot.");
        var pawn = map.mapPawns.FreeColonistsSpawned
            .Where(p => !p.Dead && !p.Downed && !p.InMentalState && p.skills != null)
            .OrderBy(p => p.thingIDNumber).FirstOrDefault()
            ?? throw new InvalidOperationException("A healthy source colonist with skills is required.");
        var glyphCell = new IntVec3(114, 0, 132);
        if (map.listerThings.AllThings.Any(t => t.Position == glyphCell && t.TryGetComp<Comp_GlyphParchment>() != null))
            throw new InvalidOperationException("Reload the owned smoke save before recreating the glyph fixture.");
        var initial = Snapshot(pawn.GetUniqueLoadID(), "");

        // A short same-map approach, clear of the gate and its DHD.
        foreach (var cell in new CellRect(113, 131, 3, 5).ClipInsideMap(map))
        {
            map.roofGrid.SetRoof(cell, null);
            foreach (var thing in cell.GetThingList(map).ToArray())
                if (thing is not Pawn && thing is not Building_Gate && thing is not Building_DHD)
                    thing.Destroy(DestroyMode.Vanish);
            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Concrete);
        }
        map.fogGrid.ClearAllFog();
        Find.ResearchManager.FinishProject(RimgateDefOf.Rimgate_GlyphDeciphering, false, pawn, false);
        pawn.skills.GetSkill(SkillDefOf.Intellectual).Level = 20;
        pawn.jobs.StopAll();
        pawn.drafter.Drafted = false;
        pawn.pather.StopDead();
        pawn.Position = new IntVec3(114, 0, 134);
        var wait = JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture);
        wait.expiryInterval = 60000;
        wait.playerForced = true;
        pawn.jobs.TryTakeOrderedJob(wait, JobTag.Misc);
        var glyph = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Rimgate_GlyphParchment"));
        glyph.stackCount = 2;
        GenSpawn.Spawn(glyph, glyphCell, map);
        return new
        {
            success = true, sourceMapId = map.uniqueID,
            sourceGateId = gate.GetUniqueLoadID(), sourceDhdId = gate.ConnectedDHD.GetUniqueLoadID(),
            pawnId = pawn.GetUniqueLoadID(), pawnName = pawn.LabelShort,
            glyphId = glyph.GetUniqueLoadID(), glyphDef = glyph.def.defName,
            useDuration = glyph.TryGetComp<Comp_GlyphParchment>().Props.useDuration,
            initial, prepared = Snapshot(pawn.GetUniqueLoadID(), glyph.GetUniqueLoadID()),
            limits = "Research, skill, same-map positioning and two planetary parchments are fixture inputs. Start Decode symbols through the real pawn menu. No discovery result has been inserted. Orbital discovery is outside this fixture."
        };
    });

    [Tool("rimgate/discovery_status", Description = "Read tracked pawn/parchment, research, current-world addresses, quests and gate quest sites after real decoding or save/load. Reacquires IDs without changing discovery state.")]
    public static Task<object> Status(IRimBridgeContext context, string pawnId, string glyphId)
        => context.MainThread.InvokeAsync<object>(() => Snapshot(pawnId, glyphId));

    private static object Snapshot(string pawnId, string glyphId)
    {
        var things = Find.Maps.SelectMany(m => m.listerThings.AllThings).ToArray();
        var pawn = things.OfType<Pawn>().SingleOrDefault(p => p.GetUniqueLoadID() == pawnId);
        var glyph = things.SingleOrDefault(t => t.GetUniqueLoadID() == glyphId);
        var world = Find.World.GetComponent<WorldComp_GateAddresses>();
        return new
        {
            success = true, tick = Find.TickManager.TicksGame, currentMapId = Find.CurrentMap?.uniqueID,
            pawn = pawn == null ? null : new
            {
                id = pawn.GetUniqueLoadID(), mapId = pawn.Map.uniqueID, x = pawn.Position.x, z = pawn.Position.z,
                dead = pawn.Dead, downed = pawn.Downed,
                intellectual = pawn.skills.GetSkill(SkillDefOf.Intellectual).Level,
                job = pawn.CurJobDef?.defName, jobTarget = pawn.CurJob?.targetA.Thing?.GetUniqueLoadID()
            },
            glyph = glyph == null ? null : new
            {
                id = glyph.GetUniqueLoadID(), def = glyph.def.defName, mapId = glyph.Map.uniqueID,
                x = glyph.Position.x, z = glyph.Position.z, stackCount = glyph.stackCount
            },
            glyphResearch = ResearchUtil.GlyphDecipheringComplete,
            modificationResearch = ResearchUtil.GateModificationComplete,
            currentWorldOwnsAddressCache = ReferenceEquals(GateUtil.WorldComp, world),
            activeQuestSiteCount = world.ActiveQuestSiteCount,
            addresses = world.AddressList.Select(t => new
            {
                tile = t.tileId, layer = t.LayerDef.defName, designation = GateUtil.GetGateDesignation(t)
            }).ToArray(),
            quests = Find.QuestManager.QuestsListForReading.Select(q => new
            {
                id = q.id, root = q.root?.defName, name = q.name, state = q.State.ToString()
            }).ToArray(),
            sites = Find.WorldObjects.MapParents.OfType<WorldObject_GateQuestSite>().Select(s => new
            {
                id = s.ID, def = s.def.defName, label = s.Label, questId = (int)SiteQuestId.GetValue(s)!,
                tile = s.Tile.tileId, layer = s.Tile.LayerDef.defName,
                designation = GateUtil.GetGateDesignation(s.Tile),
                hasMap = s.HasMap, mapId = s.Map?.uniqueID, visited = s.WasEverVisited,
                parts = s.parts.Select(p => p.def.defName).ToArray()
            }).ToArray(),
            limits = "Pawn/parchment lookup covers spawned map objects; a missing object alone does not establish destruction. Quest and site state are read directly; address cleanup is not invoked."
        };
    }

    private static void Guard(string expectedConfigPath, string expectedModRoot)
    {
        var comparison = StringComparison.OrdinalIgnoreCase;
        var mod = LoadedModManager.RunningModsListForReading.Single(m => m.PackageId == "mrdav30.rimgate");
        if (VersionControl.CurrentVersion.Major != 1 || VersionControl.CurrentVersion.Minor != 7
            || !Prefs.DevMode || Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null
            || Path.GetFileName(GenFilePaths.SaveDataFolderPath) != "Profile-1.7"
            || !Path.GetFullPath(GenFilePaths.ModsConfigFilePath).Equals(Path.GetFullPath(expectedConfigPath), comparison)
            || !Path.GetFullPath(mod.RootDir).Equals(Path.GetFullPath(expectedModRoot), comparison))
            throw new InvalidOperationException("Refusing fixture changes outside the expected isolated 1.7 game/profile/deployment.");
    }
}
