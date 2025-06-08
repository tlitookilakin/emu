using EMU.Framework;
using EMU.Framework.Attributes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;
using System.Diagnostics.CodeAnalysis;
using static HarmonyLib.AccessTools;

namespace EMU.Features;

[Feature("Perching Birds")]
internal class FancyBirds
{
	public const string PROP_PERCH = ModEntry.MOD_ID + "_BirdPerch";
	public const string PROP_BIRD = ModEntry.MOD_ID + "_Birds";

	public record class BirdData(int Count, string? TextureName, List<int> Types);
	public enum AllowState { Roost, Perch, Both, None }

	private readonly TileCache<AllowState> perchTileCache;
	private readonly PropertyCache<BirdData> birdCache;
	private readonly PerScreen<PerchingBirds?> birds = new();
	private readonly IMonitor Monitor;

	private static readonly FieldRef<PerchingBirds, Texture2D> birdSheet 
		= FieldRefAccess<PerchingBirds, Texture2D>("_birdSheet");
	private static readonly FieldRef<PerchingBirds, Point[]> perchSpots
		= FieldRefAccess<PerchingBirds, Point[]>("_birdLocations");
	private static readonly FieldRef<PerchingBirds, Point[]> roostSpots
		= FieldRefAccess<PerchingBirds, Point[]>("_birdRoostLocations");

	public FancyBirds(ICacheProvider cache, IModHelper helper, IMonitor monitor)
	{
		Monitor = monitor;
		perchTileCache = cache.CreateTileCache(PROP_PERCH, "Paths", ReadTileState);
		birdCache = cache.CreatePropertyCache(PROP_BIRD, ReadProperty);
		ModEntry.OnLocationChanged += ChangeLocation;
		helper.Events.Display.RenderedWorld += Draw;

		perchTileCache.OnChanged += TilesChanged;
	}

	private void TilesChanged(GameLocation where)
	{
		if (where != Game1.currentLocation)
			return;

		if (birds.Value is not PerchingBirds bird)
			return;

		FindPerches(where, out var perches, out var roosts);
		roostSpots(bird) = [.. roosts];
		perchSpots(bird) = [.. perches];

		var targets = bird.GetCurrentBirdLocationList();

		foreach ((var tile, var b) in bird._birdPointOccupancy)
		{
			if (!targets.Contains(tile))
			{
				b.FlyToNewPoint();
				b.birdState = Bird.BirdState.Flying;
			}
		}
	}

	private void PropertyChanged(BirdData data)
	{
		if (data.Count is 0)
			birds.Value = null;
		else if (birds.Value is not PerchingBirds bird)
			birds.Value = CreateFrom(data, Game1.currentLocation);
		else
			CopyInto(data, bird);
	}

	private PerchingBirds? CreateFrom(BirdData data, GameLocation where)
	{
		if (!TryGetBirdTexture(data.TextureName, out var tex))
			return null;

		FindPerches(where, out var perches, out var roosts);
		var birds = new PerchingBirds(tex, 2, 16, 16, new(8, 14), [.. perches], [.. roosts])
		{
			roosting = Game1.isDarkOut(Game1.currentLocation)
		};
		ApplyCount(data, birds);
		return birds;
	}

	private void FindPerches(GameLocation where, out List<Point> perches, out List<Point> roosts)
	{
		perches = [];
		roosts = [];
		foreach ((var tile, var status) in perchTileCache.GetAll(where))
		{
			switch (status)
			{
				case AllowState.Roost:
					roosts.Add(tile);
					break;
				case AllowState.Perch:
					perches.Add(tile);
					break;
				case AllowState.Both:
					roosts.Add(tile);
					perches.Add(tile);
					break;
			}
		}
	}

	private void CopyInto(BirdData data, PerchingBirds birds)
	{
		if (TryGetBirdTexture(data.TextureName, out var tex))
			birdSheet(birds) = tex;

		ApplyCount(data, birds);
		for (int i = birds._birds.Count; i > 0; i--)
		{
			if (data.Types.Contains(birds._birds[i].birdType))
				continue;

			birds._birds.RemoveAt(i);
			birds.AddBird(Game1.random.ChooseFrom(data.Types));
		}
	}

	private bool TryGetBirdTexture(string? which, [NotNullWhen(true)] out Texture2D? tex)
	{
		if (which != null)
		{
			try
			{
				tex = Game1.content.Load<Texture2D>(which);
			}
			catch (Exception ex)
			{
				Monitor.Log($"Could not load bird texture: '{which}':\n{ex}", LogLevel.Error);
				tex = null;
				return false;
			}
		}
		else
		{
			tex = Game1.birdsSpriteSheet;
		}

		return true;
	}

	private static void ApplyCount(BirdData data, PerchingBirds birds)
	{
		int toAdd = birds._birds.Count;
		if (toAdd is 0)
			return;

		if (toAdd > 0)
			for (int i = 0; i < toAdd; i++)
				birds.AddBird(Game1.random.ChooseFrom(data.Types));
		else
			birds._birds.RemoveRange(birds._birds.Count + toAdd, -toAdd);
	}

	private void Draw(object? sender, RenderedWorldEventArgs e)
	{
		if (birds.Value is PerchingBirds bird)
		{
			bool wasRoosting = bird.roosting;
			bird.roosting = Game1.isDarkOut(Game1.currentLocation);

			if (wasRoosting != bird.roosting)
			{
				foreach (var b in bird._birds)
				{
					b.FlyToNewPoint();
					b.birdState = Bird.BirdState.Flying;
				}
			}

			var data = birdCache.Get(Game1.currentLocation, out var changed);
			if (changed)
				PropertyChanged(data);

			bird.Update(Game1.currentGameTime);
			bird.Draw(e.SpriteBatch);
		}
	}

	private void ChangeLocation(GameLocation where, Farmer who)
	{
		if (where is null) 
		{
			birds.Value = null;
			return;
		}

		var data = birdCache.Get(where, out _);
		if (data.Count is not 0)
			birds.Value = CreateFrom(data, where);
		else
			birds.Value = null;
	}

	private static AllowState ReadTileState(GameLocation where, Point tile, string val)
	{
		return val switch
		{
			"Roost" or "roost" => AllowState.Roost,
			"Perch" or "perch" => AllowState.Perch,
			"Both" or "both" or "All" or "all" => AllowState.Both,
			_ => AllowState.None,
		};
	}

	private static BirdData ReadProperty(GameLocation where, string? val)
	{
		if (val is null)
			return new(0, null, GetDefaultTypes(where));

		var split = ArgUtility.SplitBySpaceQuoteAware(val);

		if (!ArgUtility.TryGetInt(split, 0, out int count, out _))
			return new(0, null, GetDefaultTypes(where));

		List<int> types = [];

		int i = 1;
		for (; i < split.Length && int.TryParse(split[i], out int type); i++)
			types.Add(type);

		if (types.Count is 0)
			types = GetDefaultTypes(where);

		string? name = i < split.Length ? split[i] : null;

		return new(count, name, types);
	}

	private static List<int> GetDefaultTypes(GameLocation where)
	{
		if (where.IsFallHere())
			return [10];
		else
			return [0, 1, 2, 3];
	}
}
