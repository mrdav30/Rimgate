using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace Rimgate;

public static class SarcophagusRestUtility
{
    private static List<ThingDef> sarcophagusDefsBestToWorst => sarcophagusDefsBestToWorstCached ??= RestUtility.AllBedDefBestToWorst
        .Where(x => 
            x.thingClass == typeof(Building_Bed_Sarcophagus)).ToList();

    private static List<ThingDef> sarcophagusDefsBestToWorstCached;

    private static List<Thing> tempSarcophagusList = new();

    public static string NoPathTrans;

    public static void ResetStaticData()
    {
        NoPathTrans = "NoPath".Translate();
    }

    public static bool IsValidBedForUserType(Building_Bed_Sarcophagus bedSarcophagus, Pawn pawn)
    {
        // VetPods: skip execution early and return true if patient is an animal
        if (pawn.RaceProps.Animal && !bedSarcophagus.def.building.bed_humanlike)
            return true;
        
        // Otherwise, check for humanlike patients
        bool isSlave = pawn.GuestStatus == GuestStatus.Slave;
        bool isPrisoner = pawn.GuestStatus == GuestStatus.Prisoner;

        if (bedSarcophagus.ForSlaves != isSlave)
            return false;

        if (bedSarcophagus.ForPrisoners != isPrisoner)
            return false;

        if (bedSarcophagus.ForColonists 
            && (!pawn.IsColonist || pawn.GuestStatus == GuestStatus.Guest) 
            && !bedSarcophagus.allowGuests)
        {
            return false;
        }

        return true;
    }

    public static bool IsValidSarcophagusFor(
        Building_Bed_Sarcophagus bedSarcophagus,
        Pawn patientPawn,
        Pawn travelerPawn,
        GuestStatus? guestStatus = null)
    {
        if (bedSarcophagus == null)
            return false;

        if (!bedSarcophagus.powerComp.PowerOn)
            return false;

        if (bedSarcophagus.IsForbidden(travelerPawn))
            return false;

        if (!travelerPawn.CanReserve(bedSarcophagus))
        {
            Pawn otherPawn = travelerPawn.Map.reservationManager.FirstRespectedReserver(bedSarcophagus, patientPawn);
            if (otherPawn != null)
            {
                JobFailReason.Is("ReservedBy".Translate(otherPawn.LabelShort, otherPawn));
            }
            return false;
        }

        if (!travelerPawn.CanReach(bedSarcophagus, PathEndMode.OnCell, Danger.Deadly))
        {
            JobFailReason.Is(NoPathTrans);
            return false;
        }

        if (travelerPawn.Map.designationManager.DesignationOn(bedSarcophagus, DesignationDefOf.Deconstruct) != null)
        {
            return false;
        }

        if (!RestUtility.CanUseBedEver(patientPawn, bedSarcophagus.def))
        {
            return false;
        }

        if (!IsValidBedForUserType(bedSarcophagus, patientPawn))
        {
            return false;
        }

        if (!SarcophagusHealthAIUtility.ShouldSeekSarcophagusRest(patientPawn, bedSarcophagus))
        {
            return false;
        }

        if (!SarcophagusHealthAIUtility.HasAllowedMedicalCareCategory(patientPawn))
        {
            return false;
        }

        if (!SarcophagusHealthAIUtility.IsValidRaceForSarcophagus(patientPawn, bedSarcophagus.DisallowedRaces))
        {
            return false;
        }

        if (!SarcophagusHealthAIUtility.IsValidXenotypeForSarcophagus(patientPawn, bedSarcophagus.DisallowedXenotypes))
        {
            return false;
        }

        if (SarcophagusHealthAIUtility.HasUsageBlockingHediffs(patientPawn, bedSarcophagus.UsageBlockingHediffs))
        {
            return false;
        }

        if (SarcophagusHealthAIUtility.HasUsageBlockingTraits(patientPawn, bedSarcophagus.UsageBlockingTraits))
        {
            return false;
        }

        if (bedSarcophagus.Aborted) 
            return false;

        if (bedSarcophagus.IsBurning())
            return false;
 
        if (bedSarcophagus.IsBrokenDown())
            return false;

        return true;
    }

    public static Building_Bed_Sarcophagus FindBestSarcophagus(Pawn pawn, Pawn patient)
    {
        // Skip if there are no sarcophagus bed defs
        if (!sarcophagusDefsBestToWorst.Any())
        {
            return null;
        }

        Map map = patient.Map;
        ListerThings listerThings = map.listerThings;
        tempSarcophagusList.Clear();

        // Prioritize searching for usable sarcophagi by distance, followed by sarcophagus type and path danger level
        try
        {
            foreach (ThingDef sarcophagusDef in sarcophagusDefsBestToWorst)
            {
                // Skip sarcophagus types that the patient can never use
                if (!RestUtility.CanUseBedEver(patient, sarcophagusDef))
                    continue;

                // Check each sarcophagus thing of the current type on the map, and add the ones usable by the current patient to a temporary list
                foreach (Thing sarcophagus in listerThings.ThingsOfDef(sarcophagusDef))
                {
                    if (sarcophagus is Building_Bed_Sarcophagus { Medical: true } bedSarcophagus
                        && IsValidSarcophagusFor(bedSarcophagus, patient, pawn, patient.GuestStatus))
                    {
                        tempSarcophagusList.Add(bedSarcophagus);
                    }
                }
            }

            // Look for the closest reachable sarcophagus from the temporary list, going down by danger level
            for (int i = 0; i < 2; i++)
            {
                Danger maxDanger = i == 0 ? Danger.None : Danger.Deadly;

                Building_Bed_Sarcophagus bedSarcophagus = (Building_Bed_Sarcophagus)GenClosest.ClosestThingReachable(
                    patient.Position,
                    map,
                    ThingRequest.ForUndefined(),
                    PathEndMode.OnCell,
                    TraverseParms.For(pawn),
                    validator: thing => thing.Position.GetDangerFor(patient, map) <= maxDanger,
                    customGlobalSearchSet: tempSarcophagusList);

                if (bedSarcophagus != null)
                    return bedSarcophagus;
            }
        }
        finally 
        { 
            // Clean up out temporary list once we're done
            tempSarcophagusList.Clear();
        }

        // Can't find any valid sarcophagi
        return null;
    }
}
