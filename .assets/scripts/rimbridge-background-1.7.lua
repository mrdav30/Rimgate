-- RG-Issue-002 regression: prepare, negative, positive (separate calls).
-- Pass configPath, modRoot, evidenceDirectory, phase and a unique capturePrefix.
-- After prepare, pass its fixture to later phases. Start the bounded engine-
-- occlusion fixture separately: suppressRedraw=true for negative, false for
-- positive. Always end that fixture in the caller's finally/cleanup path.
local config = rb.call("rimworld/get_mod_configuration_status", {})
rb.assert(config.success and config.result.success, "Configuration query failed.")
rb.assert(config.result.configPath == params.configPath and config.result.configurationIssueCount == 0, "Use the healthy private profile.")
local deployment = false
for i, mod in ipairs(config.result.activeMods) do
    if mod.packageId == "mrdav30.rimgate" then deployment = mod.loadedInSession and mod.rootDir == params.modRoot end
end
rb.assert(deployment, "Expected private deployment is not loaded.")
if params.phase == "prepare" then
    local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-bridge-smoke", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.result.success, "Load the owned smoke save.")
    local fixture = rb.call("rimgate/prepare_transit_fixture", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot })
    rb.assert(fixture.result.success, "Private input fixture failed.")
    local camera = rb.call("rimworld/jump_camera_to_cell", { x = 110, z = 132 })
    rb.assert(camera.result.success, "Camera positioning failed.")
    local selected = rb.call("rimworld/select_pawn", { pawnId = fixture.result.pawnId })
    rb.assert(selected.result.success, "Fixture pawn selection failed.")
    local shot = rb.call("rimworld/take_screenshot", { fileName = params.capturePrefix .. "-before", outputDirectory = params.evidenceDirectory, includeTargets = false })
    rb.assert(shot.result.success and shot.result.sizeBytes > 0, "Require a presented hidden frame before simulating occlusion.")
    return { success = true, fixture = fixture.result, screenshot = shot.result.path }
