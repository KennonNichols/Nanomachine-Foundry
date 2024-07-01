using System;
using System.Collections.Generic;
using System.Linq;
using NanomachineFoundry.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace NanomachineFoundry.OperatorTabs
{
    public abstract class OperatorTab: ITab
    {
        //private CompNaniteOperator _naniteOperator;
        //protected CompNaniteOperator Operator => _naniteOperator ?? (_naniteOperator = SelThing.TryGetComp<CompNaniteOperator>());
        //Getting manually is way safer than caching the operator
        protected CompNaniteOperator Operator => SelThing.TryGetComp<CompNaniteOperator>();
        
        protected Rect BaseBounds => new Rect(0f, 0f, size.x, size.y).ContractedBy(20f);
        
        protected bool TryGetLinkedTracker(out NaniteTracker_Pawn tracker)
        {
            return Operator.TryGetLinkedTracker(out tracker);
        }
        
        protected bool TryGetLinkedPawn(out Pawn pawn)
        {
            return Operator.TryGetLinkedPawn(out pawn);
        }

        protected IEnumerable<NaniteOperationDef> GetAllVisibleOperationsForTab<T>()
        {
            if (TryGetLinkedPawn(out Pawn pawn))
            {
                return DefDatabase<NaniteOperationDef>.AllDefs.Where(op => op.IsVisible(pawn) && op.tabType == typeof(T));
            }
            throw new Exception("Tried getting valid administrations while no pawn was inside.");
        }

        public override bool IsVisible => !Operator.IsOperating;
    }
}