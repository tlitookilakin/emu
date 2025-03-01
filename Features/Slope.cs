using HarmonyLib;
using Microsoft.Xna.Framework;
using MUMPs.Framework;
using StardewModdingAPI;
using StardewValley;
using static MUMPs.Framework.IFeature;
namespace MUMPs.Features
{
	internal class Slope : IPatch
	{
		public string Name => "Slopes";

		private static int offset;
		private static float oldX;

		public void Init(Logger log, IModHelper helper) { }

		public void Patch(Harmony harmony, out string? Error)
		{
			Error = null;

			var positionMod = new HarmonyMethod(typeof(Slope), nameof(DoCheck));

			harmony.Patch(
				typeof(Farmer).GetMethod(nameof(Farmer.nextPosition)),
				postfix: positionMod
			);
			harmony.Patch(
				typeof(Farmer).GetMethod(nameof(Farmer.nextPositionHalf)),
				postfix: positionMod
			);
			harmony.Patch(
				typeof(Farmer).GetMethod(nameof(Farmer.MovePosition)),
				postfix: new(typeof(Slope), nameof(ApplyModifier))
			);
		}

		private static void ApplyModifier(Farmer __instance)
		{
			if (offset is not 0 && oldX is not 0 && oldX != __instance.Position.X)
				__instance.position.Y += offset;
			offset = 0;
		}

		private static Rectangle DoCheck(Rectangle bounds, Farmer __instance)
		{
			var loc = Game1.currentLocation;
			if (loc is null)
				return bounds;

			oldX = __instance.Position.X;
			offset = 0;

			var pt = __instance.TilePoint;
			if (!float.TryParse(loc.doesTileHavePropertyNoNull(pt.X, pt.Y, "Slope", "Back"), out var off))
				return bounds;

			off *= __instance.GetBoundingBox().X - bounds.X;

			bounds.Y += (int)(off + .5f);
			return bounds;
		}
	}
}
