using System.Linq;
using NanomachineFoundry.OperatorTabs;
using NanomachineFoundry.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace NanomachineFoundry
{
    public class ITab_NaniteOperation: OperatorTab
    {
        private readonly Vector2 _preferredSize;

        public ITab_NaniteOperation()
        {
            labelKey = "THNMF.NaniteOperationTab";
            _preferredSize = new Vector2(300f, 350f);
            size = _preferredSize;
        }
        
        
        
        protected override void FillTab()
        {
            if (TryGetLinkedPawn(out Pawn pawn))
            {
                NmfMenuUtils.DoOperationMenu(BaseBounds.LeftPart(0.90f), pawn, Operator, GetAllVisibleOperationsForTab<ITab_NaniteOperation>());
            }
        }

        public override bool IsVisible
        {
            get
            {
                if (!TryGetLinkedPawn(out _)) return false;
                return GetAllVisibleOperationsForTab<ITab_NaniteOperation>().Any() && base.IsVisible;
            }
        }
    }
}