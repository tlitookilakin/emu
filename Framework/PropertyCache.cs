using StardewValley;
using System.Runtime.CompilerServices;

namespace EMU.Framework;

public abstract class PropertyCache
{
	public string PropertyName { get; init; }

	public PropertyCache(string name)
	{
		PropertyName = name;
	}

	protected class PropertyElement<T>(string p, T elem)
	{
		public string? propValue = p;
		public T Value = elem;
	}
}

public class PropertyCache<T> : PropertyCache where T : class
{
	private readonly ConditionalWeakTable<GameLocation, PropertyElement<T>> cache = [];
	private readonly Func<GameLocation, string?, T> Factory;

	public PropertyCache(string propertyName, Func<GameLocation, string?, T> factory) : base(propertyName)
	{
		Factory = factory;
	}

	public T Get(GameLocation location)
	{
		var p = location.getMapProperty(PropertyName);

		if (cache.TryGetValue(location, out var item))
		{
			if (item.propValue == p)
			{
				return item.Value;
			}
		}

		T val = Factory(location, p);
		cache.AddOrUpdate(location, new(p, val));
		return val;
	}
}
