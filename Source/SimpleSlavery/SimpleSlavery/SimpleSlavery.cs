using HugsLib;

namespace SimpleSlavery {
	class SimpleSlavery : ModBase {
		public static SimpleSlavery Inst { get; private set; }
		public static HugsLib.Utils.ModLogger InstLogger => Inst.Logger;

		public override string ModIdentifier => "syl.simpleslavery";

		SimpleSlavery() {
			Inst = this;
		}

		public override void WorldLoaded() {
			base.WorldLoaded();
			// Update willpower from old range to new
			if (SlaveUtility.SlaveData.Compat < 1) {
				foreach (var slave in SlaveUtility.GetSlaves()) {
					var hediff = SlaveUtility.GetEnslavedHediff(slave);
					hediff.SetWillpowerDirect(hediff.SlaveWillpower / 100f);
				}
			}
		}
	}
}
