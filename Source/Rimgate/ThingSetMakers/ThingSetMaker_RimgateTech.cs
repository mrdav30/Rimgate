using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Rimgate;

public class ThingSetMaker_RimgateTech : ThingSetMaker
{
    public static List<ResearchProjectDef> ResearchDefs;

    public static List<ThingDef> ThingDefs;

    protected override void Generate(ThingSetMakerParams parms, List<Thing> outThings)
    {
        ResearchDefs ??= new List<ResearchProjectDef>()
        {
            Rimgate_DefOf.Rimgate_GoauldBasic,
            Rimgate_DefOf.Rimgate_GoauldTelTak,
            Rimgate_DefOf.Rimgate_Sarcophagus,
            Rimgate_DefOf.Rimgate_TauriBasic,
            Rimgate_DefOf.Rimgate_TauriPuddleJumper,
            Rimgate_DefOf.Rimgate_TretoninSynthesis,
            Rimgate_DefOf.Rimgate_WraithBasic,
            Rimgate_DefOf.Rimgate_WraithCloneGenome,
            Rimgate_DefOf.Rimgate_WraithDart
        };

        ThingDefs ??= new List<ThingDef>()
        {
            Rimgate_DefOf.Rimgate_GoauldBasic.Techprint,
            Rimgate_DefOf.Rimgate_GoauldTelTak.Techprint,
            Rimgate_DefOf.Rimgate_Sarcophagus.Techprint,
            Rimgate_DefOf.Rimgate_TauriBasic.Techprint,
            Rimgate_DefOf.Rimgate_TauriPuddleJumper.Techprint,
            Rimgate_DefOf.Rimgate_TretoninSynthesis.Techprint,
            Rimgate_DefOf.Rimgate_WraithBasic.Techprint,
            Rimgate_DefOf.Rimgate_WraithCloneGenome.Techprint,
            Rimgate_DefOf.Rimgate_WraithDart.Techprint,
            Rimgate_DefOf.Rimgate_GlyphParchment,
            Rimgate_DefOf.Rimgate_SymbioteImplant,
            Rimgate_DefOf.Rimgate_ScrapNote
        };

        IntRange intRange = new IntRange(0, 20);
        int chance = intRange.RandomInRange;

        if (chance <= 8)
        {
            ResearchProjectDef chosenDef = chance <= ResearchDefs.Count ? ResearchDefs[chance] : null;
            if (chosenDef != null && !chosenDef.IsFinished)
                outThings.Add(ThingMaker.MakeThing(ThingDefs[chance]));
        }
        else if (chance < 11)
        {
            ThingDef thingDef = chance <= ThingDefs.Count ? ThingDefs[chance] : null;
            if (thingDef != null)
                outThings.Add(ThingMaker.MakeThing(thingDef));

            return;
        }
        else if (chance < 16)
            outThings.Add(ThingMaker.MakeThing(Rimgate_DefOf.Rimgate_ScrapNote));
    }

    protected override IEnumerable<ThingDef> AllGeneratableThingsDebugSub(ThingSetMakerParams parms) => ThingDefs;
}