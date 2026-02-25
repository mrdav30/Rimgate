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

    public static AbilityDef Rimgate_WraithLifeDrainAbility;
    public static AbilityDef Rimgate_WraithEssenceMendingAbility;

    public static BackstoryDef Rimgate_DamagedClone;
    public static BackstoryDef Rimgate_Replicant;
    public static BackstoryDef Rimgate_EnhancedClone;

    public static BodyPartTagDef SightSource;

    public static ChemicalDef Rimgate_SarcophagusChemical;

    public static ConceptDef Rimgate_GateIrisProtection;

    public static DamageDef Rimgate_KawooshExplosion;

    public static DesignationDef Rimgate_DesignationCloseGate;
    public static DesignationDef Rimgate_DesignationPushCart;
    public static DesignationDef Rimgate_DesignationToggle;

    public static DutyDef Rimgate_MaraudColony;

    public static EffecterDef Rimgate_ClonePod_Idle;
    public static EffecterDef Rimgate_ClonePod_Operating;

    public static FactionDef Rimgate_Replicator;
    public static FactionDef Rimgate_TreasureHunters;

    public static GameConditionDef Rimgate_GatePsychicDrone;
    public static GameConditionDef Rimgate_GateToxicFallout;
    public static GameConditionDef Rimgate_GateHeatWave;

    [MayRequireBiotech]
    public static GeneDef Eyes_Red;
    [MayRequireBiotech]
    public static GeneDef Hair_SnowWhite;
    [MayRequireBiotech]
    public static GeneDef Rimgate_CellularDegradation;
    [MayRequireBiotech]
    public static GeneDef Rimgate_SymbiotePouchMutation;
    [MayRequireBiotech]
    public static GeneDef Rimgate_WraithWebbingGene;
    [MayRequireBiotech]
    public static GeneDef Rimgate_WraithPsychic;
    [MayRequireBiotech]
    public static GeneDef Skin_SheerWhite;

    public static HediffDef Rimgate_Clone;
    public static HediffDef Rimgate_ClonedEnduring;
    public static HediffDef Hediff_ClonedTracker;
    public static HediffDef Rimgate_ClonePodSickness;
    public static HediffDef Rimgate_CorticalCatalyst;
    public static HediffDef Rimgate_CorticalCatalyst_Mk2;
    public static HediffDef Rimgate_CrystalResonanceEffect;
    public static HediffDef Rimgate_FlashPulse;
    public static HediffDef Rimgate_KrintakSickness;
    public static HediffDef Rimgate_PouchDegeneration;
    public static HediffDef Rimgate_PrimtaInPouch;
    public static HediffDef Rimgate_PushingCart;
    public static HediffDef Rimgate_SarcophagusHigh;
    public static HediffDef Rimgate_SarcophagusAddiction;
    public static HediffDef Rimgate_SymbioteImplant;
    public static HediffDef Rimgate_SymbioteImplant_Kull;
    public static HediffDef Rimgate_SymbiotePlague;
    public static HediffDef Rimgate_SymbiotePouch;
    public static HediffDef Rimgate_SymbioteWithdrawal;
    public static HediffDef Rimgate_SystemShock;
    public static HediffDef Rimgate_TretoninAddiction;
    public static HediffDef Rimgate_WildPrimtaTakeover;
    public static HediffDef Rimgate_WraithCocoonPodSickness;
    public static HediffDef Rimgate_WraithEssenceDeficit;
    public static HediffDef Rimgate_ZatShock;

    public static HistoryEventDef Rimgate_UsedSarcophagus;
    public static HistoryEventDef Rimgate_InstalledSymbiote;
    public static HistoryEventDef Rimgate_Dwarfgate_ExpeditionStarted;
    public static HistoryEventDef Rimgate_Dwarfgate_SiteExpiredUnvisited;
    public static HistoryEventDef Rimgate_SymbioteDestroyed;

    public static IncidentDef Rimgate_Marauders;

    public static JobDef Rimgate_BringToGate;
    public static JobDef Rimgate_CarryToSarcophagus;
    public static JobDef Rimgate_CarryToCloningPod;
    public static JobDef Rimgate_CarryCorpseToCloningPod;
    public static JobDef Rimgate_CalibrateClonePodForPawn;
    public static JobDef Rimgate_CloseGate;
    public static JobDef Rimgate_DrainLifeFromCocoonedPrisoner;
    public static JobDef Rimgate_WraithCocoonPrisoner;
    public static JobDef Rimgate_DecodeGlyphs;
    public static JobDef Rimgate_DialGate;
    public static JobDef Rimgate_EjectZpmFromHousing;
    public static JobDef Rimgate_EnterCloningPod;
    public static JobDef Rimgate_EnterGate;
    public static JobDef Rimgate_EnterGateWithContainer;
    public static JobDef Rimgate_HaulToContainer;
    public static JobDef Rimgate_IngestFromMobileContainer;
    public static JobDef Rimgate_InsertSymbioteQueen;
    public static JobDef Rimgate_InsertZpmIntoHousing;
    public static JobDef Rimgate_MeditateAtWraithTable;
    public static JobDef Rimgate_MeditateOnGoauldThrone;
    public static JobDef Rimgate_PatientGoToSarcophagus;
    public static JobDef Rimgate_PushContainer;
    public static JobDef Rimgate_RecoverBiomaterialFromCorpse;
    public static JobDef Rimgate_RescueToSarcophagus;
    public static JobDef Rimgate_Toggle;
    public static JobDef Rimgate_WraithEssenceMending;

    public static MapGeneratorDef Rimgate_TransitSiteMap;

    public static NeedDef Food;
    public static NeedDef Rimgate_SarcophagusChemicalNeed;
    public static NeedDef Rimgate_TretoninChemicalNeed;

    public static QuestScriptDef Rimgate_ProtectZPM;
    public static QuestScriptDef Rimgate_GateQuestScript_Planet;
    public static QuestScriptDef Rimgate_GateQuestScript_Orbit;

    public static PawnsArrivalModeDef Rimgate_GateEnterMode;

    public static RaidStrategyDef ImmediateAttackSmart;
    public static RaidStrategyDef StageThenAttack;

    public static ResearchProjectDef Rimgate_DHDResearch;
    public static ResearchProjectDef Rimgate_GateModification;
    public static ResearchProjectDef Rimgate_GlyphDeciphering;
    public static ResearchProjectDef Rimgate_ParallelSubspaceCoupling;
    public static ResearchProjectDef Rimgate_SarcophagusBioregeneration;
    public static ResearchProjectDef Rimgate_SarcophagusOptimization;
    public static ResearchProjectDef Rimgate_WraithCloneGenome;
    public static ResearchProjectDef Rimgate_WraithCloneFull;
    public static ResearchProjectDef Rimgate_WraithCloneEnhancement;
    public static ResearchProjectDef Rimgate_WraithCloneCorpse;
    public static ResearchProjectDef Rimgate_ZPMIntegration;

    public static RulePackDef Rimgate_NamerSymbiote;
    public static RulePackDef Rimgate_DamageEvent_SymbioteAmbushTrap;

    public static SoundDef Rimgate_GoauldGuardHelmToggle;
    public static SoundDef Rimgate_IrisOpen;
    public static SoundDef Rimgate_IrisClose;
    public static SoundDef Rimgate_IrisHit;
    public static SoundDef Rimgate_GateOpen;
    public static SoundDef Rimgate_GateFailDial;
    public static SoundDef Rimgate_GateIdle;
    public static SoundDef Rimgate_GateClose;
    public static SoundDef Rimgate_SymbioteSpawn;
    public static SoundDef Rimgate_WraithCocoonCast;

    public static StatDef MedicalOperationSpeed;
    public static StatDef Rimgate_EssenceCostFactor;
    public static StatDef Rimgate_EssenceGainFactor;

    public static StatCategoryDef RG_QueenLineage;
    public static StatCategoryDef RG_SymbioteMemory;

    public static TattooDef NoTattoo_Body;
    public static TattooDef NoTattoo_Face;

    public static ThingDef Gun_Autopistol;
    public static ThingDef Rimgate_DialHomeDevice;
    public static ThingDef Rimgate_DialHomeDeviceDestroyed;
    public static ThingDef Rimgate_Dwarfgate;
    public static ThingDef Rimgate_GoauldThrone;
    public static ThingDef Rimgate_JammedBlastDoor;
    public static ThingDef Rimgate_Malp;
    public static ThingDef Rimgate_MineableNaquadah;
    public static ThingDef Rimgate_MobileCartProxy;
    public static ThingDef Rimgate_PrimtaSymbiote;
    public static ThingDef Rimgate_PushedCartVisual;
    public static ThingDef Rimgate_SecretDoor;
    public static ThingDef Rimgate_SymbioteSpawningPool;
    public static ThingDef Rimgate_GoauldSymbiote;
    public static ThingDef Rimgate_SubspacePhaseDiverter;
    public static ThingDef Rimgate_Wheelbarrow;
    public static ThingDef Rimgate_WraithBiomassStabilizer;
    public static ThingDef Rimgate_WraithCloningPod;
    public static ThingDef Rimgate_WraithCocoonPod;
    public static ThingDef Rimgate_WraithCyclicWaveInducer;
    public static ThingDef Rimgate_WraithMeatLab;
    public static ThingDef Rimgate_WraithModificationEquipment;
    public static ThingDef Rimgate_WraithTable;
    public static ThingDef Rimgate_ZPM;
    public static ThingDef Rimgate_ZPMHousing;

    public static ThoughtDef Rimgate_GoauldThroneCravingDominion;
    public static ThoughtDef Rimgate_PrimtaFirstPrimtaThought;
    public static ThoughtDef Rimgate_PrimtaMaturedThought;
    public static ThoughtDef Rimgate_PrimtaNewPrimtaThought;
    public static ThoughtDef Rimgate_WraithCommunedWithHive;
    public static ThoughtDef Rimgate_WraithWhispersFromVoid;
    public static ThoughtDef Rimgate_WraithCocoonPod_ReleasedVictim;

    public static ThingSetMakerDef Rimgate_Meteorite;

    public static XenotypeDef Rimgate_Wraith;

    public static WorldObjectDef Rimgate_GateTransitSite;
}
