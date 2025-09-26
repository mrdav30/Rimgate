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

public class Building_DHD : Building
{
    public Comp_DHDControl DialHomeDevice
    {
        get
        {
            _cachedDialHomeDevice ??= GetComp<Comp_DHDControl>();
            return _cachedDialHomeDevice;
        }
    }

    public Graphic ActiveGraphic => _activeGraphic ??= DialHomeDevice?.Props?.activeGraphicData.Graphic;

    private Graphic _activeGraphic;

    private Comp_DHDControl _cachedDialHomeDevice;

    public override Graphic Graphic
    {
        get
        {
            Comp_StargateControl stargate = DialHomeDevice.GetLinkedStargate();
            if (ActiveGraphic == null) return base.DefaultGraphic;
            return stargate == null || !stargate.parent.Spawned || !stargate.IsActive
                ? base.DefaultGraphic
                : ActiveGraphic;
        }
    }
}