end
rb.assert(params.phase == "negative" or params.phase == "positive", "Choose a documented phase.")
local f = params.fixture
local before = rb.call("rimgate/render_capture_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot })
rb.assert(before.result.backgroundRendering.Installed and before.result.renderLoop.hideWindow and before.result.renderLoop.headless == false, "The hidden rendering companion is not installed.")
rb.assert(before.result.occlusionFixture.completed == false and before.result.renderLoop.occluded and before.result.renderLoop.shouldPresent == false, "Start the bounded occlusion fixture before this phase.")
rb.assert(before.result.renderLoop.visible == false and before.result.renderLoop.focused == false, "The game must remain hidden and unfocused.")
rb.assert(before.result.renderLoop.hasSurface and before.result.renderLoop.surfaceIsZero == false, "A nonzero graphics surface is required for the occlusion control.")
if params.phase == "negative" then
    rb.assert(before.result.backgroundRendering.Suppressed and before.result.renderLoop.renderWanted == false, "Suppress redraws for the negative control.")
    local shot = rb.call("rimworld/take_screenshot", { fileName = params.capturePrefix .. "-negative", outputDirectory = params.evidenceDirectory, includeTargets = false })
    rb.assert(shot.success and shot.result.success == false and shot.result.message == "Timed out waiting for RimWorld to finish writing the screenshot file.", "The expected capture failure was not reproduced.")
    local menu = rb.call("rimworld/right_click_cell", { x = 110, z = 134 })
    rb.assert(menu.success and menu.result.success == false and menu.result.message == "Timed out waiting 2000ms for RimWorld to process the synthetic right-click.", "The expected synthetic-input failure was not reproduced.")
    local after = rb.call("rimgate/render_capture_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot })
    rb.assert(after.result.occlusionFixture.completed == false and after.result.renderLoop.occluded and after.result.renderLoop.renderWanted == false, "The negative fixture ended before observation.")
    rb.assert(after.result.frame > before.result.frame and after.result.pendingFileCount > 0 and after.result.captureRequested, "The expected update-without-capture state was not observed.")
    rb.assert(after.result.renderLoop.hasSurface and after.result.renderLoop.surfaceIsZero == false, "The negative control lost its graphics surface.")
    return { success = true, negativeControl = true, before = before.result, after = after.result, captureFailure = shot.result, inputFailure = menu.result, cleanupRequired = true }
end
rb.assert(before.result.backgroundRendering.Suppressed == false, "Enable redraws for the positive control.")
for attempt = 1, 5 do
    local click = rb.call("rimworld/click_cell", { x = 110, z = 130, button = "left" })
    rb.assert(click.result.success, "Occluded left-click failed.")
    local selection = rb.call("rimworld/get_selection_semantics", {})
    rb.assert(selection.result.selectedObjects[1].id == f.sourceGateId, "Occluded click selected the wrong gate.")
    local pawn = rb.call("rimworld/select_pawn", { pawnId = f.pawnId })
    rb.assert(pawn.result.success, "Pawn selection failed.")
    local menu = rb.call("rimworld/right_click_cell", { x = 110, z = 134 })
    rb.assert(menu.result.success and menu.result.provider == "ui_event", "Occluded right-click did not traverse real UI events.")
    local found = false
    for i, option in ipairs(menu.result.options) do
        if option.label == "Dial " .. f.destinationDesignation .. " (" .. f.destinationName .. ")" then found = true end
    end
    rb.assert(found, "The expected real DHD option was not generated.")
    local shot = rb.call("rimworld/take_screenshot", { fileName = params.capturePrefix .. "-positive-" .. attempt, outputDirectory = params.evidenceDirectory, includeTargets = false })
    rb.assert(shot.result.success and shot.result.sizeBytes > 0, "Occluded capture failed.")
    local closed = rb.call("rimworld/close_context_menu", {})
    rb.assert(closed.result.success, "Context-menu cleanup failed.")
end
local click = rb.call("rimworld/click_cell", { x = 110, z = 130, button = "left" })
rb.assert(click.result.success, "Gate reselection failed.")
local beforeSave = rb.call("rimworld/get_selection_semantics", {})
local saved = rb.call("rimworld/save_game", { saveName = "Rimgate-background-check" })
rb.assert(saved.result.success and saved.result.exists, "Background checkpoint save failed.")
local gizmos = rb.call("rimworld/list_selected_gizmos", {})
local changed = false
for i, gizmo in ipairs(gizmos.result.gizmos) do
    if gizmo.label == "Add/remove iris" then
        local command = rb.call("rimworld/execute_gizmo", { gizmoId = gizmo.id })
        rb.assert(command.result.success, "Post-save iris mutation failed.")
        changed = true
    end
end
rb.assert(changed, "The owned gate needs its development iris command.")
local mutation = rb.call("rimworld/get_selection_semantics", {})
rb.assert(mutation.result.selectedObjects[1].inspectString ~= beforeSave.result.selectedObjects[1].inspectString, "The post-save mutation did not change the iris state.")
local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-background-check", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
rb.assert(loaded.result.success, "Occluded save reload failed.")
click = rb.call("rimworld/click_cell", { x = 110, z = 130, button = "left" })
rb.assert(click.result.success, "Occluded selection after reload failed.")
local restored = rb.call("rimworld/get_selection_semantics", {})
rb.assert(restored.result.selectedObjects[1].id == f.sourceGateId and restored.result.selectedObjects[1].inspectString == beforeSave.result.selectedObjects[1].inspectString, "Reload did not restore gate identity/iris state.")
local ticks = rb.call("rimworld/step_game_ticks", { ticks = 120, timeoutMs = 30000 })
rb.assert(ticks.result.success and ticks.result.advancedTicks == 120 and ticks.result.advancedFrames == 120, "Exact background tick stepping failed.")
local shot = rb.call("rimworld/take_screenshot", { fileName = params.capturePrefix .. "-reloaded", outputDirectory = params.evidenceDirectory, includeTargets = false })
rb.assert(shot.result.success, "Capture after occluded reload failed.")
local after = rb.call("rimgate/render_capture_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot })
rb.assert(after.result.occlusionFixture.completed == false and after.result.renderLoop.occluded and after.result.renderLoop.shouldPresent == false, "The positive fixture ended before observation.")
rb.assert(after.result.renderLoop.visible == false and after.result.renderLoop.focused == false, "The window became visible or focused.")
rb.assert(after.result.renderLoop.hasSurface and after.result.renderLoop.surfaceIsZero == false, "The positive control lost its graphics surface.")
rb.assert(after.result.backgroundRendering.RedrawRequests > before.result.backgroundRendering.RedrawRequests and after.result.pendingFileCount == 0 and after.result.captureRequested == false, "Redraw/capture completion was not observed.")
local logs = rb.call("rimbridge/list_logs", { minimumLevel = "error", limit = 50 })
for i, entry in ipairs(logs.result.logs) do rb.fail("Investigate bridge errors before accepting background operation.", entry) end
return { success = true, negativeControl = false, before = before.result, after = after.result, screenshot = shot.result.path, ticks = ticks.result.advancedTicks, save = saved.result.path, cleanupRequired = true }
