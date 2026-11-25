-- Native partial-stack, corpse and downed-pawn BringToGate acceptance.
-- prepare takes mode partial|corpse|downed and transitFixture; retain its fixture.
-- carry-reload returns the ACTUAL carriedCargoId (new after a native split).
-- Pass that ID with fixture to arrive or interrupt, each branching from its
-- mode-specific carried checkpoint. Ordinary draft/drop may remerge a partial
-- split into its original remainder; conservation is checked across ONLY those IDs.
-- No job-count/stat overrides, direct carry, gate admission or outcome setters.
-- Compile with the installed SDK. Bound invocations to 60 seconds; the host MUST
-- set_time_speed Paused in finally after every failure/timeout.
local config = rb.call("rimworld/get_mod_configuration_status", {})
rb.assert(config.success and config.result.success and config.result.configPath == params.configPath and config.result.configurationIssueCount == 0, "Use the healthy expected private profile.")
local deployment = false
for i, mod in ipairs(config.result.activeMods) do
    if mod.packageId == "mrdav30.rimgate" then deployment = mod.loadedInSession and mod.rootDir == params.modRoot end
end
rb.assert(deployment, "Expected private deployment is not loaded.")
if params.phase == "prepare" then
    local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-transit-active", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.success and loaded.result.success, "Load the owned mature active connection.")
    local t = params.transitFixture
    local prepared = rb.call("rimgate/prepare_cargo_kind", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = t.sourceGateId, destinationParentId = t.destinationParentId, pawnId = t.pawnId, mode = params.mode })
    rb.assert(prepared.success and prepared.result.success, "Stage only the native-valid cargo kind and guarded fixture inputs.")
    if params.mode == "partial" then rb.assert(prepared.result.initialCount == 99 and prepared.result.nativeCarryLimit > 0 and prepared.result.nativeCarryLimit < 99, "The real native capacity must force a legal split.") end
    return { success = true, fixture = prepared.result }
end
rb.assert(params.phase == "carry-reload" or params.phase == "arrive" or params.phase == "interrupt", "Choose prepare, carry-reload, arrive or interrupt.")
local f = params.fixture
local subjectPawnId = ""
local markerId = ""
if f.mode ~= "partial" then
    subjectPawnId = f.subjectPawnId
    markerId = f.markerId
