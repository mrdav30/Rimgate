using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Rimgate;

public class CompProperties_FrameApparel : CompProperties
{
    public List<string> requiredFrameDefNames;
    public string failReason = "requires equipped frame";

    public CompProperties_FrameApparel() => this.compClass = typeof(Comp_FrameApparel);
}
