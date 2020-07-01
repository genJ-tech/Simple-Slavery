using System;
using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;

namespace SimpleSlavery {
	public class Hediff_EmancipateFix : HediffWithComps {
		public Faction actualFaction = null;
		public Faction slaverFaction = null;
		public float willpower = 100;
		public override void PostTick() {
			base.PostTick();
			if (this.ageTicks > 1) {
				pawn.SetFaction(actualFaction);
				pawn.guest.SetGuestStatus(Faction.OfPlayer, true);
				if (pawn.GetRoom().isPrisonCell) {
					pawn.guest.interactionMode = PrisonerInteractionModeDefOf.NoInteraction;
				} else {
					if (willpower <= 1 || // Broken slaves will typically join
							(willpower <= 25 && pawn.needs.mood.CurLevelPercentage > Rand.Range(0.55f, 0.95f)) &&
							pawn.story.traits.allTraits.Find(x => x.def == TraitDefOf.Nerves && x.Degree > 0) == null // Iron-willed/steadfast pawns never join on emancipation
					) {// Join the colony
						pawn.guest.SetGuestStatus(null);
						pawn.guest.isPrisonerInt = false;
						pawn.SetFaction(slaverFaction);
					} else
						pawn.guest.interactionMode = PrisonerInteractionModeDefOf.Release;
					pawn.guest.Released = true;
				}
				pawn.health.RemoveHediff(this);
			}
		}
	}

	public class Hediff_SlaveMemory : HediffWithComps {
		// Saved player settings
		public Dictionary<WorkTypeDef, int> savedWorkPriorities = new Dictionary<WorkTypeDef, int> { };
		public Area savedRestrictedArea;
		public sbyte savedMedicalCare;

		// Saved willpower
		public float savedWillpower = 0;

		public override void PostMake() {
			base.PostMake();
			SaveMemory();
		}

		public void SaveMemory() {
			if (pawn.workSettings == null) {
				pawn.workSettings = new Pawn_WorkSettings(pawn);
				pawn.workSettings.EnableAndInitialize();
			}
			foreach (WorkTypeDef work in DefDatabase<WorkTypeDef>.AllDefs) {
				if (!savedWorkPriorities.ContainsKey(work)) {
					savedWorkPriorities.Add(work, pawn.workSettings.GetPriority(work));
				} else {
					savedWorkPriorities[work] = pawn.workSettings.GetPriority(work);
				}
			}
			if (pawn.playerSettings != null && pawn.playerSettings.AreaRestriction != null)
				savedRestrictedArea = pawn.playerSettings.AreaRestriction;
			savedMedicalCare = (sbyte)pawn.playerSettings.medCare;
			if (SlaveUtility.GetEnslavedHediff(pawn) != null)
				savedWillpower = SlaveUtility.GetEnslavedHediff(pawn).SlaveWillpower;
		}

		public override void ExposeData() {
			base.ExposeData();
			Scribe_Values.Look<float>(ref savedWillpower, "savedWillpower");
			Scribe_Collections.Look<WorkTypeDef, int>(ref savedWorkPriorities, "savedWorkPriorities");
			Scribe_References.Look<Area>(ref savedRestrictedArea, "savedRestrictedArea");
			Scribe_Values.Look<sbyte>(ref savedMedicalCare, "savedMedicalCare");
		}

		public override bool Visible {
			get {
				return false;
			}
		}
	}


	public class Hediff_Enslaved : HediffWithComps {
		const int minHoursBetweenEscapeAttempts = 24;
		const int maxWillpower = 100;

		public Faction actualFaction = null;
		public Faction slaverFaction = null;
		float willpower = 100;
		int hoursSinceLastEscapeAttempt = 6;
		public bool waitingInJail = false;
		public bool isMovingToEscape = false;

		public bool toBeFreed = false;
		public bool shackledGoal = true;
		public bool shackled = true;

