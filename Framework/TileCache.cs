using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Objects;
using System.Runtime.CompilerServices;
using xTile;
using xTile.Tiles;

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

	public abstract void UpdateAll(GameLocation where, Rectangle region = default);
	public abstract void UpdateBlock(GameLocation where, Rectangle region = default);
	public abstract void UpdateFurniture(GameLocation where, IEnumerable<Furniture>? furniture = null);
	public abstract void UpdateBuildings(GameLocation where, IEnumerable<Building>? buildings = null);

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
		foreach (var b in e.Removed)
		{
			Rectangle bounds = b.GetPropertyBox();
			foreach (var cache in caches)
				cache.UpdateBlock(loc, bounds);
		}

		foreach (var cache in caches)
			cache.UpdateBuildings(loc, e.Added);
	}

	private static void FurnitureChanged(object? sender, FurnitureListChangedEventArgs e)
	{
		var loc = e.Location;
		foreach (var f in e.Removed)
		{
			var bounds = f.GetPropertyBox();
			foreach (var cache in caches)
				cache.UpdateBlock(loc, bounds);
		}

		foreach (var cache in caches)
			cache.UpdateFurniture(loc, e.Added);
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

		UpdateBlockImpl(where, vals);
		UpdateBuildingsImpl(where, vals, where.buildings);
		UpdateFurnitureImpl(where, vals, where.furniture);

		return vals;
	}

	public override void UpdateAll(GameLocation where, Rectangle region = default)
	{
		// only update existing instances, to enforce lazy loading
		if (!cache.TryGetValue(where, out var data))
			return;

		UpdateBlockImpl(where, data);
		UpdateBuildingsImpl(where, data, where.buildings);
		UpdateFurnitureImpl(where, data, where.furniture);
		OnChanged?.Invoke(where);
	}

	public override void UpdateFurniture(GameLocation where, IEnumerable<Furniture>? furniture = null)
	{
		// only update existing instances, to enforce lazy loading
		if (!cache.TryGetValue(where, out var data))
			return;

		UpdateFurnitureImpl(where, data, where.furniture);
		OnChanged?.Invoke(where);
	}

	public override void UpdateBuildings(GameLocation where, IEnumerable<Building>? buildings = null)
	{
		// only update existing instances, to enforce lazy loading
		if (!cache.TryGetValue(where, out var data))
			return;

		UpdateBuildingsImpl(where, data, where.buildings);
		OnChanged?.Invoke(where);
	}

	public override void UpdateBlock(GameLocation where, Rectangle region = default)
	{
		// only update existing instances, to enforce lazy loading
		if (!cache.TryGetValue(where, out var data))
			return;

		UpdateBlockImpl(where, data, region);
		OnChanged?.Invoke(where);
	}

	private void UpdateBuildingsImpl(GameLocation where, Dictionary<Point, T> data, IEnumerable<Building> buildings)
	{

		var prop = PropertyName;
		var layer = Layer;
		string val = "";

		foreach (var build in buildings)
		{
			var bounds = build.GetPropertyBox();

			for (int x = bounds.Left; x < bounds.Right; x++)
			{
				for (int y = bounds.Top; y < bounds.Bottom; y++)
				{
					if (build.doesTileHaveProperty(x, y, prop, layer, ref val))
					{
						Point pos = new(x, y);
						data.Add(pos, Factory(where, pos, val));
					}
				}
			}
		}
	}

	private void UpdateFurnitureImpl(GameLocation where, Dictionary<Point, T> data, IEnumerable<Furniture> furniture)
	{
		var prop = PropertyName;
		var layer = Layer;
		string val = "";

		foreach (var furn in furniture)
		{
			var bounds = furn.GetPropertyBox();

			for (int x = bounds.Left; x < bounds.Right; x++)
			{
				for (int y = bounds.Top; y < bounds.Bottom; y++)
				{
					if (furn.DoesTileHaveProperty(x, y, prop, layer, ref val)) 
					{
						Point pos = new(x, y);
						data.Add(pos, Factory(where, pos, val));
					}
				}
			}
		}
	}

	private void UpdateBlockImpl(GameLocation where, Dictionary<Point, T> data, Rectangle region = default)
	{
		bool isFullMap = false;
		var l = where.Map.GetLayer(Layer);
		if (l is null)
			return;

		if (region.Equals(default))
		{
			data.Clear();
			isFullMap = true;
			region = new(0, 0, l.LayerWidth, l.LayerHeight);
		}
		else if (region.Width is 0 && region.Height is 0)
		{
			return;
		}

		var prop = PropertyName;

		for (int x = region.X; x < region.Right; x++)
		{
			for (int y = region.Y; y < region.Bottom; y++)
			{
				Point pos = new(x, y);

				if (l.Tiles[x, y] is Tile tile)
				{
					if (tile.Properties.TryGetValue(prop, out var val) || tile.TileIndexProperties.TryGetValue(prop, out val))
					{
						data.Add(pos, Factory(where, pos, (string)val));
					}
				}
				else if (!isFullMap)
				{
					data.Remove(pos);
				}
			}
		}
	}
}
