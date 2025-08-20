using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;

namespace Rimgate;

[StaticConstructorOnStartup]
public class Building_Stargate : Building
{
    public CompPowerTrader PowerComp;

    public Comp_Stargate StargateComp;

    public static Graphic ActiveGateGraphic = null;

    static Building_Stargate()
    {
        if (ActiveGateGraphic != null)
            return;

        ActiveGateGraphic = new Graphic_Single();

        GraphicRequest request = new GraphicRequest(
            Type.GetType("Graphic_Single"),
            $"Things/Building/Misc/RGStargateAncient_Active",
            ShaderDatabase.DefaultShader,
            new Vector2(5.3f, 5.3f),
            Color.white,
            Color.white,
            new GraphicData(),
            0,
            null,
            null);

        ActiveGateGraphic.Init(request);
        ActiveGateGraphic.data.drawOffset = Rimgate_DefOf.Rimgate_Stargate.graphicData.drawOffset;
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        PowerComp = GetComp<CompPowerTrader>();
        StargateComp = GetComp<Comp_Stargate>();

        // fix nullreferenceexception that happens when
        // the innercontainer disappears for some reason
        CompTransporter transComp = GetComp<CompTransporter>();
        if (transComp != null && transComp.innerContainer == null)
        {
            if (RimgateMod.Debug)
                Log.Warning($"Rimgate :: attempting to fix null container for {this.ThingID}");
            transComp.innerContainer = new ThingOwner<Thing>(transComp);
        }

    }

    protected override void Tick()
    {
        base.Tick();

        if (StargateComp == null || PowerComp == null)
            return;

        if (StargateComp.HasIris)
        {
            float powerConsumption = -(StargateComp.Props.irisPowerConsumption + PowerComp.Props.PowerConsumption);
            PowerComp.PowerOutput = powerConsumption;

            StargateComp.HasPower = PowerComp.PowerOn;
            if (!StargateComp.HasPower && StargateComp.IsIrisActivated)
                StargateComp.ToggleIris();
        }
        else
            PowerComp.PowerOutput = -PowerComp.Props.PowerConsumption;
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos())
        {
            if (gizmo is Command_LoadToTransporter)
            {
                if (StargateComp.IsActive)
                    yield return gizmo;
                continue;
            }
            yield return gizmo;
        }
    }

    public override Graphic Graphic
    {
        get
        {
            return !StargateComp.IsActive
                ? base.DefaultGraphic
                : ActiveGateGraphic;
        }
    }

    public override string GetInspectString()
    {
        if (StargateComp == null)
            return string.Empty;

        StringBuilder sb = new StringBuilder();

        sb.AppendLine(StargateComp.GetInspectString());

        if (StargateComp.HasIris && PowerComp != null)
            sb.AppendLine(PowerComp.CompInspectStringExtra());

        return sb.ToString().TrimEndNewlines();
    }
}
