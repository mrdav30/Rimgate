using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Rimgate;

public class Comp_AlternateApparel : ThingComp
{
    public CompProperties_AlternateApparel Props => (CompProperties_AlternateApparel)props;

    public Apparel Apparel => parent as Apparel;

    public Pawn Pawn => Apparel?.Wearer;

    // Either open or closed, whichever is not this
    private Apparel _cachedAlternate;

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
        if (_cachedAlternate == null)
        {
            if (RimgateMod.Debug)
                Log.Warning($"Rimgate :: unable to get alternate for {Apparel}");
            return;
        }

        // Equip the alternate
        Pawn.apparel.Wear(_cachedAlternate, dropReplacedApparel: false);
    }

    private Apparel GetOrCreateAlternate()
    {
        // Make other version if needed
        if (_cachedAlternate == null)
        {
            if (RimgateMod.Debug)
                Log.Message($"Rimgate :: creating new alternate {Props.alternateDef} for {Apparel}");

            _cachedAlternate = (Apparel)ThingMaker.MakeThing(Props.alternateDef);
            if (_cachedAlternate == null) return null;
        }

        _cachedAlternate.compQuality = Apparel.compQuality;

        // Copy material if stuffable
        if (Apparel?.def.MadeFromStuff == true
            && Apparel.Stuff != null
            && _cachedAlternate.def.MadeFromStuff)
        {
            _cachedAlternate.SetStuffDirect(Apparel.Stuff);
        }

        _cachedAlternate.HitPoints = Apparel.HitPoints;

        Comp_AlternateApparel comp = ThingCompUtility.TryGetComp<Comp_AlternateApparel>(_cachedAlternate);
        if (comp != null)
            comp._cachedAlternate ??= Apparel;

        return _cachedAlternate;
    }
}
