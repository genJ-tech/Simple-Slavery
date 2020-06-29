using System;
using RimWorld;
using Verse;

namespace BorgNames
{
	public class Hediff_BorgInfection : HediffWithComps
	{
		public override void Notify_PawnDied ()
		{
			base.Notify_PawnDied ();
			Corpse corpse = pawn.Corpse;
			Pawn newBorg = PawnGenerator.GeneratePawn (PawnKindDef.Named ("BorgDrone"), FactionUtility.DefaultFactionFrom (FactionDef.Named ("BorgFaction")));
			newBorg.SpawnSetup (corpse.Map);
			newBorg.Position = corpse.Position;
			if (corpse != null)
				corpse.Destroy ();
		}
	}
}

