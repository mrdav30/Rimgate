-- After a real cargo-kind arrival: close, checkpoint, dial, connect, status.
-- Pass the cargo-kind fixture, carriedCargoId and separate source closerId.
-- Reads actual unloaded value records; no original item ID claim after recreation.
-- Downed player pawns may natively prevent map removal and must remain owned.
-- Retain actual scavenging state; never alter RNG or snapshot/restore methods.
-- Compile with installed SDK. Host MUST pause in finally on failure.
local f = params.fixture
local cargoId = params.carriedCargoId or f.cargoId
local state = rb.call("rimgate/blocked_landing_report", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, receiverGateId = f.receiverGateId, destinationParentId = f.destinationParentId, cargoId = cargoId })
rb.assert(state.success and state.result.success, "Use the actual owned receiving site.")
if params.phase == "close" then
    rb.assert(state.result.destinationHasMap and state.result.sourceActive, "Close only after real loaded arrival.")
    local selected = rb.call("rimworld/click_cell", { x = 110, z = 134, button = "left" })
    rb.assert(selected.success and selected.result.success, "Select the real source DHD.")
    local commands = rb.call("rimworld/list_selected_gizmos", {})
    rb.assert(commands.success and commands.result.success, "Read actual source controls.")
    local requested = false
    for i, command in ipairs(commands.result.gizmos) do
        if command.label == "Close Dwarfgate" then
            local clicked = rb.call("rimworld/execute_gizmo", { gizmoId = command.id })
            rb.assert(clicked.success and clicked.result.success, "Request normal closure.")
            requested = true
        end
    end
    rb.assert(requested, "The native close command is absent.")
    selected = rb.call("rimworld/select_pawn", { pawnId = params.closerId })
    rb.assert(selected.success and selected.result.success, "Select the separate actual closer.")
    local menu = rb.call("rimworld/right_click_cell", { x = 110, z = 134 })
    rb.assert(menu.success and menu.result.success and menu.result.provider == "ui_event", "Open the real close work menu.")
    local order = rb.call("rimworld/execute_context_menu_option", { label = "Prioritize closing" })
    rb.assert(order.success and order.result.success, "Order the production close job.")
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Run ordinary closure ticks.")
    local closed = rb.poll("rimgate/blocked_landing_report", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, receiverGateId = f.receiverGateId, destinationParentId = f.destinationParentId, cargoId = cargoId }, { timeoutMs = 20000, pollIntervalMs = 50, condition = { path = "result.sourceActive", equals = false } })
    rb.assert(closed.success and closed.result.success and closed.result.sourceActive == false, "The real close job must close the source.")
    local settled = rb.poll("rimgate/blocked_landing_report", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, receiverGateId = f.receiverGateId, destinationParentId = f.destinationParentId, cargoId = cargoId }, { timeoutMs = 15000, pollIntervalMs = 50, condition = { path = "result.tick", greaterThanOrEqual = state.result.tick + 2000 } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success and settled.success and settled.result.success and settled.result.tick >= state.result.tick + 2000, "Pause after verified ordinary world-map lifetime checks.")
elseif params.phase == "checkpoint" then
    local saved = rb.call("rimworld/save_game", { saveName = "Rimgate-cargo-site-" .. f.mode })
    rb.assert(saved.success and saved.result.success and saved.result.exists, "Save the actual closed site.")
    local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-cargo-site-" .. f.mode, readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.success and loaded.result.success, "Reload actual map lifetime/storage state.")
elseif params.phase == "dial" then
    local selected = rb.call("rimworld/select_pawn", { pawnId = params.closerId })
    rb.assert(selected.success and selected.result.success, "Select the real source dialer.")
    local menu = rb.call("rimworld/right_click_cell", { x = 110, z = 134 })
    rb.assert(menu.success and menu.result.success and menu.result.provider == "ui_event", "Open the real source dial menu.")
    local order = rb.call("rimworld/execute_context_menu_option", { label = "Rimgate transit destination" })
    rb.assert(order.success and order.result.success, "Dial the same registered site normally.")
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Allow the real dial job and map generation.")
    local connected = rb.poll("rimgate/blocked_landing_report", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, receiverGateId = f.receiverGateId, destinationParentId = f.destinationParentId, cargoId = cargoId }, { timeoutMs = 30000, pollIntervalMs = 100, condition = { path = "result.sourceActive", equals = true } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success and connected.success and connected.result.success and connected.result.sourceActive and connected.result.destinationHasMap, "Pause after a verified real connection.")
elseif params.phase == "connect" then
    local idle = rb.call("rimbridge/wait_for_long_event_idle", { timeoutMs = 120000 })
    rb.assert(idle.success and idle.result.success, "Wait for actual native map generation.")
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Run ordinary reopened-map ticks.")
    local settled = rb.poll("rimgate/blocked_landing_report", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, receiverGateId = f.receiverGateId, destinationParentId = f.destinationParentId, cargoId = cargoId }, { timeoutMs = 15000, pollIntervalMs = 100, condition = { path = "result.tick", greaterThanOrEqual = state.result.tick + 180 } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success and settled.success and settled.result.success and settled.result.tick >= state.result.tick + 180 and settled.result.sourceActive and settled.result.destinationHasMap, "Pause after verified reopened-map progression.")
else
    rb.assert(params.phase == "status", "Choose a documented site phase.")
end
local after = rb.call("rimgate/blocked_landing_report", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, receiverGateId = f.receiverGateId, destinationParentId = f.destinationParentId, cargoId = cargoId })
rb.assert(after.success and after.result.success, "Observe actual stored records or loaded items without recreating them.")
return { success = true, before = state.result, after = after.result, limits = "Lifecycle observation; host must assert quantities/subject ownership and distinguish native scavenging. Stored value records do not preserve original item IDs. No snapshot, loot chance, world-pawn registration or map-removal setter is invoked." }
