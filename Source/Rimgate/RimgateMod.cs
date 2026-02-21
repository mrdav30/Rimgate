using HarmonyLib;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace Rimgate;

public class RimgateMod : Mod
{
    public static Harmony harmony;

    public static RimgateModSettings Settings { get; private set; }

    public RimgateMod(ModContentPack pack) : base(pack)
    {
        Settings = GetSettings<RimgateModSettings>();

        try
        {
            harmony = new Harmony("com.Rimgate.patches");
            harmony.PatchAll();
        }
        catch (Exception ex)
        {
            LogUtil.Error(ex, "Failed to apply Harmony patches");
        }
    }

    public override string SettingsCategory() => (string)"RG_Settings_Title".Translate();

    public override void DoSettingsWindowContents(Rect inRect)
    {
        Settings.DoSettingsWindowContents(inRect);
    }
}
