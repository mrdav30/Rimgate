# AI Coding Agent Instructions for Rimgate

## Overview

Rimgate is a mod for Rimworld that introduces new gameplay mechanics, assets, and systems. The project is structured to support modularity and extensibility, with a focus on XML-based definitions and C# assemblies for advanced logic. This document provides guidance for AI coding agents to navigate and contribute effectively to the codebase.

## Codebase Structure

- **Mod/1.6/Defs/**: Contains XML files defining game objects, abilities, factions, and other gameplay elements. Subdirectories are organized by category (e.g., `AbilityDefs`, `ThingDefs_Items`).
- **Mod/1.6/Patches/**: XML patches for modifying or extending existing game definitions. Each file targets specific mods or core game systems.
- **Mod/1.6/Assemblies/**: Compiled C# code in `Rimgate.dll`. The source code is located in `Source/Rimgate/`.
- **Source/Rimgate/**: C# source files implementing advanced game logic. Key files include `Rimgate_DefOf.cs`.
- **Mod/Languages/**: Localization files for translating the mod into different languages.
- **AssetsRaw/**: Raw textures and audio used by the mod. These are bundled into Unity AssetBundles before release.
- **UnityAssetBuilder/**: Dedicated Unity 2022.3 LTS project used to build AssetBundles.
- **Mod/1.6/AssetBundles/**: Generated output folder containing compiled AssetBundles. This folder should NOT contain raw assets.

## Development Workflows

### Building the Project

1. Open `Source/Rimgate.sln` in Visual Studio.
2. Build the solution to generate `Rimgate.dll` and put in `Mod/1.6/Assemblies/`.
3. Run `build-assetbundle.ps1` to generate Unity AssetBundles into `Mod/1.6/AssetBundles/`.

This script:

- Copies raw assets from `AssetsRaw/` into the Unity builder project
- Builds the `rimgate_core` AssetBundle
- Outputs:
  - rimgate_core
  - rimgate_core.manifest
  - AssetBundles
  - AssetBundles.manifest

Do NOT manually copy textures or sounds into `Mod/`.

## Asset Workflow Rules

- All textures and audio must be placed in `AssetsRaw/`.
- Raw assets must NOT be stored inside `Mod/`.
- Always run `build-assetbundle.ps1` after modifying textures or audio.
- The `Mod/1.6/AssetBundles/` directory is generated output and should not be manually edited.
- Never commit generated AssetBundles to source control unless explicitly required.
- Only the `Mod/` directory is uploaded to Steam Workshop.

## 🛠 Unity Builder Requirements

- Unity Version: 2022.3 LTS
- Template: 2D (Built-in Render Pipeline)
- Build Target: StandaloneWindows64
- Compression: ChunkBasedCompression (LZ4)
- No scenes required.

Do not convert the Unity project to URP or HDRP.

### Testing Changes

- XML changes: Load the mod in Rimworld and verify the new definitions or patches.
- C# changes: Attach a debugger to Rimworld to test runtime behavior.

### Debugging

- Use the Rimworld debug console (`~` key) to inspect logs and test in-game changes.
- For C# debugging, attach Visual Studio to the Rimworld process.

## Project-Specific Conventions

- **XML Definitions**: Follow the structure and naming conventions in existing files. Use descriptive names for new definitions.
- **C# Code**: Organize classes by functionality. Use `DefOf` classes to centralize references to XML definitions.
- **Patches**: Ensure compatibility with other mods by targeting specific definitions and using conditional patches where necessary.

## Integration Points

- **Rimworld Core**: The mod integrates with Rimworld's core systems through XML definitions and Harmony patches in C#.
- **Other Mods**: Compatibility patches are provided in `Mod/1.6/Patches/` for popular mods like `Vanilla Expanded` and `Dubs Bad Hygiene`.

## Examples

- Adding a new ability: Define it in `Mod/1.6/Defs/AbilityDefs/` and reference it in `Rimgate_DefOf.cs`.
- Modifying an existing item: Create a patch in `Mod/1.6/Patches/` targeting the item's definition.

## External Dependencies

- **Harmony**: Used for runtime patching in C#.
- **Rimworld Modding Tools**: Recommended for XML validation and debugging.

## Key Files

- `Mod/1.6/Defs/`: XML definitions.
- `Source/Rimgate/`: C# source code.
- `Mod/1.6/Patches/`: Compatibility patches.
- `build-assetbundle.ps1`: Script for building AssetBundles.
- `AssetsRaw/`: Raw assets for textures and audio.

---

This document is a starting point. Update it as the project evolves to ensure it remains accurate and helpful.
