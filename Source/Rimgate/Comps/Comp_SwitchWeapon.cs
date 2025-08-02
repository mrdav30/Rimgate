using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace Rimgate;

public class Comp_SwitchWeapon : ThingComp
{
    public CompProperties_SwitchWeapon Props => (CompProperties_SwitchWeapon)this.props;

    public ThingWithComps WeaponDef => this.parent;

    public ThingWithComps CachedSwitchWeapon;

    private Pawn _pawn;
    public Pawn Pawn
    {
        get
        {
            if (_pawn == null)
            {
                var equipable = parent.GetComp<CompEquippable>();
                if(equipable.ParentHolder is Pawn_EquipmentTracker parentHolder)
                    _pawn = parentHolder.pawn;
            }

            return _pawn;
        }
    }

    public IEnumerable<Gizmo> SwitchWeaponOptions()
    {
        if (Pawn == null || !Pawn.IsPlayerControlled)
            yield break;

        ThingDef weaponDef = Props.weaponToSwitch;
        yield return new Command_Action()
        {
            defaultLabel = "RG_SwitchWeaponCommand_Label".Translate(weaponDef.label),
            defaultDesc = "RG_SwitchWeaponCommand_Desc".Translate(weaponDef.label),
            activateSound = SoundDefOf.Click,
            icon = weaponDef.uiIcon,
            action = ToggleWeapon
        };
    }

    public void ToggleWeapon()
    {
        if (this.Pawn == null || this.WeaponDef == null)
            return;

        GetOrCreateAlternate();

        if (this.CachedSwitchWeapon == null)
        {
            if (RimgateMod.debug)
                Log.Warning($"Rimgate :: unable to get switch weapon for {this.WeaponDef}");
            return;
        }

        this.Pawn.equipment.Remove(this.WeaponDef);
        this.Pawn.equipment.AddEquipment(this.CachedSwitchWeapon);
    }

    public ThingWithComps GetOrCreateAlternate()
    {
        // Make other version if needed
        if (CachedSwitchWeapon == null)
        {
            if (RimgateMod.debug)
                Log.Message($"Rimgate :: creating new switch weapon {Props.weaponToSwitch} for {WeaponDef}");

            CachedSwitchWeapon = (ThingWithComps)ThingMaker.MakeThing(Props.weaponToSwitch, null);
            if (CachedSwitchWeapon == null) return null;

            CachedSwitchWeapon.compQuality = WeaponDef.compQuality;

            // Copy material if stuffable
            if (WeaponDef?.def.MadeFromStuff == true
                && WeaponDef.Stuff != null
                && CachedSwitchWeapon.def.MadeFromStuff)
            {
                CachedSwitchWeapon.SetStuffDirect(WeaponDef.Stuff);
            }

            Comp_SwitchWeapon comp = ThingCompUtility.TryGetComp<Comp_SwitchWeapon>(CachedSwitchWeapon);
            if (comp != null)
                comp.CachedSwitchWeapon ??= WeaponDef;
        }

        return CachedSwitchWeapon;
    }
}
