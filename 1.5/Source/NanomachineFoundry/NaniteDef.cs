using PipeSystem;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Verse;
using System.Text;

namespace NanomachineFoundry
{
    public class NaniteDef: Def
    {
		
		
		public int tier;
		public Color color;
		public HediffDef shockHediff;
		public HediffDef passiveHediff;
		public PipeNetDef pipeNet;
		public string pawnReferrer = "user";
		public List<NaniteEffectCategoryDef> categories;
		public bool modifiable = true;
		public bool renewable = true;
		public bool spawnable = false;
		public bool colonistSpawnable = false;
		public int spawnWeight = 1;

		public int valuePerUnit;
		//Whether to lose a percent of total capacity instead of a percent of current population during blood loss
		public bool absoluteLoss = false;

		public Color altColor;
		public string plural => label + "s";
		
		
		
		public override IEnumerable<string> ConfigErrors()
		{
			
			foreach (string item in base.ConfigErrors())
			{
				yield return item;
			}
			if (color == null)
			{
				yield return "Color cannot be null";
			}
			if (categories == null)
            {
				yield return "Effect categories (categories) cannot be null";
            }
			else if (categories.Empty())
            {
				yield return "Must specify at least one effect category";
            }
			/*
			if (!typeof(PowerWorker).IsAssignableFrom(workerClass))
			{
				yield return "Worker class must be a subclass of PowerWorker";
			}*/
		}

		public string NaniteReport => _cachedReport ??= BuildNaniteReport();
		private string _cachedReport;

		private string BuildNaniteReport()
        {
			StringBuilder categoryBuilder = new StringBuilder();
			foreach(NaniteEffectCategoryDef category in categories)
            {
				categoryBuilder.Append("\n • " + category.label);
            }
			return "THNMF.NaniteTypeReport".Translate(label.CapitalizeFirst(), tier, categoryBuilder.ToString(), description);
		}

		
	

		public override void PostLoad()
		{
			float dimAmount = 0.2f;
			altColor = new Color(
				Mathf.Max(0, color.r - dimAmount),
				Mathf.Max(0, color.g - dimAmount),
				Mathf.Max(0, color.b - dimAmount)
			);
		}

		public static bool TryGetNaniteTypeAssociatedWithPipeNetwork(PipeNetDef netDef, out  NaniteDef naniteType)
		{
			foreach (NaniteDef def in DefDatabase<NaniteDef>.AllDefs)
			{
				if (def.pipeNet != netDef) continue;
				naniteType = def;
				return true;
			}

			naniteType = null;
			return false;
		}

		public static IEnumerable<NaniteDef> GetNanitesCapableOfEffect(NaniteEffectCategoryDef effect)
		{
			return from type in DefDatabase<NaniteDef>.AllDefs where type.categories.Contains(effect) select type;
		}
	}

	public enum PawnNaniteFillMode
    {
		ForceAlways,
		ForceRenewable,
		ForceNever
    }

}
