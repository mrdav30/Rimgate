using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Rimgate;

internal static class CloneUtility
{
    public static bool TryCreateClonePawn(
        Building_CloningPod pod,
        CloneType cloneType,
        out Pawn clonePawn,
        out CalibrationOutcome outcome)
    {
        clonePawn = null;
        outcome = null;

        Pawn innerPawn = pod.HostPawn;
        if (innerPawn == null || !innerPawn.RaceProps.Humanlike)
        {
            if (RimgateMod.Debug)
                Log.Warning($"Rimgate: unable to clone pawn in {pod}");
            return false;
        }

        bool minorGenderSwap = false;
        bool minorGenderSwap0 = false;
        bool minorAlbino = false;
        bool minorBonusGenes = false;
        bool minorRandomTraits = false;
        bool majorCloneDies = false;
        bool majorHostDies = false;
        bool majorIdiotClone = false;
        bool majorInsaneClone = false;
        bool majorFailure = false;

        int majorFailureChance = RimgateModSettings.MajorFailureChance;
        int minorFailureChance = RimgateModSettings.MinorFailureChance;
        int num1 = Rand.RangeInclusive(1, 101);
        int num2 = Rand.RangeInclusive(1, 101);
        if (RimgateModSettings.MajorFailures && num1 <= majorFailureChance)
        {
            majorFailure = true;
            switch (Rand.RangeInclusive(0, 4))
            {
                case 0:
                    majorCloneDies = true;
                    break;
                case 1:
                    majorHostDies = true;
                    break;
                case 2:
                    majorIdiotClone = true;
                    break;
                case 3:
                    majorInsaneClone = true;
                    break;
            }
        }

        if (cloneType == CloneType.Reconstruct && majorHostDies)
        {
            majorHostDies = false;
            switch (Rand.RangeInclusive(0, 3))
            {
                case 0:
                    majorCloneDies = true;
                    break;
                case 1:
                    majorIdiotClone = true;
                    break;
                case 2:
                    majorInsaneClone = true;
                    break;
            }
        }

        if (!majorFailure && RimgateModSettings.MinorFailures && num2 <= minorFailureChance)
        {
            switch (Rand.RangeInclusive(0, 4))
            {
                case 0:
                    minorGenderSwap = true;
                    break;
                case 1:
                    minorAlbino = true;
                    break;
                case 2:
                    minorBonusGenes = true;
                    break;
                case 3:
                    minorRandomTraits = true;
                    break;
            }
        }

        if (majorHostDies)
        {
            Find.LetterStack.ReceiveLetter(
                Translator.Translate("RG_LetterCloneMajorFailureLabel"),
                TranslatorFormattedStringExtensions.Translate(
                    "RG_LetterCloneHostDiedDesc",
                    innerPawn.Name.ToStringShort),
                LetterDefOf.NegativeEvent,
                innerPawn);
            innerPawn.Kill(null, null);
            return false;
        }

        float minAge = innerPawn.kindDef.RaceProps.lifeStageAges.Last<LifeStageAge>().minAge;
        float num3 = cloneType == CloneType.Genome || cloneType == CloneType.Enhanced
            ? minAge
            : innerPawn.ageTracker.AgeChronologicalYearsFloat;
        PawnKindDef kindDef = innerPawn.kindDef;
        Faction faction = innerPawn.Faction;
        Gender currentGender = innerPawn.gender;
        if (minorGenderSwap)
        {
            if (currentGender == Gender.Male)
            {
                currentGender = Gender.Female;
                minorGenderSwap0 = true;
            }
            else
            {
                if (currentGender == Gender.Female)
                    currentGender = Gender.Male;
            }
        }

        clonePawn = PawnGenerator.GeneratePawn(
            new PawnGenerationRequest(
                kindDef,
                faction,
                PawnGenerationContext.NonPlayer,
                -1,
                true,
                false,
                false,
                false,
                false,
                1f,
                false,
                true,
                true,
                true,
                true,
                false,
                false,
                false,
                false,
                0.0f,
                0.0f,
                null,
                0.0f,
                null,
                null,
                null,
                null,
                null,
                minAge,
                num3,
                currentGender));
        clonePawn.equipment?.DestroyAllEquipment(DestroyMode.Vanish);
        clonePawn.apparel?.DestroyAll(DestroyMode.Vanish);
        clonePawn.inventory?.DestroyAll(DestroyMode.Vanish);
        clonePawn.health?.hediffSet?.Clear();

        Pawn_StoryTracker story1 = innerPawn.story;
        Pawn_StoryTracker story2 = clonePawn.story;
        if (story2 == null)
        {
            if (RimgateMod.Debug)
                Log.Warning($"Rimgate: {clonePawn} cloned without story");
            return false;
        }

        if (ModsConfig.BiotechActive)
            clonePawn.genes.ClearXenogenes();

        clonePawn.genes.Endogenes.Clear();
        clonePawn.ageTracker.AgeBiologicalTicks = innerPawn.ageTracker.AgeBiologicalTicks;
        clonePawn.ageTracker.BirthAbsTicks = innerPawn.ageTracker.BirthAbsTicks;
        clonePawn.ageTracker.CurLifeStage.developmentalStage = innerPawn.ageTracker.CurLifeStage.developmentalStage;
        if (ModsConfig.BiotechActive)
        {
            if (minorAlbino)
            {
                clonePawn.genes.AddGene(RimgateDefOf.Skin_SheerWhite, true);
                clonePawn.genes.AddGene(RimgateDefOf.Eyes_Red, true);
                clonePawn.genes.AddGene(RimgateDefOf.Hair_SnowWhite, true);
            }

            List<Gene> sourceXenogenes = innerPawn.genes.Xenogenes;
            foreach (Gene gene in sourceXenogenes)
                clonePawn.genes.AddGene(gene.def, true);

            for (int j = 0; j < sourceXenogenes.Count; j++)
                clonePawn.genes.Xenogenes[j].overriddenByGene = !sourceXenogenes[j].Overridden
                    ? null
                    : clonePawn.genes.GenesListForReading.First<Gene>(e =>
                        e.def == sourceXenogenes[j].overriddenByGene.def);

            if (minorBonusGenes)
            {
                List<GeneDef> defsListForReading = DefDatabase<GeneDef>.AllDefsListForReading;
                if (defsListForReading == null || defsListForReading.Count == 0)
                {
                    Log.Error("Rimgate :: No genes found in DefDatabase.");
                    return false;
                }

                int num4 = Rand.RangeInclusive(1, 4);
                List<GeneDef> geneDefList = new List<GeneDef>();
                while (geneDefList.Count < num4)
                {
                    GeneDef geneDef = defsListForReading[Rand.RangeInclusive(0, defsListForReading.Count)];
                    if (!innerPawn.genes.HasActiveGene(geneDef) && !geneDefList.Contains(geneDef))
                    {
                        Log.Message($"Rimgate :: Added gene: {geneDef.label} to random gene list");
                        geneDefList.Add(geneDef);
                    }
                }

                foreach (GeneDef geneDef in geneDefList)
                {
                    clonePawn.genes.AddGene(geneDef, true);
                    Log.Message($"Rimgate :: Added gene: {((Def)geneDef).label} to {clonePawn.Name}");
                }
            }

            clonePawn.ageTracker.growthPoints = innerPawn.ageTracker.growthPoints;
            clonePawn.ageTracker.vatGrowTicks = innerPawn.ageTracker.vatGrowTicks;
            clonePawn.genes.xenotypeName = innerPawn.genes.xenotypeName;
            clonePawn.genes.iconDef = innerPawn.genes.iconDef;
            XenotypeDef xenotype = innerPawn.genes.Xenotype;
            clonePawn.genes.SetXenotypeDirect(xenotype);
        }

        List<Gene> sourceEndogenes = innerPawn.genes.Endogenes;
        foreach (Gene gene in sourceEndogenes)
            clonePawn.genes.AddGene(gene.def, false);

        for (int i = 0; i < sourceEndogenes.Count; i++)
            clonePawn.genes.Endogenes[i].overriddenByGene = !sourceEndogenes[i].Overridden
                ? null
                : clonePawn.genes.GenesListForReading.First<Gene>(e =>
                    e.def == sourceEndogenes[i].overriddenByGene.def);

        if (ModsConfig.BiotechActive)
        {
            float rollChance = cloneType switch
            {
                CloneType.Enhanced => 0.40f,
                CloneType.Genome => 0.20f,
                _ => 0.25f
            };

            if (Rand.Chance(rollChance))
            {
                GeneDef degradation = RimgateDefOf.Rimgate_CellularDegradation;
                if (!clonePawn.genes.HasActiveGene(degradation))
                {
                    clonePawn.genes.AddGene(degradation, false);
                    if (RimgateMod.Debug)
                        Log.Message($"Rimgate :: Added random degradation gene to {clonePawn.Name}");
                }
            }
        }

        if (!ModsConfig.BiotechActive && minorAlbino)
        {
            Color white = Color.white;
            story2.SkinColorBase = white;
            story2.skinColorOverride = new Color?(white);
            story2.HairColor = white;
        }

        if (minorGenderSwap)
        {
            AssignRandomGenderAppropriateHair(clonePawn);
            if (minorGenderSwap0)
            {
                clonePawn.style.beardDef = null;
                if (story1.bodyType == BodyTypeDefOf.Male)
                    story2.bodyType = BodyTypeDefOf.Female;
            }
            else if (story1.bodyType == BodyTypeDefOf.Female)
                story2.bodyType = BodyTypeDefOf.Male;
        }
        else
        {
            story2.hairDef = story1.hairDef;
            clonePawn.style.beardDef = innerPawn.style.beardDef;
            story2.bodyType = story1.bodyType;
        }

        story2.favoriteColor = story1.favoriteColor;
        story2.furDef = story1.furDef;
        story2.headType = story1.headType;
        clonePawn.style.BodyTattoo = RimgateDefOf.NoTattoo_Body;
        clonePawn.style.FaceTattoo = RimgateDefOf.NoTattoo_Face;
        if (RimgateModSettings.CloneTattoos)
        {
            clonePawn.style.BodyTattoo = innerPawn.style.BodyTattoo;
            clonePawn.style.FaceTattoo = innerPawn.style.FaceTattoo;
        }

        clonePawn.style.Notify_StyleItemChanged();
        story2.traits.allTraits.Clear();
        if (majorIdiotClone)
        {
            story2.Childhood = RimgateDefOf.Rimgate_DamagedClone;
            story2.traits.GainTrait(new Trait(TraitDef.Named("SlowLearner"), 0, false), false);
            story2.traits.GainTrait(new Trait(TraitDef.Named("Industriousness"), -2, false), false);
            story2.traits.GainTrait(new Trait(TraitDef.Named("SpeedOffset"), -1, false), false);
        }
        else if (minorRandomTraits)
        {
            List<TraitDef> defsListForReading = DefDatabase<TraitDef>.AllDefsListForReading;
            if (defsListForReading == null || defsListForReading.Count == 0)
            {
                Log.Error("Rimgate :: No traits found in DefDatabase.");
                return false;
            }

            int num5 = Rand.RangeInclusive(2, 4);
            List<TraitDef> traitDefList = new List<TraitDef>();
            while (traitDefList.Count < num5)
            {
                TraitDef traitDef = defsListForReading[Rand.RangeInclusive(0, defsListForReading.Count)];
                if (!innerPawn.story.traits.HasTrait(traitDef) && !traitDefList.Contains(traitDef))
                    traitDefList.Add(traitDef);
            }

            foreach (TraitDef traitDef in traitDefList)
            {
                Trait trait = new Trait(traitDef, GetRandomDegreeForTrait(traitDef), false);
                clonePawn.story.traits.GainTrait(trait, false);
            }
        }
        else
        {
            foreach (Trait allTrait in story1.traits.allTraits)
                story2.traits.GainTrait(allTrait, false);

            switch (cloneType)
            {
                case CloneType.Genome:
                    story2.Childhood = RimgateDefOf.Rimgate_Replicant;
                    story2.traits.GainTrait(new Trait(TraitDef.Named("FastLearner"), 0, false), false);
                    break;
                case CloneType.Enhanced:
                    story2.traits.allTraits.Clear();
                    story2.Childhood = RimgateDefOf.Rimgate_EnhancedClone;
                    story2.traits.GainTrait(new Trait(TraitDef.Named("NaturalMood"), 2, false), false);
                    story2.traits.GainTrait(new Trait(TraitDef.Named("Nerves"), 2, false), false);
                    story2.traits.GainTrait(new Trait(TraitDef.Named("Ascetic"), 0, false), false);
                    break;
                default:
                    story2.Childhood = story1.Childhood;
                    story2.Adulthood = story1.Adulthood;
                    break;
            }
        }

        Pawn_SkillTracker skills1 = clonePawn.skills;
        if (majorIdiotClone)
        {
            Pawn_SkillTracker skills2 = innerPawn.skills;
            if (skills2 != null)
            {
                foreach (SkillRecord skill1 in skills1.skills)
                {
                    SkillRecord skill2 = skills2.GetSkill(skill1.def);
                    skill1.Level = (int)((double)skill2.levelInt * 0.30000001192092896);
                    skill1.Notify_SkillDisablesChanged();
                }
            }

            SkillRecord skill = skills1.GetSkill(SkillDefOf.Intellectual);
            skill.passion = Passion.None;
            skill.Level = 0;
            skill.Notify_SkillDisablesChanged();
        }
        else if (skills1 != null)
        {
            Pawn_SkillTracker skills3 = innerPawn.skills;
            if (skills3 != null)
            {
                foreach (SkillRecord skill3 in skills1.skills)
                {
                    if (cloneType == CloneType.Genome || cloneType == CloneType.Enhanced)
                    {
                        skill3.Level = 0;
                        skill3.passion = Passion.None;
                        skill3.Notify_SkillDisablesChanged();
                    }
                    else
                    {
                        SkillRecord skill4 = skills3.GetSkill(skill3.def);
                        skill3.Level = cloneType != CloneType.Full
                            ? (int)((double)skill4.levelInt * 0.5)
                            : (!RimgateModSettings.NoSkillLoss
                                ? (int)((double)skill4.levelInt * 0.800000011920929)
                                : skill4.levelInt);
                        skill3.passion = skill4.passion;
                        skill3.Notify_SkillDisablesChanged();
                    }
                }

                if (cloneType == CloneType.Enhanced)
                {
                    SkillRecord skill5 = skills1.GetSkill(SkillDefOf.Shooting);
                    skill5.passion = Passion.Major;
                    skill5.Level = 16 /*0x10*/;
                    skill5.Notify_SkillDisablesChanged();
                    SkillRecord skill6 = skills1.GetSkill(SkillDefOf.Melee);
                    skill6.passion = Passion.Major;
                    skill6.Level = 16 /*0x10*/;
                    skill6.Notify_SkillDisablesChanged();
                }

                if (cloneType == CloneType.Genome)
                {
                    List<SkillRecord> skillRecordList = new List<SkillRecord>();
                    int num6 = 0;
                    for (int index = 0; num6 < 6 && index < 100; ++index)
                    {
                        SkillRecord skillRecord = GenCollection.RandomElement<SkillRecord>((IEnumerable<SkillRecord>)skills1.skills);
                        if (!skillRecordList.Contains(skillRecord))
                        {
                            skillRecordList.Add(skillRecord);
                            skillRecord.passion = Passion.Minor;
                            ++num6;
                        }
                    }
                }
            }
        }

        clonePawn.workSettings?.EnableAndInitialize();
        if (majorInsaneClone)
        {
            if (clonePawn.RaceProps.IsFlesh)
            {
                clonePawn.health.AddHediff(RimgateDefOf.Rimgate_Clone);
                clonePawn.health.AddHediff(HediffDefOf.Scaria);
            }
        }
        else if (majorIdiotClone)
        {
            if (clonePawn.RaceProps.IsFlesh)
            {
                clonePawn.health.AddHediff(RimgateDefOf.Rimgate_Clone);
                clonePawn.health.AddHediff(HediffDefOf.Dementia);
                clonePawn.health.AddHediff(RimgateDefOf.Rimgate_ClonePodSickness);
                clonePawn.health.AddHediff(RimgateDefOf.Rimgate_SystemShock);
            }
        }
        else if (clonePawn.RaceProps.IsFlesh)
        {
            clonePawn.health.AddHediff(RimgateDefOf.Rimgate_Clone);
            clonePawn.health.AddHediff(RimgateDefOf.Rimgate_ClonePodSickness);
            clonePawn.health.AddHediff(RimgateDefOf.Rimgate_SystemShock);
            if (cloneType == CloneType.Enhanced)
                clonePawn.health.AddHediff(RimgateDefOf.Rimgate_ClonedEnduring);
        }

        if (innerPawn.mutant != null)
            MutantUtility.SetPawnAsMutantInstantly(clonePawn, innerPawn.mutant.Def, RottableUtility.GetRotStage(innerPawn));

        if (RimgateModSettings.GenerateSocialRelations)
        {
            clonePawn.relations.ClearAllRelations();
            foreach (var direct in innerPawn.relations.DirectRelations)
                clonePawn.relations.AddDirectRelation(direct.def, direct.otherPawn);
        }

        string first = ((NameTriple)innerPawn.Name).First;
        string nick = ((NameTriple)innerPawn.Name).Nick;
        string last = ((NameTriple)innerPawn.Name).Last;
        if (first.Length == 0)
            first = ((NameTriple)clonePawn.Name).First;

        if (last.Length == 0)
            last = ((NameTriple)clonePawn.Name).Last;

        Hediff_ClonedTracker clonedTracker = GetOrAddTracker(innerPawn);
        clonedTracker.TimesCloned++;

        Hediff_Clone hostCloneHeddif = innerPawn.GetHediff<Hediff_Clone>();

        Hediff_Clone cloneHediff = clonePawn.GetHediff<Hediff_Clone>();
        cloneHediff.CloneGeneration = hostCloneHeddif != null
            ? hostCloneHeddif.CloneGeneration++
            : 1;

        string str = IsCloneName(last)
            ? $"{last.Substring(0, 2)}-{$"{Rand.RangeInclusive(0, 65536):X4}"}"
            : first.Length <= 0 || last.Length <= 0
                ? $"{RandomLetters()}-{$"{Rand.RangeInclusive(0, 65536):X4}"}"
                : $"{first.Substring(0, 1).ToUpper()}{last.Substring(0, 1).ToUpper()}-{$"{Rand.RangeInclusive(0, 65536):X4}"}";
        clonePawn.Name = (Name)new NameTriple(first, $"{nick}-{clonedTracker.TimesCloned.ToString()}", str);
        clonePawn.Drawer.renderer.SetAllGraphicsDirty();

        outcome = new CalibrationOutcome
        {
            cloneType = cloneType,
            minorGenderSwap = minorGenderSwap,
            minorAlbino = minorAlbino,
            minorBonusGenes = minorBonusGenes,
            minorRandomTraits = minorRandomTraits,
            majorCloneDies = majorCloneDies,
            majorHostDies = majorHostDies,
            majorIdiotClone = majorIdiotClone,
            majorInsaneClone = majorInsaneClone
        };

        return true;
    }

