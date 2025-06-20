﻿using EMU.Framework;
using EMU.Framework.Attributes;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using System.Reflection.Emit;
using xTile;

namespace EMU.Features;

[Feature("Seasonal Tiles")]
internal class UseSeasonalTiles
{
	private static IMonitor Monitor = null!;

	public UseSeasonalTiles(IMonitor monitor, HarmonyHelper harmony)
	{
		Monitor = monitor;

		harmony
			.With<GameLocation>(nameof(GameLocation.updateSeasonalTileSheets)).Transpiler(SkipCheck);
	}

	private static IEnumerable<CodeInstruction>? SkipCheck(IEnumerable<CodeInstruction> codes, ILGenerator gen)
	{
		var il = new CodeMatcher(codes, gen);

		il.MatchStartForward(
			new(OpCodes.Ldarg_0),
			new(OpCodes.Isinst, typeof(Summit))
		);

		if (il.IsInvalid)
		{
			Monitor.Log("Failed to apply seasonal tiles: 1st anchor not found.", LogLevel.Error);
			return null;
		}

		il.InsertAndAdvance(
			new(OpCodes.Ldarg_1),
			new(OpCodes.Call, typeof(UseSeasonalTiles).GetMethod(nameof(ForceSeasonalTiles))),
			new(OpCodes.Brtrue_S, il.InstructionAt(2).operand)
		);

		return il.InstructionEnumeration();
	}

	public static bool ForceSeasonalTiles(Map map)
	{
		return map.Properties.ContainsKey("EMU_UseSeasonalTiles");
	}
}
