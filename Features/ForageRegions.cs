using EMU.Framework;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Extensions;
using StardewValley.GameData.Locations;
using StardewValley.Internal;

namespace EMU.Features
{
	internal class ForageRegions : IPatch
	{
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
			if (!Game1.IsMasterGame)
				return;

			if (!Assets.ExtendedData.TryGetValue(__instance.GetId(), out var data))
				return;

			if (data.ForageRegions is not List<ForageRegionData> regions)
				return;

			var r = Utility.CreateDaySaveRandom();
			var season = __instance.GetSeason();

			var ctx = new GameStateQueryContext(__instance, Game1.player, null, null, r);

			foreach (var region in regions)
			{
				if (region.Forage is null)
					continue;

				if (region.Condition != null && !GameStateQuery.CheckConditions(region.Condition, ctx))
					continue;

				var forages = region.Forage.Where(
					e => (e.Season is null || e.Season == season) && 
					(e.Condition is null || GameStateQuery.CheckConditions(e.Condition, ctx))
				);

				if (!forages.Any())
					continue;

				var spawnables = forages.ToList();
				var itemQueryContext = new ItemQueryContext(__instance, null, r, "EMU Forage Regions");
				var rect = region.Region;

				for (int i = r.Next(region.Min, region.Max); i > 0; i--)
				{
					for (int attempt = 0; attempt < 11; attempt++)
					{
						int x = r.Next(rect.Width) + rect.X;
						int y = r.Next(rect.Height) + rect.Y;
						var tile = new Vector2(x, y);

						if (
							region.RequiredTerrainType is List<string> types &&
							(__instance.doesTileHavePropertyNoNull(x, y, "Type", "Back") is not string s ||
							!types.Contains(s, StringComparer.OrdinalIgnoreCase))
						)
							continue;

						if(
							__instance.objects.ContainsKey(tile) || 
							__instance.IsNoSpawnTile(tile) || 
							__instance.doesTileHaveProperty(x, y, "Spawnable", "Back") == null || 
							__instance.doesEitherTileOrTileIndexPropertyEqual(x, y, "Spawnable", "Back", "F") || 
							!__instance.CanItemBePlacedHere(tile) || 
							__instance.hasTileAt(x, y, "AlwaysFront") || 
							__instance.hasTileAt(x, y, "AlwaysFront2") || 
							__instance.hasTileAt(x, y, "AlwaysFront3") || 
							__instance.hasTileAt(x, y, "Front") || 
							__instance.isBehindBush(tile) || 
							(!r.NextBool(0.1) && __instance.isBehindTree(tile))
						)
							continue;

						SpawnForageData forage = r.ChooseFrom(spawnables);

						if (!r.NextBool(forage.Chance))
							continue;

						Item forageItem = ItemQueryResolver.TryResolveRandomItem(
							forage, itemQueryContext, avoidRepeat: false, null, null, null, (query, error)
							=> Log($"Could not retrieve forage item '{query}' to spawn in region '{region.Id}' in '{__instance.Name}': {error}", LogLevel.Warn)
						);

						if (forageItem == null)
							continue;

						if (forageItem is not StardewValley.Object forageObj)
						{
							Log($"Failed to spawn non-object forage item from entry '{forage.Id}' in region '{region.Id}' in '{__instance.Name}'.", LogLevel.Warn);
						}
						else
						{
							forageObj.IsSpawnedObject = true;
							if (__instance.dropObject(forageObj, tile * 64f, Game1.viewport, initialPlacement: true))
								break;
						}
					}
				}
			}
		}
	}
}
