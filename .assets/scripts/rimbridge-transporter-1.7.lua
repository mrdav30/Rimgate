-- Native transporter regression: prepare, load_dialog, accept, carry, checkpoint,
-- first-arrive, then arrive OR second-carry followed by close.
-- second-carry observes the existing job; it does not order hauling or carry.
-- prepare takes transitFixture;
-- later phases take prepare's fixture. Parent sets both unique gun quantities to1
-- in the real Items tab and passes fresh acceptTargetId. No DEV instant loading.
-- Compile with the installed SDK. Limit each invocation to 60 seconds; host MUST
-- pause in finally on failure/timeout. Polls observe outcomes, not exact tick counts.
-- A close can unload an unoccupied destination into value records; that branch
-- proves source cancellation, not original first-cargo identity after map unloading.
-- Native Cancel load is absent at the tested carried/partial boundary because
-- the streaming transporter container is empty; this scenario uses real DHD closure.
local config = rb.call("rimworld/get_mod_configuration_status", {})
rb.assert(config.success and config.result.success and config.result.configPath == params.configPath and config.result.configurationIssueCount == 0, "Use the healthy expected private profile.")
local deployment = false
for i, mod in ipairs(config.result.activeMods) do
    if mod.packageId == "mrdav30.rimgate" then deployment = mod.loadedInSession and mod.rootDir == params.modRoot end
end
rb.assert(deployment, "Expected private deployment is not loaded.")
if params.phase == "prepare" then
    local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-transit-active", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.success and loaded.result.success, "Reload the owned mature transit checkpoint.")
    local f = params.transitFixture
    local prepared = rb.call("rimgate/prepare_transporter_fixture", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, closerId = f.closerId })
    rb.assert(prepared.success and prepared.result.success, "Stage native-loading inputs only.")
    return { success = true, fixture = prepared.result }
