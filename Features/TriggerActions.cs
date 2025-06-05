using EMU.Framework;
using EMU.Framework.Attributes;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Triggers;
using System.Reflection;
using static StardewValley.GameStateQuery;

namespace EMU.Features;

[Feature("Trigger Actions")]
public class TriggerActions
{
	public TriggerActions()
	{
			foreach (var method in typeof(TriggerActions).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly))
				TriggerActionManager.RegisterAction($"EMU_{method.Name}", method.CreateDelegate<TriggerActionDelegate>());
		}

	public static bool RefreshRenovations(string[] args, TriggerActionContext context, out string? error)
	{
		GameLocation? location = null;
		if (!Helpers.TryGetLocationArg(args, 0, ref location, out error))
			return false;

		location?.MakeMapModifications();
		return true;
	}

	public static bool ReloadMap(string[] args, TriggerActionContext context, out string? error)
	{
		GameLocation? location = null;
		if (!Helpers.TryGetLocationArg(args, 0, ref location, out error))
			return false;

		location?.reloadMap();
		location?.MakeMapModifications(true);
		return true;
	}

	public static bool UpgradeBuilding(string[] args, TriggerActionContext context, out string? error)
	{
		GameLocation? location = null;
		if (!Helpers.TryGetLocationArg(args, 0, ref location, out error) || location is null ||
			!ArgUtility.TryGet(args, 1, out var from, out error, false) ||
			!ArgUtility.TryGet(args, 2, out var to, out error, false))
			return false;

		if (!DataLoader.Buildings(Game1.content).TryGetValue(to, out var upgrade))
		{
			error = $"Building with type {to} does not exist";
			return false;
		}

		if (upgrade.BuildingToUpgrade != from)
		{
			error = $"Building {from} cannot be upgraded to {to}";
			return false;
		}

		foreach (var building in location.buildings)
		{
			if (building.buildingType.Value != from)
				continue;

			building.upgradeName.Value = to;
			building.FinishConstruction();
		}

		return true;
	}

	public static bool SetModData(string[] args, TriggerActionContext context, out string? error)
	{
		if (!ArgUtility.TryGet(args, 0, out var type, out error))
			return false;

		IHaveModData? target = null;
		int argNum = 1;

		switch (type)
		{
			case "Location":
				GameLocation? where = context.TriggerArgs.FirstOfType<GameLocation, object>();
				if (!Helpers.TryGetLocationArg(args, argNum++, ref where, out error))
					return false;

				target = where;
				break;

			case "Farmer":
				target = Game1.player;
				break;

			case "Character":
				if (!ArgUtility.TryGet(args, argNum++, out var charName, out error))
					return false;

				if (Game1.getCharacterFromName(charName) is not NPC npc)
				{
					error = $"Could not find character named '{charName}'";
					return false;
				}

				target = npc;
				break;
		}

		if (target == null)
			return false;

		if (!ArgUtility.TryGet(args, argNum++, out var key, out error) ||
			!ArgUtility.TryGetOptional(args, argNum++, out var value, out error))
			return false;

		if (value is null or "")
			target.modData.Remove(key);
		else
			target.modData[key] = value;

		return true;
	}
}
