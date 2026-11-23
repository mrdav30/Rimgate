# Sarcophagus Guide

This guide covers the Goa'uld sarcophagus as a player system: unlock flow, intake rules, treatment behavior, power planning, and side-effect management.

For broader unlock flow, see [Research and Progression](research-and-progression.md).  
For power backbone planning, see [Power, Shields, and ZPM Infrastructure](power-shields-and-zpm-infrastructure.md).  
For symbiote/Tretonin interactions, see [Symbiotes and Prim'ta](symbiotes-and-primta.md).

## At a Glance

- `Goa'uld sarcophagus` is an endgame medical structure with very high burst power draw.
- You must analyze a recovered sarcophagus artifact before the research can be completed.
- Non-guests must be assigned to a sarcophagus to use it.
- Missing-part regrowth needs separate research (`sarcophagus regeneration`).
- Full treatments can apply `sarcophagus high` and `sarcophagus addiction`.
- Power loss mid-cycle interrupts treatment and ejects the patient.

## Unlock Path

1. Complete `Goa'uld tech` (`Rimgate_GoauldBasic`).
2. Recover a sarcophagus artifact and analyze it:
   - analysis time: `10` hours
   - catastrophic failure chance: base `25%` (reduced by higher Intellectual)
   - the analyzed artifact is consumed either way
   - catastrophic results do not unlock the research, so you need another artifact copy
3. Complete `sarcophagus` research (`Rimgate_GoauldSarcophagus`, base cost `3500`).
4. Optional upgrades:
   - `sarcophagus regeneration` (`Rimgate_SarcophagusBioregeneration`) for missing-part regrowth
   - `sarcophagus calibration` (`Rimgate_SarcophagusOptimization`) for major power-efficiency gain (includes a techprint gate)

## Build Profile

| Item | Value |
| --- | --- |
| Build cost | `350 steel`, `250 gold`, `75 plasteel`, `150 naquadah`, `10 industrial components`, `25 spacer components` |
| Build work / skill | `25000` work, construction skill `10` |
| Footprint | `2x3` |
| Idle power | `500W` |
| Active power (default) | `4000W` diagnosis, `8000W` healing |
| Wealth impact | Market value `80000` (large raid-point jump) |

## Who Can Use It

- Humanlikes only (no animals/mechs; non-entitled mutants are excluded).
- Medical care cannot be set to `No care`.
- Sarcophagus must be powered, reachable, not burning, not broken, and not already occupied.
- `Allow guests`, `Allow slaves`, and `Allow prisoners` toggles are enforced.
- Non-guests must be assigned to that sarcophagus.
- A pawn can only be assigned to one sarcophagus at a time.
- Ideology edge case: if a pawn's ideology requires scars or blindness, they may not treat those conditions as valid reasons to enter.

## What It Treats

Base behavior includes:

- Tendable injuries and diseases.
- Chronic and permanent conditions (unless explicitly excluded).
- Addictions (except excluded addiction hediffs).
- Immunizable illnesses and other serious medical states.
- Blood loss is stabilized immediately during diagnosis.

Advanced behavior:

- Missing body-part regrowth only activates after `sarcophagus regeneration`.

Notably excluded by default:

- `sterilized`
- childbirth labor hediffs
- `sarcophagus high`
- `sarcophagus addiction`
- `tretonin addiction`
- `Krin'tak sickness`
- `pouch degeneration`
- `symbiote withdrawal`

## Treatment Cycle

1. Intake:
   - patient can self-seek treatment
   - doctors/wardens can auto-rescue/carry valid patients
   - a manual `Rescue to sarcophagus` float-menu option is also available
2. Diagnosis:
   - default max time: `15` seconds, scaled by body size
3. Healing:
   - default max per-hediff time: `30` seconds, scaled by body size and severity
   - each treatable hediff is processed in sequence
4. Discharge:
   - normal completion restores saved needs and applies full post-treatment effects
   - interruption (power loss/abort) ejects patient with interrupted outcome

While in active treatment, food/thirst/hygiene/bladder needs are held full and restored on discharge.

## Power and Stability

- Default power profile:
  - `500W` idle
  - `4000W` diagnosis
  - `8000W` healing
- With `sarcophagus calibration` (default reduction factor `0.65`):
  - diagnosis drops to `2600W`
  - healing drops to `5200W`
- Any power outage during treatment interrupts the cycle and ejects the patient.

Practical rule: treat sarcophagus lines as protected critical infrastructure, not overflow load.

## Side Effects and Dependency Loop

On normal completion, sarcophagus use can apply:

- `sarcophagus high`
- `sarcophagus addiction`
- sarcophagus chemical-need increase

Additional effects:

- Existing sarcophagus addiction can intensify with repeated use.
- Addicted pawns may seek treatment even when lightly injured, driven by chemical need.
- Biological age is pushed back toward adult minimum age.
- With Biotech active, age-reversal demand is reset and hemogen/wraith essence genes are topped up.

Important: the sarcophagus does not clear `sarcophagus addiction` or `tretonin addiction`.

## Automation and Interaction Rules

While a patient is inside a powered sarcophagus:

- doctors/wardens skip normal feeding and fluid-administer jobs
- scheduled surgeries are blocked
- manual tend/strip/arrest/carry float-menu options are suppressed

If you need surgery or manual tending, eject first.

## Common Mistakes

- Forgetting assignment rules and expecting unassigned colonists/prisoners to use it.
- Enabling prisoner/slave usage but not toggling `Allow prisoners` / `Allow slaves`.
- Running sarcophagus cycles on unstable grids and causing repeated interruption ejections.
- Treating it as an addiction cure for sarcophagus/Tretonin dependencies.
- Ignoring its `80000` wealth hit when raid pressure is already high.

## Strategy Checklist

- Put the first sarcophagus on a protected, redundant power lane.
- Use high-Intellectual analysts for artifact study to reduce catastrophic loss risk.
- Assign a small priority pool (critical combat pawns, key specialists) before opening wider access.
- Get `sarcophagus regeneration` before relying on it for limb-loss recovery.
- Get `sarcophagus calibration` before scaling usage across many pawns.
- Pair usage policy with your drug/medical doctrine and tune values in [Mod Settings Reference](mod-settings-reference.md).
