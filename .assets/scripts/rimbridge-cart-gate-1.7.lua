-- Loaded cart gate checks. prepare takes the existing transit fixture as transitFixture.
-- Later phases take prepare's fixture: load_dialog, accept_load, start, checkpoint,
-- close, close-checkpoint or iris (while still carrying), then reject; use arrive
-- for ordinary handoff. close-checkpoint reloads the actual assigned close job.
-- Set only the unique loose pistol manifest count to 1 via real UI. accept_load takes
-- its fresh acceptTargetId. No DEV loading, direct job starts or gate outcome APIs.
-- Compile with the installed bridge before running; offline parsing is insufficient.
-- Each invocation is bounded to 60 seconds. Host MUST pause in finally on failure.
-- Bulk normal Ultrafast ticks may overshoot; one-tick steps protect critical boundaries.
local config = rb.call("rimworld/get_mod_configuration_status", {})
rb.assert(config.success and config.result.success and config.result.configPath == params.configPath and config.result.configurationIssueCount == 0, "Use the healthy expected private profile.")
local deployment = false
for i, mod in ipairs(config.result.activeMods) do
    if mod.packageId == "mrdav30.rimgate" then deployment = mod.loadedInSession and mod.rootDir == params.modRoot end
end
rb.assert(deployment, "Expected private deployment is not loaded.")
if params.phase == "prepare" then
    local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-transit-active", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.success and loaded.result.success, "Load the owned mature transit checkpoint.")
    local f = params.transitFixture
    local prepared = rb.call("rimgate/prepare_cart_gate_fixture", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, closerId = f.closerId })
    rb.assert(prepared.success and prepared.result.success, "Stage only cart/cargo/operator inputs.")
    return { success = true, fixture = prepared.result }
