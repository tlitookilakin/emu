﻿using EMU.Framework;
using EMU.Framework.Attributes;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;

namespace EMU.Features;

[Feature("Water Color")]
internal class WaterColor
{
	public WaterColor(Harmony harmony)
	{
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
	/// Reads water color from map property 'WaterColor'. seasons are separated with '/' (optional).
	/// </summary>
	private static void AdjustWaterColor(GameLocation __instance)
	{
		if (__instance.Map is not xTile.Map map || !map.Properties.TryGetValue("EMU_WaterColor", out var val))
			return;

		var prop = val.ToString();
		var chunks = prop.Split('/');
		var index = __instance.GetSeasonIndex();
		var chunk = chunks.Length >= 4 && chunks[index].Length is not 0 ? chunks[index] : chunks[0];

		if(Utility.StringToColor(chunk) is Color color)
		{
			__instance.waterColor.Value = color;
		}
	}
}
