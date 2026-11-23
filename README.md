# Rimgate #

Rimgate is a large-scale Stargate-inspired expansion for RimWorld, introducing an ancient planetary gate network, alien technologies, and faction-driven conflict centered around mysterious Dwarfgates.

Unlike traditional Stargate interpretations, this mod reimagines gate technology as a sealed planetary lattice integrated directly into the RimWorld universe. Dwarfgates are relic constructs—smaller, dimmer cousins of myth—left scattered across the Rim for unknown reasons.

Players can discover, study, control, and weaponize this technology while navigating escalating tensions between factions drawn to its power.

## 🌌 Core Features ##

- 🌀 Dwarfgates — Ancient planetary transit structures with configurable dialing mechanics
- 🧠 Reverse Engineering Systems — Analyze and unlock alien technologies
- ⚡ Advanced Power & Materials — Naquadah, ZPMs, and high-tier constructs
- ⚔️ Faction Integration — Tau’ri, Goa’uld, Wraith, Replicators, and more
- 🔬 Research-Driven Progression — Study artifacts to unlock advanced capabilities
- 🏛 Archotech & RimWorld Integration — Seamless lore blending with vanilla systems

## 📦 Requirements ##

- RimWorld **1.6** (the currently configured version).
- Royalty, Ideology, Biotech, Anomaly and Odyssey.
- Harmony and Vanilla Expanded Framework.

Rimgate is maintained here as an open source project and used locally. It has not
been published as a mod release or on Steam Workshop. See the
[Compatibility Matrix](wiki/Compatibility-Matrix.md) for declared dependencies
and implemented integrations; these are not a claim of 1.7 compatibility.

## 🧪 Developer Resources ##

For build workflows, asset pipeline rules, testing checklists, and release packaging details:

- [Developer and Contributor Notes](wiki/Developer-and-Contributor-Notes.md)
- [AGENTS.md](AGENTS.md)
- [RimWorld modding skill](.agents/skills/rimworld-modding/SKILL.md)
- [Rimgate specialization](.agents/skills/rimgate-development/SKILL.md)

Build and development scripts live in `.assets/scripts/`; build logs stay in the
repository root and are gitignored. The build scripts accept `-RimWorldVersion 1.6`
and default to `1.6`. Other versions are rejected until their content, source and
load configuration exist.
The 1.7 migration remains separate work.

## 🤝 Contributing ##

Contributions are welcome.

If you'd like to contribute:

1. Fork the repository
2. Create a feature branch
3. Follow RimWorld XML and C# conventions
4. Submit a pull request with a clear description of changes

Before opening a PR:

- New content follows established naming conventions (Rimgate_*)
- Balance aligns with RimWorld progression
- XML is clean and commented where appropriate
- C# patches avoid unnecessary Harmony conflicts

For larger contributions, open an issue first to discuss design direction.

Release/build instructions are maintained in [Developer and Contributor Notes](wiki/Developer-and-Contributor-Notes.md).

## 💬 Community & Support

For questions, discussions, or general support, join the official Discord community:

👉 **[Join the Discord Server](https://discord.gg/mhwK2QFNBA)**

For bug reports or feature requests, please open an issue in this repository.

We welcome feedback, contributors, and community discussion across all projects.

## 📜 License ##

See LICENSE file for details.
