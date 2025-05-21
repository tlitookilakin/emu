using EMU.Framework;
using EMU.Framework.Attributes;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace EMU.Features;

[Feature("Slopes")]
internal class Slope
{
	private static int offset;
	private static float oldX;

	public Slope(Harmony harmony, IMonitor monitor)
	{
		harmony.Patcher<Farmer>(monitor)
			.With(nameof(Farmer.nextPosition)).Postfix(DoCheck)
			.With(nameof(Farmer.nextPositionHalf)).Postfix(DoCheck)
			.With(nameof(Farmer.MovePosition)).Postfix(ApplyModifier);
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
		if (!float.TryParse(loc.doesTileHavePropertyNoNull(pt.X, pt.Y, "EMU_Slope", "Back"), out var off))
			return bounds;

		off *= __instance.GetBoundingBox().X - bounds.X;

		bounds.Y += (int)(off + .5f);
		return bounds;
	}
}
