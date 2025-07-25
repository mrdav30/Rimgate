using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace Rimgate;

public class Dialog_RenameStargateSite : Dialog_Rename<WorldObject_PermanentStargateSite>
{
    WorldObject_PermanentStargateSite sgSite;

    public Dialog_RenameStargateSite(WorldObject_PermanentStargateSite sgSite) : base(sgSite)
    {
        this.sgSite = sgSite;
    }
}
