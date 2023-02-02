﻿using GiddyUp.Jobs;
using RimWorld;
using System.Text;
using Verse;

namespace GiddyUp.Stats
{
    class StatPart_Armor : StatPart
    {
        public override string ExplanationPart(StatRequest req)
        {
            StringBuilder sb = new StringBuilder();
            if (req.Thing is Pawn pawn && pawn.jobs != null && pawn.jobs.curDriver is JobDriver_Mounted)
            {
                var modExt = pawn.def.GetModExtension<CustomStatsPatch>();
                if (modExt != null && modExt.armorModifier != 1.0f)
                {
                    sb.AppendLine("GUC_GiddyUp".Translate());
                    sb.AppendLine("    " + "GUC_StatPart_MountTypeMultiplier".Translate() + ": " + (modExt.armorModifier).ToStringByStyle(ToStringStyle.PercentZero, ToStringNumberSense.Factor));
                }  
            }
            return sb.ToString();
        }
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.Thing is Pawn pawn && pawn.jobs != null && pawn.jobs.curDriver is JobDriver_Mounted)
            {
                var modExt = pawn.def.GetModExtension<CustomStatsPatch>();
                if (modExt != null && modExt.armorModifier != 1.0f)
                {
                    val *= modExt.armorModifier;
                }
            }
        }
    }
}
