using RimWorld;

namespace Rimgate;

public class Plant_MeatPlant : Plant
{
    public override bool BlightableNow => false;

    public override void CropBlighted() { }

    public override void TickLong()
    {
        if (!Spawned || Map == null) return;
        base.TickLong();
    }

    public override string GetInspectString()
    {
        if (!Spawned || Map == null) return string.Empty;
        return base.GetInspectString();
    }
}
