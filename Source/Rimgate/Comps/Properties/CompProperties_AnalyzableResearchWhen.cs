using RimWorld;
using Verse;

namespace Rimgate;

public class CompProperties_AnalyzableResearchWhen : CompProperties_CompAnalyzableUnlockResearch
{
    public ResearchProjectDef requiresResearchDef;

    public bool hideWhenDone = true;

    // 0–1
    public float catastrophicFailureChance = 0f;

    // destroy parent on failure
    public bool catastrophicDestroysThing = true;          

    public bool catastrophicExplosion = false;

    // standard small explosion radius
    public float catastrophicExplosionRadius = 2.9f;       

    // defaults to Bomb if null
    public DamageDef catastrophicExplosionDamageDef;       

    // base damage if exploding
    public int catastrophicExplosionDamage = 50;           

    // if true: no research unlock
    public bool catastrophicBlocksResearch = true;

    // Optional: letter feedback
    public string catastrophicLetterLabelKey;

    public string catastrophicLetterTextKey;

    public LetterDef catastrophicLetterDef;

    public CompProperties_AnalyzableResearchWhen() => compClass = typeof(Comp_AnalyzableResearchWhen);
}
