# Compatibility Matrix

This page tracks how Rimgate interacts with other mods and core RimWorld content.

Source of truth: `Mod/1.6/Patches/`, each version project's `Compatibility/`
code and `Mod/About/About.xml`.

## At a Glance

- RimWorld `1.6` is the only declared supported target. Experimental `1.7`
  has passed the representative engine and hidden input/capture checks; its
  broader feature/save/optional-mod acceptance remains in progress in the
  [migration tracker](https://github.com/mrdav30/Rimgate/blob/main/docs/feature-work/rimworld-1.7-migration-plan.md).
- `Integrated` below means integration code/patches exist; it does not identify
  a recently tested game build or dependency combination.
- Rimgate has required dependencies that must be present for the mod to load.
- It also ships multiple optional compatibility patches that activate only when the target mod is detected.
- Most optional integrations are focused on keeping `Goa'uld sarcophagus` behavior sane across other modded health systems.
- If a mod is not listed here, treat it as unverified rather than confirmed incompatible.

## Required Dependencies

These are hard requirements from `About.xml`.

| Dependency | PackageId | Required For |
| --- | --- | --- |
| `Harmony` | `brrainz.harmony` | Runtime patching |
| `Vanilla Expanded Framework` | `OskarPotocki.VanillaFactionsExpanded.Core` | Shared framework support |
| `Royalty` | `Ludeon.RimWorld.Royalty` | Core content dependency |
| `Ideology` | `Ludeon.RimWorld.Ideology` | Core content dependency |
| `Odyssey` | `Ludeon.RimWorld.Odyssey` | Core content dependency |
| `Biotech` | `Ludeon.RimWorld.Biotech` | Core content dependency |
| `Anomaly` | `Ludeon.RimWorld.Anomaly` | Core content dependency |

The C# build also needs Dubs Bad Hygiene reference binaries because the 1.6
project compiles its optional integration against `BadHygiene.dll`. That is a
build prerequisite, not an additional required runtime mod in `About.xml`.
Runtime absence/presence checks remain necessary when changing that integration.

## Optional Mod Integrations

These integrations activate conditionally when the matching mod is present.
The C# hooks and wrappers are version-owned alongside the gameplay assembly.

| External Mod | Owner | Integration Summary | Status |
| --- | --- | --- | --- |
| `Altered Carbon 2: ReSleeved` | `Patches_AlteredCarbon.xml` | Prevents empty sleeves from being sent to sarcophagus treatment. | `Integrated` |
| `Dubs Bad Hygiene` (`Lite` / `Thirst` variants) | `Patches_DubsBadHygiene.xml` | Excludes hygiene-related hediff treatment in sarcophagus flows. | `Integrated` |
| `Dubs Rimatomics` | `Patches_DubsRimatomics.xml` | Allows sarcophagus treatment for `RadiationIncurable`. | `Integrated` |
| `Expanded Prosthetics and Organ Engineering - Forked` | `Patches_EpoeForked.xml` | Adjusts specific EPOE post-surgery consciousness caps to prevent bad outcomes. | `Integrated` |
| `Quarry` | `Patches_Quarry.xml` | Enables `MALP` mech and `Unas` quarry mining work type. | `Integrated` |
| `Rimsec Security (Continued)` | `Patches_RimsecSecurity.xml` | Blocks purely mechanical Rimsec units from sarcophagus treatment. | `Integrated` |
| `Vanilla Factions Expanded - Pirates` | `Patches_VanillaFactionsExpandedPirates.xml` | Applies warcasket/shellcasket-specific sarcophagus trait rules. | `Integrated` |
| `Vanilla Furniture Expanded - Medical Module` | `Patches_VanillaFurnitureExpandedMedicalModule.xml` | Adds Tau'ri surgery lights as an operating-table linkable facility. | `Integrated` |
| `Vanilla Races Expanded - Android` | `Patches_VanillaRacesExpandedAndroid.xml` | Prevents android xenotypes from sarcophagus treatment. | `Integrated` |
| `Vanilla Races Expanded - Saurid` | `Patches_VanillaRacesExpandedSaurid.xml` | Prevents treatment of specific Saurid-specific condition hediffs. | `Integrated` |
| `Vanilla Traits Expanded` | `Patches_VanillaTraitsExpanded.xml` | Adds trait-specific sarcophagus treatment handling (`Lush`, `Stoner`). | `Integrated` |
| `Alpha Genes` | `Compatibility/ModCompatibility.cs`, `AlphaGenesCompatibility.cs` | Guards the dependency's bone-breaking hook while a pawn is held in a sarcophagus. | `Integrated` |
| `Elite Bionics Framework` | `Compatibility/EbfCompatibilityWrapper.cs` | Uses the dependency's body-part maximum-health calculation when its optional endpoint is available; otherwise uses vanilla. | `Integrated` |

Experimental 1.7 acceptance checks the actual loaded versions, resolved content
and gameplay, with dependencies absent and present. Full Dubs Bad Hygiene does
not establish its separate Lite/Thirst variants. An older selected load folder
alone establishes neither compatibility nor failure. The installed Quarry copies
currently declare only through 1.6 and are excluded from the 1.7 acceptance profile;
this does not remove the retained integration or declare an untested fork broken.
The migration tracker owns exact combinations and outstanding checks.

## Core/Gamewide Patches (Always Shipped)

These are not external mod integrations, but they do affect compatibility expectations across playthroughs.

| Patch File | Scope | Effect |
| --- | --- | --- |
| `Patches_AddRaidStrategies.xml` | Raid behavior | Adds gate arrival mode to immediate attack strategies. |
| `Patches_CoreThinkTrees.xml` | Pawn AI | Adds sarcophagus treatment job logic to core humanlike think trees. |
| `Patches_CoreTraders.xml` | Economy/trade pools | Adds Rimgate trade entries and excludes sarcophagus from trader building stock. |
| `Patches_Factions.xml` | Faction diplomacy (Royalty) | Adjusts Empire permanent-enemy exceptions for Rimgate player faction variants. |
| `Patches_Linkables.xml` | Facility linking | Links Rimgate facilities into vanilla beds/research benches where applicable. |
| `Patches_MechRecipes.xml` | Mech gestation | Adds Rimgate mech gestation recipes to the vanilla mech gestator. |
| `Patches_Terrain.xml` | Build costs | Replaces vanilla `Concrete`/`PavedTile` cost inputs with `Rimgate_Concrete`. |

## Non-Compatible Mods (Placeholder)

No confirmed hard incompatibility is currently recorded here. Keep unavailable
or unverified experimental combinations distinct from reproduced conflicts.

| Mod | Status | Notes |
| --- | --- | --- |
| `None confirmed yet` | `Placeholder` | Add entries here when incompatibilities are reproduced and verified. |

## Notes

- Optional compatibility is detected through patch conditions in `Mod/1.6/Patches/`.
- Some patches key off mod display names; forks or renamed variants may not always trigger automatically.
- If you find a conflict, include your active mod list and error log so this matrix can be updated quickly.
