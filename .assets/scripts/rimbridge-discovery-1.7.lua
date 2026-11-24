-- Native 1.7 planetary discovery: prepare, decode, connect, visit (separate invocations).
-- Pass configPath, modRoot, evidenceDirectory and phase; later phases also take
-- prepare's fixture and, for connect, decode's discovered site object as site.
-- Uses only owned Rimgate-discovery-* saves and the isolated English profile.
local config = rb.call("rimworld/get_mod_configuration_status", {})
rb.assert(config.success and config.result.success, "Configuration query failed.")
rb.assert(config.result.configPath == params.configPath and config.result.configurationIssueCount == 0, "Use the expected healthy private profile.")
local deployment = false
for i, mod in ipairs(config.result.activeMods) do
    if mod.packageId == "mrdav30.rimgate" then
        deployment = mod.loadedInSession and mod.rootDir == params.modRoot
    end
end
rb.assert(deployment, "Expected private deployment is not loaded.")
if params.phase == "prepare" then
    local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-bridge-smoke", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.result.success, "Load the owned smoke save.")
    local assets = rb.call("rimgate/asset_report", {})
    rb.assert(assets.result.success and assets.result.assetFormat == "WaylithPack", "Use the validated native pack.")
    local prepared = rb.call("rimgate/prepare_discovery_fixture", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot })
    rb.assert(prepared.result.success and prepared.result.prepared.glyph.stackCount == 2, "Fixture preparation failed.")
    return { success = true, fixture = prepared.result }
end
local f = params.fixture
rb.assert(params.phase == "decode" or params.phase == "connect" or params.phase == "visit", "Choose a documented phase.")
if params.phase == "visit" then
    local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-discovery-active", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.result.success, "Load the owned discovered active link.")
    local ticks = rb.call("rimworld/step_game_ticks", { ticks = 180, timeoutMs = 30000 })
    rb.assert(ticks.result.success, "Mature the reloaded gate before entry.")
    local selected = rb.call("rimworld/select_pawn", { pawnId = f.pawnId })
    rb.assert(selected.result.success, "Traveler selection failed.")
    local menu = rb.call("rimworld/right_click_cell", { x = 110, z = 130 })
    rb.assert(menu.result.success, "Entry context menu failed.")
    local enter = rb.call("rimworld/execute_context_menu_option", { label = "Enter Dwarfgate" })
    rb.assert(enter.result.success, "Normal entry job failed.")
    local arrived = false
    for attempt = 1, 60 do
        if arrived == false then
            ticks = rb.call("rimworld/step_game_ticks", { ticks = 5, timeoutMs = 10000 })
            rb.assert(ticks.result.success, "Quest entry stepping failed.")
            local status = rb.call("rimgate/discovery_status", { pawnId = f.pawnId, glyphId = f.glyphId })
            if status.result.pawn then
                for i, site in ipairs(status.result.sites) do
                    if site.id == params.site.id then
                        arrived = site.visited and status.result.pawn.mapId == site.mapId and status.result.pawn.dead == false
                    end
                end
            end
        end
    end
    rb.assert(arrived, "A living traveler did not reach the discovered site.")
    local saved = rb.call("rimworld/save_game", { saveName = "Rimgate-discovery-visited" })
    rb.assert(saved.result.success, "Visited save failed.")
    loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-discovery-visited", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.result.success, "Visited reload failed.")
    local status = rb.call("rimgate/discovery_status", { pawnId = f.pawnId, glyphId = f.glyphId })
    local retained = false
    for i, site in ipairs(status.result.sites) do
        if site.id == params.site.id then
            retained = site.visited and site.questId == params.site.questId and status.result.pawn.mapId == site.mapId
        end
    end
    rb.assert(retained and status.result.currentWorldOwnsAddressCache and status.result.activeQuestSiteCount == f.prepared.activeQuestSiteCount + 1, "Visited reload lost site ownership/state.")
    return { success = true, status = status.result, save = saved.result.path }
