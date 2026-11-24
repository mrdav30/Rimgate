-- Native 1.7 gate acceptance, in bounded phases: prepare, dial, connect,
-- send, receive, close. Pass configPath, modRoot, evidenceDirectory and phase.
-- Reuse prepare's returned fixture as params.fixture in later phases.
-- send/receive reload the owned active checkpoint; close follows either arrival.
-- Optional cancelPending=true with send exercises developer Force close cleanup.
local config = rb.call("rimworld/get_mod_configuration_status", {})
rb.assert(config.success and config.result.success, "Configuration query failed.")
rb.assert(config.result.configPath == params.configPath, "Refusing a different save-data profile.")
rb.assert(config.result.configurationIssueCount == 0, "Resolve configuration issues first.")
local deployment = false
for i, mod in ipairs(config.result.activeMods) do
    if mod.packageId == "mrdav30.rimgate" then
        deployment = mod.loadedInSession and mod.rootDir == params.modRoot
    end
end
rb.assert(deployment, "The expected private Rimgate deployment is not loaded.")
if params.phase == "prepare" then
    local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-bridge-smoke", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.success and loaded.result.success, "Load the owned smoke save first.")
    local assets = rb.call("rimgate/asset_report", {})
    rb.assert(assets.result.success and assets.result.assetFormat == "WaylithPack", "This case requires the validated native package.")
    local first = rb.call("rimgate/prepare_transit_fixture", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot })
    rb.assert(first.result.success, "First fixture preparation failed.")
    loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-bridge-smoke", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.result.success, "Same-process reload failed.")
    local fixture = rb.call("rimgate/prepare_transit_fixture", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot })
    rb.assert(fixture.result.success, "Reloaded fixture preparation failed.")
    local registered = rb.call("rimgate/transit_status", { sourceGateId = fixture.result.sourceGateId, destinationParentId = fixture.result.destinationParentId, pawnId = fixture.result.pawnId, itemId = fixture.result.itemId })
    rb.assert(registered.result.worldComponentIsCurrent, "The address cache belongs to the previous world.")
    local addressMatches = 0
    for i, address in ipairs(registered.result.addresses) do
        if address.tile == fixture.result.destinationTile then addressMatches = addressMatches + 1 end
    end
    rb.assert(addressMatches == 1, "The current world did not register the destination exactly once.")
    rb.assert(registered.result.destinationHasMap == false, "Preparation must leave destination generation to real dialing.")
    return { success = true, fixture = fixture.result, registration = registered.result }
end
rb.assert(params.phase == "dial" or params.phase == "connect" or params.phase == "send" or params.phase == "receive" or params.phase == "close", "Choose a documented phase.")
local f = params.fixture
local state = rb.call("rimgate/transit_status", { sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, itemId = f.itemId })
rb.assert(state.success and state.result.worldComponentIsCurrent, "The fixture world/address owner is incorrect.")
if params.phase == "dial" then
    local power = rb.call("rimworld/step_game_ticks", { ticks = 750, timeoutMs = 30000 })
    rb.assert(power.result.success, "Power initialization failed.")
    local pawn = rb.call("rimworld/select_pawn", { pawnId = f.pawnId })
    rb.assert(pawn.result.success, "Traveler selection failed.")
    local menu = rb.call("rimworld/right_click_cell", { x = state.result.source.dhd.x, z = state.result.source.dhd.z })
    rb.assert(menu.result.success, "DHD context menu failed.")
    local dial = rb.call("rimworld/execute_context_menu_option", { label = f.destinationName })
    rb.assert(dial.result.success, "Normal DHD dial job failed.")
    local ticks = rb.call("rimworld/step_game_ticks", { ticks = 100, timeoutMs = 30000 })
    rb.assert(ticks.result.success, "Dial job stepping failed.")
    state = rb.call("rimgate/transit_status", { sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, itemId = f.itemId })
    rb.assert(state.result.source.openingQueued and state.result.destinationHasMap == false, "Dial must queue a lazy destination.")
    local saved = rb.call("rimworld/save_game", { saveName = "Rimgate-transit-queued" })
    rb.assert(saved.result.success and saved.result.exists, "Queued save failed.")
    local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-transit-queued", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.result.success, "Queued reload failed.")
    state = rb.call("rimgate/transit_status", { sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, itemId = f.itemId })
    local addressMatches = 0
    for i, address in ipairs(state.result.addresses) do
        if address.tile == f.destinationTile then addressMatches = addressMatches + 1 end
    end
    rb.assert(state.result.worldComponentIsCurrent and addressMatches == 1, "Queued reload lost the current address book.")
    rb.assert(state.result.source.openingQueued and state.result.source.ticksUntilOpen > 0, "Queued dial did not survive reload.")
    return { success = true, status = state.result, save = saved.result.path }
