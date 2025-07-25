using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Rimgate;

public class HediffCompProperties_GoauldSymbiote : HediffCompProperties
{
    public float lifespanFactor = 1f;

    public HediffCompProperties_GoauldSymbiote() => compClass = typeof(HediffComp_GoauldSymbiote);
}
