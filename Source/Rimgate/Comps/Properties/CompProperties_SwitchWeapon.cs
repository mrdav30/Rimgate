using Verse;

namespace Rimgate;

public class CompProperties_SwitchWeapon : CompProperties
{
    public ThingDef weaponToSwitch;

    public CompProperties_SwitchWeapon() => compClass = typeof(Comp_SwitchWeapon);
}
