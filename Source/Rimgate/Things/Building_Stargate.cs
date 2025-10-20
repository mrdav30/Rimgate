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
        => _cachedPowerTrader ??= GetComp<CompPowerTrader>();

    public CompTransporter Transporter
        => _cachedTransporter ??= GetComp<CompTransporter>();

    public Comp_StargateControl StargateControl
        => _cachedStargate ??= GetComp<Comp_StargateControl>();

    public CompGlower Glower =>
        _cachedGlowComp ??= GetComp<CompGlower>();

    public CompExplosive Explosive => _cachedexplosiveComp ??= GetComp<CompExplosive>();

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
        bool blockInteractions = StargateControl != null 
            && (StargateControl.IsActive 
                || StargateControl.ExternalHoldCount > 0);
        string why = "RG_StargateHeldCannotReinstall".Translate();

        foreach (Gizmo gizmo in base.GetGizmos())
        {
            if (gizmo is Command_LoadToTransporter 
                && !StargateControl.IsActive) continue;

            if (blockInteractions 
                && gizmo is Designator_Install dInstall)
            {
                dInstall.Disable(why);
                yield return dInstall;
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

    public static Building_Stargate GetStargateOnMap(
        Map map,
        Thing thingToIgnore = null)
    {
        Building_Stargate gateOnMap = null;
        foreach (Thing thing in map.listerThings.AllThings)
        {
            if (thing != thingToIgnore
                && thing is Building_Stargate bsg)
            {
                gateOnMap = bsg;
                break;
            }
        }

        return gateOnMap;
    }
}
