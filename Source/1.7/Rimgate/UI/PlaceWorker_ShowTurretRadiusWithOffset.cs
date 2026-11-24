using Verse;

namespace Rimgate;

public class PlaceWorker_ShowTurretRadiusWithOffset : PlaceWorker
{
    public override AcceptanceReport AllowsPlacing(
      BuildableDef checkingDef,
      IntVec3 loc,
      Rot4 rot,
      Map map,
      Thing thingToIgnore = null,
      Thing thing = null)
    {
        VerbProperties verbProperties = ((ThingDef)checkingDef).building.turretGunDef.Verbs.Find(v =>
            v.verbClass == typeof(Verb_LaunchWithOffset));
        if ((double)verbProperties.range > 0.0)
            GenDraw.DrawRadiusRing(loc, verbProperties.range);

        if ((double)verbProperties.minRange > 0.0)
            GenDraw.DrawRadiusRing(loc, verbProperties.minRange);

        return AcceptanceReport.WasAccepted;
    }
}

