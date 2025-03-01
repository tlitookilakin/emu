using EMU.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace EMU
{
	public class ModEntry : Mod
	{
		public const string MOD_ID = "tlitoo.mumps";

		internal delegate void LocationChanged(GameLocation where, Farmer who);
		internal static event LocationChanged? OnLocationChanged;
		internal static event Action<Farmer>? OnCleanup;
		internal static Config config = new();
		internal static IModHelper? helper;

		private static readonly PerScreen<GameLocation> lastLocation = new();

		public override void Entry(IModHelper helper)
		{
			helper = Helper;
			Helper.Events.GameLoop.GameLaunched += OnLaunch;
			Helper.Events.GameLoop.UpdateTicking += OnTick;

			config = Helper.ReadConfig<Config>();
			I18n.Init(Helper.Translation);
			Assets.Init(Helper, Monitor.Log);
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

		private void OnLaunch(object? sender, GameLaunchedEventArgs e)
		{
			MiscPatches.OnMapUpdate = CheckMapRemodel;

			IFeature.InitAll(Monitor.Log, Helper);
			IPatch.PatchAll(ModManifest.UniqueID, Monitor.Log);
			ITileAction.RegisterAll(Monitor.Log);

			TriggerActions.RegisterActions();
		}

		private static void CheckMapRemodel(GameLocation where)
		{
			if (Game1.currentLocation == where)
				OnLocationChanged?.Invoke(where, Game1.player);
		}
	}
}
