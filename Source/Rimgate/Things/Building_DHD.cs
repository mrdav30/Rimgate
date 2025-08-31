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
    public Comp_DialHomeDevice DialHomeDevice
    {
        get
        {
            _cachedDialHomeDevice ??= GetComp<Comp_DialHomeDevice>();
            return _cachedDialHomeDevice;
        }
    }

    public Graphic ActiveGateGraphic
    {
        get
        {
            _activeGraphic ??= GraphicDatabase.Get<Graphic_Single>(
                _cachedDialHomeDevice.Props.activeTexture,
                ShaderDatabase.DefaultShader,
                new Vector2(2, 2),
                Color.white,
                Color.white,
                new());

            return _activeGraphic;
        }
    }

    private Graphic _activeGraphic;

    private Comp_DialHomeDevice _cachedDialHomeDevice;

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
