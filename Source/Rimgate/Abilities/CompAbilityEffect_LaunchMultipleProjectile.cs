using RimWorld.Planet;
using Verse;
using VEF.Abilities;
using RimWorld;

namespace Rimgate;

public class CompAbilityEffect_LaunchMultipleProjectile : CompAbilityEffect
{
    public bool FiringNow = false;

    public int ProjectilesFired = 0;

    public int WaitCounter = 0;

    public const int TicksBetweenProjectiles = 30;

    private LocalTargetInfo _targetVariable;

    public new CompProperties_MultipleProjectile Props => (CompProperties_MultipleProjectile)props;


    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);
        LaunchProjectile(target);
    }

    private Projectile LaunchProjectile(LocalTargetInfo target)
    {
        Projectile projectile = null;
        _targetVariable = target;
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

        FiringNow = true;

        return projectile;
    }

    public override void CompTick()
    {
        base.CompTick();

        if (!FiringNow)
            return;

        WaitCounter++;
        if (WaitCounter > TicksBetweenProjectiles)
        {
            Projectile projectile = GenSpawn.Spawn(
                Props.projectile,
                parent.pawn.Position,
                parent.pawn.Map) as Projectile;

            if (_targetVariable.HasThing)
                projectile?.Launch(parent.pawn,
                    parent.pawn.DrawPos,
                    _targetVariable.Thing,
                    _targetVariable.Thing,
                    ProjectileHitFlags.IntendedTarget);
            else
                projectile?.Launch(parent.pawn,
                    parent.pawn.DrawPos,
                    _targetVariable.Cell,
                    _targetVariable.Cell,
                    ProjectileHitFlags.IntendedTarget);

            ProjectilesFired++;
            WaitCounter = 0;
        }

        if (ProjectilesFired >= Props.numberOfProjectiles - 1)
        {
            ProjectilesFired = 0;
            WaitCounter = 0;
            FiringNow = false;
        }
    }
}