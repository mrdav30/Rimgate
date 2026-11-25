-- Real blocked Steel1 arrival, closure, reload and inactive retry.
-- First use gate-cargo prepare with cargoDef Steel/stackCount 1; then block here,
-- gate-cargo carry-reload, blocked, checkpoint, keeper, close, closed-checkpoint,
-- release, recover. Pass fixture, cargoId, receiverGateId and private paths throughout;
-- block returns blocked, passed as params.blocked to release. No direct admission.
-- The keeper is the actual tracked pawn entering normally behind the blocked FIFO
-- head, then dropping normally on closure; no fixture pawn prevents map unloading.
-- Compile through the installed SDK. Bound each invocation to 60 seconds and the
-- host MUST set_time_speed Paused in finally after failure/timeout.
local config = rb.call("rimworld/get_mod_configuration_status", {})
rb.assert(config.success and config.result.success and config.result.configPath == params.configPath and config.result.configurationIssueCount == 0, "Use the healthy expected private profile.")
local deployment = false
for i, mod in ipairs(config.result.activeMods) do
    if mod.packageId == "mrdav30.rimgate" then deployment = mod.loadedInSession and mod.rootDir == params.modRoot end
end
rb.assert(deployment, "Expected private deployment is not loaded.")
local f = params.fixture
rb.assert(params.cargoId and params.receiverGateId, "Supply the exact original Steel1 and receiving gate IDs.")
if params.phase == "block" then
    local prepared = rb.call("rimgate/prepare_blocked_landing", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, receiverGateId = params.receiverGateId, destinationParentId = f.destinationParentId, cargoId = params.cargoId })
    rb.assert(prepared.success and prepared.result.success and prepared.result.cargoDef == "Steel" and prepared.result.cargoCount == 1, "Prepare ordinary Steel1 landing congestion without touching the original cargo.")
    rb.assert(prepared.result.search.nativeCanPlace == false and prepared.result.search.positiveQualityCount == 0, "The complete native search must prove actual placement is blocked.")
    return { success = true, blocked = prepared.result, instruction = "Run gate-cargo carry-reload with this exact Steel cargo, then blocked here." }
elseif params.phase == "blocked" then
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Resume the real hauling/admission job.")
    local reached = rb.poll("rimgate/blocked_landing_report", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, receiverGateId = params.receiverGateId, destinationParentId = f.destinationParentId, cargoId = params.cargoId }, { timeoutMs = 15000, pollIntervalMs = 50, condition = { path = "result.cargo.receiverOwnsReference", equals = true } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success, "Pause after production receiver admission.")
    rb.assert(reached.success and reached.result.success and reached.result.cargo.receiverOwnsReference, "Normal hauling did not reach the receiver's real deep owner.")
    -- Receive attempts reset their timer and have a maximum normal delay of 80 ticks.
    local retried = rb.call("rimworld/step_game_ticks", { ticks = 81, timeoutMs = 15000 })
    rb.assert(retried.success and retried.result.success and retried.result.advancedTicks == 81, "Observe a production receive retry after admission, not just its queue boundary.")
