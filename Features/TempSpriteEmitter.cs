using EMU.Framework;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Extensions;
using System.Runtime.CompilerServices;

namespace EMU.Features
{
	// tile property on paths layer
	// chance:float id:string pixel_region:rectangle +depth_offset:float +condition:string...
	internal class TempSpriteEmitter : IFeature
	{
		const string PROPERTY_NAME = "EMU_Emitter";

		private sealed record class Emitter(TemporarySpriteData Sprite, Point Tile, Rectangle Region, string Condition, float chance, float offset);

		private static readonly PerScreen<List<Emitter>?> Emitters = new();
		private static readonly ConditionalWeakTable<GameLocation, List<Emitter>> EmitterCache = new();

		public void Init(IFeature.Logger log, IModHelper helper)
		{
			ModEntry.OnLocationChanged += LocationChanged;
			helper.Events.GameLoop.TimeChanged += TimeChanged;
			helper.Events.GameLoop.UpdateTicked += Tick;
		}

		private void Tick(object? sender, UpdateTickedEventArgs e)
		{
			var where = Game1.currentLocation;

			if (where is null || Emitters.Value is not List<Emitter> emitters)
				return;

			foreach (var emitter in emitters)
			{
				if (!Game1.random.NextBool(emitter.chance))
					continue;

				emitter.Sprite.Emit(where, new(
					emitter.Tile.X * Game1.tileSize + emitter.Region.X * Game1.pixelZoom + Game1.random.Next(emitter.Region.Width * Game1.pixelZoom),
					emitter.Tile.Y * Game1.tileSize + emitter.Region.Y * Game1.pixelZoom + Game1.random.Next(emitter.Region.Height * Game1.pixelZoom)
				));
			}
		}

		private void TimeChanged(object? sender, TimeChangedEventArgs e)
		{
			LocationChanged(Game1.currentLocation, Game1.player);
		}

		private void LocationChanged(GameLocation? where, Farmer who)
		{
			if (where is null)
			{
				Emitters.Value = null;
				return;
			}

			if (!EmitterCache.TryGetValue(where, out var emitters))
				EmitterCache.Add(where, emitters = ReadFromPathsLayer(where.Map));

			var ctx = new GameStateQueryContext(where, who, null, null, Game1.random);

			Emitters.Value = emitters.Where(e => e.Condition is null || GameStateQuery.CheckConditions(e.Condition, ctx)).ToList();
		}

		private static List<Emitter> ReadFromPathsLayer(xTile.Map map)
		{
			var layer = map.GetLayer("Paths");
			if (layer is null)
				return [];

			var emitters = new List<Emitter>();
			for (int x = 0; x < layer.LayerWidth; x++)
				for (int y = 0; y < layer.LayerHeight; y++)
					if (layer.Tiles[x, y] is xTile.Tiles.Tile tile && tile.Properties.TryGetValue(PROPERTY_NAME, out var prop))
						TryReadEmitter(x, y, emitters, prop.ToString().Split(' ', 8, StringSplitOptions.RemoveEmptyEntries));

			return emitters;
		}

		private static void TryReadEmitter(int x, int y, List<Emitter> emitters, string[] args)
		{
			if (
				!ArgUtility.TryGetFloat(args, 0, out var chance, out string? error) ||
				!ArgUtility.TryGet(args, 1, out var name, out error) ||
				!ArgUtility.TryGetRectangle(args, 2, out var region, out error) ||
				!ArgUtility.TryGetOptionalFloat(args, 6, out var offset, out error) ||
				!ArgUtility.TryGetOptional(args, 7, out string? condition, out error, null, false)
			)
			{
				// TODO log;
				return;
			}

			if (!Assets.TempSprites.TryGetValue(name, out var data))
			{
				// TODO log;
				return;
			}

			emitters.Add(new(data, new(x, y), region, condition, chance, offset));
		}
	}
}
