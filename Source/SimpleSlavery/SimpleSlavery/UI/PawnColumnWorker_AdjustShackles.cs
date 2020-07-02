using RimWorld;
using Verse;

namespace SimpleSlavery.UI {
	class PawnColumnWorker_AdjustShackles : PawnColumnWorker_Checkbox {
		protected override bool GetValue(Pawn pawn) {
			return SlaveUtility.GetEnslavedHediff(pawn).shackledGoal;
		}

		protected override void SetValue(Pawn pawn, bool value) {
			SlaveUtility.GetEnslavedHediff(pawn).shackledGoal = value;
		}
	}
}
