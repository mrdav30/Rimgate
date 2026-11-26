-- Remaining medical paths, using MedicalProbe and the existing medical roundtrips.
-- prepare: cloneMode Genome|Full|Enhanced|Reconstruct; initialBiomass defaults250,
-- looseBiomass250. Optional patientConditionDef/patientTraitDef create fresh inputs.
-- sourceSave defaults to Rimgate-bridge-smoke; use the owned matching-profile save
-- for optional integrations so repeated preparation does not repeat faction prompts.
-- host/corpse/calibrate perform real jobs. The corpse job also calibrates; use
-- corpse then wait-stage for Reconstruct. wait-stage accepts Incubating|Paused;
-- progress advances ordinary Ultrafast ticks in bounded chunks, never writes progress.
-- refuel-menu exposes real UI; refuel requires its actual refuelLabel. Abort/eject
-- and sarc-abort execute real player gizmos. reset-roundtrip verifies empty cleanup.
-- Existing rimbridge-medical handles patient entry, active roundtrip and completion.
-- Pass fixture returned by prepare; retain first cloneId from wait-stage/progress.
-- Compile with installed SDK. Host MUST pause in finally, max60s per invocation.
local config = rb.call("rimworld/get_mod_configuration_status", {})
rb.assert(config.success and config.result.success and config.result.configPath == params.configPath and config.result.configurationIssueCount == 0, "Use the healthy expected private profile.")
local deployment = false
for i, mod in ipairs(config.result.activeMods) do
    if mod.packageId == "mrdav30.rimgate" then deployment = mod.loadedInSession and mod.rootDir == params.modRoot end
end
rb.assert(deployment, "Expected private deployment is not loaded.")
if params.phase == "prepare" then
    local loaded = rb.call("rimworld/load_game_ready", { saveName = params.sourceSave or "Rimgate-bridge-smoke", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.success and loaded.result.success, "Load the owned smoke input.")
    local base = rb.call("rimgate/prepare_medical_fixture", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot })
    rb.assert(base.success and base.result.success, "Prepare the existing native medical power/actor inputs.")
    local f = base.result
    local edge = rb.call("rimgate/prepare_medical_edge_inputs", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, hostId = f.hostId, doctorId = f.doctorId, patientId = f.patientId, podId = f.podId, sarcophagusId = f.sarcophagusId, hostMarkerId = f.hostMarkerId, patientMarkerId = f.patientMarkerId, injuryId = f.injuryId, cloneMode = params.cloneMode or "Full", initialBiomass = params.initialBiomass or 250, looseBiomass = params.looseBiomass or 250, patientConditionDef = params.patientConditionDef or "", patientTraitDef = params.patientTraitDef or "", patientRaceDef = params.patientRaceDef or "", patientXenotypeDef = params.patientXenotypeDef or "", patientEbfArm = params.patientEbfArm or false, patientBrittleBones = params.patientBrittleBones or false })
    rb.assert(edge.success and edge.result.success, "Prepare only the bounded clone/health/resource inputs.")
    local ticks = rb.call("rimworld/step_game_ticks", { ticks = 750, timeoutMs = 30000 })
    rb.assert(ticks.success and ticks.result.success, "Allow native power connection.")
    return { success = true, fixture = edge.result }
