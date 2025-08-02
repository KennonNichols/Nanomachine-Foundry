using System.Collections.Generic;
using System.Linq;
using NanomachineFoundry.NaniteModifications;
using NanomachineFoundry.Utils;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace NanomachineFoundry
{
    public class WindowModificationConfiguration: Window
    {
        private readonly NaniteTracker_Pawn _tracker;
        private readonly NaniteModificationDef _modification;
        private Vector2 _scrollPosition;
        private int _selectedLevel;
        private NaniteDef _selectedNaniteType;
        private int _maxLevel;
        public WindowModificationConfiguration(NaniteTracker_Pawn tracker, NaniteModificationDef modification, int existingLevel, NaniteDef existingNaniteType)
        {
            _selectedNaniteType = existingNaniteType;
            _selectedLevel = existingLevel;
            _tracker = tracker;
            _modification = modification;
            absorbInputAroundWindow = true;
            resizeable = false;
            closeOnClickedOutside = false;
            doCloseX = true;
            
            ReloadValues();
        }
        
        public override void DoWindowContents(Rect inRect)
        {
            Rect bounds = inRect.ContractedBy(10f);
            Rect informationArea = bounds.TopPart(.8f);
            Rect controlBarArea = bounds.BottomPart(.2f);
            Rect greaterDescriptionArea = informationArea.RightPart(.6f);
            Rect nameArea = greaterDescriptionArea.TopPart(.20f).ContractedBy(2f);
            Rect descriptionArea = greaterDescriptionArea.BottomPart(.80f).ContractedBy(2f);
            Rect visualArea = informationArea.LeftPart(.4f).ContractedBy(5f);
            Rect symbolArea = visualArea.TopPartPixels(visualArea.width);
            Rect counterArea = visualArea.BottomPartPixels(visualArea.height - visualArea.width).ContractedBy(2f);
            Rect sliderArea = controlBarArea.TopPart(.4f);
            Rect naniteTypeDropdownArea = controlBarArea.BottomPart(.4f).LeftHalf().ContractedBy(2f);
            Rect confirmButtonArea = controlBarArea.BottomPart(.4f).RightHalf().ContractedBy(2f);
            

            Text.CurFontStyle.fontSize = (int)(nameArea.height * .3);
            Widgets.Label(nameArea, _modification.label.ToUpper());
            Text.CurFontStyle.fontSize = 0;
            
            Widgets.DrawLine(new Vector2(nameArea.xMin, nameArea.yMax), new Vector2(nameArea.xMax, nameArea.yMax), Color.white, 2);
              
            Widgets.DrawAltRect(descriptionArea);
            Widgets.LabelScrollable(descriptionArea.ContractedBy(5f), _modification.FullDescription(_selectedLevel), ref _scrollPosition);
            
            GUI.DrawTexture(symbolArea, _modification.getIcon());
            
            TooltipHandler.TipRegion(visualArea, NmfUtils.GetScalingDetails(_selectedLevel, _modification));
            
            Text.CurFontStyle.fontSize = (int)(nameArea.height);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(counterArea, _selectedLevel.ToString());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.CurFontStyle.fontSize = 0;

            _selectedLevel = (int)Widgets.HorizontalSlider(sliderArea, _selectedLevel, 0, _maxLevel, true, string.Format("THNMF.NanitesAllocated".Translate(), _selectedNaniteType?.plural ?? "nanites"), roundTo: 1f);
            
            Widgets.Dropdown(naniteTypeDropdownArea, _selectedNaniteType, t => t, DefDropdown, _selectedNaniteType.plural.CapitalizeFirst());

            if (Widgets.ButtonText(confirmButtonArea, "THNMF.AllocateNanites".Translate()))
            {
                _tracker.SetNaniteAllocation(_modification, new ModAllocation(_selectedNaniteType, _selectedLevel));
                Close();
            }
        }

        private void ReloadValues()
        {
            _maxLevel = _tracker.FreeAllocatedSpaceOfType(_selectedNaniteType, _modification);
            _selectedLevel = Mathf.Min(_selectedLevel, _maxLevel);
        }
        
        
        private IEnumerable<Widgets.DropdownMenuElement<NaniteDef>> DefDropdown(NaniteDef currentDef)
        {
            //.Where(def => def != currentDef)
            return NaniteDef.GetNanitesCapableOfEffect(_modification.categoryDef).Intersect(_tracker.AllowedNaniteTypes).Select(naniteType => new Widgets.DropdownMenuElement<NaniteDef>
            {
                option = new FloatMenuOption(naniteType.plural.CapitalizeFirst(), () =>
                {
                    _selectedNaniteType = naniteType;
                    ReloadValues();
                }),
                payload = naniteType
            });
        }
    }
}