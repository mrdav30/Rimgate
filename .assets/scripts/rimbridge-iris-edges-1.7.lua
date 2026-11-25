-- Receiver iris jobs with exact same-object destruction/pass-through observation.
-- Phases: prepare, close, send-closed, open, send-open, clear-watch.
-- Pass private configPath/modRoot and the owned transit fixture; prepare returns
-- receiverGateId. It reloads the mature active checkpoint, sends the real operator
-- normally, and installs only ordinary power/iris inputs. Later phases never reload:
-- start each cargo watch AFTER any external reload; it is a non-owning observation,
-- not an identity claim across saves. Never close the gate before the iris kill.
-- Compile with the installed SDK. Bound invocations to 60 seconds; the host MUST
-- set_time_speed Paused in finally after every failure/timeout, then call
-- receiver_iris_report with clearWatch=true even when a later phase fails.
local config = rb.call("rimworld/get_mod_configuration_status", {})
rb.assert(config.success and config.result.success and config.result.configPath == params.configPath and config.result.configurationIssueCount == 0, "Use the healthy expected private profile.")
local deployment = false
for i, mod in ipairs(config.result.activeMods) do
    if mod.packageId == "mrdav30.rimgate" then deployment = mod.loadedInSession and mod.rootDir == params.modRoot end
end
rb.assert(deployment, "Expected private deployment is not loaded.")
local f = params.fixture
if params.phase == "prepare" then
    local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-transit-active", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.success and loaded.result.success, "Load the owned mature active link before starting any watch.")
end
local state = rb.call("rimgate/transit_status", { sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, itemId = f.itemId })
rb.assert(state.success and state.result.success, "Read the actual fixture state.")
local receiverGateId = params.receiverGateId or state.result.destination.id
if params.phase == "clear-watch" then
    local cleared = rb.call("rimgate/receiver_iris_report", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, receiverGateId = receiverGateId, destinationParentId = f.destinationParentId, operatorId = f.pawnId, clearWatch = true })
    rb.assert(cleared.success and cleared.result.success and cleared.result.watchExplicitlyCleared and cleared.result.watchActive == false, "Release the observational cargo reference explicitly.")
    return { success = true, report = cleared.result }
