using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace EMU.Framework;

public interface ITileCacheProvider
{
	public TileCache<T> Create<T>(string propName, string layer, Func<GameLocation, Point, string, T> factory);
}

internal class TileCacheProvider : ITileCacheProvider
{
	private static readonly Dictionary<string, TileCache> knownProperties = [];

	public TileCacheProvider(Harmony harmony, IModHelper helper)
	{
		TileCache.Init(harmony, helper);
	}

	public TileCache<T> Create<T>(string propName, string layer, Func<GameLocation, Point, string, T> factory)
	{
		if (knownProperties.TryGetValue(propName, out var p))
			return (TileCache<T>)p;

		TileCache<T> cache = new(propName, layer, factory);
		knownProperties[propName] = cache;
		return cache;
	}
}
