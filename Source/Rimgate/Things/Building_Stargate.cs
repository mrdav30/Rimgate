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

    public Comp_StargateControl GateControl
        => _cachedStargate ??= GetComp<Comp_StargateControl>();

    public CompGlower Glower =>
        _cachedGlowComp ??= GetComp<CompGlower>();

    public CompExplosive Explosive => _cachedexplosiveComp ??= GetComp<CompExplosive>();

    private CompPowerTrader _cachedPowerTrader;

    private CompTransporter _cachedTransporter;

    private Comp_StargateControl _cachedStargate;

    private CompGlower _cachedGlowComp;

    private CompExplosive _cachedexplosiveComp;

    private Graphic _activeGraphic;

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

        if (GateControl == null || PowerTrader == null)
            return;

        if (GateControl.HasIris)
        {
            float powerConsumption = -(GateControl.Props.irisPowerConsumption + PowerTrader.Props.PowerConsumption);
            PowerTrader.PowerOutput = powerConsumption;

            GateControl.HasPower = PowerTrader.PowerOn;
            if (!GateControl.HasPower && GateControl.IsIrisActivated)
                GateControl.ToggleIris();
        }
        else
            PowerTrader.PowerOutput = -PowerTrader.Props.PowerConsumption;
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        bool blockInteractions = GateControl != null
            && (GateControl.IsActive
                || GateControl.ExternalHoldCount > 0);
        string why = "RG_StargateHeldCannotReinstall".Translate();

        foreach (Gizmo gizmo in base.GetGizmos())
        {
            if (gizmo is Command_LoadToTransporter
                && !GateControl.IsActive) continue;

            if (gizmo is Designator_Install dInstall)
            {
                if (blockInteractions)
                    dInstall.Disable(why);
                else if (dInstall.Disabled)
                    dInstall.Disabled = false;
                yield return dInstall;
                continue;
            }

            yield return gizmo;
        }
    }

    // override to hide interaction cell
    public override void DrawExtraSelectionOverlays()
    {
        if (def.specialDisplayRadius > 0.1f)
            GenDraw.DrawRadiusRing(Position, def.specialDisplayRadius);

        if (def.drawPlaceWorkersWhileSelected 
            && def.PlaceWorkers != null)
        {
            for (int i = 0; i < def.PlaceWorkers.Count; i++)
            {
                def.PlaceWorkers[i].DrawGhost(def, Position, Rotation, Color.white, this);
            }
        }
    }

    public override string GetInspectString()
    {
        if (GateControl == null)
            return string.Empty;

        StringBuilder sb = new StringBuilder();

        sb.AppendLine(GateControl.GetInspectString());

        if (GateControl.HasIris && PowerTrader != null)
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