end
if params.phase == "connect" then
    rb.assert(state.result.source.openingQueued, "Run the queued dial phase first.")
    local ticks = rb.call("rimworld/step_game_ticks", { ticks = state.result.source.ticksUntilOpen + 1, timeoutMs = 30000 })
    if ticks.result.success == false then
        rb.assert(ticks.result.message == "RimWorld is busy with a long event.", "Unexpected stepping failure while opening the map.")
    end
    local idle = rb.call("rimbridge/wait_for_long_event_idle", { timeoutMs = 120000 })
    rb.assert(idle.success and idle.result.success, "Destination generation did not finish.")
    local ready = rb.call("rimbridge/wait_for_game_loaded", { readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(ready.result.success, "Generated destination was not ready.")
    ticks = rb.call("rimworld/step_game_ticks", { ticks = 180, timeoutMs = 30000 })
    rb.assert(ticks.result.success, "Vortex maturation stepping failed.")
    state = rb.call("rimgate/transit_status", { sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, itemId = f.itemId })
    rb.assert(state.result.ready and state.result.reciprocal, "Production dialing did not connect both gates.")
    rb.assert(state.result.source.IsActive and state.result.destination.IsReceivingGate, "Incorrect gate direction.")
    local saved = rb.call("rimworld/save_game", { saveName = "Rimgate-transit-active" })
    rb.assert(saved.result.success, "Active link save failed.")
    local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-transit-active", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.result.success, "Active link reload failed.")
    state = rb.call("rimgate/transit_status", { sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, itemId = f.itemId })
    rb.assert(state.result.reciprocal and state.result.worldComponentIsCurrent, "Active gate references did not survive reload.")
    local camera = rb.call("rimworld/jump_camera_to_cell", { x = state.result.source.x, z = state.result.source.z })
    rb.assert(camera.result.success, "Active gate camera positioning failed.")
    local shot = rb.call("rimworld/take_screenshot", { fileName = "rimgate-transit-active", outputDirectory = params.evidenceDirectory, includeTargets = false })
    rb.assert(shot.result.success, "Active gate screenshot failed.")
    return { success = true, status = state.result, save = saved.result.path, screenshot = shot.result.path }
end
if params.phase == "send" or params.phase == "receive" then
    local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-transit-active", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.result.success, "Load the owned active link checkpoint first.")
    local approach = rb.call("rimgate/stage_transit_approach", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, pawnId = f.pawnId, closerId = f.closerId, checkpoint = params.phase })
    rb.assert(approach.result.success, "Same-map approach staging failed.")
    if params.phase == "receive" then
        local dummy = rb.call("rimworld/select_pawn", { pawnId = approach.result.dummyId })
        rb.assert(dummy.result.success, "Priming traveler selection failed.")
        local menu = rb.call("rimworld/right_click_cell", { x = state.result.source.x, z = state.result.source.z })
        rb.assert(menu.result.success, "Priming traveler menu failed.")
        local enter = rb.call("rimworld/execute_context_menu_option", { label = "Enter Dwarfgate" })
        rb.assert(enter.result.success, "Priming traveler entry failed.")
    end
    local pawn = rb.call("rimworld/select_pawn", { pawnId = f.pawnId })
    rb.assert(pawn.result.success, "Tracked traveler selection failed.")
    local drafted = rb.call("rimworld/set_draft", { pawnId = f.pawnId, drafted = true })
    rb.assert(drafted.result.success, "Tracked traveler drafting failed.")
    local menu = rb.call("rimworld/right_click_cell", { x = state.result.source.x, z = state.result.source.z })
    rb.assert(menu.result.success, "Tracked traveler menu failed.")
    local enter = rb.call("rimworld/execute_context_menu_option", { label = "Enter Dwarfgate" })
    rb.assert(enter.result.success, "Normal gate entry job failed.")
    local reached = false
    for attempt = 1, 80 do
        if reached == false then
            state = rb.call("rimgate/transit_status", { sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, itemId = f.itemId })
            if params.phase == "send" then
                for i, id in ipairs(state.result.source.send) do
                    if id == f.pawnId then reached = true end
                end
            else
                for i, id in ipairs(state.result.destination.receive) do
                    if id == f.pawnId then reached = true end
                end
            end
            if reached == false then
                local ticks = rb.call("rimworld/step_game_ticks", { ticks = 1, timeoutMs = 5000 })
                rb.assert(ticks.result.success, "Checkpoint stepping failed.")
            end
        end
    end
    rb.assert(reached, "The requested real in-flight boundary was not observed.")
    rb.assert(state.result.pawn.distinctObjectCount == 1 and state.result.pawn.ownershipOccurrenceCount == 1, "Buffered pawn has incorrect ownership.")
    rb.assert(state.result.item.distinctObjectCount == 1 and state.result.item.ownershipOccurrenceCount == 1, "Buffered inventory item has incorrect ownership.")
    if params.cancelPending then
        rb.assert(params.phase == "send", "Cancellation starts from the source send checkpoint.")
        local checkpoint = state.result
        local selected = rb.call("rimworld/click_cell", { x = state.result.source.x, z = state.result.source.z, button = "left" })
        rb.assert(selected.result.success, "Pending source gate selection failed.")
        local gizmos = rb.call("rimworld/list_selected_gizmos", {})
        local closed = false
        for i, gizmo in ipairs(gizmos.result.gizmos) do
            if gizmo.label == "Force close" then
                local command = rb.call("rimworld/execute_gizmo", { gizmoId = gizmo.id })
                rb.assert(command.result.success, "Developer cancellation command failed.")
                closed = true
            end
        end
        rb.assert(closed, "Developer cancellation command was absent.")
        state = rb.call("rimgate/transit_status", { sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, itemId = f.itemId })
        rb.assert(state.result.source.IsActive == false and state.result.destination.IsActive == false, "Pending cancellation retained an active link.")
        rb.assert(state.result.source.pendingOwnedCount == 0 and state.result.source.sendCount == 0, "Cancellation left pending ownership behind.")
        rb.assert(state.result.pawn.pawn.Spawned and state.result.pawn.pawn.mapId == state.result.source.mapId, "Cancellation failed to return the pending pawn to its source map.")
        rb.assert(state.result.pawn.distinctObjectCount == 1 and state.result.pawn.ownershipOccurrenceCount == 1 and state.result.item.distinctObjectCount == 1 and state.result.item.ownershipOccurrenceCount == 1, "Cancellation lost or duplicated matter.")
        return { success = true, checkpoint = checkpoint, status = state.result, limits = "Used the existing developer Force close command while paused, exercising production cleanup without a close job race." }
    end
    local checkpointName = "Rimgate-transit-send"
    if params.phase == "receive" then checkpointName = "Rimgate-transit-receive" end
    local saved = rb.call("rimworld/save_game", { saveName = checkpointName })
    rb.assert(saved.result.success and saved.result.exists, "In-flight save failed.")
    local checkpoint = state.result
    loaded = rb.call("rimworld/load_game_ready", { saveName = checkpointName, readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.result.success, "In-flight reload failed.")
    state = rb.call("rimgate/transit_status", { sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, itemId = f.itemId })
    rb.assert(state.result.pawn.distinctObjectCount == 1 and state.result.item.distinctObjectCount == 1, "Reload lost or duplicated the traveler/cargo.")
    rb.assert(state.result.pawn.ownershipOccurrenceCount == 1 and state.result.item.ownershipOccurrenceCount == 1, "Reloaded matter has missing or multiple owners.")
    if state.result.pawn.pawn.Spawned == false then
        local queued = false
        for i, id in ipairs(state.result.source.send) do
            if id == f.pawnId then queued = true end
        end
        for i, id in ipairs(state.result.destination.receive) do
            if id == f.pawnId then queued = true end
        end
        rb.assert(queued, "Reload stranded an owned pawn outside the transit queues.")
    end
    local ticks = rb.call("rimworld/step_game_ticks", { ticks = 300, timeoutMs = 30000 })
    rb.assert(ticks.result.success, "Resumed transit stepping failed.")
    state = rb.call("rimgate/transit_status", { sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, itemId = f.itemId })
    rb.assert(state.result.pawn.pawn.mapId == state.result.destination.mapId and state.result.pawn.pawn.Spawned, "Traveler did not arrive on the destination map.")
    rb.assert(state.result.pawn.pawn.Drafted, "Arrival lost the traveler's redraft intent across reload.")
    rb.assert(state.result.pawn.ownershipOccurrenceCount == 1 and state.result.item.ownershipOccurrenceCount == 1, "Arrived matter has multiple or missing owners.")
    local inventoryMatches = 0
    for i, id in ipairs(state.result.pawn.pawn.inventory) do
        if id == f.itemId then inventoryMatches = inventoryMatches + 1 end
    end
    rb.assert(inventoryMatches == 1, "The unique inventory item did not arrive with its pawn.")
    rb.assert(state.result.source.sendCount == 0 and state.result.destination.receiveCount == 0, "Transit left pending matter behind.")
    return { success = true, checkpoint = checkpoint, status = state.result, save = saved.result.path }
end
if params.phase == "close" then
    local view = rb.call("rimgate/view_transit_map", { mapId = state.result.source.mapId })
    rb.assert(view.result.success, "Source map view failed.")
    local click = rb.call("rimworld/click_cell", { x = state.result.source.dhd.x, z = state.result.source.dhd.z, button = "left" })
    rb.assert(click.result.success, "Source DHD selection failed.")
    local gizmos = rb.call("rimworld/list_selected_gizmos", {})
    local requested = false
    for i, gizmo in ipairs(gizmos.result.gizmos) do
        if gizmo.label == "Close Dwarfgate" then
            rb.assert(gizmo.disabled == false, "The source close command was disabled.")
            local close = rb.call("rimworld/execute_gizmo", { gizmoId = gizmo.id })
            rb.assert(close.result.success, "Close request failed.")
            requested = true
        end
    end
    rb.assert(requested, "The normal DHD close command was absent.")
    local closer = rb.call("rimworld/select_pawn", { pawnId = f.closerId })
    rb.assert(closer.result.success, "Source closer selection failed.")
    local menu = rb.call("rimworld/right_click_cell", { x = state.result.source.dhd.x, z = state.result.source.dhd.z })
    rb.assert(menu.result.success, "Close-job menu failed.")
    local job = rb.call("rimworld/execute_context_menu_option", { label = "Prioritize closing" })
    rb.assert(job.result.success, "Normal close job failed.")
    local ticks = rb.call("rimworld/step_game_ticks", { ticks = 300, timeoutMs = 30000 })
    rb.assert(ticks.result.success, "Close job stepping failed.")
    state = rb.call("rimgate/transit_status", { sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, itemId = f.itemId })
    rb.assert(state.result.source.IsActive == false and state.result.destination.IsActive == false, "Closure did not close both ends.")
    rb.assert(state.result.source.connectedTile == -1 and state.result.destination.connectedTile == -1, "Closure retained a connection.")
    rb.assert(state.result.source.sendCount == 0 and state.result.source.receiveCount == 0 and state.result.destination.sendCount == 0 and state.result.destination.receiveCount == 0, "Closure left pending matter.")
    rb.assert(state.result.source.transporterCount == 0 and state.result.destination.transporterCount == 0, "Closure left transporter contents.")
    rb.assert(state.result.source.dhd.wantsClosed == false and state.result.destination.dhd.wantsClosed == false, "Closure retained a DHD request.")
    local saved = rb.call("rimworld/save_game", { saveName = "Rimgate-transit-closed" })
    rb.assert(saved.result.success, "Closed-state save failed.")
    local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-transit-closed", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.result.success, "Closed-state reload failed.")
    state = rb.call("rimgate/transit_status", { sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, itemId = f.itemId })
    rb.assert(state.result.source.IsActive == false and state.result.destination.IsActive == false and state.result.reciprocal == false, "Closed links reappeared after reload.")
    rb.assert(state.result.worldComponentIsCurrent and state.result.pawn.distinctObjectCount == 1 and state.result.item.distinctObjectCount == 1, "Closed-state reload lost ownership.")
    local logs = rb.call("rimbridge/list_logs", { minimumLevel = "error", limit = 50 })
    for i, entry in ipairs(logs.result.logs) do
        rb.fail("Bridge errors require investigation before accepting transit.", entry)
    end
    return { success = true, status = state.result, save = saved.result.path }
end
