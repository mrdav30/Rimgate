# Rimgate: The Dwarfgate Initiative #

Rimgate: The Dwarfgate Initiative is a large-scale Stargate-inspired expansion for RimWorld, introducing an ancient planetary gate network, alien technologies, and faction-driven conflict centered around mysterious Dwarfgates.

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

- RimWorld (latest stable version)
- DLC compatibility may vary by module

Detailed compatibility notes are available on the Workshop page.

## 🧪 Development Notes ##

This project integrates:

- Custom ThingDefs, ResearchDefs, and QuestDefs
- Custom CompProperties and ModExtensions
- Harmony patches where required
- Modular faction definitions
- Custom graphics and VFX

The goal is long-term maintainability and expandability with a focus on performance.

## 🤝 Contributing ##

Contributions are welcome.

If you'd like to contribute:

1. Fork the repository
2. Create a feature branch
3. Follow RimWorld XML and C# conventions
4. Submit a pull request with a clear description of changes

Please ensure:

- New content follows established naming conventions (Rimgate_*)
- Balance aligns with RimWorld progression
- XML is clean and commented where appropriate
- C# patches avoid unnecessary Harmony conflicts

For larger contributions, open an issue first to discuss design direction.

## 🚀 Release Process ##

1. Update XML and/or C#.
2. Update textures or audio in `AssetsRaw/`.
3. Run the full release build:

    ```powershell
    .\build-release.ps1
   ```

4. Verify output files:
   - `Mod/1.6/Assemblies/Rimgate.dll`
   - `Mod/1.6/AssetBundles/rimgate_core`
5. Upload the `Mod/` folder to Steam Workshop.

Do not upload:

- UnityAssetBuilder/
- Source/
- AssetsRaw/
- Raw textures or audio

## 💬 Community & Support

For questions, discussions, or general support, join the official Discord community:

👉 **[Join the Discord Server](https://discord.gg/mhwK2QFNBA)**

For bug reports or feature requests, please open an issue in this repository.

We welcome feedback, contributors, and community discussion across all projects.

## 📜 License ##

See LICENSE file for details.
