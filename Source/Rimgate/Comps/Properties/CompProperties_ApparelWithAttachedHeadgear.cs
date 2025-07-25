using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Rimgate;

public class CompProperties_ApparelWithAttachedHeadgear : CompProperties
{
    public ThingDef attachedHeadgearDef;
    [NoTranslate]
    public string toggleUiIconPath;

    public CompProperties_ApparelWithAttachedHeadgear() => this.compClass = typeof(Comp_ApparelWithAttachedHeadgear);
}
