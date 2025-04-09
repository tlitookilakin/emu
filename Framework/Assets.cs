using StardewModdingAPI;
using StardewModdingAPI.Events;
using static EMU.ModEntry;

namespace EMU.Framework
{
	internal class Assets
	{
		const string SPRITE_DATA = MOD_ID + "/Sprites";
		const string EXTENDED_DATA = MOD_ID + "/ExtendedLocationData";

		public static Dictionary<string, TemporarySpriteData> TempSprites
			=> tempSprites ??= content.Load<Dictionary<string, TemporarySpriteData>>(SPRITE_DATA);
		private static Dictionary<string, TemporarySpriteData>? tempSprites;

		public static Dictionary<string, ExtendedLocationData> ExtendedData
			=> extendedData ??= content.Load<Dictionary<string, ExtendedLocationData>>(EXTENDED_DATA);
		private static Dictionary<string, ExtendedLocationData>? extendedData;

		private static IFeature.Logger Log = ModUtilities.LogDefault;

		private static IGameContentHelper content = null!;

		public static bool TryLoad<T>(string name, out T asset, bool showError = true) where T : notnull
		{
			try
			{
				asset = content.Load<T>(name);
				return true;
			}
			catch (Exception ex)
			{
				Log($"Failed to load asset '{name}': {ex}", showError ? LogLevel.Warn : LogLevel.Trace);
			}
			asset = default!;
			return false;
		}

		public static void Init(IModHelper helper, IFeature.Logger logger)
		{
			Log = logger;
			content = helper.GameContent;

			helper.Events.Content.AssetRequested += Requested;
			helper.Events.Content.AssetsInvalidated += Invalidated;
		}

		private static void Invalidated(object? sender, AssetsInvalidatedEventArgs e)
		{
			foreach (var name in e.NamesWithoutLocale)
			{
				if (name.IsEquivalentTo(SPRITE_DATA))
					tempSprites = null;
				else if (name.IsEquivalentTo(EXTENDED_DATA))
					extendedData = null;
			}
		}

		private static void Requested(object? sender, AssetRequestedEventArgs e)
		{
			if (e.NameWithoutLocale.IsEquivalentTo(SPRITE_DATA))
				e.LoadFromModFile<Dictionary<string, TemporarySpriteData>>("assets/sprites.json", AssetLoadPriority.Low);
			else if (e.NameWithoutLocale.IsEquivalentTo(EXTENDED_DATA))
				e.LoadFrom(static () => new Dictionary<string, ExtendedLocationData>(), AssetLoadPriority.Low);
		}
	}
}
