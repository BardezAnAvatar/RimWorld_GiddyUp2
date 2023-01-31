﻿using GiddyUp.Jobs;
using GiddyUp.Storage;
using GiddyUp.Utilities;
using GiddyUp.Zones;
using HarmonyLib;
using RimWorld;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace GiddyUpCaravan.Harmony
{
    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.DetermineNextJob))]
    static class Pawn_JobTracker_DetermineNextJob
    {
        static void Postfix(Pawn_JobTracker __instance, ref ThinkResult __result)
        {
            Pawn pawn = __instance.pawn;
            if (pawn.RaceProps.Animal && pawn.Faction != Faction.OfPlayer && pawn.Faction != null)
            {
                if (pawn.GetLord() != null && (pawn.GetLord().CurLordToil is LordToil_DefendPoint || pawn.GetLord().CurLordToil.GetType().Name == "LordToil_DefendTraderCaravan"))
                {

                    if (__result.SourceNode is JobGiver_Wander)
                    {
                        JobGiver_Wander jgWander = (JobGiver_Wander)__result.SourceNode;
                        jgWander.wanderRadius = 5f;
                    }

                }

            }
            //Check if pawn is enemy and can mount.
            if (pawn.IsColonistPlayerControlled || pawn.IsBorrowedByAnyFaction() || pawn.RaceProps.Animal || pawn.Faction.HostileTo(Faction.OfPlayer) || !pawn.RaceProps.Humanlike)
            {            
                return;
            }
            if (pawn.IsPrisoner)
            {
                return;
            }
            if(__result.Job == null) //shouldn't happen, but may happen with mods.
            {
                return;
            }

            LocalTargetInfo target = DistanceUtility.GetFirstTarget(__result.Job, TargetIndex.A);
            if (!target.IsValid)
            {
                return;
            }

            ExtendedDataStorage store = GiddyUp.Setup._extendedDataStorage;

            //Log.Message("wrong duty");
            ExtendedPawnData PawnData = store.GetExtendedDataFor(pawn.thingIDNumber);
            Lord lord = pawn.GetLord();
            if (lord == null)
            {
                return;

            }
            if(__result.Job.def == GiddyUp.ResourceBank.JobDefOf.Dismount || __result.Job.def == GiddyUp.ResourceBank.JobDefOf.Mount)
            {
                return;
            }

            QueuedJob qJob = pawn.jobs.jobQueue.FirstOrFallback(null);
            if(qJob != null && (qJob.job.def == GiddyUp.ResourceBank.JobDefOf.Dismount || qJob.job.def == GiddyUp.ResourceBank.JobDefOf.Mount))
            {
                return;
            }

            if (lord.CurLordToil is LordToil_ExitMapAndEscortCarriers || lord.CurLordToil is LordToil_Travel || lord.CurLordToil is LordToil_ExitMap || lord.CurLordToil is LordToil_ExitMapTraderFighting)
            {
                if (PawnData.owning != null &&
                    PawnData.owning.Faction == pawn.Faction &&
                    PawnData.mount == null && 
                    !PawnData.owning.Downed &&
                    PawnData.owning.Spawned && 
                    !pawn.IsBurning() &&
                    !pawn.Downed)
                {
                    MountAnimal(__instance, pawn, PawnData, ref __result);

                }
            }
            else if(lord.CurLordToil.GetType().Name == "LordToil_DefendTraderCaravan" || lord.CurLordToil is LordToil_DefendPoint) //first option is internal class, hence this way of accessing. 
            {
                if (PawnData.mount != null)
                {
                    ParkAnimal(__instance, pawn, PawnData);
                }
            }
        }
        static void MountAnimal(Pawn_JobTracker __instance, Pawn pawn, ExtendedPawnData pawnData, ref ThinkResult __result)
        {
            Job oldJob = __result.Job;
            Job mountJob = new Job(GiddyUp.ResourceBank.JobDefOf.Mount, pawnData.owning);
            mountJob.count = 1;
            __result = new ThinkResult(mountJob, __result.SourceNode, __result.Tag, false);
            __instance.jobQueue.EnqueueFirst(oldJob);
        }
        static void ParkAnimal(Pawn_JobTracker __instance, Pawn pawn, ExtendedPawnData pawnData)
        {
            Area_GU areaFound = (Area_GU) pawn.Map.areaManager.GetLabeled(Base.DropAnimal_NPC_LABEL);
            IntVec3 targetLoc = pawn.Position;

            if(areaFound != null && areaFound.ActiveCells.Count() > 0)
            {
                targetLoc = DistanceUtility.getClosestAreaLoc(pawn, areaFound);
            }
            if (pawn.Map.reachability.CanReach(pawn.Position, targetLoc, PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)))
            {
                Job dismountJob = new Job(GiddyUp.ResourceBank.JobDefOf.Dismount);
                dismountJob.count = 1;
                __instance.jobQueue.EnqueueFirst(dismountJob);
                __instance.jobQueue.EnqueueFirst(new Job(JobDefOf.Goto, targetLoc));
                PawnDuty animalDuty = pawnData.mount.mindState.duty;
                //if(pawnData.mount.GetLord().CurLordToil is LordToil)

                if(animalDuty != null)
                {
                    animalDuty.focus = new LocalTargetInfo(targetLoc);
                }
            }
            else
            {
                Messages.Message("GU_Car_NotReachable_DropAnimal_NPC_Message".Translate(), new RimWorld.Planet.GlobalTargetInfo(targetLoc, pawn.Map), MessageTypeDefOf.NegativeEvent);
            }
        }
    }
}
