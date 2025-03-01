using EMU.Framework;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace EMU.Features
{
	internal class LocalWarp : IPatch
	{
		public string Name => throw new NotImplementedException();

		private static IFeature.Logger Log = ModUtilities.LogDefault;

		public void Init(IFeature.Logger log, IModHelper helper)
		{
			Log = log;
		}

		public void Patch(Harmony harmony, out string? Error)
		{
			Error = null;

			harmony.Patch(
				typeof(GameLocation).GetMethod(nameof(GameLocation.updateWarps)),
				postfix: new(typeof(LocalWarp), nameof(AddLocalWarps))
			);
		}

		private static void AddLocalWarps(GameLocation __instance)
		{
			if (!__instance.TryGetMapProperty("LocalWarps", out var prop))
				return;

			var split = ArgUtility.SplitBySpace(prop);
			for (int i = 0; i < split.Length; i += 4)
			{
				if (!ArgUtility.TryGetRectangle(split, i, out var rect, out var error))
				{
					Log($"Failed parsing LocalWarps '{prop}' for location '{__instance.NameOrUniqueName}': {error}"
						+ ". Local warps must have 4 fields in the form of 'fromX fromY toX toY'.", LogLevel.Warn);
					return;
				}

				__instance.warps.Add(new(rect.X, rect.Y, __instance.NameOrUniqueName, rect.Width, rect.Height, false));
			}

			if (split.Length % 4 is not 0)
				Log($"Malformed LocalWarps value '{prop}' for location '{__instance.NameOrUniqueName}'. {split.Length % 4} extra fields detected.", LogLevel.Warn);
		}
	}
}
