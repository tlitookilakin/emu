using HarmonyLib;
using MUMPs.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;

namespace MUMPs.Features
{
	internal class BuildingSpawn : IPatch
	{
		public string Name
			=> "Building Spawn";

		private static IFeature.Logger Log = ModUtilities.LogDefault;

		public void Init(IFeature.Logger log, IModHelper helper)
		{
			Log = log;
		}

		public void Patch(Harmony harmony, out string? Error)
		{
			Error = null;
			var patch = new HarmonyMethod(typeof(BuildingSpawn), nameof(AddMoreBuildings));

			harmony.Patch(typeof(GameLocation).GetMethod(nameof(GameLocation.AddDefaultBuildings)), postfix: patch);
			harmony.Patch(typeof(Farm).GetMethod(nameof(GameLocation.AddDefaultBuildings)), postfix: patch);
		}

		private static void AddMoreBuildings(GameLocation __instance)
		{
			var data = __instance.GetData();

			if (data is null)
				return;

			if (data.CustomFields is Dictionary<string, string> fields && 
				fields.TryGetValue("MUMPS/DefaultBuildings", out var value))
			{
				var split = ArgUtility.SplitBySpaceQuoteAware(value);

				for (int i = 0; i < split.Length; i += 4)
				{
					if (!ArgUtility.TryGetVector2(split, i + 1, out var tile, out var err))
					{
						Log($"Failed to read additional buildings data from {data.DisplayName}. {err}", LogLevel.Warn);
						continue;
					}

					if (ArgUtility.TryGetOptionalBool(split, i + 3, out var AllowDuplicates, out _))
						i++;

					if (AllowDuplicates)
					{
						if (__instance.getBuildingAt(tile) is null)
						{
							var building = Building.CreateInstanceFromId(split[i], tile);
							building.load();
							__instance.buildings.Add(building);
						}
					}
					else
					{
						__instance.AddDefaultBuilding(split[i], tile);
					}
				}
			}
		}
	}
}
