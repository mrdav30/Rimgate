-- Private native utilities: prepare, insert, eject, load_dialog, accept_load,
-- loaded, push_start, checkpoint, push_resume, interrupt, unload, status.
-- Pass configPath/modRoot/phase and prepare's fixture in subsequent phases.
-- load_dialog returns real UI layout. Set only the autopistol count to1 through
-- normal core UI, then pass its fresh Accept ui-element as acceptTargetId.
-- Override insertLabel/loadLabel/pushLabel only with labels from the real menu.
-- checkpoint saves/reloads the current state; use while carrying a proxy too.
-- Pass the same optional destination={x,z} to push_start and push_resume.
-- interrupt drafts the pusher. Charge totals are reported, not conservation-tested.
-- Bulk waits run normal Ultrafast ticks without changing the boost; overshoot is allowed.
-- Limit each invocation to 60 seconds. The host MUST pause in finally on any failure.
local config = rb.call("rimworld/get_mod_configuration_status", {})
rb.assert(config.success and config.result.success and config.result.configPath == params.configPath and config.result.configurationIssueCount == 0, "Use the healthy private profile.")
local deployment = false
for i, mod in ipairs(config.result.activeMods) do
    if mod.packageId == "mrdav30.rimgate" then deployment = mod.loadedInSession and mod.rootDir == params.modRoot end
