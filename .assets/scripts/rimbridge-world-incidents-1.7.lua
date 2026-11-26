-- Real incident lifecycle, separate invocations: prepare, trigger, reload, end.
-- prepare takes incidentDef (HeatWave, ToxicFallout, NaquadahMeteorite or Marauders
-- full Rimgate defName), plus orbitalFixture for known smoke gate/pawn IDs and site.
-- Later phases take fixture. end requires a fresh native endConditionPath discovered
-- under Actions\Remove Game Condition...; it runs the real condition End lifecycle.
-- No quest/reward completion, artificial raid pawn, ore or condition output is inserted.
-- Compile in the actual bridge; 60-second invocation budget. Host MUST pause in finally.
local config = rb.call("rimworld/get_mod_configuration_status", {})
rb.assert(config.success and config.result.success and config.result.configPath == params.configPath and config.result.configurationIssueCount == 0, "Use the healthy expected private profile.")
local deployment = false
for i, mod in ipairs(config.result.activeMods) do
    if mod.packageId == "mrdav30.rimgate" then deployment = mod.loadedInSession and mod.rootDir == params.modRoot end
end
rb.assert(deployment, "Expected private deployment is not loaded.")
if params.phase == "prepare" then
    rb.assert(params.incidentDef == "Rimgate_GateHeatWave" or params.incidentDef == "Rimgate_GateToxicFallout" or params.incidentDef == "Rimgate_IncidentNaquadahMeteorite" or params.incidentDef == "Rimgate_Marauders", "Choose a scoped remaining world incident.")
    local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-bridge-smoke", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.success and loaded.result.success, "Load the owned smoke input before the actual incident.")
    local o = params.orbitalFixture
    local status = rb.call("rimgate/world_edge_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = o.sourceGateId, destinationParentId = params.site.id, pawnId = o.pawnId })
    rb.assert(status.success and status.result.success and status.result.source.IsActive == false and status.result.source.ExternalHoldCount == 0, "Start from the actual unheld inactive gate.")
    return { success = true, fixture = { sourceGateId = o.sourceGateId, destinationParentId = params.site.id, pawnId = o.pawnId, incidentDef = params.incidentDef, initial = status.result } }
end
local f = params.fixture
rb.assert(params.phase == "trigger" or params.phase == "reload" or params.phase == "end", "Choose prepare, trigger, reload or end.")
local conditionCase = f.incidentDef == "Rimgate_GateHeatWave" or f.incidentDef == "Rimgate_GateToxicFallout"
if params.phase == "trigger" then
    local available = rb.call("rimworld/list_debug_action_children", { path = "Actions\\Do incident" })
    rb.assert(available.success and available.result.success, "Discover the actual native incident actions.")
    local triggered = rb.call("rimworld/execute_debug_action", { path = "Actions\\Do incident\\" .. f.incidentDef })
    rb.assert(triggered.success and triggered.result.success, "Execute the actual incident worker.")
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Run ordinary incident simulation.")
    local progressed = rb.poll("rimgate/world_edge_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId }, { timeoutMs = 15000, pollIntervalMs = 50, condition = { path = "result.tick", greaterThanOrEqual = f.initial.tick + 600 } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success and progressed.success and progressed.result.success, "Pause after actual incident progress.")
elseif params.phase == "reload" then
    local saved = rb.call("rimworld/save_game", { saveName = "Rimgate-world-incident-" .. f.incidentDef })
    rb.assert(saved.success and saved.result.success and saved.result.exists, "Save actual incident state.")
    local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-world-incident-" .. f.incidentDef, readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.success and loaded.result.success, "Reload actual incident state.")
else
    rb.assert(conditionCase and params.endConditionPath, "Supply the actual native condition-removal action for a held-gate condition.")
    local removed = rb.call("rimworld/execute_debug_action", { path = params.endConditionPath })
    rb.assert(removed.success and removed.result.success, "Run the normal condition End lifecycle.")
    local beforeCleanup = rb.call("rimgate/world_edge_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId })
    local running = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    local cleanup = rb.poll("rimgate/world_edge_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId }, { timeoutMs = 15000, pollIntervalMs = 50, condition = { path = "result.tick", greaterThanOrEqual = beforeCleanup.result.tick + 5 } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(running.result.success and cleanup.success and paused.result.success, "Allow native condition-manager pruning after End, then pause.")
end
local status = rb.call("rimgate/world_edge_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId })
rb.assert(status.success and status.result.success, "Read actual native incident outcomes.")
if conditionCase then
    local count = 0
    for i, condition in ipairs(status.result.conditions) do
        if condition.defName == f.incidentDef then
            count = count + 1
            rb.assert(condition.heldGateId == f.sourceGateId and condition.TicksLeft > 0, "The actual condition must retain its exact gate hold reference/default duration.")
            if f.incidentDef == "Rimgate_GateHeatWave" then rb.assert(condition.temperatureOffset > 0, "The real heat-wave ramp must produce a positive temperature offset.") end
            if f.incidentDef == "Rimgate_GateToxicFallout" then rb.assert(condition.skyLerp > 0, "The real toxic-fallout ramp must produce its sky effect.") end
        end
    end
    if params.phase == "end" then
        rb.assert(count == 0 and status.result.source.IsActive == false and status.result.source.ExternalHoldCount == 0, "Normal End must remove the condition, release the hold and close the local receiver.")
    else
        rb.assert(count == 1 and status.result.source.IsActive and status.result.source.IsReceivingGate and status.result.source.ExternalHoldCount == 1, "A real single incident must own exactly one local receiver hold through reload.")
    end
elseif f.incidentDef == "Rimgate_IncidentNaquadahMeteorite" then
    local initialOre = 0
    local actualOre = 0
    for i, ore in ipairs(f.initial.naquadah) do initialOre = initialOre + 1 end
    for i, ore in ipairs(status.result.naquadah) do actualOre = actualOre + 1 end
    rb.assert(actualOre > initialOre, "The isolated actual meteorite must increase native Naquadah mineables, not just accept an incident request; retain both ID lists for comparison.")
else
    local newRaiders = 0
    for i, pawn in ipairs(status.result.hostiles) do
        local existing = false
        for j, original in ipairs(f.initial.hostiles) do
            if original.id == pawn.id then existing = true end
        end
        if existing == false then
            rb.assert(pawn.lordJob == "Rimgate.LordJob_MaraudColony", "New actual marauders must have their custom native lord job.")
            newRaiders = newRaiders + 1
        end
    end
    rb.assert(newRaiders > 0, "The actual Marauders worker must generate new hostile actors and its lord.")
end
return { success = true, status = status.result, limits = "Actual default-duration incident workers, native ticks, current-save state and normal condition End only. Meteorite mineables are loot generation, not mined-item pickup; raider generation/lord continuation is not combat victory. No reward or quest outcome was inserted." }
