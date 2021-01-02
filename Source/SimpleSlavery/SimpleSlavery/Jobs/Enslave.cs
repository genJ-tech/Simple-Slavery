﻿using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace SimpleSlavery.Jobs {
	public class WorkGiver_Warden_DoEnslavement : WorkGiver_Warden {
		public override PathEndMode PathEndMode {
			get {
				return PathEndMode.OnCell;
			}
		}
		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) {
			// TODO: Look into this deprecation and see if anything needs to actually change for the new signature (which adds an optional bool)
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
							!Victim.InMentalState &&
							!Victim.Downed
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
								SlaveUtility.GetEnslavedHediff(Victim).TakeWillpowerHit(1.5f);
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
}
