using System.Linq;
using System.Text;
using NanomachineFoundry.AdministrationWorkers;
using NanomachineFoundry.NaniteModifications;
using NanomachineFoundry.OperatorTabs;
using NanomachineFoundry.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace NanomachineFoundry
{
    public class ITab_NaniteModificationInstallation: OperatorTab
    {
        private readonly Vector2 _preferredSize;
        private bool _showDisabledOperations;

        public ITab_NaniteModificationInstallation()
        {
            labelKey = "THNMF.NaniteModificationTab";
            _preferredSize = new Vector2(600f, 600f);
            size = _preferredSize;
        }
        
        protected override void FillTab()
        {
            if (!TryGetLinkedPawn(out Pawn pawn)) return;
            Widgets.CheckboxLabeled(BaseBounds.TopPart(0.08f).RightPart(0.4f), "Show Disabled", ref _showDisabledOperations);
            NmfMenuUtils.DoOperationMenu(BaseBounds.BottomPart(0.92f).LeftPart(0.90f), pawn, Operator, GetAllVisibleOperationsForTab<ITab_NaniteModificationInstallation>(), _showDisabledOperations);
        }

        public override bool IsVisible
        {
            get
            {
                if (!TryGetLinkedPawn(out _)) return false;
                return GetAllVisibleOperationsForTab<ITab_NaniteModificationInstallation>().Any() && base.IsVisible;
            }
        }
    }
}
