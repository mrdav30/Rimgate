using Verse;

namespace Rimgate;

public class Dialog_RenameGateSite : Dialog_Rename<WorldObject_GateTransitSite>
{
    WorldObject_GateTransitSite sgSite;

    public Dialog_RenameGateSite(WorldObject_GateTransitSite sgSite) : base(sgSite)
    {
        this.sgSite = sgSite;
    }
}