end
if params.phase == "decode" then
    local selected = rb.call("rimworld/select_pawn", { pawnId = f.pawnId })
    rb.assert(selected.result.success, "Decoder selection failed.")
    local menu = rb.call("rimworld/right_click_cell", { x = f.prepared.glyph.x, z = f.prepared.glyph.z })
    rb.assert(menu.result.success, "Parchment context menu failed.")
    local decode = rb.call("rimworld/execute_context_menu_option", { label = "Decode symbols" })
    rb.assert(decode.result.success, "Normal decode action failed.")
    local ticks = rb.call("rimworld/step_game_ticks", { ticks = 100, timeoutMs = 30000 })
    rb.assert(ticks.result.success, "Decode stepping failed.")
    local working = rb.call("rimgate/discovery_status", { pawnId = f.pawnId, glyphId = f.glyphId })
    rb.assert(working.result.pawn.job == "Rimgate_DecodeGlyphs" and working.result.glyph.stackCount == 2, "Did not observe an unfinished real decode job.")
    local saved = rb.call("rimworld/save_game", { saveName = "Rimgate-discovery-working" })
    rb.assert(saved.result.success, "Working decode save failed.")
    local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-discovery-working", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.result.success, "Working decode reload failed.")
    ticks = rb.call("rimworld/step_game_ticks", { ticks = f.useDuration + 100, timeoutMs = 30000 })
    rb.assert(ticks.result.success, "Resumed decode stepping failed.")
    local status = rb.call("rimgate/discovery_status", { pawnId = f.pawnId, glyphId = f.glyphId })
    rb.assert(status.result.glyph.stackCount == 1, "Successful decoding must consume exactly one parchment.")
    local site = nil
    local newSites = 0
    for i, candidate in ipairs(status.result.sites) do
        local existing = false
        for j, old in ipairs(f.initial.sites) do
            if old.id == candidate.id then existing = true end
        end
        if existing == false then
            site = candidate
            newSites = newSites + 1
        end
    end
    rb.assert(newSites == 1 and site.hasMap == false, "Decoding must create one lazy quest site.")
    local newQuests = 0
    for i, quest in ipairs(status.result.quests) do
        local existing = false
        for j, old in ipairs(f.initial.quests) do
            if old.id == quest.id then existing = true end
        end
        if existing == false then
            rb.assert(quest.root == "Rimgate_GateQuestScript_Planet" and quest.state == "Ongoing" and quest.id == site.questId, "Incorrect generated quest/site binding.")
            newQuests = newQuests + 1
        end
    end
    rb.assert(newQuests == 1 and status.result.currentWorldOwnsAddressCache, "Quest generation/address owner is incorrect.")
    local addresses = 0
    for i, address in ipairs(status.result.addresses) do
        if address.tile == site.tile then addresses = addresses + 1 end
    end
    rb.assert(addresses == 1, "Discovered address was not registered exactly once.")
    saved = rb.call("rimworld/save_game", { saveName = "Rimgate-discovery-decoded" })
    rb.assert(saved.result.success, "Decoded save failed.")
    loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-discovery-decoded", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.result.success, "Decoded reload failed.")
    status = rb.call("rimgate/discovery_status", { pawnId = f.pawnId, glyphId = f.glyphId })
    local retained = false
    for i, current in ipairs(status.result.sites) do
        if current.id == site.id then
            retained = current.questId == site.questId and current.tile == site.tile and current.hasMap == false
        end
    end
    rb.assert(retained and status.result.glyph.stackCount == 1, "Reload lost discovery state.")
    local questRetained = false
    for i, quest in ipairs(status.result.quests) do
        if quest.id == site.questId then
            questRetained = quest.root == "Rimgate_GateQuestScript_Planet" and quest.state == "Ongoing"
        end
    end
    addresses = 0
    for i, address in ipairs(status.result.addresses) do
        if address.tile == site.tile then addresses = addresses + 1 end
    end
    rb.assert(questRetained and addresses == 1 and status.result.currentWorldOwnsAddressCache, "Reload lost the actual quest or current address book.")
    return { success = true, site = site, status = status.result, save = saved.result.path }
