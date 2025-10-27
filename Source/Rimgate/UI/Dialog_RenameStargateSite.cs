using RimWorld;
using Verse;

namespace Rimgate;

public class Dialog_RenameStargateSite : Dialog_Rename<WorldObject_StargateTransitSite>
{
    WorldObject_StargateTransitSite sgSite;

    public Dialog_RenameStargateSite(WorldObject_StargateTransitSite sgSite) : base(sgSite)
    {
        sgSite = sgSite;
    }
}
