-- Orbital prerequisites/discovery: prepare, decode, equipment (separate invocations).
-- Pass configPath, modRoot, phase; later phases need prepare's fixture.
-- Equipment also needs decode's site. No dialing, arrival or vacuum bypass.
-- Bulk waits use normal Ultrafast simulation; elapsed ticks can overshoot.
-- Caller cleanup must set time speed Paused after any script failure.
local config = rb.call("rimworld/get_mod_configuration_status", {})
rb.assert(config.success and config.result.success and config.result.configPath == params.configPath and config.result.configurationIssueCount == 0, "Use the healthy expected private profile.")
local deployment = false
for i, mod in ipairs(config.result.activeMods) do
    if mod.packageId == "mrdav30.rimgate" then deployment = mod.loadedInSession and mod.rootDir == params.modRoot end
end
rb.assert(deployment, "Expected private deployment is not loaded.")
rb.assert(params.phase == "prepare" or params.phase == "decode" or params.phase == "equipment", "Choose prepare, decode or equipment.")
if params.phase == "prepare" then
    local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-bridge-smoke", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.success and loaded.result.success, "Load the owned smoke save.")
    local assets = rb.call("rimgate/asset_report", {})
    rb.assert(assets.success and assets.result.success and assets.result.assetFormat == "WaylithPack", "Use the validated native pack.")
    local prepared = rb.call("rimgate/prepare_orbital_fixture", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot })
    rb.assert(prepared.success and prepared.result.success, "Orbital input preparation failed.")
    local status = rb.call("rimgate/discovery_status", { pawnId = prepared.result.pawnId, glyphId = prepared.result.glyphId })
    rb.assert(status.success and status.result.success and status.result.glyph.def == "Rimgate_GlyphParchment_Orbit" and status.result.glyph.stackCount == 2 and status.result.glyphResearch and status.result.modificationResearch, "Orbital prerequisites were not prepared.")
    return { success = true, fixture = prepared.result, status = status.result }
