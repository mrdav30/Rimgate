using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Rimgate;

/*
 * Unas are are an intermediate between animal and humanlike.
 * They have full-fledged cleaning and hauling work givers instead of stubs other animals have, allowing them to do all relevant jobs, like refueling!
 *
 * They are still not fully sapient, however, so you need to teach them what they have to do and keep necessary training up.
 *
 */
public class Pawn_Unas : Pawn
{
    private static List<WorkTypeDef> _allWorkTypes;

    private List<WorkTypeDef> _cachedDisabledWorkTypes;

    private List<WorkTypeDef> _cachedDisabledWorkTypesPermanent;

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);

        if (IsFormerHuman())
        {
            LogUtil.Debug($"Skipping Unas setup for former human {def.defName}");
            return; // former humans have their own logic, ignore them
        }

        // Can avoid this by making them using mech code,
        // but it may require way more work so this hack would do
        skills ??= new(this);
        foreach (SkillRecord skill in skills.skills)
            skill.Level = 6;  // Make skills neutral for price factor

        // TODO: is this still necessary?
        // necessary for job giver to work properly,
        // but adds a bunch of problems since only humanlikes are supposed to have it
        story ??= new(this);
        story.bodyType = gender == Gender.Male
            ? BodyTypeDefOf.Hulk
            : BodyTypeDefOf.Female;
        //crownType = CrownType.Average,
        //childhood = xxx,
        ////adulthood = xxx

        // only used for WorkGiversInOrderNormal / WorkGiversInOrderEmergency
        workSettings ??= new(this);
        workSettings.EnableAndInitialize();

        // both genders can do both cleaning and hauling,
        // but males prefer hauling and females prefer cleaning so they divide jobs
        // and don't neglect one or another too much
        if (gender == Gender.Female)
            workSettings.SetPriority(WorkTypeDefOf.Hauling, 4);

        GetDisabledWorkTypes(); // init stuff
        workSettings.Notify_DisabledWorkTypesChanged();
    }

    /**
     * Stripped to bare bones. Uses mechanoid tags for available job tags.
     */
    public new List<WorkTypeDef> GetDisabledWorkTypes(bool permanentOnly = false)
    {
        if (IsFormerHuman())
            return base.GetDisabledWorkTypes(permanentOnly);

        _allWorkTypes ??= DefDatabase<WorkTypeDef>.AllDefsListForReading;
        if (permanentOnly)
        {
            _cachedDisabledWorkTypesPermanent ??= new();
            _cachedDisabledWorkTypesPermanent.Clear();
            FillList(_cachedDisabledWorkTypesPermanent);
            return _cachedDisabledWorkTypesPermanent;
        }

        _cachedDisabledWorkTypes ??= new();
        _cachedDisabledWorkTypes.Clear();
        FillList(_cachedDisabledWorkTypes);
        return _cachedDisabledWorkTypes;

        void FillList(List<WorkTypeDef> list)
        {
            for (int j = 0; j < _allWorkTypes.Count; j++)
            {
                var workType = _allWorkTypes[j];
                if (!RaceProps.mechEnabledWorkTypes.Contains(workType)
                    && !list.Contains(workType))
                {
                    list.Add(workType);
                }
            }
        }
    }

    /**
     * For Pawnmorpher compatibility
     */
    public bool IsFormerHuman()
    {
        return story != null && (story.Childhood != null || story.Adulthood != null);
    }
}