end
rb.assert(deployment, "Expected private deployment is not loaded.")
if params.phase == "prepare" then
    local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-bridge-smoke", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.result.success, "Load the owned smoke save.")
    local assets = rb.call("rimgate/asset_report", {})
    rb.assert(assets.result.success and assets.result.assetFormat == "WaylithPack", "Use the native package.")
    local fixture = rb.call("rimgate/prepare_utilities_fixture", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot })
    rb.assert(fixture.result.success and fixture.result.snapshot.housing.spawned and fixture.result.snapshot.fixtureLampSpawned and fixture.result.snapshot.housing.count == 0, "Preparation must leave the empty housing and lamp spawned.")
    local clockStart = rb.call("rimgate/utilities_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, pawnId = fixture.result.pawnId, housingId = fixture.result.housingId, cargoId = fixture.result.cargoId, zpmIds = fixture.result.zpmIds[1] .. "," .. fixture.result.zpmIds[2] .. "," .. fixture.result.zpmIds[3] })
    rb.assert(clockStart.success and clockStart.result.success, "Read the actual starting clock.")
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Run ordinary engine ticks.")
    local elapsed = rb.poll("rimgate/utilities_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, pawnId = fixture.result.pawnId, housingId = fixture.result.housingId, cargoId = fixture.result.cargoId, zpmIds = fixture.result.zpmIds[1] .. "," .. fixture.result.zpmIds[2] .. "," .. fixture.result.zpmIds[3] }, { timeoutMs = 15000, pollIntervalMs = 50, condition = { path = "result.tick", greaterThanOrEqual = clockStart.result.tick + 250 } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success, "Pause after the bulk clock wait.")
    rb.assert(elapsed.success and elapsed.result.tick >= clockStart.result.tick + 250, "Initialize the fixture's normal power network.")
    local ready = rb.call("rimgate/utilities_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, pawnId = fixture.result.pawnId, housingId = fixture.result.housingId, cargoId = fixture.result.cargoId, zpmIds = fixture.result.zpmIds[1] .. "," .. fixture.result.zpmIds[2] .. "," .. fixture.result.zpmIds[3] })
    rb.assert(ready.result.housing.spawned and ready.result.housing.connected and ready.result.fixtureLampSpawned, "Normal ticks must connect the spawned housing/lamp fixture.")
    return { success = true, fixture = fixture.result, ready = ready.result }
end
local f = params.fixture
local ids = f.zpmIds[1] .. "," .. f.zpmIds[2] .. "," .. f.zpmIds[3]
local state = rb.call("rimgate/utilities_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, pawnId = f.pawnId, housingId = f.housingId, cargoId = f.cargoId, zpmIds = ids })
rb.assert(state.result.success and state.result.pawn.dead == false and state.result.pawn.downed == false, "A healthy tracked actor is required.")
if params.phase == "insert" then
    rb.assert(state.result.housing.spawned and state.result.fixtureLampSpawned, "Insertion requires a spawned housing/lamp fixture.")
    rb.assert(state.result.housing.count == 0, "Begin with the empty fixture housing.")
    for i, cell in ipairs(f.zpmCells) do
        local selected = rb.call("rimworld/select_pawn", { pawnId = f.pawnId })
        rb.assert(selected.result.success, "Select the hauler.")
        local menu = rb.call("rimworld/right_click_cell", { x = cell.x, z = cell.z })
        rb.assert(menu.result.success, "Open the real ZPM menu.")
        local order = rb.call("rimworld/execute_context_menu_option", { label = params.insertLabel or "Insert into ZPM containment housing" })
        rb.assert(order.result.success, "Order normal ZPM insertion.")
        local clockStart = rb.call("rimgate/utilities_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, pawnId = f.pawnId, housingId = f.housingId, cargoId = f.cargoId, zpmIds = ids })
        rb.assert(clockStart.success and clockStart.result.success, "Read the actual starting clock.")
        local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
        rb.assert(speed.success and speed.result.success, "Run ordinary engine ticks.")
        local elapsed = rb.poll("rimgate/utilities_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, pawnId = f.pawnId, housingId = f.housingId, cargoId = f.cargoId, zpmIds = ids }, { timeoutMs = 15000, pollIntervalMs = 50, condition = { path = "result.tick", greaterThanOrEqual = clockStart.result.tick + 500 } })
        local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
        rb.assert(paused.success and paused.result.success, "Pause after the bulk clock wait.")
        rb.assert(elapsed.success and elapsed.result.tick >= clockStart.result.tick + 500, "Insertion clock wait failed.")
        state = rb.call("rimgate/utilities_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, pawnId = f.pawnId, housingId = f.housingId, cargoId = f.cargoId, zpmIds = ids })
        rb.assert(state.result.housing.spawned and state.result.housing.connected and state.result.fixtureLampSpawned and state.result.housing.count == i, "The real insertion job did not fill the next spawned connected housing slot.")
    end
elseif params.phase == "eject" then
    rb.assert(state.result.housing.spawned and state.result.fixtureLampSpawned, "Ejection requires a spawned housing/lamp fixture.")
    rb.assert(state.result.housing.count == 3, "Fill all three slots first.")
    local selected = rb.call("rimworld/select_pawn", { pawnId = f.pawnId })
    rb.assert(selected.result.success, "Select the hauler.")
    local menu = rb.call("rimworld/right_click_cell", { x = f.housingCell.x, z = f.housingCell.z })
    rb.assert(menu.result.success, "Open the real housing menu.")
    local order = rb.call("rimworld/execute_context_menu_option", { label = "Eject all ZPMs" })
    rb.assert(order.result.success, "Order normal ejection.")
    local clockStart = rb.call("rimgate/utilities_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, pawnId = f.pawnId, housingId = f.housingId, cargoId = f.cargoId, zpmIds = ids })
    rb.assert(clockStart.success and clockStart.result.success, "Read the actual starting clock.")
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Run ordinary engine ticks.")
    local elapsed = rb.poll("rimgate/utilities_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, pawnId = f.pawnId, housingId = f.housingId, cargoId = f.cargoId, zpmIds = ids }, { timeoutMs = 15000, pollIntervalMs = 50, condition = { path = "result.tick", greaterThanOrEqual = clockStart.result.tick + 250 } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success, "Pause after the bulk clock wait.")
    rb.assert(elapsed.success and elapsed.result.tick >= clockStart.result.tick + 250, "Ejection clock wait failed.")
elseif params.phase == "checkpoint" then
    rb.assert(state.result.housing.spawned and state.result.fixtureLampSpawned, "The utility checkpoint requires a spawned housing and lamp.")
    local saved = rb.call("rimworld/save_game", { saveName = "Rimgate-utilities-checkpoint" })
    rb.assert(saved.result.success and saved.result.exists, "Save the utility checkpoint.")
    local loaded = rb.call("rimworld/load_game_ready", { saveName = "Rimgate-utilities-checkpoint", readiness = "visual", pauseIfNeeded = true, timeoutMs = 120000 })
    rb.assert(loaded.result.success, "Reload the utility checkpoint.")
    local restored = rb.call("rimgate/utilities_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, pawnId = f.pawnId, housingId = f.housingId, cargoId = f.cargoId, zpmIds = ids })
    rb.assert(restored.result.housing.spawned and restored.result.fixtureLampSpawned, "Reload minified or despawned the housing/lamp fixture.")
    rb.assert(restored.result.housing.count == state.result.housing.count and restored.result.proxiesCount == state.result.proxiesCount, "Housing/proxy ownership changed on reload.")
    rb.assert(restored.result.pawn.pushingHediffCount == state.result.pawn.pushingHediffCount, "Pushing state changed on reload.")
    rb.assert(restored.result.cargo.ownershipOccurrenceCount == 1 and restored.result.cargo.distinctObjectCount == 1, "Reload lost or duplicated the actual cargo.")
    for i, zpm in ipairs(restored.result.zpms) do
        rb.assert(zpm.ownership.ownershipOccurrenceCount == 1 and zpm.ownership.distinctObjectCount == 1, "Reload lost or duplicated a tracked ZPM.")
    end
    if state.result.proxiesCount == 1 then
        rb.assert(restored.result.proxies[1].id == state.result.proxies[1].id and restored.result.proxies[1].heldIds[1] == f.cargoId, "Reload replaced the proxy or detached its actual cargo.")
        rb.assert(restored.result.pawn.carriedId == restored.result.proxies[1].id and restored.result.pawn.job == state.result.pawn.job, "Reload did not restore the pusher's carried proxy/job.")
        rb.assert(restored.result.pawn.attachedCartVisual, "Reload lost the in-flight cart visual attachment.")
    end
    return { success = true, before = state.result, restored = restored.result }
elseif params.phase == "load_dialog" then
    rb.assert(state.result.cartsCount == 1 and state.result.carts[1].spawned, "A single docked cart is required.")
    local cart = state.result.carts[1]
    local selected = rb.call("rimworld/click_cell", { x = cart.cell.x, z = cart.cell.z, button = "left" })
    rb.assert(selected.result.success, "Select the cart.")
    local commands = rb.call("rimworld/list_selected_gizmos", {})
    local opened = false
    for i, command in ipairs(commands.result.gizmos) do
        if command.label == "Load" then
            local clicked = rb.call("rimworld/execute_gizmo", { gizmoId = command.id })
            rb.assert(clicked.result.success, "Open the normal loading dialog.")
            opened = true
        end
    end
    rb.assert(opened, "The normal Load command was absent.")
    local layout = rb.call("rimworld/get_ui_layout", {})
    rb.assert(layout.result.success, "Read the real loading controls.")
    return { success = true, layout = layout.result, instruction = "Set only the loose autopistol count to1 using core UI; never use DEV: Load instantly. Pass a fresh Accept target ID to accept_load." }
elseif params.phase == "accept_load" then
    rb.assert(params.acceptTargetId, "Supply the fresh normal Accept target after setting cargo count.")
    local accepted = rb.call("rimworld/click_ui_target", { targetId = params.acceptTargetId })
    rb.assert(accepted.result.success, "Accept the normal load manifest.")
    local selected = rb.call("rimworld/select_pawn", { pawnId = f.pawnId })
    rb.assert(selected.result.success, "Select the hauler.")
    local cart = state.result.carts[1]
    local menu = rb.call("rimworld/right_click_cell", { x = cart.cell.x, z = cart.cell.z })
    rb.assert(menu.result.success, "Open the cart's loading work menu.")
    local order = rb.call("rimworld/execute_context_menu_option", { label = params.loadLabel or "Prioritize loading steel wheelbarrow" })
    rb.assert(order.result.success, "Order a real load-cart hauling job.")
    local clockStart = rb.call("rimgate/utilities_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, pawnId = f.pawnId, housingId = f.housingId, cargoId = f.cargoId, zpmIds = ids })
    rb.assert(clockStart.success and clockStart.result.success, "Read the actual starting clock.")
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Run ordinary engine ticks.")
    local elapsed = rb.poll("rimgate/utilities_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, pawnId = f.pawnId, housingId = f.housingId, cargoId = f.cargoId, zpmIds = ids }, { timeoutMs = 15000, pollIntervalMs = 50, condition = { path = "result.tick", greaterThanOrEqual = clockStart.result.tick + 500 } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success, "Pause after the bulk clock wait.")
    rb.assert(elapsed.success and elapsed.result.tick >= clockStart.result.tick + 500, "Load job clock wait failed.")
elseif params.phase == "push_start" then
    rb.assert(state.result.cartsCount == 1 and state.result.carts[1].spawned and state.result.carts[1].heldCount == 1, "Load the unique cargo first.")
    local cart = state.result.carts[1]
    local selected = rb.call("rimworld/select_pawn", { pawnId = f.pawnId })
    rb.assert(selected.result.success, "Select the pusher.")
    local menu = rb.call("rimworld/right_click_cell", { x = cart.cell.x, z = cart.cell.z })
    rb.assert(menu.result.success, "Open the real push menu.")
    local order = rb.call("rimworld/execute_context_menu_option", { label = params.pushLabel or "Push to..." })
    rb.assert(order.result.success, "Begin normal push targeting.")
    local destination = params.destination or f.pushDestination
    local target = rb.call("rimworld/click_cell", { x = destination.x, z = destination.z, button = "left" })
    rb.assert(target.result.success, "Choose a normal same-map push destination.")
    local carrying = false
    for i = 1, 600 do
        if carrying == false then
            local ticks = rb.call("rimworld/step_game_ticks", { ticks = 1, timeoutMs = 30000 })
            rb.assert(ticks.result.success, "Advance the normal push approach.")
            state = rb.call("rimgate/utilities_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, pawnId = f.pawnId, housingId = f.housingId, cargoId = f.cargoId, zpmIds = ids })
            carrying = state.result.proxiesCount == 1 and state.result.pawn.carriedId == state.result.proxies[1].id
        end
    end
    rb.assert(carrying and state.result.proxies[1].heldIds[1] == f.cargoId, "Normal pushing never acquired the unique cargo proxy.")
    return { success = true, status = state.result, instruction = "Run checkpoint now to save/reload in flight; then push_resume or interrupt." }
elseif params.phase == "push_resume" then
    rb.assert(state.result.proxiesCount == 1, "Begin from an in-flight proxy, optionally reloaded.")
    local clockStart = rb.call("rimgate/utilities_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, pawnId = f.pawnId, housingId = f.housingId, cargoId = f.cargoId, zpmIds = ids })
    rb.assert(clockStart.success and clockStart.result.success, "Read the actual starting clock.")
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Run ordinary engine ticks.")
    local elapsed = rb.poll("rimgate/utilities_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, pawnId = f.pawnId, housingId = f.housingId, cargoId = f.cargoId, zpmIds = ids }, { timeoutMs = 15000, pollIntervalMs = 50, condition = { path = "result.tick", greaterThanOrEqual = clockStart.result.tick + 2000 } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success, "Pause after the bulk clock wait.")
    rb.assert(elapsed.success and elapsed.result.tick >= clockStart.result.tick + 2000, "Resume the normal push job.")
elseif params.phase == "interrupt" then
    rb.assert(state.result.proxiesCount == 1, "Interrupt an actual in-flight push.")
    local draft = rb.call("rimworld/set_draft", { pawnId = f.pawnId, drafted = true })
    rb.assert(draft.result.success, "Draft to interrupt the real job.")
    local ticks = rb.call("rimworld/step_game_ticks", { ticks = 1, timeoutMs = 30000 })
    rb.assert(ticks.result.success, "Advance normal interruption cleanup.")
elseif params.phase == "unload" then
    rb.assert(state.result.cartsCount == 1 and state.result.carts[1].spawned, "A docked cart is required.")
    local cart = state.result.carts[1]
    local selected = rb.call("rimworld/click_cell", { x = cart.cell.x, z = cart.cell.z, button = "left" })
    rb.assert(selected.result.success, "Select the actual converted cart.")
    local commands = rb.call("rimworld/list_selected_gizmos", {})
    local unloaded = false
    for i, command in ipairs(commands.result.gizmos) do
        if command.label == "Unload all" then
            local clicked = rb.call("rimworld/execute_gizmo", { gizmoId = command.id })
            rb.assert(clicked.result.success, "Use the player's normal unload command.")
            unloaded = true
        end
    end
    rb.assert(unloaded, "The normal unload command was absent.")
else
    rb.assert(params.phase == "loaded" or params.phase == "status", "Choose a documented utility phase.")
end
state = rb.call("rimgate/utilities_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, pawnId = f.pawnId, housingId = f.housingId, cargoId = f.cargoId, zpmIds = ids })
rb.assert(state.result.cargo.ownershipOccurrenceCount == 1 and state.result.cargo.distinctObjectCount == 1, "Cargo lost its single deep owner or identity.")
for i, zpm in ipairs(state.result.zpms) do
    rb.assert(zpm.ownership.ownershipOccurrenceCount == 1 and zpm.ownership.distinctObjectCount == 1, "A ZPM was lost or duplicated.")
    if params.phase == "eject" then rb.assert(zpm.state.spawned, "Ejection did not restore each actual ZPM to the map.") end
end
if params.phase == "eject" then rb.assert(state.result.housing.count == 0, "Ejection left housing contents.") end
if params.phase == "insert" then
    rb.assert(state.result.housing.connected and state.result.housing.storedEnergy > 0, "Inserted ZPMs must supply positive stored energy to the connected housing.")
    local lampPowered = false
    for i, lamp in ipairs(state.result.lamp) do
        if lamp.id == f.lampId then lampPowered = lamp.spawned and lamp.powerOn end
    end
    rb.assert(lampPowered, "The actual fixture lamp must receive power after insertion.")
end
if params.phase == "loaded" or params.phase == "accept_load" or params.phase == "push_resume" or params.phase == "interrupt" then
    rb.assert(state.result.cartsCount == 1 and state.result.proxiesCount == 0 and state.result.carts[1].heldIds[1] == f.cargoId, "The unique cargo did not finish in one actual cart.")
    rb.assert(state.result.carts[1].stuff == "Steel" and state.result.carts[1].allowUseContents == false, "Cart conversion lost its stuff/use setting.")
end
if params.phase == "push_resume" or params.phase == "interrupt" then
    rb.assert(state.result.pawn.pushingHediffCount == 0 and state.result.pawn.attachedCartVisual == false, "Push cleanup left slowdown or visual attachments.")
end
if params.phase == "push_resume" then
    local destination = params.destination or f.pushDestination
    rb.assert(state.result.carts[1].spawned and state.result.carts[1].cell.x == destination.x and state.result.carts[1].cell.z == destination.z, "Normal pushing did not place the cart at the actual selected destination.")
    rb.assert(state.result.pawn.job ~= "Rimgate_PushContainer" and state.result.pawn.job ~= "Rimgate_EnterGateWithContainer", "Normal pushing left its push job running.")
end
if params.phase == "unload" then rb.assert(state.result.carts[1].heldCount == 0 and state.result.cargo.locations[1].spawned, "Unload did not release the cargo to the map.") end
return { success = true, status = state.result }
