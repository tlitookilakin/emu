using EMU.Framework;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace EMU.Features
{
	internal class CamRegion : IPatch
	{
		private static readonly PropertyCache<List<Rectangle>> regions =
			new("EMU_CamRegion", ParseRegions);
		private static IFeature.Logger Log = ModUtilities.LogDefault;

		public string Name => "Camera Regions";

		public void Init(IFeature.Logger log, IModHelper helper)
		{
			Log = log;
		}

		private static List<Rectangle> ParseRegions(GameLocation where, string? prop)
		{
			if (prop is null)
				return [];

			var regions = new List<Rectangle>();
			var split = ArgUtility.SplitBySpace(prop);

			for (int i = 0; i < split.Length; i += 4)
			{
				if (!ArgUtility.TryGetRectangle(split, i, out Rectangle rect, out var error))
				{
					Log($"Failed to parse CamRegion map property for {where.DisplayName}:\n{error}", LogLevel.Warn);
					return [];
				}
				regions.Add(rect);
			}

			return regions;
		}

		public void Patch(Harmony harmony, out string? Error)
		{
			Error = null;

			harmony.Patch(
				typeof(Game1).GetMethod(nameof(Game1.UpdateViewPort)),
				prefix: new(typeof(CamRegion), nameof(UpdateCamera))
			);
		}

		private static void UpdateCamera(bool overrideFreeze, ref Point centerPoint)
		{
			if (Game1.currentLocation.forceViewportPlayerFollow || !overrideFreeze && Game1.viewportFreeze)
				return;

			Point tileCenter = new(centerPoint.X / 64, centerPoint.Y / 64);
			foreach (var region in regions.Get(Game1.currentLocation))
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
}
