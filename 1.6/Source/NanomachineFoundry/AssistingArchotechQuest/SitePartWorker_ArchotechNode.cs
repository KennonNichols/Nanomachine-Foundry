using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace NanomachineFoundry.AssistingArchotechQuest
{
    public class SitePartWorker_ArchotechNode: SitePartWorker
    {
        public override void PostDestroy(SitePart sitePart)
        {
            base.PostDestroy(sitePart);
            AssistingArchotechQuestlineUtility.CompleteAllNodeQuests(QuestEndOutcome.Fail);
        }
    }
}