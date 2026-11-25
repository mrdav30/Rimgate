---
name: rimgate-development
description: Develop Rimgate, the RimWorld mod in mrdav30/Rimgate. Applies to its gameplay, defs, compatibility patches, saved state, build and asset tooling, contributor docs and reviews. Specializes the shared rimworld-modding workflow.
---

# Rimgate Development

Use the sibling [rimworld-modding](../rimworld-modding/SKILL.md) workflow and the
repository's [AGENTS.md](../../../AGENTS.md). Resolve repository links from this
versioned skill directory. If loaded through a local junction, resolve its
physical target before following repository links. Rimgate is a public source
repository used locally;
it has not been published as a mod release. Do not infer Workshop distribution.

## Read the owner for the task

- Build, source layout, assets, checks and the 1.6 freeze strategy:
  [Developer and Contributor Notes](../../../docs/guides/developer-and-contributor-notes.md).
- Planning and status: [feature work overview](../../../docs/feature-work/feature-work-overview.md).
  Use the [issue tracker](../../../docs/feature-work/issue-tracker.md) for defects
  and investigations, and the [performance backlog](../../../docs/feature-work/performance-signal-backlog.md)
  for measured signals and measurement reliability. Their intake/evidence rules
  specialize the workflow for the actual game, mod list, settings and scenario.
