using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Rimgate;

// Produces nothing. Always "can generate" so it can be used as a legit option bucket.
public class ThingSetMaker_None : ThingSetMaker
{
    protected override bool CanGenerateSub(ThingSetMakerParams parms) => true;
    protected override void Generate(ThingSetMakerParams parms, List<Thing> outThings) { }
    protected override IEnumerable<ThingDef> AllGeneratableThingsDebugSub(ThingSetMakerParams parms)
    {
        yield break;
    }
}