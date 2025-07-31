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
}
