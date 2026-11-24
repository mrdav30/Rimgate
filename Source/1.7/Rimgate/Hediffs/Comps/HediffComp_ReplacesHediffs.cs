using Verse;

namespace Rimgate;

public class HediffComp_ReplacesHediffs : HediffComp
{
    public HediffCompProperties_ReplacesHediffs Props => (HediffCompProperties_ReplacesHediffs)props;

    public override void CompPostPostAdd(DamageInfo? dinfo)
    {
        if (Pawn == null
            || !Pawn.Spawned
            || Props.hediffDefs == null) return;

        foreach (var def in Props.hediffDefs)
        {
            if (!Pawn.HasHediffOf(def)) continue;

            Pawn.RemoveHediffOf(def);

            if (!Props.spawnAnyThings
                || def.spawnThingOnRemoved == null
                || Pawn.Map != null) continue;

            var thing = ThingMaker.MakeThing(def.spawnThingOnRemoved);
            if (thing == null) continue;
            GenPlace.TryPlaceThing(thing, Pawn.Position, Pawn.Map, ThingPlaceMode.Near);
        }
    }
}
