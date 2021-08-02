﻿using GiddyUpCore.Jobs;
using GiddyUpCore.Storage;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace GiddyUpCore.Utilities
{
    public static class IsMountableUtility
    {
        public enum Reason{NotFullyGrown, NeedsObedience, NotInModOptions, CanMount, IsRoped};

        public static Pawn CurMount(this Pawn pawn)
        {
            if (Base.Instance.GetExtendedDataStorage() is ExtendedDataStorage store && store.GetExtendedDataFor(pawn) is ExtendedPawnData pawnData)
            {
                return pawnData.mount;
            }
            return null;
        }
        public static bool IsMountable(ThingDef thingDef)
        {
            return isMountable(thingDef.GetConcreteExample() as Pawn);
        }
        public static bool isMountable(Pawn pawn)
        {
            return isMountable(pawn, out Reason reason);
        }

        public static bool IsCurrentlyMounted(Pawn animal)
        {
            if(animal.CurJob == null || animal.CurJob.def != GUC_JobDefOf.Mounted)
            {
                return false;
            }
            JobDriver_Mounted mountedDriver = (JobDriver_Mounted)animal.jobs.curDriver;
            Pawn Rider = mountedDriver.Rider;
            ExtendedDataStorage store = Base.Instance.GetExtendedDataStorage();
            if(store == null || store.GetExtendedDataFor(Rider).mount == null)
            {
                return false;
            }
            return true;
        }

        public static bool isMountable(Pawn animal, out Reason reason)
        {
            reason = Reason.CanMount;
            if (!isAllowedInModOptions(animal.def.defName))
            {
                reason = Reason.NotInModOptions;
                return false;
            }
            if (animal.ageTracker.CurLifeStageIndex != animal.RaceProps.lifeStageAges.Count - 1)
            {
                if (!animal.def.HasModExtension<AllowedLifeStagesPatch>())
                {
                    reason = Reason.NotFullyGrown;
                    return false;
                }
                else //Use custom life stages instead of last life stage if a patch exists for that
                {
                    AllowedLifeStagesPatch customLifeStages = animal.def.GetModExtension<AllowedLifeStagesPatch>();
                    if (!customLifeStages.getAllowedLifeStagesAsList().Contains(animal.ageTracker.CurLifeStageIndex))
                    {
                        reason = Reason.NotFullyGrown;
                        return false;
                    }
                }
            }
            if (animal.training == null || (animal.training != null && !animal.training.HasLearned(TrainableDefOf.Tameness)))
            {
                reason = Reason.NeedsObedience;
                return false;
            }
            if (animal.roping != null && animal.roping.IsRoped)
            {
                reason = Reason.IsRoped;
                return false;
            }
            return true;
        }

        public static bool isAllowedInModOptions(String animalName)
        {
            GiddyUpCore.AnimalRecord value;
            bool found = GiddyUpCore.Base.animalSelecter.Value.InnerList.TryGetValue(animalName, out value);
            if (found && value.isSelected)
            {
                return true;
            }
            return false;
        }
    }
}
