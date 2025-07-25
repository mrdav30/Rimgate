using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Rimgate;

public class CompProperties_Stargate : CompProperties
{
    public CompProperties_Stargate() => this.compClass = typeof(Comp_Stargate);

    public bool canHaveIris = true;
    public bool explodeOnUse = false;
    public string puddleTexture;
    public string irisTexture;
    public Vector2 puddleDrawSize;
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
}
