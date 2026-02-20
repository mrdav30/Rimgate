using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Rimgate;

public class Comp_AlternateApparel : ThingComp
{
    public CompProperties_AlternateApparel Props => (CompProperties_AlternateApparel)props;

    public Apparel Apparel => parent as Apparel;

    public Pawn Pawn => Apparel?.Wearer;

    // Either open or closed, whichever is not this
    public Apparel CachedAlternate;

    public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
    {
        if (Pawn == null || !Pawn.IsPlayerControlled)
            yield break;

        string label = Props.alternateDef.label;
        yield return new Command_Action
        {
            defaultLabel = "RG_SwitchToCommandLabel".Translate(label),
            defaultDesc = "RG_SwitchToCommand_Desc".Translate(label),
            activateSound = Props.toggleSound ?? SoundDefOf.Click,
            icon = Props.alternateDef.uiIcon,
            action = ToggleApparelSwitch
        };
    }

    private void ToggleApparelSwitch()
    {
        if (Pawn == null || Apparel == null)
            return;

        GetOrCreateAlternate();
        if (CachedAlternate == null)
        {
            if (RimgateMod.Debug)
                Log.Warning($"Rimgate :: unable to get alternate for {Apparel}");
            return;
        }

        // Equip the alternate
        Pawn.apparel.Wear(CachedAlternate, dropReplacedApparel: false);
    }

    private Apparel GetOrCreateAlternate()
    {
        // Make other version if needed
        if (CachedAlternate == null)
        {
            if (RimgateMod.Debug)
                Log.Message($"Rimgate :: creating new alternate {Props.alternateDef} for {Apparel} {(Apparel.Stuff != null ? $"using {Apparel.Stuff}" : "")}");

            CachedAlternate = (Apparel)ThingMaker.MakeThing(Props.alternateDef, Apparel.Stuff);
            if (CachedAlternate == null) return null;
        }

        CachedAlternate.compQuality = Apparel.compQuality;
        CachedAlternate.HitPoints = Apparel.HitPoints;

        Comp_AlternateApparel comp = ThingCompUtility.TryGetComp<Comp_AlternateApparel>(CachedAlternate);
        if (comp != null)
            comp.CachedAlternate ??= Apparel;

        return CachedAlternate;
    }
}
