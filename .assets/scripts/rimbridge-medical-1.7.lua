-- Native medical acceptance, bounded independent invocations.
-- Required: configPath, modRoot, phase. prepare returns fixture for later calls.
-- host/patient/calibrate start genuine float-menu jobs. advance accepts ticks (1..30000).
-- roundtrip accepts checkpoint: host/calibration/incubating/diagnosing/healing.
-- Track the first pending cloneId returned by status in later calls.
-- assert-complete accepts target pod|sarcophagus; pod also requires cloneId and expectedFuel.
-- Interruption: caller uses the real building power gizmo and a real flick job,
-- then assert-interruption (target sarcophagus|pod, before status). Power on normally,
-- then assert-resumed (before interruption status). No fixture state is fast-forwarded.
local config = rb.call("rimworld/get_mod_configuration_status", {})
rb.assert(config.success and config.result.success and config.result.configPath == params.configPath
    and config.result.configurationIssueCount == 0, "Use the healthy isolated profile.")
local deployment = false
for i, mod in ipairs(config.result.activeMods) do
    if mod.packageId == "mrdav30.rimgate" then deployment = mod.loadedInSession and mod.rootDir == params.modRoot end
end
rb.assert(deployment, "Expected private deployment is not loaded.")
if params.phase == "prepare" then
    local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-bridge-smoke", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.success and loaded.result.success, "Load the owned smoke save.")
    local prepared = rb.call("rimgate/prepare_medical_fixture", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot })
    rb.assert(prepared.success and prepared.result.success, "Medical fixture preparation failed.")
    local ticks = rb.call("rimworld/step_game_ticks", { ticks = 750, timeoutMs = 30000 })
    rb.assert(ticks.success and ticks.result.success, "Power initialization failed.")
    return { success = true, fixture = prepared.result }
