-- Native People manifest: prepare, load_dialog, accept, boarding, checkpoint,
-- admitted (one-tick capture of real transporter contents), arrive.
-- prepare takes transitFixture and optional mixed=true. Later phases take fixture.
-- Host uses the real People checkbox for only fixture.boarderId and optional
-- Items rifle qty1, then supplies fresh acceptTargetId. No instant loading.
-- Save only while the real EnterTransporter job is walking on the source.
-- Boundary scans allow 200 ticks per invocation to stay inside the SDK statement
-- budget. Inspect a missed boundary before continuing from the paused live state.
-- Compile with installed SDK; bound invocation and host MUST pause in finally.
local config = rb.call("rimworld/get_mod_configuration_status", {})
rb.assert(config.success and config.result.success and config.result.configPath == params.configPath and config.result.configurationIssueCount == 0, "Use the healthy expected private profile.")
local deployment = false
for i, mod in ipairs(config.result.activeMods) do
    if mod.packageId == "mrdav30.rimgate" then deployment = mod.loadedInSession and mod.rootDir == params.modRoot end
end
rb.assert(deployment, "Expected private deployment is not loaded.")
if params.phase == "prepare" then
    local f = params.transitFixture
    local prepared = rb.call("rimgate/prepare_transporter_pawn", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, closerId = f.closerId, mixed = params.mixed or false })
    rb.assert(prepared.success and prepared.result.success and prepared.result.boarderNativeSendable and prepared.result.itemNativeSendable, "Stage genuine native-sendable inputs only.")
    return { success = true, fixture = prepared.result }
