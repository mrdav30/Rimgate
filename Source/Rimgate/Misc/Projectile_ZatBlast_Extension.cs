using RimWorld;
using Verse;

namespace Rimgate;

public class Projectile_ZatBlast_Extension : DefModExtension
{
    public float addHediffChance = 0.82f;

    public FloatRange severityRange = new FloatRange(0.15f, 0.30f);
}