end
local carriedCargoId = params.carriedCargoId or ""
local saveName = "Rimgate-cargo-kind-" .. f.mode .. "-held"
if params.phase == "carry-reload" then
    local initial = rb.call("rimgate/cargo_kind_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, mode = f.mode, originalCargoId = f.cargoId, subjectPawnId = subjectPawnId, markerId = markerId })
    rb.assert(initial.success and initial.result.success and initial.result.cargo.spawned and initial.result.cargo.mapId == f.sourceMapId and initial.result.cargo.count == f.initialCount and initial.result.hauler.dead == false and initial.result.hauler.downed == false and initial.result.actualCarriedId == nil, "Begin with the exact loose input and healthy empty-handed source operator.")
    rb.assert(initial.result.transit.reciprocal and initial.result.transit.source.IsActive and initial.result.transit.source.irisClosed == false and initial.result.transit.destination.irisClosed == false, "Use the actual open-iris reciprocal link.")
    local viewed = rb.call("rimgate/view_transit_map", { mapId = f.sourceMapId })
    rb.assert(viewed.success and viewed.result.success, "View the source without moving its actors.")
    local selected = rb.call("rimworld/select_pawn", { pawnId = f.pawnId })
    rb.assert(selected.success and selected.result.success, "Select the actual hauler.")
    local menu = rb.call("rimworld/right_click_cell", { x = initial.result.transit.source.x, z = initial.result.transit.source.z })
    rb.assert(menu.success and menu.result.success and menu.result.provider == "ui_event", "Open the real gate cargo menu.")
    local ordered = rb.call("rimworld/execute_context_menu_option", { label = "Bring selected to Dwarfgate" })
    rb.assert(ordered.success and ordered.result.success, "Begin ordinary cargo targeting.")
    local target = rb.call("rimworld/click_cell", { x = f.cargoX, z = f.cargoZ, button = "left" })
    rb.assert(target.success and target.result.success, "Target the actual stack, corpse or downed subject.")
    local carrying = false
    for attempt = 1, 160 do
        if carrying == false then
            local tick = rb.call("rimworld/step_game_ticks", { ticks = 1, timeoutMs = 5000 })
            rb.assert(tick.success and tick.result.success, "Observe real pickup one normal tick at a time.")
            local held = rb.call("rimgate/cargo_kind_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, mode = f.mode, originalCargoId = f.cargoId, subjectPawnId = subjectPawnId, markerId = markerId })
            rb.assert(held.success and held.result.success, "Read the actual native carry identity.")
            if held.result.actualCarriedId ~= nil then
                carriedCargoId = held.result.actualCarriedId
                carrying = held.result.hauler.job == "Rimgate_BringToGate" and held.result.hauler.jobTargetA == carriedCargoId
            end
        end
    end
    rb.assert(carrying, "The real hauling job did not acquire and target its actual carried cargo.")
    local boundary = rb.call("rimgate/cargo_kind_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, mode = f.mode, originalCargoId = f.cargoId, carriedCargoId = carriedCargoId, subjectPawnId = subjectPawnId, markerId = markerId })
    rb.assert(boundary.success and boundary.result.success and boundary.result.transit.item.ownershipOccurrenceCount == 1 and boundary.result.transit.item.distinctObjectCount == 1 and boundary.result.cargo.parentHolder == "Pawn_CarryTracker", "Save only the real singly owned carried object.")
    if f.mode == "partial" then
        rb.assert(carriedCargoId ~= f.cargoId and boundary.result.pairDistinctObjectCount == 2 and boundary.result.pairStackCount == 99 and boundary.result.cargo.count == f.nativeCarryLimit and boundary.result.remainder.count == 99 - f.nativeCarryLimit and boundary.result.remainder.spawned and boundary.result.remainder.mapId == f.sourceMapId, "The real native split must create one carried ID and retain its exact source remainder.")
    else
        rb.assert(carriedCargoId == f.cargoId and boundary.result.subject.inventoryMarkerPresent, "The whole subject/corpse must retain its original identity and nested marker.")
    end
    local saved = rb.call("rimworld/save_game", { saveName = saveName })
    rb.assert(saved.success and saved.result.success and saved.result.exists, "Save the actual carried cargo checkpoint.")
end
rb.assert(carriedCargoId ~= "", "Pass the actual carriedCargoId returned by carry-reload.")
local loaded = rb.call("rimworld/load_game_ready", { saveName = saveName, readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
rb.assert(loaded.success and loaded.result.success, "Reload the matching mode's actual carried checkpoint.")
local restored = rb.call("rimgate/cargo_kind_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, mode = f.mode, originalCargoId = f.cargoId, carriedCargoId = carriedCargoId, subjectPawnId = subjectPawnId, markerId = markerId })
rb.assert(restored.success and restored.result.success and restored.result.actualCarriedId == carriedCargoId and restored.result.hauler.job == "Rimgate_BringToGate" and restored.result.hauler.jobTargetA == carriedCargoId and restored.result.cargo.parentHolder == "Pawn_CarryTracker", "Reload must retain the exact carried identity, real job and target.")
if params.phase == "arrive" then
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Resume ordinary hauling and gate ticks.")
    local arrived = rb.poll("rimgate/cargo_kind_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, mode = f.mode, originalCargoId = f.cargoId, carriedCargoId = carriedCargoId, subjectPawnId = subjectPawnId, markerId = markerId }, { timeoutMs = 30000, pollIntervalMs = 50, condition = { all = { { path = "result.cargo.spawned", equals = true }, { path = "result.cargo.mapId", equals = f.destinationMapId }, { path = "result.transit.item.ownershipOccurrenceCount", equals = 1 }, { path = "result.transit.item.referenceBufferCount", equals = 0 } } } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success and arrived.success and arrived.result.success, "Pause after actual destination arrival.")
elseif params.phase == "interrupt" then
    local drafted = rb.call("rimworld/set_draft", { pawnId = f.pawnId, drafted = true })
    rb.assert(drafted.success and drafted.result.success, "Interrupt the actual hauling job by normal drafting.")
    local tick = rb.call("rimworld/step_game_ticks", { ticks = 1, timeoutMs = 5000 })
    rb.assert(tick.success and tick.result.success, "Let ordinary interruption cleanup run.")
end
local state = rb.call("rimgate/cargo_kind_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, mode = f.mode, originalCargoId = f.cargoId, carriedCargoId = carriedCargoId, subjectPawnId = subjectPawnId, markerId = markerId })
rb.assert(state.success and state.result.success and state.result.hauler.dead == false and state.result.hauler.downed == false and state.result.hauler.spawned and state.result.hauler.mapId == f.sourceMapId, "Keep the real healthy hauler on the source map.")
local remerged = false
if f.mode == "partial" then
    rb.assert(state.result.pairStackCount == 99 and state.result.remainder.id == f.cargoId and state.result.remainder.spawned and state.result.remainder.mapId == f.sourceMapId and state.result.remainderTransit.item.ownershipOccurrenceCount == 1 and state.result.remainderTransit.item.distinctObjectCount == 1 and state.result.remainderTransit.item.referenceBufferCount == 0, "Conserve all 99 units across only the actual split and original-ID source remainder; native compression quantity or identity loss must fail.")
    if params.phase == "interrupt" and state.result.cargo == nil then
        remerged = state.result.pairDistinctObjectCount == 1 and state.result.remainder.count == 99 and state.result.transit.item.distinctObjectCount == 0 and state.result.transit.item.ownershipOccurrenceCount == 0
        rb.assert(remerged, "A missing split is valid only when the original remainder now owns all 99 units.")
    else
        rb.assert(state.result.pairDistinctObjectCount == 2 and state.result.cargo.id == carriedCargoId and state.result.cargo.count == f.nativeCarryLimit and state.result.remainder.count == 99 - f.nativeCarryLimit, "Preserve exact split/remainder identities and quantities across carry/reload/arrival.")
    end
else
    rb.assert(state.result.cargo.id == f.cargoId and state.result.cargo.count == 1 and state.result.subject.id == subjectPawnId and state.result.subject.inventoryMarkerPresent and state.result.marker.id == markerId and state.result.marker.count == 1 and state.result.subjectTransit.pawn.distinctObjectCount == 1 and state.result.subjectTransit.pawn.ownershipOccurrenceCount == 1 and state.result.subjectTransit.item.distinctObjectCount == 1 and state.result.subjectTransit.item.ownershipOccurrenceCount == 1, "Retain the exact subject and its singly owned inventory marker through nested holders.")
    if f.mode == "corpse" then
        rb.assert(state.result.subject.dead and state.result.cargo.innerPawnId == subjectPawnId and state.result.subject.corpseId == f.cargoId, "The exact corpse must still deeply contain its original dead fixture pawn.")
    else
        rb.assert(state.result.subject.dead == false and state.result.subject.downed and state.result.subject.anestheticIds[1] == f.anestheticId, "The original fixture subject must remain alive, natively downed and carrying the same anesthetic condition.")
    end
end
if remerged == false then
    rb.assert(state.result.cargo.destroyed == false and state.result.transit.item.distinctObjectCount == 1 and state.result.transit.item.ownershipOccurrenceCount == 1, "The actual carried cargo must remain singly owned, not replaced or duplicated.")
end
if params.phase == "carry-reload" then
    rb.assert(state.result.actualCarriedId == carriedCargoId and state.result.transit.item.referenceBufferCount == 0, "The checkpoint must still be owned by the carry tracker, not a gate alias.")
elseif params.phase == "arrive" then
    rb.assert(state.result.cargo.spawned and state.result.cargo.mapId == f.destinationMapId and state.result.actualCarriedId == nil and state.result.transit.item.referenceBufferCount == 0, "Only normal gate admission may deliver this exact cargo to the receiving map.")
else
    rb.assert(state.result.actualCarriedId == nil and state.result.hauler.job ~= "Rimgate_BringToGate" and state.result.transit.item.referenceBufferCount == 0, "Normal interruption must stop the hauling job and release its carry owner.")
    if remerged == false then rb.assert(state.result.cargo.spawned and state.result.cargo.mapId == f.sourceMapId, "Interrupted cargo must remain spawned on the source map.") end
end
rb.assert(state.result.transit.source.pendingOwnedCount == 0 and state.result.transit.source.sendCount == 0 and state.result.transit.source.receiveCount == 0 and state.result.transit.destination.pendingOwnedCount == 0 and state.result.transit.destination.sendCount == 0 and state.result.transit.destination.receiveCount == 0, "Every final checkpoint must leave gate pending owners and queue aliases empty.")
return { success = true, fixture = f, carriedCargoId = carriedCargoId, remergedOnInterrupt = remerged, status = state.result, limits = "Native split/carry, loaded corpse/downed subject and nested inventory acceptance only. Strict two-ID conservation excludes unrelated map stacks; source-cell diagnostics cannot substitute a compressed replacement. Native draft/drop may absorb a split back into its original remainder. No transporter, unloaded-site, combat or historical broken-save repair claim." }
