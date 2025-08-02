using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using NanomachineFoundry.Utils;
using RimWorld;
using Verse;

namespace NanomachineFoundry.Patches
{
    public class MiscPatches
    {
		public static void ApplyPatches(Harmony harmony)
		{
			harmony.Patch(AccessTools.Method(typeof(Building_CommsConsole), nameof(Building_CommsConsole.GetCommTargets)),
				postfix: new HarmonyMethod(typeof(MiscPatches), nameof(PostGetCommTargets)));
		}

		public static void PostGetCommTargets(ref IEnumerable<ICommunicable> __result, Pawn myPawn)
		{
            GameComponent_NanomachineFoundry comp = Current.Game.GetComponent<GameComponent_NanomachineFoundry>();
            if (comp.HailActive)
            {
	            List <ICommunicable> tempList = __result.ToList();
	            tempList.Add(comp.HailCommunicator);
	            __result = tempList;
            }
		}
		
		
    }
}