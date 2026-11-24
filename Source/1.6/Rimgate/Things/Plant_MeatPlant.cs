using RimWorld;

namespace Rimgate;

public class Plant_MeatPlant : Plant
{
    public override bool BlightableNow => false;

    public override bool DyingBecauseExposedToLight => false;

    public override bool DyingBecauseExposedToVacuum => false;

    public override bool DyingBecauseOfTerrainTags => false;

    public override void CropBlighted() { }

    public override void TickLong()
    {
        if (!Spawned || Destroyed || Map == null) return;
        base.TickLong();
    }
}
