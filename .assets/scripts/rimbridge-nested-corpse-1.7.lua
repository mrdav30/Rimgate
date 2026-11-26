-- RG-Issue-028: real nested-corpse cart loading, travel and unloaded-site lifecycle.
-- Pass private configPath/modRoot and fixture (cart prep + corpse prep IDs).
-- Set only corpse and loose sibling quantities through the actual manifest UI.
-- Phases: load(acceptTargetId), loaded, push, close, dial-menu/dial(dialLabel,
-- fromDestination), entry-menu/enter(entryLabel, fromDestination), unload,
-- checkpoint(saveName), park(parkX/parkZ), settle, status, assert-intact. Host MUST pause on failure.
-- idle-close waits for ordinary outgoing idle expiry; hold-home prevents wandering
-- into the next opening vortex. assert-intact selects the cart by exact corpse ID.
-- Never enter a receiving gate; reverse-dial home before returning the operator.
-- Then use idle-close (home is now receiving). Before reopening, hold-home drafts
-- the returned operator at an already-safe cell; use real movement first if needed.
local config = rb.call("rimworld/get_mod_configuration_status", {})
rb.assert(config.success and config.result.success and config.result.configPath == params.configPath and config.result.configurationIssueCount == 0, "Use the expected healthy private profile.")
local f = params.fixture
local before = rb.call("rimgate/nested_corpse_report", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, corpseId = f.corpseId, subjectPawnId = f.subjectPawnId, markerId = f.markerId, originalCartId = f.cartId })
rb.assert(before.success and before.result.success, "Read actual native owners before the phase.")
if params.phase == "hold-home" then
    rb.assert(before.result.pusherPawn and before.result.pusherPawn.dead == false and before.result.pusherPawn.downed == false and before.result.pusherPawn.mapId == f.sourceMapId, "Hold a healthy operator already home.")
    local state = rb.call("rimgate/transit_status", { sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, itemId = f.corpseId })
    rb.assert(state.success and state.result.success, "Read the actual home gate.")
    local dx = before.result.pusherPawn.position.x - state.result.source.x
    local dz = before.result.pusherPawn.position.z - state.result.source.z
    rb.assert(dx * dx + dz * dz >= 25, "Use real movement to leave the opening-vortex area before holding the operator.")
    local held = rb.call("rimworld/set_draft", { pawnId = f.pawnId, drafted = true })
    rb.assert(held.success and held.result.success, "Hold the operator through the real draft control before reopening.")
elseif params.phase == "load" then
    local accepted = rb.call("rimworld/click_ui_target", { targetId = params.acceptTargetId })
    rb.assert(accepted.success and accepted.result.success, "Accept the actual player-selected manifest.")
    local selected = rb.call("rimworld/select_pawn", { pawnId = f.pawnId })
    rb.assert(selected.success and selected.result.success, "Select the real hauler.")
    local menu = rb.call("rimworld/right_click_cell", { x = f.cartX, z = f.cartZ })
    rb.assert(menu.success and menu.result.success, "Open actual cart work.")
    local ordered = rb.call("rimworld/execute_context_menu_option", { label = "Prioritize loading steel wheelbarrow" })
    rb.assert(ordered.success and ordered.result.success, "Order normal corpse/cart loading.")
