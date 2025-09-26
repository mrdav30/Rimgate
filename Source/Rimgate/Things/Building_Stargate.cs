using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;
using static HarmonyLib.Code;

namespace Rimgate;

public class Building_Stargate : Building
{
    public CompPowerTrader PowerTrader
    {
        get
        {
            _cachedPowerTrader ??= GetComp<CompPowerTrader>();
            return _cachedPowerTrader;
        }
    }

    public CompTransporter Transporter
    {
        get
        {
            _cachedTransporter ??= GetComp<CompTransporter>();
            return _cachedTransporter;
        }
    }

    public Comp_StargateControl StargateControl
    {
        get
        {
            _cachedStargate ??= GetComp<Comp_StargateControl>();
            return _cachedStargate;
        }
    }

    public CompGlower Glower
    {
        get
        {
            _cachedGlowComp ??= GetComp<CompGlower>();
            return _cachedGlowComp;
        }
    }

    public CompExplosive Explosive
    {
        get
        {
            _cachedexplosiveComp ??= GetComp<CompExplosive>();
            return _cachedexplosiveComp;
        }
    }

    public Graphic ActiveGraphic => _activeGraphic ??= StargateControl?.Props?.activeGraphicData.Graphic;

    private CompPowerTrader _cachedPowerTrader;

    private CompTransporter _cachedTransporter;

    private Comp_StargateControl _cachedStargate;

    private CompGlower _cachedGlowComp;

    private CompExplosive _cachedexplosiveComp;

    private Graphic _activeGraphic;

    public override Graphic Graphic
    {
        get
        {
            if (ActiveGraphic == null) return base.DefaultGraphic;
            return !StargateControl.IsActive
                ? base.DefaultGraphic
                : ActiveGraphic;
        }
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);

        // fix nullreferenceexception that happens when
        // the innercontainer disappears for some reason
        if (Transporter != null && Transporter.innerContainer == null)
        {
            if (RimgateMod.Debug)
                Log.Warning($"Rimgate :: attempting to fix null container for {this.ThingID}");
            _cachedTransporter.innerContainer = new ThingOwner<Thing>(_cachedTransporter);
        }

    }

    protected override void Tick()
    {
        base.Tick();

        if (StargateControl == null || PowerTrader == null)
            return;

        if (StargateControl.HasIris)
        {
            float powerConsumption = -(StargateControl.Props.irisPowerConsumption + PowerTrader.Props.PowerConsumption);
            PowerTrader.PowerOutput = powerConsumption;

            StargateControl.HasPower = PowerTrader.PowerOn;
            if (!StargateControl.HasPower && StargateControl.IsIrisActivated)
                StargateControl.ToggleIris();
        }
        else
            PowerTrader.PowerOutput = -PowerTrader.Props.PowerConsumption;
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos())
        {
            if (gizmo is Command_LoadToTransporter)
            {
                if (StargateControl.IsActive)
                    yield return gizmo;
                continue;
            }
            yield return gizmo;
        }
    }

    public override string GetInspectString()
    {
        if (StargateControl == null)
            return string.Empty;

        StringBuilder sb = new StringBuilder();

        sb.AppendLine(StargateControl.GetInspectString());

        if (StargateControl.HasIris && PowerTrader != null)
            sb.AppendLine(PowerTrader.CompInspectStringExtra());

        return sb.ToString().TrimEndNewlines();
    }
}
