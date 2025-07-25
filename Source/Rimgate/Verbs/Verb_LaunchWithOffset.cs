using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Rimgate;

public class Verb_LaunchWithOffset : Verb_Shoot
{
    protected override bool TryCastShot()
    {
        if (currentTarget.HasThing && currentTarget.Thing.Map != caster.Map)
            return false;

        ThingDef projectile1 = Projectile;
        if (projectile1 == null)
            return false;

        bool shootLineFromTo = TryFindShootLineFromTo(caster.Position, currentTarget, out ShootLine shootLine);
        if (verbProps.stopBurstWithoutLos && !shootLineFromTo)
            return false;

        if (EquipmentSource != null)
        {
            EquipmentSource.GetComp<CompChangeableProjectile>()?.Notify_ProjectileLaunched();
            EquipmentSource.GetComp<CompApparelReloadable>()?.UsedOnce();
        }

        lastShotTick = Find.TickManager.TicksGame;
        Thing thing1 = caster;
        Thing thing2 = EquipmentSource;
        CompMannable comp = ThingCompUtility.TryGetComp<CompMannable>(caster);
        if (comp != null && comp.ManningPawn != null)
        {
            thing1 = comp.ManningPawn;
            thing2 = caster;
        }

        Vector3 vector3 = Vector3.MoveTowards(caster.DrawPos, currentTarget.CenterVector3, 4f);
        Projectile projectile2 = (Projectile)GenSpawn.Spawn(
            projectile1,
            shootLine.Source,
            caster.Map,
            WipeMode.Vanish);
        if (verbProps.ForcedMissRadius > 0.5)
        {
            float forcedMissRadius = verbProps.ForcedMissRadius;
            if (thing1 != null && thing1 is Pawn pawn)
                forcedMissRadius *= verbProps.GetForceMissFactorFor(thing2, pawn);

            float adjustedForcedMiss = VerbUtility.CalculateAdjustedForcedMiss(forcedMissRadius, currentTarget.Cell - caster.Position);
            if (adjustedForcedMiss > 0.5)
            {
                int index = Rand.Range(0, GenRadial.NumCellsInRadius(adjustedForcedMiss));
                if (index > 0)
                {
                    IntVec3 c = currentTarget.Cell + GenRadial.RadialPattern[index];
                    if (RimgateMod.debugLogging && DebugViewSettings.drawShooting)
                        Utils.ThrowDebugText("Rimgate :: ToRadius: ", c, caster.Map);

                    ProjectileHitFlags projectileHitFlags = ProjectileHitFlags.NonTargetWorld;
                    if (Rand.Chance(0.5f))
                        projectileHitFlags = ProjectileHitFlags.All;

                    if (!canHitNonTargetPawnsNow)
                        projectileHitFlags = (ProjectileHitFlags)((int)projectileHitFlags & -3);

                    projectile2.Launch(
                        thing1,
                        vector3,
                        c,
                        currentTarget,
                        projectileHitFlags,
                        preventFriendlyFire,
                        thing2,
                        null);

                    return true;
                }
            }
        }

        ShotReport shotReport = ShotReport.HitReportFor(caster, this, this.currentTarget);
        Thing randomCoverToMissInto = shotReport.GetRandomCoverToMissInto();
        ThingDef def = randomCoverToMissInto?.def;
        if (!Rand.Chance(shotReport.AimOnTargetChance_IgnoringPosture))
        {
            shootLine.ChangeDestToMissWild(shotReport.AimOnTargetChance_StandardTarget, true, caster.Map);
            if (RimgateMod.debugLogging && DebugViewSettings.drawShooting)
            {
                Utils.ThrowDebugText("Rimgate :: ToWild "
                    + (canHitNonTargetPawnsNow ? "chntp " : "")
                    + "WildDest " + shootLine.Dest, caster.DrawPos, caster.Map);
            }

            ProjectileHitFlags projectileHitFlags = ProjectileHitFlags.NonTargetWorld;
            if (Rand.Chance(0.5f) && canHitNonTargetPawnsNow)
                projectileHitFlags = (ProjectileHitFlags)((int)projectileHitFlags | 2);
            projectile2.Launch(
                thing1,
                vector3,
                shootLine.Dest,
                currentTarget,
                projectileHitFlags,
                preventFriendlyFire,
                thing2,
                def);

            return true;
        }
        if (currentTarget.Thing != null
            && currentTarget.Thing.def.category == ThingCategory.Pawn
            && !Rand.Chance(shotReport.PassCoverChance))
        {
            if (RimgateMod.debugLogging && DebugViewSettings.drawShooting)
            {
                Utils.ThrowDebugText("Rimgate :: ToCover "
                    + (canHitNonTargetPawnsNow ? "chntp " : "")
                    + "CoverDest\", randomCoverToMissInto.Position", caster.DrawPos, caster.Map);
            }

            ProjectileHitFlags projectileHitFlags = ProjectileHitFlags.NonTargetWorld;
            if (canHitNonTargetPawnsNow)
                projectileHitFlags = (ProjectileHitFlags)((int)projectileHitFlags | 2);

            projectile2.Launch(
                thing1,
                vector3,
                randomCoverToMissInto,
                currentTarget,
                projectileHitFlags,
                preventFriendlyFire,
                thing2,
                def);

            return true;
        }

        ProjectileHitFlags projectileHitFlags1 = ProjectileHitFlags.IntendedTarget;
        if (canHitNonTargetPawnsNow)
            projectileHitFlags1 = (ProjectileHitFlags)((int)projectileHitFlags1 | 2);

        if (!currentTarget.HasThing || currentTarget.Thing.def.Fillage == FillCategory.Full)
            projectileHitFlags1 = (ProjectileHitFlags)((int)projectileHitFlags1 | 4);

        if (RimgateMod.debugLogging && DebugViewSettings.drawShooting)
            Utils.ThrowDebugText("Rimgate :: ToHit" + (canHitNonTargetPawnsNow ? "\nchntp" : ""), caster.DrawPos, caster.Map);

        if (currentTarget.Thing != null)
        {
            projectile2.Launch(thing1,
                vector3,
                currentTarget,
                currentTarget,
                projectileHitFlags1,
                preventFriendlyFire,
                thing2,
                def);

            if (RimgateMod.debugLogging && DebugViewSettings.drawShooting)
                Utils.ThrowDebugText("Rimgate :: HitDest " + currentTarget.Cell, caster.DrawPos, caster.Map);
        }
        else
        {
            projectile2.Launch(
                thing1,
                vector3,
                shootLine.Dest,
                currentTarget,
                projectileHitFlags1,
                preventFriendlyFire,
                thing2,
                def);

            if (RimgateMod.debugLogging && DebugViewSettings.drawShooting)
                Utils.ThrowDebugText("Rimgate :: HitDest ", shootLine.Dest, caster.Map);
        }
        return true;
    }
}
