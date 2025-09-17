using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace Rimgate;

public class Comp_SwitchWeapon : ThingComp
{
    public CompProperties_SwitchWeapon Props => (CompProperties_SwitchWeapon)props;

    public ThingWithComps Weapon => parent;

    private ThingWithComps _cachedSwitchWeapon;

    private Pawn _cachedPawn;
    public Pawn Pawn
    {
        get
        {
            if (_cachedPawn == null)
            {
                var equipable = parent.GetComp<CompEquippable>();
                if (equipable != null &&
                    equipable.ParentHolder is Pawn_EquipmentTracker parentHolder)
                {
                    _cachedPawn = parentHolder.pawn;
                }
            }

            return _cachedPawn;
        }
    }

    public IEnumerable<Gizmo> SwitchWeaponOptions()
    {
        if (Pawn == null || !Pawn.IsPlayerControlled)
            yield break;

        ThingDef weaponDef = Props.alternateDef;
        yield return new Command_Action()
        {
            defaultLabel = "RG_SwitchToCommandLabel".Translate(weaponDef.label),
            defaultDesc = "RG_SwitchToCommand_Desc".Translate(weaponDef.label),
            activateSound = SoundDefOf.Click,
            icon = weaponDef.uiIcon,
            action = ToggleWeapon
        };
    }

    public void ToggleWeapon()
    {
        if (Pawn == null || Weapon == null)
            return;

        GetOrCreateAlternate();

        if (_cachedSwitchWeapon == null)
        {
            if (RimgateMod.Debug)
                Log.Warning($"Rimgate :: unable to get switch weapon for {Weapon}");
            return;
        }

        Pawn.equipment.Remove(Weapon);
        Pawn.equipment.AddEquipment(_cachedSwitchWeapon);
    }

    public ThingWithComps GetOrCreateAlternate()
    {
        // Make other version if needed
        if (_cachedSwitchWeapon == null)
        {
            if (RimgateMod.Debug)
                Log.Message($"Rimgate :: creating new switch weapon {Props.alternateDef} for {Weapon}");

            _cachedSwitchWeapon = (ThingWithComps)ThingMaker.MakeThing(Props.alternateDef, null);
            if (_cachedSwitchWeapon == null) return null;
        }

        _cachedSwitchWeapon.compQuality = Weapon.compQuality;

        // Copy material if stuffable
        if (Weapon?.def.MadeFromStuff == true
            && Weapon.Stuff != null
            && _cachedSwitchWeapon.def.MadeFromStuff)
        {
            _cachedSwitchWeapon.SetStuffDirect(Weapon.Stuff);
        }

        _cachedSwitchWeapon.HitPoints = Weapon.HitPoints;

        Comp_SwitchWeapon comp = ThingCompUtility.TryGetComp<Comp_SwitchWeapon>(_cachedSwitchWeapon);
        if (comp != null)
            comp._cachedSwitchWeapon ??= Weapon;

        return _cachedSwitchWeapon;
    }
}
