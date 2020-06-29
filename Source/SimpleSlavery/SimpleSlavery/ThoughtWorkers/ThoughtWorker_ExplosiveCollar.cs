using System;
using RimWorld;
using Verse;
using System.Linq;

namespace SimpleSlavery {
	public class ThoughtWorker_ExplosiveCollar : ThoughtWorker {
		protected override ThoughtState CurrentStateInternal(Pawn p) {
			Pawn pawn = p;
			if (SlaveUtility.HasSlaveCollar(pawn) && SlaveUtility.GetSlaveCollar(pawn).def.thingClass == typeof(SlaveCollar_Explosive)) {
				if ((SlaveUtility.GetSlaveCollar(pawn) as SlaveCollar_Explosive).armed) return ThoughtState.ActiveAtStage(1);
				return SlaveUtility.IsPawnColonySlave(pawn) ? ThoughtState.ActiveAtStage(0) : ThoughtState.ActiveAtStage(2);
			}
			return ThoughtState.Inactive;
		}
	}
}