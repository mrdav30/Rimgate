using Verse;
using RimWorld.Planet;
using RimWorld;
using System.Text;

namespace Rimgate;

public class SitePartWorker_Stargate : SitePartWorker
{
    public override string GetPostProcessedThreatLabel(Site site, SitePart sitePart)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("RG_GateAddress".Translate(StargateUtility.GetStargateDesignation(site.Tile)));
        return sb.ToString();
    }
}
