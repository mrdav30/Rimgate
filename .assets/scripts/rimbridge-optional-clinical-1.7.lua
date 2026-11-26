-- Alpha Genes: bounded real sarcophagus destruction/downing consumer.
-- inputs takes patientId/shooterId/sarcophagusId from a fresh medical fixture.
-- Later phases take returned fixture: equip-menu/equip(equipLabel),
-- admit-menu/admit(admitLabel), combat-menu/combat(attackGizmoId), status.
-- Stage/equip and move shooter normally to a clear nearby firing cell BEFORE
-- admission. Combat requires the exact equipped input gun and real verb-target
-- gizmo, not Fire at will. No treatment/HP/downing/RNG outcome is inserted.
-- Compile with installed SDK. Host MUST pause and End the Alpha watch in finally
-- on any failed/timeout invocation, verifying actual observer removal.
local config = rb.call("rimworld/get_mod_configuration_status", {})
rb.assert(config.success and config.result.success and config.result.configPath == params.configPath and config.result.configurationIssueCount == 0, "Use the healthy expected private profile.")
local deployment = false
for i, mod in ipairs(config.result.activeMods) do
    if mod.packageId == "mrdav30.rimgate" then deployment = mod.loadedInSession and mod.rootDir == params.modRoot end
end
rb.assert(deployment, "The expected private development package must be loaded.")
if params.phase == "inputs" then
    local inputs = rb.call("rimgate/prepare_alpha_clinical_inputs", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, patientId = params.patientId, shooterId = params.shooterId, sarcophagusId = params.sarcophagusId })
    rb.assert(inputs.success and inputs.result.success, "Stage only native starting health and loose weapon inputs.")
    return { success = true, fixture = inputs.result }
