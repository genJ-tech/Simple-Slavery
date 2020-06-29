using Verse;
using RimWorld;

namespace SimpleSlavery {
	[DefOf]
	public static class SS_HediffDefOf {
		static SS_HediffDefOf() {
			DefOfHelper.EnsureInitializedInCtor(typeof(HediffDefOf));
		}
		public static HediffDef Crypto_Stasis;

		public static HediffDef Electrocuted;

		public static HediffDef Enslaved;

		public static HediffDef Hediff_EmancipateFix;

		public static HediffDef SlaveMemory;
	}
	[DefOf]
	public static class SS_JobDefOf {
		static SS_JobDefOf() {
			DefOfHelper.EnsureInitializedInCtor(typeof(JobDefOf));
		}
		public static JobDef EmancipateSlave;

		public static JobDef EnslavePrisoner;

		public static JobDef ShackleSlave;
	}
	[DefOf]
	public static class SS_MentalStateDefOf {
		static SS_MentalStateDefOf() {
			DefOfHelper.EnsureInitializedInCtor(typeof(MentalStateDefOf));
		}
		public static MentalStateDef CryptoStasis;
	}
	[DefOf]
	public static class SS_PrisonerInteractionModeDefOf {
		static SS_PrisonerInteractionModeDefOf() {
			DefOfHelper.EnsureInitializedInCtor(typeof(PrisonerInteractionModeDefOf));
		}
		public static PrisonerInteractionModeDef PIM_Enslave;
	}
	[DefOf]
	public static class SS_ThingDefOf {
		static SS_ThingDefOf() {
			DefOfHelper.EnsureInitializedInCtor(typeof(ThingDefOf));
		}
		public static ThingDef Apparel_SlaveCollar;
	}
	[DefOf]
	public static class SS_ThoughtDefOf {
		static SS_ThoughtDefOf() {
			DefOfHelper.EnsureInitializedInCtor(typeof(ThoughtDefOf));
		}
		public static ThoughtDef EnslavedThought;

		public static ThoughtDef ExplosiveCollar;

		public static ThoughtDef KnowFellowSlaveDied;

		public static ThoughtDef KnowSlaveDied;

		public static ThoughtDef SlaveColonyThought;
	}

	public static class SS_WorkGiverDefOf {
		static SS_WorkGiverDefOf() {
			DefOfHelper.EnsureInitializedInCtor(typeof(WorkGiverDefOf));
		}
		public static WorkGiverDef DoEmancipate;

		public static WorkGiverDef DoEnslavement;

		public static WorkGiverDef DoShackling;
	}
}