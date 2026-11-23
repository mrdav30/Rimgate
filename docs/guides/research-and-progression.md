# Research and Progression

This guide explains how Rimgate progression actually unfolds in play: what to research first, what hidden gates block progress, and how analyzable artifacts tie the whole tree together.

## At a Glance

- Rimgate progression is split across four tabs: `Dwarfgate`, `Tau'ri`, `Goa'uld`, and `Wraith`.
- Most projects use a `Hi-Tech Research Bench` + `Multi-Analyzer` baseline.
- Many mid/late projects are also gated by special facilities, techprints, or analyzable artifacts.
- Major cross-tab dependencies are intentional: you will often need progress in one tab to advance another.

## How Progression Works

### 1) Normal research prerequisites

Like vanilla RimWorld, projects can require:

- Other research projects
- Specific research facilities
- Techprints
- Special analysis unlocks (`requiredAnalyzed`)

### 2) Special research facilities

Two facilities matter a lot in this mod:

- `blueprint table` (`Rimgate_TauriDraftingDesk`):
  - Buildable after `Tau'ri engineering`.
  - Adds `+15%` research speed to linked Hi-Tech benches.
  - Required by the Tau'ri high-tier branch (`Rimgate_TauriResearchHiBase` parent projects).
- `ancient research bank` (`Rimgate_AncientResearchBank`):
  - Adds `+35%` research speed to linked benches.
  - Required by many advanced Dwarfgate/Goa'uld/Tau'ri projects.
  - Also required by the entire Wraith research base.

Important note:

- `ancient research bank` is defined as non-buildable (`neverBuildable=true`). In practice, you obtain it from exploration/site content and recover it.

### 3) Techprint gates

