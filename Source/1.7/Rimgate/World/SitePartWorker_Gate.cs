using RimWorld;
using RimWorld.Planet;
using System.Text;
using Verse;

namespace Rimgate;

public class SitePartWorker_Gate : SitePartWorker
{
    public override string GetPostProcessedThreatLabel(Site site, SitePart sitePart)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("RG_GateAddress".Translate(GateUtil.GetGateDesignation(site.Tile)));
        return sb.ToString();
    }
}
