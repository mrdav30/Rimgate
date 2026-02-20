using RimWorld;
using UnityEngine;
using Verse;

namespace Rimgate;

public class Projectile_ZatBlast_Ext : DefModExtension
{
    public float oddsOfCatatonia = 0.15f;

    public float oddsOfHangover = 0.20f;

    public float oddsOfZatShock = 0.55f;

    public bool enableCorpseDisintegration = true;
}

public class Projectile_ZatBlast : Bullet
{
    public Projectile_ZatBlast_Ext Props => _cachedProps ??= def.GetModExtension<Projectile_ZatBlast_Ext>();

    private Projectile_ZatBlast_Ext _cachedProps;

    private static readonly TraitDef PsychicSensitivityTraitDef = TraitDef.Named("PsychicSensitivity");

    protected override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        base.Impact(hitThing, blockedByShield);
        if (blockedByShield) return;
        if (Props == null || hitThing == null) return;
        ZatBlastImpact(hitThing);
    }

    private void ZatBlastImpact(Thing hitThing)
    {
        Pawn hitPawn = hitThing is Corpse corpse
            ? corpse.InnerPawn
            : hitThing as Pawn;

        // zat doesn't affect non-flesh things or non-pawns
        if (hitPawn == null || !hitPawn.RaceProps.IsFlesh) return;

        // If the pawn already has zat shock, they can potentially die from additional blasts.
        if (hitPawn.TryGetHediffOf(RimgateDefOf.Rimgate_ZatShock, out Hediff h))
        {
            if (Props.enableCorpseDisintegration && hitThing is Corpse && TryDisintegrateCorpse(hitThing as Corpse))
                return;

            float max = h.def.maxSeverity;
            h.Severity = Mathf.Min(h.Severity + 1f, max);
            if (h.Severity >= max)
                hitPawn.Kill(null);
            return;
        }

        // If the pawn is psychically sensitive, or has bad luck, put them in a state of catatonia.
        int psychicSensitivity = 0;
        if (hitPawn.story?.traits != null
            && hitPawn.story.traits.HasTrait(PsychicSensitivityTraitDef))
        {
            Trait psychicSensitivityTrait = hitPawn.story.traits.GetTrait(PsychicSensitivityTraitDef);
            psychicSensitivity = psychicSensitivityTrait.Degree;
        }

        if (psychicSensitivity > 0 && Rand.Chance(Props.oddsOfCatatonia))
        {
            hitPawn.health?.AddHediff(HediffDefOf.CatatonicBreakdown);
            return;
        }

        if (psychicSensitivity < -1 && Rand.Chance(Props.oddsOfHangover))
        {
            hitPawn.health?.AddHediff(HediffDefOf.PsychicHangover);
            return;
        }

        if (!Rand.Chance(Props.oddsOfZatShock))
            return;

        var shock = hitPawn.health?.AddHediff(RimgateDefOf.Rimgate_ZatShock);
        // small chance of less severe initial shock or none at all
        shock.Severity = Rand.Chance(0.5f)
            ? 1f
            : Rand.Chance(0.5f)
                ? 0.5f
                : 0f;
    }

    private bool TryDisintegrateCorpse(Corpse corpse)
    {
        if (corpse == null) return false;
        if (corpse.GetRotStage() != RotStage.Fresh) return false;

        if (RimgateMod.Debug)
            Messages.Message($"Rimgate :: {Label} disintegrated {corpse.InnerPawn.LabelShort}.", MessageTypeDefOf.NeutralEvent);

        corpse.Destroy();
        if (corpse.Spawned)
            FleckMaker.ThrowExplosionCell(corpse.Position, corpse.Map, FleckDefOf.ExplosionFlash, Color.blue);
        return true;
    }
}
