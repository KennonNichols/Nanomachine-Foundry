using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using NanomachineFoundry.NaniteModifications;
using PipeSystem;
using Unity.Collections;
using UnityEngine;
using Verse;

namespace NanomachineFoundry.Utils
{
    public static class NmfUtils
	{
		
		public static string GetScalingDetails(int level, NaniteModificationDef def)
		{
			bool started = false;

			StringBuilder builder = new StringBuilder();

			if (level > 0)
			{
				builder.Append("THNMF.AtCurrentLevel".Translate());
			}
			
			AddNoteItem(ref started, builder, "THNMF.ScalingFlat", ModificationScalingReportStage.Constant, level, def);
			AddNoteItem(ref started, builder, "THNMF.ScalingPerLevel", ModificationScalingReportStage.Linear, level, def);
			AddNoteItem(ref started, builder, "THNMF.ScalingBonus", ModificationScalingReportStage.Bonus, level, def);
			
			return builder.ToString();
		}
		
		private static void AddNoteItem(ref bool started, StringBuilder builder, string labelKey, ModificationScalingReportStage type, int level, NaniteModificationDef def)
		{
			string[] notes = def.modScalingDetails
				//.Where(detail => detail.scalingStyle == type)
				//.Select(detail => detail.GetScaledString(level)).ToArray();
				.Select(detail => detail.GetScaledStringForStage(level, type)).ToArray();
			if (notes.All(s => s == "")) return;
			if (started && level == 0)
			{
				builder.AppendLine();
			}
			StringBuilder noteBuilder = new StringBuilder();
			notes.Do(s =>
			{
				if (s != "") noteBuilder.AppendLine(s);
			});
			if (level == 0)
			{
				builder.Append(labelKey.Translate());
			}
			builder.Append(noteBuilder);
			started = true;
		}
        
        
		
		
		
		
        
        
        
		
		public static class DiminishingReturnsCalculator
        {
	        private static readonly Dictionary<(int, float), float> Cache = new Dictionary<(int, float), float>();
	        private static readonly HashSet<(int, float)> AccessedRecords = new HashSet<(int, float)>();

	        public static float CalculateSumDiminishingReturns(int levels, float halfLife)
	        {
		        if (Cache.TryGetValue((levels, halfLife), out float cachedResult))
		        {
			        AccessedRecords.Add((levels, halfLife));
			        return cachedResult;
		        }

		        double sum = 0;
		        for (int i = 0; i < levels; i++)
		        {
			        sum += 1 / (Math.Pow(2, i / halfLife));
		        }
		        float result = (float)sum;
		        Cache[(levels, halfLife)] = result;
		        AccessedRecords.Add((levels, halfLife));
		        return result;
	        }
	        
	        /*
	         *	float halfLifeSquared = halfLife * halfLife;
		        double invSum = 0;
		        for (int i = 0; i < levels; i++)
		        {
			        invSum += i*i;
		        }
		        float result = (float)(1 / (invSum / halfLifeSquared));
	         *
	         */

	        public static void CleanCache()
	        {
		        var keysToRemove = (from record in Cache where !AccessedRecords.Contains(record.Key) select record.Key).ToList();

		        foreach (var key in keysToRemove)
		        {
			        Cache.Remove(key);
		        }

		        AccessedRecords.Clear();
	        }
	        
	        public static float CalculateSumDiminishingReturnsFloatSlow(float levels, float halfLife)
	        {
		        int currentLevel = 0;
		        
		        double sum = 0;

		        
		        while (levels > 0)
		        {
			        sum += 1 / (Math.Pow(2,currentLevel / halfLife));
			        levels--;
			        currentLevel++;
		        }

		        if (levels != 0)
		        {
			        sum += levels / (Math.Pow(2, currentLevel / halfLife));
		        }
		        
		        return (float)sum;
	        }
        }
	}

