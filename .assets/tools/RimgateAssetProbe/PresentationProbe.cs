using RimBridgeServer.Sdk;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimgate.AssetProbe;

// Private-fixture orientation/controller preparation; production jobs still dial and operate the iris.
public sealed class PresentationProbe
{
    [Tool("rimgate/stage_gate_presentation", Description = "Reposition the inactive owned smoke gate at one cardinal rotation and select its ancient or Tau'ri controller. Does not dial or activate it.")]
    public static Task<object> Stage(IRimBridgeContext context, string expectedConfigPath,
        string expectedModRoot, int rotation, string controller = "Ancient")
        => context.MainThread.InvokeAsync<object>(() =>
    {
        var mod = LoadedModManager.RunningModsListForReading.Single(m => m.PackageId == "mrdav30.rimgate");
        if (!Prefs.DevMode || Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null
            || Path.GetFileName(GenFilePaths.SaveDataFolderPath) != "Profile-1.7"
            || !Path.GetFullPath(GenFilePaths.ModsConfigFilePath).Equals(Path.GetFullPath(expectedConfigPath), StringComparison.OrdinalIgnoreCase)
            || !Path.GetFullPath(mod.RootDir).Equals(Path.GetFullPath(expectedModRoot), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Use the expected private 1.7 profile/deployment.");
        if (rotation is < 0 or > 3 || controller is not ("Ancient" or "Tauri"))
            throw new ArgumentException("Use rotation 0..3 and controller Ancient or Tauri.");
        var map = Find.CurrentMap;
        var gate = map.listerThings.AllThings.OfType<Building_Gate>()
            .Single(g => g.Position == new IntVec3(110, 0, 130));
        if (gate.IsActive || gate.IsOpeningQueued || gate.IsIrisActivated || gate.GetDirectlyHeldThings().Count != 0)
            throw new InvalidOperationException("Prepare an inactive, open-iris, empty smoke gate first.");
        var previous = gate.ConnectedDHD ?? throw new InvalidOperationException("The source controller must be linked.");
        gate.DeSpawn();
        previous.Destroy(DestroyMode.Vanish);
        GenSpawn.Spawn(gate, new IntVec3(110, 0, 130), map, new Rot4(rotation));
        var def = DefDatabase<ThingDef>.GetNamed(controller == "Tauri"
            ? "Rimgate_TauriDialHomeDevice" : "Rimgate_DialHomeDevice");
        var dhd = (Building_DHD)GenSpawn.Spawn(def, new IntVec3(110, 0, 134), map,
            controller == "Tauri" ? Rot4.North : new Rot4(rotation));
        dhd.SetFaction(Faction.OfPlayer);
        return new { success = true, rotation, controller, sourceGateId = gate.GetUniqueLoadID(),
            sourceDhdId = dhd.GetUniqueLoadID(), limits = "Inactive fixture respawn only; use real DHD dialing and iris jobs for active captures." };
    });

    [Tool("rimgate/gate_presentation_status", Description = "Read the owned gate/controller state and material/texture selection at its actual rotation. Does not prove the pixels rendered correctly.")]
    public static Task<object> Status(IRimBridgeContext context) => context.MainThread.InvokeAsync<object>(() =>
    {
        var gate = Find.CurrentMap.listerThings.AllThings.OfType<Building_Gate>()
            .Single(g => g.Position == new IntVec3(110, 0, 130));
        var dhd = gate.ConnectedDHD;
        var assets = new[] {
            Material("gate", gate.Graphic, gate.Rotation, gate),
            Material("eventHorizon", gate.GatePuddle, gate.Rotation, gate),
            Material("iris", gate.GateIris, gate.Rotation, gate),
            Material("chevrons", gate.ChevronHighlight, gate.Rotation, gate),
            Material("dhd", dhd?.Graphic, dhd?.Rotation ?? Rot4.North, dhd),
            Material("dhdActive", dhd?.ActiveGraphic, dhd?.Rotation ?? Rot4.North, dhd) };
        return new { success = dhd != null && assets.All(a => a.valid), rotation = gate.Rotation.AsInt,
            gate.IsActive, gate.IsReceivingGate, gate.IsOpeningQueued, gate.TicksSinceOpened,
            ticksUntilOpen = gate.TicksUntilOpen,
            gate.HasIris, irisClosed = gate.IsIrisActivated, gate.Powered,
            dhdId = dhd?.GetUniqueLoadID(), dhdDef = dhd?.def.defName,
            dhdPowered = dhd?.Powered, dhdLinked = dhd?.TryGetLinkedGate(out var linked) == true && linked == gate,
            assets, tick = Find.TickManager.TicksGame };
    });

    private sealed record MaterialState(string role, bool valid, string texture, string shader);

    private static MaterialState Material(string role,
        Graphic graphic, Rot4 rotation, Thing thing)
    {
        var mat = graphic?.MatAt(rotation, thing);
        return new(role, mat?.shader != null && mat.mainTexture != null && mat.mainTexture != BaseContent.BadTex,
            mat?.mainTexture?.name, mat?.shader?.name);
    }
}
