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
public class Building_DHD : Building
{
    public static Graphic ActiveGateGraphic = null;

    public Comp_DialHomeDevice DialHomeDevice
    {
        get
        {
            _cachedDialHomeDevice ??= GetComp<Comp_DialHomeDevice>();
            return _cachedDialHomeDevice;
        }
    }

    private Comp_DialHomeDevice _cachedDialHomeDevice;

    static Building_DHD()
    {
        if (ActiveGateGraphic != null)
            return;

        ActiveGateGraphic = new Graphic_Single();

        GraphicRequest request = new GraphicRequest(
            Type.GetType("Graphic_Single"),
            $"Things/Building/Misc/RGDHD_Active",
            ShaderDatabase.DefaultShader,
            new Vector2(2, 2),
            Color.white,
            Color.white,
            new GraphicData(),
            0,
            null,
            null);

        ActiveGateGraphic.Init(request);
    }
  
    public override Graphic Graphic
    {
        get
        {
            Comp_Stargate stargate = DialHomeDevice.GetLinkedStargate();

            return stargate == null || !stargate.IsActive
                ? base.DefaultGraphic
                : ActiveGateGraphic;
        }
    }
}
