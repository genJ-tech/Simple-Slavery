using System;
using RimWorld; 
using Verse; 
using Harmony;
using HugsLib;
using System.Reflection;

namespace BorgNames 
{

	public class BorgNamesBase : ModBase
	{
		public override string ModIdentifier {
			get {
				return "BorgNames";
			}
		}
	}

	[StaticConstructorOnStartup]
	internal static class HarmonyPatches{

		static HarmonyPatches(){
			HarmonyInstance harmonyInstance = HarmonyInstance.Create ("rimworld.orannj.borgnames");
			harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
		}
	}
		
	[HarmonyPatch(typeof(GenMapUI), "GetPawnLabel")]
	public static class GenMapUI_GetPawnLabel_Patch{
		[HarmonyPostfix]
		public static void GetPawnLabel_Patch(ref String __result, ref Pawn pawn){
			pawn
		}
	}
}

