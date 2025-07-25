using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Rimgate;

public class HediffComp_GoauldSymbiote : HediffComp
{
    public HediffCompProperties_GoauldSymbiote Props => (HediffCompProperties_GoauldSymbiote)props;

    public override void CompPostPostAdd(DamageInfo? dinfo)
    {
        if (Props == null)
            return;

        var newLifeExpectancy = Pawn.RaceProps.lifeExpectancy * Props.lifespanFactor;
        if(newLifeExpectancy < 0)
            newLifeExpectancy = 0;

        Pawn.RaceProps.lifeExpectancy = newLifeExpectancy;
    }

    public override void CompPostPostRemoved()
    {
        if (Props == null)
            return;

        var newLifeExpectancy = Pawn.RaceProps.lifeExpectancy / Props.lifespanFactor;
        if (newLifeExpectancy < 0)
            newLifeExpectancy = 0;

        Pawn.RaceProps.lifeExpectancy = newLifeExpectancy;
    }
}