		public void SaveMemory() {
			Hediff_SlaveMemory slaveMemory = null;
			// Create the slave memory hediff if we don't have it yet
			if (!pawn.health.hediffSet.HasHediff(SS_HediffDefOf.SlaveMemory)) {
				pawn.health.AddHediff(SS_HediffDefOf.SlaveMemory);
			}
			// Find the hediff
			slaveMemory = pawn.health.hediffSet.GetFirstHediffOfDef(SS_HediffDefOf.SlaveMemory) as Hediff_SlaveMemory;
			slaveMemory.SaveMemory();
		}
		public void LoadMemory() {
			if (pawn.health.hediffSet.HasHediff(SS_HediffDefOf.SlaveMemory)) {
				// Re-apply all player settings that get reset upon leaving faction
				var memory = pawn.health.hediffSet.GetFirstHediffOfDef(SS_HediffDefOf.SlaveMemory) as Hediff_SlaveMemory;
				foreach (KeyValuePair<WorkTypeDef, int> workPriority in memory.savedWorkPriorities) {
					pawn.workSettings.SetPriority(workPriority.Key, workPriority.Value);
				}
				if (memory.savedRestrictedArea != null)
					pawn.playerSettings.AreaRestriction = memory.savedRestrictedArea;
				pawn.playerSettings.medCare = (MedicalCareCategory)memory.savedMedicalCare;
				willpower = memory.savedWillpower;
			} else
				Log.Error("[SimpleSlavery]: Failed to find SlaveMemory hediff for pawn " + pawn.Name.ToStringShort + "."); //Z- NameStringShort -> Name.ToStringShort
		}

		public float SlaveWillpower {
			get {
				return willpower;
			}
		}

		public override void PostMake() {
			base.PostMake();
			actualFaction = pawn.Faction;
			slaverFaction = (Faction.OfPlayer);
			if (pawn.Faction == slaverFaction) {
				pawn.guest.SetGuestStatus(null);
			} else
				pawn.SetFaction(Faction.OfPlayer);

			// If the hediff was added without equipping a slave collar, ensure they get one
			if (pawn.apparel.WornApparel.Find(SlaveUtility.IsSlaveCollar) == null)
				SlaveUtility.GiveSlaveCollar(pawn);

			// Certain backstories begin with no willpower
			if (pawn.story.childhood.title == "Vatgrown slavegirl") //Z- Title -> title
				willpower = 0;
			if (pawn.story.adulthood != null) {
				if (pawn.story.adulthood.title == "Urbworld sex slave") //Z- Title - title
					willpower = 0;
			}

			// We were freed, but then enslaved AGAIN
			if (pawn.health.hediffSet.HasHediff(SS_HediffDefOf.SlaveMemory)) {
				SlaveAgain();
				// Take a willpower hit, but only if we were free for a while
				if (pawn.health.hediffSet.GetFirstHediffOfDef(SS_HediffDefOf.SlaveMemory).ageTicks > 10000) {
					TakeWillpowerHit(50);
				}
			}

			SaveMemory();
		}

		public override void Tick() {
			base.Tick();

			// Each day the pawn loses some willpower
			if (pawn.IsHashIntervalTick(60000)) {
				// Make sure we're not already at rock bottom
				if (willpower > 0) {
					TakeWillpowerHit(10);
				}
			}

			// Break here if we're not spawned on a map
			if (!pawn.Spawned)
				return;

			// Every second or so
			if (pawn.IsHashIntervalTick(60 * 1)) {
				// Return to slave-state
				if (waitingInJail && pawn.GetRoom().isPrisonCell && hoursSinceLastEscapeAttempt >= 6 && !pawn.Downed && !pawn.jobs.curDriver.asleep) {
					//Log.Message ("DEBUG: " + pawn.Name.ToStringShort + " has returned to being a slave."); //Z- NameStringShort -> Name.ToStringShort
					SlaveAgain();
				}
				// Looks like we got interrupted while moving to escape
				if (isMovingToEscape && pawn.CurJob.def != JobDefOf.Goto)
					isMovingToEscape = false;
				// Run!
				if (isMovingToEscape && pawn.GetRoom().TouchesMapEdge) {
					TryToEscape();
				}
			}

			// Every three hours
			if (pawn.IsHashIntervalTick(2500 * 3)) {
				if (hoursSinceLastEscapeAttempt < 72)
					hoursSinceLastEscapeAttempt += 1;
				// The pawn will consider escape
				if (willpower > 0 &&
					Rand.Chance(0.1f) &&
					pawn.Faction == Faction.OfPlayer &&
					pawn.health.capacities.CanBeAwake &&
					pawn.health.capacities.CapableOf(PawnCapacityDefOf.Consciousness) &&
					pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving) &&
					!pawn.health.Downed &&
					!pawn.jobs.curDriver.asleep
					)
					ConsiderEscape();
			}
		}

