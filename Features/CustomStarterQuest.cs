using HarmonyLib;
using EMU.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using System.Reflection.Emit;

namespace EMU.Features
{
	internal class CustomStarterQuest : IPatch
	{
		public const string MAP_PROPERTY = "EMU_StarterQuest";

		public string Name => "Custom Starter Quest";

		public void Init(IFeature.Logger log, IModHelper helper)
		{
		}

		public void Patch(Harmony harmony, out string? Error)
		{
			Error = null;
			harmony.Patch(
				typeof(Chest).GetMethod(nameof(Chest.dumpContents)),
				transpiler: new(typeof(CustomStarterQuest), nameof(InjectReplacement))
			);
		}

		private static IEnumerable<CodeInstruction> InjectReplacement(IEnumerable<CodeInstruction> source, ILGenerator gen)
		{
			var il = new CodeMatcher(source, gen);

			il
				.MatchStartForward(
					new CodeMatch(OpCodes.Ldstr, "9")
				)
				.MatchStartForward(
					new CodeMatch(OpCodes.Callvirt, typeof(Farmer).GetMethod(nameof(Farmer.addQuest)))
				)
				.Insert(
					new CodeInstruction(OpCodes.Call, typeof(CustomStarterQuest).GetMethod(nameof(ReplaceQuestID)))
				);

			return il.InstructionEnumeration();
		}

		public static string ReplaceQuestID(string original)
			=> Game1.getFarm().getMapProperty(MAP_PROPERTY) ?? original;
	}
}
