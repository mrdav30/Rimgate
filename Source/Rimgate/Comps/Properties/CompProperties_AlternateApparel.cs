using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Rimgate;

public class CompProperties_AlternateApparel : CompProperties
{
    public ThingDef attachedHeadgearDef;
    [NoTranslate]
    public string toggleUiIconPath;
    public bool isClosedState;

    public CompProperties_AlternateApparel() => compClass = typeof(Comp_AlternateApparel);
}
