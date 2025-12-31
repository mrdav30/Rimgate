using UnityEngine;
using Verse;

namespace Rimgate;

public class CompProperties_TintedEmptyStateGraphic : CompProperties
{
    // overlay shown when "empty"
    public GraphicData graphicData;        

    // draw parent even when overlay is shown?
    public bool alwaysDrawParent;  

    // draw at parent’s altitude in PostDraw
    public bool useParentAltitude = true;  

    public CompProperties_TintedEmptyStateGraphic() => compClass = typeof(Comp_TintedEmptyStateGraphic);
}
