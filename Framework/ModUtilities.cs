using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace MUMPs.Framework
{
	public static class ModUtilities
	{
		public static void LogDefault(string message, LogLevel _)
		{
			Debug.WriteLine(message);
		}

		public static Func<I, O> CreateFieldGetter<I, O>(this FieldInfo field)
		{
			// what the actual fuck
			if (field.DeclaringType is null)
				throw new NullReferenceException("Declaring type for field is null");

			if (!typeof(O).IsAssignableFrom(field.FieldType))
				throw new ArgumentException($"Field type is {field.FieldType.FullName}, which cannot be assigned to type {typeof(O).FullName}");

			if (!field.DeclaringType.IsAssignableFrom(typeof(I)))
				throw new ArgumentException($"Field owner is {field.DeclaringType.FullName}, which cannot be assigned from type {typeof(I).FullName}");

			var param = Expression.Parameter(typeof(I));
			return Expression.Lambda<Func<I, O>>(
				Expression.Field(param, field),
				param
			).Compile();
		}

		public static bool TryGetCue(this ISoundBank soundbank, string? name, [NotNullWhen(true)] out ICue? cue)
		{
			cue = null;

			if (soundbank is null || name is null || !soundbank.Exists(name))
				return false;

			cue = soundbank.GetCue(name);
			return true;
		}

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

		public static uint Reverse(this uint word)
		{	
			return
				((word & 0x000000FF) << (8 * 3)) |
				((word & 0x0000FF00) << (8 * 1)) |
				((word & 0x00FF0000) >> (8 * 1)) |
				((word & 0xFF000000) >> (8 * 3)) ;
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
	}
}
