using System.Collections.Generic;
using Verse;
using RimWorld;

namespace Rimgate;

public class Projectile_ZatBlast : Bullet
{
    public Projectile_ZatBlast_Extension Props => new Projectile_ZatBlast_Extension();

    protected override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        base.Impact(hitThing, false);
        this.ZatBlastImpact(hitThing);
    }

    protected void ZatBlastImpact(Thing hitThing)
    {
        if (Props == null || hitThing == null || hitThing is not Pawn hitPawn)
            return;

        Hediff zatShocked = hitPawn.health?.hediffSet?.GetFirstHediffOfDef(Rimgate_DefOf.Rimgate_ZatShock);

        float randomSeverity = Rand.Range(0.15f, 0.30f);
        if (zatShocked != null)
        {
            // If the pawn has already been shot with the zat gun, the second shot is fatal.
            if (RimgateMod.debugLogging)
                Messages.Message("Rimgate :: Killing " + hitPawn.Name + " because of 2nd zat blast.", MessageTypeDefOf.NegativeEvent);
            hitPawn.Kill(null);
        }
        else
        {
            // Destroy any dead corpse regardless of whether or not it was hit by a zat gun.
            if (hitPawn.Dead)
            {
                hitPawn.Corpse.Destroy();
                return;
            }

            float rand = Rand.Value;
            if (rand > Props.addHediffChance)
                return;

            Hediff hediff;

            // If the pawn is psychically sensitive, or has bad luck, put them in a state of catatonia.
            int psychicSensitivity = 0;
            if (hitPawn.story?.traits != null
                && hitPawn.story.traits.HasTrait(TraitDef.Named("PsychicSensitivity")))
            {
                Trait psychicSensitivityTrait = hitPawn.story.traits.GetTrait(TraitDef.Named("PsychicSensitivity"));
                psychicSensitivity = psychicSensitivityTrait.Degree;
            }

            float oddsOfCatatonia = 0.15f;
            float oddsOfHangover = 0.20f;
            if (psychicSensitivity > 0 || Rand.Value <= oddsOfCatatonia)
                hitPawn.health?.AddHediff(HediffDefOf.CatatonicBreakdown, null, null);

            // Chance of just having a "psycic hangover."
            if (psychicSensitivity < -1 || Rand.Value < oddsOfHangover)
                hitPawn.health?.AddHediff(HediffDefOf.PsychicHangover, null, null);

            hediff = HediffMaker.MakeHediff(Rimgate_DefOf.Rimgate_ZatShock, hitPawn);
            hediff.Severity = randomSeverity;
            hitPawn.health.AddHediff(hediff);
        }
    }
}