elseif params.phase == "loaded" or params.phase == "push" then
    rb.assert(before.result.graph.exactInnerPawn and before.result.graph.exactMarkerInInventory, "The original corpse/pawn/marker input must still be intact.")
    local expectedMap = f.sourceMapId
    if params.phase == "push" then
        rb.assert(before.result.carts[1].heldCount == 2 and before.result.corpse.ownershipOccurrenceCount == 1, "Load the corpse and ordinary sibling through real jobs first.")
        local selected = rb.call("rimworld/select_pawn", { pawnId = f.pawnId })
        rb.assert(selected.success and selected.result.success, "Select the real cart pusher.")
        local menu = rb.call("rimworld/right_click_cell", { x = 110, z = 130 })
        rb.assert(menu.success and menu.result.success, "Open the real outgoing gate menu.")
        local ordered = rb.call("rimworld/execute_context_menu_option", { label = "Bring selected to Dwarfgate" })
        rb.assert(ordered.success and ordered.result.success, "Start native loaded-cart targeting.")
        local target = rb.call("rimworld/click_cell", { x = f.cartX, z = f.cartZ, button = "left" })
        rb.assert(target.success and target.result.success, "Choose the actual loaded cart.")
        expectedMap = before.result.destinationMapId
    end
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Run ordinary hauling/pushing ticks.")
    local completed = rb.poll("rimgate/nested_corpse_report", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, corpseId = f.corpseId, subjectPawnId = f.subjectPawnId, markerId = f.markerId, originalCartId = f.cartId }, { timeoutMs = 30000, pollIntervalMs = 50, condition = { all = { { path = "result.carts", countEquals = 1 }, { path = "result.proxies", countEquals = 0 }, { path = "result.carts", allItems = { all = { { path = "heldCount", equals = 2 }, { path = "mapId", equals = expectedMap } } } }, { path = "result.pusher.locations", allItems = { path = "mapId", equals = expectedMap } } } } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success and completed.success and completed.result.success, "Pause after actual loaded-cart completion.")
elseif params.phase == "park" then
    rb.assert(before.result.destinationHasMap and before.result.sourceActive == false, "Move the arrived cart only with the connection closed.")
    local view = rb.call("rimgate/view_transit_map", { mapId = before.result.destinationMapId })
    rb.assert(view.success and view.result.success, "View the actual destination cart.")
    local selected = rb.call("rimworld/select_pawn", { pawnId = f.pawnId })
    rb.assert(selected.success and selected.result.success, "Select its actual pusher.")
    local menu = rb.call("rimworld/right_click_cell", { x = before.result.carts[1].cell.x, z = before.result.carts[1].cell.z })
    rb.assert(menu.success and menu.result.success, "Open the actual cart push menu.")
    local ordered = rb.call("rimworld/execute_context_menu_option", { label = "Push to..." })
    rb.assert(ordered.success and ordered.result.success, "Request a real ordinary cart push.")
    local target = rb.call("rimworld/click_cell", { x = params.parkX, z = params.parkZ, button = "left" })
    rb.assert(target.success and target.result.success, "Choose an ordinary safe parking cell.")
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Run ordinary cart movement.")
    local parked = rb.poll("rimgate/nested_corpse_report", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, corpseId = f.corpseId, subjectPawnId = f.subjectPawnId, markerId = f.markerId, originalCartId = f.cartId }, { timeoutMs = 20000, pollIntervalMs = 50, condition = { all = { { path = "result.carts", countEquals = 1 }, { path = "result.proxies", countEquals = 0 }, { path = "result.carts", allItems = { all = { { path = "cell.x", greaterThanOrEqual = params.parkX - 1 }, { path = "cell.x", lessThanOrEqual = params.parkX + 1 }, { path = "cell.z", greaterThanOrEqual = params.parkZ - 1 }, { path = "cell.z", lessThanOrEqual = params.parkZ + 1 }, { path = "overlapsVortex", equals = false } } } } } } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success and parked.success and parked.result.success, "Pause after actual safe parking, before reversing the gate.")
