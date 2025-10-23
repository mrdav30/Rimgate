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

    private Comp_DHDControl _cachedDialHomeDevice;

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
