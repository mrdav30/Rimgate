using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Rimgate;

public class Comp_ShieldEmitter : ThingComp
{
    public const float MaxStressLevel = 1f;

    private float _curStressLevel;

    private int _ticksToReset;

    private bool _overloaded;

    private int _curShieldRadius = -1;

    private float _lastTempChange;

    private Color _currentColor;

    private MaterialPropertyBlock _matPropertyBlock = new();

    private int _lastInterceptTicks = -999999;

    private bool _wasHitByEmp;

    private float _lastInterceptAngle;

    private bool _debugInterceptNonHostileProjectiles = true;

    private bool _showShieldToggle;

    private CompPowerTrader _powerComp;

    private Comp_Toggle _toggleComp;

    private CompHeatPusher _heatComp;

    private CompRefuelable _refuelableComp;

    public CompProperties_ShieldEmitter Props => (CompProperties_ShieldEmitter)props;

    public float CurStressLevel => _curStressLevel;

    public bool Powered => (PowerTrader == null || PowerTrader.PowerOn)
        && (Refuelable == null || Refuelable.HasFuel);

    public virtual bool Active
    {
        get
        {
            if (_overloaded || !Powered)
                return false;
            return Toggle == null || Toggle.SwitchIsOn;
        }
    }

    public Vector3 CurShieldPosition => parent.TrueCenter();

    public int SetShieldRadius
    {
        get => _curShieldRadius;
        set
        {
            _curShieldRadius = Mathf.Clamp(
                value,
                Props.shieldScaleLimits.min,
                Props.shieldScaleLimits.max);
        }
    }

    public int TicksFromLastIntercept
    {
        get
        {
            var ticks = Find.TickManager.TicksGame - _lastInterceptTicks;
            return ticks > 0 ? ticks : 0;
        }
    }

    public bool ReactivatedThisTick => TicksFromLastIntercept == Props.resetTime;

    public float ScaleDamageFactor => !Props.shieldCanBeScaled
        ? 1f
        : Mathf.Lerp(0.5f, 2f, GetShieldScalePercentage);

    public float GetShieldScalePercentage => !Props.shieldCanBeScaled
        ? 1f
        : Mathf.InverseLerp(
            Props.shieldScaleLimits.min,
            Props.shieldScaleLimits.max,
            _curShieldRadius);

    public CompPowerTrader PowerTrader => _powerComp ??= parent.GetComp<CompPowerTrader>();

    public Comp_Toggle Toggle => _toggleComp ??= parent.GetComp<Comp_Toggle>();

    public CompHeatPusher HeatPusher => _heatComp ??= parent.GetComp<CompHeatPusher>();

    public CompRefuelable Refuelable => _refuelableComp ??= parent.GetComp<CompRefuelable>();

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        if (_curShieldRadius < Props.shieldScaleLimits.min)
            SetShieldRadius = Props.shieldScaleDefault;

        if (!respawningAfterLoad)
        {
            _currentColor = Props.shieldColor;
            SetShieldRadius = Props.shieldScaleDefault;
        }

