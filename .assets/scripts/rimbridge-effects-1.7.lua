-- Conditional native iris rendering and lifecycle: prepare, condition, reload, end.
-- Pass configPath, modRoot, evidenceDirectory, rotation (0..3), capturePrefix,
-- phase and prepare's fixture. end additionally takes the discovered endConditionPath.
-- Uses the owned smoke save, real iris job and the game's real psychic-drone incident.
local config = rb.call("rimworld/get_mod_configuration_status", {})
rb.assert(config.result.success and config.result.configPath == params.configPath and config.result.configurationIssueCount == 0, "Use the healthy private profile.")
local deployment = false
for i, mod in ipairs(config.result.activeMods) do
    if mod.packageId == "mrdav30.rimgate" then deployment = mod.loadedInSession and mod.rootDir == params.modRoot end
end
rb.assert(deployment, "Expected private deployment is not loaded.")
if params.phase == "prepare" then
    local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-bridge-smoke", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.result.success, "Load the owned smoke save.")
    local assets = rb.call("rimgate/asset_report", {})
    rb.assert(assets.result.success and assets.result.assetFormat == "WaylithPack", "Use the native pack.")
    local fixture = rb.call("rimgate/prepare_transit_fixture", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot })
    rb.assert(fixture.result.success, "Private preparation failed.")
    local stage = rb.call("rimgate/stage_gate_presentation", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, rotation = params.rotation, controller = "Ancient" })
    rb.assert(stage.result.success, "Inactive staging failed.")
    local ticks = rb.call("rimworld/step_game_ticks", { ticks = 750, timeoutMs = 30000 })
    rb.assert(ticks.result.success, "Power initialization failed.")
    local selected = rb.call("rimworld/click_cell", { x = 110, z = 130, button = "left" })
    rb.assert(selected.result.success, "Gate selection failed.")
    local gizmos = rb.call("rimworld/list_selected_gizmos", {})
    local requested = false
    for i, gizmo in ipairs(gizmos.result.gizmos) do
        if gizmo.label == "Close iris" then
            local command = rb.call("rimworld/execute_gizmo", { gizmoId = gizmo.id })
            rb.assert(command.result.success, "Iris request failed.")
            requested = true
        end
    end
    rb.assert(requested, "Close iris command was absent.")
    selected = rb.call("rimworld/select_pawn", { pawnId = fixture.result.pawnId })
    rb.assert(selected.result.success, "Operator selection failed.")
    local menu = rb.call("rimworld/right_click_cell", { x = 110, z = 130 })
    rb.assert(menu.result.success, "Iris job menu failed.")
    local job = rb.call("rimworld/execute_context_menu_option", { label = "Prioritize toggling switch" })
    rb.assert(job.result.success, "Real iris job failed.")
    ticks = rb.call("rimworld/step_game_ticks", { ticks = 200, timeoutMs = 30000 })
    rb.assert(ticks.result.success, "Iris stepping failed.")
    local status = rb.call("rimgate/gate_presentation_status", {})
    rb.assert(status.result.success and status.result.irisClosed and status.result.Powered and status.result.IsActive == false and status.result.ExternalHoldCount == 0, "Prepare a powered inactive closed iris.")
    local camera = rb.call("rimworld/jump_camera_to_cell", { x = 110, z = 130 })
    rb.assert(camera.result.success, "Camera failed.")
    local zoom = rb.call("rimworld/set_camera_zoom", { rootSize = 11 })
    rb.assert(zoom.result.success, "Close zoom failed.")
    local shot = rb.call("rimworld/take_screenshot", { fileName = params.capturePrefix .. "-before", outputDirectory = params.evidenceDirectory, includeTargets = false })
    rb.assert(shot.result.success, "Baseline capture failed.")
    return { success = true, fixture = fixture.result, status = status.result }
end
rb.assert(params.phase == "condition" or params.phase == "reload" or params.phase == "end", "Choose a documented phase.")
if params.phase == "condition" then
    local browse = rb.call("rimworld/list_debug_action_children", { path = "Actions\\Do incident" })
    rb.assert(browse.result.success, "Incident discovery failed.")
    local incident = rb.call("rimworld/execute_debug_action", { path = "Actions\\Do incident\\Rimgate_GatePsychicDrone" })
    rb.assert(incident.result.success, "Production incident failed.")
end
if params.phase == "reload" then
    local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-effects-condition", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.result.success, "Condition reload failed.")
end
if params.phase == "end" then
    local browse = rb.call("rimworld/list_debug_action_children", { path = "Actions\\Remove Game Condition..." })
    rb.assert(browse.result.success, "Condition-end discovery failed.")
    local ended = rb.call("rimworld/execute_debug_action", { path = params.endConditionPath })
    rb.assert(ended.result.success, "Normal condition end failed.")
end
local ticks = rb.call("rimworld/step_game_ticks", { ticks = 250, timeoutMs = 30000 })
rb.assert(ticks.result.success, "Condition stepping failed.")
local status = rb.call("rimgate/gate_presentation_status", {})
rb.assert(status.result.success and status.result.irisClosed and status.result.Powered, "Iris state or assets are invalid.")
if params.phase == "end" then
    rb.assert(status.result.IsActive == false and status.result.ExternalHoldCount == 0 and status.result.gateConditionActive == false and status.result.glowRequested == false, "Condition end left a gate hold or glow behind.")
else
    rb.assert(status.result.IsActive and status.result.IsReceivingGate and status.result.ExternalHoldCount == 1 and status.result.glowRequested, "Condition/receiver hold did not survive.")
    local held = 0
    for i, condition in ipairs(status.result.conditions) do
        rb.assert(condition.defName == "Rimgate_GatePsychicDrone" and condition.heldGateId == params.fixture.sourceGateId and condition.TicksLeft > 0, "Condition lost its held gate or duration.")
        held = held + 1
    end
    rb.assert(held == 1, "Expected exactly one owned gate condition.")
end
local camera = rb.call("rimworld/jump_camera_to_cell", { x = 110, z = 130 })
rb.assert(camera.result.success, "Camera positioning failed.")
local zoom = rb.call("rimworld/set_camera_zoom", { rootSize = 11 })
rb.assert(zoom.result.success, "Close zoom failed.")
local shot = rb.call("rimworld/take_screenshot", { fileName = params.capturePrefix .. "-" .. params.phase, outputDirectory = params.evidenceDirectory, includeTargets = false })
rb.assert(shot.result.success, "Conditional capture failed.")
if params.phase == "condition" then
    local saved = rb.call("rimworld/save_game", { saveName = "Rimgate-effects-condition" })
    rb.assert(saved.result.success and saved.result.exists, "Condition checkpoint failed.")
end
return { success = true, status = status.result }
