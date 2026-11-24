-- Native gate orientation/controller checks: prepare then open, optionally iris.
-- Pass configPath, modRoot, evidenceDirectory, phase, rotation (0..3), controller
-- (Ancient|Tauri), capturePrefix. Later phases take prepare's returned fixture.
-- Does not overwrite the smoke/transit baseline saves. Output is private evidence.
local config = rb.call("rimworld/get_mod_configuration_status", {})
rb.assert(config.result.success and config.result.configPath == params.configPath and config.result.configurationIssueCount == 0, "Use the expected healthy private profile.")
local deployment = false
for i, mod in ipairs(config.result.activeMods) do
    if mod.packageId == "mrdav30.rimgate" then deployment = mod.loadedInSession and mod.rootDir == params.modRoot end
end
rb.assert(deployment, "Expected private deployment is not loaded.")
if params.phase == "prepare" then
    local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-bridge-smoke", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.result.success, "Load the owned smoke save.")
    local assets = rb.call("rimgate/asset_report", {})
    rb.assert(assets.result.success and assets.result.assetFormat == "WaylithPack", "Use the validated native pack.")
    local fixture = rb.call("rimgate/prepare_transit_fixture", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot })
    rb.assert(fixture.result.success, "Private source/site preparation failed.")
    local stage = rb.call("rimgate/stage_gate_presentation", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, rotation = params.rotation, controller = params.controller })
    rb.assert(stage.result.success, "Inactive presentation staging failed.")
    local ticks = rb.call("rimworld/step_game_ticks", { ticks = 750, timeoutMs = 30000 })
    rb.assert(ticks.result.success, "Power initialization failed.")
    local status = rb.call("rimgate/gate_presentation_status", {})
    rb.assert(status.result.success and status.result.dhdLinked and status.result.dhdPowered and status.result.Powered, "Fixture assets, power or real facility links are invalid.")
    rb.assert(status.result.rotation == params.rotation and status.result.IsActive == false, "Incorrect inactive rotation/state.")
    local camera = rb.call("rimworld/jump_camera_to_cell", { x = 110, z = 130 })
    rb.assert(camera.result.success, "Camera positioning failed.")
    local shot = rb.call("rimworld/take_screenshot", { fileName = params.capturePrefix .. "-inactive", outputDirectory = params.evidenceDirectory, includeTargets = false })
    rb.assert(shot.result.success, "Inactive capture failed.")
    return { success = true, fixture = fixture.result, status = status.result, screenshot = shot.result.path }
end
local f = params.fixture
if params.phase == "open" then
    local selected = rb.call("rimworld/select_pawn", { pawnId = f.pawnId })
    rb.assert(selected.result.success, "Dialer selection failed.")
    local menu = rb.call("rimworld/right_click_cell", { x = 110, z = 134 })
    rb.assert(menu.result.success, "DHD context menu failed.")
    local dial = rb.call("rimworld/execute_context_menu_option", { label = f.destinationName })
    rb.assert(dial.result.success, "Normal DHD dial job failed.")
    local ticks = rb.call("rimworld/step_game_ticks", { ticks = 80, timeoutMs = 30000 })
    rb.assert(ticks.result.success, "Dial stepping failed.")
    local queued = rb.call("rimgate/gate_presentation_status", {})
    rb.assert(queued.result.success and queued.result.IsOpeningQueued, "Queued chevron state not observed.")
    local shot = rb.call("rimworld/take_screenshot", { fileName = params.capturePrefix .. "-queued", outputDirectory = params.evidenceDirectory, includeTargets = false })
    rb.assert(shot.result.success, "Queued capture failed.")
    ticks = rb.call("rimworld/step_game_ticks", { ticks = queued.result.ticksUntilOpen + 1, timeoutMs = 30000 })
    if ticks.result.success == false then
        rb.assert(ticks.result.message == "RimWorld is busy with a long event.", "Unexpected opening step failure.")
    end
    local ready = rb.call("rimbridge/wait_for_long_event_idle", { timeoutMs = 120000 })
    rb.assert(ready.result.success, "Destination generation did not finish.")
    ready = rb.call("rimbridge/wait_for_game_loaded", { readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(ready.result.success, "Generated map was not ready.")
    ticks = rb.call("rimworld/step_game_ticks", { ticks = 20, timeoutMs = 30000 })
    rb.assert(ticks.result.success, "Opening effect stepping failed.")
    local opening = rb.call("rimgate/gate_presentation_status", {})
    rb.assert(opening.result.IsActive and opening.result.TicksSinceOpened < 150, "Opening effect window was missed.")
    shot = rb.call("rimworld/take_screenshot", { fileName = params.capturePrefix .. "-opening", outputDirectory = params.evidenceDirectory, includeTargets = false })
    rb.assert(shot.result.success, "Opening capture failed.")
    ticks = rb.call("rimworld/step_game_ticks", { ticks = 180, timeoutMs = 30000 })
    rb.assert(ticks.result.success, "Mature gate stepping failed.")
    local status = rb.call("rimgate/gate_presentation_status", {})
    rb.assert(status.result.success and status.result.IsActive and status.result.dhdPowered and status.result.dhdLinked and status.result.rotation == params.rotation, "Active gate/controller state or assets are invalid.")
    shot = rb.call("rimworld/take_screenshot", { fileName = params.capturePrefix .. "-active", outputDirectory = params.evidenceDirectory, includeTargets = false })
    rb.assert(shot.result.success, "Active capture failed.")
    return { success = true, queued = queued.result, opening = opening.result, status = status.result, screenshot = shot.result.path }
end
rb.assert(params.phase == "iris", "Choose a documented phase.")
local selected = rb.call("rimworld/click_cell", { x = 110, z = 130, button = "left" })
rb.assert(selected.result.success, "Iris gate selection failed.")
local gizmos = rb.call("rimworld/list_selected_gizmos", {})
local requested = false
for i, gizmo in ipairs(gizmos.result.gizmos) do
    if gizmo.label == "Close iris" then
        local command = rb.call("rimworld/execute_gizmo", { gizmoId = gizmo.id })
        rb.assert(command.result.success, "Normal iris request failed.")
        requested = true
    end
end
rb.assert(requested, "Close iris command was absent.")
selected = rb.call("rimworld/select_pawn", { pawnId = f.pawnId })
rb.assert(selected.result.success, "Iris operator selection failed.")
local menu = rb.call("rimworld/right_click_cell", { x = 110, z = 130 })
rb.assert(menu.result.success, "Iris job context menu failed.")
local job = rb.call("rimworld/execute_context_menu_option", { label = "Prioritize toggling switch" })
rb.assert(job.result.success, "Normal iris operation job failed.")
local ticks = rb.call("rimworld/step_game_ticks", { ticks = 200, timeoutMs = 30000 })
rb.assert(ticks.result.success, "Iris stepping failed.")
local status = rb.call("rimgate/gate_presentation_status", {})
rb.assert(status.result.success and status.result.IsActive and status.result.irisClosed, "Active iris did not close.")
local shot = rb.call("rimworld/take_screenshot", { fileName = params.capturePrefix .. "-iris-closed", outputDirectory = params.evidenceDirectory, includeTargets = false })
rb.assert(shot.result.success, "Closed-iris capture failed.")
return { success = true, status = status.result, screenshot = shot.result.path }
