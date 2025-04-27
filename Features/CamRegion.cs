using EMU.Framework;
using EMU.Framework.Attributes;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace EMU.Features;

[Feature("Camera Regions")]
internal class CamRegion
{
	private static PropertyCache<List<Rectangle>> regions = null!;
	private readonly IMonitor Monitor;

	public CamRegion(IMonitor monitor, Harmony harmony, ICacheProvider propCache)
	{
		Monitor = monitor;
		regions = propCache.CreatePropertyCache("EMU_CamRegion", ParseRegions);

		harmony.Patch(
			typeof(Game1).GetMethod(nameof(Game1.UpdateViewPort)),
			prefix: new(typeof(CamRegion), nameof(UpdateCamera))
		);
	}

	private List<Rectangle> ParseRegions(GameLocation where, string? prop)
	{
		if (prop is null)
			return [];

		var regions = new List<Rectangle>();
		var split = ArgUtility.SplitBySpace(prop);

		for (int i = 0; i < split.Length; i += 4)
		{
			if (!ArgUtility.TryGetRectangle(split, i, out Rectangle rect, out var error))
			{
				Monitor.Log($"Failed to parse CamRegion map property for {where.DisplayName}:\n{error}", LogLevel.Warn);
				return [];
			}
			regions.Add(rect);
		}

		return regions;
	}

	private static void UpdateCamera(bool overrideFreeze, ref Point centerPoint)
	{
		var where = Game1.currentLocation;
		if (where is null)
			return;

		if (where.forceViewportPlayerFollow || !overrideFreeze && Game1.viewportFreeze)
			return;

		Point tileCenter = new(centerPoint.X / 64, centerPoint.Y / 64);
		foreach (var region in regions.Get(where))
			if (region.Contains(tileCenter))
			{
				centerPoint.X = Game1.viewport.Width >= region.Width ? region.X + region.Width / 2 :
					Math.Clamp(centerPoint.X, region.X + Game1.viewport.Width / 2, region.X + region.Width - Game1.viewport.Width / 2);
				centerPoint.Y = Game1.viewport.Height >= region.Height ? region.Y + region.Height / 2 :
					Math.Clamp(centerPoint.Y, region.Y + Game1.viewport.Height / 2, region.Y + region.Height - Game1.viewport.Height / 2);
				break;
			}
	}
}
