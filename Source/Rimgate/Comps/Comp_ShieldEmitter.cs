using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Rimgate;

[StaticConstructorOnStartup]
public class Comp_ShieldEmitter : ThingComp
{
    public static readonly Material ForceFieldMat = MaterialPool.MatFrom("Other/ForceField", ShaderDatabase.MoteGlow);

    public static readonly Material ForceFieldConeMat = MaterialPool.MatFrom("Other/ForceFieldCone", ShaderDatabase.MoteGlow);

    public static readonly MaterialPropertyBlock MatPropertyBlock = new MaterialPropertyBlock();

    public static readonly Color InactiveColor = new Color(0.2f, 0.2f, 0.2f);

    public float CurStressLevel;

    public float MaxStressLevel = 1f;

    public int TicksToReset;

    public bool Overloaded;

    public bool ActiveLastTick;

    public int CurShieldRadius = -1;

    public float LastTempChange;

    public Color CurrentColor;

    private int _lastInterceptTicks = -999999;

    private int _lastHitByEmpTicks = -999999;

    private float _lastInterceptAngle;

    private bool _debugInterceptNonHostileProjectiles = true;

    private bool _showShieldToggle;

    private CompPowerTrader _cachedPowerComp;

    private Comp_Toggle _cachedFlickableComp;

    private CompHeatPusher _cachedHeatComp;

    private CompRefuelable _cachedFuelComp;

    public CompProperties_ShieldEmitter Props => (CompProperties_ShieldEmitter)props;

    public virtual bool Active
    {
        get
        {
            if (Overloaded
                || PowerTrader != null && !PowerTrader.PowerOn
                || FuelComp != null && !FuelComp.HasFuel) return false;
            return Flicker == null || Flicker.SwitchIsOn;
        }
    }

    public Vector3 CurShieldPosition => parent.Position.ToVector3Shifted();

    public int SetShieldRadius
    {
        get => CurShieldRadius;
        set
        {
            CurShieldRadius = Mathf.Clamp(
                value,
                Props.shieldScaleLimits.min,
                Props.shieldScaleLimits.max);
        }
    }

    public bool ReactivatedThisTick
    {
        get => Find.TickManager.TicksGame - _lastInterceptTicks == Props.resetTime;
    }

    public float ScaleDamageFactor => Mathf.Lerp(0.5f, 2f, GetShieldScalePercentage);

    public float GetShieldScalePercentage
    {
        get
        {
            return !Props.shieldCanBeScaled
                ? 1f
                : Mathf.InverseLerp(Props.shieldScaleLimits.min, Props.shieldScaleLimits.max, CurShieldRadius);
        }
    }

    public CompPowerTrader PowerTrader
    {
        get
        {
            _cachedPowerComp ??= parent.GetComp<CompPowerTrader>();
            return _cachedPowerComp;
        }
    }

    public Comp_Toggle Flicker
    {
        get
        {
            _cachedFlickableComp ??= parent.GetComp<Comp_Toggle>();
            return _cachedFlickableComp;
        }
    }

    public CompHeatPusher HeatComp
    {
        get
        {
            _cachedHeatComp ??= parent.GetComp<CompHeatPusher>();
            return _cachedHeatComp;
        }
    }

    public CompRefuelable FuelComp
    {
        get
        {
            _cachedFuelComp ??= parent.GetComp<CompRefuelable>();
            return _cachedFuelComp;
        }
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        if (!ModLister.CheckRoyalty("Projectile interception"))
        {
            Log.Message("Rimgate :: Shield Setup skipped because user lacks Royalty.");
            parent.Destroy(DestroyMode.Vanish);
            return;
        }

        base.PostSpawnSetup(respawningAfterLoad);
        if (CurShieldRadius < Props.shieldScaleLimits.min)
            SetShieldRadius = Props.shieldScaleDefault;

        if (!respawningAfterLoad)
        {
            CurrentColor = Props.shieldColour;
            SetShieldRadius = Props.shieldScaleDefault;
        }
    }