end
local f = params.fixture
local beforeCall = rb.call("rimgate/medical_status", {
    expectedConfigPath = params.configPath, expectedModRoot = params.modRoot,
    hostId = f.hostId, doctorId = f.doctorId, patientId = f.patientId,
    podId = f.podId, sarcophagusId = f.sarcophagusId,
    hostMarkerId = f.hostMarkerId, patientMarkerId = f.patientMarkerId,
    cloneId = params.cloneId or ""
})
rb.assert(beforeCall.success and beforeCall.result.success, "Medical status failed.")
local before = beforeCall.result
rb.assert(before.host.distinctObjectCount == 1 and before.host.ownershipOccurrences + before.host.mapOccurrences == 1, "Host must have one actual object and one owner/map registration.")
rb.assert(before.doctor.distinctObjectCount == 1 and before.doctor.ownershipOccurrences + before.doctor.mapOccurrences == 1, "Doctor must have one actual object and one owner/map registration.")
rb.assert(before.patient.distinctObjectCount == 1 and before.patient.ownershipOccurrences + before.patient.mapOccurrences == 1, "Patient must have one actual object and one owner/map registration.")
rb.assert(before.hostMarker.distinctObjectCount == 1 and before.hostMarker.ownershipOccurrences + before.hostMarker.mapOccurrences == 1, "Host inventory marker must have one actual object and one owner/map registration.")
rb.assert(before.patientMarker.distinctObjectCount == 1 and before.patientMarker.ownershipOccurrences + before.patientMarker.mapOccurrences == 1, "Patient inventory marker must have one actual object and one owner/map registration.")
rb.assert(before.hostMarker.stackCount == 7 and before.patientMarker.stackCount == 11, "Inventory marker quantity changed.")
if params.phase == "host" or params.phase == "patient" or params.phase == "calibrate" then
    local pawnId = f.hostId
    local building = before.pod
    local label = nil
    if params.phase == "patient" then
        pawnId = f.patientId
        building = before.sarcophagus
        rb.assert(building.powered, "Sarcophagus power prerequisite failed.")
        for i, option in ipairs(building.patientMenu) do
            if option.disabled == false then label = option.label end
        end
    elseif params.phase == "host" then
        rb.assert(building.powered, "Pod power prerequisite failed.")
        for i, option in ipairs(building.hostMenu) do
            if option.disabled == false and option.label == building.genomeEntryLabel then label = option.label end
        end
    else
        pawnId = f.doctorId
        rb.assert(building.powered and building.hostId == f.hostId, "Calibration needs the admitted host and power.")
        for i, option in ipairs(building.doctorMenu) do
            if option.disabled == false and option.label == (params.calibrationLabel or building.genomeCalibrationLabel) then label = option.label end
        end
    end
    rb.assert(label ~= nil, "Expected enabled medical action is absent; inspect the status prerequisites.")
    local select = rb.call("rimworld/select_pawn", { pawnId = pawnId })
    rb.assert(select.success and select.result.success, "Actor selection failed.")
    local menu = rb.call("rimworld/right_click_cell", { x = building.x, z = building.z })
    rb.assert(menu.success and menu.result.success, "Medical context menu failed.")
    local action = rb.call("rimworld/execute_context_menu_option", { label = label })
    rb.assert(action.success and action.result.success, "Real medical job request failed.")
    local step = rb.call("rimworld/step_game_ticks", { ticks = params.phase == "calibrate" and 100 or 600, timeoutMs = 30000 })
    rb.assert(step.success and step.result.success, "Medical entry/work stepping failed.")
    local afterCall = rb.call("rimgate/medical_status", {
        expectedConfigPath = params.configPath, expectedModRoot = params.modRoot,
        hostId = f.hostId, doctorId = f.doctorId, patientId = f.patientId,
        podId = f.podId, sarcophagusId = f.sarcophagusId,
        hostMarkerId = f.hostMarkerId, patientMarkerId = f.patientMarkerId,
        cloneId = params.cloneId or ""
    })
    rb.assert(afterCall.success and afterCall.result.success, "Medical status failed.")
    local after = afterCall.result
    rb.assert(after.host.distinctObjectCount == 1 and after.host.ownershipOccurrences + after.host.mapOccurrences == 1, "Host must have one actual object and one owner/map registration.")
    rb.assert(after.doctor.distinctObjectCount == 1 and after.doctor.ownershipOccurrences + after.doctor.mapOccurrences == 1, "Doctor must have one actual object and one owner/map registration.")
    rb.assert(after.patient.distinctObjectCount == 1 and after.patient.ownershipOccurrences + after.patient.mapOccurrences == 1, "Patient must have one actual object and one owner/map registration.")
    rb.assert(after.hostMarker.distinctObjectCount == 1 and after.hostMarker.ownershipOccurrences + after.hostMarker.mapOccurrences == 1, "Host inventory marker must have one actual object and one owner/map registration.")
    rb.assert(after.patientMarker.distinctObjectCount == 1 and after.patientMarker.ownershipOccurrences + after.patientMarker.mapOccurrences == 1, "Patient inventory marker must have one actual object and one owner/map registration.")
    rb.assert(after.hostMarker.stackCount == 7 and after.patientMarker.stackCount == 11, "Inventory marker quantity changed.")
    if params.phase == "host" then rb.assert(after.pod.hostId == f.hostId and after.host.ownershipOccurrences == 1, "Host entry did not transfer ownership.") end
    if params.phase == "patient" then rb.assert(after.sarcophagus.patientId == f.patientId and after.patient.ownershipOccurrences == 1, "Patient entry did not transfer ownership.") end
    if params.phase == "calibrate" then rb.assert(after.pod.status == "CloningStarted" and after.doctor.pawn.job == "Rimgate_CalibrateClonePodForPawn" and after.pod.calibrationRemaining > 0, "Observe an unfinished genuine calibration job.") end
    return { success = true, status = after }
