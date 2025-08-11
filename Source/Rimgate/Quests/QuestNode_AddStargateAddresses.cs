using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace Rimgate;

public class QuestNode_AddStargateAddresses : QuestNode
{
    public SlateRef<Site> address;

    public SlateRef<bool> remove;

    protected override bool TestRunInt(Slate slate) => true;

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        Site site = address.GetValue(slate);
        PlanetTile tile = site.Tile;

        WorldComp_StargateAddresses sgWorldComp = Find.World.GetComponent<WorldComp_StargateAddresses>();
        if (sgWorldComp == null)
        {
            Log.Error("Rimgate :: QuestNode_AddStargateAddresses tried to get WorldComp_StargateAddresses but it was null.");
            return;
        }

        sgWorldComp.CleanupAddresses();
        if (remove.GetValue(slate)) sgWorldComp.RemoveAddress(tile);
        else sgWorldComp.AddAddress(tile);
    }
}
