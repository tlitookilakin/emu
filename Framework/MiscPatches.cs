using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace MUMPs.Framework
{
	internal class MiscPatches : IPatch
	{
		public string Name => "Misc Patches";
		public static Action<GameLocation> OnMapUpdate = g => { };

		public void Init(IFeature.Logger log, IModHelper helper)
		{
		}

		public void Patch(Harmony harmony, out string? Error)
		{
			Error = null;

			harmony.Patch(
				typeof(GameLocation).GetMethod(nameof(GameLocation.SortLayers)),
				postfix: new(typeof(MiscPatches), nameof(RefreshLayers))
			);

			harmony.Patch(
				typeof(Options).GetProperty(nameof(Options.lightingQuality))!.GetMethod,
				postfix: new(typeof(MiscPatches), nameof(UpgradeLighting))
			);
		}

		private static void RefreshLayers(GameLocation __instance)
		{
			OnMapUpdate(__instance);
		}

		private static int UpgradeLighting(int original)
		{
			return ModEntry.config.HighQualityLighting ? 2 : original;
		}
	}
}
