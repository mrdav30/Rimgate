-- Run after rimbridge-smoke.lua has created its owned iris-equipped save.
-- Use the same configPath, modRoot and evidenceDirectory parameters.
local config = rb.call("rimworld/get_mod_configuration_status", {})
rb.assert(config.success and config.result.success, "Mod configuration query failed.")
rb.assert(config.result.configPath == params.configPath, "Refusing a different save-data profile.")
rb.assert(config.result.configurationIssueCount == 0, "Resolve mod configuration issues first.")
local deploymentFound = false
for i, mod in ipairs(config.result.activeMods) do
    if mod.packageId == "mrdav30.rimgate" then
        deploymentFound = mod.rootDir == params.modRoot and mod.loadedInSession
    end
end
rb.assert(deploymentFound, "The expected isolated Rimgate deployment is not loaded.")
local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-bridge-smoke", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
rb.assert(loaded.success and loaded.result.success, "The owned smoke save could not be loaded.")
local selected = rb.call("rimworld/click_cell", { x = 110, z = 130, button = "left" })
rb.assert(selected.success and selected.result.success, "The smoke gate could not be selected.")
local report = rb.call("rimgate/asset_report", {})
rb.assert(report.success and report.result.success, "Asset resolution or metadata validation failed.")
if params.assetFormat then
    rb.assert(report.result.assetFormat == params.assetFormat, "The deployed asset format differs from the requested comparison.")
end
rb.assert(report.result.configPath == params.configPath and report.result.modRoot == params.modRoot, "Asset probe inspected a different deployment.")
rb.assert(report.result.textureCount > 0 and report.result.soundCount > 0, "Asset inventory was empty.")
rb.assert(report.result.selectedGateCount == 1, "Select the smoke case's gate first.")
rb.assert(report.result.irisComps[1].valid and report.result.irisComps[1].hasIris, "The gate's real iris icons were not validated.")
local devIcons = 0
local irisCommand = false
for i, command in ipairs(report.result.commands) do
    if command.defaultDevIcon then
        rb.assert(command.iconIsNull, "The inherited debug command icon changed; review its new expectation.")
        devIcons = devIcons + 1
    end
    if command.label == "Close iris" or command.label == "Open iris" then
        rb.assert(command.valid and command.iconIsNull == false and command.iconIsBadTexture == false, "The player's iris command has an invalid icon.")
        irisCommand = true
    end
end
rb.assert(devIcons == 2, "Expected both inherited null-icon developer commands.")
rb.assert(irisCommand, "The real iris command was not inspected.")
local unfog = rb.call("rimworld/execute_debug_action", { path = "Actions\\Clear All Fog" })
rb.assert(unfog.success and unfog.result.success, "Could not reveal the private smoke map for visual inspection.")
-- Supply real power and use the normal pawn job to exercise both iris graphics.
-- The generator is outside the gate's five-cell footprint.
local generator = rb.call("rimworld/spawn_thing", { defName = "VanometricPowerCell", x = 105, z = 130 })
rb.assert(generator.success and generator.result.success, "Test power source spawn failed.")
selected = rb.call("rimworld/click_cell", { x = 105, z = 130, button = "left" })
rb.assert(selected.result.success, "Test power source selection failed.")
local gizmos = rb.call("rimworld/list_selected_gizmos", {})
for i, gizmo in ipairs(gizmos.result.gizmos) do
    if gizmo.label == "Claim" then
        local claim = rb.call("rimworld/execute_gizmo", { gizmoId = gizmo.id })
        rb.assert(claim.result.success, "Test power source claim failed.")
    end
end
local ticks = rb.call("rimworld/step_game_ticks", { ticks = 750, timeoutMs = 30000 })
rb.assert(ticks.success and ticks.result.success, "Power initialization stepping failed.")
selected = rb.call("rimworld/click_cell", { x = 110, z = 130, button = "left" })
rb.assert(selected.result.success, "Powered gate selection failed.")
gizmos = rb.call("rimworld/list_selected_gizmos", {})
local closeRequested = false
for i, gizmo in ipairs(gizmos.result.gizmos) do
    if gizmo.label == "Close iris" then
        rb.assert(gizmo.disabled == false, "Provide power before checking iris rendering.")
        local close = rb.call("rimworld/execute_gizmo", { gizmoId = gizmo.id })
        rb.assert(close.result.success, "Close iris request failed.")
        closeRequested = true
    end
