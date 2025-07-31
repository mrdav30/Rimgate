using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Rimgate;

public class Comp_AlternateApparel : ThingComp
{
    public Apparel cachedAlternateHelmet;  // Either open or closed, whichever is not this

    public CompProperties_AlternateApparel Props => (CompProperties_AlternateApparel)this.props;

    public Apparel Apparel => this.parent as Apparel;

    public Pawn Pawn => this.Apparel?.Wearer;

    public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
    {
        if (Pawn == null || !Pawn.IsPlayerControlled)
            yield break;

        foreach (Gizmo gizmo in base.CompGetWornGizmosExtra())
            yield return gizmo;

        string label = !Props.isClosedState ? " (close)" : "";
        yield return new Command_Action
        {
            defaultLabel = "RG_ToggleableHeadgearCommand_Label".Translate(Props.attachedHeadgearDef.label + label),
            defaultDesc = "RG_ToggleableHeadgearCommand_Desc".Translate(Props.attachedHeadgearDef.label + label),
            icon = ContentFinder<Texture2D>.Get(Props.toggleUiIconPath),
            action = ToggleHelmet
        };
    }

    private void ToggleHelmet()
    {
        if (Pawn == null || Apparel == null)
            return;

        // Determine the replacement helmet
        var currentHelmet = Apparel;
        var newHelmet = GetOrCreateAlternate(currentHelmet);

        if (newHelmet == null)
        {
            if (RimgateMod.debug)
                Log.Warning($"Rimgate :: unable to get alternate headgear for {currentHelmet}");
            return;
        }

        Rimgate_DefOf.Rimgate_GoauldGuardHelmToggle.PlayOneShot(new TargetInfo(Pawn.Position, Pawn.Map));

        // Equip the new helmet
        Pawn.apparel.Wear(newHelmet, dropReplacedApparel: false);
    }

    private Apparel GetOrCreateAlternate(Apparel current)
    {
        // Make other version if needed
        if (cachedAlternateHelmet == null)
        {
            if (RimgateMod.debug)
                Log.Message($"Rimgate :: creating new alternate helm {Props.attachedHeadgearDef} for {current}");

            cachedAlternateHelmet = (Apparel)ThingMaker.MakeThing(Props.attachedHeadgearDef);
            cachedAlternateHelmet.compQuality = current.compQuality;

            // Copy material if stuffable
            if (current?.def.MadeFromStuff == true
                && current.Stuff != null
                && cachedAlternateHelmet.def.MadeFromStuff)
            {
                cachedAlternateHelmet.SetStuffDirect(current.Stuff);
            }

            Comp_AlternateApparel comp = ThingCompUtility.TryGetComp<Comp_AlternateApparel>(cachedAlternateHelmet);
            comp.cachedAlternateHelmet = Apparel;
        }

        return cachedAlternateHelmet;
    }
}
