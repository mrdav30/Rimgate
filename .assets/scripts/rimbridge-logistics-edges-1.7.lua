-- Supplement ordinary utilities with fuel variants and genuine cart obstructions.
-- Reuse the utility fixture. fuel-input replaces its EMPTY cart and returns the
-- new cartId; host retains it. refuel-menu exposes actual labels; refuel takes
-- refuelLabel. block requires an actual carried proxy; blocked-interrupt uses
-- prepare_cart_obstructions' returned blockerIds AND blockers, then checks inputs.
-- block accepts radius2..13; full-radius original-blocker comparison is a host
-- check because the restricted runner bounds statements (the default2 loop fits).
-- Compile with installed SDK. Host MUST pause in finally after any failure.
local f = params.fixture
local state = rb.call("rimgate/utilities_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, pawnId = f.pawnId, housingId = f.housingId, cargoId = f.cargoId, zpmIds = f.zpmIds[1] .. "," .. f.zpmIds[2] .. "," .. f.zpmIds[3] })
rb.assert(state.success and state.result.success and state.result.pawn.dead == false and state.result.pawn.downed == false, "Use the private healthy utility operator.")
if params.phase == "fuel-input" then
    local prepared = rb.call("rimgate/prepare_fuel_cart", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, cartId = f.cartId, variant = params.variant, initialFuel = params.initialFuel or 0 })
    rb.assert(prepared.success and prepared.result.success, "Stage an empty cart and loose fuel only.")
    return prepared.result
elseif params.phase == "refuel-menu" or params.phase == "refuel" then
    rb.assert(state.result.cartsCount == 1 and state.result.carts[1].spawned, "Use the actual docked cart.")
    local selected = rb.call("rimworld/select_pawn", { pawnId = f.pawnId })
    rb.assert(selected.success and selected.result.success, "Select the refuelling actor.")
    local menu = rb.call("rimworld/right_click_cell", { x = state.result.carts[1].cell.x, z = state.result.carts[1].cell.z })
    rb.assert(menu.success and menu.result.success and menu.result.provider == "ui_event", "Read the native contextual fuel menu.")
    if params.phase == "refuel-menu" then return menu.result end
    rb.assert(params.refuelLabel, "Supply the actual enabled native refuel label.")
    local order = rb.call("rimworld/execute_context_menu_option", { label = params.refuelLabel })
    rb.assert(order.success and order.result.success, "Order a real refuelling job.")
    local speed = rb.call("rimworld/set_time_speed", { speed = "Ultrafast" })
    rb.assert(speed.success and speed.result.success, "Run ordinary refuelling ticks.")
    local waited = rb.poll("rimgate/utilities_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, pawnId = f.pawnId, housingId = f.housingId, cargoId = f.cargoId, zpmIds = f.zpmIds[1] .. "," .. f.zpmIds[2] .. "," .. f.zpmIds[3] }, { timeoutMs = 15000, pollIntervalMs = 50, condition = { path = "result.carts[0].fuel", greaterThan = 0.05 } })
    local paused = rb.call("rimworld/set_time_speed", { speed = "Paused" })
    rb.assert(paused.success and paused.result.success and waited.success and waited.result.success and waited.result.carts[1].fuel > 0.05, "Pause after actual native refuelling.")
elseif params.phase == "block" then
    rb.assert(state.result.proxiesCount == 1 and state.result.pawn.carriedId == state.result.proxies[1].id, "Pause during real cart carrying.")
    local prepared = rb.call("rimgate/prepare_cart_obstructions", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, pawnId = f.pawnId, blockCenter = params.blockCenter or false, radius = params.radius or 2 })
    rb.assert(prepared.success and prepared.result.success, "Stage obstruction inputs without touching the carried owner.")
    return prepared.result
elseif params.phase == "blocked-interrupt" then
    rb.assert(state.result.proxiesCount == 1 and params.blockerIds and params.blockers, "Interrupt the actual carried proxy with retained blocker inputs.")
    local draft = rb.call("rimworld/set_draft", { pawnId = f.pawnId, drafted = true })
    rb.assert(draft.success and draft.result.success, "Draft to interrupt normally.")
    local tick = rb.call("rimworld/step_game_ticks", { ticks = 1, timeoutMs = 5000 })
    rb.assert(tick.success and tick.result.success, "Run ordinary interruption cleanup.")
    local blockers = rb.call("rimgate/cart_obstruction_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, blockerIds = params.blockerIds })
    rb.assert(blockers.success and blockers.result.success, "Observe original blocker survival.")
    for i, blocker in ipairs(blockers.result.blockers) do
        rb.assert(blocker.found, "Cart restoration removed an original obstruction.")
        local matched = false
        for j, original in ipairs(params.blockers) do
            if original.id == blocker.id then
                rb.assert(blocker.state[1].spawned and blocker.state[1].x == original.x and blocker.state[1].z == original.z and blocker.state[1].hitPoints == original.hitPoints, "Cart restoration moved or damaged an original obstruction.")
                matched = true
            end
        end
        rb.assert(matched, "Every checked obstruction requires its original observed state.")
    end
else
    rb.assert(params.phase == "status", "Choose a documented logistics phase.")
end
local after = rb.call("rimgate/utilities_status", { expectedConfigPath = params.configPath, expectedModRoot = params.modRoot, pawnId = f.pawnId, housingId = f.housingId, cargoId = f.cargoId, zpmIds = f.zpmIds[1] .. "," .. f.zpmIds[2] .. "," .. f.zpmIds[3] })
rb.assert(after.success and after.result.success and after.result.cargo.ownershipOccurrenceCount == 1 and after.result.cargo.distinctObjectCount == 1, "Retain exactly one real cargo object and owner.")
if params.phase == "refuel" then rb.assert(after.result.cartsCount == 1 and after.result.carts[1].fuel > 0.05, "The real refuelling job must leave fuel in the actual cart.") end
if params.phase == "blocked-interrupt" then
    rb.assert(after.result.proxiesCount == 0 and after.result.cartsCount == 1 and after.result.pawn.pushingHediffCount == 0 and after.result.pawn.attachedCartVisual == false, "Interruption must restore one usable cart and clear proxy/slowdown/visual state.")
end
return { success = true, before = state.result, after = after.result }