elseif params.phase == "checkpoint" or params.phase == "closed-checkpoint" then
    local saveName = "Rimgate-gate-blocked"
    if params.phase == "closed-checkpoint" then saveName = "Rimgate-gate-blocked-closed" end
    local saved = rb.call("rimworld/save_game", { saveName = saveName })
    rb.assert(saved.success and saved.result.success and saved.result.exists, "Save actual blocked ownership and queue references.")
    local loaded = rb.call("rimworld/load_game_ready", { saveName = saveName, readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.success and loaded.result.success, "Reload the exact blocked checkpoint.")
elseif params.phase == "keeper" then
    local before = rb.call("rimgate/transit_status", { sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, itemId = params.cargoId })
    rb.assert(before.success and before.result.success and before.result.reciprocal and before.result.source.IsActive and before.result.source.irisClosed == false and before.result.destination.receiveCount == 1 and before.result.destination.receive[1] == params.cargoId, "Begin with the real Steel FIFO head blocking an active receiver.")
    rb.assert(before.result.pawn.pawn.Spawned and before.result.pawn.pawn.mapId == before.result.source.mapId, "The actual tracked pawn must still be on the source map.")
    local selected = rb.call("rimworld/select_pawn", { pawnId = f.pawnId })
    rb.assert(selected.success and selected.result.success, "Select the actual source traveler.")
    local menu = rb.call("rimworld/right_click_cell", { x = 110, z = 130 })
    rb.assert(menu.success and menu.result.success and menu.result.provider == "ui_event", "Open the real source entry menu.")
    local entered = rb.call("rimworld/execute_context_menu_option", { label = "Enter Dwarfgate" })
    rb.assert(entered.success and entered.result.success, "Order the normal pawn entry job.")
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Advance the real entry job.")
    local queued = rb.poll("rimgate/transit_status", { sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, itemId = params.cargoId }, { timeoutMs = 15000, pollIntervalMs = 50, condition = { path = "result.destination.receiveCount", equals = 2 } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success, "Pause at the real two-object receive queue.")
    rb.assert(queued.success and queued.result.success and queued.result.destination.receiveCount == 2, "The real traveler did not queue behind the Steel head.")
elseif params.phase == "close" then
    local selected = rb.call("rimworld/click_cell", { x = 110, z = 134, button = "left" })
    rb.assert(selected.success and selected.result.success, "Select the actual source controller.")
    local commands = rb.call("rimworld/list_selected_gizmos", {})
    rb.assert(commands.success and commands.result.success, "Read actual DHD commands.")
    local requested = false
    for i, command in ipairs(commands.result.gizmos) do
        if command.label == "Close Dwarfgate" then
            rb.assert(command.disabled == false, "Normal DHD closure is disabled.")
            local clicked = rb.call("rimworld/execute_gizmo", { gizmoId = command.id })
            rb.assert(clicked.success and clicked.result.success, "Request ordinary DHD closure.")
            requested = true
        end
    end
    rb.assert(requested, "The real close command was absent.")
    selected = rb.call("rimworld/select_pawn", { pawnId = f.closerId })
    rb.assert(selected.success and selected.result.success, "Select the separate actual source closer.")
    local menu = rb.call("rimworld/right_click_cell", { x = 110, z = 134 })
    rb.assert(menu.success and menu.result.success and menu.result.provider == "ui_event", "Open the real controller work menu.")
    local order = rb.call("rimworld/execute_context_menu_option", { label = "Prioritize closing" })
    rb.assert(order.success and order.result.success, "Order the normal DHD close job.")
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Advance the actual closer job.")
    local closed = rb.poll("rimgate/blocked_landing_report", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, receiverGateId = params.receiverGateId, destinationParentId = f.destinationParentId, cargoId = params.cargoId }, { timeoutMs = 15000, pollIntervalMs = 50, condition = { path = "result.sourceActive", equals = false } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success, "Pause after normal mutual closure.")
    rb.assert(closed.success and closed.result.success and closed.result.sourceActive == false, "The real closer did not close the source.")
elseif params.phase == "release" then
    local b = params.blocked
    rb.assert(b and b.cargoId == params.cargoId and b.receiverGateId == params.receiverGateId and b.destinationParentId == f.destinationParentId and b.sourceGateId == f.sourceGateId, "Use the matching original blocked fixture.")
    local before = rb.call("rimgate/blocked_landing_report", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, receiverGateId = params.receiverGateId, destinationParentId = f.destinationParentId, cargoId = params.cargoId })
    rb.assert(before.success and before.result.success and before.result.sourceActive == false and before.result.receiverActive == false and before.result.cargo.receiverOwnsReference and before.result.placement.nativeCanPlace == false, "Release only an input of the still-blocked inactive receiver.")
    local released = rb.call("rimgate/release_blocked_landing", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, receiverGateId = params.receiverGateId, destinationParentId = f.destinationParentId, cargoId = params.cargoId, blockerIds = b.blockerIds, releaseBlockerId = b.releaseBlockerId })
    rb.assert(released.success and released.result.success and released.result.removedId == b.releaseBlockerId and released.result.receiverActive == false, "Remove only the exact tracked input while the receiver is inactive.")
    rb.assert(released.result.search.nativeCanPlace and released.result.search.positiveQualityCount == 1, "Releasing one input must expose exactly one valid native candidate.")
    return { success = true, release = released.result, instruction = "Run recover; ordinary inactive gate ticks must perform placement." }
elseif params.phase == "recover" then
    local before = rb.call("rimgate/blocked_landing_report", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, receiverGateId = params.receiverGateId, destinationParentId = f.destinationParentId, cargoId = params.cargoId })
    rb.assert(before.success and before.result.success and before.result.sourceActive == false and before.result.receiverActive == false and before.result.cargo.receiverOwnsReference and before.result.placement.nativeCanPlace, "Begin with the same pending cargo, inactive gates and released landing.")
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Allow ordinary inactive hash retries.")
    local recovered = rb.poll("rimgate/blocked_landing_report", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, receiverGateId = params.receiverGateId, destinationParentId = f.destinationParentId, cargoId = params.cargoId }, { timeoutMs = 15000, pollIntervalMs = 50, condition = { path = "result.cargo.spawned", equals = true } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success, "Pause after production inactive retry.")
    rb.assert(recovered.success and recovered.result.success and recovered.result.cargo.spawned, "The normal inactive retry did not restore the exact cargo.")
