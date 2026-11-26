-- Real orbital travel and site cleanup. Compile through the installed bridge.
-- prepare takes orbitalFixture + site from the real orbital discovery scenario.
-- Later phases take prepare's fixture. Phases: wear-menu, wear (gear=suit|helmet,
-- wearLabel from the actual menu), dial, enter, observe, close-home, return-dial,
-- return, abandon-menu, abandon (fresh abandonTargetId from the actual world UI).
-- A lazy abandon may branch directly after prepare. No quest completion/reward,
-- vacuum flags, native stats or worn-apparel outcomes are inserted.
-- observe accepts exposureTicks=20..600 (default600); report actual elapsed ticks.
-- Bound invocations to 60 seconds. Host MUST pause in finally on failure/timeout.
local config = rb.call("rimworld/get_mod_configuration_status", {})
rb.assert(config.success and config.result.success and config.result.configPath == params.configPath and config.result.configurationIssueCount == 0, "Use the healthy expected private profile.")
local deployment = false
for i, mod in ipairs(config.result.activeMods) do
    if mod.packageId == "mrdav30.rimgate" then deployment = mod.loadedInSession and mod.rootDir == params.modRoot end
end
rb.assert(deployment, "Expected private deployment is not loaded.")
if params.phase == "prepare" then
    local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-orbital-equipped", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.success and loaded.result.success, "Load the actual decoded/equipped orbital checkpoint.")
    local o = params.orbitalFixture
    local prepared = rb.call("rimgate/prepare_orbital_travel", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = o.sourceGateId, destinationParentId = params.site.id, pawnId = o.pawnId })
    rb.assert(prepared.success and prepared.result.success, "Stage only ordinary protection inputs.")
    return { success = true, fixture = prepared.result }
