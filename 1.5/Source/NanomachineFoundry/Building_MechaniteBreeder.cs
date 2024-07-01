using NanomachineFoundry.NaniteProduction;
using UnityEngine;
using Verse;

namespace NanomachineFoundry
{
    public class Building_MechaniteBreeder: Building
    {

        private CompMechaniteBreeder BreederComp => _breederComp ??= this.TryGetComp<CompMechaniteBreeder>();
        private CompMechaniteBreeder _breederComp;

        
        private static float drawY = 3 / 16f;
        
        public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
        {
            base.DynamicDrawPhaseAt(phase, drawLoc, flip);

            Pawn occupant = BreederComp.Occupant;
            
            if (occupant == null) return;
            //Draw the mechanoid
            Vector3 drawPos = DrawPos;
            drawPos.z += 0.2f;
            
            drawPos.y += drawY;
            drawY += 0.01f;
			
            //Building_HoldingPlatform
			
            occupant.Drawer.renderer.DynamicDrawPhaseAt(DrawPhase.Draw, drawPos, Rot4.FromIntVec3(new IntVec3(0, 0, 1)), neverAimWeapon: true);
        }
    }
}