using EMU.Data;
using EMU.Framework;
using EMU.Framework.Attributes;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace EMU.Features;

[Feature("Map Renovations")]
internal class MapRenovations
{
	private static IMonitor Monitor = null!;
	private static Assets Assets = null!;

	public MapRenovations(IMonitor monitor, Harmony harmony, Assets assets)
	{
		Monitor = monitor;
		Assets = assets;

		harmony.Patcher(monitor)
			.With<GameLocation>(nameof(GameLocation.MakeMapModifications)).Postfix(ApplyRenovations);
	}

	private static void ApplyRenovations(GameLocation __instance)
	{
		var name = __instance.GetId();

		if (!Assets.ExtendedData.TryGetValue(name, out var data))
			return;

		if (data.Renovations is not Dictionary<string, Renovation> renovations)
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
				Monitor.Log($"Error applying renovation '{id}' in location '{name}': {ex}", LogLevel.Warn);
			}
		}
	}
}