    [StaticConstructorOnStartup]
    public static class NmfMenuUtils
	{
		//private static Color naniteBarBg = new Color(.33f, .66f, .66f);
		private static readonly Color NaniteBarBg = new Color(.06f, .12f, .12f);
		private static Vector2 _scrollPos;
		private static Vector2 _operationScrollPos;
		private const float NodeMinSize = 20f;
		private const float NodeMaxSize = 30f;

		private static readonly Dictionary<PawnNaniteFillMode, string> FillModeNames =
			new Dictionary<PawnNaniteFillMode, string>()
			{
				{ PawnNaniteFillMode.ForceNever, "THNMF.NeverForce".Translate() },
				{ PawnNaniteFillMode.ForceAlways, "THNMF.AlwaysForce".Translate() },
				{ PawnNaniteFillMode.ForceRenewable, "THNMF.Renewables".Translate() }
			};
		
		public static void DoTweakConfigMenu(Pawn pawn, ref Dictionary<NaniteDef, int> tempConfigs, Rect bounds, Action reconfigureAction, bool devMode = false)
		{
			NaniteTracker_Pawn tracker = pawn.GetNaniteTracker();


			Rect modContentRect = bounds.TopHalf();
			Rect naniteContentRect = bounds.BottomHalf();

			Rect naniteBarRect = naniteContentRect.TopPart(.3f);
			Rect naniteTweakRect = naniteContentRect.BottomPart(.7f);


			Rect naniteValueBarRect = naniteBarRect.TopHalf();
			Rect naniteConfigBarRect = naniteBarRect.BottomHalf();
			if (tracker.NaniteCapacity > 0)
			{
				DoNaniteBar(naniteValueBarRect, tracker, pawn);
				DoNaniteConfigBar(naniteConfigBarRect, tempConfigs, tracker.NaniteCapacity);
				DoNaniteConfigTweakMenu(naniteTweakRect, ref tempConfigs, tracker, pawn, devMode, reconfigureAction);
			}
			
			DoTweakModMenu(pawn, modContentRect, true);
			DoFillingOptions(modContentRect, tracker); 

			//Capacity tweak area:
			if (!devMode) return;
			Rect capacityTweakArea = new Rect(naniteTweakRect.xMax - 170f, naniteTweakRect.yMax - 50f, 150f, 30f);
			string idkRef = tracker.NaniteCapacity.ToString();
			Widgets.TextFieldNumericLabeled(capacityTweakArea, "Cap:", ref tracker.CapacityReference(), ref idkRef, 0, 40);
		}