		public void TakeWillpowerHit(float severity) {
			if (pawn.story.traits.HasTrait(TraitDef.Named("Wimp")))
				severity *= 2;
			if (pawn.story.traits.HasTrait(TraitDefOf.Nerves)) {
				int nerveDegree = pawn.story.traits.GetTrait(TraitDefOf.Nerves).Degree;
				if (nerveDegree > 0)
					severity /= nerveDegree;
				else if (nerveDegree < 0)
					severity *= nerveDegree;
			}

			willpower -= (float)Math.Abs(severity * 0.05);
			if (willpower < 0)
				willpower = 0;
			//Log.Message ("DEBUG: Slave " + pawn.NameStringShort + " Willpower = " + willpower);
		}

		public void SetWillpowerDirect(int newWill) {
			willpower = Math.Min(Math.Max(newWill, 0), maxWillpower);
		}

		// Try to drum up the courage to escape
		public void ConsiderEscape() {
			if (!pawn.guest.IsPrisoner && !pawn.GetRoom().isPrisonCell && !isMovingToEscape) {

				// moodFactor multiplies the time between escape attempts
				var moodFactor = Math.Min(pawn.needs.mood.CurInstantLevelPercentage + 0.5, 1f);
				//if (true) { // debugging for fast escape attempts
				if (hoursSinceLastEscapeAttempt > minHoursBetweenEscapeAttempts * moodFactor) {

					int combined_bonus = 0;
					// If we're not in a "room" aka free to run outside, then chance improves
					if (pawn.GetRoom().TouchesMapEdge)
						combined_bonus += 10;
					// Bonus from being outside but walled in (for example, a courtyard)
					else if (pawn.GetRoom().PsychologicallyOutdoors)
						combined_bonus += 5;
					// Bonus from not wearing slave collar
					if (SlaveUtility.HasSlaveCollar(pawn)) {
						var collar = SlaveUtility.GetSlaveCollar(pawn);
						var collarMalus = new Dictionary<TechLevel, int>{
							{TechLevel.Neolithic, 10},
							{TechLevel.Medieval, 20},
							{TechLevel.Industrial, 35},
							{TechLevel.Spacer, 50},
														{TechLevel.Archotech, 75}
						};
						if (collarMalus.ContainsKey(collar.def.techLevel))
							combined_bonus -= collarMalus[collar.def.techLevel];
					}
					if (pawn.story.traits.HasTrait(TraitDefOf.Nerves))
						combined_bonus += pawn.story.traits.GetTrait(TraitDefOf.Nerves).Degree * 10;
					// Health malus
					combined_bonus -= (int)((1 - pawn.health.summaryHealth.SummaryHealthPercent) * 50);
					// Take hours since last attempt into account
					combined_bonus += (int)Math.Round((Math.Max(hoursSinceLastEscapeAttempt, 72) * (willpower / 100)) * 0.2083f);

					// Do a willpower check
					if (Math.Round(willpower) + combined_bonus > Rand.Range(1, maxWillpower)) {
						if (pawn.GetRoom().TouchesMapEdge)
							TryToEscape();
						else
							MoveToEscape();
					}
				}
			}
		}

		public void MoveToEscape() {
			IntVec3 c;
			if (RCellFinder.TryFindBestExitSpot(pawn, out c)) {
				pawn.jobs.StartJob(new Job(JobDefOf.Goto, c), JobCondition.InterruptForced);
				isMovingToEscape = true;
			}
		}

		// Freedom!!!
		public void TryToEscape() {
			isMovingToEscape = false; // We've moved into place to escape already
			hoursSinceLastEscapeAttempt = 0; // Reset time tracker
			SaveMemory(); // Save our work priorities and willpower to the external hediff
			Messages.Message("MessageSlaveEscaping".Translate(pawn.Name.ToStringShort), pawn, MessageTypeDefOf.ThreatBig); //Z- NameStringShort -> Name.ToStringShort
																																																										 //Z- Added Letter to escaping slaves event
			string text = "LetterIncidentSlaveEscaping".Translate(pawn.Name.ToString());
			Find.LetterStack.ReceiveLetter("LetterLabelSlaveEscaping".Translate(), text, LetterDefOf.NegativeEvent, null);
			pawn.SetFaction(actualFaction); // Revert to real faction
			pawn.guest.SetGuestStatus(slaverFaction, true);
			pawn.guest.Released = false; // Ensure the slave is not set to released mode
			pawn.guest.interactionMode = PrisonerInteractionModeDefOf.NoInteraction; // Ensure the interaction mode is not "release"
			pawn.guilt.Notify_Guilty();
		}

