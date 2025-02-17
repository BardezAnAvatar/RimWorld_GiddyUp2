﻿using GiddyUp;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using Settings = GiddyUp.ModSettings_GiddyUp;
using static GiddyUp.IsMountableUtility;
//using Multiplayer.API;

namespace GiddyUpCaravan.Harmony
{
    [HarmonyPatch(typeof(TransferableOneWayWidget), nameof(TransferableOneWayWidget.DoRow))]
    static class Patch_TransferableOneWayWidget
    {
        static bool Prepare()
        {
            return Settings.caravansEnabled;
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool flag = false;
            foreach (var code in instructions)
            {
                yield return code;

                if (code.OperandIs(AccessTools.Method(typeof(TooltipHandler), nameof(TooltipHandler.TipRegion), new Type[] { typeof(Rect), typeof(TipSignal) } ) ) )
                {
                    flag = true;
                }
                if (flag && code.opcode == OpCodes.Stloc_0)
                {
                    //TODO: Improve this to be less fragile
                    yield return new CodeInstruction(OpCodes.Ldarg_0); //Load instance
                    yield return new CodeInstruction(OpCodes.Ldloc_0); //load count local variable
                    yield return new CodeInstruction(OpCodes.Ldarg_1); //Load rectangle
                    yield return new CodeInstruction(OpCodes.Ldarg_2); //Load trad
                    yield return new CodeInstruction(OpCodes.Call, typeof(Patch_TransferableOneWayWidget).GetMethod(nameof(AddMountSelector)));
                    yield return new CodeInstruction(OpCodes.Stloc_0); //store count local variable
                    flag = false;
                }
            }
        }
        public static float AddMountSelector(TransferableOneWayWidget widget, float num, Rect rect, TransferableOneWay trad)
        {
            float buttonWidth = 75f;

            if (trad.AnyThing is not Pawn pawn) return num; //not an animal, return; 
            
            Rect buttonRect = new Rect(num - buttonWidth, 0f, buttonWidth, rect.height);
            List<TransferableOneWay> cachedTransferables = widget.sections[0].cachedTransferables;
            List<Pawn> pawns = new List<Pawn>();

            if (cachedTransferables != null)
            {
                foreach (TransferableOneWay tow in cachedTransferables)
                {
                    Pawn towPawn = tow.AnyThing as Pawn;
                    if (towPawn != null) pawns.Add(tow.AnyThing as Pawn);
                }
                //It quacks like a duck, so it is one!
            }
            SetSelectedForCaravan(pawn, trad);
            if (pawn.RaceProps.Animal && pawns.Count > 0)
            {
                HandleAnimal(num, buttonRect, pawn, pawns, trad);
            }
            else return num;

            return num - (buttonWidth - 25f);
        }
        static void SetSelectedForCaravan(Pawn pawn, TransferableOneWay trad)
        {
            ExtendedPawnData pawnData = pawn.GetGUData();
            var reservedMount = pawnData.reservedMount;

            if (trad.CountToTransfer == 0) //unset pawndata when pawn is not selected for caravan. 
            {
                pawnData.selectedForCaravan = false;
                if (reservedMount != null) UnsetDataForRider(pawnData);
                if (pawnData.reservedBy != null) UnsetDataForMount(pawnData);
            }
            if (reservedMount != null && (reservedMount.Dead || reservedMount.Downed)) UnsetDataForRider(pawnData);

            if (trad.CountToTransfer > 0) pawnData.selectedForCaravan = true;
        }
        static void UnsetDataForRider(ExtendedPawnData pawnData)
        {
            pawnData.reservedMount.GetGUData().ReservedBy = null;
            pawnData.ReservedMount = null;
        }
        static void UnsetDataForMount(ExtendedPawnData pawnData)
        {
            pawnData.reservedBy.GetGUData().ReservedMount = null;
            pawnData.ReservedBy = null;
        }
        static void HandleAnimal(float num, Rect buttonRect, Pawn animal, List<Pawn> pawns, TransferableOneWay trad)
        {
            ExtendedPawnData animalData = animal.GetGUData();
            Text.Anchor = TextAnchor.MiddleLeft;

            List<FloatMenuOption> list = new List<FloatMenuOption>();

            string buttonText;
            bool canMount;

            if (!animalData.selectedForCaravan || 
                (!animal.IsMountable(out Reason reason, null) && (reason == Reason.NotInModOptions || reason == Reason.NotFullyGrown)))
            {
                buttonText = "";
                canMount = false;
            }
            else
            {
                buttonText = animalData.reservedBy != null && animalData.reservedBy.GetGUData().selectedForCaravan ? 
                    animalData.reservedBy.Name.ToStringShort : "GU_Car_Set_Rider".Translate();
                canMount = true;
            }

            if (!canMount) Widgets.ButtonText(buttonRect, buttonText, false, false, false);
            else if (Widgets.ButtonText(buttonRect, buttonText, true, false, true))
            {
                var length = pawns.Count;
                for (int i = 0; i < length; i++)
                {
                    var pawn = pawns[i];
                    if (pawn.IsColonist)
                    {
                        ExtendedPawnData pawnData = pawn.GetGUData();
                        if (!pawnData.selectedForCaravan)
                        {
                            list.Add(new FloatMenuOption(pawn.Name.ToStringShort + " (" + "GU_Car_PawnNotSelected".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                            continue;
                        }

                        if (pawnData.reservedMount != null)
                        {
                            continue;
                        }
                        if (pawn.IsWorkTypeDisabledByAge(WorkTypeDefOf.Handling, out int age))
                        {
                            list.Add(new FloatMenuOption(pawn.Name.ToStringShort + " (" + "GU_Car_TooYoung".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                            continue;
                        }
                        list.Add(new FloatMenuOption(pawn.Name.ToStringShort, delegate
                        {
                            {
                                SelectMountRider(animalData, pawnData, animal, pawn);
                                trad.CountToTransfer = 1;
                            }
                        }, MenuOptionPriority.High, null, null, 0f, null, null));
                    }
                }
                list.Add(new FloatMenuOption("GU_Car_No_Rider".Translate(), delegate
                {
                    {
                        ClearMountRider(animalData);
                        trad.CountToTransfer = 1;
                    }
                }, MenuOptionPriority.Low, null, null, 0f, null, null));
                Find.WindowStack.Add(new FloatMenu(list));
            }
        }
        //[SyncMethod]
        static void SelectMountRider(ExtendedPawnData animalData, ExtendedPawnData pawnData, Pawn animal, Pawn pawn)
        {
            if (animalData.reservedBy != null) animalData.reservedBy.GetGUData().ReservedMount = null;

            pawnData.ReservedMount = animal;
            animalData.ReservedBy = pawn;

            animalData.selectedForCaravan = true;
        }
        //[SyncMethod]
        static void ClearMountRider(ExtendedPawnData animalData)
        {
            if (animalData.reservedBy != null)
            {
                ExtendedPawnData riderData = animalData.reservedBy.GetGUData();
                riderData.ReservedMount = null;
            }
            animalData.ReservedBy = null;
            animalData.selectedForCaravan = true;
        }
    }
    [HarmonyPatch(typeof(TransferableUtility), nameof(TransferableUtility.TransferAsOne))]
    static class Patch_TransferableUtility
    {
        static bool Prepare()
        {
            return Settings.caravansEnabled;
        }
        static bool Postfix(bool __result, Thing a, Thing b)
        {
            if (__result && a.def.category == ThingCategory.Pawn && b.def.category == ThingCategory.Pawn &&
                (IsMountableUtility.IsEverMountable(a as Pawn) || IsMountableUtility.IsEverMountable(b as Pawn)) )
            {
                return false;
            }
            return __result;
        }
    }
}