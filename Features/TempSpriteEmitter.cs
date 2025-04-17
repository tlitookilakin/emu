using EMU.Data;
using EMU.Framework;
using EMU.Framework.Attributes;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Extensions;

namespace EMU.Features;

// tile property on paths layer
// chance:float id:string pixel_region:rectangle +depth_offset:float +condition:string...

[Feature("Particle Emitters")]
internal class TempSpriteEmitter
{
	const string PROPERTY_NAME = "EMU_Emitter";

	private sealed record class Emitter(TemporarySpriteData Sprite, Point Tile, Rectangle Region, string Condition, float chance, float offset)
	{
		public bool Enabled { get; set; } = true;
	}

	private TileCache<Emitter?> EmitterCache;
	private Assets Assets;
	private IMonitor Monitor;

	public TempSpriteEmitter(IModHelper helper, ITileCacheProvider tileCache, Assets assets, IMonitor monitor)
	{
		Assets = assets;
		Monitor = monitor;
		EmitterCache = tileCache.Create(PROPERTY_NAME, "Paths", ReadFromTile);

		ModEntry.OnLocationChanged += LocationChanged;
		helper.Events.GameLoop.TimeChanged += TimeChanged;
		helper.Events.GameLoop.UpdateTicked += Tick;
	}

	private Emitter? ReadFromTile(GameLocation where, Point tile, string prop)
	{
		var args = ArgUtility.SplitBySpaceQuoteAware(prop);
		if (
			!ArgUtility.TryGetFloat(args, 0, out var chance, out string? error) ||
			!ArgUtility.TryGet(args, 1, out var name, out error) ||
			!ArgUtility.TryGetRectangle(args, 2, out var region, out error) ||
			!ArgUtility.TryGetOptionalFloat(args, 6, out var offset, out error) ||
			!ArgUtility.TryGetOptional(args, 7, out string? condition, out error, null, false)
		)
		{
			Monitor.Log($"Could not read emitter @ tile {tile}:\n{error}", LogLevel.Warn);
			return null;
		}

		if (!Assets.TempSprites.TryGetValue(name, out var data))
		{
			Monitor.Log($"Temporary sprite data with id {name} does not exist! @ tile {tile}", LogLevel.Warn);
			return null;
		}

		return new(data, tile, region, condition, chance, offset);
	}

	private void Tick(object? sender, UpdateTickedEventArgs e)
	{
		var where = Game1.currentLocation;
		if (where is null)
			return;

		var emitters = EmitterCache.GetAll(where);

		foreach (var emitter in emitters.Values)
		{
			if (emitter is null)
				continue;

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
			return;

		var ctx = new GameStateQueryContext(where, who, null, null, Game1.random);

		foreach (var emitter in EmitterCache.GetAll(where).Values)
		{
			if (emitter is null)
				continue;

			emitter.Enabled = emitter.Condition is null || GameStateQuery.CheckConditions(emitter.Condition, ctx);
		}
	}
}
