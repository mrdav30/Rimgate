using HarmonyLib;
using RimWorld;
using Verse;

namespace Rimgate.HarmonyPatches;

// Disallow Guests from using Sarcophagi set for Prisoners or Slaves
// Ideology DLC uses different bed owner type gizmo compared with vanilla
[HarmonyPatch(typeof(Building_Bed), nameof(Building_Bed.SetBedOwnerTypeByInterface))]
public static class Harmony_Building_Bed_SetBedOwnerType
{
    public static void Postfix(Building_Bed __instance)
    {
        if (__instance is Building_Bed_Sarcophagus bedSarcophagus 
            && __instance.ForOwnerType != BedOwnerType.Colonist) 
        {
            bedSarcophagus.AllowGuests = false;
        }
    }
}