- Live runtime validation: the same guide's RimBridge workflow,
  [setup-rimbridge-dev.ps1](../../../.assets/scripts/setup-rimbridge-dev.ps1) and
  [rimbridge-smoke.lua](../../../.assets/scripts/rimbridge-smoke.lua). Use the isolated
  experimental `rimgate17` profile after setup with `-RimWorldVersion 1.7`;
  discover the installed bridge's schemas rather than assuming upstream examples
  match the loaded SDK. Setup's 1.7 profile uses graphics-enabled `-NoWindow`
  with a fixed 1280x720 framebuffer for bridge input/capture without desktop
  focus. Do not substitute `-Headless` for rendered checks or use desktop
  automation without coordination. Selection, iris save/load, 120 ticks and
  capture passed. For asset checks, setup deploys the development-only
  `rimgate/asset_report` companion from `.assets/tools/RimgateAssetProbe/`.
  Run [rimbridge-assets-1.7.lua](../../../.assets/scripts/rimbridge-assets-1.7.lua)
  after the smoke case to verify owned assets and real pawn-driven iris
  close/open rendering. Null icons on the two dev-only gate commands are
  inherited defaults; validate the player's iris icons independently. The
  migration plan still owns broader presentation and audible playback. For a
  complete known-address operation, run
  [rimbridge-transit-1.7.lua](../../../.assets/scripts/rimbridge-transit-1.7.lua)
  using the contributor guide's bounded phase/fixture contract. Prepare a lazy
  site through normal registration; real DHD/entry jobs must generate its map
  and transfer the traveler. Test queued/active links and both in-flight save
  boundaries, exact object identity, one deep owner, inventory, redraft intent,
  resumed arrival and closure. Queue references are not ownership; temporary
  Scribe lists must survive the phases that populate and consume them. Use
  reference equality when counting distinct Thing objects, since game equality
  can equate different objects with the same def/ID. Separate developer pending
  cancellation from normal DHD closure and inventory cargo from hauling/container
  coverage. Do not expand this case into a mock game or player dependency.
  Use the contributor guide's medical, utility and loose gate-cargo phases for
  wider acceptance. Probes prepare inputs and observe; real jobs must complete
  cycles. Verify spawned, connected fixtures, default durations, exact treatment
  queues and deep ownership across reload/interruption. Compile changed Lua with
  the actual bridge `compile_lua_file`; its restricted subset is narrower than
  offline MoonSharp parsing. The guide owns syntax limits and phase parameters,
  including blocked-landing, cart-gate and receiver-iris cases with their watch
  cleanup and transit-site value-record boundaries. Use its native-transporter
  and cargo-kind protocols for manifest routing, real capacity splits and nested
  corpse/downed ownership; pending transit ownership is separate from loading.
  Planetary discovery uses
  [rimbridge-discovery-1.7.lua](../../../.assets/scripts/rimbridge-discovery-1.7.lua):
  prepare inputs only; the real decode job must create and persist its quest,
  lazy site and address before real dialing/visitation. Use the guide for the
  presentation scenario, scoped production-audio probes and render diagnostics.
  Review native gate layers at minimum normal zoom as well as ordinary framing;
  valid materials can still render behind the camera or tilted base. Use the
  presentation scenario's bounded approach, powered controller and held dialer
  rather than accepting a missing operator after an uncontrolled lethal opening.
  Use [rimbridge-effects-1.7.lua](../../../.assets/scripts/rimbridge-effects-1.7.lua)
  for a real condition's iris glow, held-gate save/reload and normal end cleanup.
  A saved hold count is not its owner; restore references without pushing again,
  and distinguish missing legacy keys from explicit null or ambiguous ownership.
  The read-only `rimgate/patch_report` proves registration, not feature behavior.
  Keep sampled states distinct from continuous motion and device/listening checks.
  Idle audio mutes while paused; observe normal unpaused frames between separate
  calls and check sampling cadence plus restored gains. Per-voice PCM does not
  establish device/human audibility. An accepted capture request or frame count
  does not establish rendered pixels; preserve failed payloads and stop a bounded
  investigation at the unresolved native stage instead of patching gameplay.
  Hidden rendering is maintained by the private 1.7 companion's public Waylith
  redraw callback. Provider discovery must construct its instance tool owner;
  do not rely on a module initializer in an all-static dynamically loaded tool
  assembly. Verify startup before companion calls and exercise
  [rimbridge-background-1.7.lua](../../../.assets/scripts/rimbridge-background-1.7.lua)
  with redraw suppressed/enabled under bounded engine-only occlusion. End the
  fixture in caller cleanup; never lock the user's session or alter display power
  to reproduce it. Keep rendering, visibility and focus as separate observations.
  Experimental 1.7 setup defaults to `WaylithPack`; retained 1.6 defaults to
  `UnityBundles`. Use `-AssetFormat UnityBundles` explicitly for a 1.7 comparison.
  Supply the chosen `assetFormat` to the Lua comparison and preserve
  direct reports. Native checks must prove the exact mounted pack with no loose
  or bundle fallback. Native is the owner's selected 1.7 development baseline;
  keep the observed softer sampling and outstanding visual/effect/audio checks
  qualified. Successful inventory checks do not establish identical presentation.
- Required dependencies and optional patches:
  [Compatibility Matrix](../../../docs/guides/compatibility-matrix.md), checked against
  Mod/About/About.xml and the affected patch/C# code.
- Gameplay: start with the matching page in [Guides](../../../docs/guides/index.md),
  then trace the implementation. Guides describe intended behavior; source and
  reproduction determine whether a defect exists.

Guides and the generated API site are owned by this repository under `docs/`;
the former wiki submodule has been removed. Build with
`.assets/scripts/build-docs.ps1`, which compiles Rimgate against pinned public
references and runs strict DocFX/site checks. Keep guide links relative with
their `.md` extensions or use API xrefs. Keep authored navigation/config/templates
in source control; `docs/obj/` and restored `.assets/references/` are generated.
The site follows `main` and the latest configured target, with no legacy wiki
mirrors. Preserve user changes and leave work unstaged/uncommitted for review.
Keep status, decisions and evidence in one owning plan or tracker entry. Features
use descriptive plan names rather than bug IDs; preserve issue/performance IDs
through closure. Move completed plans into `docs/feature-work/done/`, keeping
historical observations and unresolved findings qualified. Routine fixes and
documentation edits need no separate plan. Keep guides evergreen and tracking
records repository-only; use GitHub source URLs when a published guide links to
them. Do not add tracking records to DocFX content/navigation or create parallel
feature reports.

