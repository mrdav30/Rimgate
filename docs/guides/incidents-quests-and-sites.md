# Incidents, Quests, and Sites

This guide explains the major event pressure in Rimgate: random incidents, gate-driven world quests, and special site loops that can snowball if you overextend.

## At a Glance

- Not all incidents are pure RNG; some require specific conditions (for example, having a Dwarfgate on your map).
- Planet and orbit gate quests are time-limited and can expire if ignored.
- Many gate-site quests spawn recurring raids after the site map is generated.
- ZPM usage can start a dedicated defense quest with repeat attacks until the signal stops.
- Some hostile raids can arrive through your gate and temporarily lock it in incoming mode until the raid ends.

## Incident Groups

### General incidents

| Incident | What it does | Key notes |
| --- | --- | --- |
| Naquadah meteorite | Drops naquadah via meteor impact | Can trigger a follow-up raid focused on the impact loot. Controlled by mod setting `Enable naquadah asteroid incidents`. |
| Marauders | Large hostile raid event | Uses maraud behavior with roughly double normal raid points. |
| Mass clone failure | Instantly kills all clone pawns | Controlled by mod setting `Enable clone incidents`. |
| Symbiote-targeted pathogen | Infects symbiote hosts with plague | Can hit eligible hosts on maps and caravans; immunity can block infection on some pawns. |

### Gate-arrival raids

Some hostile raids can use your local `Dwarfgate` as their entry point instead of edge-walking.

- Treat these as internal breach events, not perimeter-only pressure.
- If this happens, the gate can be locked in incoming state until the raid is over.
- Keep your gate room defensible even if your outer walls are strong.

### Gate-borne incidents

These only fire if your map has a spawned `Dwarfgate`.

- `Dwarfgate psychic drone`:
  - Duration: about `0.75 to 1.75` days.
  - Applies hostile psychic pressure by gender, scaling with threat points.
- `Dwarfgate toxic fallout`:
  - Duration: about `1 to 3` days.
  - Causes outdoor toxic exposure, plant loss, and faster rot for exposed items/corpses.
- `Dwarfgate heat wave`:
  - Duration: about `1 to 2` days.
  - Adds up to about `+17C` local temperature offset.

Important behavior:

- During these conditions, the local gate is forcibly held open as a receiving gate until the condition ends.

## Gate Exploration Quests

Most gate exploration content starts when you decode glyphs (see [Gate Network Guide](gate-network-guide.md) for dialing/unlock flow).

There are two main exploration pools:

- `Planet gate quests`
- `Orbit gate quests`

Both are auto-accepted scripted quests and share core behavior:

- Site is generated on the world map with a quest timer.
- If ignored too long, the site times out and the quest fails.
- Timeout is `900000` ticks (`15` in-game days).
- Once the site map is generated, recurring detection raids begin.
- Some remote site maps can hide from normal map view when empty unless gate modification equipment is active.
- Detection raid cadence:
  - First raid: about `18000 to 36000` ticks (`0.3 to 0.6` days) after map generation.
  - Repeat raids: about every `60000 to 90000` ticks (`1 to 1.5` days).

## Planet site variety

Planet quests can roll many archetypes, including:

- Tau'ri outposts
- Goa'uld outposts
- Wraith sites
- Mechanoid and insect sites
- Mixed ruins/ancient complexes
- Barren gate and bandit-style compounds

Expect optional extra defenders (enemy presence, mechs, replicators, or Tau'ri groups) depending on rolled site parts.

## Orbit site variety

Orbit quests can roll:

- Abandoned platforms
- Settlement platforms
- Mechanoid platforms
- Wrecks
- Satellites
- Asteroids

Orbit encounters are generally higher-risk and may include drones/turrets, salvage factions, or mech presence depending on site type.

Note: some orbital settlement variants can require signal-jamming capability before entry.

## Treasure Hunters at Sites

Gate-site scripts can also spawn small `Treasure Hunter` teams when a site map is generated, often near the gate.

Treat them as opportunistic third-party combatants rather than guaranteed allies or neutrals.

## Special Quest: Protect the ZPM

This quest loop is tied to your colony's broadcasting ZPM state.

- Starts when at least one player ZPM is actively broadcasting and raids are not suppressed.
- First attack warning delay: about `18000 to 30000` ticks (`0.3 to 0.5` days).
- Recurring raids then fire every `90000 to 120000` ticks (`1.5 to 2` days).
- Raiders prioritize your broadcasting ZPM as the target.
- If your ZPM signal stops, the raid loop stops (for example, uninstalled, destroyed, disconnected, or signal-scrambled states).

## Special Quest: Treasure Vault

`Treasure Vault` is a dedicated gate-site quest with a fixed vault-style site part and a high challenge profile.

- Also uses a `900000` tick (`15` day) world timeout if ignored.

## Practical Strategy

- Treat every fresh gate site as time-sensitive: plan transport, medicine, and extraction before stepping through.
- For long site operations, expect periodic reinforcement raids rather than a single-clear map.
- Keep your home gate defenses ready; some quest-driven raids can use gate arrival and temporarily lock your gate while the raid is active.
- If a site is no longer worth the risk, abandon it early rather than losing a team to stacked raid waves.
