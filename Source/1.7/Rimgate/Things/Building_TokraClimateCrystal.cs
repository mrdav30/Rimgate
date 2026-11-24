using RimWorld;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace Rimgate;

public class TokraClimateCrystalDefModExt : DefModExtension
{
    public float maxDeltaPerRareTick = 1.6f;
    public float fuelPerDegree = 0.06f;

    public int softClampMin = -10;
    public int softClampMax = 40;

    // state graphics (texPath, no extensions)
    public string texPathIdle;
    public string texPathHeating;
    public string texPathCooling;
}

public class Building_TokraClimateCrystal : Building_TempControl
{
    private enum ClimateMode : byte
    {
        Idle = 0,
        Heating = 1,
        Cooling = 2
    }

    private CompRefuelable Refuel => _cachedRefuel ??= GetComp<CompRefuelable>();
    private CompTempControl TempControl => _cachedTempControl ??= GetComp<CompTempControl>();
    private TokraClimateCrystalDefModExt Ext => _cachedExt ??= def.GetModExtension<TokraClimateCrystalDefModExt>() ?? new();

    private CompRefuelable _cachedRefuel;
    private CompTempControl _cachedTempControl;
    private TokraClimateCrystalDefModExt _cachedExt;

    // --- Graphic state ---
    private ClimateMode _mode;
    private Graphic _gfxIdle;
    private Graphic _gfxHeat;
    private Graphic _gfxCool;

    public override Graphic Graphic
    {
        get
        {
            // Lazily build per-instance graphics (cheap, happens once)
            EnsureGraphics();

            return _mode switch
            {
                ClimateMode.Heating => _gfxHeat ?? _gfxIdle,
                ClimateMode.Cooling => _gfxCool ?? _gfxIdle,
                _ => _gfxIdle ?? base.Graphic
            };
        }
    }

    public override void TickRare()
    {
        if (!Spawned)
            return;

        if (Refuel == null || !Refuel.HasFuel)
        {
            compTempControl.operatingAtHighPower = false;
            SetMode(ClimateMode.Idle);
            return;
        }

        Room room = this.GetRoom();
        if (room == null || room.TouchesMapEdge)
        {
            compTempControl.operatingAtHighPower = false;
            SetMode(ClimateMode.Idle);
            return;
        }

        float current = room.Temperature;
        float target = compTempControl.TargetTemperature;

        // Discourage insane targets by softly clamping effective target
        if (target < Ext.softClampMin) target = Mathf.Lerp(target, Ext.softClampMin, 0.75f);
        if (target > Ext.softClampMax) target = Mathf.Lerp(target, Ext.softClampMax, 0.75f);

        // If already close enough, do nothing (and consume no fuel).
        float diff = target - current;
        if (Mathf.Abs(diff) < 0.05f)
        {
            compTempControl.operatingAtHighPower = false;
            SetMode(ClimateMode.Idle);
            return;
        }

        // Energy budget for this rare tick.
        // Vanilla uses * 4.16666651f (250 ticks per rare tick / 60 per sec).
        float energyLimit = compTempControl.Props.energyPerSecond * 4.16666651f;
        float delta = GenTemperature.ControlTemperatureTempChange(Position, Map, energyLimit, target);

        // Safety clamp: limit the "feel" per rare tick (helps prevent huge swings in small rooms).
        if (Ext.maxDeltaPerRareTick > 0f)
            delta = Mathf.Clamp(delta, -Ext.maxDeltaPerRareTick, Ext.maxDeltaPerRareTick);

        if (Mathf.Approximately(delta, 0f))
        {
            compTempControl.operatingAtHighPower = false;
            SetMode(ClimateMode.Idle);
            return;
        }

        // Mode based on sign of actual work performed
        SetMode(delta > 0f ? ClimateMode.Heating : ClimateMode.Cooling);

        room.Temperature += delta;
        compTempControl.operatingAtHighPower = true;

        // Fuel cost proportional to actual work done (degrees moved)
        float fuelUsed = Mathf.Abs(delta) * Ext.fuelPerDegree;
        if (fuelUsed > 0f)
            Refuel.ConsumeFuel(fuelUsed);
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var refuelGizmo in Refuel.CompGetGizmosExtra())
            yield return refuelGizmo;

