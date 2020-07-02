using RimWorld.Planet;
using Verse;

namespace SimpleSlavery {
	public class WorldSlaveData : WorldComponent {
		private const int CurrentCompatVersion = 1;
		private int compat;

		public int Compat => compat;

		public WorldSlaveData(World world) : base(world) { }

		public override void ExposeData() {
			Scribe_Values.Look(ref compat, "compat", CurrentCompatVersion);

			if (compat > CurrentCompatVersion) {
				SimpleSlavery.InstLogger.Error("Downgraded compat version from {0} to {1}! This will cause issues! Update Simple Slavery to avoid issues!", compat, CurrentCompatVersion);
			}
			compat = CurrentCompatVersion;
		}
	}
}
