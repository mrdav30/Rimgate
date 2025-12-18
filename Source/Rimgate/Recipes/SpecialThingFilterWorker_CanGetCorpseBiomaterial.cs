using System;
using System.Collections.Generic;
using System.Linq;
using Rimgate;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimgate;

public abstract class SpecialThingFilterWorker_CanGetCorpseBiomaterial : SpecialThingFilterWorker
{
    private readonly bool _isAnimal;
    private readonly bool _isRecoverable;

    protected SpecialThingFilterWorker_CanGetCorpseBiomaterial(bool harvestable, bool animal)
    {
        _isRecoverable = harvestable;
        _isAnimal = animal;
    }

    public override bool Matches(Thing t) => DoesMatch(t as Corpse);

    public override bool CanEverMatch(ThingDef def) => def.IsWithinCategory(ThingCategoryDefOf.Corpses);

    protected virtual bool DoesMatch(Corpse corpse)
    {
        if (corpse == null)
            return false;

        RaceProperties race = corpse.InnerPawn?.RaceProps;

        if (_isAnimal)
            return race.Animal
                && !race.IsAnomalyEntity
                && !race.Humanlike
                && CanProcureBiomaterials(corpse) == _isRecoverable;

        return race.Humanlike
            && !race.IsAnomalyEntity
            && !race.Animal
            && CanProcureBiomaterials(corpse) == _isRecoverable;
    }

    private bool CanProcureBiomaterials(Corpse corpse)
    {
        CompRottable rot = corpse.TryGetComp<CompRottable>();
        bool notRotten = rot == null
            ? corpse.Age <= MedicalUtil.MaxCorpseAgeForHarvest * 2500
            : rot.RotProgress + (corpse.Age - rot.RotProgress) * MedicalUtil.MaxFrozenDecay <=
              MedicalUtil.MaxCorpseAgeForHarvest * 2500;

        Pawn pawn = corpse.InnerPawn;
        BodyPartRecord core = pawn.RaceProps.body.corePart;
        List<BodyPartRecord> queue = new List<BodyPartRecord> { core };
        HediffSet hediffSet = pawn.health.hediffSet;
        while (queue.Count > 0)
        {
            BodyPartRecord part = queue.First();
            queue.Remove(part);
            if (CanGetBiomaterial(pawn, part, notRotten) && core != part)
                return true;
            queue.AddRange(part.parts.Where(x => !hediffSet.PartIsMissing(x)));
        }

        return false;
    }

    public bool CanGetBiomaterial(Pawn pawn, BodyPartRecord part, bool notRotten)
    {
        if (!_isAnimal && notRotten && MedicalUtil.IsCleanAndDroppable(pawn, part))
            return true;

        return pawn.health.hediffSet.hediffs.Any(x =>
            part.Equals(x.Part) 
            && x.def.spawnThingOnRemoved != null 
            && (x is Hediff_Implant || x is Hediff_AddedPart));
    }
}

public class CanProcureCorpseBiomaterial : SpecialThingFilterWorker_CanGetCorpseBiomaterial
{
    public CanProcureCorpseBiomaterial() : base(true, false) { }
}

public class CanProcureAnimalCorpseBiomaterial : SpecialThingFilterWorker_CanGetCorpseBiomaterial
{
    public CanProcureAnimalCorpseBiomaterial() : base(true, true) { }
}

public class NoCorpseBiomaterialToProcure : SpecialThingFilterWorker_CanGetCorpseBiomaterial
{
    public NoCorpseBiomaterialToProcure() : base(false, false) { }
}

public class NoAnimalCorpseBiomaterialToProcure : SpecialThingFilterWorker_CanGetCorpseBiomaterial
{
    public NoAnimalCorpseBiomaterialToProcure() : base(false, true) { }
}