        parent.Map?.GetComponent<MapComp_ShieldList>()?.ShieldGenList.Add(parent);
    }

    #region Tick

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

            if (!_overloaded
                && CurStressLevel >= Props.shieldOverloadThreshold
                && Rand.Chance(Props.shieldOverloadChance * (1 + CurStressLevel)))
            {
                CellRect cellRect1 = GenAdj.OccupiedRect(parent);
                CellRect cellRect2 = cellRect1.ExpandedBy(Props.extraOverloadRange);
                GenExplosion.DoExplosion(
                    cellRect2.RandomCell,
                    parent.Map,
                    1.9f,
                    DamageDefOf.EMP,
                    null);
            }
        }
        else // reduce stress on overload
            UpdateStress(true);

        if (PowerTrader != null)
            UpdatePowerUsage();

        if (Refuelable != null)
            UpdateFuelUsage();

        if (_overloaded && Powered)
        {
            --_ticksToReset;
            if (_ticksToReset <= 0)
                _overloaded = false;
        }

        if (HeatPusher != null)
            UpdateHeatPusher();
    }

    public void UpdateStress(bool tickUpdate = false, bool cooling = false)
    {
        if (tickUpdate)
        {
            float num = 0f - Props.stressReduction;
            if (!Active)
                num = -Props.stressReduction;

            _lastTempChange = (float)(num * 0.0099999997764825821f / 60f);
            _curStressLevel = Mathf.Clamp(
                _curStressLevel + _lastTempChange,
                0.0f,
                MaxStressLevel);
        }

        if (!_overloaded && (CurStressLevel >= MaxStressLevel || _wasHitByEmp))
            OverloadShield();
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

        _ticksToReset = Props.resetTime;
        _overloaded = true;
        _wasHitByEmp = false;
        _curStressLevel = 0.0f;
        if (!Props.explodeOnCollapse
            || ThingCompUtility.TryGetComp<CompExplosive>(parent) == null) return;

        ThingCompUtility.TryGetComp<CompExplosive>(parent).StartWick(null);
    }

    private void UpdatePowerUsage()
    {
        if (!Props.shieldCanBeScaled || !Props.sizeScalesPowerUsage)
            return;

        if (!Active)
        {
            PowerTrader.PowerOutput = 0;
            return;
        }

        PowerTrader.PowerOutput = Mathf.Lerp(
            0 - Props.powerUsageRange.min,
            0 - Props.powerUsageRange.max,
            GetShieldScalePercentage);
    }

    private void UpdateFuelUsage()
    {
        if (!Props.shieldCanBeScaled
            || !Props.sizeScalesFuelUsage
            || !Active
            || !parent.IsHashIntervalTick(2))
            return;

        float fuelRate = Mathf.Lerp(
            Props.fuelConsumptionRange.min,
            Props.fuelConsumptionRange.max,
            GetShieldScalePercentage);

        Refuelable.ConsumeFuel(fuelRate / 60000f);
    }

    public void UpdateHeatPusher()
    {
        if (!Active)
        {
            HeatPusher.Props.heatPerSecond = 0.0f;
            return;
        }

        HeatPusher.Props.heatPerSecond = Mathf.Lerp(
            Props.heatGenRange.min,
            Props.heatGenRange.max,
            CurStressLevel);
    }

    #endregion

    #region Air Projectiles

    public bool CheckIntercept(Skyfaller skyfaller)
    {
        if (!HoldsAnyHostiles(skyfaller) || !ShouldBeBlocked(skyfaller))
            return false;

        _lastInterceptAngle = Vector3Utility.AngleToFlat(skyfaller.Position.ToVector3(), CurShieldPosition);
        _lastInterceptTicks = Find.TickManager.TicksGame;

        TriggerFlecks(skyfaller);
        UpdateStressIntercept(skyfaller);

        return true;
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
                    IntVec3Utility.ToIntVec3(CurShieldPosition)) <= _curShieldRadius;
    }

    public void TriggerFlecks(Skyfaller skyFaller)
    {
        SoundDefOf.EnergyShield_AbsorbDamage.PlayOneShot((SoundInfo)new TargetInfo(skyFaller.Position, skyFaller.Map));
        foreach (IntVec3 intVec3 in skyFaller.OccupiedRect().ToList<IntVec3>())
        {
            FleckMaker.ThrowHeatGlow(intVec3, skyFaller.Map, 1f);
            FleckMaker.ThrowLightningGlow(intVec3.ToVector3Shifted(), skyFaller.Map, 1f);
            FleckMaker.Static(intVec3, skyFaller.Map, DefDatabase<FleckDef>.GetNamed("ElectricalSpark"), 2f);
            FleckMaker.Static(intVec3, skyFaller.Map, DefDatabase<FleckDef>.GetNamed("PsycastPsychicEffect"), 2f);
        }
    }

    public void UpdateStressIntercept(Skyfaller skyfaller)
    {
        float num = (float)(30000 * Props.stressPerDamage / 100f);
        if (skyfaller is DropPodIncoming)
            num /= 3f;
        _curStressLevel = Mathf.Clamp(
            CurStressLevel + num * ScaleDamageFactor,
            0.0f,
            MaxStressLevel);
        UpdateStress();
    }

    #endregion

    #region Ground Projectiles

    public bool CheckIntercept(
        Projectile projectile,
        Vector3 lastExactPos,
        Vector3 newExactPos)
    {
        Vector3 shieldPos = CurShieldPosition;
        float effectiveRadius = _curShieldRadius + projectile.def.projectile.SpeedTilesPerTick + 0.1f;

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

        bool shouldNotIntercept = outsideEffectiveRadius
            || notActive
            || notInterceptable
            || nonHostileShouldSkip;
        if (shouldNotIntercept)
            return false;

        if (!Props.interceptOutgoingProjectiles)
        {
            Vector2 displacement = new Vector2(shieldPos.x, shieldPos.z) - new Vector2(lastExactPos.x, lastExactPos.z);
            if (displacement.sqrMagnitude <= _curShieldRadius * _curShieldRadius)
                return false;
        }

        bool intersectsShield = GenGeo.IntersectLineCircleOutline(
            new Vector2(shieldPos.x, shieldPos.z),
            _curShieldRadius,
            new Vector2(lastExactPos.x, lastExactPos.z),
            new Vector2(newExactPos.x, newExactPos.z));
        if (!intersectsShield)
            return false;

        _lastInterceptAngle = Vector3Utility.AngleToFlat(lastExactPos, CurShieldPosition);
        _lastInterceptTicks = Find.TickManager.TicksGame;

        TriggerEffecter(IntVec3Utility.ToIntVec3(newExactPos));
        UpdateStressIntercept(projectile);

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

    public void UpdateStressIntercept(Projectile projectile)
    {
        float num = (float)(projectile.DamageAmount * Props.stressPerDamage / 100f);
        if (Props.disabledByEmp
            && projectile.def.projectile.damageDef == DamageDefOf.EMP)
        {
            _wasHitByEmp = true;
            num *= Props.empDamageFactor;
        }

        _curStressLevel = Mathf.Clamp(
            CurStressLevel + num * ScaleDamageFactor,
            0.0f,
            MaxStressLevel);
        UpdateStress();
    }

    #endregion

    #region FX

    public override void PostDraw()
    {
        Vector3 curShieldPosition = CurShieldPosition;
        curShieldPosition.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
        float currentAlpha = GetCurrentAlpha();
        if (currentAlpha > 0.0)
        {
            Color color = Active || !Find.Selector.IsSelected((object)parent)
                ? _currentColor
                : Props.inactiveColor;
            color.a *= currentAlpha;
            _matPropertyBlock.SetColor(ShaderPropertyIDs.Color, color);
            Matrix4x4 matrix4x4 = new Matrix4x4();
            matrix4x4.SetTRS(
                curShieldPosition,
                Quaternion.identity,
                new Vector3(
                    _curShieldRadius * 2f * (297f / 256f),
                    1f,
                    _curShieldRadius * 2f * (297f / 256f)));
            Graphics.DrawMesh(
                MeshPool.plane10,
                matrix4x4,
                RimgateTex.ForceFieldMat,
                0,
                null,
                0,
                _matPropertyBlock);
        }
        float recentlyIntercepted = GetCurrentConeAlpha_RecentlyIntercepted();
        if (recentlyIntercepted <= 0)
            return;

        Color currentColor = _currentColor;
        currentColor.a *= recentlyIntercepted;
        _matPropertyBlock.SetColor(ShaderPropertyIDs.Color, currentColor);
        Matrix4x4 matrix4x4_1 = new Matrix4x4();
        matrix4x4_1.SetTRS(
            curShieldPosition,
            Quaternion.Euler(0.0f, _lastInterceptAngle - 90f, 0.0f),
            new Vector3(
                _curShieldRadius * 2f * (297f / 256f),
                1f,
                _curShieldRadius * 2f * (297f / 256f)));
        Graphics.DrawMesh(
            MeshPool.plane10,
            matrix4x4_1,
            RimgateTex.ForceFieldConeMat,
            0,
            null,
            0,
            _matPropertyBlock);
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
            || (parent.Faction.IsOfPlayerFaction() && !_debugInterceptNonHostileProjectiles)
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
        float ticks = 1 - TicksFromLastIntercept / 40f;
        return Mathf.Clamp01(ticks) * 0.09f;
    }

    public float GetCurrentAlpha_RecentlyActivated()
    {
        if (!Active) return 0.0f;

        float ticks = 1 - (TicksFromLastIntercept - Props.resetTime) / 50f;
        return Mathf.Clamp01(ticks) * 0.09f;
    }

    public float GetCurrentConeAlpha_RecentlyIntercepted()
    {
        if (!Props.drawInterceptCone) return 0.0f;

        float ticks = 1 - TicksFromLastIntercept / 40f;
        return Mathf.Clamp01(ticks) * 0.82f;
    }

    #endregion

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (parent.Faction.IsOfPlayerFaction())
        {
            yield return new Gizmo_ShieldStatus(this);

            if (Props.shieldCanBeScaled)
            {
                Command_Action commandAction = new Command_Action();
                commandAction.defaultLabel = Translator.Translate("RG_ShieldGenRadiusLabel");
                commandAction.defaultDesc = Translator.Translate("RG_ShieldGenRadiusDescription");
                commandAction.icon = RimgateTex.ShieldRadiusCommandTex;
                commandAction.action = () =>
                {
                    Find.WindowStack.Add(new Dialog_SliderWithValue(
                        Translator.Translate("RG_ShieldGenRadiusTitle"),
                        Props.shieldScaleLimits.min,
                        Props.shieldScaleLimits.max,
                        val => SetShieldRadius = val,
                        SetShieldRadius,
                        unitLabel: "tiles"));
                };
                yield return commandAction;
            }

            Command_Toggle commandToggle = new Command_Toggle();
            commandToggle.defaultLabel = Translator.Translate("RG_ShieldGenToggleVisibility");
            commandToggle.defaultDesc = Translator.Translate("RG_ShieldGenToggleVisibilityDesc");
            commandToggle.isActive = () => _showShieldToggle;
            commandToggle.icon = RimgateTex.ShieldVisibilityCommandTex;
            commandToggle.toggleAction = () => _showShieldToggle = !_showShieldToggle;
            yield return commandToggle;
        }

        if (Prefs.DevMode)
        {
            if (_ticksToReset > 0)
            {
                Command_Action commandAction = new Command_Action();
                commandAction.defaultLabel = "Reset shield cooldown";
                // ISSUE: reference to a compiler-generated method
                commandAction.action = () => _ticksToReset = 0;
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
            stringBuilder.Append("Shield Active");
        else
            stringBuilder.Append("Shield Inactive");

        if (_ticksToReset > 0)
            stringBuilder.AppendInNewLine($"CooldownTime : {GenDate.ToStringTicksToPeriod(_ticksToReset)}");

        return stringBuilder.ToString();
    }

    public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
    {
        if (Props.disabledByEmp && dinfo.Def == DamageDefOf.EMP)
            _wasHitByEmp = true;
    }

    public override void PostExposeData()
    {
        Scribe_Values.Look<int>(ref _lastInterceptTicks, "_lastInterceptTicks", -999999, false);
        Scribe_Values.Look<bool>(ref _wasHitByEmp, "_wasHitByEmp", false);
        Scribe_Values.Look<bool>(ref _showShieldToggle, "_showShieldToggle", false, false);
        Scribe_Values.Look<float>(ref _curStressLevel, "_curStressLevel", 0.0f, false);
        Scribe_Values.Look<int>(ref _ticksToReset, "_ticksToReset", -1, false);
        Scribe_Values.Look<bool>(ref _overloaded, "_overloaded", false, false);
        Scribe_Values.Look<int>(ref _curShieldRadius, "_curShieldRadius", Props.shieldScaleDefault, false);
        Scribe_Values.Look<Color>(ref _currentColor, "_currentColor", Props.shieldColor, false);
    }
}