    public bool CheckIntercept(Skyfaller skyfaller)
    {
        return HoldsAnyHostiles(skyfaller)
            && ShouldBeBlocked(skyfaller);
    }

    public bool HoldsAnyHostiles(Skyfaller skyfaller)
    {
        foreach (Thing directlyHeldThing in skyfaller.GetDirectlyHeldThings())
        {
            switch (directlyHeldThing)
            {
                case Pawn pawn when GenHostility.HostileTo(pawn, Faction.OfPlayer):
                    {
                        if (!pawn.IsSlaveOfColony && !pawn.IsPrisonerOfColony)
                            return true;
                        continue;

                    }
                case Building building when GenHostility.HostileTo(building, Faction.OfPlayer)
                    || building.Faction == Faction.OfMechanoids:
                    {
                        return true;
                    }
                case DropPodIncoming pod when HoldsAnyHostiles(pod):
                    return true;
                default:
                    continue;
            }
        }
        return false;
    }

    public bool HoldsAnyHostiles(DropPodIncoming pod)
    {
        Faction faction = pod.Faction;
        bool hostilePresent = faction != null
            ? FactionUtility.HostileTo(faction, Faction.OfPlayer)
            : 0 != 0;
        if (hostilePresent)
            return true;

        foreach (Thing thing in pod.Contents.innerContainer)
        {
            if (thing is not Pawn pawn)
                continue;

            if (GenHostility.HostileTo(pawn, Faction.OfPlayer))
            {
                if (!pawn.IsSlaveOfColony && !pawn.IsPrisonerOfColony)
                    return true;
            }
            else if (thing is Building building)
            {
                if (GenHostility.HostileTo(building, Faction.OfPlayer)
                    || building.Faction == Faction.OfMechanoids) return true;
            }
        }

        return false;
    }

    public bool ShouldBeBlocked(Skyfaller skyfaller)
    {
        return Props.skyfallerClassWhitelist.Any() == false
            || !Props.skyfallerClassWhitelist.Contains(skyfaller.def.thingClass)
                && Active
                && Props.podBlocker
                && IntVec3Utility.DistanceTo(
                    skyfaller.Position,
                    IntVec3Utility.ToIntVec3(CurShieldPosition)) <= CurShieldRadius;
    }

    public bool BombardmentCanStartFireAt(Bombardment bombardment, IntVec3 cell)
    {
        return !Active
            || !Props.interceptAirProjectiles
            || bombardment.instigator == null
            || !GenHostility.HostileTo(bombardment.instigator, parent)
                && !_debugInterceptNonHostileProjectiles
                && !Props.interceptNonHostileProjectiles
            || !cell.InHorDistOf(parent.Position, CurShieldRadius);
    }

    public bool CheckIntercept(Projectile projectile, Vector3 lastExactPos, Vector3 newExactPos)
    {
        if (!ModLister.CheckRoyalty("Projectile interception"))
            return false;

        Vector3 shieldPos = CurShieldPosition;
        float effectiveRadius = CurShieldRadius + projectile.def.projectile.SpeedTilesPerTick + 0.1f;

        float dx = newExactPos.x - shieldPos.x;
        float dz = newExactPos.z - shieldPos.z;
        float sqrDistance = dx * dx + dz * dz;

        bool outsideEffectiveRadius = sqrDistance > effectiveRadius * effectiveRadius;
        bool notActive = !Active;
        bool notInterceptable = !Comp_ShieldEmitter.InterceptsProjectile(Props, projectile);

        bool isHostile = projectile.Launcher != null
            && GenHostility.HostileTo(projectile.Launcher, parent);
        bool shouldIgnoreNonHostile = !_debugInterceptNonHostileProjectiles
            && Props.interceptNonHostileProjectiles;
        bool nonHostileShouldSkip = !isHostile && shouldIgnoreNonHostile;

        if (outsideEffectiveRadius
            || notActive
            || notInterceptable
            || nonHostileShouldSkip) return false;

        if (!Props.interceptOutgoingProjectiles)
        {
            Vector2 displacement = new Vector2(shieldPos.x, shieldPos.z) - new Vector2(lastExactPos.x, lastExactPos.z);
            if (displacement.sqrMagnitude <= CurShieldRadius * CurShieldRadius)
                return false;
        }

        bool intersectsShield = GenGeo.IntersectLineCircleOutline(
            new Vector2(shieldPos.x, shieldPos.z),
            CurShieldRadius,
            new Vector2(lastExactPos.x, lastExactPos.z),
            new Vector2(newExactPos.x, newExactPos.z));

        if (!intersectsShield)
            return false;

        _lastInterceptAngle = Vector3Utility.AngleToFlat(lastExactPos, GenThing.TrueCenter(parent));
        _lastInterceptTicks = Find.TickManager.TicksGame;

        if (projectile.def.projectile.damageDef == DamageDefOf.EMP)
            _lastHitByEmpTicks = Find.TickManager.TicksGame;

        TriggerEffecter(IntVec3Utility.ToIntVec3(newExactPos));
        UpdateStress(projectile);

        return true;
    }

