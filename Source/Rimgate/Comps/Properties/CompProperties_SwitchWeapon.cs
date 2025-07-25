using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Rimgate;

public class CompProperties_SwitchWeapon : CompProperties
{
    public List<ThingDef> weaponsToSwitch;

    public CompProperties_SwitchWeapon() => this.compClass = typeof(Comp_SwitchWeapon);
}
