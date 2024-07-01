using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace NanomachineFoundry.NaniteProduction
{
    [StaticConstructorOnStartup]
    public class MechaniteBreederNaniteGizmo : Gizmo
    {
        private CompMechaniteBreeder _breedingPlatform;
        private const float Width = 160f;

        private static readonly Texture2D BarTex =
            SolidColorMaterials.NewSolidColorTexture(new Color32(179, 166, 143, byte.MaxValue));
        
        private static readonly Texture2D DangerBarTex =
            SolidColorMaterials.NewSolidColorTexture(new Color32(220, 45, 45, byte.MaxValue));

        private static readonly Texture2D EmptyBarTex =
            SolidColorMaterials.NewSolidColorTexture(GenUI.FillableBar_Empty);

        public MechaniteBreederNaniteGizmo(CompMechaniteBreeder carrier)
        {
            _breedingPlatform = carrier;
        }

        public override float GetWidth(float maxWidth)
        {
            return 160f;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            bool inDanger = _breedingPlatform.InDanger;
            
            Rect rect1 = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Rect rect2 = rect1.ContractedBy(10f);
            Widgets.DrawWindowBackground(rect1);
            var str = (string)"THNMF.BreederExcessNanites".Translate();
            Rect rect3 = new Rect(rect2.x, rect2.y, rect2.width, Text.CalcHeight(str, rect2.width) + 8f);
            Text.Font = GameFont.Small;
            Widgets.Label(rect3, str);
            Rect barRect = new Rect(rect2.x, rect3.yMax, rect2.width, rect2.height - rect3.height);
            float percentFull = _breedingPlatform.PercentFull;
            
            Widgets.FillableBar(barRect, percentFull, inDanger? DangerBarTex: BarTex, EmptyBarTex, true);
            
            
            
            DoBarThreshold(CompMechaniteBreeder.DangerPercent);
            
            
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(barRect,percentFull.ToStringPercent());
            Text.Anchor = TextAnchor.UpperLeft;
            var tooltip = (string)"THNMF.BreederExcessNanitesTip".Translate();
            TooltipHandler.TipRegion(rect2, () => tooltip,
                Gen.HashCombineInt(_breedingPlatform.GetHashCode(), 34242369));
            return new GizmoResult(GizmoState.Clear);

            void DoBarThreshold(float percent)
            {
                GUI.DrawTexture(new Rect
                {
                    x = (float)(barRect.x + 3.0 + (barRect.width - 8.0) * percent),
                    y = (float)(barRect.y + barRect.height - 9.0),
                    width = 2f,
                    height = 6f
                }, BaseContent.BlackTex);
            }
        }
    }
}