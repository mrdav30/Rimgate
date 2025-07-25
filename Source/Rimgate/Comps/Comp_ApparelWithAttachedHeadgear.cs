using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Rimgate;

public class Comp_ApparelWithAttachedHeadgear : ThingComp
{
    public bool isHatOn;

    public CompProperties_ApparelWithAttachedHeadgear Props => (CompProperties_ApparelWithAttachedHeadgear)this.props;

    public Apparel Apparel => this.parent as Apparel;

    public Pawn Pawn => this.Apparel?.Wearer;

    public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
    {
        if(Pawn == null || !Pawn.IsPlayerControlled)
        {
            base.CompGetWornGizmosExtra();
            yield break;
        }

        foreach (Gizmo gizmo in base.CompGetWornGizmosExtra())
            yield return gizmo;
 
        Command_Toggle commandToggle = new Command_Toggle();
        commandToggle.defaultLabel = "Rimgate_ToggleableHeadgearCommand_Label".Translate(Props.attachedHeadgearDef.label);
        commandToggle.defaultDesc = "Rimgate_ToggleableHeadgearCommand_Desc".Translate(Props.attachedHeadgearDef.label);
        commandToggle.icon = ContentFinder<Texture2D>.Get(Props.toggleUiIconPath, true);

        commandToggle.isActive = () => isHatOn;

        commandToggle.toggleAction = delegate
        {
            isHatOn = !isHatOn;
        };
        commandToggle.turnOffSound = (SoundDef)null;
        commandToggle.turnOnSound = (SoundDef)null;

        yield return commandToggle;
    }

    public override void PostExposeData()
    {
        Scribe_Values.Look<bool>(ref this.isHatOn, "isHatOn", false, true);
    }

}
