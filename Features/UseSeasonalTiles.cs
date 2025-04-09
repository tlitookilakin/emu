using EMU.Framework;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using System.Reflection.Emit;
using xTile;

namespace EMU.Features
{
	internal class UseSeasonalTiles : IPatch
	{
		public string Name => "Use Seasonal Tiles";

		private static IFeature.Logger Log = ModUtilities.LogDefault;

		public void Init(IFeature.Logger log, IModHelper helper)
		{
			Log = log;
		}

		public void Patch(Harmony harmony, out string? Error)
		{
			Error = null;

			harmony.Patch(
				typeof(GameLocation).GetMethod(nameof(GameLocation.updateSeasonalTileSheets)),
				transpiler: new(typeof(UseSeasonalTiles), nameof(SkipCheck))
			);
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
				Log("Failed to apply seasonal tiles: 1st anchor not found.", LogLevel.Error);
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
}
