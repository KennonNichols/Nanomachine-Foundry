using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using Verse.Sound;

namespace NanomachineFoundry.AssistingArchotechQuest
{
    public class QuestNode_Root_AssistingArchotechSecondaryNode : QuestNode
    {
        protected override void RunInt()
        {
            if (TryFindSiteTile(out int tile))
            {
                Find.WorldObjects.Add(SiteMaker.TryMakeSite(new[] { NMF_DefsOf.THNMF_ArchotechNodeSite }, tile));
            }
            else
            {
                Log.Error("Failed to generate node site!");
            }
            
            Quest quest = QuestGen.quest;
            quest.End(QuestEndOutcome.Success, 0, null, "ArchotechNodeAccessed", sendStandardLetter: true);
        }
        
        
        
        private bool TryFindSiteTile(out int tile, bool exitOnFirstTileFound = false)
        {
            return TileFinder.TryFindNewSiteTile(out tile, 2, 5, allowCaravans: true, TileFinderMode.Near, -1, exitOnFirstTileFound);
        }

        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
    }
}