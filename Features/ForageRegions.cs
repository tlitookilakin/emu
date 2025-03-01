using HarmonyLib;
using Microsoft.Xna.Framework;
using MUMPs.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.GameData.Locations;
using StardewValley.Internal;

namespace MUMPs.Features
{
	internal class ForageRegions : IPatch
	{
		public const string FLAG = "MUMPS/ForageRegions";

		public string Name => "Forage Regions";
		private static IFeature.Logger Log = ModUtilities.LogDefault;

		public void Init(IFeature.Logger log, IModHelper helper)
		{
			Log = log;
		}

		public void Patch(Harmony harmony, out string? Error)
		{
			Error = null;

			harmony.Patch(
				typeof(GameLocation).GetMethod(nameof(GameLocation.spawnObjects)),
				postfix: new(typeof(ForageRegions), nameof(SpawnRegions))
			);
		}

		private static void SpawnRegions(GameLocation __instance)
		{
			var data = __instance.GetData();
			if (data?.CustomFields is null || !data.CustomFields.TryGetValue(FLAG, out var val))
				return;

			var r = Utility.CreateDaySaveRandom();
			var season = __instance.GetSeason();
			var split = ArgUtility.SplitBySpaceQuoteAware(val);
			var locs = DataLoader.Locations(Game1.content);

			for (int i = 0; i < split.Length; i += 5)
			{
				if (!ArgUtility.TryGet(split, i, out var name, out var error, false) ||
					!ArgUtility.TryGetRectangle(split, i + 1, out var region, out error))
				{
					Log($"Could not parse forage region in {__instance.Name} at index {i}: {error}", LogLevel.Warn);
					continue;
				}

				int max = data.MaxDailyForageSpawn;
				int limit = data.MaxSpawnedForageAtOnce;
				if (ArgUtility.TryGetInt(split, i + 5, out var min, out error))
				{
					if (
						!ArgUtility.TryGetInt(split, i + 6, out max, out error) ||
						!ArgUtility.TryGetInt(split, i + 7, out limit, out error)
					)
					{
						Log($"Specified spawn limits for region '{name}' could not be parsed in '{__instance.Name}': {error}", LogLevel.Warn);
						continue;
					}
					i += 3;
				}
				else
				{
					min = data.MinDailyForageSpawn;
				}

				if (limit > 0 && __instance.numberOfSpawnedObjectsOnMap >= data.MaxSpawnedForageAtOnce)
					continue;

				if (!locs.TryGetValue(name, out var loc))
				{
					Log($"Could not process forage region in {__instance.Name}: Location data '{name}' does not exist!", LogLevel.Warn);
					continue;
				}

				Spawn(__instance, region, loc, name, season, r, min, max, limit);
			}
		}

		private static void Spawn(
			GameLocation where, Rectangle region, LocationData data, string name, Season season, Random r, int min, int max, int limit
		)
		{
			if (data.Forage is not List<SpawnForageData> forageItems)
			{
				Log($"No forage data for '{name}', skipping spawns.", LogLevel.Info);
				return;
			}

			var forages = forageItems.Where(
				e => (e.Season is null || e.Season == season) && (e.Condition is null || GameStateQuery.CheckConditions(e.Condition, where, random: r))
			);

			if (!forages.Any())
			{
				Log($"No valid forage items available for '{name}', skipping spawns", LogLevel.Info);
				return;
			}
			forageItems = forages.ToList();

			int numberToSpawn = r.Next(min, max);

			if (limit > 0)
				numberToSpawn = Math.Min(numberToSpawn, limit - GetSpawnCount(where, region));

			var itemQueryContext = new ItemQueryContext(where, null, r, "Mumps Forage Regions");
			for (int j = 0; j < numberToSpawn; j++)
			{
				for (int attempt = 0; attempt < 11; attempt++)
				{
					int x = r.Next(region.Width) + region.X;
					int y = r.Next(region.Height) + region.Y;
					var tile = new Vector2(x, y);

					if (
						where.objects.ContainsKey(tile) || 
						where.IsNoSpawnTile(tile) || 
						where.doesEitherTileOrTileIndexPropertyEqual(x, y, "Spawnable", "Back", "F") || 
						!where.CanItemBePlacedHere(tile) || 
						where.getTileIndexAt(x, y, "AlwaysFront") != -1 || 
						where.getTileIndexAt(x, y, "Front") != -1 || 
						where.isBehindBush(tile) || 
						(!r.NextBool(0.1) && where.isBehindTree(tile))
					)
						continue;

					SpawnForageData forage = r.ChooseFrom(forageItems);

					if (!r.NextBool(forage.Chance))
						continue;

					Item forageItem = ItemQueryResolver.TryResolveRandomItem(
						forage, itemQueryContext, avoidRepeat: false, null, null, null, (query, error) 
						=> Log($"Could not retrieve forage item '{query}' to spawn in region '{name}' in '{where.Name}': {error}", LogLevel.Warn)
					);

					if (forageItem == null)
						continue;

					if (forageItem is not StardewValley.Object forageObj)
					{
						Log($"Failed to spawn non-object forage item from entry '{forage.Id}' in region '{name}' in '{where.Name}'.", LogLevel.Warn);
					}
					else
					{
						forageObj.IsSpawnedObject = true;
						if (where.dropObject(forageObj, tile * 64f, Game1.viewport, initialPlacement: true))
						{
							break;
						}
					}
				}
			}
		}

		private static int GetSpawnCount(GameLocation where, Rectangle region)
			=> where.Objects.Pairs.Where(p => region.Contains(p.Key) && p.Value.isForage()).Count();
	}
}
