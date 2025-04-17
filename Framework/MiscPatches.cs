using EMU.Framework.Attributes;
using HarmonyLib;
using StardewValley;

namespace EMU.Framework;

[Feature("Misc Patches")]
internal class MiscPatches
{
	private static Config cfg = null!;

	public MiscPatches(Harmony harmony, Config config)
	{
		cfg = config;

		harmony.Patch(
			typeof(Options).GetProperty(nameof(Options.lightingQuality))!.GetMethod,
			postfix: new(typeof(MiscPatches), nameof(UpgradeLighting))
		);
	}

	private static int UpgradeLighting(int original)
	{
		return cfg.HighQualityLighting ? 2 : original;
	}
}
