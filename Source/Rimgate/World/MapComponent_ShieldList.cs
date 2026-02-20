using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Rimgate;

public class MapComp_ShieldList : MapComponent
{
    public List<ThingWithComps> ShieldGenList = new();

    public IEnumerable<ThingWithComps> ActiveShieldGens
    {
        get
        {
            return ShieldGenList
                .Where<ThingWithComps>(shieldGen =>
                    shieldGen.TryGetComp<Comp_ShieldEmitter>()?.Active ?? false);
        }
    }

    public MapComp_ShieldList(Map map)
        : base(map) { }
}

