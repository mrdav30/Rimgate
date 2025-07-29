using RimWorld.Planet;
using Verse;
using VEF.Abilities;
using RimWorld;

namespace Rimgate;

public class CompAbilityEffect_LaunchMultipleProjectile : CompAbilityEffect
{
    public bool firingNow = false;
    public int projectilesFired = 0;
    public int waitCounter = 0;
    public const int ticksBetweenProjectiles = 30;
    LocalTargetInfo targetVariable;

    public new CompProperties_MultipleProjectile Props => (CompProperties_MultipleProjectile)props;


    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);
        LaunchProjectile(target);
    }

    private Projectile LaunchProjectile(LocalTargetInfo target)
    {
        Projectile projectile = null;
        targetVariable = target;
        projectile = GenSpawn.Spawn(Props.projectile, parent.pawn.Position, parent.pawn.Map) as Projectile;

        if (target.HasThing)
            projectile?.Launch(parent.pawn,
                parent.pawn.DrawPos,
                target.Thing,
                target.Thing,
                ProjectileHitFlags.IntendedTarget);
        else
            projectile?.Launch(parent.pawn,
                parent.pawn.DrawPos,
                target.Cell,
                target.Cell,
                ProjectileHitFlags.IntendedTarget);

        firingNow = true;

        return projectile;
    }

    public override void CompTick()
    {
        base.CompTick();

        if (!firingNow)
            return;

        waitCounter++;
        if (waitCounter > ticksBetweenProjectiles)
        {
            Projectile projectile = GenSpawn.Spawn(
                Props.projectile,
                parent.pawn.Position,
                parent.pawn.Map) as Projectile;

            if (targetVariable.HasThing)
                projectile?.Launch(parent.pawn,
                    parent.pawn.DrawPos,
                    targetVariable.Thing,
                    targetVariable.Thing,
                    ProjectileHitFlags.IntendedTarget);
            else
                projectile?.Launch(parent.pawn,
                    parent.pawn.DrawPos,
                    targetVariable.Cell,
                    targetVariable.Cell,
                    ProjectileHitFlags.IntendedTarget);

            projectilesFired++;
            waitCounter = 0;
        }

        if (projectilesFired >= Props.numberOfProjectiles - 1)
        {
            projectilesFired = 0;
            waitCounter = 0;
            firingNow = false;
        }
    }
}