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
    public bool isFrameRoot = false;

    public List<ThingDef> frameRootDefs;

    public string failReasonKey = "RG_FrameApparel_CannotWear_NoFrame";

    public CompProperties_FrameApparel() => compClass = typeof(Comp_FrameApparel);

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
    {

        if(isFrameRoot) yield break;

        yield return new StatDrawEntry(
            StatCategoryDefOf.Apparel,
            "RG_Stat_FrameApparel_RequiredFrames_Label".Translate(),
            frameRootDefs?.Count == 0 ? "n/a" : string.Join(", ", frameRootDefs.Select(def => def.label)),
            "RG_Stat_FrameApparel_RequiredFrames_Desc".Translate(),
            4994);
    }
}
