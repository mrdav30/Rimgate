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

    public static bool Debug = true;
    public static bool DebugZPM = false;

    public static bool FlagAsteroids = true;

    public static readonly IntVec3 MinMapSize = new IntVec3(75, 1, 75);

    public const string StargateQuestTag = "Rimgate_QuestStargateSite";

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