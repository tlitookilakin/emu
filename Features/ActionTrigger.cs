using EMU.Framework.Attributes;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Triggers;

namespace EMU.Features;

[Feature("Tile Trigger")]
internal class ActionTrigger(IMonitor Monitor)
{

	[TileAction("Trigger")]
	public bool TileTrigger(GameLocation where, string[] args, Farmer who, Point tile)
	{
		if (args.Length is 1)
		{
			Monitor.Log("Could not trigger action, no action specified.", LogLevel.Warn); 
			return false;
		}

		if(!TriggerActionManager.TryRunAction(string.Join(' ', args[2..]), out var err, out _))
		{
			Monitor.Log(err, LogLevel.Warn);
			return false;
		}

		return true;
	}
}
