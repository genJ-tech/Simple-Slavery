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
			if (parent != null && SlaveUtility.IsPawnColonySlave(parent as Pawn)) {
				var pawn = parent as Pawn;
				var hediff = SlaveUtility.GetEnslavedHediff(parent as Pawn);

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
