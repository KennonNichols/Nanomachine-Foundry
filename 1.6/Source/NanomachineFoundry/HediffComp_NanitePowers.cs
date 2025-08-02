using NanomachineFoundry.Utils;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NanomachineFoundry
{
    public abstract class BreedingHediff : HediffComp
    {
        protected abstract HediffCompProperties_BreedingHediff Props { get; }
        protected Color _baseEyeColor = new Color(0, 0, 0);
        private bool _hasStarted;

        public override void CompPostTick(ref float severityAdjustment)
        {
            OnTick();
            if (!Pawn.IsHashIntervalTick(Props.tickDelay)) return;
            OnProcessTick();
            NaniteTracker_Pawn tracker = Pawn.GetNaniteTracker();
            if (tracker.TryChangeNanitesLevel(Props.naniteType, Props.breedAmount, true))
            {
                OnBreedCreated();
            }
            ModifyHairAndSkin();
            ModifyEyes();
            Pawn.Drawer.renderer.SetAllGraphicsDirty();
        }

        private void OnTick()
        {
            if (_hasStarted) return;
            _hasStarted = true;
            ModifyEyes();
            Pawn.Drawer.renderer.SetAllGraphicsDirty();
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref _baseEyeColor, "thnmf_baseEyeColor");
        }

        protected virtual void OnProcessTick()
        {
        }
        protected virtual void OnBreedCreated() { }

        private void ModifyHairAndSkin()
        {
            Pawn.story.HairColor = Color.Lerp(Pawn.story.HairColor, Props.hairColorGoal, Props.colorChange);
            Pawn.story.skinColorOverride = Color.Lerp(Pawn.story.SkinColor, Props.skinColorGoal, Props.colorChange);
        }

        private void ModifyEyes()
        {
            _baseEyeColor = Color.Lerp(_baseEyeColor, Props.eyeColorGoal, Props.colorChange);

            //Why didn't you give me your render tree, Ludeon?
            PawnRenderNode[] nodes = Pawn?.Drawer?.renderer?.renderTree?.rootNode?.children;
            if (nodes == null) return;
            foreach (PawnRenderNode node in nodes)
            {
                if (node.GetType() != typeof(PawnRenderNode_Head)) continue;
                foreach (PawnRenderNode nodeJr in node.children)
                {
                    if (nodeJr.Props is PawnRenderNodeProperties_EyeColored propertiesEyeColored)
                    {
                        propertiesEyeColored.color = _baseEyeColor;
                    }
                }
            }
        }
    }


    public class HediffComp_BionanitePower : BreedingHediff
    {
        
        private int _progressionAmount;

        public void Progress(int amount)
        {
            _progressionAmount += amount;
        }
        
        private float Stage => (float)_progressionAmount / DerivedProps.progressionPerStage;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref _progressionAmount, "thnmf_progression");
        }

        public override string CompDebugString()
        {
            return $"Progression of madness: {_progressionAmount}";
        }

        private HediffCompProperties_BionanitePower DerivedProps => (HediffCompProperties_BionanitePower)props;

        protected override HediffCompProperties_BreedingHediff Props => DerivedProps;

        protected override void OnProcessTick()
        {
            base.OnProcessTick();
            DoBreaker();
        }

        protected override void OnBreedCreated()
        {
            if (Stage < DerivedProps.maxStage)
            {
                _progressionAmount += 1;
            }
        }

        private void DoBreaker()
        {
            //Random increasing chance
            if (!Rand.Chance(DerivedProps.breakCurve.Evaluate(Stage))) return;
            //If we aren't inhumanized
            if (Pawn.health.hediffSet.HasHediff(HediffDefOf.Inhumanized)) return;
            //If we are in the final stage, the break is inhumanization
            if (Stage >= DerivedProps.maxStage)
            {
                BreakPawn("HumanityBreak", "THNMF.BionaniteInhumanize".Translate());
            }
            else if (DerivedProps.mentalBreaksWeighted.TryRandomElementByWeight(kvp => kvp.Value, out KeyValuePair<string, int> result))
            {
                BreakPawn(result.Key, string.Format("THNMF.BionaniteBreak".Translate(), Pawn.Possessive()));
            }
        }

        private void BreakPawn(string name, string cause)
        {
            DefDatabase<MentalBreakDef>.GetNamed(name).Worker.TryStart(Pawn, cause, false);
        }
    }


    public class HediffComp_ArchitePower : BreedingHediff
    {
        public void SetColorsToMax()
        {
            Pawn.story.HairColor = Props.hairColorGoal;
            Pawn.story.skinColorOverride = Props.skinColorGoal;
            _baseEyeColor = Props.eyeColorGoal;

            //Why didn't you give me your render tree, Ludeon?
            PawnRenderNode[] nodes = Pawn?.Drawer?.renderer?.renderTree?.rootNode?.children;
            if (nodes == null) return;
            foreach (PawnRenderNode node in nodes)
            {
                if (node.GetType() != typeof(PawnRenderNode_Head)) continue;
                foreach (PawnRenderNode nodeJr in node.children)
                {
                    if (nodeJr.Props is PawnRenderNodeProperties_EyeColored propertiesEyeColored)
                    {
                        propertiesEyeColored.color = _baseEyeColor;
                    }
                }
            }
        }
        
        private HediffCompProperties_ArchitePower DerivedProps => (HediffCompProperties_ArchitePower)props;
        protected override HediffCompProperties_BreedingHediff Props => DerivedProps;
    }
}
