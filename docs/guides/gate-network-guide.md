# Gate Network Guide

This guide explains how to run a Dwarfgate network in normal play: finding destinations, dialing safely, moving people/items, and defending your gate with an iris.

## At a Glance

- A working link needs a `Dwarfgate` and a linked `dial home device (DHD)`.
- You discover new destinations by decoding `glyph parchment`.
- Planet destinations are available earlier; orbit destinations require deeper research.
- Gate travel is extremely dangerous at activation if pawns stand in front of the gate.
- Hostile raids can sometimes enter through your gate and temporarily lock it in incoming mode until the raid is over.

## Core Gate Components

- `Dwarfgate`: The portal itself.
- `dial home device` (Ancient): Basic controller for dialing, closing, and exploration management (no external power needed).
- `dial home device` (Tau'ri): Faster dialing, requires power, longer link distance, remote iris control.
- `glyph parchment`: Consumable item used to unlock/add destinations.
- `iris installation kit`: Upgrades a gate with defensive iris hardware.
- `remote gate control`: Wall control for iris open/close.
- `gate modification equipment`: Required for orbital dialing and advanced gate-site visibility features.

## Gate Setup

1. Place or claim a Dwarfgate.
2. Place/claim a DHD close enough to link (use facility connection lines while placing).
3. Check the gate inspect panel for `Gate address`.
4. Start destination progression (next section) if your DHD has no dial targets yet.

Important placement rules:

- Only one active Dwarfgate is allowed per map.
- Gates cannot be placed in pocket maps.
- You cannot uninstall/reinstall a gate while it is active.

## Unlocking Destinations

### 1) Research prerequisites

- `glyph deciphering` is required before pawns can decode parchments.
- `gate modification` is required before orbit-only glyphs can be decoded.

### 2) Decode glyph parchment

- Select a pawn, right-click a parchment, choose `Decode symbols`.
- Decoding can fail (better Intellectual + manipulation helps).
- Successful decode starts a gate quest and adds a destination to your address book.
- One parchment is consumed per decode.

### 3) Understand limits

- Global address book cap is 11 total addresses.
- In practice, a DHD typically shows up to 10 dialable addresses from your current map.
- `Restricted connections` cap is 2 active restricted sites at once.
- If you hit caps, use `Abandon exploration` from a DHD to clear stale exploration links.

## Dialing a Gate

1. Select a pawn.
2. Right-click a linked DHD.
3. Choose `Dial ...` and pick a destination.
4. Wait for lock and gate activation.

Dial behavior:

- Base DHD lock delay is about 500 ticks (~8.3 seconds).
- Tau'ri DHD dials in half time.
- You cannot dial if the gate is already active, receiving an incoming wormhole, unlinked, or out of valid destinations.
- Orbit addresses additionally require powered `gate modification equipment` linked to a DHD.

## Entering and Sending Through a Gate

- Right-click an active gate and use `Enter ...` to send selected pawns.
- Multi-select is supported for sending multiple reachable pawns.
- `Bring selected to ...` can send downed pawns, corpses, and many claimable items/buildings.

Critical safety notes:

- The first moments of activation produce an unstable vortex in front of the gate that can destroy anything in its path.
- The receiving side is for arrivals. Walking into an incoming/receiving gate is lethal.

## Iris Defense

### Installing an iris

1. Research `gate iris`.
2. Craft an `iris installation kit`.
3. Use the kit and target a gate with no iris installed.

### Using the iris

- Toggle from the gate, Tau'ri DHD, or Remote gate control.
- Closed iris blocks inbound reintegration (incoming matter is destroyed).
- Iris hardware adds extra power draw (shown on gate inspect panel).
- If power fails, the iris becomes non-functional and opens automatically.

## DHD Controls You Should Use

- `Close gate`: Ends an active outgoing connection.
- `Abandon exploration`: Removes existing exploration links/sites from your known network list.
- Inspect panel counters:
  - `Addresses known`
  - `Restricted connections`

Note: incoming wormholes cannot be forcibly disengaged from the DHD.
This also applies to raid-driven incoming gate assaults: if enemies are arriving through the gate, expect temporary lockout until the raid resolves.

## Transit Sites (Caravan Network Expansion)

- Caravans carrying a Dwarfgate get a `Create new transit site` command.
- Creating a site establishes a permanent world-map gate location for future network use.
- Some landmarks/tiles are blocked from transit-site setup.
- Transit sites can be renamed and may be scavenged/looted if left unmanaged.

## Emergency and Edge Cases

- `Ruined dial home device`: After `DHD logic` research, pawns can attempt emergency dialing from a destroyed DHD.
- Emergency dialing does not let you pick a destination and uses a risky emergency routine.
- Emergency dialing is risky and can fail catastrophically (destroying the ruined DHD).
- Active gates cannot be removed cleanly until closed.

## Troubleshooting Quick Reference

- `Cannot dial: no gate connected`:
  - Your DHD is not linked to a gate.
- `Cannot dial: no available destinations`:
  - Decode more glyphs or free address slots.
- `Cannot dial: missing mod equipment`:
  - You tried an orbital destination without active modification equipment.
- `Cannot dial: locked out`:
  - Restricted-connection cap reached (2/2).
- `Cannot enter portal: not active`:
  - Dial first or wait for lock.
- `Cannot enter portal: iris closed`:
  - Open iris (and ensure power is available if needed).
- `Gate seems locked during a raid`:
  - Some enemy raids can force incoming gate state and prevent manual closure until the raid is over.
- `Cannot place gate`:
  - One-per-map rule, pocket-map restriction, or address book cap.