end
rb.assert(state.result.reciprocal and state.result.source.IsActive and state.result.destination.IsActive and state.result.source.ticksSinceOpened >= 150 and state.result.source.irisClosed == false, "Use the mature reciprocal outgoing link with an open source iris.")
rb.assert(receiverGateId == state.result.destination.id, "Resolve the original receiver gate ID.")
if params.phase == "prepare" then
    rb.assert(state.result.destination.irisClosed == false and state.result.pawn.pawn.Spawned and state.result.pawn.pawn.mapId == state.result.source.mapId, "The real operator must begin on the source side of an open receiver.")
    local viewed = rb.call("rimgate/view_transit_map", { mapId = state.result.source.mapId })
    rb.assert(viewed.success and viewed.result.success, "View the actual source map without moving the operator.")
    local selected = rb.call("rimworld/select_pawn", { pawnId = f.pawnId })
    rb.assert(selected.success and selected.result.success, "Select the actual receiver operator.")
    local menu = rb.call("rimworld/right_click_cell", { x = state.result.source.x, z = state.result.source.z })
    rb.assert(menu.success and menu.result.success and menu.result.provider == "ui_event", "Open the real source entry menu.")
    local entered = rb.call("rimworld/execute_context_menu_option", { label = "Enter Dwarfgate" })
    rb.assert(entered.success and entered.result.success, "Order ordinary operator entry.")
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Advance the real entry job.")
    local arrived = rb.poll("rimgate/transit_status", { sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, itemId = f.itemId }, { timeoutMs = 30000, pollIntervalMs = 50, condition = { all = { { path = "result.pawn.pawn.Spawned", equals = true }, { path = "result.pawn.pawn.mapId", equals = state.result.destination.mapId }, { path = "result.pawn.ownershipOccurrenceCount", equals = 1 } } } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success and arrived.success and arrived.result.success, "Pause after the actual operator arrives on the receiver map.")
    local power = rb.call("rimgate/prepare_receiver_power", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, receiverGateId = receiverGateId, destinationParentId = f.destinationParentId })
    rb.assert(power.success and power.result.success, "Stage only the safe ordinary receiver generator input.")
    viewed = rb.call("rimgate/view_transit_map", { mapId = state.result.destination.mapId })
    rb.assert(viewed.success and viewed.result.success, "View the actual receiver map.")
    if state.result.destination.HasIris == false then
        selected = rb.call("rimworld/click_cell", { x = state.result.destination.x, z = state.result.destination.z, button = "left" })
        rb.assert(selected.success and selected.result.success, "Select the receiver for installation input only.")
        local commands = rb.call("rimworld/list_selected_gizmos", {})
        rb.assert(commands.success and commands.result.success, "Read actual receiver installation commands.")
        local installed = false
        for i, command in ipairs(commands.result.gizmos) do
            if command.label == "Add/remove iris" then
                local clicked = rb.call("rimworld/execute_gizmo", { gizmoId = command.id })
                rb.assert(clicked.success and clicked.result.success, "Install the iris as an explicit fixture input.")
                installed = true
            end
        end
        rb.assert(installed, "The developer installation input was absent.")
    end
    speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Allow native power connection/evaluation.")
    local powered = rb.poll("rimgate/receiver_iris_report", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, receiverGateId = receiverGateId, destinationParentId = f.destinationParentId, operatorId = f.pawnId }, { timeoutMs = 30000, pollIntervalMs = 50, condition = { all = { { path = "result.powered", equals = true }, { path = "result.hasIris", equals = true } } } })
    paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success and powered.success and powered.result.success, "Pause after native receiver power becomes available.")
    rb.assert(powered.result.operatorState.spawned and powered.result.operatorState.mapId == state.result.destination.mapId and powered.result.operatorState.dead == false and powered.result.operatorState.downed == false and powered.result.receiverActive, "Keep the real healthy operator on the active receiver map.")
    return { success = true, receiverGateId = receiverGateId, fixture = f, power = power.result, report = powered.result }
elseif params.phase == "close" or params.phase == "open" then
    local expectedClosed = params.phase == "close"
    local before = rb.call("rimgate/receiver_iris_report", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, receiverGateId = receiverGateId, destinationParentId = f.destinationParentId, operatorId = f.pawnId })
    rb.assert(before.success and before.result.success and before.result.hasIris and before.result.powered and before.result.irisClosed ~= expectedClosed and before.result.operatorState.spawned and before.result.operatorState.mapId == state.result.destination.mapId and before.result.operatorState.dead == false and before.result.operatorState.downed == false, "Use the powered receiver and its actual healthy operator before requesting a state change.")
    local viewed = rb.call("rimgate/view_transit_map", { mapId = state.result.destination.mapId })
    rb.assert(viewed.success and viewed.result.success, "View the actual receiver control.")
    local selected = rb.call("rimworld/click_cell", { x = state.result.destination.x, z = state.result.destination.z, button = "left" })
    rb.assert(selected.success and selected.result.success, "Select the actual receiver iris.")
    local commands = rb.call("rimworld/list_selected_gizmos", {})
    rb.assert(commands.success and commands.result.success, "Read real iris commands.")
    local label = "Open iris"
    if expectedClosed then label = "Close iris" end
    local requested = false
    for i, command in ipairs(commands.result.gizmos) do
        if command.label == label then
            rb.assert(command.disabled == false, "The actual iris command is disabled.")
            local clicked = rb.call("rimworld/execute_gizmo", { gizmoId = command.id })
            rb.assert(clicked.success and clicked.result.success, "Request the ordinary iris toggle designation.")
            requested = true
        end
    end
    rb.assert(requested, "The normal iris command was absent.")
    selected = rb.call("rimworld/select_pawn", { pawnId = f.pawnId })
    rb.assert(selected.success and selected.result.success, "Select the real receiver operator.")
    local menu = rb.call("rimworld/right_click_cell", { x = state.result.destination.x, z = state.result.destination.z })
    rb.assert(menu.success and menu.result.success and menu.result.provider == "ui_event", "Open the actual toggle work menu.")
    local ordered = rb.call("rimworld/execute_context_menu_option", { label = "Prioritize toggling switch" })
    rb.assert(ordered.success and ordered.result.success, "Order the normal iris toggle job.")
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Advance the real toggle job.")
    local toggled = rb.poll("rimgate/receiver_iris_report", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, receiverGateId = receiverGateId, destinationParentId = f.destinationParentId, operatorId = f.pawnId }, { timeoutMs = 30000, pollIntervalMs = 50, condition = { path = "result.irisClosed", equals = expectedClosed } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success and toggled.success and toggled.result.success and toggled.result.irisClosed == expectedClosed and toggled.result.receiverActive and toggled.result.powered and toggled.result.operatorState.dead == false and toggled.result.operatorState.downed == false, "The actual powered iris job must reach the requested state while the receiver stays active.")
    return { success = true, report = toggled.result }
