using EMU.Framework;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Locations;
using StardewValley.Network;
using System.Reflection;

namespace EMU.Features
{
	internal class PlayerInstancedLocations : IPatch
	{
		const string FLAG = ModEntry.MOD_ID + "_PlayerInstanced";

		public string Name => "Instanced Locations";

		private static IFeature.Logger Log = ModUtilities.LogDefault;

		public void Init(IFeature.Logger log, IModHelper helper)
		{
			Log = log;
		}

		public void Patch(Harmony harmony, out string? Error)
		{
			Error = null;
			harmony.Patch(
				typeof(Game1).GetMethod(nameof(Game1.AddLocations)),
				postfix: new(typeof(PlayerInstancedLocations), nameof(AddInstancedLocations))
			);
			harmony.Patch(
				typeof(Client).GetMethod("setUpGame", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
				postfix: new(typeof(PlayerInstancedLocations), nameof(AddInstancedLocations))
			);

			var warpPatch = new HarmonyMethod(typeof(PlayerInstancedLocations), nameof(ModifyWarpTarget));

			foreach (var method in typeof(Game1).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
			{
				if (method.Name is nameof(Game1.warpFarmer))
				{
					var param = method.GetParameters();
					if (param.Length > 0 && param[0].ParameterType == typeof(string))
					{
						harmony.Patch(method, prefix: warpPatch);
					}
				}
			}
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
				var suffix = GetInstanceIndex();
				if (suffix is not 0)
					__0 += suffix;
			}
		}

		private static int GetInstanceIndex()
		{
			long id = Game1.player.UniqueMultiplayerID;

			foreach (int i in Game1.player.team.cellarAssignments.Keys)
				if (Game1.player.team.cellarAssignments[i] == id)
					return i;

			return 0;
		}
	}
}