    public static bool InterceptsProjectile(CompProperties_ShieldEmitter props, Projectile projectile)
    {
        return props.interceptGroundProjectiles && !projectile.def.projectile.flyOverhead
            || props.interceptAirProjectiles && projectile.def.projectile.flyOverhead;
    }

    public void TriggerEffecter(IntVec3 pos)
    {
        Effecter effecter = new Effecter(EffecterDefOf.Interceptor_BlockedProjectile);
        effecter.Trigger(new TargetInfo(pos, parent.Map, false), TargetInfo.Invalid, -1);
        effecter.Cleanup();
    }

    public void UpdateStress(bool tickUpdate = false, bool cooling = false)
    {
        if (tickUpdate)
        {
            float num1 = 0.0f;
            float num2 = num1 - Props.stressReduction;
            if (!Active)
                num2 = -Props.stressReduction;

            LastTempChange = (float)(num2 * 0.0099999997764825821 / 60.0);
            CurStressLevel = Mathf.Clamp(
                CurStressLevel + LastTempChange,
                0.0f,
                MaxStressLevel);
        }

        if (CurStressLevel >= MaxStressLevel)
            OverloadShield();
    }

    public void UpdateStress(Projectile projectile)
    {
        float num = (float)(projectile.DamageAmount * Props.stressPerDamage / 100f);
        if (projectile.def.projectile.damageDef == DamageDefOf.EMP)
            num *= Props.empDamageFactor;

        CurStressLevel = Mathf.Clamp(
            CurStressLevel + num * ScaleDamageFactor,
            0.0f,
            MaxStressLevel);
        UpdateStress();
    }

    public void UpdateStress(Skyfaller skyfaller)
    {
        float num = (float)(30000 * Props.stressPerDamage / 100f);
        if (skyfaller is DropPodIncoming)
            num /= 3f;
        CurStressLevel = Mathf.Clamp(
            CurStressLevel + num * ScaleDamageFactor,
            0.0f,
            MaxStressLevel);
        UpdateStress();
    }

    public void OverloadShield()
    {
        if (Props.breakSound != null)
            SoundStarter.PlayOneShot(
                Props.breakSound,
                new TargetInfo(parent.Position, parent.Map, false));

        FleckMaker.ThrowExplosionInterior(
            GenThing.TrueCenter(parent),
            parent.Map,
            FleckDefOf.ExplosionFlash);
        for (int index = 0; index < 6; ++index)
        {
            Vector3 location = GenThing.TrueCenter(parent)
                + Vector3Utility.HorizontalVectorFromAngle(Rand.Range(0, 360)) * Rand.Range(0.3f, 0.6f);
            FleckMaker.ThrowDustPuff(
                location,
                parent.Map,
                Rand.Range(0.8f, 1.2f));
        }

        TicksToReset = Props.resetTime;
        Overloaded = true;
        CurStressLevel = 0.0f;
        if (!Props.explodeOnCollapse
            || ThingCompUtility.TryGetComp<CompExplosive>(parent) == null) return;

        ThingCompUtility.TryGetComp<CompExplosive>(parent).StartWick(null);
    }

