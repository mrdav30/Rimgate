using RimWorld;

namespace Rimgate;

public class Plant_MeatPlant : Plant
{
    public override bool BlightableNow => false;

    public override void CropBlighted() { }
}