end
local f = params.fixture
local state = rb.call("rimgate/world_edge_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, suitId = f.suitId, helmetId = f.helmetId })
rb.assert(state.success and state.result.success, "Read the actual world/traveler state.")
if params.phase ~= "abandon-menu" and params.phase ~= "abandon" then
    rb.assert(state.result.pawn and state.result.pawn.Dead == false and state.result.pawn.Downed == false, "Keep the actual traveler alive and conscious for travel.")
end
if params.phase == "wear-menu" or params.phase == "wear" then
    rb.assert(params.gear == "suit" or params.gear == "helmet", "Choose the actual staged suit or helmet.")
    local x = f.suitX
    local z = f.suitZ
    if params.gear == "helmet" then x = f.helmetX; z = f.helmetZ end
    local selected = rb.call("rimworld/select_pawn", { pawnId = f.pawnId })
    rb.assert(selected.success and selected.result.success, "Select the actual wearer.")
    local menu = rb.call("rimworld/right_click_cell", { x = x, z = z })
    rb.assert(menu.success and menu.result.success and menu.result.provider == "ui_event", "Open the real apparel menu.")
    if params.phase == "wear-menu" then return { success = true, menu = menu.result } end
    rb.assert(params.wearLabel, "Supply the actual enabled Wear label from wear-menu.")
    local ordered = rb.call("rimworld/execute_context_menu_option", { label = params.wearLabel })
    rb.assert(ordered.success and ordered.result.success, "Order the real Wear job.")
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Advance ordinary apparel work.")
    local path = "result.pawn.suitWorn"
    if params.gear == "helmet" then path = "result.pawn.helmetWorn" end
    local worn = rb.poll("rimgate/world_edge_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, suitId = f.suitId, helmetId = f.helmetId }, { timeoutMs = 30000, pollIntervalMs = 50, condition = { path = path, equals = true } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success and worn.success and worn.result.success, "Pause after actual equipment ownership changes.")
elseif params.phase == "dial" or params.phase == "return-dial" then
    rb.assert(state.result.pawn.suitWorn and state.result.pawn.helmetWorn and state.result.pawn.vacuumResistance >= 1 and state.result.pawn.harmedByVacuum == false, "Real worn protection is required before orbital operations.")
    local control = state.result.source
    local label = "Dial " .. state.result.site.designation .. " (" .. state.result.site.label .. ")"
    local path = "result.source.IsActive"
    if params.phase == "return-dial" then
        control = state.result.receiver
        rb.assert(state.result.source.IsActive == false, "Close the previous link normally before dialing home.")
        label = params.returnDialLabel
        path = "result.receiver.IsActive"
        rb.assert(label, "Supply the actual enabled home-address label from the orbital DHD menu.")
    end
    rb.assert(control and control.dhd and control.IsActive == false and state.result.pawn.mapId == control.mapId, "Use the actual inactive controller on the traveler's map.")
    local view = rb.call("rimgate/view_transit_map", { mapId = control.mapId })
    rb.assert(view.success and view.result.success, "View the actual dialing map.")
    local selected = rb.call("rimworld/select_pawn", { pawnId = f.pawnId })
    rb.assert(selected.success and selected.result.success, "Select the actual dialer.")
    local menu = rb.call("rimworld/right_click_cell", { x = control.dhd.x, z = control.dhd.z })
    rb.assert(menu.success and menu.result.success and menu.result.provider == "ui_event", "Open the real DHD address menu.")
    local ordered = rb.call("rimworld/execute_context_menu_option", { label = label })
    rb.assert(ordered.success and ordered.result.success, "Order the normal DHD dialing job.")
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Allow real dialing and native map generation.")
    local connected = rb.poll("rimgate/world_edge_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, suitId = f.suitId, helmetId = f.helmetId }, { timeoutMs = 30000, pollIntervalMs = 50, condition = { path = path, equals = true } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success and connected.success and connected.result.success, "Pause after the real link opens.")
    rb.assert(connected.result.site.layer == "Orbit" and connected.result.site.HasMap and connected.result.source.IsActive and connected.result.receiver.IsActive, "The real discovered orbital site must generate a connected map.")
elseif params.phase == "enter" or params.phase == "return" then
    rb.assert(state.result.pawn.suitWorn and state.result.pawn.helmetWorn and state.result.pawn.vacuumResistance >= 1, "Retain actual vacuum gear.")
    local gate = state.result.source
    local targetMap = state.result.receiver.mapId
    local maturePath = "result.source.TicksSinceOpened"
    if params.phase == "return" then gate = state.result.receiver; targetMap = f.sourceMapId; maturePath = "result.receiver.TicksSinceOpened" end
    rb.assert(gate.IsActive and gate.IsReceivingGate == false and gate.irisClosed == false and state.result.pawn.mapId == gate.mapId, "Use the actual outgoing side before entry.")
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Allow the actual opening vortex to mature.")
    local mature = rb.poll("rimgate/world_edge_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, suitId = f.suitId, helmetId = f.helmetId }, { timeoutMs = 15000, pollIntervalMs = 50, condition = { path = maturePath, greaterThanOrEqual = 150 } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success and mature.success and mature.result.success, "Pause after the mature opening.")
    local view = rb.call("rimgate/view_transit_map", { mapId = gate.mapId })
    rb.assert(view.success and view.result.success, "View the actual entry gate.")
    local draft = rb.call("rimworld/set_draft", { pawnId = f.pawnId, drafted = true })
    rb.assert(draft.success and draft.result.success, "Hold the actual traveler after arrival through ordinary drafting.")
    local selected = rb.call("rimworld/select_pawn", { pawnId = f.pawnId })
    rb.assert(selected.success and selected.result.success, "Select the actual traveler.")
    local menu = rb.call("rimworld/right_click_cell", { x = gate.x, z = gate.z })
    rb.assert(menu.success and menu.result.success and menu.result.provider == "ui_event", "Open the actual entry menu.")
    local ordered = rb.call("rimworld/execute_context_menu_option", { label = "Enter Dwarfgate" })
    rb.assert(ordered.success and ordered.result.success, "Order the real entry job.")
    speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Advance real protected transit.")
    local arrived = rb.poll("rimgate/world_edge_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, suitId = f.suitId, helmetId = f.helmetId }, { timeoutMs = 30000, pollIntervalMs = 50, condition = { all = { { path = "result.pawn.Spawned", equals = true }, { path = "result.pawn.mapId", equals = targetMap }, { path = "result.pawn.Dead", equals = false }, { path = "result.pawn.Downed", equals = false } } } })
    paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success and arrived.success and arrived.result.success, "Pause after actual living protected arrival.")
elseif params.phase == "close-home" then
    rb.assert(state.result.source.IsActive and state.result.source.IsReceivingGate == false, "Use the actual outgoing home DHD.")
    local view = rb.call("rimgate/view_transit_map", { mapId = f.sourceMapId })
    rb.assert(view.success and view.result.success, "View the source closer.")
    local selected = rb.call("rimworld/click_cell", { x = state.result.source.dhd.x, z = state.result.source.dhd.z, button = "left" })
    rb.assert(selected.success and selected.result.success, "Select the real DHD.")
    local commands = rb.call("rimworld/list_selected_gizmos", {})
    local requested = false
    for i, command in ipairs(commands.result.gizmos) do
        if command.label == "Close Dwarfgate" then
            local clicked = rb.call("rimworld/execute_gizmo", { gizmoId = command.id })
            rb.assert(clicked.success and clicked.result.success, "Request real home closure.")
            requested = true
        end
    end
    rb.assert(requested, "The home closure command was absent.")
    selected = rb.call("rimworld/select_pawn", { pawnId = f.closerId })
    rb.assert(selected.success and selected.result.success, "Select the actual separate closer.")
    local menu = rb.call("rimworld/right_click_cell", { x = state.result.source.dhd.x, z = state.result.source.dhd.z })
    rb.assert(menu.success and menu.result.success, "Open the real DHD work menu.")
    local ordered = rb.call("rimworld/execute_context_menu_option", { label = "Prioritize closing" })
    rb.assert(ordered.success and ordered.result.success, "Start the normal closer job.")
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Advance ordinary closure.")
    local closed = rb.poll("rimgate/world_edge_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, suitId = f.suitId, helmetId = f.helmetId }, { timeoutMs = 30000, pollIntervalMs = 50, condition = { path = "result.source.IsActive", equals = false } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success and closed.success and closed.result.success and closed.result.receiver.IsActive == false, "Close both real ends before the return dial.")
elseif params.phase == "observe" then
    rb.assert(params.exposureTicks == nil or (params.exposureTicks >= 20 and params.exposureTicks <= 600), "Bound the requested vacuum sample to 20..600 ticks.")
    rb.assert(state.result.pawn.mapId == state.result.receiver.mapId and state.result.site.WasEverVisited and state.result.pawn.suitWorn and state.result.pawn.helmetWorn and state.result.pawn.vacuumResistance >= 1 and state.result.pawn.harmedByVacuum == false, "Observe the real protected orbital traveler.")
    rb.assert(state.result.pawn.roomVacuum ~= nil and state.result.pawn.roomVacuum > 0, "Use a genuinely unpressurized walkable location through ordinary movement before vacuum acceptance.")
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Observe ordinary protected vacuum ticks.")
    local elapsed = rb.poll("rimgate/world_edge_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, suitId = f.suitId, helmetId = f.helmetId }, { timeoutMs = 15000, pollIntervalMs = 50, condition = { path = "result.tick", greaterThanOrEqual = state.result.tick + (params.exposureTicks or 600) } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success and elapsed.success and elapsed.result.success, "Pause after actual exposure observation.")
    local saved = rb.call("rimworld/save_game", { saveName = "Rimgate-orbital-visited" })
    rb.assert(saved.success and saved.result.success and saved.result.exists, "Save the actual protected orbital visit.")
    local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-orbital-visited", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.success and loaded.result.success, "Reload the visited orbital checkpoint.")
elseif params.phase == "abandon-menu" then
    local selected = rb.call("rimgate/select_quest_site", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, destinationParentId = f.destinationParentId })
    rb.assert(selected.success and selected.result.success, "Select the actual quest site in the world UI.")
    local layout = rb.call("rimworld/get_ui_layout", {})
    rb.assert(layout.success and layout.result.success, "Read the actual world-site controls.")
    return { success = true, layout = layout.result, before = state.result, instruction = "If no blocking pawn remains, pass the fresh real Abandon exploration target to abandon." }
elseif params.phase == "abandon" then
    rb.assert(state.result.site and state.result.site.blockingPawns == false and params.abandonTargetId, "Remove all actors through ordinary travel and supply the fresh actual Abandon target.")
    local clicked = rb.call("rimworld/click_ui_target", { targetId = params.abandonTargetId })
    rb.assert(clicked.success and clicked.result.success, "Execute the site's actual Abandon control.")
    local saved = rb.call("rimworld/save_game", { saveName = "Rimgate-orbital-abandoned" })
    rb.assert(saved.success and saved.result.success, "Save real site cleanup.")
    local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-orbital-abandoned", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.success and loaded.result.success, "Reload actual site cleanup.")
else
    rb.assert(false, "Choose a documented world-edge phase.")
end
local after = rb.call("rimgate/world_edge_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, sourceGateId = f.sourceGateId, destinationParentId = f.destinationParentId, pawnId = f.pawnId, suitId = f.suitId, helmetId = f.helmetId })
rb.assert(after.success and after.result.success, "Read the actual resulting world/traveler state.")
if params.phase ~= "abandon" then
    rb.assert(after.result.pawn and after.result.pawn.Dead == false and after.result.pawn.Downed == false, "Preserve the real living traveler during travel.")
end
if params.phase == "observe" then
    rb.assert(after.result.site.WasEverVisited and after.result.pawn.mapId == after.result.receiver.mapId and after.result.pawn.suitWorn and after.result.pawn.helmetWorn and after.result.pawn.vacuumResistance >= 1 and after.result.pawn.harmedByVacuum == false and after.result.pawn.roomVacuum > 0, "Reload must preserve the actual visit, worn protection and unpressurized location.")
    for i, health in ipairs(after.result.pawn.health) do
        if health.defName == "VacuumExposure" or health.defName == "VacuumBurn" then rb.assert(health.severity == 0, "Native protection must avoid vacuum injury during the sampled interval.") end
    end
elseif params.phase == "abandon" then
    rb.assert(after.result.site == nil and after.result.activeQuestSites == f.initialActiveQuestSites, "Actual abandonment must remove the site/map and restore the active-site baseline.")
    local questFailed = false
    for i, quest in ipairs(after.result.quests) do
        if quest.id == f.questId then questFailed = quest.state == "EndedFailed" end
    end
    rb.assert(questFailed, "The real abandoned quest must remain failed after reload.")
    for i, address in ipairs(after.result.addresses) do
        rb.assert(address.tile ~= f.siteTile or address.layer ~= f.siteLayer, "Abandonment retained the discovered address.")
    end
    if state.result.site.WasEverVisited then rb.assert(after.result.LastIgnoredTick == f.initialIgnoredTick, "A visited site must not be recorded as ignored.")
    else rb.assert(after.result.LastIgnoredTick > f.initialIgnoredTick, "An unvisited abandoned site must record its actual ignored event.") end
end
return { success = true, before = state.result, after = after.result, limits = "Real protection, dialing, entry/return and world-site UI only. Sampled vacuum survival is separate from combat safety and indefinite oxygen endurance; observed site loot is not a forced quest reward." }
