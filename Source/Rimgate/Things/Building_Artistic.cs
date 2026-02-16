using RimWorld;
using Verse;

namespace Rimgate;

public class Building_Artistic : Building_Art
{
    public override void PostMake()
    {
        base.PostMake();
        var compArt = GetComp<CompArt>();
        if (compArt != null)
            compArt.InitializeArt(!def.building.neverBuildable && Faction.IsOfPlayerFaction() 
                ? ArtGenerationContext.Colony 
                : ArtGenerationContext.Outsider);
    }
}