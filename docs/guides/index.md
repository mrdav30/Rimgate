# Rimgate Guides

Rimgate is a Stargate-inspired expansion mod for RimWorld focused on ancient gate travel, faction conflict, alien biotech, and high-tier technology progression.

These guides cover the current implementation on `main`. Rimgate is used locally and has not been published as a mod release.

## Current Snapshot

- Mod name: `Rimgate: Ancient Legacy`
- Supported RimWorld version: `1.6`
- Source repository: <https://github.com/mrdav30/Rimgate>
- Community Discord: <https://discord.gg/mhwK2QFNBA>

## Requirements

From `Mod/About/About.xml`, the current dependency set is:

- Harmony (`brrainz.harmony`)
- Vanilla Expanded Framework (`OskarPotocki.VanillaFactionsExpanded.Core`)
- Royalty
- Ideology
- Odyssey
- Biotech
- Anomaly

## Core Gameplay Systems

- Dwarfgate network and dialing/travel systems
- Gate exploration sites and quest chains (planet and orbit variants)
- Faction packs (Tau'ri, Goa'uld, Wraith, Replicators, plus player-aligned variants)
- Custom xenotypes and gene-driven faction identities (Jaffa, Kull, Wraith)
- Symbiote lifecycle systems (hosts, queens, prim'ta progression)
- Cloning pod workflows (multiple clone types, incidents, tuning via mod settings)
- Sarcophagus treatment systems (diagnosis, regeneration, restrictions, assignable access)
- New resources, artifacts, gear, weapons, apparel, and research branches
- Storyteller incidents and world interactions tied to gate/alien content

## Compatibility and Integrations

Rimgate includes conditional patch support for multiple external mods in `Mod/1.6/Patches/`, including:

- Dubs Bad Hygiene
- Dubs Rimatomics
- Quarry
- EPOE Forked
- Vanilla Factions Expanded - Pirates
- Vanilla Furniture Expanded - Medical Module
- Vanilla Races Expanded (Android, Saurid)
- Vanilla Traits Expanded
- Altered Carbon
- Rimsec Security

These are patch-based integrations and are only applied when the target mod is present.

## Guides

- [Gate Network Guide](gate-network-guide.md)
- [Factions and Pawn Types](factions-and-pawn-types.md)
- [Xenotypes and Genes](xenotypes-and-genes.md)
- [Incidents, Quests, and Sites](incidents-quests-and-sites.md)
- [Research and Progression](research-and-progression.md)
- [Items and Resources](items-and-resources.md)
- [Biomaterial Recovery Guide](biomaterial-recovery-guide.md)
- [Apparel and Utilities](apparel-and-utilities.md)
- [Weapons](weapons.md)
- [Buildings and Structures](buildings-and-structures.md)
- [Power, Shields, and ZPM Infrastructure](power-shields-and-zpm-infrastructure.md)
- [Symbiotes and Prim'ta](symbiotes-and-primta.md)
- [Sarcophagus Guide](sarcophagus-guide.md)
- [Wraith Biomass and Cocoon Operations](wraith-biomass-and-cocoon-operations.md)
- [Wraith Essence Management](wraith-essence-management.md)
- [Cloning Systems](cloning-systems.md)
- [Mobile Logistics and Carts](mobile-logistics-and-carts.md)
- [Shuttles and Planetary Transport](shuttles-and-planetary-transport.md)
- [Base Defense and Fortification](base-defense-and-fortification.md)
- [Compatibility Matrix](compatibility-matrix.md)
- [Troubleshooting and Known Issues](troubleshooting-and-known-issues.md)
- [Developer and Contributor Notes](developer-and-contributor-notes.md)

## Contributor Quick Reference

- XML definitions: `Mod/1.6/Defs/`
- Compatibility patches: `Mod/1.6/Patches/`
- C# source: `Source/Rimgate/`
- Build script (assembly): `.assets/scripts/build-assembly.ps1`
- Build script (assetbundle): `.assets/scripts/build-assetbundle.ps1`
- Build script (full release): `.assets/scripts/build-release.ps1`
- Raw art/audio source: `AssetsRaw/` (do not manually place raw assets in `Mod/`)

## Feedback

- Report bugs or propose features in the repository issues: <https://github.com/mrdav30/Rimgate/issues>
- Use Discord for discussion and community support: <https://discord.gg/mhwK2QFNBA>
