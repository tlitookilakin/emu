using StardewValley;

namespace EMU.Framework;

public interface IPropertyCacheProvider
{
	public PropertyCache<T> Create<T>(string propertyName, Func<GameLocation, string?, T> factory) where T : class;
}

internal class PropertyCacheProvider : IPropertyCacheProvider
{
	private static readonly Dictionary<string, PropertyCache> caches = [];

	public PropertyCache<T> Create<T>(string propertyName, Func<GameLocation, string?, T> factory) where T : class
	{
		if (caches.TryGetValue(propertyName, out var p))
			return (PropertyCache<T>)p;

		PropertyCache<T> cache = new(propertyName, factory);
		caches[propertyName] = cache;
		return cache;
	}
}
