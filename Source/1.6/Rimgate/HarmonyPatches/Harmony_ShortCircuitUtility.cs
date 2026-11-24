using HarmonyLib;
using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace Rimgate.HarmonyPatches;

[HarmonyPatch(typeof(ShortCircuitUtility), "DrainBatteriesAndCauseExplosion")]
public static class Harmony_ShortCircuitUtility
{
    public static bool Prefix(
        PowerNet net,
        Building culprit,
        ref float totalEnergy,
        ref float explosionRadius)
    {
        if (net?.batteryComps == null || net.batteryComps.Count == 0)
        {
            totalEnergy = 0f;
            explosionRadius = 0f;
            return false; // nothing to do, skip original
        }

        // Filter out ZPM batteries; everything else behaves vanilla
        bool IsZpmBattery(CompPowerBattery b) =>
            b?.parent?.def == RimgateDefOf.Rimgate_ZPM;

        var toDrain = net.batteryComps.Where(b =>
            b != null && !IsZpmBattery(b)).ToList();

        // Sum and drain ONLY non-ZPM energy
        totalEnergy = 0f;
        foreach (var bat in toDrain)
        {
            float e = bat.StoredEnergy;
            if (e > 0f)
            {
                totalEnergy += e;
                bat.DrawPower(e);
            }
        }

        // If there’s no non-ZPM energy on the net, do not explode at all
        if (totalEnergy <= 0f)
        {
            explosionRadius = 0f;
            return false; // suppress vanilla explosion
        }

        // Vanilla radius curve from remaining (non-ZPM) energy
        explosionRadius = Mathf.Sqrt(totalEnergy) * 0.05f;
        explosionRadius = Mathf.Clamp(explosionRadius, 1.5f, 14.9f);

        // Do the explosions here (since we’re skipping original)
        GenExplosion.DoExplosion(culprit.Position, net.Map, explosionRadius, DamageDefOf.Flame, instigator: null);
        if (explosionRadius > 3.5f)
        {
            GenExplosion.DoExplosion(culprit.Position, net.Map, explosionRadius * 0.3f, DamageDefOf.Bomb, instigator: null);
        }

        return false; // handled; skip original
    }
}
