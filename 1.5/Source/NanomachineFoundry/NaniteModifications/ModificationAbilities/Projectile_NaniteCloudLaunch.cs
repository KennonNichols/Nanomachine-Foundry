using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NanomachineFoundry.NaniteModifications.ModificationAbilities
{
    public class Projectile_NaniteCloudLaunch: Projectile
    {
        private Material materialResolved;

        public override Material DrawMat => materialResolved;

        private IEnumerable<IntVec3> _cells;
        private ThingDef _naniteCloudDef;
        private HediffDef _inflictionHediff;
        private Pawn _originatorPawn;
        private int _durationTicks;
        private Color _color;
        private bool _intelligent;

        
        public void ConfigureCloudPayload(IEnumerable<IntVec3> cells, ThingDef naniteCloudDef, HediffDef inflictionHediff, Pawn originatorPawn, int durationTicks, Color color, bool intelligent)
        {
            _cells = cells;
            _naniteCloudDef = naniteCloudDef;
            _inflictionHediff = inflictionHediff;
            _originatorPawn = originatorPawn;
            _durationTicks = durationTicks;
            _color = color;
            _intelligent = intelligent;
        }
        
        
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            if (!blockedByShield && !def.projectile.soundImpact.NullOrUndefined())
                def.projectile.soundImpact.PlayOneShot(SoundInfo.InMap((TargetInfo) (Thing) this));

            HashSet<NaniteCloud> cloudsInBatch = new HashSet<NaniteCloud>();
            NaniteCloud center = null;
            
            foreach (IntVec3 cell in _cells)
            {
                if (cell.InBounds(Map))
                {
                    if (cell == Position)
                    {
                        center = DoImpact(hitThing, cell, ref cloudsInBatch);
                    }
                    else
                    {
                        DoImpact(hitThing, cell, ref cloudsInBatch);
                    }
                }
            }
            
            if (center != null)
            {
                foreach (NaniteCloud cloud in cloudsInBatch)
                {
                    if (cloud == center)
                    {
                        cloud.isCenter = true;
                    }
                    else
                    {
                        cloud.center = center;
                    }
                    cloud.peers = cloudsInBatch.Where(naniteCloud => naniteCloud != cloud).ToHashSet();
                }
            }
            
            
            base.Impact(hitThing, blockedByShield);
        }

        private NaniteCloud DoImpact(Thing hitThing, IntVec3 cell, ref HashSet<NaniteCloud> cloudsInBatch)
        {
            NaniteCloud cloud = (NaniteCloud)GenSpawn.Spawn(_naniteCloudDef, cell, Map);
            cloud.ConfigureNaniteCloud(_durationTicks, _color, _inflictionHediff, _originatorPawn, _intelligent);
            
            cloudsInBatch.Add(cloud);
            return cloud;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (materialResolved == null)
                materialResolved = def.DrawMatSingle;
            base.DrawAt(drawLoc, flip);
        }
        
    }
}