end
if params.phase == "advance" then
    rb.assert(params.ticks >= 1 and params.ticks <= 30000, "Use bounded 1..30000 tick calls; inspect progress between calls.")
    local step = rb.call("rimworld/step_game_ticks", { ticks = params.ticks, timeoutMs = 60000 })
    rb.assert(step.success and step.result.success, "Medical stepping failed.")
    local afterCall = rb.call("rimgate/medical_status", {
        expectedConfigPath = params.configPath, expectedModRoot = params.modRoot,
        hostId = f.hostId, doctorId = f.doctorId, patientId = f.patientId,
        podId = f.podId, sarcophagusId = f.sarcophagusId,
        hostMarkerId = f.hostMarkerId, patientMarkerId = f.patientMarkerId,
        cloneId = params.cloneId or ""
    })
    rb.assert(afterCall.success and afterCall.result.success, "Medical status failed.")
    local after = afterCall.result
    rb.assert(after.host.distinctObjectCount == 1 and after.host.ownershipOccurrences + after.host.mapOccurrences == 1, "Host must have one actual object and one owner/map registration.")
    rb.assert(after.doctor.distinctObjectCount == 1 and after.doctor.ownershipOccurrences + after.doctor.mapOccurrences == 1, "Doctor must have one actual object and one owner/map registration.")
    rb.assert(after.patient.distinctObjectCount == 1 and after.patient.ownershipOccurrences + after.patient.mapOccurrences == 1, "Patient must have one actual object and one owner/map registration.")
    rb.assert(after.hostMarker.distinctObjectCount == 1 and after.hostMarker.ownershipOccurrences + after.hostMarker.mapOccurrences == 1, "Host inventory marker must have one actual object and one owner/map registration.")
    rb.assert(after.patientMarker.distinctObjectCount == 1 and after.patientMarker.ownershipOccurrences + after.patientMarker.mapOccurrences == 1, "Patient inventory marker must have one actual object and one owner/map registration.")
    rb.assert(after.hostMarker.stackCount == 7 and after.patientMarker.stackCount == 11, "Inventory marker quantity changed.")
    return { success = true, before = before, status = after }