end
local f = params.fixture
local itemId = ""
if f.mixed then itemId = f.itemId end
local state = rb.call("rimgate/transporter_pawn_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, boarderId = f.boarderId, markerId = f.markerId, itemId = itemId })
rb.assert(state.success and state.result.success, "Read the actual native manifest/boarder.")
if params.phase == "load_dialog" then
    rb.assert(state.result.loading == false and state.result.transporterCount == 0 and state.result.boarderSpawnedOnSource, "Begin before assigning the real manifest.")
    local selected = rb.call("rimworld/click_cell", { x = 110, z = 130, button = "left" })
    rb.assert(selected.success and selected.result.success, "Select the owned source gate.")
    local commands = rb.call("rimworld/list_selected_gizmos", {})
    rb.assert(commands.success and commands.result.success, "Read actual native commands.")
    local opened = false
    for i, command in ipairs(commands.result.gizmos) do
        if command.label == "Load" then
            rb.assert(command.disabled == false, "Native Load must be enabled.")
            local clicked = rb.call("rimworld/execute_gizmo", { gizmoId = command.id })
            rb.assert(clicked.success and clicked.result.success, "Open the actual native manifest.")
            opened = true
        end
    end
    rb.assert(opened, "Native Load command is absent.")
    local layout = rb.call("rimworld/get_ui_layout", {})
    rb.assert(layout.success and layout.result.success, "Read actual People/Items controls.")
    return { success = true, layout = layout.result, instruction = "Check only the fixture boarder in People; for mixed also select one loose rifle in Items. Pass fresh Accept target to accept." }
elseif params.phase == "accept" then
    rb.assert(params.acceptTargetId, "Supply the fresh native Accept target.")
    local accepted = rb.call("rimworld/click_ui_target", { targetId = params.acceptTargetId })
    rb.assert(accepted.success and accepted.result.success, "Accept the actual native People manifest.")
elseif params.phase == "boarding" or params.phase == "admitted" then
    local found = false
    for attempt = 1, 200 do
        if found == false then
            state = rb.call("rimgate/transporter_pawn_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, boarderId = f.boarderId, markerId = f.markerId, itemId = itemId })
            rb.assert(state.success and state.result.success, "Observe a real native boarding boundary.")
            if params.phase == "boarding" then found = state.result.boardingJob and state.result.boarderSpawnedOnSource end
            if params.phase == "admitted" then found = state.result.transporterHasBoarder end
            if found == false then
                rb.assert(state.result.boarderArrived == false, "The requested native boundary was missed; retain the report and retry from a saved checkpoint.")
                local tick = rb.call("rimworld/step_game_ticks", { ticks = 1, timeoutMs = 5000 })
                rb.assert(tick.success and tick.result.success, "Advance the real native job one tick.")
            end
        end
    end
    rb.assert(found, "The real native boarding boundary did not occur within this 200-tick scan; inspect the paused native job before continuing.")
    if params.phase == "boarding" then
        rb.assert(state.result.remaining >= 1 and state.result.boarderManifestCount == 1, "The walking boarder must still be an outstanding native manifest entry.")
    else
        rb.assert(state.result.boarderSpawnedOnSource == false and state.result.boarderManifestCount == 0, "Native admission must own the pawn and acknowledge its manifest entry.")
    end
elseif params.phase == "checkpoint" then
    rb.assert(state.result.boardingJob and state.result.boarderSpawnedOnSource and state.result.boarderManifestCount == 1, "Save the real native walking boarding job before admission.")
    local saved = rb.call("rimworld/save_game", { saveName = "Rimgate-transporter-pawn-walking" })
    rb.assert(saved.success and saved.result.success and saved.result.exists, "Save native boarding progress.")
    local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-transporter-pawn-walking", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.success and loaded.result.success, "Reload the actual native boarding state.")
elseif params.phase == "arrive" then
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Let actual native boarding/transit jobs continue.")
    local arrived = rb.poll("rimgate/transporter_pawn_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, boarderId = f.boarderId, markerId = f.markerId, itemId = itemId }, { timeoutMs = 30000, pollIntervalMs = 50, condition = { all = { { path = "result.boarderArrived", equals = true }, { path = "result.remaining", equals = 0 }, { path = "result.loading", equals = false }, { path = "result.transporterCount", equals = 0 } } } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success and arrived.success and arrived.result.success, "Pause after actual arrival and native manifest cleanup.")
else
    rb.assert(false, "Choose prepare, load_dialog, accept, boarding, checkpoint, admitted or arrive.")
end
state = rb.call("rimgate/transporter_pawn_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, boarderId = f.boarderId, markerId = f.markerId, itemId = itemId })
rb.assert(state.success and state.result.success and state.result.markerInBoarderInventory and state.result.boarder.dead == false and state.result.boarder.downed == false, "Retain the healthy original pawn and its exact inventory marker.")
rb.assert(state.result.transit.pawn.distinctObjectCount == 1 and state.result.transit.pawn.ownershipOccurrenceCount == 1 and state.result.transit.item.distinctObjectCount == 1 and state.result.transit.item.ownershipOccurrenceCount == 1, "Retain exactly one original pawn and inventory marker/deep owner across all sampled transitions.")
if params.phase == "accept" then
    rb.assert(state.result.loading and state.result.boarderManifestCount == 1 and state.result.boarderSpawnedOnSource, "Accept the real People manifest without inserting the pawn.")
    if f.mixed then
        rb.assert(state.result.itemManifestCount == 1 and state.result.remaining == 2, "The mixed manifest must include precisely the boarder and one loose rifle.")
    else
        rb.assert(state.result.remaining == 1, "The solo manifest must contain only the boarder.")
    end
elseif params.phase == "checkpoint" then
    rb.assert(state.result.boardingJob and state.result.boarderSpawnedOnSource and state.result.boarderManifestCount == 1, "Reload must preserve the walking native boarding job/manifest.")
elseif params.phase == "arrive" then
    rb.assert(state.result.boarderArrived and state.result.groupId == -1 and state.result.remaining == 0 and state.result.transporterCount == 0, "The original pawn must arrive while native manifest/group cleanup completes.")
    if f.mixed then rb.assert(state.result.itemArrived and state.result.cargo.item.distinctObjectCount == 1 and state.result.cargo.item.ownershipOccurrenceCount == 1, "The original mixed rifle must arrive once too.") end
    rb.assert(state.result.transit.source.pendingOwnedCount == 0 and state.result.transit.source.sendCount == 0 and state.result.transit.source.receiveCount == 0, "The source must drain all gate ownership and queue aliases.")
end
return { success = true, status = state.result, limits = "Native People manifest and real EnterTransporter job only; optional mixed loose rifle. No manual-cancel claim unless an actual current native command is observed and used." }
