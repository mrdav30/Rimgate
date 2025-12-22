using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimgate.HarmonyPatches;

[HarmonyPatch(typeof(Widgets), nameof(Widgets.DefIcon))]
public static class Harmony_Widgets_DefIcon_BiomaterialRecoveryDef
{
    public static bool Prefix(
        Rect rect,
        Def def,
        ThingDef stuffDef = null,
        float scale = 1f,
        ThingStyleDef thingStyleDef = null,
        bool drawPlaceholder = false,
        Color? color = null,
        Material material = null,
        int? graphicIndexOverride = null,
        float alpha = 1f)
    {
        if (def is not BiomaterialRecoveryDef bioDef)
            return true; // let vanilla handle everything else

        // 1) If the def exposes an explicit UI icon, use it
        Texture2D tex = bioDef.UIIcon;
        if (tex != null)
        {
            Widgets.DrawTextureFitted(rect, tex, scale, material, alpha);
            return false;
        }

        if (tex != null)
        {
            Widgets.DrawTextureFitted(rect, tex, scale, material, alpha);
            return false;
        }

        // 2) Fallback: show the recovered item’s icon
        ThingDef thingIconDef = bioDef.spawnThingOverride 
            ?? bioDef.removesHediff?.spawnThingOnRemoved;

        if (thingIconDef != null)
        {
            Widgets.ThingIcon(rect, thingIconDef, stuffDef, thingStyleDef, scale, color, graphicIndexOverride, alpha);
            return false;
        }

        // 3) Placeholder if requested
        if (drawPlaceholder)
        {
            Widgets.DrawTextureFitted(rect, Widgets.PlaceholderIconTex, scale, material, alpha);
            return false;
        }

        // Nothing to draw, skip vanilla
        return false;
    }
}
