using EMU.Framework;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Triggers;

namespace EMU.Features
{
	internal class ActionTrigger : ITileAction
	{
		private static IFeature.Logger Log = null!;

		public string ID => "Trigger";

		public Func<GameLocation, string[], Farmer, Point, bool>? DoAction
			=> TileTrigger;

		public Action<GameLocation, string[], Farmer, Vector2>? DoTileAction 
			=> null;

		public void Init(IFeature.Logger log, IModHelper helper)
		{
			Log = log;
		}

		private static bool TileTrigger(GameLocation where, string[] args, Farmer who, Point tile)
		{
			if (args.Length is 1)
			{
				Log("Could not trigger action, no action specified.", LogLevel.Warn); 
				return false;
			}

			if(!TriggerActionManager.TryRunAction(string.Join(' ', args[2..]), out var err, out _))
			{
				Log(err, LogLevel.Warn);
				return false;
			}

			return true;
		}
	}
}
