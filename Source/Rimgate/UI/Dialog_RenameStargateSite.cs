using RimWorld;
using Verse;

namespace Rimgate;

public class Dialog_RenameStargateSite : Dialog_Rename<WorldObject_GateTransitSite>
{
    WorldObject_GateTransitSite sgSite;

    public Dialog_RenameStargateSite(WorldObject_GateTransitSite sgSite) : base(sgSite)
    {
        sgSite = sgSite;
    }
}
