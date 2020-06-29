using System;
using HarmonyLib;
using RimWorld;
using Verse;
using HugsLib;
using System.Reflection;
using Verse.AI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Reflection.Emit;
using RimWorld.Planet;

namespace SimpleSlavery {
	public class SlaveryBase : ModBase {
		public override string ModIdentifier {
			get {
				return "SimpleSlavery";
			}
		}
	}

	[StaticConstructorOnStartup]
	internal static class HarmonyPatches {

		static HarmonyPatches() {
			var harmonyInstance = new Harmony("rimworld.thirite.simpleslavery");
			Harmony.DEBUG = false;
			// Break Risk Alert patches
			MethodInfo breakRiskAlertUtility_transpiler = AccessTools.Method(typeof(BRAU_Patches), "BreakRiskAlertUtility_Transpiler");
			harmonyInstance.Patch(typeof(BreakRiskAlertUtility).GetProperty("PawnsAtRiskExtreme").GetMethod, null, null, new HarmonyMethod(breakRiskAlertUtility_transpiler));
			harmonyInstance.Patch(typeof(BreakRiskAlertUtility).GetProperty("PawnsAtRiskMajor").GetMethod, null, null, new HarmonyMethod(breakRiskAlertUtility_transpiler));
			harmonyInstance.Patch(typeof(BreakRiskAlertUtility).GetProperty("PawnsAtRiskMinor").GetMethod, null, null, new HarmonyMethod(breakRiskAlertUtility_transpiler));
			// Alert Thought patch
			MethodInfo alertThought_transpiler = AccessTools.Method(typeof(Alert_Thought_Patch), "Alert_Thought_Transpiler");
			harmonyInstance.Patch(typeof(Alert_Thought).GetProperty("AffectedPawns", AccessTools.all).GetMethod, null, null, new HarmonyMethod(alertThought_transpiler));
			// Colonist Bar
			MethodInfo checkRecacheEntries_transpiler = AccessTools.Method(typeof(CheckRecacheEntries_Patch), "CheckRecacheEntries_Transpiler");
			harmonyInstance.Patch(typeof(ColonistBar).GetMethod("CheckRecacheEntries", AccessTools.all), null, null, new HarmonyMethod(checkRecacheEntries_transpiler));
			// Death Thoughts
			//MemoryThoughtHandler_TryGainMemory_Transpiler
			MethodInfo deathThought_postfix = AccessTools.Method(typeof(MemoryThoughtHandler_Patch), "TryGainMemory_Postfix");
			harmonyInstance.Patch(AccessTools.Method(typeof(MemoryThoughtHandler), "TryGainMemory", new[] { typeof(Thought_Memory), typeof(Pawn) }), null, new HarmonyMethod(deathThought_postfix), null);
		}
	}

	public static class SS_Helper {
		public static float PrisonerInteractionModeDefCount() {
			return DefDatabase<PrisonerInteractionModeDef>.DefCount * 32f;
		}
		public static bool TrySlaveMentalBreak(Pawn pawn) {
			if (pawn.needs.mood.CurInstantLevel < pawn.mindState.mentalBreaker.BreakThresholdExtreme) {
				// TODO: Violent slave uprising

			}
			return true;
		}

		public static List<Pawn> ClearOutSlavesFromColonistBar(List<Pawn> pawns) {
			var newList = new List<Pawn> { };
			foreach (Pawn pawn in pawns) {
				if (!SlaveUtility.IsPawnColonySlave(pawn)) {
					newList.Add(pawn);
				}
			}
			return newList;
		}
		public static bool DoesMapHaveSlaves(Map map) {
			if (map == null) return false;
			bool slaveExists = false;
			foreach (Pawn pawn in map.mapPawns.FreeColonists) {
				if (SlaveUtility.IsPawnColonySlave(pawn)) {
					slaveExists = true;
					break;
				}
			}
			return slaveExists;
		}
		public static int MapsWithSlavesCount() {
			int num = 0;
			foreach (Map map in Find.Maps) {
				if (DoesMapHaveSlaves(map)) num++;
			}
			return num;
		}

		public static void ModifyDeathThoughts(MemoryThoughtHandler handler, ThoughtDef def, Pawn otherPawn) {
			ThoughtDef slaveDiedDef = SS_ThoughtDefOf.KnowSlaveDied;
			//handler.TryGainMemory((Thought_Memory)ThoughtMaker.MakeThought(slaveDiedDef), otherPawn);

		}
	}

