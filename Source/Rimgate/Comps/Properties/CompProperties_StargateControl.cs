using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Rimgate;

public class CompProperties_StargateControl : CompProperties
{
    public bool canHaveIris = true;

    public float irisPowerConsumption = 0;

    public bool explodeOnUse = false;

    public GraphicData puddleGraphicData;

    public GraphicData irisGraphicData;

    public string activeTexture;

    public List<IntVec3> vortexPattern = new List<IntVec3>
    {
        new IntVec3(0,0,1),
        new IntVec3(1,0,1),
        new IntVec3(-1,0,1),
        new IntVec3(0,0,0),
        new IntVec3(1,0,0),
        new IntVec3(-1,0,0),
        new IntVec3(0,0,-1),
        new IntVec3(1,0,-1),
        new IntVec3(-1,0,-1),
        new IntVec3(0,0,-2),
        new IntVec3(1,0,-2),
        new IntVec3(-1,0,-2),
        new IntVec3(0,0,-3)
    };

    public CompProperties_StargateControl() => compClass = typeof(Comp_StargateControl);
}
