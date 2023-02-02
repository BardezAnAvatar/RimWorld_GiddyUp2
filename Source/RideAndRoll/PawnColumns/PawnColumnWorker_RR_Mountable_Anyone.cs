﻿using GiddyUp.Utilities;
using RimWorld;
using Verse;
//using Multiplayer.API;

namespace GiddyUpRideAndRoll.PawnColumns
{
    class PawnColumnWorker_RR_Mountable_Anyone : PawnColumnWorker_Checkbox
    {
        public override bool HasCheckbox(Pawn pawn)
        {
            return IsMountableUtility.IsMountable(pawn);
        }
        public override bool GetValue(Pawn pawn)
        {
            return GiddyUp.Setup._extendedDataStorage.GetExtendedDataFor(pawn.thingIDNumber).mountableByAnyone;
        }

        //[SyncMethod]
        public override void SetValue(Pawn pawn, bool value, PawnTable table)
        {
            GiddyUp.Setup._extendedDataStorage.GetExtendedDataFor(pawn.thingIDNumber).mountableByAnyone = value;
        }
    }
}
