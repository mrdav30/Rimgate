using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Rimgate;

public class Thing_PrimtaSymbiote : ThingWithComps
{
    public SymbioteQueenLineage Lineage;

    public bool IgnoreDestroyEvent;

    public void AssumeQueenLineage(SymbioteQueenLineage lineage)
    {
        Lineage = SymbioteQueenLineage.DeepCopy(lineage);
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        if (!IgnoreDestroyEvent)
            RimgateEvents.Notify_SymbioteDestroyed(this);
        base.Destroy(mode);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref Lineage, "Lineage");
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
    {
        foreach (StatDrawEntry stat in base.SpecialDisplayStats())
            yield return stat;

        if (Lineage?.HasQueenName == true)
        {
            yield return new StatDrawEntry(
                StatCategoryDefOf.BasicsImportant,
                "RG_Symbiote_Stat_MotherQueen_Label".Translate(),
                Lineage.QueenName,
                "RG_Symbiote_Stat_MotherQueen_Desc".Translate(),
                4993);
        }
    }
}
