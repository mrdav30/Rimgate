using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Runtime.CompilerServices;
using Verse;

namespace Rimgate.HarmonyPatches;

// <summary>
// Suppresses messages that would target GateQuestSite world objects.
// </summary>
[HarmonyPatch(typeof(Messages))]
public static class Harmony_Messages_Message
{
    [HarmonyPatch(typeof(Messages), "Message", new Type[] { typeof(string), typeof(LookTargets), typeof(MessageTypeDef), typeof(Quest), typeof(bool) })]
    [HarmonyPrefix]
    public static bool Prefix(string text, LookTargets lookTargets, MessageTypeDef def, Quest quest, bool historical)
    {
        GlobalTargetInfo target = lookTargets?.TryGetPrimaryTarget() ?? GlobalTargetInfo.Invalid;
        if (!ShouldSkip(target))
            return true;

        return false;
    }

    [HarmonyPatch(typeof(Messages), "Message", new Type[] { typeof(string), typeof(LookTargets), typeof(MessageTypeDef), typeof(bool) })]
    [HarmonyPrefix]
    public static bool Prefix(string text, LookTargets lookTargets, MessageTypeDef def, bool historical)
    {
        GlobalTargetInfo target = lookTargets?.TryGetPrimaryTarget() ?? GlobalTargetInfo.Invalid;
        if (!ShouldSkip(target))
            return true;

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ShouldSkip(GlobalTargetInfo info)
    {
        if (!info.IsValid)
            return false;
        MapParent mapParent = info.Map?.Parent ?? null;
        bool nobodyVisible = info.Map?.mapPawns.AnyPawnBlockingMapRemoval ?? true;
        if (mapParent == null || mapParent is not WorldObject_GateQuestSite || nobodyVisible)
            return false;

        LogUtil.Debug("Suppressed message for GateQuestSite");

        return true;
    }
}