end
local f = params.fixture
local state = rb.call("rimgate/cart_gate_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, cargoId = f.cargoId })
rb.assert(state.success and state.result.success and state.result.utility.pawn.dead == false and state.result.utility.pawn.downed == false, "Use the living tracked operator.")
if params.phase == "load_dialog" then
    rb.assert(state.result.utility.cartsCount == 1 and state.result.utility.carts[1].spawned and state.result.utility.carts[1].heldCount == 0, "Begin with the empty staged cart.")
    local selected = rb.call("rimworld/click_cell", { x = f.cartX, z = f.cartZ, button = "left" })
    rb.assert(selected.success and selected.result.success, "Select the staged cart.")
    local commands = rb.call("rimworld/list_selected_gizmos", {})
    rb.assert(commands.success and commands.result.success, "Read the real cart commands.")
    local opened = false
    for i, command in ipairs(commands.result.gizmos) do
        if command.label == "Load" then
            local clicked = rb.call("rimworld/execute_gizmo", { gizmoId = command.id })
            rb.assert(clicked.success and clicked.result.success, "Open the normal manifest.")
            opened = true
        end
    end
    rb.assert(opened, "The real Load command was absent.")
    local layout = rb.call("rimworld/get_ui_layout", {})
    rb.assert(layout.success and layout.result.success, "Read actual manifest controls.")
    return { success = true, layout = layout.result, instruction = "Set only the unique loose autopistol count to 1 through core UI; pass the fresh Accept target to accept_load." }
elseif params.phase == "accept_load" then
    rb.assert(params.acceptTargetId, "Supply the fresh Accept target after setting the real manifest.")
    local accepted = rb.call("rimworld/click_ui_target", { targetId = params.acceptTargetId })
    rb.assert(accepted.success and accepted.result.success, "Accept the real manifest.")
    local selected = rb.call("rimworld/select_pawn", { pawnId = f.pawnId })
    rb.assert(selected.success and selected.result.success, "Select the actual hauler.")
    local menu = rb.call("rimworld/right_click_cell", { x = f.cartX, z = f.cartZ })
    rb.assert(menu.success and menu.result.success and menu.result.provider == "ui_event", "Open the real cart work menu.")
    local order = rb.call("rimworld/execute_context_menu_option", { label = params.loadLabel or "Prioritize loading steel wheelbarrow" })
    rb.assert(order.success and order.result.success, "Order the production cart-loading job.")
elseif params.phase == "start" then
    rb.assert(state.result.sourceGateExists and state.result.transit.source.IsActive and state.result.transit.source.irisClosed == false, "Use a mature outgoing open-iris gate.")
    rb.assert(state.result.utility.cartsCount == 1 and state.result.utility.carts[1].heldIds[1] == f.cargoId, "Load the unique cargo through the manifest first.")
    local selected = rb.call("rimworld/select_pawn", { pawnId = f.pawnId })
    rb.assert(selected.success and selected.result.success, "Select the actual pusher.")
    local menu = rb.call("rimworld/right_click_cell", { x = 110, z = 130 })
    rb.assert(menu.success and menu.result.success and menu.result.provider == "ui_event", "Open the real gate menu.")
    local order = rb.call("rimworld/execute_context_menu_option", { label = "Bring selected to Dwarfgate" })
    rb.assert(order.success and order.result.success, "Begin normal gate cargo targeting.")
    local target = rb.call("rimworld/click_cell", { x = f.cartX, z = f.cartZ, button = "left" })
    rb.assert(target.success and target.result.success, "Target the actual loaded cart.")
    local carrying = false
    for attempt = 1, 600 do
        if carrying == false then
            local tick = rb.call("rimworld/step_game_ticks", { ticks = 1, timeoutMs = 5000 })
            rb.assert(tick.success and tick.result.success, "Advance the real cart pickup exactly one tick.")
            state = rb.call("rimgate/cart_gate_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, cargoId = f.cargoId })
            rb.assert(state.success and state.result.success, "Observe the real pickup boundary.")
            carrying = state.result.utility.proxiesCount == 1 and state.result.utility.pawn.carriedId == state.result.utility.proxies[1].id
        end
    end
    rb.assert(carrying and state.result.utility.pawn.job == "Rimgate_EnterGateWithContainer" and state.result.utility.proxies[1].heldIds[1] == f.cargoId and state.result.utility.pawn.attachedCartVisual, "Real gate pushing did not acquire the unique loaded proxy.")
    rb.assert(state.result.sourceGateExists and state.result.transit.source.IsActive and state.result.transit.source.irisClosed == false, "The happy-path gate must still be available at actual cart pickup; apply cancellation afterward.")
    return { success = true, status = state.result, instruction = "checkpoint now; then arrive, or close/iris followed by reject. Reload the carried checkpoint to branch." }
elseif params.phase == "checkpoint" then
    rb.assert(state.result.utility.proxiesCount == 1 and state.result.utility.pawn.carriedId == state.result.utility.proxies[1].id, "Save an actual carried cart proxy.")
    local saved = rb.call("rimworld/save_game", { saveName = "Rimgate-cart-gate-held" })
    rb.assert(saved.success and saved.result.success and saved.result.exists, "Save the carried cart checkpoint.")
    local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-cart-gate-held", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.success and loaded.result.success, "Reload the carried cart checkpoint.")
    local restored = rb.call("rimgate/cart_gate_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, cargoId = f.cargoId })
    rb.assert(restored.success and restored.result.success and restored.result.sourceGateExists, "The gate vanished during the carried reload.")
    rb.assert(restored.result.utility.proxiesCount == 1 and restored.result.utility.proxies[1].id == state.result.utility.proxies[1].id and restored.result.utility.proxies[1].heldIds[1] == f.cargoId, "Reload lost proxy/cargo identity.")
    rb.assert(restored.result.utility.pawn.carriedId == restored.result.utility.proxies[1].id and restored.result.utility.pawn.job == "Rimgate_EnterGateWithContainer" and restored.result.utility.pawn.attachedCartVisual and restored.result.utility.pawn.pushingHediffCount == 1, "Reload lost the real pushing job/state.")
    rb.assert(restored.result.transit.item.ownershipOccurrenceCount == 1 and restored.result.transit.item.distinctObjectCount == 1, "Reload lost single deep cargo ownership.")
    rb.assert(restored.result.transit.pawn.ownershipOccurrenceCount == 1 and restored.result.transit.pawn.distinctObjectCount == 1, "Reload lost single deep operator ownership.")
    return { success = true, before = state.result, restored = restored.result, save = saved.result.path }
elseif params.phase == "close" or params.phase == "close-checkpoint" or params.phase == "iris" then
    rb.assert(state.result.sourceGateExists and state.result.utility.proxiesCount == 1 and state.result.utility.pawn.job == "Rimgate_EnterGateWithContainer", "Apply cancellation during a real carried gate push.")
    local x = 110
    local z = 134
    local commandLabel = "Close Dwarfgate"
    local jobLabel = "Prioritize closing"
    if params.phase == "iris" then
        z = 130
        commandLabel = "Close iris"
        jobLabel = "Prioritize toggling switch"
    end
    local selected = rb.call("rimworld/click_cell", { x = x, z = z, button = "left" })
    rb.assert(selected.success and selected.result.success, "Select the real cancellation control.")
    local commands = rb.call("rimworld/list_selected_gizmos", {})
    rb.assert(commands.success and commands.result.success, "Read actual cancellation commands.")
    local requested = false
    for i, command in ipairs(commands.result.gizmos) do
        if command.label == commandLabel then
            rb.assert(command.disabled == false, "The real cancellation command is disabled.")
            local clicked = rb.call("rimworld/execute_gizmo", { gizmoId = command.id })
            rb.assert(clicked.success and clicked.result.success, "Request normal closure/toggling.")
            requested = true
        end
    end
    rb.assert(requested, "The normal cancellation command was absent.")
    selected = rb.call("rimworld/select_pawn", { pawnId = f.closerId })
    rb.assert(selected.success and selected.result.success, "Select the separate source closer.")
    local menu = rb.call("rimworld/right_click_cell", { x = x, z = z })
    rb.assert(menu.success and menu.result.success and menu.result.provider == "ui_event", "Open the real closer job menu.")
    local order = rb.call("rimworld/execute_context_menu_option", { label = jobLabel })
    rb.assert(order.success and order.result.success, "Start the real closer job.")
    if params.phase == "close-checkpoint" then
        local saved = rb.call("rimworld/save_game", { saveName = "Rimgate-cart-gate-close-request" })
        rb.assert(saved.success and saved.result.success and saved.result.exists, "Save the actual pending close designation and closer job.")
        local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-cart-gate-close-request", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
        rb.assert(loaded.success and loaded.result.success, "Reload the actual pending close request.")
        local resumed = rb.call("rimgate/cart_gate_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, cargoId = f.cargoId })
        rb.assert(resumed.success and resumed.result.success and resumed.result.sourceGateExists, "Retain the original gate across pending-close reload.")
        rb.assert(resumed.result.transit.source.IsActive == false or resumed.result.transit.source.dhd.wantsClosed, "Reload must retain close intent until the native job consumes it.")
    end
    local changed = false
    for attempt = 1, 400 do
        if changed == false then
            local tick = rb.call("rimworld/step_game_ticks", { ticks = 1, timeoutMs = 5000 })
            rb.assert(tick.success and tick.result.success, "Advance the separate closer job one tick.")
            state = rb.call("rimgate/cart_gate_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, cargoId = f.cargoId })
            rb.assert(state.success and state.result.success and state.result.sourceGateExists, "The source gate disappeared before cancellation.")
            if params.phase == "close" or params.phase == "close-checkpoint" then changed = state.result.transit.source.IsActive == false end
            if params.phase == "iris" then changed = state.result.transit.source.irisClosed end
        end
    end
    rb.assert(changed, "The real cancellation control did not finish.")
    if state.result.utility.proxiesCount == 1 then
        rb.assert(state.result.utility.pawn.carriedId == state.result.utility.proxies[1].id, "Cancellation detached the actual carried proxy.")
    else
        -- A path invalidated by iris closure can clean up on the same tick.
        rb.assert(state.result.utility.cartsCount == 1 and state.result.utility.carts[1].mapId == f.sourceMapId
            and state.result.utility.carts[1].heldIds[1] == f.cargoId and state.result.utility.pawn.mapId == f.sourceMapId,
            "Cancellation must retain the loaded cart/operator on the source side.")
    end
    return { success = true, cancellation = params.phase, status = state.result, instruction = "Run reject to verify complete cleanup, including cleanup already performed on the closing tick." }
else
    rb.assert(params.phase == "arrive" or params.phase == "reject", "Choose a documented cart-gate phase.")
    if params.phase == "arrive" then rb.assert(state.result.utility.proxiesCount == 1, "Resume an actual carried cart proxy.") end
    if params.phase == "reject" then
        rb.assert(state.result.utility.proxiesCount == 1 or state.result.utility.cartsCount == 1, "Retain the carried or already-restored cart during rejection.")
    end
    if params.phase == "reject" then rb.assert(state.result.transit.source.IsActive == false or state.result.transit.source.irisClosed, "First apply a real closure or iris job.") end
end
-- Wait for the actual job outcome rather than an arbitrary elapsed tick count.
local expectedMap = f.sourceMapId
if params.phase == "arrive" then expectedMap = state.result.transit.destination.mapId end
local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
rb.assert(speed.success and speed.result.success, "Run ordinary engine ticks without changing boost.")
local completed = false
if params.phase == "accept_load" then
    completed = rb.poll("rimgate/cart_gate_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, cargoId = f.cargoId }, { timeoutMs = 30000, pollIntervalMs = 50, condition = { all = { { path = "result.utility.proxiesCount", equals = 0 }, { path = "result.utility.carts", countEquals = 1 }, { path = "result.utility.carts", allItems = { all = { { path = "spawned", equals = true }, { path = "heldCount", equals = 1 }, { path = "loading", equals = false } } } } } } })
else
    completed = rb.poll("rimgate/cart_gate_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, cargoId = f.cargoId }, { timeoutMs = 30000, pollIntervalMs = 50, condition = { all = { { path = "result.utility.proxiesCount", equals = 0 }, { path = "result.utility.carts", countEquals = 1 }, { path = "result.utility.carts", allItems = { all = { { path = "spawned", equals = true }, { path = "mapId", equals = expectedMap } } } }, { path = "result.utility.pawn.mapId", equals = expectedMap }, { path = "result.utility.pawn.pushingHediffCount", equals = 0 }, { path = "result.utility.pawn.attachedCartVisual", equals = false }, { path = "result.utility.pawn.job", notEquals = "Rimgate_EnterGateWithContainer" }, { path = "result.utility.pawn.job", notEquals = "Rimgate_PushContainer" } } } })
end
local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
rb.assert(paused.success and paused.result.success, "Pause after the bounded completion wait.")
rb.assert(completed.success and completed.result.success, "The real loading, handoff or rejection outcome did not complete within the bounded wait.")
state = rb.call("rimgate/cart_gate_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, cargoId = f.cargoId })
rb.assert(state.success and state.result.success and state.result.sourceGateExists, "The original source gate must survive cart handoff/rejection.")
rb.assert(state.result.transit.item.ownershipOccurrenceCount == 1 and state.result.transit.item.distinctObjectCount == 1, "Cargo must retain exactly one deep owner and object identity.")
rb.assert(state.result.transit.pawn.ownershipOccurrenceCount == 1 and state.result.transit.pawn.distinctObjectCount == 1, "Operator must retain exactly one deep owner and object identity.")
rb.assert(state.result.utility.cartsCount == 1 and state.result.utility.proxiesCount == 0 and state.result.utility.carts[1].spawned and state.result.utility.carts[1].heldCount == 1 and state.result.utility.carts[1].heldIds[1] == f.cargoId, "One actual cart must retain the unique cargo after the real job.")
rb.assert(state.result.utility.carts[1].stuff == "Steel" and state.result.utility.carts[1].hitPoints == f.hitPoints and state.result.utility.carts[1].allowUseContents == false, "Cart conversion changed retained properties.")
rb.assert(state.result.utility.pawn.dead == false and state.result.utility.pawn.downed == false and state.result.utility.pawn.pushingHediffCount == 0 and state.result.utility.pawn.attachedCartVisual == false, "The living operator must leave no pushing slowdown/visual.")
rb.assert(state.result.utility.pawn.job ~= "Rimgate_EnterGateWithContainer" and state.result.utility.pawn.job ~= "Rimgate_PushContainer", "The production cart job did not finish.")
rb.assert(state.result.transit.source.pendingOwnedCount == 0 and state.result.transit.source.sendCount == 0 and state.result.transit.source.receiveCount == 0, "Source gate retained pending matter after the real job.")
if state.result.transit.destination ~= nil then
    rb.assert(state.result.transit.destination.pendingOwnedCount == 0 and state.result.transit.destination.sendCount == 0 and state.result.transit.destination.receiveCount == 0, "Receiver retained pending matter after the real job.")
else
    rb.assert(state.result.transit.destinationHasMap == false, "A loaded destination must retain its receiving gate.")
end
if params.phase == "arrive" then
    rb.assert(state.result.transit.destination ~= nil and state.result.utility.carts[1].mapId == state.result.transit.destination.mapId and state.result.utility.pawn.mapId == state.result.transit.destination.mapId, "Normal handoff must deliver the loaded cart and operator to the receiving map.")
elseif params.phase == "reject" then
    rb.assert(state.result.utility.carts[1].mapId == f.sourceMapId and state.result.utility.pawn.mapId == f.sourceMapId, "Rejected cart/operator must remain on the source map.")
    local dx = state.result.utility.carts[1].cell.x - state.result.utility.pawn.cell.x
    local dz = state.result.utility.carts[1].cell.z - state.result.utility.pawn.cell.z
    rb.assert(dx * dx + dz * dz <= 169, "Rejected cart must be restored near its operator.")
end
return { success = true, status = state.result, limits = "Real manifest, loading, carried reload and gate job only. Sampled ownership does not establish every blocked-placement, fuel or orbital behavior." }
