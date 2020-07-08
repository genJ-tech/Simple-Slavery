using System.Text;
using RimWorld;
using Verse;

namespace SimpleSlavery {
	class Alert_Timeout : Alert {
		public Alert_Timeout() {
			defaultPriority = AlertPriority.Medium;
		}

		public override string GetLabel() {
			return "Label_Timeout".Translate();
		}

		public override TaggedString GetExplanation() {
			var slaves = SlaveUtility.GetSlavesInTimeout();
			string text = "";
			if (slaves.Count > 1) {
				StringBuilder stringBuilder = new StringBuilder();
				foreach (var slave in slaves) {
					stringBuilder.AppendLine("  - " + slave.NameShortColored.Resolve());
				}
				text = "Desc_TimeoutPlural".Translate(stringBuilder).Resolve();
			} else {
				text = "Desc_Timeout".Translate(slaves[0].NameShortColored.Resolve()).Resolve();
			}
			return text;
		}

		public override AlertReport GetReport() {
			return AlertReport.CulpritsAre(SlaveUtility.GetSlavesInTimeout());
		}
	}
}