    public static void FinalizeSpawn(Building_CloningPod pod, Pawn clone, CalibrationOutcome outcome)
    {
        if (pod == null || clone == null || outcome == null)
        {
            if (RimgateMod.Debug)
                Log.Warning("Rimgate: unable to finalize spawn of clone pawn due to null argument");
            return;
        }

        GenSpawn.Spawn(clone, pod.Position, pod.Map, WipeMode.Vanish);
        SoundStarter.PlayOneShot(
            SoundDef.Named("CryptosleepCasketEject"),
            SoundInfo.InMap(new TargetInfo(pod.Position, pod.Map, false), MaintenanceType.None));
        ThingDef filthSlime = ThingDefOf.Filth_Slime;
        if (outcome.majorCloneDies)
        {
            Find.LetterStack.ReceiveLetter(
                Translator.Translate("RG_LetterCloneMajorFailureLabel"),
                TranslatorFormattedStringExtensions.Translate(
                    "RG_LetterCloneDiedDesc",
                    clone.Name.ToStringShort),
                LetterDefOf.NegativeEvent,
                new GlobalTargetInfo(clone));
            clone.filth.GainFilth(filthSlime);
            clone.Kill(null, null);
        }
        else if (outcome.minorAlbino)
        {
            Find.LetterStack.ReceiveLetter(
                Translator.Translate("RG_LetterCloneMinorFailureLabel"),
                TranslatorFormattedStringExtensions.Translate(
                    "RG_LetterAlbinoBodyDesc",
                    clone.Name.ToStringShort),
                LetterDefOf.NegativeEvent,
                new GlobalTargetInfo(clone));
            clone.filth.GainFilth(filthSlime);
        }
        else if (outcome.minorBonusGenes)
        {
            Find.LetterStack.ReceiveLetter(
                Translator.Translate("RG_LetterCloneMinorFailureLabel"),
                TranslatorFormattedStringExtensions.Translate(
                    "RG_LetterBonusGenesDesc",
                    clone.Name.ToStringShort),
                LetterDefOf.NegativeEvent,
                new GlobalTargetInfo(clone));
            clone.filth.GainFilth(filthSlime);
        }
        else if (outcome.minorRandomTraits)
        {
            Find.LetterStack.ReceiveLetter(
                Translator.Translate("RG_LetterCloneMinorFailureLabel"),
                TranslatorFormattedStringExtensions.Translate(
                    "RG_LetterRandomTraitsDesc",
                    clone.Name.ToStringShort),
                LetterDefOf.NegativeEvent,
                new GlobalTargetInfo(clone));
            clone.filth.GainFilth(filthSlime);
        }
        else if (outcome.minorGenderSwap)
        {
            Find.LetterStack.ReceiveLetter(
                Translator.Translate("RG_LetterCloneMinorFailureLabel"),
                TranslatorFormattedStringExtensions.Translate(
                    "RG_LetterGenderSwapDesc",
                    clone.Name.ToStringShort),
                LetterDefOf.NegativeEvent,
                new GlobalTargetInfo(clone));
            clone.filth.GainFilth(filthSlime);
        }
        else if (outcome.majorIdiotClone)
        {
            Find.LetterStack.ReceiveLetter(
                Translator.Translate("RG_LetterCloneMajorFailureLabel"),
                TranslatorFormattedStringExtensions.Translate(
                    "RG_LetterIdiotCloneDesc",
                    clone.Name.ToStringShort),
                LetterDefOf.NegativeEvent,
                new GlobalTargetInfo(clone));
            clone.filth.GainFilth(filthSlime);
        }
        else if (outcome.majorInsaneClone)
        {
            Find.LetterStack.ReceiveLetter(
                Translator.Translate("RG_LetterCloneMajorFailureLabel"),
                TranslatorFormattedStringExtensions.Translate(
                    "RG_LetterInsaneCloneDesc",
                    clone.Name.ToStringShort),
                LetterDefOf.ThreatSmall,
                new GlobalTargetInfo(clone));
            clone.filth.GainFilth(filthSlime);
            clone.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk);
        }
        else
        {
            string letter = string.Empty;
            switch (outcome.cloneType)
            {
                case CloneType.Genome:
                    letter = "RG_LetterHostClonedGenomeDesc";
                    break;
                case CloneType.Full:
                    letter = "RG_LetterHostClonedFullDesc";
                    break;
                case CloneType.Enhanced:
                    letter = "RG_LetterHostClonedEnhancedDesc";
                    break;
                case CloneType.Reconstruct:
                    letter = "RG_LetterHostClonedReconstructedDesc";
                    break;
            }

            Find.LetterStack.ReceiveLetter(
                Translator.Translate("RG_LetterHostClonedLabel"),
                letter.Translate(clone.Name.ToStringShort),
                LetterDefOf.PositiveEvent,
                new GlobalTargetInfo(clone));
            clone.filth.GainFilth(filthSlime);
        }
    }

    internal static void AssignRandomGenderAppropriateHair(Pawn pawn)
    {
        List<HairDef> list = DefDatabase<HairDef>.AllDefsListForReading
            .Where<HairDef>(hairDef =>
            {
                if (hairDef.styleGender == StyleGender.Any || hairDef.styleGender == null && pawn.gender == Gender.Male)
                    return true;
                return hairDef.styleGender == StyleGender.Female && pawn.gender == Gender.Female;
            }).ToList<HairDef>();
        if (!GenCollection.Any<HairDef>(list))
            return;

        HairDef newHairDef = GenCollection.RandomElement<HairDef>((IEnumerable<HairDef>)list);
        pawn.story.hairDef = newHairDef;
    }

    internal static int GetRandomDegreeForTrait(TraitDef traitDef)
    {
        return traitDef.degreeDatas != null && traitDef.degreeDatas.Count > 0
            ? traitDef.degreeDatas[Rand.RangeInclusive(0, traitDef.degreeDatas.Count)].degree
            : 0;
    }

    internal static bool IsCloneName(string name)
    {
        return Regex.Match(name, "^[A-Z]{2}-[0-9A-F]{4}$").Success;
    }

    internal static string RandomLetters()
    {
        string str1 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        char ch = str1[Rand.RangeInclusive(0, str1.Length - 1)];
        string str2 = ch.ToString();
        ch = str1[Rand.RangeInclusive(0, str1.Length - 1)];
        string str3 = ch.ToString();
        return str2 + str3;
    }

    internal static Hediff_ClonedTracker GetOrAddTracker(Pawn pawn)
    {
        if (!pawn.HasHediff<Hediff_ClonedTracker>())
            pawn.health.AddHediff(RimgateDefOf.Hediff_ClonedTracker);
        return pawn.GetHediff<Hediff_ClonedTracker>();
    }
}
