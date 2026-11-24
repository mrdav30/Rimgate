# Troubleshooting and Known Issues

This page is a player-facing troubleshooting reference for common Rimgate problems, using the same wording you see in-game wherever possible.

For optional patch integrations, see [Compatibility Matrix](compatibility-matrix.md).  
For gate-specific flow, see [Gate Network Guide](gate-network-guide.md).  
For sarcophagus and cloning details, see [Sarcophagus Guide](sarcophagus-guide.md) and [Cloning Systems](cloning-systems.md).

## At a Glance

- Most issues are one of: dependency/load problems, missing power/fuel, missing research, or pathing.
- Read the exact in-game failure text first (`Cannot dial`, `Cannot decode`, `Cannot use`, etc.).
- Rimgate systems are heavily progression-gated; many actions stay locked until analysis + research are complete.
- If you use large modlists, check [Compatibility Matrix](compatibility-matrix.md) before assuming a save bug.

## 60-Second Triage

1. Confirm required dependencies are enabled:
   - `Harmony`
   - `Vanilla Expanded Framework`
   - `Royalty`
   - `Ideology`
   - `Odyssey`
   - `Biotech`
   - `Anomaly`
2. Confirm you are on RimWorld `1.6` and only running one copy of Rimgate.
3. Check power/fuel on the affected building and confirm a pawn can reach its interaction cell.
4. Check research/analysis prerequisites for the blocked action.
5. Save, fully restart RimWorld, and reload once before deeper debugging.

## Symptom-to-Fix Matrix

| Symptom (What You See) | Likely Cause | What To Do |
| --- | --- | --- |
| Rimgate missing from mod list, or immediate startup red errors | Missing dependency or broken manual install layout | Re-enable required dependencies. If installing locally, use the repository's `Mod` folder: `About` and `LoadFolders.xml` are at its root; `Defs`, `Patches`, `AssetBundles` and `Assemblies/Rimgate.dll` are under `1.6`. |
| `Cannot place ...` gate errors | One-gate-per-map rule, address book cap, or pocket map placement | Place only one Dwarfgate per map, avoid pocket maps, and free address slots if the address book is full. |
| `Cannot decode: ...` | `glyph deciphering` not researched, no path, or address book full | Research `glyph deciphering`, make parchment reachable, and clear address space if needed. |
| `Cannot dial: ...` | Unlinked DHD, no power, gate already active/incoming, no destination, or missing mod equipment | Ensure DHD is linked and powered, gate is idle, destinations exist, and `gate modification equipment` is active for orbit dialing. |
| `Cannot enter portal: ...` | Gate inactive, iris closed, or no path | Wait for full dial lock, open iris, and clear pathing to gate. |
| ZPM shows `Inert` or appears non-functional | `ZPM core` not completed | Analyze a ZPM and finish `ZPM core` research before expecting power output. |
| ZPM raid pressure does not stop | Active broadcasting ZPM without masking | Use a powered `subspace shroud emitter` linked to at least one placed ZPM. |
| `Subspace masking: offline (not linked)` | Shroud has no valid ZPM facility link | Rebuild/rewire layout so the shroud is facility-linked to a placed ZPM. |
| `Cannot take ... to sarcophagus` or treatment refusal text | Access toggles, assignment rules, medical-care threshold, or blocked trait/hediff/race | Check sarcophagus guest/prisoner/slave toggles, assignment, and medical care level; then verify compatibility-specific restrictions. |
| `Cannot use (...)` on cloning pod / calibration failures | No power, no path, missing staged host/research, or Medicine requirement not met | Restore power/pathing, stage a valid host, complete required research, and verify the current Medicine skill requirement in mod settings. |
| `Cannot shoot: Needs more ammo/fuel` on turrets | Refuelable resource missing | Refill turret fuel resource (`steel` or `naquadah` depending on turret) and ensure manned turrets have operators. |
| Cart push/load failures (`no fuel`, `cannot reach destination`, `can't finish loading`) | Fuel empty, target unreachable, or source items became unavailable | Refuel cart, choose reachable destinations, and re-issue load after stock changes. |

## If Problems Start After an Update

1. Make a new backup save before testing.
2. Restart RimWorld fully (not just to main menu) and reload.
3. Confirm there are no duplicate Rimgate copies (for example Steam + old local folder).
4. If the same issue persists, test once with only Rimgate + required dependencies.
5. Add your optional mods back in batches to isolate conflicts.

## Log Collection for Bug Reports

When reporting issues, include:

- RimWorld version and active DLC list
- full active mod list (load order)
- exact in-game message text
- reproduction steps
- `Player.log` excerpt around the failure

Windows default `Player.log` location:

`%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log`

Tip: You can enable `verbose logging` in Rimgate mod settings when collecting diagnostic info.

## Known Issues (Confirmed)

For current technical findings, reproduction evidence and dispositions, see the
[issue tracker](https://github.com/mrdav30/Rimgate/blob/main/docs/feature-work/issue-tracker.md).
Investigations needing reproduction are labeled separately from confirmed
defects. Use the triage and log-collection steps above when reporting a problem.

## Known Behavior Notes (Often Reported as Bugs)

- Multiple powered `subspace phase diverters` are currently treated as a binary requirement for recharge (having more than one does not currently stack recharge speed).
- Short-circuit protection excludes standalone `zero point module` batteries; `ZPM containment housing` does not use the same exemption.
- `durable wall` and `durable door` structures are intentionally non-deconstructible once built.
- Dwarfgate placement is intentionally limited to one per map.
