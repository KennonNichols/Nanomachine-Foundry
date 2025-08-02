using System.Collections.Generic;
using System.Linq;
using NanomachineFoundry.AdministrationWorkers;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using Verse.Sound;

namespace NanomachineFoundry.AssistingArchotechQuest
{
    public class QuestNode_Root_AssistingArchotechBuildingAntennae : QuestNode
    {
	    
		protected override void RunInt()
		{
			Find.WindowStack.Add(new Dialog_Message("THNMF.PsychicScream".Translate()));
			if (ModLister.RoyaltyInstalled)
			{
				NMF_DefsOf.AnimaTreeScream.PlayOneShotOnCamera();
			}
			Quest quest = QuestGen.quest;
			quest.End(QuestEndOutcome.Success, 0, null, "ArchotechNexusTriggered", sendStandardLetter: true);
		}

		protected override bool TestRunInt(Slate slate)
		{
			return true;
		}

    }
}