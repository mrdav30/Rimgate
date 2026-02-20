using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Rimgate;

public class Comp_SwitchWeapon : ThingComp
{
    public CompProperties_SwitchWeapon Props => (CompProperties_SwitchWeapon)props;

    public ThingWithComps Weapon => parent;

    public ThingWithComps CachedSwitchWeapon;

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

        if (CachedSwitchWeapon == null)
        {
            if (RimgateMod.Debug)
                Log.Warning($"Rimgate :: unable to get switch weapon for {Weapon}");
            return;
        }

        Pawn.equipment.Remove(Weapon);
        Pawn.equipment.AddEquipment(CachedSwitchWeapon);
    }

    public ThingWithComps GetOrCreateAlternate()
    {
        // Make other version if needed
        if (CachedSwitchWeapon == null)
        {
            if (RimgateMod.Debug)
                Log.Message($"Rimgate :: creating new switch weapon {Props.alternateDef} for {Weapon} {(Weapon.Stuff != null ? $"using {Weapon.Stuff}" : "")}");

            CachedSwitchWeapon = (ThingWithComps)ThingMaker.MakeThing(Props.alternateDef, Weapon.Stuff);
            if (CachedSwitchWeapon == null) return null;
        }

        CachedSwitchWeapon.compQuality = Weapon.compQuality;
        CachedSwitchWeapon.HitPoints = Weapon.HitPoints;

        Comp_SwitchWeapon comp = ThingCompUtility.TryGetComp<Comp_SwitchWeapon>(CachedSwitchWeapon);
        if (comp != null)
            comp.CachedSwitchWeapon ??= Weapon;

        return CachedSwitchWeapon;
    }
}
