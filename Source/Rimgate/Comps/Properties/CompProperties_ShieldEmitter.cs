using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Rimgate;

public class CompProperties_ShieldEmitter : CompProperties
{
    public bool interceptAirProjectiles;

    public bool interceptGroundProjectiles;

    public bool interceptNonHostileProjectiles = true;

    public bool interceptOutgoingProjectiles;

    public EffecterDef reactivateEffect;

    public string stressLabel = "Shield Stress Level";

    public int resetTime = 30000;

    public SoundDef startupSound;

    public SoundDef shutdownSound;

    public SoundDef impactSound;

    public SoundDef breakSound;

    public bool sizeScalesPowerUsage = false;

    public float powerUsageRangeMax = float.MaxValue;

    public bool sizeScalesFuelUsage = false;

    public float fuelConsumptionRangeMax = float.MaxValue;

    public FloatRange heatGenRange = new FloatRange(0.0f, 100f);

    public float stressReduction = 1f;

    public float stressPerDamage = 0.003f;

    public bool disabledByEmp = true;

    public float empDamageFactor = 5f;

    public float shieldOverloadThreshold = 0.9f;

    public float shieldOverloadChance = 0.3f;

    public int extraOverloadRange = 3;

    public DamageDef overloadDamageType;

    public bool explodeOnCollapse;

    public bool shieldCanBeScaled;

    public IntRange shieldScaleLimits = new IntRange(0, 10);

    public int shieldScaleDefault = 5;

    public Color shieldColor = Color.blue;

    public Color inactiveColor = new Color(0.2f, 0.2f, 0.2f);

    public bool drawInterceptCone;

    public float minAlpha;

    public float idlePulseSpeed;

    public bool podBlocker = true;

    public bool podBlockFriendlies;

    public List<Type> skyfallerClassWhitelist = new List<Type>();

    public override void ResolveReferences(ThingDef parentDef)
    {
        base.ResolveReferences(parentDef);
        startupSound ??= SoundDefOf.Power_OnSmall;
        shutdownSound ??= SoundDefOf.Power_OffSmall;
        impactSound ??= SoundDefOf.EnergyShield_AbsorbDamage;
        breakSound ??= SoundDefOf.EnergyShield_Reset;
        reactivateEffect ??= EffecterDefOf.ActivatorProximityTriggered;
    }

    public CompProperties_ShieldEmitter() => compClass = typeof(Comp_ShieldEmitter);
}
