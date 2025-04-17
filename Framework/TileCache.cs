using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Runtime.CompilerServices;
using xTile;

namespace EMU.Framework;

public abstract class TileCache
{
	private static bool inited = false;

	public string Layer { get; init; }
	public string PropertyName { get; init; }

	public TileCache(string prop, string layer)
	{
		PropertyName = prop;
		Layer = layer;
	}

	public abstract void UpdateBlock(GameLocation where, Rectangle region = default);

	public abstract void Clear(GameLocation where);

	public static void Init(Harmony harmony, IModHelper helper)
	{
		if (inited) 
			return;
		inited = true;

		helper.Events.Content.AssetsInvalidated += InvalidateMap;
		helper.Events.World.FurnitureListChanged += FurnitureChanged;
		helper.Events.World.BuildingListChanged += BuildingsChanged;

		harmony.Patch(
			typeof(GameLocation).GetMethod(nameof(GameLocation.ApplyMapOverride), 
				[typeof(Map), typeof(string), typeof(Rectangle?), typeof(Rectangle?), typeof(Action<Point>)]
			),
			postfix: new(typeof(TileCache), nameof(UpdateOverrideRegion))
		);
	}

	protected static readonly List<TileCache> caches = [];

	private static void BuildingsChanged(object? sender, BuildingListChangedEventArgs e)
	{
		var loc = e.Location;
		foreach (var b in e.Added.Concat(e.Removed))
		{
			var rad = b.GetAdditionalTilePropertyRadius();
			Rectangle bounds = new(b.tileX.Value - rad, b.tileY.Value - rad, b.tilesWide.Value + rad * 2, b.tilesHigh.Value + rad * 2);
			foreach (var cache in caches)
				cache.UpdateBlock(loc, bounds);
		}
	}

	private static void FurnitureChanged(object? sender, FurnitureListChangedEventArgs e)
	{
		var loc = e.Location;
		foreach (var f in e.Added.Concat(e.Removed))
		{
			Rectangle bounds = new(f.TileLocation.ToPoint(), new Point(f.getTilesWide(), f.getTilesHigh()));
			foreach (var cache in caches)
				cache.UpdateBlock(loc, bounds);
		}
	}

	private static void InvalidateMap(object? sender, AssetsInvalidatedEventArgs e)
	{
		var names = e.NamesWithoutLocale.Where(static n => n.StartsWith("Maps/", false)).Select(static s => s.ToString()).ToList();
		Utility.ForEachLocation(g =>
		{
			if (names.Contains(g.mapPath.Value))
				foreach (var cache in caches)
					cache.Clear(g);

			return true;
		});
	}

	private static void UpdateOverrideRegion(GameLocation __instance, Rectangle? dest_rect, Map override_map)
	{
		Rectangle dest;
		if (!dest_rect.HasValue)
		{
			int map_width = 0;
			int map_height = 0;
			for (int i = 0; i < override_map.Layers.Count; i++)
			{
				map_width = Math.Max(map_width, override_map.Layers[i].LayerWidth);
				map_height = Math.Max(map_height, override_map.Layers[i].LayerHeight);
			}
			dest = new(0, 0, map_width, map_height);
		}
		else
		{
			dest = dest_rect.Value;
		}

		foreach (var cache in caches)
			cache.UpdateBlock(__instance, dest);
	}
}

public class TileCache<T> : TileCache
{
	private readonly ConditionalWeakTable<GameLocation, Dictionary<Point, T>> cache = [];
	private readonly Func<GameLocation, Point, string, T> Factory;

	public event Action<GameLocation>? OnChanged;

	public TileCache(string propName, string layer, Func<GameLocation, Point, string, T> factory) :
		base(propName, layer)
	{
		Factory = factory;
		caches.Add(this);
	}

	public override void Clear(GameLocation where)
	{
		cache.Remove(where);
	}

	public IReadOnlyDictionary<Point, T> GetAll(GameLocation where)
	{
		if (cache.TryGetValue(where, out var result))
			return result;

		Dictionary<Point, T> vals = [];
		cache.Add(where, vals);
		UpdateImpl(where, vals);
		return vals;
	}

	public override void UpdateBlock(GameLocation where, Rectangle region = default)
	{
		// only update existing instances, to enforce lazy loading
		if (!cache.TryGetValue(where, out var data))
			return;

		UpdateImpl(where, data, region);
	}

	private void UpdateImpl(GameLocation where, Dictionary<Point, T> data, Rectangle region = default)
	{
		bool isFullMap = false;
		if (region.Equals(default))
		{
			var l = where.Map.GetLayer(Layer);
			if (l is null)
			{
				data.Clear();
				return;
			}

			data.Clear();
			isFullMap = true;
			region = new(0, 0, l.LayerWidth, l.LayerHeight);
		}

		for (int x = region.X; x < region.Right; x++)
		{
			for (int y = region.Y; y < region.Bottom; y++)
			{
				Point tile = new(x, y);

				if (where.doesTileHaveProperty(x, y, PropertyName, Layer) is string s)
					data.Add(tile, Factory(where, tile, s));

				else if (!isFullMap)
					data.Remove(tile);
			}
		}

		if (region.Width > 0 && region.Height > 0)
			OnChanged?.Invoke(where);
	}
}
