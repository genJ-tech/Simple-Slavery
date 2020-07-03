using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RimWorld;
using Verse;

namespace SimpleSlavery {
	class SlaveComp : ThingComp {
		public override IEnumerable<Gizmo> CompGetGizmosExtra() {
			if (parent == null) {
				yield break;
			}
			var pawn = parent as Pawn;

			if (pawn.apparel != null) {
				foreach (var apparel in pawn.apparel.WornApparel) {
					var slaveApparel = apparel as SlaveApparel;
					if (slaveApparel != null) {
						foreach (var g in slaveApparel.SlaveGizmos()) yield return g;
					}
				}
			}

			if (SlaveUtility.IsPawnColonySlave(pawn)) {
				var hediff = SlaveUtility.GetEnslavedHediff(pawn);

				var freeSlave = new Command_Toggle();
				freeSlave.isActive = () => hediff.toBeFreed;
				freeSlave.defaultLabel = "LabelWordEmancipate".Translate();
				freeSlave.defaultDesc = "CommandDescriptionEmancipate".Translate(pawn.Name.ToStringShort);
				freeSlave.toggleAction = () => hediff.toBeFreed = !hediff.toBeFreed;
				freeSlave.alsoClickIfOtherInGroupClicked = true;
				freeSlave.activateSound = SoundDefOf.Click;
				freeSlave.icon = ContentFinder<Texture2D>.Get("UI/Commands/Emancipate", true);
				yield return freeSlave;

				var shackleSlave = new Command_Toggle();
				shackleSlave.isActive = () => hediff.shackledGoal;
				shackleSlave.defaultLabel = "LabelWordShackle".Translate();
				shackleSlave.defaultDesc = "CommandDescriptionShackle".Translate(pawn.Name.ToStringShort);
				shackleSlave.toggleAction = () => hediff.shackledGoal = !hediff.shackledGoal;
				shackleSlave.alsoClickIfOtherInGroupClicked = true;
				shackleSlave.activateSound = SoundDefOf.Click;
				shackleSlave.icon = ContentFinder<Texture2D>.Get("UI/Commands/Shackle", true);
				yield return shackleSlave;
			}
		}
	}
}