	// Add a suffix onto PostApplyDamage to take beatings into account for slave willpower
	[HarmonyPatch(typeof(Pawn), "PostApplyDamage")]
	public static class Pawn_PostApplyDamage_Patch {

		[HarmonyPostfix]
		public static void Beaten(Pawn __instance, ref DamageInfo dinfo) {
			// Check if the pawn is enslaved
			if (SlaveUtility.IsPawnColonySlave(__instance)) {
				Hediff_Enslaved enslaved_def = (Hediff_Enslaved)__instance.health.hediffSet.GetFirstHediffOfDef(SS_HediffDefOf.Enslaved);
				// Is the beating coming from the faction owning the slave?
				if (dinfo.Instigator != null)
					if (dinfo.Instigator.Faction == enslaved_def.slaverFaction) {
						enslaved_def.TakeWillpowerHit(dinfo.Amount);
					}
			}
		}
	}

	// Causes a slave to take a large willpower hit when caught after trying to escape
	[HarmonyPatch(typeof(JobDriver_TakeToBed), "CheckMakeTakeePrisoner")]
	public static class JobDriver_TakeToBed_Patch {

		static readonly PropertyInfo prisonerProp = AccessTools.Property(typeof(JobDriver_TakeToBed), "Takee");

		[HarmonyPostfix]
		public static void TakeToBed_Postfix(JobDriver_TakeToBed __instance) {
			var prisoner = (Pawn)prisonerProp.GetValue(__instance, null);
			if (prisoner.health.hediffSet.HasHediff(SS_HediffDefOf.Enslaved)) {
				// Caught the slave
				((Hediff_Enslaved)prisoner.health.hediffSet.GetFirstHediffOfDef(SS_HediffDefOf.Enslaved)).CaughtSlave();
			}
		}
	}

	// Adds the "emancipate" and "shackle" toggles to slaves' gizmos
	[HarmonyPatch(typeof(Pawn), "GetGizmos")]
	public static class Pawn_GetGizmos_Patch {
		[HarmonyPostfix]
		public static void Pawn_GetGizmos_Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result) {
			__result = __result.Concat<Gizmo>(slaveGizmos(__instance));
		}

