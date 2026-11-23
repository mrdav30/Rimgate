# Xenotypes and Genes

This page covers the custom xenotypes and gene package design in Rimgate, including where these pawns appear and what each Rimgate gene does in play.

For faction encounter context, see [Factions and Pawn Types](factions-and-pawn-types.md).
For Goa'uld symbiote host workflows, see [Symbiotes and Prim'ta](symbiotes-and-primta.md).
For Wraith feeding/ability loops, see [Wraith Essence Management](wraith-essence-management.md).
For clone inheritance and mutation risk, see [Cloning Systems](cloning-systems.md).

## At a Glance

- Rimgate currently defines `3` custom xenotypes: `Goa'uld Jaffa`, `Kull`, and `Wraith`.
- These xenotypes are `inheritable` and used directly by faction/player pawn kinds.
- Most Rimgate genes are not random-roll genes; they are attached through xenotypes or scripted outcomes.
- `cellular degradation` is the main instability gene and is central to Kull identity and some clone outcomes.

## Xenotype Overview

| Xenotype | Def Name | Identity | Typical Sources |
| --- | --- | --- | --- |
| `Goa'uld Jaffa` | `Rimgate_GoauldJaffa` | Symbiote-capable warrior host with discipline/resilience package | Goa'uld military classes and `Free Jaffa Nation` start pawns |
| `Kull` | `Rimgate_KullWarrior` | Engineered supersoldier with very high combat throughput and built-in biological decline | Elite Goa'uld `Kull warrior` units |
| `Wraith` | `Rimgate_Wraith` | Ageless, essence-fed predator package with webbing and psychic utility | Wraith faction pawns and `Wraith Co-Op` start pawns |

## Xenotype Gene Packages

### Goa'uld Jaffa (`Rimgate_GoauldJaffa`)

Included genes:

- `Body_Standard`
- `MoveSpeed_Quick`
- `Rimgate_JaffaLongevity`
- `Rimgate_SymbiotePouchMutation`
- `Rimgate_JaffaWarrior`
- `Rimgate_JaffaCombatDiscipline`

Practical profile:

- Built for long service life and military reliability.
- Can host Prim'ta due to the pouch mutation.
- Trades away civilian utility (multiple work tags disabled).

### Kull (`Rimgate_KullWarrior`)

Included genes:

- `Eyes_Gray`
- `Hair_BaldOnly`
- `Skin_LightGray`
- `Beard_NoBeardOnly`
- `Body_Hulk`
- `Beauty_Ugly`
- `MoveSpeed_VeryQuick`
- `Pain_Reduced`
- `MeleeDamage_Strong`
- `Unstoppable`
- `Robust`
- `PsychicAbility_Deaf`
- `Immunity_Weak`
- `WoundHealing_Slow`
- `Rimgate_CellularDegradation`

Practical profile:

- Extremely dangerous front-line combat package.
- Built-in downside is accelerated aging/biological collapse pressure from `cellular degradation`.

### Wraith (`Rimgate_Wraith`)

Included genes:

- `Hair_SnowWhite`
- `Skin_SheerWhite`
- `Neversleep`
- `ArchiteMetabolism`
- `DarkVision`
- `MeleeDamage_Strong`
- `Pain_Reduced`
- `Robust`
- `Rimgate_WraithBeauty`
- `Rimgate_WraithEssenceMetabolism`
- `Rimgate_WraithAgeless`
- `Rimgate_WraithMovement`
- `Rimgate_WraithPsychic`
- `Rimgate_WraithWebbingGene`

Practical profile:

- Core Wraith loop pawn: no normal food need, essence resource management, cocoon/webbing toolkit.
- Strong combat/mobility baseline plus long-lifespan scaling.

## Custom Gene Reference (Rimgate)

| Gene | Def Name | Biostats (C/M/A) | Key Gameplay Effect |
| --- | --- | --- | --- |
| life longevity | `Rimgate_JaffaLongevity` | `2 / -1 / 2` | Major adult aging slowdown; reduced learning rate (`0.70x`). |
| symbiote pouch | `Rimgate_SymbiotePouchMutation` | `1 / 0 / 0` | Adds `Rimgate_SymbiotePouch` hediff to stomach, enabling Prim'ta hosting. |
| warrior caste discipline | `Rimgate_JaffaCombatDiscipline` | `3 / -4 / 0` | Combat accuracy/dodge package; disables several civilian work tags (`Caring`, `Mining`, `Artistic`, `Social`, `Cleaning`). |
| jaffa resilience | `Rimgate_JaffaWarrior` | `2 / -4 / 0` | Higher pain/toxin resilience and lower break threshold; suppresses `Wimp` trait. |
| wraith beauty | `Rimgate_WraithBeauty` | `1 / 0 / 0` | High beauty offset plus forced Wraith visual profile (hair/head/beard filters). |
| wraith mobility | `Rimgate_WraithMovement` | `2 / -2 / 0` | Flat movement boost (`MoveSpeed +0.6`). |
| wraith psionics | `Rimgate_WraithPsychic` | `1 / -1 / 0` | Psychic sensitivity boost (`+0.35`). |
| wraith regeneration | `Rimgate_WraithAgeless` | `3 / -2 / 2` | High healing/immunity multipliers with very slow long-term biological aging. |
| essence metabolism | `Rimgate_WraithEssenceMetabolism` | `3 / -2 / 3` | Disables `Food`, adds essence resource (`loss 0.02/day`), grants `drain life` and `regenerative trance`; activates at age `18+`. |
| webbing protein glands | `Rimgate_WraithWebbingGene` | `3 / -2 / 3` | Grants `web shot` and `cocoon wrap`; also required for Wraith cocoon/bill workflows. |
| cellular degradation | `Rimgate_CellularDegradation` | `1 / -3 / 0` | Extreme accelerated aging curve (up to `5.75x` biological age ticks at adulthood). |

## Acquisition and Inheritance Notes

- All three custom xenotypes are set `inheritable=true` in `XenoTypeDefs.xml`.
- Wraith and Jaffa player starts use fixed xenotype sets (`Rimgate_Wraith`, `Rimgate_GoauldJaffa`).
- Goa'uld and Wraith slave classes use mixed vanilla xenotype pools, so not every pawn in those factions is a custom xenotype.
- `primogenitor sphere` is a one-use artifact that grants `Rimgate_SymbiotePouchMutation`.
- Clones inherit host xenotype and genes, then may gain `Rimgate_CellularDegradation` (Biotech active): `20%` (`Genome`), `25%` (`Full`/`Reconstruction`), `40%` (`Enhanced`).
