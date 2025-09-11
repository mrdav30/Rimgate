# AI Coding Agent Instructions for Rimgate

## Overview
Rimgate is a mod for Rimworld that introduces new gameplay mechanics, assets, and systems. The project is structured to support modularity and extensibility, with a focus on XML-based definitions and C# assemblies for advanced logic. This document provides guidance for AI coding agents to navigate and contribute effectively to the codebase.

## Codebase Structure
- **1.6/Defs/**: Contains XML files defining game objects, abilities, factions, and other gameplay elements. Subdirectories are organized by category (e.g., `AbilityDefs`, `ThingDefs_Items`).
- **1.6/Patches/**: XML patches for modifying or extending existing game definitions. Each file targets specific mods or core game systems.
- **1.6/Assemblies/**: Compiled C# code in `Rimgate.dll`. The source code is located in `Source/Rimgate/`.
- **Source/Rimgate/**: C# source files implementing advanced game logic. Key files include `Rimgate_DefOf.cs`.
- **Languages/**: Localization files for translating the mod into different languages.
- **Sounds/**: Audio assets categorized by usage (e.g., `Abilities`, `Weapon`).
- **Textures/**: Image assets for UI and in-game objects.

## Development Workflows
### Building the Project
1. Open `Source/Rimgate.sln` in Visual Studio.
2. Build the solution to generate `Rimgate.dll` in `1.6/Assemblies/`.

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
- **Other Mods**: Compatibility patches are provided in `1.6/Patches/` for popular mods like `Vanilla Expanded` and `Dubs Bad Hygiene`.

## Examples
- Adding a new ability: Define it in `1.6/Defs/AbilityDefs/` and reference it in `Rimgate_DefOf.cs`.
- Modifying an existing item: Create a patch in `1.6/Patches/` targeting the item's definition.

## External Dependencies
- **Harmony**: Used for runtime patching in C#.
- **Rimworld Modding Tools**: Recommended for XML validation and debugging.

## Key Files
- `1.6/Defs/`: XML definitions.
- `Source/Rimgate/`: C# source code.
- `1.6/Patches/`: Compatibility patches.

---

This document is a starting point. Update it as the project evolves to ensure it remains accurate and helpful.
