using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Rimgate;

public class SymbioteQueenLineage : IExposable
{
    private readonly struct OffsetCandidate(string statDefName, FloatRange offsetRange)
    {
        public readonly string StatDefName = statDefName;
        public readonly FloatRange OffsetRange = offsetRange;

        public StatDef Stat => DefDatabase<StatDef>.GetNamedSilentFail(StatDefName);
    }

    private static readonly List<OffsetCandidate> OffsetCandidates =
    [
        new OffsetCandidate("ImmunityGainSpeed", new FloatRange(0.10f, 0.25f)),
        new OffsetCandidate("InjuryHealingFactor", new FloatRange(0.20f, 0.50f)),
        new OffsetCandidate("PsychicSensitivity", new FloatRange(0.05f, 0.20f)),
        new OffsetCandidate("PainShockThreshold", new FloatRange(0.05f, 0.15f)),
        new OffsetCandidate("RestFallRateFactor", new FloatRange(-0.12f, -0.04f)),
        new OffsetCandidate("ToxicResistance", new FloatRange(0.10f, 0.30f)),
        new OffsetCandidate("MeditationFocusGainRateFactor", new FloatRange(0.10f, 0.25f)),
        new OffsetCandidate("MeleeCooldownFactor", new FloatRange(0.85f, 0.95f)),
        new OffsetCandidate("MeleeDodgeChance", new FloatRange(4f, 8f)),
        new OffsetCandidate("StaggerDurationFactor", new FloatRange(0.75f, 0.90f)),
        new OffsetCandidate("GlobalLearningFactor", new FloatRange(0.10f, 0.30f)),
        new OffsetCandidate("NegotiationAbility", new FloatRange(0.10f, 0.25f)),
        new OffsetCandidate("WorkSpeedGlobal", new FloatRange(0.10f, 0.25f)),
    ];

    public string QueenName => _queenName;

    public List<SymbioteStatOffset> StatOffsets => _statOffsets;

    public bool HasQueenName => !_queenName.NullOrEmpty();

    public bool HasOffsets => _statOffsets != null && _statOffsets.Count > 0;

    public bool HasLineageData => HasQueenName || HasOffsets;

    private string _queenName;
    private List<SymbioteStatOffset> _statOffsets = new();

    public void ExposeData()
    {
        Scribe_Values.Look(ref _queenName, "_queenName");
        Scribe_Collections.Look(ref _statOffsets, "_statOffsets", LookMode.Deep);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
            _statOffsets ??= [];
    }

    public void EnsureInitialized()
    {
        EnsureQueenName();
        EnsureOffsets();
    }

    public void EnsureQueenName()
    {
        if (!_queenName.NullOrEmpty()) return;
        _queenName = NameGenerator.GenerateName(RimgateDefOf.Rimgate_NamerSymbiote);
    }

    public void EnsureOffsets()
    {
        if (_statOffsets != null && _statOffsets.Count > 0) return;

        _statOffsets ??= [];

        List<OffsetCandidate> candidates = new();
        for (int i = 0; i < OffsetCandidates.Count; i++)
        {
            OffsetCandidate c = OffsetCandidates[i];
            if (c.Stat != null)
                candidates.Add(c);
        }

        if (candidates.Count == 0)
            return;

        candidates.Shuffle();

        int offsetCount = Rand.RangeInclusive(1, 2);
        offsetCount = Mathf.Min(offsetCount, candidates.Count);

        for (int i = 0; i < offsetCount; i++)
        {
            OffsetCandidate candidate = candidates[i];
            _statOffsets.Add(new SymbioteStatOffset(candidate.Stat, candidate.OffsetRange.RandomInRange));
        }
    }

    public float GetOffset(StatDef stat)
    {
        if (stat == null || _statOffsets == null || _statOffsets.Count == 0)
            return 0f;

        for (int i = 0; i < _statOffsets.Count; i++)
        {
            SymbioteStatOffset offset = _statOffsets[i];
            if (offset?.Matches(stat) == true)
                return offset.offset;
        }

        return 0f;
    }

    public string OffsetsDisplayString()
    {
        if (_statOffsets == null || _statOffsets.Count == 0)
            return null;

        System.Text.StringBuilder sb = new();
        for (int i = 0; i < _statOffsets.Count; i++)
        {
            SymbioteStatOffset offset = _statOffsets[i];
            if (offset == null) continue;

            if (sb.Length > 0)
                sb.Append(", ");

            sb.Append(offset.FormatForDisplay());
        }

        return sb.ToString();
    }

    public static SymbioteQueenLineage DeepCopy(SymbioteQueenLineage src)
    {
        if (src == null)
            return null;

        SymbioteQueenLineage dst = new()
        {
            _queenName = src._queenName,
            _statOffsets = []
        };

        if (src._statOffsets != null)
        {
            for (int i = 0; i < src._statOffsets.Count; i++)
            {
                SymbioteStatOffset offset = src._statOffsets[i];
                if (offset != null)
                    dst._statOffsets.Add(offset.DeepCopy());
            }
        }

        return dst;
    }
}
