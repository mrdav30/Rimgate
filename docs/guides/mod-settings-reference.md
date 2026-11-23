# Mod Settings Reference

Open **Options → Mod settings → Rimgate** to tune the General, Gate, Clone Pod
and Sarcophagus tabs. Settings are saved by the mod rather than reset when a new
colony starts. The values below are source defaults; an existing configuration
can override them.

## Gates and incidents

The address book defaults to **11 addresses**, with **2 active gate quest sites**.
Asteroid incidents are enabled. The [Gate Network Guide](gate-network-guide.md)
explains how address discovery and abandoning exploration interact with those
limits. Verbose logging is disabled by default; enable it when collecting
diagnostic evidence.

## Cloning

| Setting | Default |
| --- | --- |
| Calibration | 4,500 ticks |
| Base incubation | 120,000 ticks (2 game days) |
| Full / enhanced / reconstruction clone factors | 1.5 / 2 / 2.5 |
| Medicine requirement | 10 |
| Clone incidents | Enabled |
| Minor failures / chance | Enabled / 10% |
| Major failures / chance | Disabled / 1% |
| Clone tattoos / suppress skill loss / generate social relations | Disabled |

The [Cloning Systems guide](cloning-systems.md) explains clone modes, supporting
facilities and interruption behavior. Changing a failure or timing setting does
not remove the mode's research, power or resource prerequisites.

## Sarcophagus

| Setting | Default |
| --- | --- |
| Maximum diagnosis time | 15 |
| Maximum healing time per hediff | 30 |
| Diagnosis / healing power | 4,000 W / 8,000 W |
| Power consumption reduction factor | 0.65 |
| Apply addiction hediff | Enabled |
| Addictiveness / high severity | 1 / 1 |
| Existing addiction severity increase | 0.1 |
| Need level increase | 0.9 |

See the [Sarcophagus Guide](sarcophagus-guide.md) for treatment eligibility,
power interruption and dependency behavior. These settings do not replace its
assignment and prisoner/slave access controls.

## Developer reference

<xref:Rimgate.RimgateModSettings> exposes the fields and persistence method.
The [source](https://github.com/mrdav30/Rimgate/blob/main/Source/Rimgate/RimgateModSettings.cs)
owns defaults, UI bounds and saved keys. Changing a saved key requires deliberate
save/config compatibility handling.