end
local f = params.fixture
local cloneId = params.cloneId or ""
local requestedState = params.state or "Incubating"
local beforeCall = rb.call("rimgate/medical_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, hostId = f.hostId, doctorId = f.doctorId, patientId = f.patientId, podId = f.podId, sarcophagusId = f.sarcophagusId, hostMarkerId = f.hostMarkerId, patientMarkerId = f.patientMarkerId, cloneId = cloneId })
rb.assert(beforeCall.success and beforeCall.result.success, "Read the actual medical fixture.")
local before = beforeCall.result
if cloneId == "" and before.pod.pendingCloneId ~= nil then cloneId = before.pod.pendingCloneId end
if params.phase == "host" or params.phase == "calibrate" then
    local actorId = f.hostId
    local label = before.pod.genomeEntryLabel
    if f.cloneMode == "Full" then label = before.pod.fullEntryLabel end
    if f.cloneMode == "Enhanced" then label = before.pod.enhancedEntryLabel end
    if params.phase == "calibrate" then
        actorId = f.doctorId
        label = before.pod.calibrationLabel
        rb.assert(before.pod.hostId == f.hostId and before.pod.type == f.cloneMode, "Calibrate the actually admitted host and UI-selected clone type.")
    else
        rb.assert(f.cloneMode ~= "Reconstruct" and before.pod.hostCount == 0 and before.pod.pendingCount == 0, "Use corpse for actual reconstruction admission.")
    end
    rb.assert(before.pod.powered and before.pod.status == "Idle", "Use the powered idle pod.")
    local selected = rb.call("rimworld/select_pawn", { pawnId = actorId })
    rb.assert(selected.success and selected.result.success, "Select the real medical actor.")
    local menu = rb.call("rimworld/right_click_cell", { x = before.pod.x, z = before.pod.z })
    rb.assert(menu.success and menu.result.success and menu.result.provider == "ui_event", "Open the real medical menu.")
    local ordered = rb.call("rimworld/execute_context_menu_option", { label = label })
    rb.assert(ordered.success and ordered.result.success, "Order actual admission/calibration.")
    if params.phase == "host" then
        local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
        rb.assert(speed.success and speed.result.success, "Advance the real admission job.")
        local held = rb.poll("rimgate/medical_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, hostId = f.hostId, doctorId = f.doctorId, patientId = f.patientId, podId = f.podId, sarcophagusId = f.sarcophagusId, hostMarkerId = f.hostMarkerId, patientMarkerId = f.patientMarkerId }, { timeoutMs = 30000, pollIntervalMs = 50, condition = { path = "result.pod.hostId", equals = f.hostId } })
        local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
        rb.assert(paused.success and paused.result.success and held.success and held.result.success, "Pause after actual host admission.")
    else
        local ticks = rb.call("rimworld/step_game_ticks", { ticks = 100, timeoutMs = 10000 })
        rb.assert(ticks.success and ticks.result.success, "Observe unfinished default calibration work.")
    end
elseif params.phase == "corpse" then
    rb.assert(f.cloneMode == "Reconstruct" and before.pod.status == "Idle" and before.pod.powered and before.pod.hostCount == 0 and before.host.pawn.dead, "Use the actual newly prepared dead donor and idle pod.")
    local cleared = rb.call("rimworld/clear_selection", {})
    rb.assert(cleared.success and cleared.result.success, "Clear previous selection so the real click does not cycle to an underlying conduit.")
    local selected = rb.call("rimworld/click_cell", { x = before.pod.x, z = before.pod.z, button = "left" })
    rb.assert(selected.success and selected.result.success, "Select the real reconstruction pod.")
    local commands = rb.call("rimworld/list_selected_gizmos", {})
    rb.assert(commands.success and commands.result.success, "Read actual reconstruction commands.")
    local requested = false
    for i, command in ipairs(commands.result.gizmos) do
        if command.label == (params.reconstructLabel or before.pod.reconstructGizmoLabel) then
            rb.assert(command.disabled == false, "Actual reconstruction prerequisites are not met.")
            local clicked = rb.call("rimworld/execute_gizmo", { gizmoId = command.id })
            rb.assert(clicked.success and clicked.result.success, "Begin the real donor/operator selection.")
            requested = true
        end
    end
    rb.assert(requested, "The actual reconstruction command was absent.")
    local actor = rb.call("rimworld/execute_context_menu_option", { label = params.operatorLabel or f.doctorLabel })
    rb.assert(actor.success and actor.result.success, "Select the actual corpse carrier from the real menu.")
    local target = rb.call("rimworld/click_cell", { x = f.corpseX, z = f.corpseZ, button = "left" })
    rb.assert(target.success and target.result.success, "Target the original fixture corpse.")
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Advance the real corpse carry/wait/admission job.")
    local held = rb.poll("rimgate/medical_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, hostId = f.hostId, doctorId = f.doctorId, patientId = f.patientId, podId = f.podId, sarcophagusId = f.sarcophagusId, hostMarkerId = f.hostMarkerId, patientMarkerId = f.patientMarkerId }, { timeoutMs = 30000, pollIntervalMs = 50, condition = { path = "result.pod.containsCorpse", equals = true } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success and held.success and held.result.success, "Pause after actual corpse admission.")
elseif params.phase == "wait-treatment" then
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Run the actual diagnosed treatment queue.")
    local completed = rb.poll("rimgate/medical_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, hostId = f.hostId, doctorId = f.doctorId, patientId = f.patientId, podId = f.podId, sarcophagusId = f.sarcophagusId, hostMarkerId = f.hostMarkerId, patientMarkerId = f.patientMarkerId }, { timeoutMs = 30000, pollIntervalMs = 100, condition = { all = { { path = "result.sarcophagus.status", equals = "Idle" }, { path = "result.sarcophagus.ownedCount", equals = 0 }, { path = "result.patient.spawned", equals = true } } } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success and completed.success and completed.result.success, "Pause after the real complete queue and native patient discharge.")
elseif params.phase == "wait-stage" or params.phase == "progress" then
    local state = requestedState
    local ticks = params.ticks or 10000
    rb.assert(ticks >= 1 and ticks <= 30000, "Use bounded ordinary progress chunks.")
    if params.phase == "wait-stage" then rb.assert(state == "Incubating" or state == "Paused", "Wait for a real staged-clone state.") end
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Run ordinary native medical work without changing settings.")
    local reached = false
    if params.phase == "wait-stage" then
        reached = rb.poll("rimgate/medical_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, hostId = f.hostId, doctorId = f.doctorId, patientId = f.patientId, podId = f.podId, sarcophagusId = f.sarcophagusId, hostMarkerId = f.hostMarkerId, patientMarkerId = f.patientMarkerId, cloneId = cloneId }, { timeoutMs = 30000, pollIntervalMs = 50, condition = { path = "result.pod.status", equals = state } })
    else
        reached = rb.poll("rimgate/medical_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, hostId = f.hostId, doctorId = f.doctorId, patientId = f.patientId, podId = f.podId, sarcophagusId = f.sarcophagusId, hostMarkerId = f.hostMarkerId, patientMarkerId = f.patientMarkerId, cloneId = cloneId }, { timeoutMs = 30000, pollIntervalMs = 50, condition = { path = "result.tick", greaterThanOrEqual = before.tick + ticks } })
    end
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success and reached.success and reached.result.success, "Pause after the bounded actual medical state/clock observation.")
elseif params.phase == "refuel-menu" or params.phase == "refuel" then
    rb.assert(before.pod.status == "Paused" and before.pod.pendingCount == 1 and before.pod.cycleFuelConsumed == false, "Refuel only a genuinely resource-paused staged clone.")
    local selected = rb.call("rimworld/select_pawn", { pawnId = f.doctorId })
    rb.assert(selected.success and selected.result.success, "Select the actual native refueler.")
    local menu = rb.call("rimworld/right_click_cell", { x = before.pod.x, z = before.pod.z })
    rb.assert(menu.success and menu.result.success and menu.result.provider == "ui_event", "Read the actual native refueling menu.")
    if params.phase == "refuel-menu" then return { success = true, menu = menu.result, status = before, instruction = "Pass its enabled native refuelLabel to refuel; toggle native auto-refuel through UI first if required." } end
    rb.assert(params.refuelLabel, "Supply the actual enabled native refueling label.")
    local ordered = rb.call("rimworld/execute_context_menu_option", { label = params.refuelLabel })
    rb.assert(ordered.success and ordered.result.success, "Order actual native biomass hauling/refueling.")
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Let native refueling and cycle admission run.")
    local resumed = rb.poll("rimgate/medical_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, hostId = f.hostId, doctorId = f.doctorId, patientId = f.patientId, podId = f.podId, sarcophagusId = f.sarcophagusId, hostMarkerId = f.hostMarkerId, patientMarkerId = f.patientMarkerId, cloneId = cloneId }, { timeoutMs = 30000, pollIntervalMs = 50, condition = { any = { { path = "result.pod.status", equals = "Incubating" }, { path = "result.pod.fuel", greaterThan = before.pod.fuel } } } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success and resumed.success and resumed.result.success, "Pause after actual native refuel/resume.")
elseif params.phase == "abort" or params.phase == "eject" or params.phase == "sarc-abort" then
    local x = before.pod.x
    local z = before.pod.z
    local label = before.pod.abortLabel
    if params.phase == "abort" then rb.assert(before.pod.status == "Incubating" and before.pod.pendingCount == 1 and before.pod.cycleFuelConsumed, "Abort a genuinely staged/fueled pending clone.") end
    if params.phase == "eject" then
        rb.assert(before.pod.hostCount == 1, "Eject an actually admitted host.")
        label = before.pod.ejectLabel
    end
    if params.phase == "sarc-abort" then
        rb.assert(before.sarcophagus.ownedCount == 1 and before.sarcophagus.needsSnapshotTaken and (before.sarcophagus.status == "DiagnosisStarted" or before.sarcophagus.status == "HealingStarted"), "Abort an actual unfinished diagnosis or treatment.")
        x = before.sarcophagus.x
        z = before.sarcophagus.z
        label = before.sarcophagus.abortLabel
    end
    local cleared = rb.call("rimworld/clear_selection", {})
    rb.assert(cleared.success and cleared.result.success, "Clear selection before the real medical building click.")
    local selected = rb.call("rimworld/click_cell", { x = x, z = z, button = "left" })
    rb.assert(selected.success and selected.result.success, "Select the actual occupied medical building.")
    local commands = rb.call("rimworld/list_selected_gizmos", {})
    rb.assert(commands.success and commands.result.success, "Read normal abort/eject commands.")
    local requested = false
    for i, command in ipairs(commands.result.gizmos) do
        if command.label == label then
            rb.assert(command.disabled == false, "The real abort/eject action is disabled.")
            local clicked = rb.call("rimworld/execute_gizmo", { gizmoId = command.id })
            rb.assert(clicked.success and clicked.result.success, "Execute the real medical abort/eject command.")
            requested = true
        end
    end
    rb.assert(requested, "The real abort/eject command was absent.")
elseif params.phase == "reset-roundtrip" then
    rb.assert(before.pod.status == "Idle" and before.pod.hostCount == 0 and before.pod.pendingCount == 0 and before.sarcophagus.status == "Idle" and before.sarcophagus.ownedCount == 0, "Roundtrip only actual empty completed/aborted owners.")
    local saved = rb.call("rimworld/save_game", { saveName = "Rimgate-medical-edges-reset" })
    rb.assert(saved.success and saved.result.success and saved.result.exists, "Save actual consumed/reset state.")
    local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-medical-edges-reset", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.success and loaded.result.success, "Reload actual reset state.")
elseif params.phase == "resource-roundtrip" then
    rb.assert(before.pod.status == "Paused" and before.pod.pendingCount == 1 and before.pod.cycleFuelConsumed == false, "Save an actual resource-paused staged clone.")
    local saved = rb.call("rimworld/save_game", { saveName = "Rimgate-medical-resource-paused" })
    rb.assert(saved.success and saved.result.success and saved.result.exists, "Save the actual resource pause without staging or fuel writes.")
    local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-medical-resource-paused", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.success and loaded.result.success, "Reload the same actual resource pause.")
elseif params.phase == "assert-host-needs" then
    rb.assert(f.cloneMode ~= "Reconstruct" and before.host.spawned and before.pod.hostCount == 0 and before.pod.needsSnapshotTaken and (before.pod.status == "Incubating" or before.pod.status == "Paused"), "Observe the donor immediately after real discharge, before ordinary needs/jobs change it.")
    rb.assert(before.host.pawn.food == before.pod.savedFood, "Real donor discharge must restore its exact native saved food value; retain a single-tick boundary if ordinary post-discharge drift is observed.")
elseif params.phase == "release-actors" then
    rb.assert(before.pod.hostCount == 0 and before.doctor.pawn.job ~= "Rimgate_CalibrateClonePodForPawn", "Release parked actors only after actual donor discharge/calibration, not during required jobs.")
    for i, pawnId in ipairs(f.routinePawnIds) do
        local drafted = rb.call("rimworld/set_draft", { pawnId = pawnId, drafted = true })
        rb.assert(drafted.success and drafted.result.success, "Use normal drafting to cancel a fixture wait.")
        local released = rb.call("rimworld/set_draft", { pawnId = pawnId, drafted = false })
        rb.assert(released.success and released.result.success, "Return the actual actor to ordinary AI needs/eating before long default cycles.")
    end
elseif params.phase == "assert-clone-shape" then
    rb.assert(cloneId ~= "" and before.pod.pendingCount == 1 and before.pod.pendingCloneId == cloneId and before.clone.pawn.dead == false and before.clone.pawn.cloneGeneration ~= nil and before.pod.sharedHostCloneTraitReferences == 0, "The actual staged clone must have its own trait instances and clone tracking before any reload or completion.")
    for i, trait in ipairs(before.host.pawn.traits) do rb.assert(trait.ownerIsThisPawn, "Cloning changed a host trait's actual owner.") end
    for i, trait in ipairs(before.clone.pawn.traits) do rb.assert(trait.ownerIsThisPawn, "A clone trait must belong to the actual clone.") end
    if f.cloneMode == "Enhanced" and before.pod.outcome.minorRandomTraits == false then
        local mood = false
        local nerves = false
        local ascetic = false
        for i, trait in ipairs(before.clone.pawn.traits) do
            if trait.defName == "NaturalMood" and trait.degree == 2 then mood = true end
            if trait.defName == "Nerves" and trait.degree == 2 then nerves = true end
            if trait.defName == "Ascetic" and trait.degree == 0 then ascetic = true end
        end
        rb.assert(mood and nerves and ascetic, "The actual enhanced clone did not receive its configured baseline trait set; inspect recorded failure outcome.")
    end
elseif params.phase == "assert-eligibility" then
    rb.assert(before.patient.spawned and before.sarcophagus.powered and before.sarcophagus.status == "Idle" and before.sarcophagus.ownedCount == 0 and before.sarcophagus.policyDenials.medicalCareAllowed, "Observe a fresh spawned subject, powered empty sarcophagus and allowed medical care.")
    local injured = false
    for i, injury in ipairs(before.patient.pawn.hediffs) do
        if injury.id == f.injuryId and (injury.defName == "Burn" or injury.defName == "Bruise") and injury.severity > 0 and injury.visible and injury.tendableNow then injured = true end
    end
    rb.assert(injured, "Eligibility integration cases need an independent actual tendable Burn/Bruise; a not-injured denial cannot establish a policy rejection.")
    local enabled = false
    for i, option in ipairs(before.sarcophagus.patientMenu) do
        if option.disabled == false then enabled = true end
    end
    rb.assert(enabled == params.enabled, "The actual fresh patient's native sarcophagus eligibility differs from the expected loaded policy.")
    if params.enabled == false then
        local label = params.expectedDenialLabel or ""
        local kind = params.denialKind or ""
        if kind == "race" then
            rb.assert(before.sarcophagus.policyDenials.raceBlocked and before.patient.pawn.raceDef == f.patientRaceDef, "The intended actual race must be excluded by the loaded policy.")
            label = before.sarcophagus.policyDenials.raceLabel
        elseif kind == "xenotype" then
            rb.assert(before.sarcophagus.policyDenials.xenotypeBlocked and before.patient.pawn.xenotypeDef == f.patientXenotypeDef, "The intended actual xenotype must be excluded by the loaded policy.")
            label = before.sarcophagus.policyDenials.xenotypeLabel
        elseif kind == "hediff" then
            rb.assert(before.sarcophagus.policyDenials.blockingHediffCount > 0, "The actual patient needs a resolved native blocking hediff.")
            label = before.sarcophagus.policyDenials.blockingHediffLabels[1]
        elseif kind == "trait" then
            rb.assert(before.sarcophagus.policyDenials.blockingTraitCount > 0, "The actual patient needs a resolved native blocking trait.")
            label = before.sarcophagus.policyDenials.blockingTraitLabels[1]
        end
        rb.assert(label ~= "", "Supply denialKind race/xenotype/hediff/trait or the exact independently expected native denial label.")
        local denied = false
        for i, option in ipairs(before.sarcophagus.patientMenu) do
            if option.disabled and option.label == label then denied = true end
        end
        rb.assert(denied, "The actual disabled menu must give the intended policy denial, not not-injured/power/assignment/medical-care failure.")
    else
        rb.assert(before.sarcophagus.policyDenials.raceBlocked == false and before.sarcophagus.policyDenials.xenotypeBlocked == false and before.sarcophagus.policyDenials.blockingHediffCount == 0 and before.sarcophagus.policyDenials.blockingTraitCount == 0, "The injured positive control must have no loaded race/xenotype/hediff/trait blocker.")
    end
    return { success = true, fixture = f, status = before, limits = "Actual native pawn/race/xenotype and real medical menu only. Blocked nonhuman races may lack inventory; this eligibility check makes no treatment or nested-inventory claim." }
elseif params.phase == "kill-clones" or params.phase == "plague" then
    if params.phase == "kill-clones" then
        rb.assert(cloneId ~= "" and before.clone.spawned and before.clone.pawn.dead == false and before.clone.pawn.cloneGeneration ~= nil, "Execute the real incident only against an actual completed living clone.")
    else
        rb.assert(before.patient.spawned and before.patient.pawn.dead == false and f.patientConditionDef == "Rimgate_SymbioteImplant", "Use the fresh symbiote-host eligibility input; never seed the plague itself.")
    end
    local browse = rb.call("rimworld/list_debug_action_children", { path = "Actions\\Do incident" })
    rb.assert(browse.success and browse.result.success, "Discover the installed native incident actions.")
    local path = "Actions\\Do incident\\Rimgate_KillAllClonesNow"
    if params.phase == "plague" then path = "Actions\\Do incident\\Rimgate_SymbiotePlague" end
    local incident = rb.call("rimworld/execute_debug_action", { path = path })
    rb.assert(incident.success and incident.result.success, "Execute the actual production incident worker.")
elseif params.phase == "solar-flare" or params.phase == "power-condition" then
    rb.assert(before.pod.status == "Idle" and before.pod.hostCount == 0 and before.pod.pendingCount == 0 and before.sarcophagus.ownedCount == 1 and before.sarcophagus.powered and (before.sarcophagus.status == "DiagnosisStarted" or before.sarcophagus.status == "HealingStarted"), "Interrupt only actual unfinished sarc treatment while other pod cycles are idle.")
    local browse = rb.call("rimworld/list_debug_action_children", { path = "Actions\\Do incident" })
    rb.assert(browse.success and browse.result.success, "Discover native incident actions.")
    local conditionPath = "Actions\\Do incident\\SolarFlare"
    if params.phase == "power-condition" then conditionPath = "Actions\\Add Game Condition...\\Solar flare\\1 day" end
    local incident = rb.call("rimworld/execute_debug_action", { path = conditionPath })
    rb.assert(incident.success and incident.result.success, "Trigger the real native electricity-disabling incident.")
    rb.assert(incident.result.effects.logCount == 0, "The native incident was not eligible on this map; use the power-condition native environmental-input phase instead.")
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Allow real grid power loss and native medical discharge.")
    local ejected = rb.poll("rimgate/medical_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, hostId = f.hostId, doctorId = f.doctorId, patientId = f.patientId, podId = f.podId, sarcophagusId = f.sarcophagusId, hostMarkerId = f.hostMarkerId, patientMarkerId = f.patientMarkerId, cloneId = cloneId }, { timeoutMs = 30000, pollIntervalMs = 50, condition = { all = { { path = "result.sarcophagus.powered", equals = false }, { path = "result.sarcophagus.status", equals = "Idle" }, { path = "result.sarcophagus.ownedCount", equals = 0 }, { path = "result.patient.spawned", equals = true } } } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success and ejected.success and ejected.result.success, "Pause after actual power-loss interruption.")
elseif params.phase == "end-power-condition" then
    rb.assert(before.sarcophagus.power.electricityDisabled and params.endConditionPath, "Supply the discovered actual solar-flare end action; medical power may not be set directly.")
    local browse = rb.call("rimworld/list_debug_action_children", { path = "Actions\\Remove Game Condition..." })
    rb.assert(browse.success and browse.result.success, "Discover the actual condition cleanup action.")
    local ended = rb.call("rimworld/execute_debug_action", { path = params.endConditionPath })
    rb.assert(ended.success and ended.result.success, "End the native incident through its normal debug lifecycle; this is not natural-duration acceptance.")
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Allow the native grid to recover.")
    local powered = rb.poll("rimgate/medical_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, hostId = f.hostId, doctorId = f.doctorId, patientId = f.patientId, podId = f.podId, sarcophagusId = f.sarcophagusId, hostMarkerId = f.hostMarkerId, patientMarkerId = f.patientMarkerId, cloneId = cloneId }, { timeoutMs = 30000, pollIntervalMs = 50, condition = { path = "result.sarcophagus.powered", equals = true } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success and powered.success and powered.result.success, "Pause after actual incident cleanup and native power recovery.")
elseif params.phase == "incident-roundtrip" then
    local saved = rb.call("rimworld/save_game", { saveName = "Rimgate-medical-incident" })
    rb.assert(saved.success and saved.result.success and saved.result.exists, "Save the actual incident result.")
    local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-medical-incident", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.success and loaded.result.success, "Reload the exact incident checkpoint.")
elseif params.phase == "assert-plague-treated" then
    rb.assert(before.patient.spawned and before.patient.pawn.dead == false and before.sarcophagus.status == "Idle" and before.sarcophagus.ownedCount == 0, "Finish the actual native patient treatment before checking plague removal.")
    for i, hediff in ipairs(before.patient.pawn.hediffs) do rb.assert(hediff.defName ~= "Rimgate_SymbiotePlague", "Normal treatment did not remove the actual incident disease.") end
elseif params.phase == "assert-cycle-complete" then
    local pending = params.pendingStatus
    rb.assert(cloneId ~= "" and pending.pod.pendingCloneId == cloneId and pending.pod.cycleFuelConsumed and pending.pod.type == f.cloneMode, "Retain the actual staged outcome and already-charged fuel balance.")
    rb.assert(before.pod.status == "Idle" and before.pod.pendingCount == 0 and before.pod.hostCount == 0 and before.pod.cycleFuelConsumed == false and before.pod.fuel == pending.pod.fuel and before.clone.distinctObjectCount == 1 and before.clone.ownershipOccurrences + before.clone.mapOccurrences == 1, "The same actual clone must complete once, empty owners and retain the single charged biomass balance.")
    rb.assert(before.clone.pawn.dead == pending.pod.outcome.majorCloneDies, "Actual clone survival must match the recorded default calibration outcome, not forced favorable RNG.")
    if pending.pod.outcome.majorCloneDies == false then rb.assert(before.clone.spawned and before.clone.pawn.cloneGeneration ~= nil, "A surviving actual clone must emerge with its generation tracking.") end
else
    rb.assert(params.phase == "status", "Choose a documented medical edge phase.")
end
local afterCall = rb.call("rimgate/medical_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, hostId = f.hostId, doctorId = f.doctorId, patientId = f.patientId, podId = f.podId, sarcophagusId = f.sarcophagusId, hostMarkerId = f.hostMarkerId, patientMarkerId = f.patientMarkerId, cloneId = cloneId })
rb.assert(afterCall.success and afterCall.result.success, "Read fresh paused medical outcomes.")
local after = afterCall.result
rb.assert(after.host.distinctObjectCount == 1 and after.host.ownershipOccurrences + after.host.mapOccurrences == 1 and after.patient.distinctObjectCount == 1 and after.patient.ownershipOccurrences + after.patient.mapOccurrences == 1 and after.hostMarker.distinctObjectCount == 1 and after.hostMarker.ownershipOccurrences + after.hostMarker.mapOccurrences == 1 and after.hostMarker.stackCount == 7 and after.patientMarker.distinctObjectCount == 1 and after.patientMarker.ownershipOccurrences + after.patientMarker.mapOccurrences == 1 and after.patientMarker.stackCount == 11, "Retain actual host/patient and exact marker objects/quantities with one actual owner or map registration.")
if after.pod.hostCount == 1 then rb.assert(after.hostMarker.ownershipOccurrences == 1, "An admitted donor must retain its exact nested inventory owner.") end
if after.sarcophagus.ownedCount == 1 then rb.assert(after.patientMarker.ownershipOccurrences == 1, "An admitted patient must retain its exact nested inventory owner.") end
if params.phase == "host" or params.phase == "corpse" then rb.assert(after.pod.hostId == f.hostId and after.pod.type == f.cloneMode and after.pod.hostCount == 1, "Real admission must retain the exact selected donor and clone mode.") end
if params.phase == "corpse" then rb.assert(after.pod.containsCorpse and after.pod.hosts[1] == f.corpseId, "The real carry job must admit the exact original corpse wrapper and its tracked inner donor.") end
if params.phase == "calibrate" then rb.assert(after.pod.status == "CloningStarted" and after.doctor.pawn.job == "Rimgate_CalibrateClonePodForPawn" and after.pod.calibrationRemaining > 0, "Observe the unfinished actual calibration job.") end
if params.phase == "wait-stage" then
    rb.assert(after.pod.pendingCount == 1 and after.clone.distinctObjectCount == 1 and after.clone.ownershipOccurrences == 1 and after.pod.hostCount == 0, "Actual calibration must stage exactly one deeply owned clone and discharge its donor.")
    if requestedState == "Paused" then rb.assert(after.pod.cycleFuelConsumed == false and after.pod.fuel == f.initialBiomass, "Insufficient biomass must pause without consuming fuel or replacing the staged clone.") end
end
if params.phase == "refuel" then
    rb.assert(after.pod.pendingCloneId == cloneId and after.pod.pendingCount == 1, "Native refueling must retain the same staged clone.")
    if after.pod.status == "Incubating" then rb.assert(after.pod.cycleFuelConsumed, "Resumption must pay the actual cycle cost.")
    else rb.assert(after.pod.status == "Paused" and after.pod.cycleFuelConsumed == false and after.pod.fuel > before.pod.fuel, "A native carry-capacity-limited refuel must add fuel without free incubation; order another actual haul if needed.") end
end
if params.phase == "resource-roundtrip" then rb.assert(after.pod.status == "Paused" and after.pod.pendingCloneId == cloneId and after.pod.pendingCount == 1 and after.clone.distinctObjectCount == 1 and after.clone.ownershipOccurrences == 1 and after.pod.cycleFuelConsumed == false and after.pod.fuel == before.pod.fuel and after.pod.incubationRemaining == before.pod.incubationRemaining, "Resource reload must retain the exact pending clone/work without fuel consumption or free incubation.") end
if params.phase == "abort" or params.phase == "eject" or params.phase == "reset-roundtrip" then
    rb.assert(after.pod.status == "Idle" and after.pod.hostCount == 0 and after.pod.pendingCount == 0 and after.pod.cycleFuelConsumed == false and after.pod.fuel == before.pod.fuel, "Native abort/eject/reset must clear owners/state without a biomass refund or extra charge.")
    if cloneId ~= "" then
        if params.phase == "reset-roundtrip" then
            rb.assert(after.clone.distinctObjectCount == before.clone.distinctObjectCount and after.clone.ownershipOccurrences == before.clone.ownershipOccurrences and after.clone.mapOccurrences == before.clone.mapOccurrences, "Reset reload must preserve actual surviving-clone ownership or aborted-clone absence.")
        else
            rb.assert(after.clone.distinctObjectCount == 0 and after.clone.ownershipOccurrences == 0 and after.clone.mapOccurrences == 0, "An aborted pending clone may not survive in any loaded owner/map.")
        end
    end
end
if params.phase == "eject" then rb.assert(after.host.spawned and after.host.pawn.food == before.pod.savedFood, "Paused normal host ejection must restore the exact native saved food need.") end
if params.phase == "sarc-abort" then
    rb.assert(after.sarcophagus.status == "Idle" and after.sarcophagus.ownedCount == 0 and after.sarcophagus.treatmentCount == 0 and after.sarcophagus.treatmentTraitCount == 0 and after.patient.spawned and after.patient.pawn.food == before.sarcophagus.savedFood, "Paused native abort must eject/reset the same patient and restore the exact saved food need.")
    for i, target in ipairs(before.sarcophagus.treatmentIds) do
        local retained = false
        for j, hediff in ipairs(after.patient.pawn.hediffs) do if hediff.id == target then retained = true end end
        rb.assert(retained, "Aborting may not complete an actual pending hediff treatment.")
    end
    for i, target in ipairs(before.sarcophagus.treatmentTraitKeys) do
        local retained = false
        for j, trait in ipairs(after.patient.pawn.traits) do if trait.defName == target.defName and trait.degree == target.degree and trait.ownerIsThisPawn then retained = true end end
        rb.assert(retained, "Aborting may clear a queued removal but may not remove the patient's actual trait.")
    end
end
if params.phase == "kill-clones" then rb.assert(after.clone.distinctObjectCount == 1 and after.clone.pawn.dead and after.clone.ownershipOccurrences == 1 and after.clone.mapOccurrences == 0, "The actual incident must leave the exact dead clone inside its native corpse owner.") end
if params.phase == "plague" then
    local infected = false
    for i, hediff in ipairs(after.patient.pawn.hediffs) do if hediff.defName == "Rimgate_SymbiotePlague" then infected = true end end
    rb.assert(infected, "The actual incident did not infect the eligible fixture patient.")
end
if params.phase == "solar-flare" or params.phase == "power-condition" then
    rb.assert(after.sarcophagus.power.electricityDisabled and after.sarcophagus.powered == false and after.sarcophagus.ownedCount == 0 and after.sarcophagus.status == "Idle" and after.patient.spawned, "Actual native electricity-disabling power loss must eject/reset the patient without a forced PowerOn value.")
    for i, target in ipairs(before.sarcophagus.treatmentIds) do
        local retained = false
        for j, hediff in ipairs(after.patient.pawn.hediffs) do if hediff.id == target then retained = true end end
        rb.assert(retained, "The actual interrupted treatment must not complete its pending hediff targets.")
    end
end
if params.phase == "incident-roundtrip" then
    if cloneId ~= "" then rb.assert(before.clone.pawn.dead == after.clone.pawn.dead and after.clone.distinctObjectCount == 1 and after.clone.ownershipOccurrences + after.clone.mapOccurrences == 1, "Incident reload must preserve the exact clone lifetime/owner state.") end
    for i, original in ipairs(before.patient.pawn.hediffs) do
        if original.defName == "Rimgate_SymbiotePlague" then
            local retained = false
            for j, restored in ipairs(after.patient.pawn.hediffs) do if restored.id == original.id and restored.defName == original.defName then retained = true end end
            rb.assert(retained, "Incident reload must preserve the actual disease reference.")
        end
    end
end
if after.pod.pendingCloneId ~= nil then cloneId = after.pod.pendingCloneId end
return { success = true, fixture = f, cloneId = cloneId, before = before, status = after, limits = "Actual jobs/native gizmos and normal timers/settings only. Low needs, initial biomass, research and fresh donor/health conditions are fixture inputs. Loaded owner absence does not independently prove destruction. Clone failures remain native recorded outcomes; this does not force favorable RNG or repair old saves." }
