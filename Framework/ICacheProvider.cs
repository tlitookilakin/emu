using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace EMU.Framework;

public interface ICacheProvider
{
	public TileCache<T> CreateTileCache<T>(string propName, string layer, Func<GameLocation, Point, string, T> factory);
	public PropertyCache<T> CreatePropertyCache<T>(string propertyName, Func<GameLocation, string?, T> factory) where T : class;
}

internal class CacheProvider : ICacheProvider
{
	private static readonly Dictionary<string, TileCache> knownProperties = [];
	private static readonly Dictionary<string, PropertyCache> caches = [];

	public CacheProvider(Harmony harmony, IModHelper helper)
	{
		TileCache.Init(harmony, helper);
	}

	public PropertyCache<T> CreatePropertyCache<T>(string propertyName, Func<GameLocation, string?, T> factory) where T : class
	{
		if (caches.TryGetValue(propertyName, out var p))
			return (PropertyCache<T>)p;

		PropertyCache<T> cache = new(propertyName, factory);
		caches[propertyName] = cache;
		return cache;
	}

	public TileCache<T> CreateTileCache<T>(string propName, string layer, Func<GameLocation, Point, string, T> factory)
	{
		if (knownProperties.TryGetValue(propName, out var p))
			return (TileCache<T>)p;

		TileCache<T> cache = new(propName, layer, factory);
		knownProperties[propName] = cache;
		return cache;
	}
}