end
local f = params.fixture
local state = rb.call("rimgate/transporter_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, firstId = f.firstId, secondId = f.secondId })
rb.assert(state.success and state.result.success and state.result.loader.dead == false and state.result.loader.downed == false, "Keep the actual healthy source loader.")
if params.phase == "load_dialog" then
    rb.assert(state.result.gateActive and state.result.loading == false and state.result.transporterCount == 0, "Begin with the active empty unassigned native transporter.")
    local selected = rb.call("rimworld/click_cell", { x = 110, z = 130, button = "left" })
    rb.assert(selected.success and selected.result.success, "Select the actual gate.")
    local commands = rb.call("rimworld/list_selected_gizmos", {})
    rb.assert(commands.success and commands.result.success, "Read native transporter commands.")
    local opened = false
    for i, command in ipairs(commands.result.gizmos) do
        if command.label == "Load" then
            rb.assert(command.disabled == false, "The native Load command is disabled.")
            local clicked = rb.call("rimworld/execute_gizmo", { gizmoId = command.id })
            rb.assert(clicked.success and clicked.result.success, "Open the real native manifest.")
            opened = true
        end
    end
    rb.assert(opened, "The actual gate Load command was absent.")
    local layout = rb.call("rimworld/get_ui_layout", {})
    rb.assert(layout.success and layout.result.success, "Read the real Items-tab manifest controls.")
    return { success = true, layout = layout.result, instruction = "Use normal UI to select Items, assign one autopistol and one bolt-action rifle, then pass the fresh Accept target to accept." }
elseif params.phase == "accept" then
    rb.assert(params.acceptTargetId, "Supply the fresh real manifest Accept target.")
    local accepted = rb.call("rimworld/click_ui_target", { targetId = params.acceptTargetId })
    rb.assert(accepted.success and accepted.result.success, "Accept the native two-gun manifest.")
elseif params.phase == "carry" then
    rb.assert(state.result.loading and state.result.remaining == 2, "Start with both actual manifest entries outstanding.")
    local selected = rb.call("rimworld/select_pawn", { pawnId = f.pawnId })
    rb.assert(selected.success and selected.result.success, "Select the actual native loader.")
    local menu = rb.call("rimworld/right_click_cell", { x = 110, z = 130 })
    rb.assert(menu.success and menu.result.success and menu.result.provider == "ui_event", "Open the real gate loading work menu.")
    local ordered = rb.call("rimworld/execute_context_menu_option", { label = params.loadLabel or "Prioritize loading Dwarfgate" })
    rb.assert(ordered.success and ordered.result.success, "Order the real native hauling job.")
    local carrying = false
    for attempt = 1, 600 do
        if carrying == false then
            local tick = rb.call("rimworld/step_game_ticks", { ticks = 1, timeoutMs = 5000 })
            rb.assert(tick.success and tick.result.success, "Advance native pickup by exactly one tick.")
            state = rb.call("rimgate/transporter_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, firstId = f.firstId, secondId = f.secondId })
            rb.assert(state.success and state.result.success, "Observe the actual carry boundary.")
            carrying = state.result.firstCarried
        end
    end
    rb.assert(carrying and state.result.loader.job == "HaulToTransporter" and state.result.loader.targetB == f.sourceGateId and state.result.remaining == 2, "Observe a real HaulToTransporter carrying the first gun before native admission.")
    return { success = true, status = state.result, instruction = "checkpoint now, then first-arrive." }
elseif params.phase == "second-carry" then
    rb.assert(state.result.firstArrived and state.result.remaining == 1 and state.result.loading and state.result.secondArrived == false, "Begin with the actual first arrival and outstanding second manifest entry.")
    local carrying = state.result.secondCarried
    for attempt = 1, 600 do
        if carrying == false then
            local tick = rb.call("rimworld/step_game_ticks", { ticks = 1, timeoutMs = 5000 })
            rb.assert(tick.success and tick.result.success, "Advance the existing second haul by exactly one tick.")
            state = rb.call("rimgate/transporter_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, firstId = f.firstId, secondId = f.secondId })
            rb.assert(state.success and state.result.success, "Observe the actual second carry boundary.")
            carrying = state.result.secondCarried
        end
    end
    rb.assert(carrying and state.result.firstArrived and state.result.remaining == 1 and state.result.loading and state.result.loader.job == "HaulToTransporter" and state.result.loader.targetA == f.secondId and state.result.loader.targetB == f.sourceGateId, "The existing native hauling job must carry the exact second gun before cancellation.")
    rb.assert(state.result.second.item.distinctObjectCount == 1 and state.result.second.item.ownershipOccurrenceCount == 1, "The actual carried second gun must retain one deep owner and identity.")
    return { success = true, status = state.result, instruction = "Run close while this real second haul remains in flight." }
elseif params.phase == "checkpoint" then
    rb.assert(state.result.firstCarried or state.result.firstArrived and state.result.remaining == 1, "Checkpoint the real carried or partially delivered manifest.")
    local saveName = "Rimgate-transporter-partial"
    if state.result.firstCarried then saveName = "Rimgate-transporter-carried" end
    local saved = rb.call("rimworld/save_game", { saveName = saveName })
    rb.assert(saved.success and saved.result.success and saved.result.exists, "Save the actual native loading state.")
    local loaded = rb.call("rimworld/load_game_ready", { saveName = saveName, readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.success and loaded.result.success, "Reload the same native loading checkpoint.")
    local restored = rb.call("rimgate/transporter_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, firstId = f.firstId, secondId = f.secondId })
    rb.assert(restored.success and restored.result.success and restored.result.groupId == state.result.groupId and restored.result.remaining == state.result.remaining and restored.result.loading, "Native group/manifest state changed on reload.")
    rb.assert(restored.result.firstCarried == state.result.firstCarried and restored.result.firstArrived == state.result.firstArrived, "Reload changed the actual first gun boundary.")
    if state.result.firstCarried then rb.assert(restored.result.loader.job == "HaulToTransporter" and restored.result.loader.targetA == f.firstId and restored.result.loader.targetB == f.sourceGateId, "Reload did not resume the exact native hauling targets.") end
    rb.assert(restored.result.first.item.distinctObjectCount == 1 and restored.result.first.item.ownershipOccurrenceCount == 1 and restored.result.second.item.distinctObjectCount == 1 and restored.result.second.item.ownershipOccurrenceCount == 1, "Both exact guns need one deep owner through reload.")
    return { success = true, before = state.result, restored = restored.result, save = saved.result.path }
elseif params.phase == "first-arrive" or params.phase == "arrive" then
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Run ordinary engine loading/transport ticks.")
    local reached = false
    if params.phase == "first-arrive" then
        reached = rb.poll("rimgate/transporter_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, firstId = f.firstId, secondId = f.secondId }, { timeoutMs = 30000, pollIntervalMs = 50, condition = { path = "result.firstArrived", equals = true } })
    else
        reached = rb.poll("rimgate/transporter_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, firstId = f.firstId, secondId = f.secondId }, { timeoutMs = 30000, pollIntervalMs = 50, condition = { all = { { path = "result.firstArrived", equals = true }, { path = "result.secondArrived", equals = true }, { path = "result.loading", equals = false }, { path = "result.transporterCount", equals = 0 } } } })
    end
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success and reached.success and reached.result.success, "Pause after the actual manifest delivery outcome.")
elseif params.phase == "close" then
    rb.assert(state.result.firstArrived and state.result.remaining == 1 and state.result.loading and state.result.secondArrived == false, "Close only the actual partial manifest after first arrival.")
    rb.assert(state.result.secondCarried and state.result.loader.job == "HaulToTransporter" and state.result.loader.targetA == f.secondId and state.result.loader.targetB == f.sourceGateId, "Close during the actual second native carry; run second-carry first.")
    local selected = rb.call("rimworld/click_cell", { x = 110, z = 134, button = "left" })
    rb.assert(selected.success and selected.result.success, "Select the actual DHD control.")
    local commands = rb.call("rimworld/list_selected_gizmos", {})
    rb.assert(commands.success and commands.result.success, "Read actual DHD commands.")
    local requested = false
    for i, command in ipairs(commands.result.gizmos) do
        if command.label == "Close Dwarfgate" then
            rb.assert(command.disabled == false, "The actual DHD close command is disabled.")
            local clicked = rb.call("rimworld/execute_gizmo", { gizmoId = command.id })
            rb.assert(clicked.success and clicked.result.success, "Request ordinary DHD closure.")
            requested = true
        end
    end
    rb.assert(requested, "The actual DHD close command was absent.")
    selected = rb.call("rimworld/select_pawn", { pawnId = f.closerId })
    rb.assert(selected.success and selected.result.success, "Select the separate actual closer.")
    local menu = rb.call("rimworld/right_click_cell", { x = 110, z = 134 })
    rb.assert(menu.success and menu.result.success and menu.result.provider == "ui_event", "Open the real DHD work menu.")
    local ordered = rb.call("rimworld/execute_context_menu_option", { label = "Prioritize closing" })
    rb.assert(ordered.success and ordered.result.success, "Order the real DHD close job.")
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Allow ordinary native hauling cancellation to settle.")
    local settled = rb.poll("rimgate/transporter_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, firstId = f.firstId, secondId = f.secondId }, { timeoutMs = 30000, pollIntervalMs = 50, condition = { all = { { path = "result.gateActive", equals = false }, { path = "result.loading", equals = false }, { path = "result.remaining", equals = 0 }, { path = "result.groupId", equals = -1 }, { path = "result.transporterCount", equals = 0 }, { path = "result.loader.hasCarriedThing", equals = false }, { path = "result.loader.job", notEquals = "HaulToTransporter" }, { path = "result.secondSpawnedOnSource", equals = true }, { path = "result.second.item.distinctObjectCount", equals = 1 }, { path = "result.second.item.ownershipOccurrenceCount", equals = 1 }, { path = "result.second.item.referenceBufferCount", equals = 0 } } } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success and settled.success and settled.result.success, "Pause after actual quiescent source-side cancellation.")
else
    rb.assert(false, "Choose prepare, load_dialog, accept, carry, checkpoint, first-arrive, second-carry, arrive or close.")
end
state = rb.call("rimgate/transporter_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, firstId = f.firstId, secondId = f.secondId })
rb.assert(state.success and state.result.success, "Read a fresh paused native loading state.")
rb.assert(state.result.second.item.distinctObjectCount == 1 and state.result.second.item.ownershipOccurrenceCount == 1, "The exact second gun must retain one object/deep owner.")
for i, location in ipairs(state.result.first.item.locations) do
    rb.assert(location.stackCount == 1 and location.destroyed == false, "The tracked first gun's live quantity must remain one.")
end
for i, location in ipairs(state.result.second.item.locations) do
    rb.assert(location.stackCount == 1 and location.destroyed == false, "The tracked second gun's live quantity must remain one.")
end
if params.phase == "accept" then
    rb.assert(state.result.loading and state.result.groupId >= 0 and state.result.remaining == 2 and state.result.transporterCount == 0, "Native manifest acceptance must assign both loose guns without loading outcomes.")
    local firstListed = false
    local secondListed = false
    for i, row in ipairs(state.result.manifest) do
        for j, thing in ipairs(row.things) do
            if thing.id == f.firstId and row.count == 1 then firstListed = true end
            if thing.id == f.secondId and row.count == 1 then secondListed = true end
        end
    end
    rb.assert(firstListed and secondListed, "Both real manifest entries must refer to the staged unique guns.")
elseif params.phase == "first-arrive" then
    rb.assert(state.result.firstArrived and state.result.secondArrived == false and state.result.remaining == 1 and state.result.loading and state.result.gateLoading, "Observe first delivery while the second manifest entry remains outstanding.")
elseif params.phase == "arrive" then
    rb.assert(state.result.firstArrived and state.result.secondArrived and state.result.remaining == 0 and state.result.loading == false and state.result.groupId == -1, "Complete both real deliveries and native manifest/group cleanup.")
else
    rb.assert(state.result.remaining == 0 and state.result.loading == false and state.result.groupId == -1 and state.result.transporterCount == 0, "DHD closure must clear native group/manifest/container state.")
    rb.assert(state.result.secondArrived == false and state.result.secondSpawnedOnSource and state.result.second.item.locations[1].mapId == f.sourceMapId and state.result.loader.job ~= "HaulToTransporter" and state.result.loader.hasCarriedThing == false and state.result.loader.carriedId == nil, "Cancellation must restore the undelivered gun to the source map and finish carry/loading cleanup.")
    rb.assert(state.result.gateActive == false, "A real DHD close must leave the gate inactive.")
end
if params.phase ~= "close" or state.result.first.destinationHasMap then
    rb.assert(state.result.first.item.distinctObjectCount == 1 and state.result.first.item.ownershipOccurrenceCount == 1, "The exact first gun must retain one object/deep owner while its map is loaded.")
end
rb.assert(state.result.first.source.pendingOwnedCount == 0 and state.result.first.source.sendCount == 0 and state.result.first.source.receiveCount == 0, "Native loading must not strand unqueued gate pending matter.")
if params.phase ~= "accept" and state.result.first.destination ~= nil then
    rb.assert(state.result.first.destination.pendingOwnedCount == 0 and state.result.first.destination.receiveCount == 0 and state.result.first.destination.sendCount == 0, "The receiver must drain actual owners/queue aliases at the sampled boundary.")
end
return { success = true, status = state.result, limits = "Real native two-item manifest and jobs only. Full-stack nonstackable gun identity is tested; partial stack splitting, pawns/corpses and blocked native cancellation need separate acceptance. An unloaded site's value records do not retain original Thing IDs." }
