using System.Text;
using RimWorld;
using Verse;

namespace SimpleSlavery {
	public class Alert_MiserableSlaves : Alert {
		public Alert_MiserableSlaves() {
			this.defaultPriority = AlertPriority.High;
		}

		public override string GetLabel() {
			return "Label_MiserableSlaves".Translate();
		}

		public override TaggedString GetExplanation() {
			var miserable = SlaveUtility.GetSlavesMiserable();
			int num = miserable.Count;
			string text = "";
			if (num > 1) {
				StringBuilder stringBuilder = new StringBuilder();
				foreach (var slave in miserable) {
					stringBuilder.AppendLine("  - " + slave.NameShortColored.Resolve());
				}
				text = "Desc_MiserableSlavesPlural".Translate(stringBuilder).Resolve();
			} else if (num == 1) {
				text = "Desc_MiserableSlaves".Translate(miserable[0].NameShortColored.Resolve()).Resolve();
			}

			return text;
		}

		public override AlertReport GetReport() {
			return AlertReport.CulpritsAre(SlaveUtility.GetSlavesMiserable());
		}
	}
}
