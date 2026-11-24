using System.Collections.Generic;
using Verse;

namespace Rimgate;

public class HediffCompProperties_ReplacesHediffs : HediffCompProperties
{
    public List<HediffDef> hediffDefs;

    public bool spawnAnyThings;

    public HediffCompProperties_ReplacesHediffs() => compClass = typeof(HediffComp_ReplacesHediffs);
}