end
if params.phase == "roundtrip" then
    local checkpoint = params.checkpoint
    rb.assert(checkpoint == "host" or checkpoint == "calibration" or checkpoint == "incubating" or checkpoint == "diagnosing" or checkpoint == "healing", "Choose a documented checkpoint.")
    if checkpoint == "host" then rb.assert(before.pod.hostId == f.hostId and before.pod.status == "Idle", "Expected admitted idle host.") end
    if checkpoint == "calibration" then rb.assert(before.pod.status == "CloningStarted" and before.pod.calibrationRemaining > 0, "Expected unfinished calibration.") end
    if checkpoint == "incubating" then rb.assert(before.pod.status == "Incubating" and before.pod.pendingCloneId ~= nil and before.pod.cycleFuelConsumed, "Expected a real staged, fueled clone.") end
    if checkpoint == "diagnosing" then rb.assert(before.sarcophagus.status == "DiagnosisStarted", "Expected diagnosis in progress.") end
    if checkpoint == "healing" then rb.assert(before.sarcophagus.status == "HealingStarted" and before.sarcophagus.treatmentCount > 0, "Expected healing with a diagnosed treatment queue.") end
    if checkpoint == "healing" and f.traitInputEligible then
        rb.assert(f.seededTraitDef ~= nil, "The live whitelist permits Lush, but the fixture did not apply its genuine trait input.")
        local seededTarget = false
        for i, target in ipairs(before.sarcophagus.treatmentTraitKeys) do
            if target.defName == f.seededTraitDef and target.degree == f.seededTraitDegree then seededTarget = true end
        end
        rb.assert(seededTarget, "Real diagnosis did not retain the seeded whitelisted trait as a removal target.")
    end
    local name = "Rimgate-medical-" .. checkpoint
    local saved = rb.call("rimworld/save_game", { saveName = name })
    rb.assert(saved.success and saved.result.success, "Medical checkpoint save failed.")
    local loaded = rb.call("rimworld/load_game_ready", { saveName = name, readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.success and loaded.result.success, "Medical checkpoint reload failed.")
    local afterCall = rb.call("rimgate/medical_status", {
        expectedConfigPath = params.configPath, expectedModRoot = params.modRoot,
        hostId = f.hostId, doctorId = f.doctorId, patientId = f.patientId,
        podId = f.podId, sarcophagusId = f.sarcophagusId,
        hostMarkerId = f.hostMarkerId, patientMarkerId = f.patientMarkerId,
        cloneId = params.cloneId or ""
    })
    rb.assert(afterCall.success and afterCall.result.success, "Medical status failed.")
    local after = afterCall.result
    rb.assert(after.host.distinctObjectCount == 1 and after.host.ownershipOccurrences + after.host.mapOccurrences == 1, "Host must have one actual object and one owner/map registration.")
    rb.assert(after.doctor.distinctObjectCount == 1 and after.doctor.ownershipOccurrences + after.doctor.mapOccurrences == 1, "Doctor must have one actual object and one owner/map registration.")
    rb.assert(after.patient.distinctObjectCount == 1 and after.patient.ownershipOccurrences + after.patient.mapOccurrences == 1, "Patient must have one actual object and one owner/map registration.")
    rb.assert(after.hostMarker.distinctObjectCount == 1 and after.hostMarker.ownershipOccurrences + after.hostMarker.mapOccurrences == 1, "Host inventory marker must have one actual object and one owner/map registration.")
    rb.assert(after.patientMarker.distinctObjectCount == 1 and after.patientMarker.ownershipOccurrences + after.patientMarker.mapOccurrences == 1, "Patient inventory marker must have one actual object and one owner/map registration.")
    rb.assert(after.hostMarker.stackCount == 7 and after.patientMarker.stackCount == 11, "Inventory marker quantity changed.")
    local elapsedTicks = after.tick - before.tick
    rb.assert(elapsedTicks >= 0, "Reload moved the observed game clock backwards.")
    rb.assert(after.pod.status == before.pod.status and after.pod.fuel == before.pod.fuel
        and after.pod.cycleFuelConsumed == before.pod.cycleFuelConsumed, "Reload changed clone phase/fuel.")
    if before.pod.status == "CloningStarted" then
        rb.assert(before.doctor.pawn.job == "Rimgate_CalibrateClonePodForPawn"
            and after.doctor.pawn.job == before.doctor.pawn.job
            and before.doctor.pawn.jobTargetA == f.podId and after.doctor.pawn.jobTargetA == f.podId,
            "Reload must retain the same doctor's real calibration job and pod target.")
        local maxSpeed = before.doctor.pawn.medicalOperationSpeed
        if after.doctor.pawn.medicalOperationSpeed > maxSpeed then maxSpeed = after.doctor.pawn.medicalOperationSpeed end
        local maxUlp = before.pod.calibrationWorkUlp
        if after.pod.calibrationWorkUlp > maxUlp then maxUlp = after.pod.calibrationWorkUlp end
        local spentWork = before.pod.calibrationRemainingExact - after.pod.calibrationRemainingExact
        -- Each float32 subtraction rounds by at most half an ULP. Exact-double
        -- snapshots avoid adding JSON's shortest-float decimal conversion error.
        -- Endpoint speeds bound this controlled fixture; this does not sample every tick.
        rb.assert(maxSpeed >= 0 and spentWork >= 0
            and spentWork <= elapsedTicks * (maxSpeed + maxUlp * 0.5),
            "Calibration must not reset or spend more than elapsed ticks at the sampled speed plus derived float32 rounding.")
    else
        rb.assert(after.pod.calibrationRemaining == before.pod.calibrationRemaining, "Reload changed inactive calibration work.")
    end
    if before.pod.status == "Incubating" then
        rb.assert(before.pod.powered and after.pod.powered
            and before.pod.incubationRemaining - after.pod.incubationRemaining == elapsedTicks,
            "Powered incubation must advance by exactly the observed game ticks.")
    else
        rb.assert(after.pod.incubationRemaining == before.pod.incubationRemaining, "Reload changed inactive incubation work.")
    end
    rb.assert(after.sarcophagus.status == before.sarcophagus.status
        and after.sarcophagus.healingTotal == before.sarcophagus.healingTotal
        and after.sarcophagus.treatmentCount == before.sarcophagus.treatmentCount
        and after.sarcophagus.treatmentTraitCount == before.sarcophagus.treatmentTraitCount, "Reload changed patient phase/total/treatment queues.")
    if before.sarcophagus.status == "DiagnosisStarted" then
        rb.assert(before.sarcophagus.diagnosisRemaining - after.sarcophagus.diagnosisRemaining == elapsedTicks,
            "Diagnosis must advance by exactly the observed game ticks.")
    else
        rb.assert(after.sarcophagus.diagnosisRemaining == before.sarcophagus.diagnosisRemaining, "Reload changed inactive diagnosis work.")
    end
    if before.sarcophagus.status == "HealingStarted" then
        rb.assert(before.sarcophagus.healingRemaining - after.sarcophagus.healingRemaining == elapsedTicks
            and after.sarcophagus.healingProgress - before.sarcophagus.healingProgress == elapsedTicks,
            "Healing remaining work and progress must advance by exactly the observed game ticks.")
    else
        rb.assert(after.sarcophagus.healingRemaining == before.sarcophagus.healingRemaining
            and after.sarcophagus.healingProgress == before.sarcophagus.healingProgress, "Reload changed inactive healing work.")
    end
    for i, id in ipairs(before.sarcophagus.treatmentIds) do
        for j, restoredId in ipairs(after.sarcophagus.treatmentIds) do
            if i == j then
                rb.assert(restoredId == id, "Reload lost exact treatment references/order.")
            end
        end
    end
    for i, target in ipairs(before.sarcophagus.treatmentTraitKeys) do
        rb.assert(target.patientOwnsReference, "Before-save trait target must be an actual patient TraitSet instance.")
        for j, restored in ipairs(after.sarcophagus.treatmentTraitKeys) do
            if i == j then
                rb.assert(restored.defName == target.defName and restored.degree == target.degree
                    and restored.patientOwnsReference, "Reload lost ordered trait keys or duplicated a patient trait instance.")
            end
        end
    end
    if before.pod.pendingCloneId ~= nil then
        rb.assert(after.pod.pendingCloneId == before.pod.pendingCloneId, "Reload replaced the pending clone.")
        rb.assert(after.clone.distinctObjectCount == 1 and after.clone.ownershipOccurrences + after.clone.mapOccurrences == 1, "Pending clone must have one actual object and one owner/map registration.")
    end
    return { success = true, before = before, status = after, elapsedTicks = elapsedTicks, save = saved.result.path,
        limits = "Inspect this checkpoint XML separately for exactly one deep host/patient/clone/marker definition. A passing roundtrip must also be followed by real cycle completion and cleanup." }
end
if params.phase == "assert-interruption" then
    local old = params.before
    if params.target == "sarcophagus" then
        rb.assert(old ~= nil and old.sarcophagus.patientId == f.patientId and old.sarcophagus.ownedCount == 1
            and old.patient.ownershipOccurrences == 1 and old.patient.spawned == false
            and ((old.sarcophagus.status == "DiagnosisStarted" and old.sarcophagus.diagnosisRemaining > 0)
                or (old.sarcophagus.status == "HealingStarted" and old.sarcophagus.healingRemaining > 0)),
            "Interruption requires a supplied occupied, unfinished diagnosis/healing before-state.")
        local injuryBefore = false
        local injuryAfter = false
        for i, h in ipairs(old.patient.pawn.hediffs) do
            if h.id == f.injuryId then injuryBefore = true end
        end
        for i, h in ipairs(before.patient.pawn.hediffs) do
            if h.id == f.injuryId then injuryAfter = true end
        end
        rb.assert(injuryBefore and injuryAfter, "The exact untreated fixture injury must remain across interruption.")
        rb.assert(before.sarcophagus.powered == false and before.sarcophagus.patientId == nil
            and before.sarcophagus.ownedCount == 0 and before.sarcophagus.status == "Idle"
            and before.patient.spawned and before.patient.ownershipOccurrences == 0, "Power loss must eject and reset the sarcophagus.")
    else
        rb.assert(params.target == "pod", "Choose sarcophagus or pod.")
        rb.assert(before.pod.powered == false and before.pod.pendingCloneId == old.pod.pendingCloneId
            and before.pod.cycleFuelConsumed and before.pod.fuel == old.pod.fuel, "Power loss must retain the pending clone and consumed-fuel marker.")
        rb.assert(before.clone.distinctObjectCount == 1 and before.clone.ownershipOccurrences + before.clone.mapOccurrences == 1, "Interrupted pending clone must have one actual object and one owner/map registration.")
    end
    return { success = true, status = before, limits = "This observes the result of a separately executed normal power/flick action. Unpowered incubation deliberately deteriorates after one second; it does not freeze indefinitely." }
end
if params.phase == "assert-resumed" then
    local old = params.before
    rb.assert(before.pod.powered and before.pod.pendingCloneId == old.pod.pendingCloneId
        and before.pod.fuel == old.pod.fuel and before.pod.cycleFuelConsumed
        and before.pod.incubationRemaining < old.pod.incubationRemaining, "Resuming the same clone must advance without charging biomass again.")
    rb.assert(before.clone.distinctObjectCount == 1 and before.clone.ownershipOccurrences + before.clone.mapOccurrences == 1, "Resumed clone must have one actual object and one owner/map registration.")
    return { success = true, status = before }
end
if params.phase == "assert-complete" then
    if params.target == "pod" then
        rb.assert(params.cloneId ~= nil and params.expectedFuel ~= nil, "Retain the real pending clone identity and expected single-cycle fuel balance.")
        rb.assert(before.pod.status == "Idle" and before.pod.pendingCount == 0 and before.pod.hostCount == 0
            and before.pod.cycleFuelConsumed == false and before.pod.fuel == params.expectedFuel, "Completed clone cycle must reset owners/state and consume biomass exactly once.")
        rb.assert(before.clone.distinctObjectCount == 1 and before.clone.ownershipOccurrences + before.clone.mapOccurrences == 1, "Completed clone must have one actual object and one owner/map registration.")
        rb.assert(before.clone.spawned and before.clone.pawn.dead == false and before.clone.pawn.cloneGeneration ~= nil,
            "The exact staged clone must emerge alive with generation tracking. Inspect configured major outcomes if this fails.")
    else
        rb.assert(params.target == "sarcophagus", "Choose pod or sarcophagus.")
        rb.assert(before.sarcophagus.status == "Idle" and before.sarcophagus.ownedCount == 0 and before.patient.spawned,
            "Successful treatment must eject the exact patient and reset its owner.")
        for i, h in ipairs(before.patient.pawn.hediffs) do
            rb.assert(h.id ~= f.injuryId, "The original diagnosed injury was not treated.")
        end
        rb.assert(before.sarcophagus.treatmentTraitCount == 0, "Completed treatment must clear pending trait targets.")
        if f.seededTraitDef ~= nil then
            for i, trait in ipairs(before.patient.pawn.traits) do
                rb.assert(trait.defName ~= f.seededTraitDef or trait.degree ~= f.seededTraitDegree,
                    "Normal treatment did not remove the genuine whitelisted fixture trait.")
            end
        end
    end
    return { success = true, status = before }
end
rb.assert(params.phase == "status", "Choose a documented medical phase.")
return { success = true, status = before }
