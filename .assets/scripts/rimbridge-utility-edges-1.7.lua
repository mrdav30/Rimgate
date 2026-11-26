-- Private ZPM lifecycle phases: status, prepare, wait, checkpoint.
-- Reuse rimbridge-utilities-1.7.lua's fixture. prepare adds empty facilities only;
-- ordinary ticks must establish their links, power and charge/broadcast behavior.
-- Set ticks (default 500) for bounded ordinary engine progression. The host must
-- pause in finally after any failure; no private Tick/tracker setters are called.
local f = params.fixture
local state = rb.call("rimgate/utilities_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, pawnId = f.pawnId, housingId = f.housingId, cargoId = f.cargoId, zpmIds = f.zpmIds[1] .. "," .. f.zpmIds[2] .. "," .. f.zpmIds[3] })
rb.assert(state.success and state.result.success and state.result.housing.spawned, "Use the private owned utility housing.")
if params.phase == "prepare" then
    local inputs = rb.call("rimgate/prepare_zpm_edges", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, housingId = f.housingId })
    rb.assert(inputs.success and inputs.result.success, "Stage facilities only.")
    return inputs.result
elseif params.phase == "wait" then
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Run ordinary engine ticks.")
    local waited = rb.poll("rimgate/utilities_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, pawnId = f.pawnId, housingId = f.housingId, cargoId = f.cargoId, zpmIds = f.zpmIds[1] .. "," .. f.zpmIds[2] .. "," .. f.zpmIds[3] }, { timeoutMs = 20000, pollIntervalMs = 50, condition = { path = "result.tick", greaterThanOrEqual = state.result.tick + (params.ticks or 500) } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success and waited.success and waited.result.success and waited.result.tick >= state.result.tick + (params.ticks or 500), "Pause after verified bounded progression.")
elseif params.phase == "checkpoint" then
    local saved = rb.call("rimworld/save_game", { saveName = "Rimgate-zpm-edges" })
    rb.assert(saved.success and saved.result.success and saved.result.exists, "Save the active ZPM lifecycle.")
    local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-zpm-edges", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.success and loaded.result.success, "Reload the ZPM lifecycle.")
else
    rb.assert(params.phase == "status", "Unknown utility-edge phase.")
end
local after = rb.call("rimgate/utilities_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, pawnId = f.pawnId, housingId = f.housingId, cargoId = f.cargoId, zpmIds = f.zpmIds[1] .. "," .. f.zpmIds[2] .. "," .. f.zpmIds[3] })
rb.assert(after.success and after.result.success and after.result.housing.count == state.result.housing.count, "Preserve the actual module container.")
return { success = true, before = state.result, after = after.result }