A chunk of high-impact projects require techprints (for example advanced armor/turret branches, Ancient structures, Tok'ra excavation, Wraith assassin gear, and more).

Notable detail:

- `Wraith science` needs `2` techprints (most other tagged projects need `1`).
- In normal play, these commonly come from gate-site loot tables (including blue supply crates) and exploration rewards.

### 4) Analyzable unlock gates

Many key projects do not become available until you analyze a specific artifact/sample.

Rimgate's custom analyzable flow (`Comp_AnalyzableResearchWhen`) adds gameplay rules:

- Some analyzables are locked behind required prior knowledge (`requiresResearchDef`).
- Most analyses are one-and-done and then hidden from interaction when complete.
- Many analyzables have catastrophic failure risk (usually `25%` base chance).
- Not every analyzable is risky; a few are configured without catastrophic failure.
- Analyzer skill matters:
  - Full risk at Intellectual `0-8`.
  - Risk tapers down between `9-15`.
  - At `15+`, risk bottoms at about `5%` of base chance (for `25%` base, about `1.25%`).
- Two analyzables additionally require a `Mechanitor` to perform (`abandoned MALP`, `Replicator Link`).

## Core Backbone: Dwarfgate Tab

This tab is your global progression spine and feeds every faction tab eventually.

Typical order:

1. `glyph deciphering` (start gate exploration).
2. Analyze `naquadah` sample -> unlock `refine naquadah`.
3. `ancient alloys`.
4. Branch:
   - `irises` (defense/safety).
   - Analyze `dial home device` -> `DHD logic` (also unlocks ruined-DHD emergency dialing behavior).
   - Analyze `inert replicator link` -> anti-replicator and replicator gestation path.
5. Analyze `ZPM` -> `ZPM core`, then:
   - `subspace masking`
   - `subspace coupling`
   - `ZPM housing`
   - `ancient shield technology`

Other major branch:

- Analyze `dilapitated puddle jumper` to unlock `Puddle Jumper`.

## Tau'ri Tab

Early Tau'ri loop:

1. `Tau'ri engineering`
2. `Tau'ri manufacturing`
3. Build `blueprint table`

From there, Tau'ri branches open into:

- Armors and weapons
- Turrets
- Logistics and power (`tracked cart`, `naquadria` generators)
- Medical line (`Tau'ri medical procedures` -> `Tretonin synthesis`)

Key analyzable gate:

- Analyze `abandoned MALP` (Mechanitor required) -> `MALP mech tech`.

Cross-tab dependency to watch:

- `Tretonin synthesis` needs `symbiote basics` (Goa'uld tab) and `refine naquadah` (Dwarfgate tab).

## Goa'uld Tab

Goa'uld progression mixes military gear, symbiote biology, and sarcophagus systems.

Main branches:

- Armor/weapons/turrets:
  - Analyze `Kull armor` -> `Kull armor` research.
  - Analyze `Kull weapon` -> `Kull weapons`.
  - Analyze `Kara kesh` -> `Kara kesh` research.
- Symbiote/biology:
  - Analyze `mature symbiote` -> `symbiote basics`.
  - `canopic jars`, `Unas biology`, `cortical dampener hacking`, `Unas bioregeneration`.
- Sarcophagus:
  - Analyze `Goa'uld sarcophagus` -> `sarcophagus`.
  - Upgrade into `sarcophagus regeneration` and `sarcophagus calibration`.

Cross-tab dependencies to watch:

- `sarcophagus calibration` requires `subspace masking` + `subspace coupling` from Dwarfgate.
- `shielded defense` (Goa'uld turret III) requires `ancient shield technology`.
- `hover skid` depends on `tracked cart` from Tau'ri.

## Wraith Tab

Wraith progression is bioengineering-heavy and strongly gated by Ancient Research Banks.

Early/mid flow:

1. `Wraith science` (techprint gated)
2. Wraith armor/weapon line
3. `meat cultivation` -> `biomass generator`

Key analyzable gates:

- Analyze `Wraith comm device` -> `Wraith hacking`.
- Analyze `decaying Dart` -> `Dart`.
- Analyze `Wraith cloning pod` -> `cloning`.

Cloning expansion chain:

- `cloning` unlocks genome-level clone workflows.
- `full-body replication` unlocks full clone mode.
- `clone enhancement` unlocks enhanced/soldier mode.
- `reconstruction` unlocks corpse-based reconstruction.
- `cloning pod optimization` improves cloning pod economics/performance systems.

Late cross-tab keystone:

- `gate modification` requires deep progress in both Dwarfgate and Wraith branches.
- It is also what enables decoding of orbit-only glyphs in normal gate exploration flow.

## High-Value Analysis Targets

| Analyze this | Needs before analysis | Unlock impact |
| --- | --- | --- |
| `naquadah` sample | `advanced fabrication` | Enables `refine naquadah` chain |
| `dial home device` | `ancient alloys` | Unlocks `DHD logic` |
| `ZPM` | `electricity` | Unlocks `ZPM core` branch |
| `inert replicator link` | `standard mechtech` + Mechanitor | Unlocks anti-replicator/replicator branch |
| `abandoned MALP` | `Tau'ri manufacturing` + Mechanitor | Unlocks MALP tech |
| `dilapitated Puddle Jumper` | `ship basics` | Unlocks Puddle Jumper |
| `deteriorating Tel'tak` | `ship basics` | Unlocks Tel'tak |
| `decaying Dart` | `ship basics` | Unlocks Dart |
| `Wraith comm device` | `standard mechtech` | Unlocks Wraith hacking chain |
| `Wraith cloning pod` | `Wraith science` | Unlocks cloning chain |
| `mature symbiote` | `Goa'uld tech` | Unlocks symbiote research line |
| `Goa'uld sarcophagus` | `Goa'uld tech` | Unlocks sarcophagus line |
| `cortical dampener` | `bionics` | Unlocks dampener hacking |
| `Kull armor` / `Kara kesh` | `charged shot` / `shield belt` | Unlocks elite Goa'uld gear projects |

## Practical Progression Strategy

- Prioritize `glyph deciphering` immediately, then secure `naquadah` analysis so your material/power curve starts early.
- Build toward `Tau'ri manufacturing` early if you want smoother mid-game scaling; the `blueprint table` is a major pacing upgrade.
- Treat analyzable artifacts as strategic resources, not just loot. High Intellectual analyzers dramatically reduce catastrophic losses.
- Push `ZPM core` only when you can defend signal pressure; pair it with `subspace masking` sooner rather than later.
- If your plan includes cloning or advanced Wraith systems, prioritize obtaining at least one `ancient research bank` early from site operations.
