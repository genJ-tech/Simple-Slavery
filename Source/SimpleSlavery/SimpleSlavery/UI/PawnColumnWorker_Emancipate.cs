using RimWorld;
using Verse;

namespace SimpleSlavery.UI {
	class PawnColumnWorker_Emancipate : PawnColumnWorker_Checkbox {
		protected override bool GetValue(Pawn pawn) {
			return SlaveUtility.GetEnslavedHediff(pawn).toBeFreed;
		}

		protected override void SetValue(Pawn pawn, bool value) {
			SlaveUtility.GetEnslavedHediff(pawn).toBeFreed = value;
		}
	}
}
