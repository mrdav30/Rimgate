using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Rimgate;

public class HediffCompProperties_GoauldSymbiote : HediffCompProperties
{
    public float lifespanFactor = 4.0f;              // 4x slower aging while symbiote is active
    public float removalAgingMultiplier = 2.5f;      // 2.5x age penalty on removal

    public HediffCompProperties_GoauldSymbiote() => compClass = typeof(HediffComp_GoauldSymbiote);
}
