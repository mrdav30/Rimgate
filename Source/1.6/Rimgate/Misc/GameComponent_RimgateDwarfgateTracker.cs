using Verse;

namespace Rimgate;

public sealed class GameComponent_RimgateDwarfgateTracker : GameComponent
{
    public int LastExpeditionTick = -1;
    public int LastIgnoredTick = -1;

    public GameComponent_RimgateDwarfgateTracker(Game game) { }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref LastExpeditionTick, "rg_lastDwarfgateExpeditionTick", -1);
        Scribe_Values.Look(ref LastIgnoredTick, "rg_lastDwarfgateIgnoredTick", -1);
    }

    public static GameComponent_RimgateDwarfgateTracker Get()
        => Current.Game.GetComponent<GameComponent_RimgateDwarfgateTracker>();
}
