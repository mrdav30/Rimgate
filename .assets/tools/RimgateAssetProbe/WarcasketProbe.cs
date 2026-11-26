using System.Reflection;
using RimBridgeServer.Sdk;
using RimWorld;
using Verse;

namespace Rimgate.AssetProbe;

// Optional-mod inputs and observations only. Real entomb/customization/welding jobs
// must create the project, consume ingredients and grant the apparel-owned trait.
public sealed class WarcasketProbe
{
    private static readonly IntVec3 FoundryCell = new(129, 0, 136);
    private const string FoundryDef = "VFEP_WarcasketFoundry";
    private const string TraitDefName = "VFEP_WarcasketTrait";

    [Tool("rimgate/prepare_warcasket_foundry_inputs", Description = "Add one empty powered-consumer foundry and basic-part raw materials beside the owned fresh medical fixture. Finish basic research and enable the separate welder's Crafting work. Does not order jobs, create projects/apparel/traits or advance work.")]
    public static Task<object> Prepare(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot,
        string sarcophagusId, string patientId, string welderId) => context.MainThread.InvokeAsync<object>(() =>
    {
        var map = Guard(expectedConfigPath, expectedModRoot);
        var sarc = Sarcophagus(map, sarcophagusId);
        var patient = Actor(map, patientId);
        var welder = Actor(map, welderId);
        var trait = DefDatabase<TraitDef>.GetNamed(TraitDefName);
        if (!Find.TickManager.Paused || patient == welder || patient.DevelopmentalStage != DevelopmentalStage.Adult
            || patient.Dead || patient.Downed || patient.InMentalState || welder.Dead || welder.Downed || welder.InMentalState
            || patient.story?.traits == null || patient.story.traits.HasTrait(trait)
            || sarc.PatientPawn != null || sarc.Status != SarcophagusStatus.Idle || !sarc.Power.PowerOn
            || welder.WorkTagIsDisabled(WorkTags.Crafting)
            || !welder.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)
            || map.listerThings.AllThings.Any(t => t.def.defName == FoundryDef))
            throw new InvalidOperationException("Use paused fresh medical inputs, an unentombed adult patient and a distinct healthy Crafting-capable welder.");

        var foundryDef = DefDatabase<ThingDef>.GetNamed(FoundryDef);
        if (foundryDef.thingClass.FullName != "VFEPirates.Building_WarcasketFoundry" || foundryDef.size != new IntVec2(3, 3))
            throw new InvalidOperationException("The loaded native foundry must retain its audited class and 3x3 footprint.");
        var inputs = new[]
        {
            (Def: foundryDef, Cell: FoundryCell, Count: 1),
            (Def: ThingDefOf.Steel, Cell: new IntVec3(134, 0, 138), Count: 75),
            (Def: ThingDefOf.Steel, Cell: new IntVec3(135, 0, 138), Count: 75),
            (Def: ThingDefOf.Steel, Cell: new IntVec3(136, 0, 138), Count: 10),
            (Def: ThingDefOf.Uranium, Cell: new IntVec3(137, 0, 138), Count: 20),
            (Def: ThingDefOf.ComponentIndustrial, Cell: new IntVec3(138, 0, 138), Count: 3)
        };
        foreach (var input in inputs)
        {
            var cells = GenAdj.OccupiedRect(input.Cell, Rot4.North, input.Def.size).Cells.ToArray();
            if (input.Count > input.Def.stackLimit || cells.Any(c => !c.InBounds(map) || !c.Standable(map)
                    || c.GetThingList(map).Any(t => t.def.defName is not ("PowerConduit" or "HiddenConduit")
                        || GenSpawn.SpawningWipes(input.Def, t.def, true)))
                || !GenSpawn.CanSpawnAt(input.Def, input.Cell, map, Rot4.North, true))
                throw new InvalidOperationException("All fixed foundry/material footprints must be empty except preserved non-wiped conduits; reload a fresh medical apron.");
        }

        var originals = map.listerThings.AllThings.Select(t => (Thing: t, t.Spawned, t.Position, t.HitPoints)).ToArray();
        var created = new List<Thing>();
        foreach (var input in inputs)
        {
            var thing = ThingMaker.MakeThing(input.Def);
            thing.stackCount = input.Count;
            if (input.Def.CanHaveFaction) thing.SetFaction(Faction.OfPlayer);
            if (!ReferenceEquals(GenSpawn.Spawn(thing, input.Cell, map, Rot4.North, WipeMode.Vanish), thing) || !thing.Spawned)
                throw new InvalidOperationException("Native input spawning did not retain the actual foundry/material object.");
            created.Add(thing);
        }
        if (originals.Any(p => p.Thing.Destroyed || p.Thing.Spawned != p.Spawned
                || p.Thing.Position != p.Position || p.Thing.HitPoints != p.HitPoints))
            throw new InvalidOperationException("Native input placement altered an original map object; reject this fixture and reload.");
        Find.ResearchManager.FinishProject(DefDatabase<ResearchProjectDef>.GetNamed("VFEP_Warcaskets"), false, welder, false);
        welder.workSettings.SetPriority(WorkTypeDefOf.Crafting, 1);
        return new
        {
            success = true, patientId, welderId, sarcophagusId, foundryId = Id(created[0]),
            foundryCell = Cell(FoundryCell),
            rawMaterials = created.Skip(1).Select(t => new { id = Id(t), def = t.def.defName, count = t.stackCount, cell = Cell(t.Position) }).ToArray(),
            limits = "Inputs only: empty foundry, basic research, raw materials and Crafting priority. Normal power connection, pilot Entomb in warcasket UI/customization and separate welder jobs must produce the result. Basic defaults cost 160 Steel, 20 Uranium and 3 industrial components, with 3000 native work; selected native project diagnostics remain authoritative."
        };
    });

    [Tool("rimgate/warcasket_foundry_status", Description = "Read the actual native foundry occupant, project choices/ingredients/work, power, pilot/welder jobs and apparel-owned warcasket trait. Does not open customization or assign jobs, projects, apparel, traits or progress.")]
    public static Task<object> Status(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot,
        string foundryId, string patientId, string welderId) => context.MainThread.InvokeAsync<object>(() =>
    {
        var map = Guard(expectedConfigPath, expectedModRoot);
        var foundry = map.listerThings.AllThings.Single(t => Id(t) == foundryId && t.def.defName == FoundryDef);
        var patient = Actor(map, patientId);
        var welder = Actor(map, welderId);
        var power = foundry.TryGetComp<CompPowerTrader>();
        var project = Read(foundry, "curWarcasketProject");
        var occupant = Read(foundry, "occupant") as Pawn;
        var ingredients = project == null ? [] : ((IEnumerable<IngredientCount>)project.GetType()
            .GetMethod("RequiredIngredients")!.Invoke(project, null)!).ToArray();
        return new
        {
            success = true, tick = Find.TickManager.TicksGame, foundryId, cell = Cell(foundry.Position),
            powerOn = power?.PowerOn, connected = power?.PowerNet != null, powerOutput = power?.PowerOutput,
            switchOn = foundry.TryGetComp<CompFlickable>()?.SwitchIsOn,
            occupantId = occupant == null ? "" : Id(occupant), occupantIsPatient = ReferenceEquals(occupant, patient),
            occupantAliveAndPresent = (bool)foundry.GetType().GetProperty("OccupantAliveAndPresent")!.GetValue(foundry)!,
            projectPresent = project != null,
            project = project == null ? null : new
            {
                armor = (Read(project, "armorDef") as ThingDef)?.defName,
                shoulders = (Read(project, "shoulderPadsDef") as ThingDef)?.defName,
                helmet = (Read(project, "helmetDef") as ThingDef)?.defName,
                workDone = (float)Read(project, "currentWorkAmountDone"), totalWork = (float)Read(project, "totalWorkAmount"),
                ingredients = ingredients.Select(i => new { defs = i.filter.AllowedThingDefs.Select(d => d.defName).ToArray(), count = i.GetBaseCount() }).ToArray()
            },
            patient = PawnState(patient), welder = PawnState(welder),
            welderWorkSpeedGlobal = welder.GetStatValue(StatDefOf.WorkSpeedGlobal),
            foundryResearchComplete = DefDatabase<ResearchProjectDef>.GetNamed("VFEP_Warcaskets").IsFinished,
            limits = "Observation only. Preview apparel during customization is not completed entombment; require project completion, normal pilot-job release and retained apparel/trait after the dialog closes. No actual clinical race/trait-denial claim follows from apparel/trait presence alone."
        };
    });

    private static object PawnState(Pawn pawn) => new
    {
        id = Id(pawn), dead = pawn.Dead, downed = pawn.Downed, cell = Cell(pawn.Position),
        job = pawn.CurJobDef?.defName ?? "", toil = pawn.jobs.curDriver?.CurToilIndex,
        targetA = pawn.CurJob?.targetA.Thing == null ? "" : Id(pawn.CurJob.targetA.Thing),
        wornApparel = pawn.apparel?.WornApparel.Select(a => new { id = Id(a), def = a.def.defName,
            wearerMatchesPawn = ReferenceEquals(a.Wearer, pawn) }).ToArray(),
        hasWarcasketTrait = pawn.story?.traits?.allTraits.Any(t => t.def.defName == TraitDefName) == true,
        warcasketTraits = pawn.story?.traits?.allTraits.Where(t => t.def.defName == TraitDefName)
            .Select(t => new { def = t.def.defName, degree = t.Degree }).ToArray(),
        conditions = pawn.health.hediffSet.hediffs.Select(h => new { id = h.GetUniqueLoadID(), def = h.def.defName,
            severity = h.Severity, part = h.Part?.def.defName, visible = h.Visible, tendable = h.TendableNow() }).ToArray()
    };

    private static Map Guard(string expectedConfigPath, string expectedModRoot)
    {
        var mod = LoadedModManager.RunningModsListForReading.Single(m => m.PackageId == "mrdav30.rimgate");
        if (Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null || !Prefs.DevMode
            || VersionControl.CurrentVersion.Major != 1 || VersionControl.CurrentVersion.Minor != 7
            || Path.GetFileName(GenFilePaths.SaveDataFolderPath) != "Profile-1.7"
            || !Path.GetFullPath(GenFilePaths.ModsConfigFilePath).Equals(Path.GetFullPath(expectedConfigPath), StringComparison.OrdinalIgnoreCase)
            || !Path.GetFullPath(mod.RootDir).Equals(Path.GetFullPath(expectedModRoot), StringComparison.OrdinalIgnoreCase)
            || !LoadedModManager.RunningModsListForReading.Any(m => m.PackageId.Equals("oskarpotocki.vfe.pirates", StringComparison.OrdinalIgnoreCase))
            || !Find.CurrentMap.listerThings.AllThings.OfType<Building_Gate>().Any(g => g.Position == new IntVec3(110, 0, 130)))
            throw new InvalidOperationException("Use the expected isolated native 1.7 Pirates profile and owned smoke medical map.");
        return Find.CurrentMap;
    }

    private static Building_Sarcophagus Sarcophagus(Map map, string id) => map.listerThings.AllThings.OfType<Building_Sarcophagus>()
        .Single(s => Id(s) == id && s.Position == new IntVec3(132, 0, 132));
    private static Pawn Actor(Map map, string id) => map.listerThings.AllThings.OfType<Pawn>()
        .Single(p => Id(p) == id && p.Spawned && p.Map == map && p.Faction == Faction.OfPlayer && p.RaceProps.Humanlike
            && p.workSettings != null);
    private static object Read(object instance, string field) => instance.GetType()
        .GetField(field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance);
    private static string Id(Thing thing) => thing.GetUniqueLoadID();
    private static object Cell(IntVec3 cell) => new { x = cell.x, z = cell.z };
}