end
rb.assert(params.phase == "send-closed" or params.phase == "send-open", "Choose prepare, close, send-closed, open, send-open or clear-watch.")
local expectedClosed = params.phase == "send-closed"
rb.assert(state.result.destination.irisClosed == expectedClosed, "Establish the requested receiver iris state through the real toggle job first.")
local viewed = rb.call("rimgate/view_transit_map", { mapId = state.result.source.mapId })
rb.assert(viewed.success and viewed.result.success, "View the source for ordinary cargo targeting.")
local cargoX = state.result.source.dhd.x + 4
local cargoZ = state.result.source.dhd.z
local cargo = rb.call("rimworld/spawn_thing", { defName = "Gun_Autopistol", stackCount = 1, x = cargoX, z = cargoZ })
rb.assert(cargo.success and cargo.result.success and cargo.result.stackCount == 1, "Stage the unique loose cargo input.")
local cleared = rb.call("rimgate/receiver_iris_report", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, receiverGateId = receiverGateId, destinationParentId = f.destinationParentId, operatorId = f.pawnId, clearWatch = true })
rb.assert(cleared.success and cleared.result.success and cleared.result.watchActive == false, "Clear the previous observational reference before the next case.")
local watched = rb.call("rimgate/receiver_iris_report", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, receiverGateId = receiverGateId, destinationParentId = f.destinationParentId, operatorId = f.pawnId, watchCargoId = cargo.result.thingId })
rb.assert(watched.success and watched.result.success and watched.result.watchActive and watched.result.watchedCargo.id == cargo.result.thingId and watched.result.watchedCargo.destroyed == false and watched.result.watchedCargo.loadedLookupSameReference, "Watch the exact actual loaded cargo before normal hauling; the reference confers no ownership.")
local selected = rb.call("rimworld/select_pawn", { pawnId = f.closerId })
rb.assert(selected.success and selected.result.success, "Select the separate actual source hauler.")
local menu = rb.call("rimworld/right_click_cell", { x = state.result.source.x, z = state.result.source.z })
rb.assert(menu.success and menu.result.success and menu.result.provider == "ui_event", "Open the real source gate cargo menu.")
local ordered = rb.call("rimworld/execute_context_menu_option", { label = "Bring selected to Dwarfgate" })
rb.assert(ordered.success and ordered.result.success, "Begin ordinary gate cargo targeting.")
local target = rb.call("rimworld/click_cell", { x = cargoX, z = cargoZ, button = "left" })
rb.assert(target.success and target.result.success, "Target the actual unique cargo.")
local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
rb.assert(speed.success and speed.result.success, "Advance the real cargo job and normal receive processing.")
local reached = false
if expectedClosed then
    reached = rb.poll("rimgate/receiver_iris_report", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, receiverGateId = receiverGateId, destinationParentId = f.destinationParentId, operatorId = f.pawnId }, { timeoutMs = 30000, pollIntervalMs = 50, condition = { path = "result.watchedCargo.destroyed", equals = true } })
