using System;
using NanomachineFoundry.Utils;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace NanomachineFoundry
{
    class WindowReconfigurePrompt : Window
    {
        IEnumerable<string> issues;
        Dictionary<NaniteDef, int> tempConfigs;
        NaniteTracker_Pawn tracker;
        private const float issueLineHeight = 50f;
        private Action onConfirm;

        public WindowReconfigurePrompt(IEnumerable<string> issues, Action onConfirm)
        {
            this.issues = issues;
            this.onConfirm = onConfirm;
            absorbInputAroundWindow = true;
            resizeable = false;
            doCloseX = false;
            closeOnClickedOutside = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect warningSection = inRect.TopPartPixels(issueLineHeight * issues.Count());
            Rect buttonSection = inRect.BottomPartPixels(issueLineHeight);

            float labelHeight = warningSection.height / issues.Count();

            int i = 0;
            foreach (string warning in issues)
            {
                Widgets.Label(new Rect(inRect.xMin, inRect.yMin + labelHeight * i, inRect.width, labelHeight), warning);
                i++;
            }

            if (Widgets.ButtonText(GenUI.ContractedBy(buttonSection.LeftHalf(), 5f), "THNMF.ReconfigureCancel".Translate(), true, true, Color.red))
            {
                Close();
            }
            if (Widgets.ButtonText(GenUI.ContractedBy(buttonSection.RightHalf(), 5f), "THNMF.ReconfigureAccept".Translate(), true, true, Color.green ))
            {
                onConfirm.Invoke();
                Close();
            }


        }
    }
    
    class WindowNeurosanitizePrompt : Window
    {
        //IEnumerable<TraitDef> traits;
        private readonly Pawn _pawn;
        private const float TraitLineHeight = 50f;

        public WindowNeurosanitizePrompt(Pawn pawn)
        {
            _pawn = pawn;
            resizeable = false;
            doCloseX = false;
            forcePause = true;
            closeOnAccept = false;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = true;
            closeOnAccept = false;
            closeOnCancel = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Widgets.Label(GetLineRect(0, inRect), "THNMF.RemoveTraitLabel".Translate());
            int i = 1;

            foreach (Trait trait in _pawn.story.traits.allTraits)
            {
                if (Widgets.ButtonText(GetLineRect(i, inRect), trait.Label))
                {
                    _pawn.story.traits.RemoveTrait(trait);
                    Close();
                    break;
                }
                i++;
            }
        }

        private static Rect GetLineRect(int rowIndex, Rect inRect)
        {
            return new Rect(inRect.xMin, inRect.yMin + TraitLineHeight * rowIndex, inRect.width, TraitLineHeight);
        }
    }
}