end
local site = params.site
local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-discovery-decoded", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
rb.assert(loaded.result.success, "Reload the owned decoded checkpoint before dialing.")
local generator = rb.call("rimworld/spawn_thing", { defName = "VanometricPowerCell", x = 105, z = 130 })
rb.assert(generator.result.success, "Private power source spawn failed.")
local selected = rb.call("rimworld/click_cell", { x = 105, z = 130, button = "left" })
rb.assert(selected.result.success, "Power source selection failed.")
local gizmos = rb.call("rimworld/list_selected_gizmos", {})
for i, gizmo in ipairs(gizmos.result.gizmos) do
    if gizmo.label == "Claim" then
        local claim = rb.call("rimworld/execute_gizmo", { gizmoId = gizmo.id })
        rb.assert(claim.result.success, "Power source claim failed.")
    end
end
local ticks = rb.call("rimworld/step_game_ticks", { ticks = 750, timeoutMs = 30000 })
rb.assert(ticks.result.success, "Power initialization failed.")
selected = rb.call("rimworld/select_pawn", { pawnId = f.pawnId })
rb.assert(selected.result.success, "Dialer selection failed.")
local menu = rb.call("rimworld/right_click_cell", { x = 110, z = 134 })
rb.assert(menu.result.success, "DHD context menu failed.")
local dial = rb.call("rimworld/execute_context_menu_option", { label = site.designation })
rb.assert(dial.result.success, "Discovered-address dial job failed.")
ticks = rb.call("rimworld/step_game_ticks", { ticks = 650, timeoutMs = 30000 })
if ticks.result.success == false then
    rb.assert(ticks.result.message == "RimWorld is busy with a long event.", "Unexpected dial stepping failure.")
end
local ready = rb.call("rimbridge/wait_for_long_event_idle", { timeoutMs = 120000 })
rb.assert(ready.result.success, "Quest map generation did not finish.")
ready = rb.call("rimbridge/wait_for_game_loaded", { readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
rb.assert(ready.result.success, "Quest map was not ready.")
local remaining = 180
for attempt = 1, 8 do
    if remaining > 0 then
        ready = rb.call("rimbridge/wait_for_long_event_idle", { timeoutMs = 120000 })
        rb.assert(ready.result.success, "Deferred quest map event did not finish.")
        ticks = rb.call("rimworld/step_game_ticks", { ticks = remaining, timeoutMs = 30000 })
        if ticks.result.success == false then
            rb.assert(ticks.result.message == "RimWorld is busy with a long event.", "Unexpected maturation stepping failure.")
        end
        remaining = ticks.result.remainingTicks
    end
end
rb.assert(remaining == 0, "Deferred map events exhausted the bounded maturation attempts.")
local transit = rb.call("rimgate/transit_status", { sourceGateId = f.sourceGateId, destinationParentId = site.id, pawnId = f.pawnId, itemId = "" })
rb.assert(transit.result.ready and transit.result.reciprocal and transit.result.source.IsActive and transit.result.destination.IsReceivingGate, "Discovered quest did not produce a connected receiving gate.")
local status = rb.call("rimgate/discovery_status", { pawnId = f.pawnId, glyphId = f.glyphId })
rb.assert(status.result.activeQuestSiteCount == f.prepared.activeQuestSiteCount + 1, "Generated quest map was not counted exactly once.")
local saved = rb.call("rimworld/save_game", { saveName = "Rimgate-discovery-active" })
rb.assert(saved.result.success, "Discovered active link save failed.")
local camera = rb.call("rimworld/jump_camera_to_cell", { x = 110, z = 130 })
rb.assert(camera.result.success, "Active discovery camera failed.")
local shot = rb.call("rimworld/take_screenshot", { fileName = "rimgate-discovery-active", outputDirectory = params.evidenceDirectory, includeTargets = false })
rb.assert(shot.result.success, "Discovered active link capture failed.")
return { success = true, status = status.result, transit = transit.result, save = saved.result.path, screenshot = shot.result.path }
