using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using JetBrains.Annotations;
using NanomachineFoundry.NaniteModifications.ModificationWorkers;
using NanomachineFoundry.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace NanomachineFoundry.NaniteModifications
{
    public class NaniteModificationDef : Def
    {
        public NaniteEffectCategoryDef categoryDef;
        
        public List<ModificationScalingDetail> modScalingDetails;
        
        public Type workerClass;
        public bool ticker = true;
        public AbilityDef ability;
        public HediffDef hediff;
        
        public bool abilityGiver => ability != null;
        public bool hediffGiver => hediff != null;
        [NoTranslate]
        public string iconPath;
        public bool forcedStatAffector = false;
        
        public List<ModificationScalingStatAffector> scalingStatOffsets;
        public List<ModificationScalingStatAffector> scalingStatFactors;
        public List<ModificationScalingCapacityAffector> scalingCapacityOffsets;
			
        public bool statAffector => forcedStatAffector || scalingStatFactors != null || scalingStatOffsets != null;
        public bool capAffector => scalingCapacityOffsets != null;
        
        
        
        protected virtual Texture2D icon { get; private set; }
        
        public string FullDescription(int level)  {
            StringBuilder builder = new StringBuilder(description);
            builder.AppendLine().AppendLine().Append(NmfUtils.GetScalingDetails(level, this));
            builder.AppendLine().Append(string.Format(
                "THNMF.ModificationInstallationDescription".Translate(), categoryDef.label));
            return builder.ToString();
        }


        public Texture2D getIcon()
        {
            return icon;
        }
        
        public ModificationWorker GetNewWorkerInstance(Pawn pawn) => (ModificationWorker)Activator.CreateInstance(workerClass, this, pawn);
        
        public override void PostLoad()
        {
            base.PostLoad();
            if (iconPath == null)
            {
                icon = BaseContent.BadTex;
            }
            else
            {
                LongEventHandler.ExecuteWhenFinished(delegate
                {
                    try
                    {
                        icon = ContentFinder<Texture2D>.Get(iconPath);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        icon = BaseContent.BadTex;
                        throw;
                    }
                });
            }
        }

        public override IEnumerable<string> ConfigErrors()
        {
            if (categoryDef == null)
            {
                yield return "Effect category cannot be null";
            }
            if (modScalingDetails == null)
            {
                yield return "Scaling details cannot be null";
            }
            else
            {
                foreach (string error in modScalingDetails.SelectMany(scalingDetail => scalingDetail.ConfigErrors()))
                {
                    yield return error;
                }
            }
        }
        
        public ModificationWorker ConfigWorkerInstance() => _configWorkerInstance ??= (ModificationWorker)Activator.CreateInstance(workerClass, this, null);
        private ModificationWorker _configWorkerInstance;


        public float TryGetScaledValueSlow(float level, string key)
        {
            foreach (ModificationScalingDetail scalingDetail in modScalingDetails)
            {
                if (scalingDetail.accessKey == key)
                {
                    return scalingDetail.GetScaledValueSlow(level);
                }
            }
            Log.Error($"Tried to get a scaled value: {key} on a modification ({label}) that doesn't have it.");
            return 1;
        }

        //Generate auto config if any aspect has a config generator
        public bool AutoConfigurable => modScalingDetails.Any(detail => detail.configGenerator != null);

        public void ResetAutoConfig()
        {
            if (AutoConfigurable)
            {
                modScalingDetails.Where(detail => detail.configGenerator != null).Do(detail => detail.configGenerator.SelectedValue = detail.configGenerator.defaultValue);
            }
        }

        public void DoAutoConfigMenu(Listing_Standard listingStandard, ref bool anyChange)
        {
            if (!AutoConfigurable) return;
            foreach (ModificationScalingDetail scalingDetail in modScalingDetails.Where(detail => detail.configGenerator != null))
            {
                scalingDetail.configGenerator.DoConfigMenu(listingStandard, ref anyChange);
            }
        }

        public void ScribeAllConfigs(ModificationWorker source)
        {
            if (!AutoConfigurable) return;
            foreach (ModificationScalingDetail scalingDetail in modScalingDetails.Where(detail => detail.configGenerator != null))
            {
                scalingDetail.configGenerator.ScribeConfigs(source);
            }
        }
    }




    public class ConfigGenerator
    {
        public ConfigType type = ConfigType.Multiplier;
        public float minValue;
        public float maxValue;
        public float defaultValue;
        public string label;
        [CanBeNull] public string tooltip = null;
        public string displayFormat;
        public bool displayAsPercent = true;

        
        public float SelectedValue;

        public void ScribeConfigs(ModificationWorker source)
        {
            Scribe_Values.Look(ref SelectedValue, source.ConfigID("tendChanceMultiplier"), defaultValue);
        }

        public void DoConfigMenu(Listing_Standard listingStandard, ref bool anyChange)
        {
            string displayValue = displayAsPercent ? SelectedValue.ToStringPercent() : SelectedValue.ToString(displayFormat);
            float newValue = listingStandard.SliderLabeled(label.Formatted(displayValue), SelectedValue, minValue, maxValue, tooltip: tooltip);
            //Return true if it was changed
            if (Mathf.Approximately(newValue, SelectedValue)) return;
            SelectedValue = newValue;
            anyChange = true;
        }
    }

    public enum ConfigType
    {
        Multiplier,
        Offset
    }
    
    
    
    
    
    
    public abstract class ModificationScalingAffector
    {
        private float amount;
        private ModificationScalingStyle scalingStyle;
        private float halfLife;
        
        public float getScaledValue(float level)
        {
            return scalingStyle switch
            {
                ModificationScalingStyle.Linear => (halfLife > 0
                    ? NmfUtils.DiminishingReturnsCalculator.CalculateSumDiminishingReturnsFloatSlow(level, halfLife)
                    : level) * amount,
                ModificationScalingStyle.Constant => amount,
                ModificationScalingStyle.Bonus => amount,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
    
    public class ModificationScalingStatAffector: ModificationScalingAffector
    {
        public StatDef affectedStat;
    }
    public class ModificationScalingCapacityAffector: ModificationScalingAffector
    {
        public PawnCapacityDef affectedCapacity;
    }
    
    public enum ModificationScalingStyle
    {
        Linear,
        Constant,
        Bonus,
        LinearWithBase
    }
    
    public enum ModificationScalingReportStage
    {
        Linear,
        Constant,
        Bonus
    }
    
    public class ModificationScalingDetail
    {
        public string label;
        public float amount;
        public int halfLife;
        private float baseValue;
        private string scalingOperatorSymbol;
        private string baseOperatorSymbol = "?";
        public ModificationScalingStyle scalingStyle;

        /// <summary>
        /// Saves the value and retrieves it via code. A single mod should not have more than one scaling detail with the same access key.
        /// </summary>
        /// <returns></returns>
        public string accessKey;

        public ConfigGenerator configGenerator;

        public float GetScaledValueSlow(float level)
        {
            return TweakValueByConfig(scalingStyle switch
            {
                ModificationScalingStyle.LinearWithBase => baseValue +
                                                           (halfLife > 0
                                                               ? NmfUtils.DiminishingReturnsCalculator
                                                                   .CalculateSumDiminishingReturnsFloatSlow(level,
                                                                       halfLife)
                                                               : level) * amount,
                ModificationScalingStyle.Linear => (halfLife > 0
                    ? NmfUtils.DiminishingReturnsCalculator.CalculateSumDiminishingReturnsFloatSlow(level, halfLife)
                    : level) * amount,
                ModificationScalingStyle.Constant => amount,
                ModificationScalingStyle.Bonus => amount,
                _ => throw new ArgumentOutOfRangeException()
            });
        }
        
        
        public float TweakValueByConfig(float value)
        {
            if (configGenerator == null) return value;
            if (configGenerator.type == ConfigType.Offset) return value + configGenerator.SelectedValue;
            return value * configGenerator.SelectedValue;
        }

        public string GetScaledStringForStage(int level, ModificationScalingReportStage type)
        {
            if (!ShouldShowInStage(type)) return "";
            
            
            if (level == 0)
            {
                return GetBaseScalingReport(type);
            }


            if (scalingStyle == ModificationScalingStyle.LinearWithBase &&
                type == ModificationScalingReportStage.Linear) return "";

            var operatorSymbol = baseOperatorSymbol == "?" ? scalingOperatorSymbol : baseOperatorSymbol;

            return operatorSymbol + string.Format(label, TweakValueByConfig(scalingStyle switch
            {
                ModificationScalingStyle.LinearWithBase => baseValue + (halfLife > 0 ? NmfUtils.DiminishingReturnsCalculator.CalculateSumDiminishingReturns(level, halfLife) : level) * amount,
                ModificationScalingStyle.Linear => (halfLife > 0? NmfUtils.DiminishingReturnsCalculator.CalculateSumDiminishingReturns(level, halfLife): level) * amount,
                ModificationScalingStyle.Constant => amount,
                ModificationScalingStyle.Bonus => amount,
                _ => throw new ArgumentOutOfRangeException()
            }).ToString("0.00"));
        }

        private string GetBaseScalingReport(ModificationScalingReportStage stage)
        {
            if (scalingStyle == ModificationScalingStyle.LinearWithBase)
            {
                if (stage == ModificationScalingReportStage.Constant)
                {
                    return (baseOperatorSymbol != "?" ? baseOperatorSymbol : "") + string.Format(label, baseValue);
                }
                return scalingOperatorSymbol + string.Format(label, TweakValueByConfig(amount)) + (halfLife > 0 ? string.Format("THNMF.DiminishingReturns".Translate(), halfLife): "");
            }
            return scalingOperatorSymbol + string.Format(label, TweakValueByConfig(amount)) + (halfLife > 0 ? string.Format("THNMF.DiminishingReturns".Translate(), halfLife): "");
        }
        
        private bool ShouldShowInStage(ModificationScalingReportStage stage)
        {
            return stage switch
            {
                ModificationScalingReportStage.Linear => scalingStyle == ModificationScalingStyle.LinearWithBase || scalingStyle == ModificationScalingStyle.Linear,
                ModificationScalingReportStage.Constant => scalingStyle == ModificationScalingStyle.LinearWithBase || scalingStyle == ModificationScalingStyle.Constant,
                ModificationScalingReportStage.Bonus => scalingStyle == ModificationScalingStyle.Bonus,
                _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, null)
            };
        }

        public IEnumerable<string> ConfigErrors()
        {
            if (baseValue == 0 && scalingStyle == ModificationScalingStyle.LinearWithBase)
            {
                yield return "baseValue should only be defined when using \"LinearWithBase\" scalingStyle";
            }
        }
    }
}
