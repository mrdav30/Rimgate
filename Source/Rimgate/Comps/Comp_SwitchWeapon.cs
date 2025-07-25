using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace Rimgate;

public class Comp_SwitchWeapon : ThingComp
{
    private CompEquippable compEquippable;
    public Dictionary<ThingDef, Thing> generatedWeapons;
    private List<ThingDef> thingDefs;
    private List<Thing> things;

    public CompProperties_SwitchWeapon Props => this.props as CompProperties_SwitchWeapon;

    private CompEquippable CompEquippable
    {
        get
        {
            if (compEquippable == null)
                compEquippable = parent.GetComp<CompEquippable>();
            return compEquippable;
        }
    }

    public Pawn Pawn
    {
        get
        {
            if (CompEquippable.ParentHolder is not Pawn_EquipmentTracker parentHolder
                || parentHolder.pawn == null) return null;

            return parentHolder.pawn;
        }
    }

    public IEnumerable<Gizmo> SwitchWeaponOptions()
    {
        foreach (ThingDef thingDef in Props.weaponsToSwitch)
        {
            ThingDef weaponDef = thingDef;
            Command_Action commandAction = new Command_Action();
            commandAction.defaultLabel = weaponDef.LabelCap;
            commandAction.defaultDesc =  weaponDef.LabelCap;
            commandAction.activateSound = SoundDefOf.Click;
            commandAction.icon = weaponDef.uiIcon;
            commandAction.action = (Action)(() =>
            {
                Pawn pawn = Pawn;
                if (generatedWeapons == null)
                    generatedWeapons = new Dictionary<ThingDef, Thing>();

                Thing thing;
                if (!generatedWeapons.TryGetValue(weaponDef, out thing))
                {
                    thing = ThingMaker.MakeThing(weaponDef, null);
                    generatedWeapons[weaponDef] = thing;
                }

                generatedWeapons[parent.def] = parent;
                ThingCompUtility.TryGetComp<Comp_SwitchWeapon>(thing).generatedWeapons = generatedWeapons;
                pawn.equipment.Remove(parent);
                pawn.equipment.AddEquipment(thing as ThingWithComps);
            });

            yield return (Gizmo)commandAction;
        }
    }

    public virtual void PostExposeData()
    {
        base.PostExposeData();
        generatedWeapons?.Remove(parent.def);
        Scribe_Collections.Look<ThingDef, Thing>(
            ref generatedWeapons,
            "generatedWeapons",
            LookMode.Def,
            LookMode.Deep,
            ref thingDefs,
            ref things);
    }
}
