using Verse;

namespace Rimgate;

public class HediffCompProperties_PouchWatcher : HediffCompProperties
{
    // in-game days after adult with no symbiote before degeneration
    public int fatalGraceDays = 60;

    public HediffCompProperties_PouchWatcher() => compClass = typeof(HediffComp_PouchWatcher);
}
