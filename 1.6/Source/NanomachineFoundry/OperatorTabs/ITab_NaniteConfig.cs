using NanomachineFoundry.OperatorTabs;
using NanomachineFoundry.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace NanomachineFoundry
{
    public class ITab_NaniteConfig: OperatorTab
    {
        private readonly Vector2 _preferredSize;

        public ITab_NaniteConfig()
        {
            labelKey = "THNMF.NaniteConfigTab";
            _preferredSize = new Vector2(850f, 500f);
            size = _preferredSize;
        }
        
        protected override void FillTab()
        {

            if (TryGetLinkedPawn(out Pawn pawn))
            {
                NmfMenuUtils.DoTweakConfigMenu(pawn, ref Operator.TempConfigs, BaseBounds, delegate {Operator.StartOperation(NMF_DefsOf.THNMF_Reconfigure);});
            }
        }

        public override bool IsVisible => TryGetLinkedPawn(out Pawn pawn) && pawn.IsMechanized() && base.IsVisible;
    }
}