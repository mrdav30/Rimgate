using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.Sound;

namespace Rimgate;

public class Comp_AlternateApparel : ThingComp
{
    public CompProperties_AlternateApparel Props => (CompProperties_AlternateApparel)props;

    public Apparel ApparelDef => parent as Apparel;

    public Pawn Pawn => ApparelDef?.Wearer;

    public Apparel CachedAlternateHelmet;  // Either open or closed, whichever is not this

    public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
    {
        if (Pawn == null || !Pawn.IsPlayerControlled)
            yield break;

        ThingDef apparelDef = Props.attachedHeadgearDef;
        string label = !Props.isClosedState ? " (close)" : "";
        yield return new Command_Action
        {
            defaultLabel = "RG_ToggleableHeadgearCommand_Label".Translate(apparelDef.label + label),
            defaultDesc = "RG_ToggleableHeadgearCommand_Desc".Translate(apparelDef.label + label),
            activateSound = Rimgate_DefOf.Rimgate_GoauldGuardHelmToggle,
            icon = Props.attachedHeadgearDef.uiIcon,
            action = ToggleHeadgear
        };
    }

    private void ToggleHeadgear()
    {
        if (Pawn == null || ApparelDef == null)
            return;

        GetOrCreateAlternate();
        if (CachedAlternateHelmet == null)
        {
            if (RimgateMod.debug)
                Log.Warning($"Rimgate :: unable to get alternate headgear for {ApparelDef}");
            return;
        }

        // Equip the new helmet
        Pawn.apparel.Wear(CachedAlternateHelmet, dropReplacedApparel: false);
    }

    private Apparel GetOrCreateAlternate()
    {
        // Make other version if needed
        if (CachedAlternateHelmet == null)
        {
            if (RimgateMod.debug)
                Log.Message($"Rimgate :: creating new alternate helm {Props.attachedHeadgearDef} for {ApparelDef}");

            CachedAlternateHelmet = (Apparel)ThingMaker.MakeThing(Props.attachedHeadgearDef);
            if (CachedAlternateHelmet == null) return null;

            CachedAlternateHelmet.compQuality = ApparelDef.compQuality;

            // Copy material if stuffable
            if (ApparelDef?.def.MadeFromStuff == true
                && ApparelDef.Stuff != null
                && CachedAlternateHelmet.def.MadeFromStuff)
            {
                CachedAlternateHelmet.SetStuffDirect(ApparelDef.Stuff);
            }

            Comp_AlternateApparel comp = ThingCompUtility.TryGetComp<Comp_AlternateApparel>(CachedAlternateHelmet);
            if (comp != null)
                comp.CachedAlternateHelmet ??= ApparelDef;
        }

        return CachedAlternateHelmet;
    }
}
