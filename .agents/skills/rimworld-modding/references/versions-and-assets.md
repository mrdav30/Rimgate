# Version and asset boundaries

Read this when changing version selection, engine-facing code, build references
or asset packaging. Inspect the target installation before adapting these rules
to a particular game release.

## Select a target without claiming support

Treat these as separate contracts: declared supported versions, LoadFolders
selection, source/project ownership, referenced game/mod binaries, generated
outputs and runtime/save validation. Keep them consistent. A version parameter
selects configured work; it must not relabel an old assembly as compatible with
an unvalidated engine or game API.

Fail on an unconfigured target before copying content or invoking a toolchain.
Resolve repository inputs relative to the scripts/project rather than the caller's
working directory. Check child-tool exit status and expected outputs; do not find
the newest DLL somewhere in a bin tree and assume it belongs to this build.

Keep existing save/def identities and version-owned outputs intact when adding
another target. Inspect every loaded shared root: a root assembly, patch, def or
bundle may still be loaded alongside a version folder. Do not duplicate the same
content across both loaded locations. Separate source trees when APIs diverge or
an older target is frozen; share code only where its contract is truly shared.

## Keep asset provenance and loading explicit

Find the real source assets, import settings, builder/editor version, build target,
compression, generated outputs and runtime paths. Preserve the repository's
source/generated boundary. Raw textures in a conventional RimWorld mod can be
legitimate; an AssetBundle-only policy belongs to repositories that require it.

The editor used to produce bundles and the engine consuming them are separate
compatibility questions. Do not choose an editor version from stale prose, or
assume that changing engines removes the need for the existing asset tools.
Build with the configured toolchain and verify actual loading in the target game.
Keep player data and the baseline artifacts recoverable during experiments.

## Investigate an engine transition incrementally

Start with a minimal mod list and one complete production behavior. Verify
startup/def loading, assets, patched methods, gameplay and saved-state continuity
at the affected boundaries. Add optional integrations after the baseline works.
Keep support declarations behind the required compatibility evidence.

Ludeon's [Waylith/1.7 announcement](https://store.steampowered.com/news/app/294100/view/672882362547178633)
describes broad mod compatibility and asks authors to test before marking mods
compatible. It is useful context, not evidence for a particular mod. Consult the
current upstream guidance and installed build during an actual migration; this
reference does not prescribe a 1.7 framework, replacement API or asset format.
