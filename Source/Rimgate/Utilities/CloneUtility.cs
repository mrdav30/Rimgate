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
    public static void Clone(Building_CloningPod pod, CloneType cloneType)
    {
        Pawn innerPawn = pod.InnerPawn;
        if (innerPawn == null || !innerPawn.RaceProps.Humanlike)
        {
            if (RimgateMod.Debug)
                Log.Warning($"Rimgate: unable to clone pawn in {pod}");
            return;
        }

        bool flag1 = false;
        bool flag2 = false;
        bool flag3 = false;
        bool flag4 = false;
        bool flag5 = false;
        bool flag6 = false;
        bool flag7 = false;
        bool flag8 = false;
        bool flag9 = false;
        bool flag10 = false;
        int majorFailureChance = RimgateModSettings.MajorFailureChance;
        int minorFailureChance = RimgateModSettings.MinorFailureChance;
        System.Random random1 = new();
        int num1 = random1.Next(1, 101);
        int num2 = random1.Next(1, 101);
        if (RimgateModSettings.MajorFailures && num1 <= majorFailureChance)
        {
            flag9 = true;
            switch (random1.Next(0, 4))
            {
                case 0:
                    flag5 = true;
                    break;
                case 1:
                    flag6 = true;
                    break;
                case 2:
                    flag7 = true;
                    break;
                case 3:
                    flag8 = true;
                    break;
            }
        }

        if (cloneType == CloneType.Reconstruct && flag6)
        {
            flag6 = false;
            switch (random1.Next(0, 3))
            {
                case 0:
                    flag5 = true;
                    break;
                case 1:
                    flag7 = true;
                    break;
                case 2:
                    flag8 = true;
                    break;
            }
        }

        if (!flag9 && RimgateModSettings.MinorFailures && num2 <= minorFailureChance)
        {
            switch (random1.Next(0, 4))
            {
                case 0:
                    flag1 = true;
                    break;
                case 1:
                    flag2 = true;
                    break;
                case 2:
                    flag3 = true;
                    break;
                case 3:
                    flag4 = true;
                    break;
            }
        }

        if (flag6)
        {
            Find.LetterStack.ReceiveLetter(
                Translator.Translate("RG_LetterCloneMajorFailureLabel"),
                TranslatorFormattedStringExtensions.Translate(
                    "RG_LetterCloneHostDiedDesc",
                    innerPawn.Name.ToStringShort),
                LetterDefOf.NegativeEvent,
                innerPawn);
            innerPawn.Kill(null, null);
            return;
        }

        float minAge = innerPawn.kindDef.RaceProps.lifeStageAges.Last<LifeStageAge>().minAge;
        float num3 = cloneType == CloneType.Genome || cloneType == CloneType.Enhanced
            ? minAge
            : innerPawn.ageTracker.AgeChronologicalYearsFloat;
        PawnKindDef kindDef = innerPawn.kindDef;
        Faction faction = innerPawn.Faction;
        Gender currentGender = innerPawn.gender;
        if (flag1)
        {
            if (currentGender == Gender.Male)
            {
                currentGender = Gender.Female;
                flag10 = true;
            }
            else
            {
                if (currentGender == Gender.Female)
                    currentGender = Gender.Male;
            }
        }

        Pawn pawn = PawnGenerator.GeneratePawn(
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
        pawn.equipment?.DestroyAllEquipment(DestroyMode.Vanish);
        pawn.apparel?.DestroyAll(DestroyMode.Vanish);
        pawn.inventory?.DestroyAll(DestroyMode.Vanish);
        pawn.health?.hediffSet?.Clear();

        Pawn_StoryTracker story1 = innerPawn.story;
        Pawn_StoryTracker story2 = pawn.story;
        if (story2 == null)
        {
            if (RimgateMod.Debug)
                Log.Warning($"Rimgate: {pawn} cloned without story");
            return;
        }

        if (ModsConfig.BiotechActive)
            pawn.genes.ClearXenogenes();

        pawn.genes.Endogenes.Clear();
        pawn.ageTracker.AgeBiologicalTicks = innerPawn.ageTracker.AgeBiologicalTicks;
        pawn.ageTracker.BirthAbsTicks = innerPawn.ageTracker.BirthAbsTicks;
        pawn.ageTracker.CurLifeStage.developmentalStage = innerPawn.ageTracker.CurLifeStage.developmentalStage;
        if (ModsConfig.BiotechActive)
        {
            if (flag2)
            {
                pawn.genes.AddGene(RimgateDefOf.Skin_SheerWhite, true);
                pawn.genes.AddGene(RimgateDefOf.Eyes_Red, true);
                pawn.genes.AddGene(RimgateDefOf.Hair_SnowWhite, true);
            }

            List<Gene> sourceXenogenes = innerPawn.genes.Xenogenes;
            foreach (Gene gene in sourceXenogenes)
                pawn.genes.AddGene(gene.def, true);

            for (int j = 0; j < sourceXenogenes.Count; j++)
                pawn.genes.Xenogenes[j].overriddenByGene = !sourceXenogenes[j].Overridden
                    ? null
                    : pawn.genes.GenesListForReading.First<Gene>(e =>
                        e.def == sourceXenogenes[j].overriddenByGene.def);

            if (flag3)
            {
                List<GeneDef> defsListForReading = DefDatabase<GeneDef>.AllDefsListForReading;
                if (defsListForReading == null || defsListForReading.Count == 0)
                {
                    Log.Error("Rimgate :: No genes found in DefDatabase.");
                    return;
                }

                int num4 = random1.Next(1, 4);
                List<GeneDef> geneDefList = new List<GeneDef>();
                while (geneDefList.Count < num4)
                {
                    GeneDef geneDef = defsListForReading[random1.Next(defsListForReading.Count)];
                    if (!innerPawn.genes.HasActiveGene(geneDef) && !geneDefList.Contains(geneDef))
                    {
                        Log.Message($"Rimgate :: Added gene: {geneDef.label} to random gene list");
                        geneDefList.Add(geneDef);
                    }
                }

                foreach (GeneDef geneDef in geneDefList)
                {
                    pawn.genes.AddGene(geneDef, true);
                    Log.Message($"Rimgate :: Added gene: {((Def)geneDef).label} to {pawn.Name}");
                }
            }

            pawn.ageTracker.growthPoints = innerPawn.ageTracker.growthPoints;
            pawn.ageTracker.vatGrowTicks = innerPawn.ageTracker.vatGrowTicks;
            pawn.genes.xenotypeName = innerPawn.genes.xenotypeName;
            pawn.genes.iconDef = innerPawn.genes.iconDef;
            XenotypeDef xenotype = innerPawn.genes.Xenotype;
            pawn.genes.SetXenotypeDirect(xenotype);
        }

        List<Gene> sourceEndogenes = innerPawn.genes.Endogenes;
        foreach (Gene gene in sourceEndogenes)
            pawn.genes.AddGene(gene.def, false);

        for (int i = 0; i < sourceEndogenes.Count; i++)
            pawn.genes.Endogenes[i].overriddenByGene = !sourceEndogenes[i].Overridden
                ? null
                : pawn.genes.GenesListForReading.First<Gene>(e =>
                    e.def == sourceEndogenes[i].overriddenByGene.def);

        if (ModsConfig.BiotechActive)
        {
            double rollChance = cloneType switch
            {
                CloneType.Enhanced => 0.40,
                CloneType.Genome => 0.20,
                _ => 0.25
            };

            System.Random rngCellDeg = new();
            if (rngCellDeg.NextDouble() < rollChance)
            {
                GeneDef degradation = RimgateDefOf.Rimgate_CellularDegradation;
                if (!pawn.genes.HasActiveGene(degradation))
                {
                    pawn.genes.AddGene(degradation, false);
                    if (RimgateMod.Debug)
                        Log.Message($"Rimgate :: Added random degradation gene to {pawn.Name}");
                }
            }
        }

        if (!ModsConfig.BiotechActive && flag2)
        {
            Color white = Color.white;
            story2.SkinColorBase = white;
            story2.skinColorOverride = new Color?(white);
            story2.HairColor = white;
        }

        if (flag1)
        {
            AssignRandomGenderAppropriateHair(pawn);
            if (flag10)
            {
                pawn.style.beardDef = null;
                if (story1.bodyType == BodyTypeDefOf.Male)
                    story2.bodyType = BodyTypeDefOf.Female;
            }
            else if (story1.bodyType == BodyTypeDefOf.Female)
                story2.bodyType = BodyTypeDefOf.Male;
        }
        else
        {
            story2.hairDef = story1.hairDef;
            pawn.style.beardDef = innerPawn.style.beardDef;
            story2.bodyType = story1.bodyType;
        }

        story2.favoriteColor = story1.favoriteColor;
        story2.furDef = story1.furDef;
        story2.headType = story1.headType;
        pawn.style.BodyTattoo = RimgateDefOf.NoTattoo_Body;
        pawn.style.FaceTattoo = RimgateDefOf.NoTattoo_Face;
        if (RimgateModSettings.CloneTattoos)
        {
            pawn.style.BodyTattoo = innerPawn.style.BodyTattoo;
            pawn.style.FaceTattoo = innerPawn.style.FaceTattoo;
        }

        pawn.style.Notify_StyleItemChanged();
        story2.traits.allTraits.Clear();
        if (flag7)
        {
            story2.Childhood = DefDatabase<BackstoryDef>.GetNamed("Rimgate_DamagedClone", true);
            story2.traits.GainTrait(new Trait(TraitDef.Named("SlowLearner"), 0, false), false);
            story2.traits.GainTrait(new Trait(TraitDef.Named("Industriousness"), -2, false), false);
            story2.traits.GainTrait(new Trait(TraitDef.Named("SpeedOffset"), -1, false), false);
        }
        else if (flag4)
        {
            List<TraitDef> defsListForReading = DefDatabase<TraitDef>.AllDefsListForReading;
            if (defsListForReading == null || defsListForReading.Count == 0)
            {
                Log.Error("Rimgate :: No traits found in DefDatabase.");
                return;
            }

            int num5 = random1.Next(2, 4);
            List<TraitDef> traitDefList = new List<TraitDef>();
            while (traitDefList.Count < num5)
            {
                TraitDef traitDef = defsListForReading[random1.Next(defsListForReading.Count)];
                if (!innerPawn.story.traits.HasTrait(traitDef) && !traitDefList.Contains(traitDef))
                    traitDefList.Add(traitDef);
            }

            foreach (TraitDef traitDef in traitDefList)
            {
                Trait trait = new Trait(traitDef, GetRandomDegreeForTrait(traitDef), false);
                pawn.story.traits.GainTrait(trait, false);
            }
        }
        else
        {
            foreach (Trait allTrait in story1.traits.allTraits)
                story2.traits.GainTrait(allTrait, false);

            switch (cloneType)
            {
                case CloneType.Genome:
                    story2.Childhood = DefDatabase<BackstoryDef>.GetNamed("Rimgate_Clone", true);
                    story2.traits.GainTrait(new Trait(TraitDef.Named("FastLearner"), 0, false), false);
                    break;
                case CloneType.Enhanced:
                    story2.traits.allTraits.Clear();
                    story2.Childhood = DefDatabase<BackstoryDef>.GetNamed("Rimgate_EnhancedClone", true);
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

        Pawn_SkillTracker skills1 = pawn.skills;
        if (flag7)
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

        pawn.workSettings?.EnableAndInitialize();
        if (flag8)
        {
            if (pawn.RaceProps.IsFlesh)
            {
                pawn.health.AddHediff(RimgateDefOf.Rimgate_Clone);
                pawn.health.AddHediff(HediffDefOf.Scaria);
            }
        }
        else if (flag7)
        {
            if (pawn.RaceProps.IsFlesh)
            {
                pawn.health.AddHediff(RimgateDefOf.Rimgate_Clone);
                pawn.health.AddHediff(HediffDefOf.Dementia);
                pawn.health.AddHediff(RimgateDefOf.Rimgate_ClonePodSickness);
                pawn.health.AddHediff(RimgateDefOf.Rimgate_SystemShock);
            }
        }
        else if (pawn.RaceProps.IsFlesh)
        {
            pawn.health.AddHediff(RimgateDefOf.Rimgate_Clone);
            pawn.health.AddHediff(RimgateDefOf.Rimgate_ClonePodSickness);
            pawn.health.AddHediff(RimgateDefOf.Rimgate_SystemShock);
            if (cloneType == CloneType.Enhanced)
                pawn.health.AddHediff(RimgateDefOf.Rimgate_ClonedEnduring);
        }

        if (innerPawn.mutant != null)
            MutantUtility.SetPawnAsMutantInstantly(pawn, innerPawn.mutant.Def, RottableUtility.GetRotStage(innerPawn));

        if (RimgateModSettings.GenerateSocialRelations)
            pawn.relations = innerPawn.relations;

        string first = ((NameTriple)innerPawn.Name).First;
        string nick = ((NameTriple)innerPawn.Name).Nick;
        string last = ((NameTriple)innerPawn.Name).Last;
        if (first.Length == 0)
            first = ((NameTriple)pawn.Name).First;

        if (last.Length == 0)
            last = ((NameTriple)pawn.Name).Last;

        Hediff_ClonedTracker clonedTracker = GetOrAddTracker(innerPawn);
        if (clonedTracker.TimesCloned > 1)
            ++clonedTracker.TimesCloned;

        Hediff_Clone hostCloneHeddif = innerPawn.GetHediff<Hediff_Clone>();

        Hediff_Clone cloneHediff = pawn.GetHediff<Hediff_Clone>();
        cloneHediff.CloneGeneration = hostCloneHeddif != null
            ? ++hostCloneHeddif.CloneGeneration
            : 1;

        System.Random random2 = new();
        string str = IsCloneName(last)
            ? $"{last.Substring(0, 2)}-{$"{random2.Next(65536 /*0x010000*/):X4}"}"
            : first.Length <= 0 || last.Length <= 0
                ? $"{RandomLetters()}-{$"{random2.Next(65536 /*0x010000*/):X4}"}"
                : $"{first.Substring(0, 1).ToUpper()}{last.Substring(0, 1).ToUpper()}-{$"{random2.Next(65536 /*0x010000*/):X4}"}";
        pawn.Name = (Name)new NameTriple(first, $"{nick}-{clonedTracker.TimesCloned.ToString()}", str);
        pawn.Drawer.renderer.SetAllGraphicsDirty();
        GenSpawn.Spawn(pawn, pod.Position, pod.Map, WipeMode.Vanish);
        SoundStarter.PlayOneShot(
            SoundDef.Named("CryptosleepCasketEject"),
            SoundInfo.InMap(new TargetInfo(pod.Position, pod.Map, false), MaintenanceType.None));
        ThingDef filthSlime = ThingDefOf.Filth_Slime;
        if (flag5)
        {
            Find.LetterStack.ReceiveLetter(
                Translator.Translate("RG_LetterCloneMajorFailureLabel"),
                TranslatorFormattedStringExtensions.Translate(
                    "RG_LetterCloneDiedDesc",
                    pawn.Name.ToStringShort),
                LetterDefOf.NegativeEvent,
                new GlobalTargetInfo(pawn));
            pawn.filth.GainFilth(filthSlime);
            pawn.Kill(null, null);
        }
        else if (flag2)
        {
            Find.LetterStack.ReceiveLetter(
                Translator.Translate("RG_LetterCloneMinorFailureLabel"),
                TranslatorFormattedStringExtensions.Translate(
                    "RG_LetterAlbinoBodyDesc",
                    pawn.Name.ToStringShort),
                LetterDefOf.NegativeEvent,
                new GlobalTargetInfo(pawn));
            pawn.filth.GainFilth(filthSlime);
        }
        else if (flag3)
        {
            Find.LetterStack.ReceiveLetter(
                Translator.Translate("RG_LetterCloneMinorFailureLabel"),
                TranslatorFormattedStringExtensions.Translate(
                    "RG_LetterBonusGenesDesc",
                    pawn.Name.ToStringShort),
                LetterDefOf.NegativeEvent,
                new GlobalTargetInfo(pawn));
            pawn.filth.GainFilth(filthSlime);
        }
        else if (flag4)
        {
            Find.LetterStack.ReceiveLetter(
                Translator.Translate("RG_LetterCloneMinorFailureLabel"),
                TranslatorFormattedStringExtensions.Translate(
                    "RG_LetterRandomTraitsDesc",
                    pawn.Name.ToStringShort),
                LetterDefOf.NegativeEvent,
                new GlobalTargetInfo(pawn));
            pawn.filth.GainFilth(filthSlime);
        }
        else if (flag1)
        {
            Find.LetterStack.ReceiveLetter(
                Translator.Translate("RG_LetterCloneMinorFailureLabel"),
                TranslatorFormattedStringExtensions.Translate(
                    "RG_LetterGenderSwapDesc",
                    pawn.Name.ToStringShort),
                LetterDefOf.NegativeEvent,
                new GlobalTargetInfo(pawn));
            pawn.filth.GainFilth(filthSlime);
        }
        else if (flag7)
        {
            Find.LetterStack.ReceiveLetter(
                Translator.Translate("RG_LetterCloneMajorFailureLabel"),
                TranslatorFormattedStringExtensions.Translate(
                    "RG_LetterIdiotCloneDesc",
                    pawn.Name.ToStringShort),
                LetterDefOf.NegativeEvent,
                new GlobalTargetInfo(pawn));
            pawn.filth.GainFilth(filthSlime);
        }
        else if (flag8)
        {
            Find.LetterStack.ReceiveLetter(
                Translator.Translate("RG_LetterCloneMajorFailureLabel"),
                TranslatorFormattedStringExtensions.Translate(
                    "RG_LetterInsaneCloneDesc",
                    pawn.Name.ToStringShort),
                LetterDefOf.ThreatSmall,
                new GlobalTargetInfo(pawn));
            pawn.filth.GainFilth(filthSlime);
            pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk);
        }
        else
        {
            string letter = string.Empty;
            switch (cloneType)
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
                letter.Translate(pawn.Name.ToStringShort),
                LetterDefOf.PositiveEvent,
                new GlobalTargetInfo(pawn));
            pawn.filth.GainFilth(filthSlime);
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
        System.Random random = new();
        return traitDef.degreeDatas != null && traitDef.degreeDatas.Count > 0
            ? traitDef.degreeDatas[random.Next(traitDef.degreeDatas.Count)].degree
            : 0;
    }

    internal static bool IsCloneName(string name)
    {
        return Regex.Match(name, "^[A-Z]{2}-[0-9A-F]{4}$").Success;
    }

    internal static string RandomLetters()
    {
        string str1 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        System.Random random = new();
        char ch = str1[random.Next(str1.Length - 1)];
        string str2 = ch.ToString();
        ch = str1[random.Next(str1.Length - 1)];
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
