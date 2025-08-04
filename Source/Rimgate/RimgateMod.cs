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