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
    private int lastInterceptTicks = -999999;

    private int lastHitByEmpTicks = -999999;

    private float lastInterceptAngle;

    private bool debugInterceptNonHostileProjectiles = true;

    private static readonly Material ForceFieldMat = MaterialPool.MatFrom("Other/ForceField", ShaderDatabase.MoteGlow);

    private static readonly Material ForceFieldConeMat = MaterialPool.MatFrom("Other/ForceFieldCone", ShaderDatabase.MoteGlow);

    private static readonly MaterialPropertyBlock MatPropertyBlock = new MaterialPropertyBlock();

    private static readonly Color InactiveColor = new Color(0.2f, 0.2f, 0.2f);

    private bool showShieldToggle;

    public float CurStressLevel;

    public float MaxStressLevel = 1f;

    public int ticksToReset;

    public bool overloaded;

    public bool activeLastTick;

    public int curShieldRadius = -1;

    private CompPowerTrader cachedPowerComp;

    private Comp_Toggle cachedFlickableComp;

    private CompHeatPusher cachedHeatComp;

    private CompRefuelable cachedFuelComp;

    public float lastTempChange;

    public Color currentColor;

    public CompProperties_ShieldEmitter Props => (CompProperties_ShieldEmitter)this.props;

    public virtual bool Active
    {
        get
        {
            if (this.overloaded
                || this.PowerTrader != null && !this.PowerTrader.PowerOn
                || this.FuelComp != null && !this.FuelComp.HasFuel) return false;
            return this.Flicker == null || this.Flicker.SwitchIsOn;
        }
    }

    public Vector3 CurShieldPosition => this.parent.Position.ToVector3Shifted();

    public int SetShieldRadius
    {
        get => this.curShieldRadius;
        set
        {
            this.curShieldRadius = Mathf.Clamp(
                value,
                this.Props.shieldScaleLimits.min,
                this.Props.shieldScaleLimits.max);
        }
    }

    public bool ReactivatedThisTick
    {
        get => Find.TickManager.TicksGame - this.lastInterceptTicks == this.Props.resetTime;
    }

    public float ScaleDamageFactor => Mathf.Lerp(0.5f, 2f, this.GetShieldScalePercentage);

    public float GetShieldScalePercentage
    {
        get
        {
            return !this.Props.shieldCanBeScaled
                ? 1f
                : Mathf.InverseLerp(this.Props.shieldScaleLimits.min, this.Props.shieldScaleLimits.max, this.curShieldRadius);
        }
    }

    public CompPowerTrader PowerTrader
    {
        get
        {
            this.cachedPowerComp ??= this.parent.GetComp<CompPowerTrader>();
            return this.cachedPowerComp;
        }
    }

    public Comp_Toggle Flicker
    {
        get
        {
            this.cachedFlickableComp ??= this.parent.GetComp<Comp_Toggle>();
            return this.cachedFlickableComp;
        }
    }

    public CompHeatPusher HeatComp
    {
        get
        {
            this.cachedHeatComp ??= this.parent.GetComp<CompHeatPusher>();
            return this.cachedHeatComp;
        }
    }

    public CompRefuelable FuelComp
    {
        get
        {
            this.cachedFuelComp ??= this.parent.GetComp<CompRefuelable>();
            return this.cachedFuelComp;
        }
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        if (!ModLister.CheckRoyalty("Projectile interception"))
        {
            Log.Message("Rimgate :: Shield Setup skipped because user lacks Royalty.");
            this.parent.Destroy(DestroyMode.Vanish);
            return;
        }

        base.PostSpawnSetup(respawningAfterLoad);
        if (this.curShieldRadius < this.Props.shieldScaleLimits.min)
            this.SetShieldRadius = this.Props.shieldScaleDefault;

        if (!respawningAfterLoad)
        {
            this.currentColor = this.Props.shieldColour;
            this.SetShieldRadius = this.Props.shieldScaleDefault;
        }
    }

    public bool CheckIntercept(Skyfaller skyfaller)
    {
        return this.HoldsAnyHostiles(skyfaller)
            && this.ShouldBeBlocked(skyfaller);
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
                case DropPodIncoming pod when this.HoldsAnyHostiles(pod):
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
        return this.Props.skyfallerClassWhitelist.Any() == false
            || !this.Props.skyfallerClassWhitelist.Contains(skyfaller.def.thingClass)
                && this.Active
                && this.Props.podBlocker
                && IntVec3Utility.DistanceTo(
                    skyfaller.Position,
                    IntVec3Utility.ToIntVec3(this.CurShieldPosition)) <= this.curShieldRadius;
    }

    public bool BombardmentCanStartFireAt(Bombardment bombardment, IntVec3 cell)
    {
        return !this.Active
            || !this.Props.interceptAirProjectiles
            || bombardment.instigator == null
            || !GenHostility.HostileTo(bombardment.instigator, this.parent)
                && !this.debugInterceptNonHostileProjectiles
                && !this.Props.interceptNonHostileProjectiles
            || !cell.InHorDistOf(this.parent.Position, this.curShieldRadius);
    }

    public bool CheckIntercept(Projectile projectile, Vector3 lastExactPos, Vector3 newExactPos)
    {
        if (!ModLister.CheckRoyalty("Projectile interception"))
            return false;

        Vector3 shieldPos = this.CurShieldPosition;
        float effectiveRadius = this.curShieldRadius + projectile.def.projectile.SpeedTilesPerTick + 0.1f;

        float dx = newExactPos.x - shieldPos.x;
        float dz = newExactPos.z - shieldPos.z;
        float sqrDistance = dx * dx + dz * dz;

        bool outsideEffectiveRadius = sqrDistance > effectiveRadius * effectiveRadius;
        bool notActive = !this.Active;
        bool notInterceptable = !Comp_ShieldEmitter.InterceptsProjectile(this.Props, projectile);

        bool isHostile = projectile.Launcher != null
            && GenHostility.HostileTo(projectile.Launcher, this.parent);
        bool shouldIgnoreNonHostile = !this.debugInterceptNonHostileProjectiles
            && this.Props.interceptNonHostileProjectiles;
        bool nonHostileShouldSkip = !isHostile && shouldIgnoreNonHostile;

        if (outsideEffectiveRadius
            || notActive
            || notInterceptable
            || nonHostileShouldSkip) return false;

        if (!this.Props.interceptOutgoingProjectiles)
        {
            Vector2 displacement = new Vector2(shieldPos.x, shieldPos.z) - new Vector2(lastExactPos.x, lastExactPos.z);
            if (displacement.sqrMagnitude <= this.curShieldRadius * this.curShieldRadius)
                return false;
        }

        bool intersectsShield = GenGeo.IntersectLineCircleOutline(
            new Vector2(shieldPos.x, shieldPos.z),
            this.curShieldRadius,
            new Vector2(lastExactPos.x, lastExactPos.z),
            new Vector2(newExactPos.x, newExactPos.z));

        if (!intersectsShield)
            return false;

        this.lastInterceptAngle = Vector3Utility.AngleToFlat(lastExactPos, GenThing.TrueCenter(this.parent));
        this.lastInterceptTicks = Find.TickManager.TicksGame;

        if (projectile.def.projectile.damageDef == DamageDefOf.EMP)
            this.lastHitByEmpTicks = Find.TickManager.TicksGame;

        this.TriggerEffecter(IntVec3Utility.ToIntVec3(newExactPos));
        this.UpdateStress(projectile);

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
        effecter.Trigger(new TargetInfo(pos, this.parent.Map, false), TargetInfo.Invalid, -1);
        effecter.Cleanup();
    }

    public void UpdateStress(bool tickUpdate = false, bool cooling = false)
    {
        if (tickUpdate)
        {
            float num1 = 0.0f;
            float num2 = num1 - this.Props.stressReduction;
            if (!this.Active)
                num2 = -this.Props.stressReduction;

            this.lastTempChange = (float)(num2 * 0.0099999997764825821 / 60.0);
            this.CurStressLevel = Mathf.Clamp(
                this.CurStressLevel + this.lastTempChange,
                0.0f,
                this.MaxStressLevel);
        }

        if (this.CurStressLevel >= this.MaxStressLevel)
            this.OverloadShield();
    }

    public void UpdateStress(Projectile projectile)
    {
        float num = (float)(projectile.DamageAmount * this.Props.stressPerDamage / 100f);
        if (projectile.def.projectile.damageDef == DamageDefOf.EMP)
            num *= this.Props.empDamageFactor;

        this.CurStressLevel = Mathf.Clamp(
            this.CurStressLevel + num * this.ScaleDamageFactor,
            0.0f,
            this.MaxStressLevel);
        this.UpdateStress();
    }

    public void UpdateStress(Skyfaller skyfaller)
    {
        float num = (float)(30000 * this.Props.stressPerDamage / 100f);
        if (skyfaller is DropPodIncoming)
            num /= 3f;
        this.CurStressLevel = Mathf.Clamp(
            this.CurStressLevel + num * this.ScaleDamageFactor,
            0.0f,
            this.MaxStressLevel);
        this.UpdateStress();
    }

    public void OverloadShield()
    {
        if (this.Props.breakSound != null)
            SoundStarter.PlayOneShot(
                this.Props.breakSound,
                new TargetInfo(this.parent.Position, this.parent.Map, false));

        FleckMaker.ThrowExplosionInterior(
            GenThing.TrueCenter(this.parent),
            this.parent.Map,
            FleckDefOf.ExplosionFlash);
        for (int index = 0; index < 6; ++index)
        {
            Vector3 location = GenThing.TrueCenter(this.parent)
                + Vector3Utility.HorizontalVectorFromAngle(Rand.Range(0, 360)) * Rand.Range(0.3f, 0.6f);
            FleckMaker.ThrowDustPuff(
                location,
                this.parent.Map,
                Rand.Range(0.8f, 1.2f));
        }

        this.ticksToReset = this.Props.resetTime;
        this.overloaded = true;
        this.CurStressLevel = 0.0f;
        if (!this.Props.explodeOnCollapse
            || ThingCompUtility.TryGetComp<CompExplosive>(this.parent) == null) return;

        ThingCompUtility.TryGetComp<CompExplosive>(this.parent).StartWick(null);
    }

    public void UpdatePowerUsage()
    {
        if (!this.Active)
        {
            this.PowerTrader.PowerOutput = 0.0f;
            return;
        }

        this.PowerTrader.PowerOutput = Mathf.Lerp(
            this.Props.powerUsageRange.min,
            this.Props.powerUsageRange.max,
            this.GetShieldScalePercentage);
    }

    public void UpdateFuelUsage()
    {
        if (!this.Active)
            return;

        this.FuelComp.ConsumeFuel(this.Props.fuelConsumptionRate);
    }

    public override void CompTick()
    {
        if (this.Active)
        {
            if (this.ReactivatedThisTick && this.Props.reactivateEffect != null)
            {
                Effecter effecter = new Effecter(this.Props.reactivateEffect);
                effecter.Trigger(this.parent, TargetInfo.Invalid, -1);
                effecter.Cleanup();
            }

            this.UpdateStress(true);
            if (this.CurStressLevel >= this.Props.shieldOverloadThreshold
                && Rand.Chance(this.Props.shieldOverloadChance * (1f - (1f - this.CurStressLevel) * 10f)))
            {
                CellRect cellRect1 = GenAdj.OccupiedRect(this.parent);
                CellRect cellRect2 = cellRect1.ExpandedBy(this.Props.extraOverloadRange);
                GenExplosion.DoExplosion(
                    cellRect2.RandomCell,
                    this.parent.Map,
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

        this.UpdateStress(true);
        if (this.PowerTrader != null && this.FuelComp == null)
        {
            this.UpdatePowerUsage();
            if (this.overloaded && this.PowerTrader.PowerOn)
            {
                --this.ticksToReset;
                if (this.ticksToReset <= 0)
                    this.overloaded = false;
            }
        }

        if (this.FuelComp != null && this.PowerTrader == null)
        {
            this.UpdateFuelUsage();
            if (this.overloaded && this.FuelComp.HasFuel)
            {
                --this.ticksToReset;
                if (this.ticksToReset <= 0)
                    this.overloaded = false;
            }
        }

        if (this.HeatComp != null)
            this.UpdateHeatPusher();
    }

    public void UpdateHeatPusher()
    {
        if (this.Active)
            this.HeatComp.Props.heatPerSecond = Mathf.Lerp(
                this.Props.heatGenRange.min,
                this.Props.heatGenRange.max,
                this.CurStressLevel);
        else
            this.HeatComp.Props.heatPerSecond = 0.0f;
    }

    public override void PostDraw()
    {
        base.PostDraw();
        Vector3 curShieldPosition = this.CurShieldPosition;
        curShieldPosition.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
        float currentAlpha = this.GetCurrentAlpha();
        if (currentAlpha > 0.0)
        {
            Color color = this.Active || !Find.Selector.IsSelected((object)this.parent)
                ? this.currentColor
                : Comp_ShieldEmitter.InactiveColor;
            color.a *= currentAlpha;
            Comp_ShieldEmitter.MatPropertyBlock.SetColor(ShaderPropertyIDs.Color, color);
            Matrix4x4 matrix4x4 = new Matrix4x4();
            matrix4x4.SetTRS(
                curShieldPosition,
                Quaternion.identity,
                new Vector3(
                    this.curShieldRadius * 2f * (297f / 256f),
                    1f,
                    this.curShieldRadius * 2f * (297f / 256f)));
            Graphics.DrawMesh(
                MeshPool.plane10,
                matrix4x4,
                Comp_ShieldEmitter.ForceFieldMat,
                0,
                null,
                0,
                Comp_ShieldEmitter.MatPropertyBlock);
        }
        float recentlyIntercepted = this.GetCurrentConeAlpha_RecentlyIntercepted();
        if (recentlyIntercepted <= 0)
            return;

        Color currentColor = this.currentColor;
        currentColor.a *= recentlyIntercepted;
        Comp_ShieldEmitter.MatPropertyBlock.SetColor(ShaderPropertyIDs.Color, currentColor);
        Matrix4x4 matrix4x4_1 = new Matrix4x4();
        matrix4x4_1.SetTRS(
            curShieldPosition,
            Quaternion.Euler(0.0f, this.lastInterceptAngle - 90f, 0.0f),
            new Vector3(
                this.curShieldRadius * 2f * (297f / 256f),
                1f,
                this.curShieldRadius * 2f * (297f / 256f)));
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
        float alphaIdle = this.GetCurrentAlpha_Idle();
        float alphaSelected = this.GetCurrentAlpha_Selected();
        float alphaIntercepted = this.GetCurrentAlpha_RecentlyIntercepted();
        float alphaActivated = this.GetCurrentAlpha_RecentlyActivated();

        float maxAlpha = Mathf.Max(alphaIdle, alphaSelected, alphaIntercepted, alphaActivated);
        return Mathf.Max(maxAlpha, this.Props.minAlpha);
    }

    private float GetCurrentAlpha_Idle()
    {
        if (!this.Active
            || this.parent.Faction == Faction.OfPlayer && !this.debugInterceptNonHostileProjectiles
            || Find.Selector.IsSelected(this.parent)) return 0.0f;

        if (!this.showShieldToggle)
        {
            int hash = Gen.HashCombineInt(this.parent.thingIDNumber, 96804938) % 100;
            float interpolation = (Mathf.Sin(hash + Time.realtimeSinceStartup * 0.7f) + 1) / 2f;
            return Mathf.Lerp(
                -1.7f,
                0.11f,
                interpolation);
        }

        float num = Mathf.Max(2f, this.Props.idlePulseSpeed);
        int hash2 = Gen.HashCombineInt(this.parent.thingIDNumber, 35990913) % 100;
        float interpolation2 = (Mathf.Sin(hash2 + Time.realtimeSinceStartup * num) + 1) / 2f;
        return Mathf.Lerp(
            0.2f,
            0.62f,
            interpolation2);
    }

    private float GetCurrentAlpha_Selected()
    {
        float num = Mathf.Max(2f, this.Props.idlePulseSpeed);
        if (!Find.Selector.IsSelected(this.parent))
            return 0.0f;

        if (!this.Active)
            return 0.41f;

        int hash = Gen.HashCombineInt(((Thing)this.parent).thingIDNumber, 35990913) % 100;
        float interpolation = (Mathf.Sin(hash + Time.realtimeSinceStartup * num) + 1) / 2f;
        return Mathf.Lerp(
            0.2f,
            0.62f,
            interpolation);
    }

    public float GetCurrentAlpha_RecentlyIntercepted()
    {
        float ticks = 1 - (Find.TickManager.TicksGame - this.lastInterceptTicks) / 40f;
        return Mathf.Clamp01(ticks) * 0.09f;
    }

    public float GetCurrentAlpha_RecentlyActivated()
    {
        if (!this.Active) return 0.0f;

        float ticks = 1 - (Find.TickManager.TicksGame - (this.lastInterceptTicks + this.Props.resetTime)) / 50f;
        return Mathf.Clamp01(ticks) * 0.09f;
    }

    public float GetCurrentConeAlpha_RecentlyIntercepted()
    {
        if (!this.Props.drawInterceptCone) return 0.0f;

        float ticks = 1 - (Find.TickManager.TicksGame - this.lastInterceptTicks) / 40f;
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
                        this.Props.shieldScaleLimits.min,
                        this.Props.shieldScaleLimits.max,
                        val => this.SetShieldRadius = val,
                        this.SetShieldRadius));
                };
                yield return commandAction;
            }

            Command_Toggle commandToggle = new Command_Toggle();
            commandToggle.defaultLabel = Translator.Translate("RG_ShieldGenToggleVisibility");
            commandToggle.defaultDesc = Translator.Translate("RG_ShieldGenToggleVisibilityDesc");
            commandToggle.isActive = () => this.showShieldToggle;
            commandToggle.icon = ContentFinder<Texture2D>.Get("UI/Icon/Button/RGShieldVisibility", true);
            commandToggle.toggleAction = () => this.showShieldToggle = !this.showShieldToggle;
            yield return commandToggle;
        }

        if (Prefs.DevMode)
        {
            if (compShield.ticksToReset > 0)
            {
                Command_Action commandAction = new Command_Action();
                commandAction.defaultLabel = "Reset shield cooldown";
                // ISSUE: reference to a compiler-generated method
                commandAction.action = () => this.ticksToReset = 0;
                yield return commandAction;
            }

            Command_Toggle commandToggle = new Command_Toggle();
            commandToggle.defaultLabel = "Intercept non-hostile";
            // ISSUE: reference to a compiler-generated method
            commandToggle.isActive = () => this.debugInterceptNonHostileProjectiles;
            // ISSUE: reference to a compiler-generated method
            commandToggle.toggleAction = () => this.debugInterceptNonHostileProjectiles = !this.debugInterceptNonHostileProjectiles;
            yield return commandToggle;
        }
    }

    public override string CompInspectStringExtra()
    {
        StringBuilder stringBuilder = new StringBuilder();
        if (this.Active)
        {
            if (this.ticksToReset > 0)
            {
                string cooldown = GenDate.ToStringTicksToPeriod(this.ticksToReset, true, false, true, true, false);
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
            this.lastHitByEmpTicks = Find.TickManager.TicksGame;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look<int>(ref this.lastInterceptTicks, "lastInterceptTicks", -999999, false);
        Scribe_Values.Look<int>(ref this.lastHitByEmpTicks, "lastHitByEmpTicks", -999999, false);
        Scribe_Values.Look<bool>(ref this.showShieldToggle, "showShieldToggle", false, false);
        Scribe_Values.Look<float>(ref this.CurStressLevel, "curStressLevel", 0.0f, false);
        Scribe_Values.Look<float>(ref this.MaxStressLevel, "maxStressLevel", 1f, false);
        Scribe_Values.Look<int>(ref this.ticksToReset, "ticksToReset", -1, false);
        Scribe_Values.Look<bool>(ref this.overloaded, "overloaded", false, false);
        Scribe_Values.Look<bool>(ref this.activeLastTick, "activeLastTick", false, false);
        Scribe_Values.Look<int>(ref this.curShieldRadius, "curShieldRadius", this.Props.shieldScaleDefault, false);
        Scribe_Values.Look<Color>(ref this.currentColor, "currentColor", this.Props.shieldColour, false);
    }
}