    public void UpdatePowerUsage()
    {
        if (!Active)
        {
            PowerTrader.PowerOutput = 0.0f;
            return;
        }

        PowerTrader.PowerOutput = Mathf.Lerp(
            Props.powerUsageRange.min,
            Props.powerUsageRange.max,
            GetShieldScalePercentage);
    }

    public void UpdateFuelUsage()
    {
        if (!Active)
            return;

        FuelComp.ConsumeFuel(Props.fuelConsumptionRate);
    }

    public override void CompTick()
    {
        if (Active)
        {
            if (ReactivatedThisTick && Props.reactivateEffect != null)
            {
                Effecter effecter = new Effecter(Props.reactivateEffect);
                effecter.Trigger(parent, TargetInfo.Invalid, -1);
                effecter.Cleanup();
            }

            UpdateStress(true);
            if (CurStressLevel >= Props.shieldOverloadThreshold
                && Rand.Chance(Props.shieldOverloadChance * (1f - (1f - CurStressLevel) * 10f)))
            {
                CellRect cellRect1 = GenAdj.OccupiedRect(parent);
                CellRect cellRect2 = cellRect1.ExpandedBy(Props.extraOverloadRange);
                GenExplosion.DoExplosion(
                    cellRect2.RandomCell,
                    parent.Map,
                    1.9f,
                    DamageDefOf.EMP,
                    null,
                    -1,
                    -1f,
                    null,
                    null,
                    null,
                    null,
                    null,
                    0.0f,
                    1,
                    new GasType?(),
                    new float?(),
                    byte.MaxValue,
                    false,
                    null,
                    0.0f,
                    1,
                    0.0f,
                    false,
                    new float?(),
                    null,
                    new FloatRange?(),
                    true,
                    1f,
                    0.0f,
                    true,
                    null,
                    1f,
                    null,
                    null,
                    null,
                    null);
            }
        }

        UpdateStress(true);
        if (PowerTrader != null && FuelComp == null)
        {
            UpdatePowerUsage();
            if (Overloaded && PowerTrader.PowerOn)
            {
                --TicksToReset;
                if (TicksToReset <= 0)
                    Overloaded = false;
            }
        }

        if (FuelComp != null && PowerTrader == null)
        {
            UpdateFuelUsage();
            if (Overloaded && FuelComp.HasFuel)
            {
                --TicksToReset;
                if (TicksToReset <= 0)
                    Overloaded = false;
            }
        }

        if (HeatComp != null)
            UpdateHeatPusher();
    }

    public void UpdateHeatPusher()
    {
        if (Active)
            HeatComp.Props.heatPerSecond = Mathf.Lerp(
                Props.heatGenRange.min,
                Props.heatGenRange.max,
                CurStressLevel);
        else
            HeatComp.Props.heatPerSecond = 0.0f;
    }

    public override void PostDraw()
    {
        base.PostDraw();
        Vector3 curShieldPosition = CurShieldPosition;
        curShieldPosition.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
        float currentAlpha = GetCurrentAlpha();
        if (currentAlpha > 0.0)
        {
            Color color = Active || !Find.Selector.IsSelected((object)parent)
                ? CurrentColor
                : Comp_ShieldEmitter.InactiveColor;
            color.a *= currentAlpha;
            Comp_ShieldEmitter.MatPropertyBlock.SetColor(ShaderPropertyIDs.Color, color);
            Matrix4x4 matrix4x4 = new Matrix4x4();
            matrix4x4.SetTRS(
                curShieldPosition,
                Quaternion.identity,
                new Vector3(
                    CurShieldRadius * 2f * (297f / 256f),
                    1f,
                    CurShieldRadius * 2f * (297f / 256f)));
            Graphics.DrawMesh(
                MeshPool.plane10,
                matrix4x4,
                Comp_ShieldEmitter.ForceFieldMat,
                0,
                null,
                0,
                Comp_ShieldEmitter.MatPropertyBlock);
        }
        float recentlyIntercepted = GetCurrentConeAlpha_RecentlyIntercepted();
        if (recentlyIntercepted <= 0)
            return;

        Color currentColor = CurrentColor;
        currentColor.a *= recentlyIntercepted;
        Comp_ShieldEmitter.MatPropertyBlock.SetColor(ShaderPropertyIDs.Color, currentColor);
        Matrix4x4 matrix4x4_1 = new Matrix4x4();
        matrix4x4_1.SetTRS(
            curShieldPosition,
            Quaternion.Euler(0.0f, _lastInterceptAngle - 90f, 0.0f),
            new Vector3(
                CurShieldRadius * 2f * (297f / 256f),
                1f,
                CurShieldRadius * 2f * (297f / 256f)));
        Graphics.DrawMesh(
            MeshPool.plane10,
            matrix4x4_1,
            Comp_ShieldEmitter.ForceFieldConeMat,
            0,
            null,
            0,
            Comp_ShieldEmitter.MatPropertyBlock);
    }