## Respect the current boundaries

Only RimWorld 1.6 is declared supported. Source/1.6/Rimgate is the retained source owner;
Mod/1.6 owns its defs, patches, assembly and AssetBundles. Keep generated bundles
in Mod/<version>/AssetBundles, with none in the shared Mod/AssetBundles location.
AssetsRaw owns raw
art/audio. UnityAssetBuilder owns retained 1.6 bundle generation; the experimental
1.7 native helper stages raw inputs and invokes Waylith's baker only in ignored
output. Keep Assets.zip and its stamps/roles/fingerprint sidecars together.
Preserve the built-in
render pipeline, Windows64 target and LZ4 compression. Do not manually copy raw
assets into Mod, manually edit bundles or add generated artifacts to commits.
The four existing 1.6 baseline files were relocated without changing their bytes.
Preserve their bytes during path-only moves; regeneration and publication of
replacement bundles require their own requested scope.

Use the build scripts in `.assets/scripts/` and select the target explicitly when
relevant. Keep PowerShell and Lua tooling there; default build logs stay at the
repository root.
Source/1.7/Rimgate is the separately owned experimental .NET 10/C# 14 port.
Private 1.7 setup reuses the authored 1.6 XML. Correct shared layout symbols in
`Mod/1.6/Defs/MapSites/Structures/` directly instead of adding a version patch.
Use the native layout-content report plus affected site generation; a static
name scan can miss case/rotation failures. Experimental 1.7 corrections belong
in `Mod/1.7/Patches/`, overlaid onto the private XML copy by setup. Keep retained
1.6 content unchanged outside explicitly shared corrections.
Only assembly builds accept `-Experimental -RimWorldVersion 1.7`, with explicit
matching 1.7 dependency paths and ignored output outside Mod. Normal release,
asset and documentation builds still select only supported 1.6. Keep metadata
support bumps and asset-pipeline changes behind actual runtime evidence.
Restore portable dependencies with `restore-build-references.ps1 -RimWorldVersion
<version>` into separate `.assets/references/<version>/` caches. CI compiles the
experimental 1.7 project independently; shipped docs still follow supported 1.6.
Preserve 1.6 source/content while working in 1.7; the migration plan owns its
freeze milestone and remaining acceptance work.

Once 1.7 is accepted, retain the final 1.6 source/content in Git and move active
builds, dependencies, RimBridge validation and documentation to 1.7. Revisit 1.6
only for explicitly scoped maintenance; do not require ongoing 1.6 environment
capture, restoration or live validation after the freeze. The existing local
snapshot is temporary migration insurance, kept untouched outside Git. Preserve
normal player-config backups and post-run checks for whichever dev target is active.

## Follow the affected system

Runtime code lives under Source/<version>/Rimgate, with XML references centralized in
Rimgate_DefOf. Follow the feature's Things/Comps, AI/jobs, World, UI, utilities and
HarmonyPatches together as applicable. Representative stateful owners include
Building_Gate plus WorldComp_GateAddresses, Building_Sarcophagus,
Building_CloningPod, Building_MobileContainer, Building_ZPM and Building_ZPMHousing.

For these systems, check the actual operation, interruption/cleanup and save/load
continuation; use the contributor guide's task-specific checks. Preserve defName,
save keys, pawn/container ownership and cross-map/world references. Verify
research, power, fuel and access prerequisites before diagnosing a blocked action
as a bug. UI string changes also require the corresponding English keyed entries.

Optional XML integrations and C# build references are different contracts. In
particular, the 1.6 project needs Dubs Bad Hygiene reference binaries to compile
although it is not declared as a required runtime mod in About.xml. Check guarded
call paths instead of silently making it a mandatory player dependency.
