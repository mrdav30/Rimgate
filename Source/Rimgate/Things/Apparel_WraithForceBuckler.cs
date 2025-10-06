using RimWorld;
using VEF.Apparels;
using Verse;

namespace Rimgate;

public class Apparel_WraithForceBuckler : Apparel_Shield
{
    protected override void Tick()
    {
        if (Wearer == null || !Wearer.Spawned || Wearer.Destroyed)
            return;

        base.Tick();
    }
}