else
    rb.assert(false, "Choose block, blocked, checkpoint, keeper, close, closed-checkpoint, release or recover.")
end
-- Fresh paused samples distinguish actual ownership from send/receive aliases.
local report = rb.call("rimgate/blocked_landing_report", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, receiverGateId = params.receiverGateId, destinationParentId = f.destinationParentId, cargoId = params.cargoId })
rb.assert(report.success and report.result.success and report.result.destinationHasMap and report.result.receiverFoundByOriginalId, "The actual receiver map and original gate must remain available.")
rb.assert(report.result.cargo.id == params.cargoId and report.result.cargo.defName == "Steel" and report.result.cargo.count == 1 and report.result.cargo.destroyed == false, "Preserve the exact original Steel1, not a merged or replacement pile.")
local state = rb.call("rimgate/transit_status", { sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, itemId = params.cargoId })
rb.assert(state.success and state.result.success and state.result.destination.id == params.receiverGateId, "Resolve the original receiving gate after reload.")
rb.assert(state.result.item.distinctObjectCount == 1 and state.result.item.ownershipOccurrenceCount == 1, "Steel must have exactly one real object and one deep owner.")
rb.assert(state.result.pawn.distinctObjectCount == 1 and state.result.pawn.ownershipOccurrenceCount == 1 and state.result.pawn.pawn.Dead == false and state.result.pawn.pawn.Downed == false, "Keep the real tracked traveler alive and singly owned.")
rb.assert(state.result.source.pendingOwnedCount == 0 and state.result.source.sendCount == 0 and state.result.source.receiveCount == 0, "The source must not retain transported ownership or queue aliases.")
if params.phase == "recover" then
    rb.assert(report.result.sourceActive == false and report.result.receiverActive == false and report.result.cargo.spawned and report.result.cargo.mapId == state.result.destination.mapId and report.result.cargo.receiverOwnsReference == false, "Recovery must place the exact cargo on the receiver map without reopening.")
    rb.assert(state.result.destination.pendingOwnedCount == 0 and state.result.destination.sendCount == 0 and state.result.destination.receiveCount == 0 and state.result.item.referenceBufferCount == 0, "Recovery must drain both the deep owner and all receiver queue aliases.")
else
    rb.assert(report.result.cargo.spawned == false and report.result.cargo.receiverOwnsReference and report.result.placement.nativeCanPlace == false and report.result.placement.positiveQualityCount == 0, "The same Steel must remain genuinely blocked in the receiver's actual owner.")
    rb.assert(state.result.destination.receive[1] == params.cargoId and state.result.destination.sendCount == 0 and state.result.item.referenceBufferCount == 1, "The exact Steel must remain the single receive-head alias.")
    if params.phase == "keeper" then
        rb.assert(state.result.destination.pendingOwnedCount == 2 and state.result.destination.receiveCount == 2 and state.result.destination.receive[2] == f.pawnId and state.result.pawn.pawn.Spawned == false and state.result.pawn.referenceBufferCount == 1, "The normal traveler must be deeply owned and queued behind the blocked Steel.")
    else
        rb.assert(state.result.destination.pendingOwnedCount == 1 and state.result.destination.receiveCount == 1, "Only the original Steel may remain pending at this checkpoint.")
    end
end
if params.phase == "close" or params.phase == "closed-checkpoint" or params.phase == "recover" then
    rb.assert(state.result.source.IsActive == false and state.result.destination.IsActive == false and state.result.source.IsReceivingGate == false and state.result.destination.IsReceivingGate == false and state.result.reciprocal == false and state.result.source.connectedGateId == nil and state.result.destination.connectedGateId == nil, "Normal closure must clear both active/receiving states and reciprocal references.")
    rb.assert(state.result.pawn.pawn.Spawned and state.result.pawn.pawn.mapId == state.result.destination.mapId and state.result.pawn.referenceBufferCount == 0 and report.result.anyPawnBlockingMapRemoval, "The actual normally transferred pawn must keep the receiver map alive after normal close/drain.")
end
return { success = true, report = report.result, status = state.result, limits = "Steel-specific native no-merge blockage, real hauling/entry/DHD jobs and loaded-map exact-ID recovery only. An unloaded transit site stores definition/count records without original IDs; this scenario makes no claim about identity across that separate value-record path." }
