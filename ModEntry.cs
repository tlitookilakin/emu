using EMU.Framework;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace EMU
{
	public class ModEntry : Mod
	{
		public const string MOD_ID = "tlitoo.emu";

		internal delegate void LocationChanged(GameLocation where, Farmer who);
		internal static event LocationChanged? OnLocationChanged;
		internal static event Action<Farmer>? OnCleanup;

		private static readonly PerScreen<GameLocation> lastLocation = new();

		public override void Entry(IModHelper helper)
		{
			RegisterProviders();
			Helper.Events.GameLoop.GameLaunched += OnLaunch;
		}

		private void RegisterProviders()
		{
			var harmony = new Harmony(ModManifest.UniqueID);

			Core.Provide(Monitor);
			Core.Provide(Helper);
			Core.Provide(ModManifest);
			Core.Provide(new Assets(Helper, Monitor));
			Core.Provide(harmony);
			Core.Provide(Helper.ReadConfig<Config>());
			Core.Provide<IPropertyCacheProvider>(new PropertyCacheProvider());
			Core.Provide<ITileCacheProvider>(new TileCacheProvider(harmony, Helper));
		}

		private void OnLaunch(object? sender, GameLaunchedEventArgs e)
		{
			Monitor.Log("Initializing...", LogLevel.Info);
			Helper.Events.GameLoop.UpdateTicking += OnTick;
			Core.Init(Monitor);
			Monitor.Log("Fully initialized.", LogLevel.Info);
		}

		private void OnTick(object? sender, UpdateTickingEventArgs e)
		{
			if (Game1.currentLocation != lastLocation.Value)
			{
				lastLocation.Value = Game1.currentLocation;
				if (Game1.currentLocation is not null)
					OnLocationChanged?.Invoke(Game1.currentLocation, Game1.player);
				else
					OnCleanup?.Invoke(Game1.player);
			}
		}
	}
}
