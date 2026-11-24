using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Rimgate;

public class ITab_CanopicJar : ITab_ContentsBase
{
    private List<Thing> listInt = new List<Thing>();

    public override IList<Thing> container
    {
        get
        {
            Building_CanopicJar jar = SelThing as Building_CanopicJar;
            listInt.Clear();
            if (jar != null && jar.ContainedThing != null)
                listInt.Add(jar.ContainedThing);

            return listInt;
        }
    }

    public ITab_CanopicJar()
    {
        labelKey = "TabCasketContents";
        containedItemsKey = "ContainedItems";
        canRemoveThings = true;
    }
}