elseif params.phase == "close" then
    local view = rb.call("rimgate/view_transit_map", { mapId = f.sourceMapId })
    rb.assert(view.success and view.result.success, "View the actual home controller.")
    local selected = rb.call("rimworld/click_cell", { x = 110, z = 134, button = "left" })
    rb.assert(selected.success and selected.result.success, "Select the real home DHD.")
    local commands = rb.call("rimworld/list_selected_gizmos", {})
    rb.assert(commands.success and commands.result.success, "Read the real close command.")
    local requested = false
    for i, command in ipairs(commands.result.gizmos) do
        if command.label == "Close Dwarfgate" then
            rb.assert(command.disabled == false, "An incoming connection cannot be closed here; use idle-close after returning home.")
            local clicked = rb.call("rimworld/execute_gizmo", { gizmoId = command.id })
            rb.assert(clicked.success and clicked.result.success, "Request normal closure.")
            requested = true
        end
    end
    rb.assert(requested, "The actual close command must exist.")
    selected = rb.call("rimworld/select_pawn", { pawnId = f.closerId })
    rb.assert(selected.success and selected.result.success, "Select the separate home closer.")
    local menu = rb.call("rimworld/right_click_cell", { x = 110, z = 134 })
    rb.assert(menu.success and menu.result.success, "Open the native closing work menu.")
    local ordered = rb.call("rimworld/execute_context_menu_option", { label = "Prioritize closing" })
    rb.assert(ordered.success and ordered.result.success, "Order the actual DHD close job.")
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Run normal closure ticks.")
    local closed = rb.poll("rimgate/nested_corpse_report", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, corpseId = f.corpseId, subjectPawnId = f.subjectPawnId, markerId = f.markerId, originalCartId = f.cartId }, { timeoutMs = 20000, pollIntervalMs = 50, condition = { path = "result.sourceActive", equals = false } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success and closed.success and closed.result.success, "Pause after normal closure.")
elseif params.phase == "dial-menu" or params.phase == "dial" or params.phase == "entry-menu" or params.phase == "enter" then
    local state = rb.call("rimgate/transit_status", { sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, itemId = f.corpseId })
    rb.assert(state.success and state.result.success, "Read actual gate direction and controls.")
    local control = state.result.source
    local actor = f.closerId
    local expectedMap = f.sourceMapId
    if state.result.destination ~= nil then expectedMap = state.result.destination.mapId end
    if params.fromDestination then control = state.result.destination; actor = f.pawnId; expectedMap = f.sourceMapId end
    local view = rb.call("rimgate/view_transit_map", { mapId = control.mapId })
    rb.assert(view.success and view.result.success, "View the actor's actual map.")
    local selected = rb.call("rimworld/select_pawn", { pawnId = actor })
    rb.assert(selected.success and selected.result.success, "Select the real operator.")
    local x = control.x
    local z = control.z
    if params.phase == "dial-menu" or params.phase == "dial" then x = control.dhd.x; z = control.dhd.z end
    local menu = rb.call("rimworld/right_click_cell", { x = x, z = z })
    rb.assert(menu.success and menu.result.success, "Open the actual gate/controller menu.")
    if params.phase == "dial-menu" or params.phase == "entry-menu" then return { success = true, menu = menu.result, transit = state.result, status = before.result } end
    local label = params.dialLabel
    if params.phase == "enter" then
        rb.assert(control.IsActive and control.IsReceivingGate == false and control.ticksSinceOpened >= 150 and control.irisClosed == false, "Enter only a mature outgoing open-iris gate, never its receiving side.")
        label = params.entryLabel
    else rb.assert(control.IsActive == false, "Close the old connection before dialing.") end
    local ordered = rb.call("rimworld/execute_context_menu_option", { label = label })
    rb.assert(ordered.success and ordered.result.success, "Order the real dial or entry job.")
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Run ordinary travel ticks.")
    if params.phase == "dial" then
        local connected = rb.poll("rimgate/nested_corpse_report", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, corpseId = f.corpseId, subjectPawnId = f.subjectPawnId, markerId = f.markerId, originalCartId = f.cartId }, { timeoutMs = 30000, pollIntervalMs = 50, condition = { path = "result.sourceActive", equals = true } })
        rb.assert(connected.success and connected.result.success, "The actual connection must open.")
    else
        local arrived = rb.poll("rimgate/nested_corpse_report", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, corpseId = f.corpseId, subjectPawnId = f.subjectPawnId, markerId = f.markerId, originalCartId = f.cartId }, { timeoutMs = 30000, pollIntervalMs = 50, condition = { all = { { path = "result.pusher.locations", countEquals = 1 }, { path = "result.pusher.locations", allItems = { all = { { path = "mapId", equals = expectedMap }, { path = "spawned", equals = true } } } } } } })
        rb.assert(arrived.success and arrived.result.success, "The actual operator must arrive home.")
    end
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success, "Pause after actual travel.")
elseif params.phase == "unload" or params.phase == "idle-close" then
    if params.phase == "unload" then rb.assert(before.result.sourceActive == false, "Close the connection normally before observing map removal.") end
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Run ordinary native map lifetime ticks.")
    local unloaded = rb.poll("rimgate/nested_corpse_report", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, corpseId = f.corpseId, subjectPawnId = f.subjectPawnId, markerId = f.markerId, originalCartId = f.cartId }, { timeoutMs = 20000, pollIntervalMs = 50, condition = { all = { { path = "result.sourceActive", equals = false }, { path = "result.destinationHasMap", equals = false } } } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success and unloaded.success and unloaded.result.success, "The receiving map must genuinely unload.")
