using HarmonyLib;
using Microsoft.Xna.Framework;
using MUMPs.Framework;
using StardewModdingAPI;
using StardewValley;
using System.Globalization;

namespace MUMPs.Features
{
	internal class WaterColor : IPatch
	{
		public string Name => "Water Color";
		private static IFeature.Logger logger = ModUtilities.LogDefault;

		public void Init(IFeature.Logger log, IModHelper helper)
		{
			logger = log;
		}

		public void Patch(Harmony harmony, out string? Error)
		{
			Error = null;

			harmony.Patch(
				typeof(GameLocation).GetMethod(nameof(GameLocation.seasonUpdate)),
				postfix: new(typeof(WaterColor), nameof(AdjustWaterColor))
			);

			harmony.Patch(
				typeof(GameLocation).GetMethod(nameof(GameLocation.loadMap)),
				postfix: new(typeof(WaterColor), nameof(AdjustWaterColor))
			);
		}


		/// <summary>
		/// Reads water color from map property 'WaterColor'. seasons are separated with '/' (optional). color must be on of these:
		/// #rgb, #rgba, #rrggbb, #rrggbbaa, r g b, r g b a. On last two, rgb are bytes and a is a float.
		/// </summary>
		private static void AdjustWaterColor(GameLocation __instance)
		{
			if (__instance.Map is not xTile.Map map || !map.Properties.TryGetValue("WaterColor", out var val))
				return;

			var prop = val.ToString();
			var chunks = prop.Split('/');
			var index = __instance.GetSeasonIndex();
			var chunk = chunks.Length >= 4 && chunks[index].Length is not 0 ? chunks[index] : chunks[0];

			var split = chunk.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
			string err;

			if (split.Length is 1 && split[0][0] is '#')
			{
				var cstr = split[0][1..];
				try
				{
					// packed color uses AABBGGRR format, so it must be reversed when parsed.
					__instance.waterColor.Value = cstr.Length switch
					{
						3 => new(uint.Parse($"{cstr[2]}{cstr[2]}{cstr[1]}{cstr[1]}{cstr[0]}{cstr[0]}", NumberStyles.HexNumber, null) | 0xFF000000),
						4 => new(uint.Parse($"{cstr[3]}{cstr[3]}{cstr[2]}{cstr[2]}{cstr[1]}{cstr[1]}{cstr[0]}{cstr[0]}", NumberStyles.HexNumber, null)),
						6 => new(((uint.Parse(cstr, NumberStyles.HexNumber, null) << 8) | 0xFF).Reverse()),
						8 => new(uint.Parse(cstr, NumberStyles.HexNumber, null).Reverse()),
						_ => throw new FormatException(
							$"'{split[0]}' is not a valid hexadecimal color code. (Must use one of these formats: #rgb #rgba #rrggbb #rrggbbaa)")
					};
					return;
				}
				catch (Exception ex)
				{
					err = ex.ToString();
				}
			}
			else if (
				ArgUtility.TryGetInt(split, 0, out int r, out err) &&
				ArgUtility.TryGetInt(split, 1, out int g, out err) &&
				ArgUtility.TryGetInt(split, 2, out int b, out err))
			{
				if (ArgUtility.TryGetOptionalFloat(split, 3, out float a, out err, 1f))
				{
					__instance.waterColor.Value = new Color(r, g, b) * a;
					return;
				}
			}

			logger($"Failed to parse WaterColor property for location '{__instance.DisplayName}' ({__instance.NameOrUniqueName}) for index {index} in value '{prop}': {err}", LogLevel.Warn);
		}
	}
}