		internal static IEnumerable<Gizmo> slaveGizmos(Pawn pawn) {
			// Return slave apparel gizmos when escaping
			if (pawn.apparel != null) {
				for (int i = 0; i < pawn.apparel.WornApparel.Count; i++) {

					var slaveApparel = pawn.apparel.WornApparel[i] as SlaveApparel;
					if (slaveApparel != null) {
						foreach (Gizmo g in slaveApparel.SlaveGizmos()) yield return g;
					}
				}
			}

			if (SlaveUtility.IsPawnColonySlave(pawn)) {
				var freeSlave = new Command_Toggle();
				Func<bool> toBeFreed = () => SlaveUtility.GetEnslavedHediff(pawn).toBeFreed;
				freeSlave.isActive = toBeFreed;
				freeSlave.defaultLabel = "LabelWordEmancipate".Translate();
				freeSlave.defaultDesc = "CommandDescriptionEmancipate".Translate(pawn.Name.ToStringShort); //Z- NameStringShort -> Name.ToStringShort
				freeSlave.toggleAction = delegate {
					SlaveUtility.GetEnslavedHediff(pawn).toBeFreed = !SlaveUtility.GetEnslavedHediff(pawn).toBeFreed;
					//Log.Message("Free slave " + pawn.Name.ToStringShort + ": " + SlaveUtility.GetEnslavedHediff(pawn).toBeFreed.ToStringYesNo()); //Z- NameStringShort -> Name.ToStringShort
				};
				freeSlave.alsoClickIfOtherInGroupClicked = true;
				freeSlave.activateSound = SoundDefOf.Click;
				freeSlave.icon = ContentFinder<Texture2D>.Get("UI/Commands/Emancipate", true);
				yield return freeSlave;

				var shackleSlave = new Command_Toggle();
				shackleSlave.isActive = () => SlaveUtility.GetEnslavedHediff(pawn).shackledGoal;
				shackleSlave.defaultLabel = "LabelWordShackle".Translate();
				shackleSlave.defaultDesc = "CommandDescriptionShackle".Translate(pawn.Name.ToStringShort); //Z- NameStringShort -> Name.ToStringShort
				shackleSlave.toggleAction = delegate {
					SlaveUtility.GetEnslavedHediff(pawn).shackledGoal = !SlaveUtility.GetEnslavedHediff(pawn).shackledGoal;
					//Log.Message("Shackle slave " + pawn.Name.ToStringShort + ": " + SlaveUtility.GetEnslavedHediff(pawn).toBeFreed.ToStringYesNo()); //Z- NameStringShort -> Name.ToStringShort
				};
				shackleSlave.alsoClickIfOtherInGroupClicked = true;
				shackleSlave.activateSound = SoundDefOf.Click;
				shackleSlave.icon = ContentFinder<Texture2D>.Get("UI/Commands/Shackle", true);
				yield return shackleSlave;
			}
		}
	}

	// Changes the behaviour of restraints to acknowledge shackled slaves
	[HarmonyPatch(typeof(RestraintsUtility), "InRestraints")]
	public static class RestraintUtility_Patch {
		[HarmonyPostfix]
		public static void InRestraints_Patch(ref Pawn pawn, ref bool __result) {
			// Pawn is a shackled slave
			if (SlaveUtility.IsPawnColonySlave(pawn))
				__result = SlaveUtility.GetEnslavedHediff(pawn).shackled;
		}
	}
	[HarmonyPatch(typeof(RestraintsUtility), "ShouldShowRestraintsInfo")]
	public static class RestraintUtility_Show_Patch {
		[HarmonyPostfix]
		public static void ShouldShowRestraintsInfo_Patch(ref Pawn pawn, ref bool __result) {
			if (RestraintsUtility.InRestraints(pawn) && SlaveUtility.IsPawnColonySlave(pawn)) {
				__result = true;
			}
		}
	}

	// Stops slaves from trying to optimize apparel & Stops Colonists from trying to optimize apparel
	[HarmonyPatch(typeof(JobGiver_OptimizeApparel), "TryGiveJob")]
	public static class OptimizeApparel_Patch {
		[HarmonyPostfix]
		public static void TryGiveJob_Patch(ref Pawn pawn, ref Job __result) {
			if (__result == null) return;
			if ((SlaveUtility.IsPawnColonySlave(pawn) && __result.targetA.Thing != null && SlaveUtility.IsSlaveCollar(__result.targetA.Thing as Apparel) && SlaveUtility.HasSlaveCollar(pawn)) ||
				 (!SlaveUtility.IsPawnColonySlave(pawn) && pawn.IsColonist && __result.targetA.Thing != null && SlaveUtility.IsSlaveCollar(__result.targetA.Thing as Apparel))) {
				__result = null;
			}
		}
	}

	// Hides the Prisoner tab from slaves (which are not currently player faction, eg: running away)
	[HarmonyPatch(typeof(ITab_Pawn_Prisoner))]
	[HarmonyPatch("IsVisible", MethodType.Getter)] //Z- PropertyMethod -> MethodType
	public static class PrisonerTab_IsVisible_Patch {
		[HarmonyPostfix]
		public static void IsVisible_Patch(ref ITab_Pawn_Prisoner __instance, ref bool __result) {
			var pawn = Find.Selector.SingleSelectedThing;
			if (pawn == null || pawn.GetType() != typeof(Pawn))
				return;

			if (__result == true) {
				if (SlaveUtility.IsPawnColonySlave(pawn as Pawn))
					__result = false;
			}
		}
	}

	// Adds the chain icon to the portrait of a slave
	[HarmonyPatch(typeof(PawnRenderer), "RenderPortrait")] //Z- RenderPortait -> RenderPortrait (Tynan fixed a typo)
	public static class PR_RP_Patch {
		[HarmonyPostfix]
		public static void RenderPortrait_Patch(ref PawnRenderer __instance) {
			Pawn pawn = __instance.graphics.pawn;
			if (!SlaveUtility.IsPawnColonySlave(pawn))
				return;
			Apparel collar = pawn.apparel.WornApparel.Find(SlaveUtility.IsSlaveCollar);
			var chainColour = new Color(0.5f, 0.5f, 0.5f, 0.5f);
			if (collar != null)
				chainColour = collar.DrawColor;

			Mesh mesh = MeshPool.humanlikeHeadSet.MeshAt(Rot4.South);
			Material mat = GraphicDatabase.Get<Graphic_Single>("Things/Pawn/Humanlike/Apparel/SlaveCollar/Chain", ShaderDatabase.CutoutComplex, new Vector2(1, 1), chainColour).MatAt(Rot4.South);
			mat.mainTexture.wrapMode = TextureWrapMode.Clamp;
			mat.mainTextureScale = new Vector2(1.75f, 1.75f);
			mat.mainTextureOffset = new Vector2(-0.37f, -0.10f);
			GenDraw.DrawMeshNowOrLater(mesh, new Vector3(0, 0.85f, 0), Quaternion.identity, mat, true);
		}
	}

	// Changes the label of a pawn to slave if applicable //Z- 1.0 Changed alot in BestKindLabel, making it overloaded and also changed PawnMainDescGendered
	[HarmonyPatch(typeof(GenLabel), "BestKindLabel", new Type[] { typeof(Pawn), typeof(bool), typeof(bool), typeof(bool), typeof(int) })] //Z- "BestKindLabel" -> "BestKindLabel", new Type[] { typeof(Pawn), typeof(bool), typeof(bool), typeof(bool), typeof(int)})]
	public static class GenLabel_BKL_Patch {
		[HarmonyPostfix]
		public static void BestKindLabel_Patch(ref string __result, ref Pawn pawn, ref bool mustNoteGender) {
			if (mustNoteGender && pawn.gender != Gender.None && SlaveUtility.IsPawnColonySlave(pawn)) {
				__result = "PawnMainDescGendered".Translate(pawn.Named("PAWN"), "LabelWordSlave".Translate()); //Z- ( new object[] { pawn.gender.GetLabel() ->(pawn.Named("PAWN")
			}
		}
	}

	// Adds owned slaves to sellable things 
	[HarmonyPatch(typeof(TradeUtility), "AllSellableColonyPawns")]
	public static class TradeUtility_ASCP_Patch {
		static IEnumerable<Pawn> sellableSlaves(Map map, IEnumerable<Pawn> previous) {
			foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned) {
				if (SlaveUtility.IsPawnColonySlave(pawn) && !previous.Contains(pawn))
					yield return pawn;
			}
		}

		[HarmonyPostfix]
		public static void ASCP_Patch(ref IEnumerable<Pawn> __result, ref Map map) {
			__result = sellableSlaves(map, __result);
		}
	}

	// Adds owned slaves to sellable things on world map
	[HarmonyPatch(typeof(Settlement_TraderTracker), "ColonyThingsWillingToBuy")]
	public static class Settlement_TraderTracker_ASCP_Patch {
		static IEnumerable<Thing> sellableThingsAndSlaves(Pawn playerNegotiator, IEnumerable<Thing> previous) {
			foreach (Thing thing in previous) yield return thing;
			Caravan caravan = playerNegotiator.GetCaravan();
			List<Pawn> caravanPawns = caravan.PawnsListForReading;
			foreach (Pawn pawn in caravanPawns) {
				if (SlaveUtility.IsPawnColonySlave(pawn) && !previous.Contains(pawn))
					yield return pawn;
			}
		}

		[HarmonyPostfix]
		public static void ASCP_Patch2(ref IEnumerable<Thing> __result, ref Pawn playerNegotiator) {
			__result = sellableThingsAndSlaves(playerNegotiator, __result);
		}
	}

	// Adds a slave collar to generated slaves
	[HarmonyPatch(typeof(PawnGenerator), "GenerateGearFor")]
	public static class PawnGenerator_GP_Patch {
		[HarmonyPostfix]
		public static void GeneratePawn_Patch(ref PawnGenerationRequest request, ref Pawn pawn) {
			//Z- Uses defName.Contains for Alien Race compatibilty
			if (request.KindDef.defName.Contains("Slave") && pawn.apparel.WornApparel.Find(SlaveUtility.IsSlaveCollar) == null) {
				SlaveUtility.GiveSlaveCollar(pawn);
			}
		}
	}

	// Patch for when a slave is sold
	[HarmonyPatch(typeof(Pawn), "PreTraded")]
	public static class Pawn_PreTraded_Patch {
		[HarmonyPostfix]
		public static void PreTraded_Patch(ref Pawn __instance, ref TradeAction action) {
			// Slave wearing a slave collar
			if (action == TradeAction.PlayerBuys && __instance.RaceProps.Humanlike && __instance.apparel.WornApparel.Find(SlaveUtility.IsSlaveCollar) != null) {
				// Add the enslaved tracker
				__instance.health.AddHediff(SS_HediffDefOf.Enslaved);
				// Set willpower to zero
				SlaveUtility.GetEnslavedHediff(__instance).SetWillpowerDirect(0);
				// Re-force wearing of the collar so the new slave does not drop it, freeing themselves
				__instance.outfits.forcedHandler.SetForced(__instance.apparel.WornApparel.Find(SlaveUtility.IsSlaveCollar), true);
			}
		}
	}


	// On capture
	[HarmonyPatch(typeof(Pawn), "CheckAcceptArrest")]
	public static class Pawn_CheckAcceptArrest_Patch {
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> CheckArrest_Transpiler(IEnumerable<CodeInstruction> instructions) {
			foreach (CodeInstruction instruction in instructions) {
				MethodInfo slaveChance = typeof(SS_ArrestChance).GetMethod("ArrestChance", AccessTools.all);
				if (instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 0.6f) {
					yield return new CodeInstruction(OpCodes.Ldarg_0) { labels = instruction.labels };
					yield return new CodeInstruction(OpCodes.Call, slaveChance);
				} else
					yield return instruction;
			}
		}
	}
	public static class SS_ArrestChance {
		public static float ArrestChance(Pawn pawn) {
			if (pawn.mindState.mentalStateHandler.CurStateDef == SS_MentalStateDefOf.CryptoStasis)
				return 1;
			if (SlaveUtility.IsPawnColonySlave(pawn))
				return 1 - SlaveUtility.GetEnslavedHediff(pawn).SlaveWillpower / 100;
			else
				return 0.6f;
		}
	}

	// Modifies the Prisoner Interaction Mode selection box to dynamically resize itself to the number
	// of available PrisonerInteractionModeDefs, rather than be the hardcoded height of 160
	[HarmonyPatch(typeof(ITab_Pawn_Visitor), "FillTab")]
	public static class ITab_PawnVisitor_FillTab_Patch {
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> FillTab_Transpiler(IEnumerable<CodeInstruction> instructions) {
			MethodInfo Method_PIMD_Height = typeof(SS_Helper).GetMethod("PrisonerInteractionModeDefCount");
			foreach (CodeInstruction inst in instructions) {
				if (inst.opcode == OpCodes.Ldc_R4 && inst.operand.ToStringSafe() == "160") {
					yield return new CodeInstruction(OpCodes.Call, Method_PIMD_Height);
				} else
					yield return inst;
			}
		}
	}

	// Patch for when a slave tries to execute a mental break. Detours the normal method
	// if the pawn is a slave, rerouting to our own "TrySlaveMentalBreak" - Thirite
	[HarmonyPatch(typeof(MentalBreakWorker), "TryStart")]
	public static class MentalBreakWorker_TryStart_Patch {
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> TryStart_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ILgen) {
			MethodInfo method_tryStartMentalState = typeof(MentalStateHandler).GetMethod("TryStartMentalState");
			MethodInfo method_trySlaveMentalBreak = typeof(SS_Helper).GetMethod("TrySlaveMentalBreak");

			Label notColonySlave = ILgen.DefineLabel();
			var codeList = instructions.ToList();

			var injection1 = new List<CodeInstruction> {
				//Check if pawn is colony slave
				new CodeInstruction(OpCodes.Ldarg_1),
				new CodeInstruction(OpCodes.Call, typeof(SlaveUtility).GetMethod("IsPawnColonySlave")),
				new CodeInstruction(OpCodes.Brfalse, notColonySlave),
				// Call slave mental breaker
				new CodeInstruction(OpCodes.Ldarg_1),
				new CodeInstruction(OpCodes.Call, method_trySlaveMentalBreak),
				new CodeInstruction(OpCodes.Ret),
				new CodeInstruction(OpCodes.Nop){labels = new List<Label>{notColonySlave}},
			};
			codeList.InsertRange(0, injection1);
			foreach (CodeInstruction inst in codeList) {
				yield return inst;
			}
		}
	}

	public static class Alert_Thought_Patch {
		static IEnumerable<CodeInstruction> Alert_Thought_Transpiler(IEnumerable<CodeInstruction> instructions) {
			//Type iterator = typeof(Alert_Thought).GetNestedType("<AffectedPawns>c__Iterator0", AccessTools.all);
			//FieldInfo pawn = AccessTools.Field(iterator, "<p>__1");
			List<CodeInstruction> ILs = instructions.ToList();
			int injectIndex = ILs.FindIndex(IL => IL.opcode == OpCodes.Bne_Un_S) + 1;
			var jump = (Label)ILs.Find(il => il.opcode == OpCodes.Bne_Un_S).operand;
			var injection = new List<CodeInstruction> {
				new CodeInstruction(OpCodes.Ldloc_1),
				new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SlaveUtility), "IsPawnColonySlave")),
				new CodeInstruction(OpCodes.Brtrue, jump),
				};
			ILs.InsertRange(injectIndex, injection);
			foreach (CodeInstruction IL in ILs)
				yield return IL;
		}
	}

	public static class CheckRecacheEntries_Patch {
		static IEnumerable<CodeInstruction> CheckRecacheEntries_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ILgen) {
			List<CodeInstruction> ILs = instructions.ToList();

			int injectIndex;
			List<CodeInstruction> injection;

			// Injection 1
			injectIndex = ILs.FindIndex(IL => IL.opcode == OpCodes.Call && IL.operand == typeof(PlayerPawnsDisplayOrderUtility).GetMethod("Sort")) - 1;
			injection = new List<CodeInstruction> {
					new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ColonistBar), "tmpPawns")),
					new CodeInstruction(OpCodes.Call, typeof(SS_Helper).GetMethod("ClearOutSlavesFromColonistBar")),
					new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(ColonistBar), "tmpPawns")),
				}; ILs.InsertRange(injectIndex, injection);

			// Injection 2
			injectIndex = ILs.FindLastIndex(IL => IL.opcode == OpCodes.Stloc_1);
			Label jump = ILgen.DefineLabel();
			injection = new List<CodeInstruction>{
					new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ColonistBar), "tmpMaps")),
					new CodeInstruction(OpCodes.Ldloc_1),
					new CodeInstruction(OpCodes.Callvirt, typeof(List<Map>).GetMethod("get_Item")),
					new CodeInstruction(OpCodes.Call, typeof(SS_Helper).GetMethod("DoesMapHaveSlaves")),
					new CodeInstruction(OpCodes.Brfalse, jump),
					new CodeInstruction(OpCodes.Ldloc_0),
					new CodeInstruction(OpCodes.Ldc_I4_1),
					new CodeInstruction(OpCodes.Add),
					new CodeInstruction(OpCodes.Stloc_0),
					new CodeInstruction(OpCodes.Nop){labels = new List<Label>{jump}},
				}; ILs.InsertRange(injectIndex - 7, injection);

			// Injection 3
			// TODO: Finish this

			foreach (CodeInstruction IL in ILs) yield return IL;
		}
	}

	public static class BRAU_Patches {

		static IEnumerable<CodeInstruction> BreakRiskAlertUtility_Transpiler(IEnumerable<CodeInstruction> instructions) {
			List<CodeInstruction> ILs = instructions.ToList();
			int injectIndex = ILs.FindIndex(IL => IL.opcode == OpCodes.Brfalse_S) + 1;
			var jump = (Label)ILs.Find(IL => IL.opcode == OpCodes.Brfalse_S).operand;
			// Add our jump target
			var injection = new List<CodeInstruction> {
				new CodeInstruction(OpCodes.Ldloc_1),
				new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SlaveUtility), "IsPawnColonySlave")),
				new CodeInstruction(OpCodes.Brtrue, jump),
			};
			ILs.InsertRange(injectIndex, injection);
			foreach (CodeInstruction IL in ILs) yield return IL;
		}
	}

	public static class MemoryThoughtHandler_Patch {
		public static void TryGainMemory_Postfix(ref Thought_Memory newThought, ref Pawn otherPawn) {
			Pawn pawn = newThought.pawn;
			ThoughtDef knowColonistDied = ThoughtDefOf.KnowColonistDied;
			ThoughtDef thoughtToGet = SS_ThoughtDefOf.KnowSlaveDied;
			if (newThought.def == knowColonistDied) {
				// Remove colonist death thoughts
				if (SlaveUtility.IsPawnColonySlave(otherPawn) || (SlaveUtility.IsPawnColonySlave(pawn) && !SlaveUtility.IsPawnColonySlave(otherPawn))) {
					pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDefWhereOtherPawnIs(knowColonistDied, otherPawn);
				}
				// Add slave death thoughts
				if (SlaveUtility.IsPawnColonySlave(otherPawn) && pawn.MapHeld == otherPawn.MapHeld) {
					//string thoughtToGet = "KnowSlaveDied";
					if (SlaveUtility.IsPawnColonySlave(pawn))
						thoughtToGet = SS_ThoughtDefOf.KnowFellowSlaveDied;
					pawn.needs.mood.thoughts.memories.TryGainMemory(thoughtToGet, otherPawn);
				}
			}
		}
	}
}