end
rb.assert(closeRequested, "The player's Close iris command was absent.")
local colonists = rb.call("rimworld/list_colonists", { currentMapOnly = true })
rb.assert(colonists.result.count > 0, "The smoke colony has no pawn for the toggle job.")
local pawn = rb.call("rimworld/select_pawn", { pawnId = colonists.result.colonists[1].pawnId })
rb.assert(pawn.result.success, "Test pawn selection failed.")
local menu = rb.call("rimworld/right_click_cell", { x = 110, z = 130 })
rb.assert(menu.result.success, "Toggle context menu failed.")
local job = rb.call("rimworld/execute_context_menu_option", { label = "Prioritize toggling switch" })
rb.assert(job.result.success, "Close iris job could not be ordered.")
ticks = rb.call("rimworld/step_game_ticks", { ticks = 600, timeoutMs = 30000 })
rb.assert(ticks.result.success, "Close iris job stepping failed.")
selected = rb.call("rimworld/click_cell", { x = 110, z = 130, button = "left" })
rb.assert(selected.result.success, "Closed gate selection failed.")
local closed = rb.call("rimgate/asset_report", {})
rb.assert(closed.result.success and closed.result.irisComps[1].switchIsOn, "The real pawn job did not close the iris.")
local closedShot = rb.call("rimworld/take_screenshot", { fileName = "rimgate-iris-closed", outputDirectory = params.evidenceDirectory, includeTargets = false })
rb.assert(closedShot.result.success and closedShot.result.sizeBytes > 0, "Closed iris capture failed.")
gizmos = rb.call("rimworld/list_selected_gizmos", {})
local openRequested = false
for i, gizmo in ipairs(gizmos.result.gizmos) do
    if gizmo.label == "Open iris" then
        rb.assert(gizmo.disabled == false, "The Open iris command was disabled.")
        local open = rb.call("rimworld/execute_gizmo", { gizmoId = gizmo.id })
        rb.assert(open.result.success, "Open iris request failed.")
        openRequested = true
    end
end
rb.assert(openRequested, "The player's Open iris command was absent.")
pawn = rb.call("rimworld/select_pawn", { pawnId = colonists.result.colonists[1].pawnId })
rb.assert(pawn.result.success, "Test pawn reselection failed.")
menu = rb.call("rimworld/right_click_cell", { x = 110, z = 130 })
rb.assert(menu.result.success, "Open iris context menu failed.")
job = rb.call("rimworld/execute_context_menu_option", { label = "Prioritize toggling switch" })
rb.assert(job.result.success, "Open iris job could not be ordered.")
ticks = rb.call("rimworld/step_game_ticks", { ticks = 600, timeoutMs = 30000 })
rb.assert(ticks.result.success, "Open iris job stepping failed.")
selected = rb.call("rimworld/click_cell", { x = 110, z = 130, button = "left" })
rb.assert(selected.result.success, "Reopened gate selection failed.")
local opened = rb.call("rimgate/asset_report", {})
rb.assert(opened.result.success and opened.result.irisComps[1].switchIsOn == false, "The real pawn job did not reopen the iris.")
local shot = rb.call("rimworld/take_screenshot", { fileName = "rimgate-assets-1.7", outputDirectory = params.evidenceDirectory, includeTargets = false })
rb.assert(shot.success and shot.result.success and shot.result.sizeBytes > 0, "Asset screenshot capture failed.")
local logs = rb.call("rimbridge/list_logs", { minimumLevel = "error", limit = 50 })
for i, entry in ipairs(logs.result.logs) do
    rb.fail("The bridge journal contains an error; inspect before acceptance.", entry)
end
return { success = true, textures = report.result.textureCount, sounds = report.result.soundCount, iris = opened.result.irisComps[1], screenshot = shot.result.path, closedScreenshot = closedShot.result.path }
