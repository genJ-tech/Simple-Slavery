using System;
using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;
using System.Diagnostics;

namespace SimpleSlavery {

	// Enslavment //

	public class WorkGiver_Warden_DoEnslavement : WorkGiver_Warden {
		public override PathEndMode PathEndMode {
			get {
				return PathEndMode.OnCell;
			}
		}
		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) {
			if (!ShouldTakeCareOfPrisoner(pawn, t)) {
				return null;
			}
			Pawn prisoner = (Pawn)t;
			var slaveCollar = (Apparel)pawn.Map.listerThings.ThingsMatching(
					ThingRequest.ForGroup(ThingRequestGroup.Apparel)).Find(x => SlaveUtility.IsSlaveCollar((Apparel)x) && !x.IsForbidden(pawn.Faction));
			if (prisoner.guest.interactionMode != SS_PrisonerInteractionModeDefOf.PIM_Enslave || SlaveUtility.IsPawnColonySlave(pawn) || !pawn.CanReserve(prisoner, 1, -1, null, false) || !pawn.CanReserve(slaveCollar, 1, -1, null, false)) {
				return null;
			}
			return slaveCollar != null ? JobMaker.MakeJob(SS_JobDefOf.EnslavePrisoner, prisoner, slaveCollar) : null;
		}
	}

	internal class JobDriver_EnslavePrisoner : JobDriver {
		const int enslaveDuration = 300;

		private Pawn Victim {
			get {
				return (Pawn)job.GetTarget(TargetIndex.A).Thing;
			}
		}

		private Apparel SlaveCollar {
			get {
				return (Apparel)job.GetTarget(TargetIndex.B).Thing;
			}
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed) {
			return pawn.Reserve(Victim, job, 1, -1, null) && pawn.Reserve(SlaveCollar, job, 1, -1, null);
		}

		//[DebuggerHidden]
		protected override IEnumerable<Toil> MakeNewToils() {
			this.FailOnDespawnedOrNull(TargetIndex.A);
			this.FailOnDestroyedOrNull(TargetIndex.B);
			this.FailOnForbidden(TargetIndex.B);
			yield return Toils_Reserve.Reserve(TargetIndex.A);
			yield return Toils_Reserve.Reserve(TargetIndex.B);
			yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
			yield return new Toil {
				initAction = delegate {
					pawn.jobs.curJob.count = 1;
				}
			};
			yield return Toils_Haul.StartCarryThing(TargetIndex.B);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
			yield return Toils_General.WaitWith(TargetIndex.A, enslaveDuration, true);
			yield return new Toil {
				initAction = delegate {
					Thing slaveCollar = null;
					pawn.carryTracker.TryDropCarriedThing(pawn.PositionHeld, ThingPlaceMode.Direct, out slaveCollar, null);
					if (slaveCollar != null) {
						var collar = (Apparel)slaveCollar;
						// Do enslave attempt
						bool success = true;

						if (!Victim.jobs.curDriver.asleep &&
							!Victim.story.traits.HasTrait(TraitDef.Named("Wimp")) &&
							!Victim.InMentalState
						) {
							if (Victim.story.traits.HasTrait(TraitDefOf.Nerves) &&
								(Victim.story.traits.GetTrait(TraitDefOf.Nerves).Degree == -2 && Rand.Value > 0.66f) ||
																Victim.needs.mood.CurInstantLevelPercentage < Rand.Range(0f, 0.33f)
														)
								success = false;
						}
						if (success) {
							Log.Message("Enslaved " + Victim.Name.ToStringShort); //Z- NameStringShort -> Name.ToStringShort
							SlaveUtility.EnslavePawn(Victim, collar);
							if (slaveCollar.Stuff.stuffProps.categories.Contains(StuffCategoryDefOf.Metallic) && !Victim.health.hediffSet.HasHediff(SS_HediffDefOf.SlaveMemory))
								SlaveUtility.GetEnslavedHediff(Victim).TakeWillpowerHit(150);
							Messages.Message("EnslavedPrisonerSuccess".Translate(pawn.Name.ToStringShort, Victim.Name.ToStringShort), MessageTypeDefOf.PositiveEvent); //Z- NameStringShort -> Name.ToStringShort
							AddEndCondition(() => JobCondition.Succeeded);
						} else {
							Victim.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, "ReasonFailedEnslave".Translate(pawn.Name.ToStringShort, Victim.Name.ToStringShort)); //Z- NameStringShort -> Name.ToStringShort
							AddEndCondition(() => JobCondition.Incompletable);
						}
					} else
						AddEndCondition(() => JobCondition.Incompletable);
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
		}
	}


	// Emancipation //

	public class WorkGiver_Warden_DoEmancipate : WorkGiver_Warden {
		//
		// Properties
		//
		public override PathEndMode PathEndMode {
			get {
				return PathEndMode.Touch;
			}
		}

		//
		// Methods
		//
		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) {
			var slave = (Pawn)t;

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

	// Shackling //

	public class WorkGiver_Warden_ShackleSlave : WorkGiver_Warden {
		//
		// Properties
		//
		public override PathEndMode PathEndMode {
			get {
				return PathEndMode.Touch;
			}
		}

		//
		// Methods
		//
		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) {
			var slave = (Pawn)t;

			if (!SlaveUtility.IsPawnColonySlave(slave) || !pawn.CanReserve(slave) || SlaveUtility.GetEnslavedHediff(slave).shackledGoal == SlaveUtility.GetEnslavedHediff(slave).shackled || slave.InAggroMentalState)
				return null;
			return JobMaker.MakeJob(SS_JobDefOf.ShackleSlave, slave);
		}
	}

	internal class JobDriver_ShackleSlave : JobDriver {
		const int shackleDuration = 300;

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
			this.FailOn(() => !SlaveUtility.IsPawnColonySlave(Victim));
			yield return Toils_Reserve.Reserve(TargetIndex.A);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
			yield return Toils_General.WaitWith(TargetIndex.A, shackleDuration, true);
			yield return new Toil {
				initAction = delegate {
					SlaveUtility.GetEnslavedHediff(Victim).shackled = SlaveUtility.GetEnslavedHediff(Victim).shackledGoal;
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
		}
	}

}