using HarmonyLib;
using Microsoft.Xna.Framework;
using MUMPs.Framework;
using StardewModdingAPI;
using StardewValley;

namespace MUMPs.Features
{
	internal class MapRenovations : IPatch
	{
		public string Name => "Map Renovations";

		private static IFeature.Logger Log = ModUtilities.LogDefault;

		public void Init(IFeature.Logger log, IModHelper helper)
		{
			Log = log;
		}

		public void Patch(Harmony harmony, out string? Error)
		{
			Error = null;
			harmony.Patch(
				typeof(GameLocation).GetMethod(nameof(GameLocation.MakeMapModifications)),
				postfix: new(typeof(MapRenovations), nameof(ApplyRenovations))
			);
		}

		private static void ApplyRenovations(GameLocation __instance)
		{
			var name = __instance is Farm ? "Farm_" + Game1.GetFarmTypeKey() : __instance.Name;

			if (!Assets.Renovations.TryGetValue(name, out var renovations))
				return;

			foreach (var (id, renovation) in renovations)
			{
				if (renovation.Condition is string query && !GameStateQuery.CheckConditions(query, __instance))
					continue;

				var destRect =
					!renovation.SourceRegion.HasValue ? null :
					!renovation.Destination.HasValue ? renovation.SourceRegion :
					new Rectangle(renovation.Destination.Value, renovation.SourceRegion.Value.Size);

				try
				{
					__instance.ApplyMapOverride(renovation.SourceMap, id, renovation.SourceRegion, destRect);
				}
				catch (Exception ex)
				{
					Log($"Error applying renovation '{id}' in location '{name}': {ex}", LogLevel.Warn);
				}
			}
		}
	}
}