		private static readonly Texture2D GrayoutTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.5f, 0.5f, 0.5f, 0.5f));


		private static Vector2 _scrollModTweak;

		private static void DoTweakModMenu(Pawn pawn, Rect parentBounds, bool tweak)
		{
			float iconSize = parentBounds.height / 2;
			Rect bounds = parentBounds.LeftPartPixels(iconSize * 4);
			
			Widgets.DrawAltRect(bounds);
			
			NaniteTracker_Pawn tracker = pawn.GetNaniteTracker();

			if (tracker.AllowedModifications.Any())
			{
				int columns = (tracker.AllowedModifications.Count + 1) / 2;

				float width = columns * iconSize;

				bool scrolling = width > bounds.width;

				if (scrolling)
				{
					iconSize -= 10;
					Widgets.BeginScrollView(bounds, ref _scrollModTweak, new Rect(bounds.xMin, bounds.yMin, width, bounds.height).TopPartPixels(bounds.height - 20));
				}

				int x = 0;
				int y = 0;
				foreach (NaniteModificationDef modification in tracker.AllowedModifications)
				{
					Rect modArea = new Rect(bounds.xMin + x * iconSize, bounds.yMin + y * iconSize, iconSize, iconSize);

					ModAllocation allocation = tracker.GetAllocation(modification);
					
					TooltipHandler.TipRegion(modArea, modification.FullDescription(allocation?.Amount ?? 0));

					if (allocation != null)
					{
						int allocedAmount = allocation.Amount;
						if (allocedAmount > 0)
						{
							//Assigned nanite level report
							float trueAmount = tracker.RelativeNaniteLevels.GetWithFallback(allocation.Def) * allocedAmount;
							TooltipHandler.TipRegion(modArea,
								Mathf.Approximately(trueAmount, allocedAmount)
									? "THNMF.ModAssignmentReport".Translate(allocedAmount, allocation.Def.plural)
									: "THNMF.ModAssignmentReportShort".Translate(trueAmount, allocedAmount,
										allocation.Def.plural));
						}
					}
					
					TooltipHandler.TipRegion(modArea, modification.LabelCap);

					Rect drawArea = modArea.ContractedBy(5f);
					
					
					GenUI.DrawTextureWithMaterial(drawArea, Command.BGTex, TexUI.GrayscaleGUI);
					
					if (tweak)
					{
						if (Widgets.ButtonImage(drawArea, modification.getIcon()))
						{
							allocation ??= new ModAllocation(tracker.FirstNaniteTypeOfCategory(modification.categoryDef), 0);
							if (allocation.Def == null)
							{
								allocation = new ModAllocation(tracker.FirstNaniteTypeOfCategory(modification.categoryDef), 0);
							}
							Find.WindowStack.Add(new WindowModificationConfiguration(tracker, modification, allocation.Amount, allocation.Def));
						}
					}
					else
					{
						GUI.DrawTexture(drawArea, modification.getIcon());
					}

					if (!tracker.AllActiveModifications.Contains(modification))
					{
						GUI.DrawTexture(drawArea, GrayoutTex);
					}
					
					y++;
					if (y > 1)
					{
						y = 0;
						x++;
					}
				}

				if (scrolling)
				{
					Widgets.EndScrollView();
				}
			}
			else
			{
				Text.CurFontStyle.fontSize = (int)(bounds.height / 12);
				Text.Anchor = TextAnchor.MiddleCenter;
				Widgets.Label(bounds, "THNMF.NoModificationsInstalled".Translate());
				Text.Anchor = TextAnchor.UpperLeft;
				Text.CurFontStyle.fontSize = 0;
			}
		}

		public static void DoReadonlyConfigMenu(Pawn pawn, Rect bounds)
		{
			NaniteTracker_Pawn tracker = pawn.GetNaniteTracker();



			//Mods
			//Widgets.Label(val.TopPartPixels.TopPartPixels(labelHeight), "THNMF.ModsLabel".Translate());

			/*Rect modContentRect = val.TopHalf().BottomPartPixels(val.height - labelHeight);


			//Nanites
			Widgets.Label(val.BottomHalf().TopPartPixels(labelHeight), "THNMF.NanitesLabel".Translate());
			Rect naniteContentRect = val.BottomHalf().BottomPartPixels(val.height - labelHeight);
			Rect naniteCounterRect = naniteContentRect.TopPart(.3f);
			Rect naniteAllocatorRect = naniteContentRect.TopPart(.7f);
			DoCapacities(naniteCounterRect, tracker.NaniteConfigRatios, tracker.NaniteLevels, tracker.NaniteCapacity);*/


			Rect modContentRect = bounds.TopPart(.8f);
			DoTweakModMenu(pawn, modContentRect, false);
			
			
			Rect naniteBarRect = bounds.BottomPart(.2f);
			DoNaniteBar(naniteBarRect, tracker, pawn);
			
			DoFillingOptions(modContentRect, tracker);
			
		}


		/// <summary>
		/// Draw's fillMode dropdown and autoRefill check box
		/// </summary>
		private static void DoFillingOptions(Rect parentBounds, NaniteTracker_Pawn tracker)
		{
			//Fill mode dropdown
			Rect dropdownArea = new Rect(parentBounds.xMax - 156f, parentBounds.yMax - 65f, 154f, 60f);
			TooltipHandler.TipRegion(dropdownArea, "THNMF.FillModeTip".Translate());
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(dropdownArea.TopHalf(), "THNMF.FillModeLabel".Translate());
			Text.Anchor = TextAnchor.UpperLeft;
			Widgets.Dropdown(dropdownArea.BottomHalf(), tracker, t => t.FillMode, FillModeDropdown, FillModeNames.GetValueSafe(tracker.FillMode));
		}
		
		private static IEnumerable<Widgets.DropdownMenuElement<PawnNaniteFillMode>> FillModeDropdown(NaniteTracker_Pawn tracker)
		{
			foreach (PawnNaniteFillMode fillMode in Enum.GetValues(typeof(PawnNaniteFillMode)))
			{
				yield return new Widgets.DropdownMenuElement<PawnNaniteFillMode>()
				{
					option = new FloatMenuOption(FillModeNames.GetValueSafe(fillMode), (() => tracker.FillMode = fillMode)),
					payload = fillMode
				};
			}
		}

		private static void DoNaniteBar(Rect bounds, NaniteTracker_Pawn tracker, Pawn pawn)
		{
			const float margin = 4f;
			//Outline
			Widgets.DrawRectFast(bounds, Color.black);
			Rect colorSection = bounds.ContractedBy(margin);

			float width = colorSection.width;
			float currentX = colorSection.x;
			Widgets.DrawRectFast(colorSection, NaniteBarBg);

			if (tracker.ConfigRemaining > 0)
			{
				TooltipHandler.TipRegion(colorSection, new TipSignal(string.Format("THNMF.NanitesLeftLabel".Translate(), pawn.Name.ToStringShort, tracker.ConfigRemaining)));
			}


			//Log.Message("Total width is: " + width);
			foreach (NaniteDef naniteType in DefDatabase<NaniteDef>.AllDefs)
			{
				int configAmount = tracker.NaniteConfigRatios.GetWithFallback(naniteType);
				float valueAmount = tracker.NaniteLevels.GetWithFallback(naniteType);


				//Draw intended and true area
				float configgedSegmentWidth = width * (configAmount / (float)tracker.NaniteCapacity);
				float trueSegmentWidth = width * (valueAmount / tracker.NaniteCapacity);

				Rect naniteTypeArea = new Rect(currentX, colorSection.y, configgedSegmentWidth, colorSection.height);

				Widgets.DrawRectFast(naniteTypeArea, naniteType.altColor);
				Widgets.DrawRectFast(new Rect(currentX, colorSection.y, trueSegmentWidth, colorSection.height), naniteType.color);
				currentX += configgedSegmentWidth;


				Widgets.DrawHighlightIfMouseover(naniteTypeArea);
				//Tooltip of nanite report
				TooltipHandler.TipRegion(naniteTypeArea, new TipSignal($"{string.Format("THNMF.NanitesQuantityLabel".Translate(), valueAmount.ToString("0.0"), configAmount, naniteType.label, naniteType.pawnReferrer)}\n\n{naniteType.NaniteReport}"));

				
				//Log.Message(naniteType.label + "spans from " + currentX + " to " + currentX + configgedSegmentWidth);
				//Log.Message("Current x is: " + currentX + ", Last segment length was: " + configgedSegmentWidth);
			}

			for (int i = 1; i < tracker.NaniteCapacity; i++)
			{
				Widgets.DrawRectFast(new Rect(colorSection.x + i * colorSection.width / tracker.NaniteCapacity - margin / 2, colorSection.yMin, margin, colorSection.height), Color.black);
			}
		}
		
		private static void DoNaniteConfigBar(Rect bounds, Dictionary<NaniteDef, int> configs, int max)
		{


			const float margin = 4f;
			//Outline
			Widgets.DrawRectFast(bounds, Color.black);
			Rect colorSection = bounds.ContractedBy(margin);
			
			float width = colorSection.width;
			float currentX = colorSection.x;
			Widgets.DrawRectFast(colorSection, NaniteBarBg);
			//Log.Message("Total width is: " + width);
			foreach (NaniteDef naniteType in DefDatabase<NaniteDef>.AllDefs)
			{
				//Draw intended and true area
				float configgedSegmentWidth = width * (configs.GetWithFallback(naniteType) / (float)max);
				Widgets.DrawRectFast(new Rect(currentX, colorSection.y, configgedSegmentWidth, colorSection.height), naniteType.color);
				currentX += configgedSegmentWidth;
				//Log.Message(naniteType.label + "spans from " + currentX + " to " + currentX + configgedSegmentWidth);
				//Log.Message("Current x is: " + currentX + ", Last segment length was: " + configgedSegmentWidth);
			}
		}

		private static void DoNaniteConfigTweakMenu(Rect bounds, ref Dictionary<NaniteDef, int> tempConfigs, NaniteTracker_Pawn tracker, Pawn pawn, bool devMode, Action reconfigureAction)
        {
			int sumExistingValues = 0;
			int highestValue = 0;
			if (tempConfigs.Any())
			{
				sumExistingValues = tempConfigs.Values.Sum();
				highestValue = tempConfigs.Values.Max();
			}

			const float labelWidth = 150f;
			int highestLevelPossible = highestValue + tracker.NaniteCapacity - sumExistingValues;
			IEnumerable<NaniteDef> types = tracker.GetConfigurableNaniteTypes(devMode);
			NaniteDef[] naniteDefs = types as NaniteDef[] ?? types.ToArray();
			int levelCounts = naniteDefs.Count();
			float nodeSize = Mathf.Min(Mathf.Max((bounds.width - labelWidth) / (highestLevelPossible + 1), NodeMinSize), NodeMaxSize);


			float height = nodeSize * (levelCounts + 1);
			//Account for 2 extra nodes in dev mode for the buttons, and 1 extra node always for the "zero" node
			float width = labelWidth + nodeSize * (highestLevelPossible + (devMode? 3:1));

			bool scrolling = false;
			
			Rect menuBounds = new Rect(bounds);

			if (height > bounds.height || width > bounds.width)
			{
				scrolling = true;
				Widgets.BeginScrollView(bounds, ref _scrollPos, new Rect(bounds.xMin, bounds.yMin, width, height));
				//Give room for scrollbar
				menuBounds = bounds.LeftPartPixels(bounds.width - 10f).TopPartPixels(bounds.height - 10f);
			}




			int finalI = 0;

			for (int i = 0; i < levelCounts; i++)
            {
				NaniteDef naniteType = naniteDefs.ElementAt(i);

				Rect labelRect = new Rect(menuBounds.xMin + 4f, menuBounds.yMin + nodeSize * i, labelWidth, nodeSize);
				if (tracker.IsNaniteTypeAllowed(naniteType))
                {
					if (tracker.NaniteConfigRatios.GetWithFallback(naniteType) == 0 && devMode)
                    {
						if (Widgets.ButtonText(labelRect.ContractedBy(4f), string.Format("THNMF.NaniteCapacityLabel".Translate(), naniteType.label)))
                        {
							tracker.DisallowNaniteType(naniteType);
                        }
						TooltipHandler.TipRegion(labelRect, new TipSignal(string.Format("THNMF.DevDisableNaniteType".Translate(), pawn.Name.ToStringShort, naniteType.label)));
					}
					else
                    {
						Widgets.Label(labelRect, string.Format("THNMF.NaniteCapacityLabel".Translate(), naniteType.label));
						TooltipHandler.TipRegion(labelRect, new TipSignal(naniteType.NaniteReport));
					}
				}
				else
				{
					TooltipHandler.TipRegion(labelRect, new TipSignal(string.Format("THNMF.DevEnabledNaniteType".Translate(), pawn.Possessive(), naniteType.label)));
					Widgets.Label(labelRect, new TaggedString($"<color=#de001e>{string.Format("THNMF.NaniteCapacityLabel".Translate(), naniteType.label)}</color>"));
				}


				int configCount = tempConfigs.GetWithFallback(naniteType);
				
				int nodeCount = configCount + tracker.NaniteCapacity - sumExistingValues + 1;
				int barStartNode = 0;
				int barEndNode = -1;
				int buttonEffectOffset = 0;

				if (devMode)
                {
					barStartNode = 1;
					nodeCount += 2;
					barEndNode = nodeCount - 1;
					buttonEffectOffset = -1;
                }

				for (int x = 0; x < nodeCount; x++)
                {
					Rect nodeArea = new Rect(menuBounds.xMin + labelWidth + nodeSize * (x), menuBounds.yMin + nodeSize * i, nodeSize, nodeSize).ContractedBy(2f);
					Color drawColor;

					if (x < barStartNode)
                    {
						//Draw - button
						if (Widgets.ButtonText(nodeArea, "-"))
						{
							tracker.TryChangeNanitesLevel(naniteType, -0.25f, true);
						}
						TooltipHandler.TipRegion(nodeArea, new TipSignal("Dev: -.25"));
						continue;
                    }
					else if (x == barStartNode)
                    {
						drawColor = NaniteBarBg;
                    }
					else if (x == barEndNode)
					{
						//Draw + button
						if (Widgets.ButtonText(nodeArea, "+"))
						{
							tracker.TryChangeNanitesLevel(naniteType, 0.25f, true);
						}
						TooltipHandler.TipRegion(nodeArea, new TipSignal("Dev: +.25"));
						continue;
					}
					else
					{
						drawColor = x + buttonEffectOffset > configCount ? naniteType.altColor : naniteType.color;
					}







					if (Widgets.ButtonInvisible(nodeArea)) {
						tempConfigs.SetOrAdd(naniteType, x + buttonEffectOffset);
                    }
					Widgets.DrawRectFast(nodeArea, drawColor);
					Widgets.DrawHighlightIfMouseover(nodeArea);
					TooltipHandler.TipRegion(nodeArea, new TipSignal(string.Format("THNMF.SetNaniteConfigLevelLabel".Translate(), naniteType.label, x + buttonEffectOffset)));
				}

				finalI = i;
			}

			Rect buttonBar = new Rect(menuBounds.xMin + 2f, menuBounds.yMin + nodeSize * (finalI + 1), labelWidth, nodeSize);


			if (Widgets.ButtonText(buttonBar, "THNMF.Apply".Translate()))
			{
				PromptReconfigure(tempConfigs, tracker, reconfigureAction);
			}




			if (scrolling)
			{
				Widgets.EndScrollView();
			}
		}

		private static void PromptReconfigure(Dictionary<NaniteDef, int> tempConfigs, NaniteTracker_Pawn tracker, Action reconfigureAction)
        {
			IEnumerable<string> issues = GetReconfigureAlerts(tempConfigs, tracker);


			IEnumerable<string> enumerable = issues as string[] ?? issues.ToArray();
			if (enumerable.Any())
            {
	            Find.WindowStack.Add(new WindowReconfigurePrompt(enumerable, reconfigureAction));
            }
			else
			{
				tracker.SetConfiguration(tempConfigs);
			}
        }

		private static IEnumerable<string> GetReconfigureAlerts(Dictionary<NaniteDef, int> tempConfigs, NaniteTracker_Pawn tracker)
		{
			return from naniteType in tempConfigs.Keys where tempConfigs.GetWithFallback(naniteType) < tracker.NaniteLevels.GetWithFallback(naniteType) select string.Format((naniteType.renewable ? "THNMF.LoseNaniteWarning" : "THNMF.LoseNaniteWarningNonRenewable").Translate(), naniteType.label);
		}

		


		
		//Stuff for the menu

		//private static readonly float OperationHeight = 100f;
		private static readonly float OperationHeight = 75f;

		private static Color forbiddenColor = new Color(200, 0, 0);
		private static Color notAffordColor = new Color(200, 200, 0);
		
		public static void DoOperationMenu(Rect bounds, Pawn pawn, CompNaniteOperator naniteOperator, IEnumerable<NaniteOperationDef> operations, bool showDisabledOperations = true)
		{
			NaniteOperationDef[] naniteOperationDefs = operations as NaniteOperationDef[] ?? operations.ToArray();
			
			float height = (showDisabledOperations
				? naniteOperationDefs.Count()
				: naniteOperationDefs.Count(def => def.CanAdministerToPawn(pawn, out _))) * OperationHeight;
			
			bool scrolling = false;
			if (height > bounds.height)
			{
				scrolling = true;
				Rect trueRect = new Rect(bounds)
				{
					height = height,
					width = bounds.width * 0.9f
				};
				Widgets.BeginScrollView(bounds, ref _operationScrollPos, trueRect);
			}

			CompResource[] compResources = naniteOperator.Resources as CompResource[] ?? naniteOperator.Resources.ToArray();

			Rect opBound = bounds.TopPartPixels(OperationHeight).ContractedBy(2f).LeftPart(0.9f);
			float width = opBound.width;

			float starty = opBound.y + 2f;
			
			int i = 0;
			foreach (NaniteOperationDef operation in naniteOperationDefs)
			{
				bool canApply = operation.CanAdministerToPawn(pawn, out string issuesTag);
				
				if (!canApply && !showDisabledOperations) continue;
				
				bool canAfford = operation.CanAfford(compResources, out NaniteDef cheapestType);
				opBound.y = i * OperationHeight + starty;

				Rect iconRect = opBound.LeftPartPixels(OperationHeight);
				
				GenUI.DrawTextureWithMaterial(iconRect, Command.BGTex, TexUI.GrayscaleGUI);
				
				GUI.DrawTexture(iconRect, operation.icon);
				TooltipHandler.TipRegion(opBound.LeftPartPixels(OperationHeight), new TipSignal(operation.FullDescription));


				Rect labelBounds = opBound.RightPartPixels(width - OperationHeight);
				if (canApply)
				{
					if (canAfford)
					{
						//Widgets.Label(labelBounds, operation.label.CapitalizeFirst());

						if (operation.HasOperationCost(out int cost, out NaniteEffectCategoryDef _))
						{
							TooltipHandler.TipRegion(labelBounds, new TipSignal(string.Format("THNMF.OperationCostReportSimple".Translate(), cost, cheapestType.label)));
						}
						
						if (Widgets.ButtonText(labelBounds, operation.label.CapitalizeFirst()))
						{
							//PromptOperation(operation, pawn);
							Find.WindowStack.Add(new Dialog_Confirm( string.Format((cheapestType.renewable? "THNMF.StartOperation": "THNMF.StartOperationWarned").Translate(), operation.label, cheapestType.plural.Colorize(cheapestType.color)), "THNMF.BeginOperation".Translate(),
								delegate
								{
									if (operation.HasOperationCost(out _, out _))
									{
										foreach (CompResource resource in compResources)
										{
											if (resource.PipeNet.def != cheapestType.pipeNet) continue;
											resource.PipeNet.DrawAmongStorage(cost, resource.PipeNet.storages);
											break;
										}
									}
									naniteOperator.StartOperation(operation);
								}
							));
						} 
					}
					else
					{
						Widgets.ButtonText(labelBounds, operation.label.CapitalizeFirst().Colorize(notAffordColor));
						TooltipHandler.TipRegion(labelBounds, string.Format(string.Format("THNMF.CannotPerformNaniteOperation".Translate(), "THNMF.CannotPerformNaniteOperationNotEnoughNanites".Translate()).Colorize(notAffordColor), operation.CostDescription));
					}
				}
				//If the operation cannot be applied due to some outright issue
				else
				{
					Widgets.ButtonText(labelBounds, operation.label.CapitalizeFirst().Colorize(forbiddenColor));
					TooltipHandler.TipRegion(labelBounds, string.Format("THNMF.CannotPerformNaniteOperation".Translate(), issuesTag.Translate()).Colorize(forbiddenColor));
				}

				i++;
			}
			


			if (scrolling)
			{
				Widgets.EndScrollView();
			}
		}
	}
}