    private float GetCurrentAlpha()
    {
        float alphaIdle = GetCurrentAlpha_Idle();
        float alphaSelected = GetCurrentAlpha_Selected();
        float alphaIntercepted = GetCurrentAlpha_RecentlyIntercepted();
        float alphaActivated = GetCurrentAlpha_RecentlyActivated();

        float maxAlpha = Mathf.Max(alphaIdle, alphaSelected, alphaIntercepted, alphaActivated);
        return Mathf.Max(maxAlpha, Props.minAlpha);
    }

    private float GetCurrentAlpha_Idle()
    {
        if (!Active
            || parent.Faction == Faction.OfPlayer && !_debugInterceptNonHostileProjectiles
            || Find.Selector.IsSelected(parent)) return 0.0f;

        if (!_showShieldToggle)
        {
            int hash = Gen.HashCombineInt(parent.thingIDNumber, 96804938) % 100;
            float interpolation = (Mathf.Sin(hash + Time.realtimeSinceStartup * 0.7f) + 1) / 2f;
            return Mathf.Lerp(
                -1.7f,
                0.11f,
                interpolation);
        }

        float num = Mathf.Max(2f, Props.idlePulseSpeed);
        int hash2 = Gen.HashCombineInt(parent.thingIDNumber, 35990913) % 100;
        float interpolation2 = (Mathf.Sin(hash2 + Time.realtimeSinceStartup * num) + 1) / 2f;
        return Mathf.Lerp(
            0.2f,
            0.62f,
            interpolation2);
    }

    private float GetCurrentAlpha_Selected()
    {
        float num = Mathf.Max(2f, Props.idlePulseSpeed);
        if (!Find.Selector.IsSelected(parent))
            return 0.0f;

        if (!Active)
            return 0.41f;

        int hash = Gen.HashCombineInt(((Thing)parent).thingIDNumber, 35990913) % 100;
        float interpolation = (Mathf.Sin(hash + Time.realtimeSinceStartup * num) + 1) / 2f;
        return Mathf.Lerp(
            0.2f,
            0.62f,
            interpolation);
    }

    public float GetCurrentAlpha_RecentlyIntercepted()
    {
        float ticks = 1 - (Find.TickManager.TicksGame - _lastInterceptTicks) / 40f;
        return Mathf.Clamp01(ticks) * 0.09f;
    }

    public float GetCurrentAlpha_RecentlyActivated()
    {
        if (!Active) return 0.0f;

        float ticks = 1 - (Find.TickManager.TicksGame - (_lastInterceptTicks + Props.resetTime)) / 50f;
        return Mathf.Clamp01(ticks) * 0.09f;
    }