end
local f = params.fixture
if params.phase == "decode" then
    local selected = rb.call("rimworld/select_pawn", { pawnId = f.pawnId })
    rb.assert(selected.success and selected.result.success, "Select the actual decoder.")
    local menu = rb.call("rimworld/right_click_cell", { x = 114, z = 132 })
    rb.assert(menu.success and menu.result.success and menu.result.provider == "ui_event", "Open the real orbital parchment menu.")
    local decode = rb.call("rimworld/execute_context_menu_option", { label = f.decodeLabel })
    rb.assert(decode.success and decode.result.success, "Start the real orbital decoding job.")
    local ticks = rb.call("rimworld/step_game_ticks", { ticks = 100, timeoutMs = 30000 })
    rb.assert(ticks.success and ticks.result.success, "Approach/decode stepping failed.")
    local status = rb.call("rimgate/discovery_status", { pawnId = f.pawnId, glyphId = f.glyphId })
    rb.assert(status.success and status.result.success and status.result.pawn.job == "Rimgate_DecodeGlyphs" and status.result.glyph.stackCount == 2, "Observe the unfinished real decode job before saving.")
    local saved = rb.call("rimworld/save_game", { saveName = "Rimgate-orbital-working" })
    rb.assert(saved.success and saved.result.success, "Save the in-progress decode.")
    local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-orbital-working", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.success and loaded.result.success, "Reload the in-progress decode.")
    local start = rb.call("rimgate/orbital_equipment_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot })
    local running = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    ticks = rb.poll("rimgate/orbital_equipment_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot }, { timeoutMs = 15000, pollIntervalMs = 50,
        condition = { path = "result.tick", greaterThanOrEqual = start.result.tick + f.useDuration + 100 } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(running.result.success and paused.result.success, "Resume and pause normal simulation.")
    rb.assert(ticks.success and ticks.result.success, "Resume actual decoding.")
    status = rb.call("rimgate/discovery_status", { pawnId = f.pawnId, glyphId = f.glyphId })
    rb.assert(status.success and status.result.success and status.result.glyph.stackCount == 1, "Consume exactly one orbital parchment.")
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
    rb.assert(newSites == 1 and site.layer == "Orbit" and site.hasMap == false, "Generate one lazy orbital quest site through decoding.")
    local newQuests = 0
    for i, quest in ipairs(status.result.quests) do
        local existing = false
        for j, old in ipairs(f.initial.quests) do
            if old.id == quest.id then existing = true end
        end
        if existing == false then
            rb.assert(quest.id == site.questId and quest.root == "Rimgate_GateQuestScript_Orbit" and quest.state == "Ongoing", "Bind the generated orbital quest to its site.")
            newQuests = newQuests + 1
        end
    end
    local addresses = 0
    for i, address in ipairs(status.result.addresses) do
        if address.tile == site.tile and address.layer == site.layer then addresses = addresses + 1 end
    end
    rb.assert(newQuests == 1 and addresses == 1 and status.result.currentWorldOwnsAddressCache, "Register the exact orbital address once in the current world.")
    saved = rb.call("rimworld/save_game", { saveName = "Rimgate-orbital-decoded" })
    rb.assert(saved.success and saved.result.success, "Save the orbital discovery.")
    loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-orbital-decoded", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.success and loaded.result.success, "Reload the orbital discovery.")
    status = rb.call("rimgate/discovery_status", { pawnId = f.pawnId, glyphId = f.glyphId })
    rb.assert(status.success and status.result.success and status.result.glyph.stackCount == 1 and status.result.currentWorldOwnsAddressCache, "Retain the consumed parchment and current world owner.")
    local retained = false
    for i, current in ipairs(status.result.sites) do
        if current.id == site.id then retained = current.questId == site.questId and current.tile == site.tile and current.layer == "Orbit" and current.hasMap == false and current.visited == false end
    end
    local questRetained = false
    for i, quest in ipairs(status.result.quests) do
        if quest.id == site.questId then questRetained = quest.root == "Rimgate_GateQuestScript_Orbit" and quest.state == "Ongoing" end
    end
    addresses = 0
    for i, address in ipairs(status.result.addresses) do
        if address.tile == site.tile and address.layer == site.layer then addresses = addresses + 1 end
    end
    rb.assert(retained and questRetained and addresses == 1, "Persist the real orbital quest/site/address without generating its map.")
    return { success = true, site = site, status = status.result, save = saved.result.path }
end
local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-orbital-decoded", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
rb.assert(loaded.success and loaded.result.success, "Load the owned decoded orbital checkpoint.")
local before = rb.call("rimgate/orbital_equipment_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot })
rb.assert(before.success and before.result.success and before.result.modificationEquipmentActive == false and before.result.dhdPowered, "Observe the missing-equipment negative control.")
local selected = rb.call("rimworld/select_pawn", { pawnId = f.pawnId })
rb.assert(selected.success and selected.result.success, "Select the actual dialer.")
local menu = rb.call("rimworld/right_click_cell", { x = 110, z = 134 })
rb.assert(menu.success and menu.result.success and menu.result.provider == "ui_event", "Open the actual DHD menu without equipment.")
local disabled = false
for i, option in ipairs(menu.result.options) do
    if option.label == f.dialPrefix .. " " .. params.site.designation .. f.blockedSuffix then disabled = option.disabled end
end
rb.assert(disabled, "The orbital destination must be disabled specifically for missing mod equipment.")
local equipment = rb.call("rimgate/stage_orbital_equipment", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot })
rb.assert(equipment.success and equipment.result.success and equipment.result.status.modificationEquipmentActive == false, "Stage ordinary equipment without setting the production flag.")
local start = rb.call("rimgate/orbital_equipment_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot })
local running = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
local ticks = rb.poll("rimgate/orbital_equipment_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot }, { timeoutMs = 15000, pollIntervalMs = 50,
    condition = { path = "result.tick", greaterThanOrEqual = start.result.tick + 750 } })
local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
rb.assert(running.result.success and paused.result.success, "Resume and pause normal simulation.")
rb.assert(ticks.success and ticks.result.success, "Allow normal power/facility and DHD rare ticks.")
local after = rb.call("rimgate/orbital_equipment_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot })
rb.assert(after.success and after.result.success and after.result.modificationEquipmentActive and after.result.dhdPowered, "Production equipment activation failed.")
local poweredLinked = false
for i, current in ipairs(after.result.equipment) do
    if current.id == equipment.result.equipmentId then poweredLinked = current.powered and current.linked end
end
rb.assert(poweredLinked, "The actual staged equipment must be powered and linked to the DHD.")
selected = rb.call("rimworld/select_pawn", { pawnId = f.pawnId })
rb.assert(selected.success and selected.result.success, "Reselect the actual dialer.")
menu = rb.call("rimworld/right_click_cell", { x = 110, z = 134 })
rb.assert(menu.success and menu.result.success and menu.result.provider == "ui_event", "Open the actual DHD menu with powered equipment.")
local enabled = false
for i, option in ipairs(menu.result.options) do
    if option.label == f.dialPrefix .. " " .. params.site.designation .. " (" .. params.site.label .. ")" then enabled = option.disabled == false end
end
rb.assert(enabled, "Powered linked equipment must enable the same orbital destination.")
local saved = rb.call("rimworld/save_game", { saveName = "Rimgate-orbital-equipped" })
rb.assert(saved.success and saved.result.success, "Save the real equipment prerequisites.")
loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-orbital-equipped", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
rb.assert(loaded.success and loaded.result.success, "Reload the equipped checkpoint.")
start = rb.call("rimgate/orbital_equipment_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot })
running = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
ticks = rb.poll("rimgate/orbital_equipment_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot }, { timeoutMs = 15000, pollIntervalMs = 50,
    condition = { path = "result.tick", greaterThanOrEqual = start.result.tick + 500 } })
paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
rb.assert(running.result.success and paused.result.success, "Resume and pause normal simulation.")
rb.assert(ticks.success and ticks.result.success, "Refresh ordinary equipment state after reload.")
after = rb.call("rimgate/orbital_equipment_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot })
rb.assert(after.success and after.result.success and after.result.modificationEquipmentActive, "Retain working equipment after reload.")
poweredLinked = false
for i, current in ipairs(after.result.equipment) do
    if current.id == equipment.result.equipmentId then poweredLinked = current.powered and current.linked end
end
rb.assert(poweredLinked, "Reload must retain the exact powered linked equipment.")
local status = rb.call("rimgate/discovery_status", { pawnId = f.pawnId, glyphId = f.glyphId })
rb.assert(status.success and status.result.success and status.result.currentWorldOwnsAddressCache and status.result.glyph.stackCount == 1, "Retain discovery state in the equipped checkpoint.")
local retained = false
for i, current in ipairs(status.result.sites) do
    if current.id == params.site.id then retained = current.questId == params.site.questId and current.tile == params.site.tile and current.layer == "Orbit" and current.hasMap == false and current.visited == false end
end
local questRetained = false
for i, quest in ipairs(status.result.quests) do
    if quest.id == params.site.questId then questRetained = quest.root == "Rimgate_GateQuestScript_Orbit" and quest.state == "Ongoing" end
end
local addresses = 0
for i, address in ipairs(status.result.addresses) do
    if address.tile == params.site.tile and address.layer == "Orbit" then addresses = addresses + 1 end
end
rb.assert(retained and questRetained and addresses == 1, "Equipment must preserve the lazy orbital quest/site/address.")
selected = rb.call("rimworld/select_pawn", { pawnId = f.pawnId })
rb.assert(selected.success and selected.result.success, "Reselect the reloaded dialer.")
menu = rb.call("rimworld/right_click_cell", { x = 110, z = 134 })
rb.assert(menu.success and menu.result.success and menu.result.provider == "ui_event", "Reopen the real reloaded DHD menu.")
enabled = false
for i, option in ipairs(menu.result.options) do
    if option.label == f.dialPrefix .. " " .. params.site.designation .. " (" .. params.site.label .. ")" then enabled = option.disabled == false end
end
rb.assert(enabled and after.result.gateActive == false and after.result.gateQueued == false, "Persist orbital eligibility without dialing or activating the gate.")
return { success = true, before = before.result, equipment = equipment.result, after = after.result, discovery = status.result, save = saved.result.path, limits = "Actual decoding and real UI eligibility only; no orbital map generation, entry, vacuum protection or combat/loot acceptance." }
