using RimWorld;
using UnityEngine;
using Verse;

namespace SimpleSlavery.UI {
	class PawnColumnWorker_Willpower : PawnColumnWorker {
		public override int Compare(Pawn a, Pawn b) {
			return SlaveUtility.GetEnslavedHediff(a).SlaveWillpower.CompareTo(SlaveUtility.GetEnslavedHediff(b).SlaveWillpower);
		}

		public override void DoCell(Rect rect, Pawn pawn, PawnTable table) {
			Hediff_Enslaved hediff = SlaveUtility.GetEnslavedHediff(pawn);
			Text.Anchor = TextAnchor.MiddleCenter;
			if (hediff.SlaveWillpower == 0) {
				Widgets.Label(rect, "broken".Translate());
			} else {
				Widgets.Label(rect, GenText.ToStringPercent(hediff.SlaveWillpower));
			}
			Text.Anchor = TextAnchor.UpperLeft;
		}
	}
}
