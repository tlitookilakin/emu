using EMU.Framework;
using EMU.Framework.Attributes;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using System.Reflection.Emit;

namespace EMU.Features;

[Feature("Custom Starter Quest")]
internal class CustomStarterQuest
{
	public const string MAP_PROPERTY = "EMU_StarterQuest";

	public CustomStarterQuest(HarmonyHelper harmony)
	{
		harmony
			.With<Chest>(nameof(Chest.dumpContents)).Transpiler(InjectReplacement);
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
