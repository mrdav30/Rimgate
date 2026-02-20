using RimWorld;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace Rimgate;

public class Comp_Dissolves : ThingComp
{
    private static List<string> _tmpDissolveReasons = new List<string>();

    private CompProperties_Dissolves Props => (CompProperties_Dissolves)props;

    public bool IsOutdoors
    {
        get
        {
            Map mapHeld = parent.MapHeld;
            IntVec3 position = parent.Position;
            if (mapHeld != null)
            {
                Room room = position.GetRoom(mapHeld);
                if (room != null && room.UsesOutdoorTemperature)
                    return true;
            }

            return false;
        }
    }

    public bool IsFrozen => parent.AmbientTemperature <= 0f;

    public bool IsBeingRainedOn => parent.MapHeld.weatherManager.curWeather.rainRate > 0.1f;

    public bool DissolutionEnabled => !_dissolutionDisabled;

    public bool CanDissolveNow
    {
        get
        {
            if (!IsFrozen)
                return DissolutionEnabled;

            return Props.destroyIfFrozen;
        }
    }

    public int DeterioarationRate => (int)parent.GetStatValue(StatDefOf.DeteriorationRate);

    public int DissolutionAfterDamage => DeterioarationRate * Props.dissolutionAfterDays;

    public int DefaultTicksUntilDissolution => 60000 * Props.dissolutionAfterDays;

    public int DissolutionIntervalTicks
    {
        get
        {
            if (IsFrozen && Props.destroyIfFrozen)
                return 1;

            float num = DefaultTicksUntilDissolution;
            if (!IsOutdoors)
                num /= Props.dissolutionFactorIndoors;
            else if (IsBeingRainedOn)
                num /= Props.dissolutionFactorRain;

            return Mathf.CeilToInt(num);
        }
    }

    private int _dissolveTicks;

    private bool _dissolutionDisabled;

    public override void CompTick()
    {
        base.CompTick();
        if (CanDissolveNow)
        {
            _dissolveTicks++;
            if (_dissolveTicks >= DissolutionIntervalTicks)
                TriggerDissolutionEvent(Props.amountPerDissolution);
        }
    }

    public void TriggerDissolutionEvent(int amountToDissolve = 1)
    {
        amountToDissolve = Mathf.Min(amountToDissolve, parent.stackCount);
        parent.stackCount -= amountToDissolve;
        _dissolveTicks = 0;
        if (parent.stackCount <= 0 && !parent.Destroyed)
            parent.Destroy();
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (!DebugSettings.ShowDevGizmos)
            yield break;

        yield return new Command_Action
        {
            defaultLabel = "DEV: Dissolution event",
            action = delegate
            {
                TriggerDissolutionEvent(Props.amountPerDissolution);
            }
        };
        yield return new Command_Action
        {
            defaultLabel = "DEV: Dissolution event until destroyed",
            action = delegate
            {
                int num = 1000;
                while (!parent.Destroyed && num > 0)
                {
                    TriggerDissolutionEvent(Props.amountPerDissolution);
                    num--;
                }
            }
        };
        yield return new Command_Action
        {
            defaultLabel = "DEV: Dissolution progress +25%",
            action = delegate
            {
                parent.TakeDamage(new DamageInfo(DamageDefOf.Deterioration, (float)DissolutionAfterDamage * 0.25f));
            }
        };
        yield return new Command_Action
        {
            defaultLabel = "DEV: Set next dissolve time",
            action = delegate
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                int[] array = new int[11]
                {
                    60, 120, 180, 240, 300, 600, 900, 1200, 1500, 1800,
                    3600
                };
                foreach (int ticks in array)
                {
                    list.Add(new FloatMenuOption(ticks.ToStringSecondsFromTicks("F0"), delegate
                    {
                        _dissolveTicks = DissolutionIntervalTicks - ticks;
                    }));
                }

                Find.WindowStack.Add(new FloatMenu(list));
            }
        };
    }

    public override string CompInspectStringExtra()
    {
        if (!DissolutionEnabled)
            return base.CompInspectStringExtra();

        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(base.CompInspectStringExtra());
        if (stringBuilder.Length > 0)
            stringBuilder.AppendLine();

        if (IsFrozen)
        {
            stringBuilder.Append(!Props.destroyIfFrozen ? "DissolutionFrozen".Translate() : "RG_DissolveFrozenDestroys".Translate());
            return stringBuilder.ToString();
        }

        _tmpDissolveReasons.Clear();
        if (!IsOutdoors)
            _tmpDissolveReasons.Add($"{"DissolutionRateIndoors".Translate()} x{Props.dissolutionFactorIndoors.ToStringPercent()}");
        else if (IsBeingRainedOn)
            _tmpDissolveReasons.Add($"{"DissolutionRain".Translate()} x{Props.dissolutionFactorRain.ToStringPercent()}");

        int amountToDissolve = Mathf.Min(Props.amountPerDissolution, parent.stackCount);
        stringBuilder.Append("RG_DissolvesEvery".Translate(amountToDissolve, parent.LabelShort, Props.dissolveEveryActionVerb, DissolutionIntervalTicks.ToStringTicksToPeriod()));
        if (_tmpDissolveReasons.Count > 0)
        {
            string str = _tmpDissolveReasons.ToLineList();
            stringBuilder.Append(" (" + str.CapitalizeFirst() + ")");
            _tmpDissolveReasons.Clear();
        }

        return stringBuilder.ToString();
    }

    public void SetDissolutionDisabled(bool disabled) => _dissolutionDisabled = disabled;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref _dissolveTicks, "_dissolveTicks", 0);
        Scribe_Values.Look(ref _dissolutionDisabled, "_dissolutionDisabled", false);
    }
}