else
    reached = rb.poll("rimgate/transit_status", { sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, itemId = cargo.result.thingId }, { timeoutMs = 30000, pollIntervalMs = 50, condition = { all = { { path = "result.item.ownershipOccurrenceCount", equals = 1 }, { path = "result.item.locations", allItems = { all = { { path = "spawned", equals = true }, { path = "mapId", equals = state.result.destination.mapId } } } } } } })
end
local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
rb.assert(paused.success and paused.result.success and reached.success and reached.result.success, "Pause after the actual receiver iris outcome.")
local report = rb.call("rimgate/receiver_iris_report", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, receiverGateId = receiverGateId, destinationParentId = f.destinationParentId, operatorId = f.pawnId })
rb.assert(report.success and report.result.success and report.result.receiverActive and report.result.powered and report.result.irisClosed == expectedClosed and report.result.operatorState.spawned and report.result.operatorState.dead == false and report.result.operatorState.downed == false, "The real healthy operator and powered active receiver must survive the case.")
rb.assert(report.result.watchedCargo.id == cargo.result.thingId and report.result.watchedCargo.sourceOwnsReference == false and report.result.watchedCargo.receiverOwnsReference == false, "The watched cargo must leave both actual pending owners.")
local final = rb.call("rimgate/transit_status", { sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, itemId = cargo.result.thingId })
rb.assert(final.success and final.result.success and final.result.reciprocal and final.result.source.IsActive and final.result.destination.IsActive and final.result.source.pendingOwnedCount == 0 and final.result.source.sendCount == 0 and final.result.source.receiveCount == 0 and final.result.destination.pendingOwnedCount == 0 and final.result.destination.sendCount == 0 and final.result.destination.receiveCount == 0, "Normal iris processing must drain all pending owners/queues without closing either gate.")
rb.assert(final.result.pawn.distinctObjectCount == 1 and final.result.pawn.ownershipOccurrenceCount == 1 and final.result.pawn.pawn.Spawned and final.result.pawn.pawn.mapId == final.result.destination.mapId, "Preserve the real singly owned receiver operator.")
if expectedClosed then
    rb.assert(report.result.watchedCargo.destroyed and report.result.watchedCargo.count == 0 and report.result.watchedCargo.spawned == false and report.result.watchedCargo.loadedLookupFound == false and final.result.item.distinctObjectCount == 0 and final.result.item.ownershipOccurrenceCount == 0 and final.result.item.referenceBufferCount == 0, "The watched same object must be destroyed by normal closed-iris receive processing, with no loaded owner/queue remaining.")
else
    rb.assert(report.result.watchedCargo.destroyed == false and report.result.watchedCargo.count == 1 and report.result.watchedCargo.spawned and report.result.watchedCargo.loadedLookupSameReference and final.result.item.distinctObjectCount == 1 and final.result.item.ownershipOccurrenceCount == 1 and final.result.item.referenceBufferCount == 0, "Open-iris arrival must preserve the exact watched object, count and single deep owner.")
end
return { success = true, cargoId = cargo.result.thingId, report = report.result, status = final.result, instruction = "Run clear-watch after retaining evidence; this observer is intentionally not save-resilient.", limits = "Actual receiver toggle and source cargo jobs only; generator and iris installation are explicit fixture inputs. Same-object destruction/pass-through is observed within one loaded Game, not inferred from disappearance or claimed across reload." }