        Command_Action commandAction = new Command_Action();
        commandAction.defaultLabel = "RG_SetTemperatureLabel".Translate();
        commandAction.defaultDesc = "RG_SetTemperatureDescription".Translate();
        commandAction.icon = ContentFinder<Texture2D>.Get("UI/Commands/TempReset");
        commandAction.action = () =>
        {
            Find.WindowStack.Add(new Dialog_SliderWithValue(
                "RG_SetTemperatureTitle".Translate(),
                Ext.softClampMin,
                Ext.softClampMax,
                val => TempControl.TargetTemperature = val,
                Mathf.RoundToInt(TempControl.TargetTemperature),
                unitLabel: "degrees"));
        };
        yield return commandAction;

        if (def.Minifiable && (Faction.IsOfPlayerFaction() || def.building.alwaysUninstallable))
            yield return InstallationDesignatorDatabase.DesignatorFor(def);

        ColorInt? glowerColorOverride = null;
        CompGlower comp = GetComp<CompGlower>();
        if (comp != null && comp.HasGlowColorOverride)
            glowerColorOverride = comp.GlowColor;

        if (!def.building.neverBuildable)
        {
            Command command = BuildCopyCommandUtility.BuildCopyCommand(def, Stuff, StyleSourcePrecept as Precept_Building, StyleDef, styleOverridden: true, glowerColorOverride);
            if (command != null)
                yield return command;
        }

        if (Faction.IsOfPlayerFaction() || def.building.alwaysShowRelatedBuildCommands)
        {
            foreach (Command item in BuildRelatedCommandUtility.RelatedBuildCommands(def))
                yield return item;
        }
    }

    // Override to fix whitespace issue caused by CompTempControl since we don't use CompPower.
    public override string GetInspectString()
    {
        StringBuilder sb = new();
        sb.AppendLine(_mode switch
        {
            ClimateMode.Heating => nameof(ClimateMode.Heating),
            ClimateMode.Cooling => nameof(ClimateMode.Cooling),
            _ => nameof(ClimateMode.Idle)
        });
        sb.AppendLine(TempControl.CompInspectStringExtra().Trim());
        sb.AppendLine(Refuel.CompInspectStringExtra());
        return sb.ToString().Trim();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref _mode, "_mode", ClimateMode.Idle);
    }

    private void EnsureGraphics()
    {
        if (_gfxIdle != null || Ext == null)
            return;

        Shader shader = def.graphicData?.shaderType?.Shader ?? ShaderDatabase.CutoutComplex;
        Vector2 size = def.graphicData?.drawSize ?? Vector2.one;

        // Account for stuff / overrides.
        Color c1 = DrawColor;
        Color c2 = DrawColorTwo;

        if (!Ext.texPathIdle.NullOrEmpty())
            _gfxIdle = GraphicDatabase.Get<Graphic_Single>(Ext.texPathIdle, shader, size, c1, c2);

        if (!Ext.texPathHeating.NullOrEmpty())
            _gfxHeat = GraphicDatabase.Get<Graphic_Single>(Ext.texPathHeating, shader, size, c1, c2);

        if (!Ext.texPathCooling.NullOrEmpty())
            _gfxCool = GraphicDatabase.Get<Graphic_Single>(Ext.texPathCooling, shader, size, c1, c2);
    }

    private void SetMode(ClimateMode next)
    {
        if (_mode == next)
            return;

        _mode = next;

        // Force redraw immediately; O(1) and only when state changes
        if (Spawned)
            Map.mapDrawer.MapMeshDirty(Position, (ulong)MapMeshFlagDefOf.Buildings | (ulong)MapMeshFlagDefOf.Things);
    }
}
