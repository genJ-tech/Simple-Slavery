using System.Collections.Generic;
using RimWorld;
using Verse;

namespace SimpleSlavery.UI {
	class MainTabWindow_Slaves : MainTabWindow_PawnTable {
		protected override IEnumerable<Pawn> Pawns => SlaveUtility.GetSlaves();
		protected override PawnTableDef PawnTableDef => SS_PawnTableDefOf.Slaves;
	}
}
