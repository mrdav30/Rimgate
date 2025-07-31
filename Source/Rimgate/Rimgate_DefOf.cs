using RimWorld;
using Verse;

namespace Rimgate;

[DefOf]
public static class Rimgate_DefOf
{
    static Rimgate_DefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(SoundDefOf));
        DefOfHelper.EnsureInitializedInCtor(typeof(Rimgate_DefOf));
    }

    public static ChemicalDef Rimgate_SarcophagusChemical;

    public static GeneDef Rimgate_WraithCocoonTrap;

    public static HediffDef Rimgate_SymbioteWithdrawal;
    public static HediffDef Rimgate_ZatShock;
    public static HediffDef Rimgate_SarcophagusHigh;
    public static HediffDef Rimgate_SarcophagusAddiction;

    public static JobDef Rimgate_CarryToSarcophagus;
    public static JobDef Rimgate_PatientGoToSarcophagus;
    public static JobDef Rimgate_RescueToSarcophagus;

    public static NeedDef Rimgate_SarcophagusChemicalNeed;

    public static SoundDef Rimgate_GoauldGuardHelmToggle;
    public static SoundDef Rimgate_IrisOpen;
    public static SoundDef Rimgate_IrisClose;
    public static SoundDef Rimgate_IrisHit;
    public static SoundDef Rimgate_StargateOpen;
    public static SoundDef Rimgate_StargateFailDial;
    public static SoundDef Rimgate_StargateIdle;
    public static SoundDef Rimgate_StargateClose;

    public static ThingDef Gun_Autopistol;
}