		public void CaughtSlave() {
			var memory = pawn.health.hediffSet.GetFirstHediffOfDef(SS_HediffDefOf.SlaveMemory) as Hediff_SlaveMemory;
			pawn.playerSettings.medCare = (MedicalCareCategory)memory.savedMedicalCare;
			SaveMemory();
			waitingInJail = true;
		}

		private void SlaveAgain() {
			if (pawn.Faction != Faction.OfPlayer) {
				pawn.SetFaction(Faction.OfPlayer);
			} else {
				Log.Warning("Pawn was already of the player faction when SlaveAgain was executed-- this should never happen.");
			}
			pawn.guest.SetGuestStatus(null);
			LoadMemory();
			TakeWillpowerHit(100);
			hoursSinceLastEscapeAttempt = 0;
			waitingInJail = false;
		}

		public void Emancipate() {
			// For whatever reason we cannot do this here, we do it in "EmancipateFix" hediff
			//pawn.SetFaction (actualFaction);
			SaveMemory();
			pawn.health.AddHediff(SS_HediffDefOf.Hediff_EmancipateFix);
			((Hediff_EmancipateFix)pawn.health.hediffSet.GetFirstHediffOfDef(SS_HediffDefOf.Hediff_EmancipateFix)).actualFaction = actualFaction;
			((Hediff_EmancipateFix)pawn.health.hediffSet.GetFirstHediffOfDef(SS_HediffDefOf.Hediff_EmancipateFix)).slaverFaction = slaverFaction;
			((Hediff_EmancipateFix)pawn.health.hediffSet.GetFirstHediffOfDef(SS_HediffDefOf.Hediff_EmancipateFix)).willpower = willpower;
			pawn.health.RemoveHediff(this);
			// See if the pawn wants to join on emancipation
			//if (!pawn.GetRoom ().isPrisonCell) {
			//	if (willpower <= 1 || // Broken slaves will typically join
			//		(willpower <= 25 && pawn.needs.mood.CurLevelPercentage > Rand.Range(0.55f,0.95f)) &&
			//		pawn.story.traits.allTraits.Find(x => x.def == TraitDefOf.Nerves && x.Degree > 0) == null // Iron-willed/steadfast pawns never join on emancipation
			//	) {// Join the colony
			//		pawn.guest.isPrisonerInt = false;
			//		pawn.SetFaction (slaverFaction);
			//	}
			//}
		}

		// Save and load all our data
		public override void ExposeData() {
			base.ExposeData();
			Scribe_References.Look<Faction>(ref slaverFaction, "slaverFaction");
			Scribe_References.Look<Faction>(ref actualFaction, "actualFaction");
			Scribe_Values.Look<float>(ref willpower, "slaveWillpower", 100);
			Scribe_Values.Look<int>(ref hoursSinceLastEscapeAttempt, "escapeHours", 3);
			Scribe_Values.Look<bool>(ref waitingInJail, "waitingInJail", false);
			Scribe_Values.Look<bool>(ref toBeFreed, "toBeFreed", false);
			Scribe_Values.Look<bool>(ref shackledGoal, "shackledGoal", false);
			Scribe_Values.Look<bool>(ref shackled, "shackled", false);
		}

		// Hidden effect
		public override bool Visible {
			get { return false; }
		}
	}
	public class Hediff_CryptoStasis : HediffWithComps {
		public MentalStateDef revertMentalStateDef;

		public void SaveMemory() {
			if (pawn.mindState.mentalStateHandler.CurStateDef == SS_MentalStateDefOf.CryptoStasis)
				revertMentalStateDef = MentalStateDefOf.Berserk;

			else
				revertMentalStateDef = pawn.mindState.mentalStateHandler.CurStateDef;
		}

		public override void ExposeData() {
			base.ExposeData();
			Scribe_Defs.Look<MentalStateDef>(ref revertMentalStateDef, "revertMentalStateDef");
		}
	}
}

