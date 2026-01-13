using System.Collections.Generic;
using Verse;
using RimWorld;
using UnityEngine;
using Verse.Sound;

namespace Rimgate;

public class Projectile_AdditionalDamage_Ext : DefModExtension
{
    public DamageDef damageDef;
}

public class Projectile_AdditionalDamage : Bullet
{
    public Projectile_AdditionalDamage_Ext Props => _cachedProps ??= def.GetModExtension<Projectile_AdditionalDamage_Ext>();

    private Projectile_AdditionalDamage_Ext _cachedProps;

    protected override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        Map map = base.Map;
        base.Impact(hitThing);
        BattleLogEntry_RangedImpact battleLogEntry_RangedImpact = new BattleLogEntry_RangedImpact(
            launcher,
            hitThing,
            intendedTarget.Thing,
            RimgateDefOf.Gun_Autopistol,
            def,
            targetCoverDef);
        Find.BattleLog.Add(battleLogEntry_RangedImpact);
        if (hitThing != null)
        {
            DamageDef damageDef = def.projectile.damageDef;
            float amount = (float)base.DamageAmount;
            float armorPenetration = base.ArmorPenetration;
            float y = ExactRotation.eulerAngles.y;
            Thing launcher = base.launcher;
            ThingDef equipmentDef = base.equipmentDef;
            DamageInfo dinfo = new DamageInfo(
                damageDef,
                amount,
                armorPenetration,
                y,
                launcher,
                null,
                null,
                DamageInfo.SourceCategory.ThingOrUnknown,
                intendedTarget.Thing);
            hitThing.TakeDamage(dinfo).AssociateWithLog(battleLogEntry_RangedImpact);
            Pawn pawn = hitThing as Pawn;
            if (pawn != null
                && pawn.stances != null
                && pawn.BodySize <= def.projectile.stoppingPower + 0.001f)
            {
                pawn.stances.stagger.StaggerFor(95);
            }

            if (Props == null || Props.damageDef == null)
                return;

            DamageInfo dinfo2 = new DamageInfo(
                Props.damageDef,
                amount / 2,
                armorPenetration,
                y,
                launcher,
                null,
                null,
                DamageInfo.SourceCategory.ThingOrUnknown,
                intendedTarget.Thing);
            hitThing.TakeDamage(dinfo2).AssociateWithLog(battleLogEntry_RangedImpact);
        }
        else
        {
            SoundDefOf.BulletImpact_Ground.PlayOneShot(new TargetInfo(base.Position, map, false));
            FleckMaker.Static(ExactPosition, map, FleckDefOf.ShotHit_Dirt, 1f);
            if (base.Position.GetTerrain(map).takeSplashes)
            {
                FleckMaker.WaterSplash(
                    ExactPosition,
                    map,
                    Mathf.Sqrt((float)base.DamageAmount) * 1f,
                    4f);
            }
        }
    }
}
