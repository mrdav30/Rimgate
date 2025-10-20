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
    public Comp_DHDControl DHDControl
    {
        get
        {
            _cachedDialHomeDevice ??= GetComp<Comp_DHDControl>();
            return _cachedDialHomeDevice;
        }
    }

    public Graphic ActiveGraphic => _activeGraphic ??= DHDControl?.Props?.activeGraphicData.Graphic;

    private Graphic _activeGraphic;

    private Comp_DHDControl _cachedDialHomeDevice;

    public override Graphic Graphic
    {
        get
        {
            Comp_StargateControl stargate = DHDControl.GetLinkedStargate();
            if (ActiveGraphic == null) return base.DefaultGraphic;
            return stargate == null || !stargate.parent.Spawned || !stargate.IsActive
                ? base.DefaultGraphic
                : ActiveGraphic;
        }
    }

    public static Building_DHD GetDhdOnMap(Map map)
    {
        Building_DHD dhdOnMap = null;
        foreach (Thing thing in map.listerThings.AllThings)
        {
            if (thing is Building_DHD bdhd)
            {
                dhdOnMap = bdhd;
                break;
            }
        }

        return dhdOnMap;
    }
}
