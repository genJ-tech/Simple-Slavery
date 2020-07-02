using System;
using RimWorld;
using Verse;
using System.Collections.Generic;

namespace SimpleSlavery {
	public static class SlaveUtility {
		public static WorldSlaveData SlaveData => Find.World.GetComponent<WorldSlaveData>();

		public static void EnslavePawn(Pawn pawn, Apparel collar = null) {
			if (pawn == null) {
				Log.Error("[SimpleSlavery] Error: Tried to enslave null pawn.");
				return;
			}
			if (!pawn.RaceProps.Humanlike) {
				Log.Error("[SimpleSlavery] Error: Tried to enslave a non-humanlike pawn.");
				return;
			}
			if (!SlaveUtility.IsPawnColonySlave(pawn)) {
				SlaveUtility.GiveSlaveCollar(pawn, collar);
				pawn.health.AddHediff(SS_HediffDefOf.Enslaved);
			}
		}

		public static void EmancipatePawn(Pawn pawn) {
			if (IsPawnColonySlave(pawn))
				(pawn.health.hediffSet.GetFirstHediffOfDef(SS_HediffDefOf.Enslaved) as Hediff_Enslaved).Emancipate();
		}

		public static bool IsPawnColonySlave(Pawn pawn) {
			return pawn.health.hediffSet.HasHediff(SS_HediffDefOf.Enslaved);
		}

		public static Hediff_Enslaved GetEnslavedHediff(Pawn pawn) {
			return pawn.health.hediffSet.GetFirstHediffOfDef(SS_HediffDefOf.Enslaved) as Hediff_Enslaved;
		}

		public static Hediff_SlaveMemory GetSlaveMemoryHediff(Pawn pawn) {
			return pawn.health.hediffSet.GetFirstHediffOfDef(SS_HediffDefOf.SlaveMemory) as Hediff_SlaveMemory;
		}

		public static bool IsSlaveCollar(Apparel apparel) {
			if (apparel == null) { return false; }
			return apparel.def.defName.Contains("SlaveCollar");
		}

		public static bool HasSlaveCollar(Pawn pawn) {
			if (pawn.apparel == null)
				return false;
			foreach (Apparel item in pawn.apparel.WornApparel) {
				if (IsSlaveCollar(item))
					return true;
			}
			return false;
		}

		public static Apparel GetSlaveCollar(Pawn pawn) {
			return HasSlaveCollar(pawn) ? pawn.apparel.WornApparel.Find(IsSlaveCollar) : null;
		}

		public static Apparel MakeRandomSlaveCollar() {
			var stuff = new List<ThingDef>{
								ThingDefOf.Steel,
								ThingDefOf.Silver,
				//ThingDef.Named ("Gold"), //Z- Slaves were spawning with collars more valuable than they were
				ThingDefOf.Plasteel,
								ThingDefOf.Uranium
						};
			int chance = (int)Math.Round(Math.Pow(Rand.Value, Math.PI) * (stuff.Count - 1));
			var slaveCollar = ThingMaker.MakeThing(SS_ThingDefOf.Apparel_SlaveCollar, stuff[chance]) as Apparel;
			return slaveCollar;
		}

		public static void GiveSlaveCollar(Pawn pawn, Apparel collar = null) {
			if (pawn == null) {
				Log.Error("Tried to give a collar to a null pawn.");
				return;
			}
			Apparel newCollar = collar;
			if (newCollar == null)
				newCollar = MakeRandomSlaveCollar();
			pawn.apparel.Wear(newCollar, true);
			if (pawn.outfits == null)
				pawn.outfits = new Pawn_OutfitTracker();
			pawn.outfits.forcedHandler.SetForced(newCollar, true);
		}

		public static List<Pawn> GetSlaves() {
			List<Pawn> pawns = new List<Pawn>();
			foreach (Pawn pawn in PawnsFinder.AllMaps_FreeColonistsSpawned) {
				if (IsPawnColonySlave(pawn)) {
					pawns.Add(pawn);
				}
			}
			return pawns;
		}

		public static List<Pawn> GetAllSlaves() {
			List<Pawn> pawns = new List<Pawn>();
			foreach (Pawn pawn in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists) {
				if (IsPawnColonySlave(pawn)) {
					pawns.Add(pawn);
				}
			}
			return pawns;
		}

		public static List<Pawn> GetSlavesMiserable() {
			List<Pawn> pawns = new List<Pawn>();
			foreach (Pawn pawn in GetSlaves()) {
				if (!pawn.Downed && (pawn.mindState.mentalBreaker.BreakMajorIsImminent || pawn.mindState.mentalBreaker.BreakExtremeIsImminent)) {
					pawns.Add(pawn);
				}
			}
			return pawns;
		}

		public static void TryInstantBreak(Pawn pawn, float chance, MentalStateDef breakDef) {
			if (pawn.Downed || pawn.jobs.curDriver.asleep || pawn.InMentalState) return;
			if (Rand.Chance(chance))
				pawn.mindState.mentalStateHandler.TryStartMentalState(breakDef, "ReasonArmedExplosiveCollar".Translate(pawn.Name.ToStringShort));
		}
		public static void TryInstantBreak(Pawn pawn, float chance) {
			if (pawn.InMentalState) return;
			TryInstantBreak(pawn, chance, MentalStateDefOf.Berserk);
		}

		public static void TryHeartAttack(Pawn pawn) {
			int age = pawn.ageTracker.AgeBiologicalYears;

			const float youngAge = 30f;

			float oldAge = pawn.RaceProps.lifeExpectancy;

			const float minChance = 0.0001f;

			const float maxChance = 0.01f;

			float chance = Math.Max(((Math.Min(Math.Max(age, youngAge), oldAge) - youngAge) / (oldAge - youngAge)) * maxChance, minChance);

			//Log.Message("Chance was : " + chance.ToStringSafe());

			BodyPartRecord heart = pawn.RaceProps.body.AllParts.Find(part => part.def == BodyPartDefOf.Heart);

			if (heart != null && Rand.Chance(chance)) {
				pawn.health.AddHediff(HediffDef.Named("HeartAttack"), heart);
				string text = "LetterIncidentECHeartAttack".Translate(pawn.Name.ToString());
				Find.LetterStack.ReceiveLetter("LetterLabelECHeartAttack".Translate(), text, LetterDefOf.NegativeEvent, null);
			}
		}
	}
}

