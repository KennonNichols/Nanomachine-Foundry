using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NanomachineFoundry
{
    public class HediffCompProperties_BreedingHediff : HediffCompProperties
    {
        public float colorChange;
        public int tickDelay;
        public float breedAmount;
        public Color hairColorGoal;
        public Color skinColorGoal;
        public Color eyeColorGoal;
        public NaniteDef naniteType;
    }




    public class HediffCompProperties_ArchitePower: HediffCompProperties_BreedingHediff
    {
        public HediffCompProperties_ArchitePower()
        {
            compClass = typeof(HediffComp_ArchitePower);
        }
    }
    
    public class HediffCompProperties_BionanitePower : HediffCompProperties_BreedingHediff
    {
        public int progressionPerStage = 200;
        public int maxStage = 3;
        public SimpleCurve breakCurve = new SimpleCurve( new List<CurvePoint> ()
            {
                new CurvePoint(0f, 0f),
                new CurvePoint(1f, 0.01f),
                new CurvePoint(2f, 0.04f),
                new CurvePoint(3f, 0.3f)
            });
        public Dictionary<string, int> mentalBreaksWeighted = new Dictionary<string, int>()
        {
            { "DarkVisions", 4 },
            { "InsaneRamblings", 4 },
            { "TerrifyingHallucinations", 4 },
            { "CorpseObsession", 2 },
            { "EntityLiberator", 1 }
        };

        public HediffCompProperties_BionanitePower()
        {
            compClass = typeof(HediffComp_BionanitePower);
        }
    }
}
