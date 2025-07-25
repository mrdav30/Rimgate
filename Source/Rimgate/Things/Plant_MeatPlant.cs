using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Rimgate;

public class Plant_MeatPlant : Plant
{
    public override bool BlightableNow => false;

    public override void CropBlighted() { }

    protected override void Tick()
    {
        base.Tick();
        if (def.plant.neverBlightable)
            return;

        def.plant.neverBlightable = true;
    }
}
