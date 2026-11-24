using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Verse;

namespace Rimgate;

public class SymbioteMemory : IExposable
{
    // must be at least "adept" to pass on skill bonuses
    public const int PawnMinSkillLevel = 6;

    // absolute cap on skill levels a symbiote can grant, to prevent absurdities with multiple hosts and/or mods that add many levels
    public const int MaxSkillLevel = 20;

    // cap on how many bonus levels a symbiote can grant for a single skill, to prevent absurdities with multiple hosts and/or mods that add many levels
    public const int SkillCap = 10;

    // Allow up to 4 hosts, afterwards symbiote is destroyed
    public const int MaxPreviousHosts = 3;

    private static readonly IntRange RandomSkillDelta = new(1, 2);

    public string SymbioteName => _symbioteName;

    public int PriorHostCount => _priorHostCount;

    public List<string> PreviousHostIds => _previousHostIds;

    public bool IsAtLimit => _priorHostCount == MaxPreviousHosts;

    public bool IsOverLimit => _priorHostCount > MaxPreviousHosts;

    // key = SkillDef.defName, value = bonus levels
    public Dictionary<string, int> SkillBonuses => _skillBonuses;

    public string _symbioteName;

    private int _priorHostCount;

    private List<string> _previousHostIds;

    private Dictionary<string, int> _skillBonuses = [];

    private List<string> _keys;
    private List<int> _values;

    public void ExposeData()
    {
        Scribe_Values.Look(ref _symbioteName, "_symbioteName");
        Scribe_Values.Look(ref _priorHostCount, "_priorHostCount");
        Scribe_Values.Look(ref _previousHostIds, "_previousHostIds");
        Scribe_Collections.Look(ref _skillBonuses, "_skillBonuses",
            LookMode.Value, LookMode.Value, ref _keys, ref _values);
    }

    public int GetBonus(SkillDef def)
    {
        return _skillBonuses.TryGetValue(def.defName, out var v)
            ? v
            : 0;
    }

    public void AddRandomBonus(SkillDef def) => AddBonus(def, RandomSkillDelta.RandomInRange);

    public void AddBonus(SkillDef def, int delta)
    {
        if (!_skillBonuses.TryGetValue(def.defName, out var cur))
            cur = 0;
        var add = Math.Min(cur + delta, SkillCap);
        if (add > 0)
            _skillBonuses[def.defName] = add;
    }

    public void RemoveBonus(SkillDef def, int delta)
    {
        if (!_skillBonuses.TryGetValue(def.defName, out var cur))
            return;
        var remove = Math.Max(cur - delta, 0);
        if (remove == 0)
            _skillBonuses.Remove(def.defName);
        else
            _skillBonuses[def.defName] = remove;
    }

    public void EnsureName()
    {
        if (!_symbioteName.NullOrEmpty()) return;
        _symbioteName = NameGenerator.GenerateName(RimgateDefOf.Rimgate_NamerSymbiote);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsPreviousHost(Pawn host)
    {
        if (_previousHostIds == null) return false;

        var hostId = host.ThingID;
        bool found = false;
        for (int i = 0; i < _previousHostIds.Count; i++)
        {
            if (_previousHostIds[i] == hostId)
            {
                found = true;
                break;
            }
        }
        return found;
    }

    public void MarkPreviousHost(Pawn host)
    {
        if (IsPreviousHost(host)) return;
        _previousHostIds ??= [];
        _previousHostIds.Add(host.ThingID);
        _priorHostCount++;
    }

    public static SymbioteMemory DeepCopy(SymbioteMemory src)
    {
        if (src == null) return null;
        var dst = new SymbioteMemory
        {
            _symbioteName = src.SymbioteName,
            _skillBonuses = src._skillBonuses?.ToDictionary(kv => kv.Key, kv => kv.Value),
            _priorHostCount = src.PriorHostCount,
            _previousHostIds = src.PreviousHostIds
        };
        return dst;
    }

    /// <summary>
    /// Reverts whatever XP this symbiote gave its current host.
    /// Call this *before* removing the hediff.
    /// </summary>
    public void RemoveSessionBonuses(Pawn pawn)
    {
        if (pawn?.skills == null)
            return;

        if (_skillBonuses == null) return;

        foreach (var kv in _skillBonuses)
        {
            var def = DefDatabase<SkillDef>.GetNamedSilentFail(kv.Key);
            if (def == null) continue;

            var rec = pawn.skills.GetSkill(def);
            if (rec == null || rec.TotallyDisabled) continue;

            int newLevel = Math.Max(rec.Level - kv.Value, 0);
            rec.Level = newLevel;
        }
    }

    public IEnumerable<StatDrawEntry> GetStatDrawEntries()
    {
        yield return new StatDrawEntry(
            StatCategoryDefOf.BasicsImportant,
            "RG_Symbiote_Stat_PreviousHostCount_Label".Translate(),
            PriorHostCount.ToString("F0"),
            "RG_Symbiote_Stat_PreviousHostCount_Desc".Translate(),
            4991);

        if (_skillBonuses == null || _skillBonuses.Count <= 0)
            yield break;

        foreach (var kv in _skillBonuses
                     .Where(kv => kv.Value > 0)
                     .OrderByDescending(kv => kv.Value))
        {
            var def = DefDatabase<SkillDef>.GetNamedSilentFail(kv.Key);
            if (def == null) continue;

            yield return new StatDrawEntry(
                RimgateDefOf.RG_SymbioteMemory,
                def.skillLabel.CapitalizeFirst(),
                $"+{kv.Value}",
                def.description,
                4670);
        }
    }
}
