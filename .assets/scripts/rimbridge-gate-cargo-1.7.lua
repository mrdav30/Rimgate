-- Run after transit prepare/dial/connect; pass its fixture and the private paths.
-- Phases: prepare, carry-reload, arrive, interrupt. prepare returns cargoId/count.
-- Later phases take cargoId/stackCount too. Optional cargoDef defaults to Gun_Autopistol.
-- Real targeting, hauling and carry/reload only; never calls gate admission directly.
-- Bulk arrival runs normal Ultrafast ticks without changing boost; overshoot is allowed.
-- Limit each invocation to 60 seconds. The host MUST pause in finally on any failure.
local config = rb.call("rimworld/get_mod_configuration_status", {})
rb.assert(config.result.success and config.result.configPath == params.configPath and config.result.configurationIssueCount == 0, "Use the healthy private profile.")
local deployment = false
for i, mod in ipairs(config.result.activeMods) do
    if mod.packageId == "mrdav30.rimgate" then deployment = mod.loadedInSession and mod.rootDir == params.modRoot end
end
rb.assert(deployment, "Expected private deployment is not loaded.")
local f = params.fixture
if params.phase == "prepare" then
    local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-transit-active", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.result.success, "Load the owned active connection.")
    local state = rb.call("rimgate/transit_status", { sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, itemId = f.itemId })
    rb.assert(state.result.reciprocal and state.result.source.IsActive and state.result.source.ticksSinceOpened >= 150 and state.result.source.irisClosed == false, "Use the mature outgoing link.")
    local cargo = rb.call("rimworld/spawn_thing", { defName = params.cargoDef or "Gun_Autopistol", stackCount = params.stackCount or 1, x = 114, z = 134 })
    rb.assert(cargo.result.success, "Loose fixture cargo creation failed.")
    return { success = true, cargoId = cargo.result.thingId, stackCount = cargo.result.stackCount }
end
rb.assert(params.phase == "carry-reload" or params.phase == "arrive" or params.phase == "interrupt", "Choose a documented phase.")
if params.phase == "carry-reload" then
    local selected = rb.call("rimworld/select_pawn", { pawnId = f.pawnId })
    rb.assert(selected.result.success, "Select the hauler.")
    local menu = rb.call("rimworld/right_click_cell", { x = 110, z = 130 })
    rb.assert(menu.result.success, "Open the real gate menu.")
    local order = rb.call("rimworld/execute_context_menu_option", { label = "Bring selected to Dwarfgate" })
    rb.assert(order.result.success, "Begin real cargo targeting.")
    local target = rb.call("rimworld/click_cell", { x = 114, z = 134, button = "left" })
    rb.assert(target.result.success, "Target the actual loose cargo.")
    local job = rb.call("rimworld/get_pawn_details", { pawnId = f.pawnId })
    rb.assert(job.result.job.defName == "Rimgate_BringToGate", "Targeting did not start the real hauling job.")
    local carrying = false
    for attempt = 1, 320 do
        if carrying == false then
            local ticks = rb.call("rimworld/step_game_ticks", { ticks = 1, timeoutMs = 5000 })
            rb.assert(ticks.result.success, "Approach/pickup stepping failed.")
            local state = rb.call("rimgate/transit_status", { sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, itemId = params.cargoId })
            for i, location in ipairs(state.result.item.locations) do
                if location.parentHolder == "Pawn_CarryTracker" then carrying = true end
            end
        end
    end
    rb.assert(carrying, "Did not observe the real carry owner before admission.")
    local saved = rb.call("rimworld/save_game", { saveName = "Rimgate-gate-cargo-held" })
    rb.assert(saved.result.success and saved.result.exists, "Carry checkpoint save failed.")
end
local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-gate-cargo-held", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
rb.assert(loaded.result.success, "Carry checkpoint reload failed.")
if params.phase == "interrupt" then
    local drafted = rb.call("rimworld/set_draft", { pawnId = f.pawnId, drafted = true })
    rb.assert(drafted.result.success, "Normal job interruption failed.")
    local ticks = rb.call("rimworld/step_game_ticks", { ticks = 1, timeoutMs = 5000 })
    rb.assert(ticks.result.success, "Interruption stepping failed.")
elseif params.phase == "arrive" then
    local clockStart = rb.call("rimgate/transit_status", { sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, itemId = params.cargoId })
    rb.assert(clockStart.success and clockStart.result.success, "Read the actual starting clock.")
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Run ordinary engine ticks.")
    local elapsed = rb.poll("rimgate/transit_status", { sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, itemId = params.cargoId }, { timeoutMs = 15000, pollIntervalMs = 50, condition = { path = "result.tick", greaterThanOrEqual = clockStart.result.tick + 500 } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success, "Pause after the bulk clock wait.")
    rb.assert(elapsed.success and elapsed.result.tick >= clockStart.result.tick + 500, "Resumed hauling/transit clock wait failed.")
end
local state = rb.call("rimgate/transit_status", { sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, itemId = params.cargoId })
rb.assert(state.result.item.distinctObjectCount == 1 and state.result.item.ownershipOccurrenceCount == 1, "Cargo was lost, duplicated or multiply owned.")
local valid = false
for i, location in ipairs(state.result.item.locations) do
    rb.assert(location.stackCount == params.stackCount and location.destroyed == false, "Cargo quantity changed or was destroyed.")
    if params.phase == "arrive" then
        if location.spawned and location.mapId == state.result.destination.mapId then valid = true end
    elseif params.phase == "carry-reload" then
        if location.parentHolder == "Pawn_CarryTracker" then valid = true end
    else
        if location.mapId == state.result.source.mapId or location.parentHolder == "Pawn_CarryTracker" then valid = true end
    end
end
rb.assert(valid, "Cargo did not occupy the expected carry/source/arrival location.")
return { success = true, status = state.result }
