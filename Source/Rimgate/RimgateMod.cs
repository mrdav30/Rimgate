using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace Rimgate;

public class RimgateMod : Mod
{
    public static Harmony harmony;

    // TODO: move into settings
    public static bool Debug = true;

    public static bool FlagAsteroids = true;

    public static readonly IntVec3 MinMapSize = new IntVec3(75, 1, 75);

    public const string GateQuestTag = "Rimgate_GateQuestSite_tag";

    public RimgateMod(ModContentPack pack) : base(pack)
    {
        try
        {
            harmony = new Harmony("com.Rimgate.patches");
            harmony.PatchAll();    
        }
        catch (Exception ex)
        {
            Log.Error("Rimgate :: Failed to apply Harmony patches:\n" + ex);
        }
    }
}