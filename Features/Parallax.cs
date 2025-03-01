using EMU.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Mods;
using System.Diagnostics.CodeAnalysis;
using RepeatStyle = EMU.Framework.ParallaxLayer.RepeatStyle;

namespace EMU.Features
{
	internal class Parallax : IFeature
	{
		private IModHelper Helper = null!;
		private IFeature.Logger Log = null!;
		private readonly PerScreen<ParallaxState?> Foreground = new();
		private readonly PerScreen<ParallaxState?> Background = new();

		public void Init(IFeature.Logger log, IModHelper helper)
		{
			Log = log;
			Helper = helper;
			ModEntry.OnLocationChanged += LocationChanged;
			ModEntry.OnCleanup += Cleanup;
			helper.Events.Display.RenderingStep += RenderStep;
		}

		private void RenderStep(object? sender, RenderingStepEventArgs e)
		{
			if (e.Step is RenderSteps.World_Background)
			{
				if (Background.Value is ParallaxState data)
					DrawBackground(data, false, e.SpriteBatch);
			}
			else if (e.Step is RenderSteps.World_AlwaysFront)
			{
				if (Foreground.Value is ParallaxState data)
					DrawBackground(data, true, e.SpriteBatch);

				if (!Game1.currentLocation.IsOutdoors)
					DrawClipBox(e.SpriteBatch);
			}
		}

		private void Cleanup(Farmer obj)
		{
			Foreground.Value = null;
			Background.Value = null;
		}

		private void LocationChanged(GameLocation where, Farmer who)
		{
			if (where.TryGetMapProperty("MUMPS_Background", out var bgid))
			{
				if (TryGetBackground(where, who, bgid, out var bg, out var err))
					Background.Value = bg;
				else
					Log($"Error loading background for location '{where.DisplayName}' ({where.NameOrUniqueName}): {err}", LogLevel.Warn);
			}

			if (where.TryGetMapProperty("MUMPS_Foreground", out var fgid))
			{
				if (TryGetBackground(where, who, fgid, out var fg, out var err))
					Foreground.Value = fg;
				else
					Log($"Error loading foreground for location '{where.DisplayName}' ({where.NameOrUniqueName}): {err}", LogLevel.Warn);
			}
		}

		private void DrawBackground(ParallaxState bg, bool isForeground, SpriteBatch batch)
		{
			if (bg.UseDynamicSky && !isForeground)
				DrawSky(batch);

			if (bg.Layers is null)
				return;

			var gview = Game1.viewport;
			var view = new Rectangle(gview.X, gview.Y, gview.Width, gview.Height);
			var offset = bg.Offset + view.Location;

			for (int i = 0; i < bg.Layers.Count; i++)
				DrawLayer(bg.Layers[i], bg.Textures[i], offset, batch, view, isForeground);
		}

		private static void DrawClipBox(SpriteBatch batch)
		{
			var view = new Rectangle(Game1.viewport.X, Game1.viewport.Y, Game1.viewport.Width, Game1.viewport.Height);
			var mapSize = new Point(Game1.currentLocation.Map.DisplayWidth, Game1.currentLocation.Map.DisplayHeight);

			if (view.X < 0)
				batch.Draw(Game1.staminaRect, new Rectangle(0, 0, -view.X, view.Height), Color.Black);
			if (view.Y < 0)
				batch.Draw(Game1.staminaRect, new Rectangle(0, 0, view.Width, -view.Y), Color.Black);
			if (view.Right > mapSize.X)
				batch.Draw(Game1.staminaRect, new Rectangle(mapSize.X - view.X, 0, view.Right - (mapSize.X - view.X), view.Height), Color.Black);
			if (view.Bottom > mapSize.Y)
				batch.Draw(Game1.staminaRect, new Rectangle(0, mapSize.Y - view.Y, view.Width, view.Bottom - (mapSize.Y - view.Y)), Color.Black);
		}

		private static void DrawSky(SpriteBatch batch)
		{

		}

		private void DrawLayer(ParallaxLayer layer, Texture2D texture, Point offset, SpriteBatch batch, Rectangle view, bool front)
		{
			offset += layer.TargetRegion.Location;
			float motion_scale = front ? 1 / layer.Depth : -layer.Depth;
			offset = new((int)(motion_scale * offset.X), (int)(motion_scale * offset.Y));

			int h_iters = 1;
			int v_iters = 1;
			var dest = layer.TargetRegion;

			if (layer.Repeat is RepeatStyle.TileH or RepeatStyle.TileBoth)
			{
				h_iters = view.Width / dest.Width + 1;
				offset.X = (offset.X - view.X) % dest.Width;
			}

			if (layer.Repeat is RepeatStyle.TileV or RepeatStyle.TileBoth)
			{
				v_iters = view.Height / dest.Height + 1;
				offset.Y = (offset.Y - view.Y) % dest.Height;
			}

			for (int hi = 0; hi < h_iters; hi++)
				for (int vi = 0; vi < v_iters; vi++)
					batch.Draw(
						texture,
						new Rectangle(offset.X + hi * dest.Height, offset.Y + vi * dest.Height, dest.Width, dest.Height),
						layer.SourceRegion, Color.White, 0f, Vector2.Zero, SpriteEffects.None,
						front ? 1000f - layer.Depth : layer.Depth
					);
		}

		private static bool TryGetBackground(GameLocation where, Farmer who, string property,
			[NotNullWhen(true)] out ParallaxState? bg, [NotNullWhen(false)] out string? error)
		{
			bg = null;
			var split = property.Split(' ');

			if (!ArgUtility.TryGet(split, 0, out var id, out error, false) ||
				!ArgUtility.TryGetPoint(split, 1, out var offset, out error))
				return false;

			if (!Assets.Backgrounds.TryGetValue(id, out var basis))
			{
				error = $"Background with ID '{id}' does not exist";
				return false;
			}

			bg = new(basis, new(where, who, null, null, Game1.random), offset);
			return true;
		}
	}
}
