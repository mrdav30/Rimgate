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

    public FloatRange powerUsageRange = new FloatRange(0.0f, 0.0f);

    public float fuelConsumptionRate = 1f;

    public FloatRange heatGenRange = new FloatRange(0.0f, 100f);

    public float stressReduction = 1f;

    public float stressPerDamage = 3f / 1000f;

    public float empDamageFactor = 5f;

    public float shieldOverloadThreshold = 0.9f;

    public float shieldOverloadChance = 0.3f;

    public int extraOverloadRange = 3;

    public DamageDef overloadDamageType;

    public bool explodeOnCollapse;

    public bool shieldCanBeScaled;

    public IntRange shieldScaleLimits = new IntRange(0, 10);

    public int shieldScaleDefault = 5;

    public Color shieldColour = Color.blue;

    public bool drawInterceptCone;

    public float minAlpha;

    public float idlePulseSpeed;

    public bool podBlocker = true;

    public bool podBlockFriendlies;

    public List<Type> skyfallerClassWhitelist = new List<Type>();

    public virtual void ResolveReferences(ThingDef parentDef)
    {
        base.ResolveReferences(parentDef);
        this.startupSound ??= SoundDefOf.Power_OnSmall;
        this.shutdownSound ??= SoundDefOf.Power_OffSmall;
        this.impactSound ??= SoundDefOf.EnergyShield_AbsorbDamage;
        this.breakSound ??= SoundDefOf.EnergyShield_Reset;
        this.reactivateEffect ??= EffecterDefOf.ActivatorProximityTriggered;
        this.overloadDamageType ??= DamageDefOf.EMP;
    }

    public CompProperties_ShieldEmitter() => this.compClass = typeof(Comp_ShieldEmitter);
}
