using NanomachineFoundry.Utils;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace NanomachineFoundry
{
	// ReSharper disable once InconsistentNaming
	public class ITab_Nanites : ITab
	{
		private bool _editMode;

		private readonly Vector2 _preferredSize;
		private readonly Vector2 _tweakSize;

		private Dictionary<NaniteDef, int> _tempConfigs;

		private NaniteTracker_Pawn SelNaniteTracker => SelPawn?.GetNaniteTracker();

		public override bool IsVisible
		{
			get
			{
				if (SelPawn == null) return false;

				if (!NaniteTracker_Pawn.PawnCanMechanize(SelPawn)) return false;
				
				return SelPawn.IsMechanized() || (NaniteTracker_Pawn.PawnCanMechanize(SelPawn) && Prefs.DevMode);
			}
		}

		public ITab_Nanites()
		{
			labelKey = "THNMF.NanitesTab";
			_preferredSize = new Vector2(550f, 250f);
			_tweakSize = new Vector2(850f, 500f);
			size = _preferredSize;
		}

        public override void OnOpen()
        {
            _tempConfigs = SelNaniteTracker.NaniteConfigRatios.ToDictionary(entry => entry.Key, entry => entry.Value);
        }


        protected override void FillTab()
		{

			Rect bounds;
			if (_editMode)
			{
				size = _tweakSize;
				bounds = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
				NmfMenuUtils.DoTweakConfigMenu(base.SelPawn, ref _tempConfigs, bounds, ConfirmConfig, true);
			}
			else
			{
				size = _preferredSize;
				bounds = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
				NmfMenuUtils.DoReadonlyConfigMenu(base.SelPawn, bounds);
			}

			


			if (Prefs.DevMode)
			{
				Widgets.CheckboxLabeled(new Rect(bounds.xMax - 100f, bounds.yMin + 45f, 100f, 30f), "Edit mode", ref _editMode);
			}
			else
			{									
				_editMode = false;
			}
		}

		private void ConfirmConfig()
		{
			SelNaniteTracker.SetConfiguration(_tempConfigs);	
		}
	}
}
