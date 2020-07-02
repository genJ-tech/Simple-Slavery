using System.Collections.Generic;
using System.Diagnostics;
using RimWorld;
using Verse;
using Verse.AI;

namespace SimpleSlavery.Jobs {
	public class WorkGiver_Warden_DoEmancipate : WorkGiver_Warden {
		//
		// Properties
		//
		public override PathEndMode PathEndMode {
			get {
				return PathEndMode.Touch;
			}
		}

		public override bool ShouldSkip(Pawn pawn, bool forced = false) {
			return SlaveUtility.GetSlaves().Count == 0;
		}

		//
		// Methods
		//
		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) {
			var slave = (Pawn)t;

			if (pawn == slave) {
				return null;
			}

			if (!SlaveUtility.IsPawnColonySlave(slave) || !pawn.CanReserve(slave) || !SlaveUtility.GetEnslavedHediff(slave).toBeFreed || slave.InAggroMentalState)
				return null;
			return JobMaker.MakeJob(SS_JobDefOf.EmancipateSlave, slave);
		}
	}

	internal class JobDriver_EmancipateSlave : JobDriver {
		const int emancipateDuration = 300;

		private Pawn Victim {
			get {
				return (Pawn)job.GetTarget(TargetIndex.A).Thing;
			}
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed) //Z- () -> bool errorOnFailed
				{
			return pawn.Reserve(Victim, job, 1, -1, null);
		}

		[DebuggerHidden]
		protected override IEnumerable<Toil> MakeNewToils() {
			this.FailOnDespawnedOrNull(TargetIndex.A);
			yield return Toils_Reserve.Reserve(TargetIndex.A);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
			yield return Toils_General.WaitWith(TargetIndex.A, emancipateDuration, true);
			yield return new Toil {
				initAction = delegate {
					Apparel collar;
					if (Victim.apparel.WornApparel.Find(SlaveUtility.IsSlaveCollar) != null) {
						Victim.apparel.TryDrop(Victim.apparel.WornApparel.Find(SlaveUtility.IsSlaveCollar), out collar);
					}
					SlaveUtility.EmancipatePawn(Victim);
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
		}
	}
}
