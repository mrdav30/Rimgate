-- Run from the isolated dev main menu; parameters must identify that version's profile.
local config = rb.call("rimworld/get_mod_configuration_status", {})
rb.assert(config.success and config.result.success, "Mod configuration query failed.")
rb.assert(config.result.configPath == params.configPath, "Refusing to modify a different save-data profile.")
rb.assert(config.result.configurationIssueCount == 0, "Resolve mod configuration issues first.")
local deploymentFound = false
for i, mod in ipairs(config.result.activeMods) do
    if mod.packageId == "mrdav30.rimgate" then
        deploymentFound = mod.rootDir == params.modRoot and mod.loadedInSession
    end
end
rb.assert(deploymentFound, "The isolated Rimgate deployment is not loaded.")
local bridge = rb.call("rimbridge/get_bridge_status", {})
rb.assert(bridge.result.state.hasCurrentGame == false, "Start this smoke case from the main menu.")
local start = rb.call("rimworld/start_debug_game_ready", { readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
rb.assert(start.success and start.result.success, "Could not create the test colony.")
local gate = rb.call("rimworld/spawn_thing", { defName = "Rimgate_Dwarfgate", x = 110, z = 130 })
rb.assert(gate.success and gate.result.success, "Gate spawn failed.")
local dhd = rb.call("rimworld/spawn_thing", { defName = "Rimgate_DialHomeDevice", x = 110, z = 134 })
rb.assert(dhd.success and dhd.result.success, "DHD spawn failed.")
local camera = rb.call("rimworld/jump_camera_to_cell", { x = 110, z = 132 })
rb.assert(camera.result.success, "Camera positioning failed.")
local click = rb.call("rimworld/click_cell", { x = 110, z = 130, button = "left" })
rb.assert(click.result.success, "Gate selection failed.")
local gizmos = rb.call("rimworld/list_selected_gizmos", {})
for i, gizmo in ipairs(gizmos.result.gizmos) do
    if gizmo.label == "Claim" then
        local claim = rb.call("rimworld/execute_gizmo", { gizmoId = gizmo.id })
        rb.assert(claim.result.success, "Gate claim failed.")
    end
end
gizmos = rb.call("rimworld/list_selected_gizmos", {})
local irisChanged = false
for i, gizmo in ipairs(gizmos.result.gizmos) do
    if gizmo.label == "Add/remove iris" then
        local add = rb.call("rimworld/execute_gizmo", { gizmoId = gizmo.id })
        rb.assert(add.result.success, "Adding the iris failed.")
        irisChanged = true
    end
end
rb.assert(irisChanged, "The gate's development iris command was not available.")
local before = rb.call("rimworld/get_selection_semantics", {})
rb.assert(before.result.selectedObjects[1].id == gate.result.thingId, "Wrong object selected.")
local saved = rb.call("rimworld/save_game", { saveName = "Rimgate-bridge-smoke" })
rb.assert(saved.result.success and saved.result.exists, "Save did not complete.")
gizmos = rb.call("rimworld/list_selected_gizmos", {})
for i, gizmo in ipairs(gizmos.result.gizmos) do
    if gizmo.label == "Add/remove iris" then
        local remove = rb.call("rimworld/execute_gizmo", { gizmoId = gizmo.id })
        rb.assert(remove.result.success, "Removing the iris failed.")
    end
end
local changed = rb.call("rimworld/get_selection_semantics", {})
rb.assert(changed.result.selectedObjects[1].inspectString ~= before.result.selectedObjects[1].inspectString, "The post-save mutation did not change the inspect state.")
local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-bridge-smoke", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
rb.assert(loaded.success and loaded.result.success, "Reload failed.")
click = rb.call("rimworld/click_cell", { x = 110, z = 130, button = "left" })
rb.assert(click.result.success, "Reloaded gate selection failed.")
local after = rb.call("rimworld/get_selection_semantics", {})
rb.assert(after.result.selectedObjects[1].id == gate.result.thingId, "Gate identity changed across reload.")
rb.assert(after.result.selectedObjects[1].inspectString == before.result.selectedObjects[1].inspectString, "The saved iris/address state was not restored.")
local ticks = rb.call("rimworld/step_game_ticks", { ticks = 120, timeoutMs = 30000 })
rb.assert(ticks.result.success and ticks.result.advancedTicks == 120 and ticks.result.advancedFrames == 120, "Exact tick stepping failed.")
local shot = rb.call("rimworld/take_screenshot", { fileName = "rimgate-bridge-smoke", outputDirectory = params.evidenceDirectory, includeTargets = false })
rb.assert(shot.result.success, "Screenshot capture failed.")
local logs = rb.call("rimbridge/list_logs", { minimumLevel = "error", limit = 50 })
for i, entry in ipairs(logs.result.logs) do
    rb.fail("The bridge journal contains an error; inspect logs before accepting this run.", entry)
end
return { success = true, gateId = gate.result.thingId, dhdId = dhd.result.thingId, inspect = after.result.selectedObjects[1].inspectString, ticks = ticks.result.advancedTicks, screenshot = shot.result.path, save = saved.result.path }
