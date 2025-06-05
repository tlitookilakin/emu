using EMU.Framework;
using EMU.Framework.Attributes;
using HarmonyLib;
using StardewValley;
using StardewValley.GameData.Locations;
using StardewValley.Locations;
using StardewValley.Network;
using System.Reflection;

namespace EMU.Features;

[Feature("Instanced Locations")]
internal class PlayerInstancedLocations
{
	const string FLAG = "EMU_PlayerInstanced";

	public PlayerInstancedLocations(HarmonyHelper harmony)
	{
		harmony
			.With<Game1>(nameof(Game1.AddLocations)).Postfix(AddInstancedLocations)
			.WithAll(nameof(Game1.warpFarmer), static (m) => {
				var par = m.GetParameters();
				return par.Length > 0 && par[0].ParameterType == typeof(string);
			}).Prefix(ModifyWarpTarget)
			.With<Client>("setUpGame").Postfix(AddInstancedLocations);
	}

	private static bool IsInstanced(LocationData location)
	{
		return location.CustomFields is not null && location.CustomFields.ContainsKey(FLAG);
	}

	private static bool IsInstanced(string name)
	{
		return DataLoader.Locations(Game1.content).TryGetValue(name, out var data) && IsInstanced(data);
	}

	private static void AddInstancedLocations()
	{
		int count = Game1.netWorldState.Value.HighestPlayerLimit;
		foreach (var (key, data) in DataLoader.Locations(Game1.content))
		{
			if (IsInstanced(data))
			{
				for (int i = 1; i < count; i++)
				{
					var loc = Game1.CreateGameLocation(key);
					loc.name.Value += i + 1;
					Game1.locations.Add(loc);
				}
			}
		}
	}

	private static void ModifyWarpTarget(ref string __0)
	{
		if (IsInstanced(__0))
		{
			int suffix;
			var where = Game1.currentLocation;
			if (where is FarmHouse house)
			{
				suffix = GetInstanceIndex(house.owner);
			}
			else if (where is Cellar || IsInstanced(where.GetData()))
			{
				__0 += FindNumberSuffix(where.NameOrUniqueName);
				return;
			}
			else
			{
				suffix = GetInstanceIndex();
			}

			if (suffix is not 0 or 1)
				__0 += suffix;
		}
	}

	private static string FindNumberSuffix(string name)
	{
		for (int i = 0; i < name.Length; i++)
			if (name[i] is >= '0' and <= '9')
				return name[i..];

		return "";
	}

	private static int GetInstanceIndex(Farmer? who = null)
	{
		who ??= Game1.player;
		long id = who.UniqueMultiplayerID;

		foreach ((var k, var v) in who.team.cellarAssignments.Pairs)
			if (v == id)
				return k;

		return 0;
	}
}
