using System;
using System.Linq;
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
			int num = SlaveUtility.GetSlavesMiserable().Count();
			string text = "";
			if (num > 1)
				text = "Desc_MiserableSlavesPlural".Translate();
			else if (num == 1)
				text = "Desc_MiserableSlaves".Translate();

			return text;
		}

		public override AlertReport GetReport() {
			return AlertReport.CulpritsAre(SlaveUtility.GetSlavesMiserable());
		}
	}
}
