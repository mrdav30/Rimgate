using Verse;

namespace Rimgate;

public class CalibrationOutcome : IExposable
{
    public CloneType cloneType;

    public bool AnyIssues => MinorIssues || MajorIssues;

    public bool MinorIssues => minorGenderSwap || minorAlbino || minorBonusGenes || minorRandomTraits;

    public bool minorGenderSwap;
    public bool minorAlbino;
    public bool minorBonusGenes;
    public bool minorRandomTraits;

    public bool MajorIssues => majorCloneDies || majorHostDies || majorIdiotClone || majorInsaneClone;

    public bool majorCloneDies;
    public bool majorHostDies;
    public bool majorIdiotClone;
    public bool majorInsaneClone;

    public void ExposeData()
    {
        Scribe_Values.Look(ref cloneType, nameof(cloneType));

        Scribe_Values.Look(ref minorGenderSwap, nameof(minorGenderSwap));
        Scribe_Values.Look(ref minorAlbino, nameof(minorAlbino));
        Scribe_Values.Look(ref minorBonusGenes, nameof(minorBonusGenes));
        Scribe_Values.Look(ref minorRandomTraits, nameof(minorRandomTraits));

        Scribe_Values.Look(ref majorCloneDies, nameof(majorCloneDies));
        Scribe_Values.Look(ref majorHostDies, nameof(majorHostDies));
        Scribe_Values.Look(ref majorIdiotClone, nameof(majorIdiotClone));
        Scribe_Values.Look(ref majorInsaneClone, nameof(majorInsaneClone));
    }
}
