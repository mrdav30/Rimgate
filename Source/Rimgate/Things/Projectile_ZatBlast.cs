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
        ZatBlastImpact(hitThing);
    }

    protected void ZatBlastImpact(Thing hitThing)
    {
        if (Props == null || hitThing == null) return;

        Corpse corpse = hitThing as Corpse ?? (hitThing as Pawn)?.Corpse;
        if(corpse != null)
        {
            // Destroy any dead corpse regardless of whether or not it was hit by a zat gun.
            corpse.Destroy();
            return;
        }

        if (hitThing is not Pawn hitPawn) return;

        Hediff zatShocked = hitPawn.health?.hediffSet?.GetFirstHediffOfDef(RimgateDefOf.Rimgate_ZatShock);

        float randomSeverity = Props.severityRange.RandomInRange;
        if (zatShocked != null)
        {
            // If the pawn has already been shot with the zat gun, the second shot is fatal.
            if (RimgateMod.Debug)
                Messages.Message("Rimgate :: Killing " + hitPawn.Name + " because of 2nd zat blast.", MessageTypeDefOf.NegativeEvent);
            hitPawn.Kill(null);
        }
        else
        {           
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

            hediff = HediffMaker.MakeHediff(RimgateDefOf.Rimgate_ZatShock, hitPawn);
            hediff.Severity = randomSeverity;
            hitPawn.health.AddHediff(hediff);
        }
    }
}