    public float GetCurrentConeAlpha_RecentlyIntercepted()
    {
        if (!Props.drawInterceptCone) return 0.0f;

        float ticks = 1 - (Find.TickManager.TicksGame - _lastInterceptTicks) / 40f;
        return Mathf.Clamp01(ticks) * 0.82f;
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        Comp_ShieldEmitter compShield = this;
        foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            yield return gizmo;

        if (compShield.parent.Faction == Faction.OfPlayer)
        {
            yield return new Gizmo_ShieldStatus()
            {
                shield = compShield
            };

            if (compShield.Props.shieldCanBeScaled)
            {
                Command_Action commandAction = new Command_Action();
                commandAction.defaultLabel = Translator.Translate("RG_ShieldGenRadiusLabel");
                commandAction.defaultDesc = Translator.Translate("RG_ShieldGenRadiusDescription");
                commandAction.icon = ContentFinder<Texture2D>.Get("UI/Icon/Button/RGShieldRadius", true);
                commandAction.action = () =>
                {
                    Find.WindowStack.Add(new Dialog_Slider(
                        Translator.Translate("RG_ShieldGenRadiusTitle"),
                        Props.shieldScaleLimits.min,
                        Props.shieldScaleLimits.max,
                        val => SetShieldRadius = val,
                        SetShieldRadius));
                };
                yield return commandAction;
            }

            Command_Toggle commandToggle = new Command_Toggle();
            commandToggle.defaultLabel = Translator.Translate("RG_ShieldGenToggleVisibility");
            commandToggle.defaultDesc = Translator.Translate("RG_ShieldGenToggleVisibilityDesc");
            commandToggle.isActive = () => _showShieldToggle;
            commandToggle.icon = ContentFinder<Texture2D>.Get("UI/Icon/Button/RGShieldVisibility", true);
            commandToggle.toggleAction = () => _showShieldToggle = !_showShieldToggle;
            yield return commandToggle;
        }

        if (Prefs.DevMode)
        {
            if (compShield.TicksToReset > 0)
            {
                Command_Action commandAction = new Command_Action();
                commandAction.defaultLabel = "Reset shield cooldown";
                // ISSUE: reference to a compiler-generated method
                commandAction.action = () => TicksToReset = 0;
                yield return commandAction;
            }

            Command_Toggle commandToggle = new Command_Toggle();
            commandToggle.defaultLabel = "Intercept non-hostile";
            // ISSUE: reference to a compiler-generated method
            commandToggle.isActive = () => _debugInterceptNonHostileProjectiles;
            // ISSUE: reference to a compiler-generated method
            commandToggle.toggleAction = () => _debugInterceptNonHostileProjectiles = !_debugInterceptNonHostileProjectiles;
            yield return commandToggle;
        }
    }

    public override string CompInspectStringExtra()
    {
        StringBuilder stringBuilder = new StringBuilder();
        if (Active)
        {
            if (TicksToReset > 0)
            {
                string cooldown = GenDate.ToStringTicksToPeriod(TicksToReset, true, false, true, true, false);
                stringBuilder.Append($"CooldownTime : {cooldown}");
            }
            else
                stringBuilder.Append("Shield Active");
        }
        else
            stringBuilder.Append("Shield Inactive");

        return stringBuilder.ToString();
    }

    public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
    {
        base.PostPostApplyDamage(dinfo, totalDamageDealt);
        if (dinfo.Def == DamageDefOf.EMP)
            _lastHitByEmpTicks = Find.TickManager.TicksGame;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look<int>(ref _lastInterceptTicks, "_lastInterceptTicks", -999999, false);
        Scribe_Values.Look<int>(ref _lastHitByEmpTicks, "_lastHitByEmpTicks", -999999, false);
        Scribe_Values.Look<bool>(ref _showShieldToggle, "_showShieldToggle", false, false);
        Scribe_Values.Look<float>(ref CurStressLevel, "CurStressLevel", 0.0f, false);
        Scribe_Values.Look<float>(ref MaxStressLevel, "MaxStressLevel", 1f, false);
        Scribe_Values.Look<int>(ref TicksToReset, "TicksToReset", -1, false);
        Scribe_Values.Look<bool>(ref Overloaded, "Overloaded", false, false);
        Scribe_Values.Look<bool>(ref ActiveLastTick, "ActiveLastTick", false, false);
        Scribe_Values.Look<int>(ref CurShieldRadius, "CurShieldRadius", Props.shieldScaleDefault, false);
        Scribe_Values.Look<Color>(ref CurrentColor, "CurrentColor", Props.shieldColour, false);
    }
}
