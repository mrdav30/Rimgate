using RimWorld;
using Verse;
using Verse.AI;

namespace Rimgate;

[DefOf]
public static class RimgateDefOf
{
    static RimgateDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(SoundDefOf));
        DefOfHelper.EnsureInitializedInCtor(typeof(RimgateDefOf));
    }

    public static ChemicalDef Rimgate_SarcophagusChemical;

    public static DamageDef Rimgate_KawooshExplosion;

    public static DesignationDef Rimgate_DesignationToggle;
    public static DesignationDef Rimgate_DesignationToggleIris;
    public static DesignationDef Rimgate_DesignationCloseStargate;

    public static DutyDef Rimgate_MaraudColony;

    public static EffecterDef Rimgate_ClonePod_Idle;
    public static EffecterDef Rimgate_ClonePod_Operating;

    public static FactionDef Rimgate_Replicator;
    public static FactionDef Rimgate_TreasureHunters;
    public static FactionDef Rimgate_TreasureHuntersHostile;

    public static GameConditionDef Rimgate_StargatePsychicDrone;
    public static GameConditionDef Rimgate_StargateToxicFallout;
    public static GameConditionDef Rimgate_StargateHeatWave;

    [MayRequireBiotech]
    public static GeneDef Skin_SheerWhite;
    [MayRequireBiotech]
    public static GeneDef Hair_SnowWhite;
    [MayRequireBiotech]
    public static GeneDef Eyes_Red;
    [MayRequireBiotech]
    public static GeneDef Rimgate_WraithCocoonTrap;
    [MayRequireBiotech]
    public static GeneDef Rimgate_WraithPsychic;
    [MayRequireBiotech]
    public static GeneDef Rimgate_CellularDegradation;

    public static HediffDef Rimgate_Clone;
    public static HediffDef Hediff_ClonedTracker;
    public static HediffDef Rimgate_ClonePodSickness;
    public static HediffDef Rimgate_Enduring;
    public static HediffDef Rimgate_PushingCart;
    public static HediffDef Rimgate_SarcophagusHigh;
    public static HediffDef Rimgate_SarcophagusAddiction;
    public static HediffDef Rimgate_SymbioteImplant;
    public static HediffDef Rimgate_SymbiotePlague;
    public static HediffDef Rimgate_SymbiotePouch;
    public static HediffDef Rimgate_SymbioteWithdrawal;
    public static HediffDef Rimgate_SystemShock;
    public static HediffDef Rimgate_WraithEssenceDeficit;
    public static HediffDef Rimgate_ZatShock;

    public static HistoryEventDef Rimgate_UsedGoauldSarcophagus;

    public static IncidentDef Rimgate_Marauders;

    public static JobDef Rimgate_BringToStargate;
    public static JobDef Rimgate_CarryToSarcophagus;
    public static JobDef Rimgate_CarryToCloningPod;
    public static JobDef Rimgate_CarryCorpseToCloningPod;
    public static JobDef Rimgate_CloneOccupantGenes;
    public static JobDef Rimgate_CloneOccupantFull;
    public static JobDef Rimgate_CloneOccupantSoldier;
    public static JobDef Rimgate_CloneReconstructDead;
    public static JobDef Rimgate_CloseStargate;
    public static JobDef Rimgate_DecodeGlyphs;
    public static JobDef Rimgate_DialStargate;
    public static JobDef Rimgate_EnterCloningPod;
    public static JobDef Rimgate_EnterStargate;
    public static JobDef Rimgate_EnterStargateWithContainer;
    public static JobDef Rimgate_HaulToContainer;
    public static JobDef Rimgate_InsertSymbioteQueen;
    public static JobDef Rimgate_MeditateAtWraithTable;
    public static JobDef Rimgate_MeditateOnGoauldThrone;
    public static JobDef Rimgate_PatientGoToSarcophagus;
    public static JobDef Rimgate_PushMobileContainer;
    public static JobDef Rimgate_PushAndDumpMobileContainer;
    public static JobDef Rimgate_RescueToSarcophagus;
    public static JobDef Rimgate_Toggle;
    public static JobDef Rimgate_ToggleIris;

    public static MapGeneratorDef Rimgate_TransitSiteMap;

    public static NeedDef Food;
    public static NeedDef Rimgate_SarcophagusChemicalNeed;

    public static QuestScriptDef Rimgate_ProtectZPM;
    public static QuestScriptDef Rimgate_StargateQuestScript;

    public static PawnsArrivalModeDef Rimgate_StargateEnterMode;

    public static RaidStrategyDef ImmediateAttackSmart;
    public static RaidStrategyDef StageThenAttack;

    public static ResearchProjectDef Rimgate_CloningPodOptimization;
    public static ResearchProjectDef Rimgate_GlyphDeciphering;
    public static ResearchProjectDef Rimgate_ParallelSubspaceCoupling;
    public static ResearchProjectDef Rimgate_SarcophagusBioregeneration;
    public static ResearchProjectDef Rimgate_SarcophagusOptimization;
    public static ResearchProjectDef Rimgate_WraithCloneGenome;
    public static ResearchProjectDef Rimgate_WraithCloneFull;
    public static ResearchProjectDef Rimgate_WraithCloneEnhancement;
    public static ResearchProjectDef Rimgate_WraithCloneCorpse;
    public static ResearchProjectDef Rimgate_WraithModificationEquipment;
    public static ResearchProjectDef Rimgate_ZPMIntegration;

    public static SoundDef Rimgate_GoauldGuardHelmToggle;
    public static SoundDef Rimgate_IrisOpen;
    public static SoundDef Rimgate_IrisClose;
    public static SoundDef Rimgate_IrisHit;
    public static SoundDef Rimgate_StargateOpen;
    public static SoundDef Rimgate_StargateFailDial;
    public static SoundDef Rimgate_StargateIdle;
    public static SoundDef Rimgate_StargateClose;
    public static SoundDef Rimgate_SymbioteSpawn;

    public static TattooDef NoTattoo_Body;
    public static TattooDef NoTattoo_Face;

    public static ThingDef Gun_Autopistol;
    public static ThingDef Rimgate_DialHomeDevice;
    public static ThingDef Rimgate_GoauldThrone;
    public static ThingDef Rimgate_JammedBlastDoor;
    public static ThingDef Rimgate_Malp;
    public static ThingDef Rimgate_MineableNaquadah;
    public static ThingDef Rimgate_MobileCartProxy;
    public static ThingDef Rimgate_PushedCartVisual;
    public static ThingDef Rimgate_SecretDoor;
    public static ThingDef Rimgate_Stargate;
    public static ThingDef Rimgate_SubspacePhaseDiverter;
    public static ThingDef Rimgate_Wheelbarrow;
    public static ThingDef Rimgate_WraithMeatLab;
    public static ThingDef Rimgate_WraithTable;
    public static ThingDef Rimgate_ZPM;

    public static ThoughtDef Rimgate_GoauldThroneCravingDominion;
    public static ThoughtDef Rimgate_WraithCommunedWithHive;
    public static ThoughtDef Rimgate_WraithWhispersFromVoid;

    public static ThingSetMakerDef Rimgate_Meteorite;

    public static XenotypeDef Rimgate_Wraith;

    public static WorldObjectDef Rimgate_StargateQuestSite;
    public static WorldObjectDef Rimgate_StargateTransitSite;
}