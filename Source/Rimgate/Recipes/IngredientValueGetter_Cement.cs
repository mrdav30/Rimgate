using RimWorld;
using UnityEngine;
using Verse;

namespace Rimgate;

public class IngredientValueGetter_Cement : IngredientValueGetter
{
    private float limestoneMultiplier = 1.5f;

    private float rawChunkValue = 20f;

    public override float ValuePerUnitOf(ThingDef t)
    {
        // is stuff made of stone?
        bool isStoneyStuff = IngredientValueGetter_Cement.IsStoneyStuff(t);
        if (isStoneyStuff)
        {
            if(RimgateMod.Debug)
                Log.Message($"IngredientValueGetter_Cement: {t.defName} is stoney stuff");
            return t.defName == "BlocksLimestone"
                ? limestoneMultiplier // limestone blocks are a little more valuable
                : 1f;
        }

        // is it a chunk?
        bool isChunk = IngredientValueGetter_Cement.IsChunk(t);
        if (!isChunk)
        {
            if(RimgateMod.Debug)
                Log.Message($"IngredientValueGetter_Cement: {t.defName} is not a chunk");
            return 0.0f; // only stoney stuff chunks are valid
        }

        var result = rawChunkValue;
        if (t.defName == "ChunkLimestone")
            result *= limestoneMultiplier; // raw chunk limestone is much more valuable

        if(RimgateMod.Debug)
            Log.Message($"IngredientValueGetter_Cement: {t.defName} is chunk, value per unit: {result}");

        return result;
    }

    public static bool IsStoneyStuff(ThingDef t)
    {
        if (!t.IsStuff || t.stuffProps?.categories == null)
            return false;

        foreach (StuffCategoryDef category in t.stuffProps.categories)
        {
            if (category == StuffCategoryDefOf.Stony)
                return true;
        }

        return false;
    }

    public static bool IsChunk(ThingDef t)
    {
        if(t?.thingCategories == null)
            return false;

        foreach (ThingCategoryDef category in t.thingCategories)
        {
            if (category == ThingCategoryDefOf.StoneChunks)
                return true;
        }

        return false;
    }

    public override string ExtraDescriptionLine(RecipeDef r)
    {
        return "RG_CementValueDesc".Translate(rawChunkValue, limestoneMultiplier.ToStringPercent());
    }

    public override string BillRequirementsDescription(RecipeDef r, IngredientCount ing)
    {
        return $"{ing.GetBaseCount()}x {ing.filter.Summary}";
    }
}
