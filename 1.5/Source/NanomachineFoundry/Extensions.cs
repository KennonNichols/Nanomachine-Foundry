using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NanomachineFoundry.NaniteModifications;
using NanomachineFoundry.NaniteModifications.ModificationWorkers;
using NanomachineFoundry.Utils;
using Verse;

namespace NanomachineFoundry
{
    static class Extensions
    {
        public static NaniteTracker_Pawn GetNaniteTracker(this Pawn pawn) => NaniteTracker_Pawn.Get(pawn);
        
        public static ModificationWorker GetModificationWorkerOfType(this Pawn pawn, Type modificationWorkerType)
        {
            if (pawn.GetNaniteTracker().ActiveModWorkers.All(modificationWorker => modificationWorker.GetType() != modificationWorkerType)) return null;
            return pawn.GetNaniteTracker().ActiveModWorkers
                .First(modificationWorker => modificationWorker.GetType() == modificationWorkerType);
        }
        
        public static bool IsModificationWorkerEnabled<T>(this Pawn pawn, out T worker) where T : ModificationWorker
        {
            NaniteTracker_Pawn tracker = pawn.GetNaniteTracker();
            if (tracker == null)
            {
                worker = null;
                return false;
            }
            bool enabled = tracker.ActiveModWorkers.Any(modificationWorker => modificationWorker is T);
            worker = enabled? (T)tracker.ActiveModWorkers.First(modificationWorker => modificationWorker is T): null;
            return enabled;
        }
        
        
        public static bool IsMechanized(this Pawn pawn)
        {
            return (pawn.GetNaniteTracker()?.NaniteCapacity ?? 0) > 0;
        }
        
        public static bool IsModifiedMechanized(this Pawn pawn)
        {
            return pawn.GetNaniteTracker()?.HasModifications ?? false;
        }
        
        public static bool IsTruesightDeaf(this Pawn pawn)
        {
            return pawn.IsModificationWorkerEnabled(out ModificationWorker_Truesight truesight) && truesight.PsychicallyImmune;
        }
        /*
         *  if (pawn.IsModificationWorkerEnabled(out ModificationWorker_Truesight truesight))
            {
                return truesight.PsychicallyImmune;
            }
            return false;
         */
        
        public static IEnumerable<Hediff> GetAllDiseases(this Pawn pawn) => pawn.health.hediffSet.hediffs.Where(hediff => hediff.def.isInfection);
        
        
        public static string AsReadableList<T>(this IEnumerable<T> list, Func<T, string> nameGetter)
        {
            var enumerable = list as T[] ?? list.ToArray();
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < enumerable.Length; i++)
            {
                builder.Append(i >= 1 ? $", {(i == enumerable.Length - 1 ? "and ": "")}{nameGetter.Invoke(enumerable[i])}" : nameGetter.Invoke(enumerable[i]));
            }
            return builder.ToString();
        }
    }
}
