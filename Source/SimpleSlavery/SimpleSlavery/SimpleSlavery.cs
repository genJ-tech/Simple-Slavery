using HugsLib;
using HugsLib.Settings;
using Verse;

namespace SimpleSlavery {
	class SimpleSlavery : ModBase {
		public static SimpleSlavery Inst { get; private set; }
		public static HugsLib.Utils.ModLogger InstLogger => Inst.Logger;
		public static float SlaveValue => Inst.slaveValue.Value;
		public static bool EscapesEnabled => Inst.escapesEnabled.Value;
		public static bool ShowSlavesInColonistBar => Inst.showSlavesInColonistBar.Value;
		public static float WillpowerFallRate => Inst.willpowerFallRate.Value;

		private SettingHandle<float> slaveValue;
		private SettingHandle<bool> escapesEnabled;
		private SettingHandle<bool> showSlavesInColonistBar;
		private SettingHandle<float> willpowerFallRate;

		public override string ModIdentifier => "syl.simpleslavery";

		SimpleSlavery() {
			Inst = this;
		}

		public override void DefsLoaded() {
			slaveValue = Settings.GetHandle("ssSlaveValue",
				"slaveValueSetting_title".Translate(),
				"slaveValueSetting_desc".Translate(),
				0.5f,
				Validators.FloatRangeValidator(0f, 2f));
			escapesEnabled = Settings.GetHandle("ssEscapesEnabled",
				"escapesEnabledSetting_title".Translate(),
				"escapesEnabledSetting_desc".Translate(),
				true);
			showSlavesInColonistBar = Settings.GetHandle("ssShowSlavesInColonistBar",
				"showSlavesInColonistBar_title".Translate(),
				"showSlavesInColonistBar_desc".Translate(),
				false);
			willpowerFallRate = Settings.GetHandle("ssWillpowerFallRate",
				"willpowerFallRate_title".Translate(),
				"willpowerFallRate_desc".Translate(),
				1f,
				Validators.FloatRangeValidator(0f, 10f));
		}

		public override void SettingsChanged() {
			if (Find.World != null) { // Refresh colonist bar if in a game
				Find.ColonistBar.MarkColonistsDirty();
			}
		}

		public override void WorldLoaded() {
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