end
local f = params.fixture
local before = rb.call("rimgate/alpha_clinical_inputs_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, patientId = f.patientId, shooterId = f.shooterId, sarcophagusId = f.sarcophagusId })
rb.assert(before.success and before.result.success, "Read actual clinical state.")
if params.phase == "equip-menu" or params.phase == "equip" then
    rb.assert(before.result.patientHeld == false and before.result.sarcExists and before.result.patientDead == false and before.result.patientDowned == false, "Equip before admitting the healthy patient.")
    local selected = rb.call("rimworld/select_pawn", { pawnId = f.shooterId })
    rb.assert(selected.success and selected.result.success, "Select the actual shooter.")
    local menu = rb.call("rimworld/right_click_cell", { x = f.gunX, z = f.gunZ })
    rb.assert(menu.success and menu.result.success and menu.result.provider == "ui_event", "Open the real loose-weapon menu.")
    if params.phase == "equip-menu" then return { success = true, menu = menu.result } end
    rb.assert(params.equipLabel ~= nil, "Supply the actual enabled Equip label.")
    local ordered = rb.call("rimworld/execute_context_menu_option", { label = params.equipLabel })
    rb.assert(ordered.success and ordered.result.success, "Order actual native equipping.")
    rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    local equipped = rb.poll("rimgate/alpha_clinical_inputs_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, patientId = f.patientId, shooterId = f.shooterId, sarcophagusId = f.sarcophagusId }, { timeoutMs = 10000, pollIntervalMs = 50, condition = { path = "result.shooterPrimaryId", equals = f.gunId } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success and equipped.success and equipped.result.success, "Pause only after the original input gun is actually equipped.")
elseif params.phase == "admit-menu" or params.phase == "admit" then
    rb.assert(before.result.patientHeld == false and before.result.patientDead == false and before.result.patientDowned == false and before.result.brittleActive and before.result.shooterPrimaryId == f.gunId, "Admit the actual conscious brittle patient after real equipping.")
    local addictionCount = 0
    for i, hediff in ipairs(before.result.patientHediffs) do
        if hediff.defName == "AlcoholAddiction" or hediff.defName == "SmokeleafAddiction" or hediff.defName == "PsychiteAddiction" then addictionCount = addictionCount + 1 end
    end
    rb.assert(addictionCount == 3, "The real long-treatment starting conditions must remain present.")
    rb.call("rimworld/select_pawn", { pawnId = f.patientId })
    local menu = rb.call("rimworld/right_click_cell", { x = f.sarcX, z = f.sarcZ })
    rb.assert(menu.success and menu.result.success and menu.result.provider == "ui_event", "Open the actual admission menu.")
    if params.phase == "admit-menu" then return { success = true, menu = menu.result } end
    rb.assert(params.admitLabel ~= nil, "Supply the actual enabled admission label.")
    local ordered = rb.call("rimworld/execute_context_menu_option", { label = params.admitLabel })
    rb.assert(ordered.success and ordered.result.success, "Order real sarcophagus admission.")
    rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    local held = rb.poll("rimgate/alpha_clinical_inputs_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, patientId = f.patientId, shooterId = f.shooterId, sarcophagusId = f.sarcophagusId }, { timeoutMs = 10000, pollIntervalMs = 50, condition = { path = "result.patientHeld", equals = true } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success and held.success and held.result.success and held.result.patientDowned == false, "Pause with the original conscious patient actually deep-owned by the sarcophagus.")
elseif params.phase == "combat-menu" or params.phase == "combat" then
    rb.assert(before.result.patientHeld and before.result.patientDead == false and before.result.patientDowned == false and before.result.brittleActive and before.result.shooterPrimaryId == f.gunId, "Require the exact occupied clinical input and equipped gun.")
    rb.call("rimworld/select_pawn", { pawnId = f.shooterId })
    local drafted = rb.call("rimworld/set_draft", { pawnId = f.shooterId, drafted = true })
    rb.assert(drafted.success and drafted.result.success, "Draft the real shooter.")
    local commands = rb.call("rimworld/list_selected_gizmos", {})
    rb.assert(commands.success and commands.result.success, "Read actual weapon commands.")
    if params.phase == "combat-menu" then return { success = true, gizmos = commands.result.gizmos } end
    local validCommand = false
    for i, command in ipairs(commands.result.gizmos) do
        if command.id == params.attackGizmoId and command.gizmoType == "Verse.Command_VerbTarget" and command.disabled == false then validCommand = true end
    end
    rb.assert(validCommand, "Use the actual ranged verb-target gizmo rather than Fire at will.")
    rb.assert(params.maxTicks == nil or (params.maxTicks >= 1 and params.maxTicks <= 4500), "Bound ordinary combat observation to at most4500 ticks.")
    local watch = rb.call("rimgate/begin_alpha_clinical_watch", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, patientId = f.patientId, durationMs = 60000 })
    rb.assert(watch.success and watch.result.success and watch.result.observerRegistered and watch.result.active, "Register the actual passive observer before attacking.")
    local targeting = rb.call("rimworld/execute_gizmo", { gizmoId = params.attackGizmoId })
    rb.assert(targeting.success and targeting.result.success, "Activate the actual ranged targeting command.")
    local target = rb.call("rimworld/click_cell", { x = f.sarcX, z = f.sarcZ, button = "left" })
    rb.assert(target.success and target.result.success, "Select the occupied sarcophagus as the real attack target.")
    rb.call("rimworld/step_game_ticks", { ticks = 1, timeoutMs = 5000 })
    local job = rb.call("rimgate/alpha_clinical_inputs_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, patientId = f.patientId, shooterId = f.shooterId, sarcophagusId = f.sarcophagusId })
    rb.assert(job.success and job.result.success and job.result.shooterJob == "AttackStatic" and job.result.shooterTargetId == f.sarcophagusId, "Require a real player attack job against the exact occupied sarcophagus.")
    rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    local terminal = rb.poll("rimgate/alpha_clinical_inputs_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, patientId = f.patientId, shooterId = f.shooterId, sarcophagusId = f.sarcophagusId }, { timeoutMs = 15000, pollIntervalMs = 50, condition = { any = { { path = "result.sarcExists", equals = false }, { path = "result.patientHeld", equals = false }, { path = "result.tick", greaterThanOrEqual = job.result.tick + (params.maxTicks or 3000) } } } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    local ended = rb.call("rimgate/end_alpha_clinical_watch", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot })
    rb.assert(paused.success and paused.result.success and ended.success and ended.result.success and ended.result.unpatched and ended.result.observerRegistered == false, "Pause and verify actual owned observer cleanup before assessing acceptance.")
    rb.assert(terminal.success and terminal.result.success, "Read the bounded actual combat boundary.")
    local qualifying = false
    for i, call in ipairs(ended.result.calls) do
        if call.patientId == f.patientId and call.holderId == f.sarcophagusId and call.ownedBySarcophagus and call.returned == false then qualifying = true end
    end
    rb.assert(qualifying, "Zero calls or a non-held call cannot establish the Alpha clinical suppression contract.")
    return { success = true, before = before.result, actualCombatJob = job.result, after = terminal.result, watch = ended.result, limits = "An actual qualifying held-patient guard call only. This does not establish weapon balance, every damage path or long-term patient survival. No treatment/HP/RNG/downing outcome was inserted." }
else rb.assert(params.phase == "status", "Choose inputs, equip-menu/equip, admit-menu/admit, combat-menu/combat or status.") end
local after = rb.call("rimgate/alpha_clinical_inputs_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, patientId = f.patientId, shooterId = f.shooterId, sarcophagusId = f.sarcophagusId })
rb.assert(after.success and after.result.success, "Read actual resulting clinical state.")
return { success = true, before = before.result, after = after.result }
