using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace NanomachineFoundry
{
    public class HediffCompProperties_ScaledVerbGiver: HediffCompProperties_VerbGiver
    {
        public float maxDamage;
        public float maxCooldown;
        public float minCooldown;
        public bool blockedByClothing;
        public SimpleCurve coverageCurve = new SimpleCurve
        {
            new CurvePoint(0, 0.1f),
            new CurvePoint(0.2f, 0.5f),
            new CurvePoint(0.6f, 0.8f),
            new CurvePoint(1, 1f),
        };
        
        public HediffCompProperties_ScaledVerbGiver() => compClass = typeof (HediffComp_ScaledVerbGiver);
        
        public override IEnumerable<string> ConfigErrors(HediffDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
            {
                yield return error;
            }

            if (maxDamage == 0)
            {
                yield return "max damage should not be zero";
            }
            if (maxCooldown == 0)
            {
                yield return "max cooldown should not be zero";
            }
            if (minCooldown == 0)
            {
                yield return "min cooldown should not be zero";
            }
        }
    }

    public class HediffComp_ScaledVerbGiver : HediffComp_VerbGiver
    {
        private HediffCompProperties_ScaledVerbGiver Props => (HediffCompProperties_ScaledVerbGiver)props;

        private float _savedSeverity;
        
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (Mathf.Approximately(_savedSeverity, parent.Severity)) return;
            _savedSeverity = parent.Severity;
            ScaleAllVerbs();
        }

        private void ScaleAllVerbs()
        {
            float damage = Props.maxDamage * parent.Severity;
            float cooldown = Mathf.Lerp(Props.maxCooldown, Props.minCooldown, parent.Severity);
            if (Props.blockedByClothing)
            {
                float cooldownMultiplier = Props.coverageCurve.Evaluate(BodyCoverage());
                cooldown *= cooldownMultiplier;
            }
            foreach (Tool tool in Props.tools)
            {
                tool.power = damage;
                tool.cooldownTime = cooldown;
            }
        }

        private float BodyCoverage()
        {
            //Legs, arms, torso, head
            return new[] { BodyPartGroupDefOf.Torso, BodyPartGroupDefOf.FullHead, BodyPartGroupDefOf.Legs, BodyPartGroupDefOf.RightHand, BodyPartGroupDefOf.LeftHand }.Where(bodyPartGroup => parent.pawn.apparel.BodyPartGroupIsCovered(bodyPartGroup)).Sum(bodyPartGroup => 0.2f);
        }
    }
}