elseif params.phase == "checkpoint" then
    local saved = rb.call("rimworld/save_game", { saveName = params.saveName })
    rb.assert(saved.success and saved.result.success and saved.result.exists, "Save actual site ownership.")
    local loaded = rb.call("rimworld/load_game_ready", { saveName = params.saveName, readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.success and loaded.result.success, "Reload actual site ownership.")
elseif params.phase == "settle" then
    local ticks = rb.call("rimworld/step_game_ticks", { ticks = 200, timeoutMs = 30000 })
    rb.assert(ticks.success and ticks.result.success, "Observe real mature opening ticks.")
else rb.assert(params.phase == "status" or params.phase == "assert-intact", "Choose a documented phase.") end
local after = rb.call("rimgate/nested_corpse_report", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, corpseId = f.corpseId, subjectPawnId = f.subjectPawnId, markerId = f.markerId, originalCartId = f.cartId })
rb.assert(after.success and after.result.success, "Read the actual resulting ownership.")
if params.phase == "assert-intact" then
    rb.assert(after.result.corpse.ownershipOccurrenceCount == 1 and after.result.subject.ownershipOccurrenceCount == 1 and after.result.marker.ownershipOccurrenceCount == 1, "Retain exactly one deep corpse, pawn and inventory marker.")
    rb.assert(after.result.graph.exactInnerPawn and after.result.graph.pawnParentIsCorpse and after.result.graph.exactMarkerInInventory and after.result.graph.markerParentIsInventory and after.result.graph.dead, "Preserve the exact corpse/pawn/inventory graph.")
    local trackedCart = nil
    for i, cart in ipairs(after.result.carts) do
        for j, id in ipairs(cart.heldIds) do
            if id == f.corpseId then trackedCart = cart end
        end
    end
    rb.assert(trackedCart ~= nil and trackedCart.heldCount == 2 and after.result.proxies[1] == nil, "Retain original container structure without flattening or residual proxies.")
    local siblingHeld = false
    local corpseHeld = false
    for i, id in ipairs(trackedCart.heldIds) do
        if id == f.cargoId then siblingHeld = true end
        if id == f.corpseId then corpseHeld = true end
    end
    rb.assert(siblingHeld and corpseHeld, "Keep the original ordinary sibling and corpse directly inside their cart.")
    if params.expectedCartId then rb.assert(trackedCart.id == params.expectedCartId, "Preserve the actual arrived cart through site storage and reopening.") end
end
return { success = true, before = before.result, after = after.result, limits = "Real manifest/jobs, ordinary ticking and native save/load only. Value records do not establish deep ownership; host must verify quantities, exact identities, health and scavenging separately. Host must pause after any failure." }

