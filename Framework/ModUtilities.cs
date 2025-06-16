using EMU.Data;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Objects;
using System.Reflection;
using System.Text;

namespace EMU.Framework;

public static class ModUtilities
{
	public const BindingFlags AnyDeclared = BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public;

	public static string GetId(this GameLocation where)
		=> where is Farm ? "Farm_" + Game1.GetFarmTypeKey() : where.Name;
	public static void Emit(this TemporarySpriteData sprite,
		GameLocation location, Vector2 worldPos, float depthOffset = 0f, bool local = false)
	{
		float pixelDepth = sprite.UseDepth ? worldPos.Y / 1000f : 0f;

		var tas = TemporaryAnimatedSprite.GetTemporaryAnimatedSprite(
			sprite.Texture, sprite.SourceRect, sprite.Interval, sprite.Frames, sprite.Loops, worldPos + sprite.PositionOffset * 4f,
			sprite.Flicker, sprite.Flip, pixelDepth + depthOffset + sprite.SortOffset * 64f / 1000f, sprite.AlphaFade,
			Utility.StringToColor(sprite.Color) ?? Color.White, sprite.Scale, sprite.ScaleChange, sprite.Rotation, sprite.RotationChange, local
		);

		location.TemporarySprites.Add(tas);
	}

	public static string ToCapitalCase(this string original)
	{
		List<int> capitals = [];

		for (int i = 1; i < original.Length; i++)
			if (char.IsUpper(original[i]))
				capitals.Add(i);

		StringBuilder sb = new(original.Length + capitals.Count);
		original = original.ToUpper();

		int lastIndex = 0;
		foreach (int i in capitals)
		{
			if (lastIndex != 0)
				sb.Append('_');
			sb.Append(original[lastIndex..i]);
			lastIndex = i;
		}
		return sb.ToString();
	}

	public static double Distance(this Point a, Point b)
	{
		int x = Math.Abs(a.X - b.X);
		int y = Math.Abs(a.Y - b.Y);
		return Math.Sqrt(x * x + y * y);
	}

	public static Rectangle GetPropertyBox(this Furniture furn)
	{
		// tiles or pixels?
		var bounds = furn.boundingBox.Value;
		var radius = furn.GetAdditionalTilePropertyRadius();
		return new(bounds.X / 64 - radius, bounds.Y / 64 - radius, bounds.Width / 64 + radius * 2, bounds.Height / 64 + radius * 2);
	}

	public static Rectangle GetPropertyBox(this Building build)
	{
		var radius = build.GetAdditionalTilePropertyRadius();
		return new(build.tileX.Value - radius, build.tileY.Value - radius, build.tilesWide.Value + radius * 2, build.tilesHigh.Value + radius * 2);
	}

	public static TO? FirstOfType<TO, TI>(this IEnumerable<TI> items) 
		where TO : class, TI
		where TI : class
	{
		return items.FirstOrDefault(static i => i is TO) as TO;
	}

	public static double SmoothStep(double fromA, double toA, double fromB, double toB, double A)
	{
		double sizeA = toA - fromA;
		return Math.Clamp(A - fromA, 0, sizeA) * (toB - fromB) / sizeA + fromB;
	}
}
