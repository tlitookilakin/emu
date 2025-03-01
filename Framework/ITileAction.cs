using Microsoft.Xna.Framework;
using StardewGenTools;
using StardewValley;

namespace EMU.Framework
{
	[Collector("Actions")]
	internal partial interface ITileAction : IFeature
	{
		public string ID { get; }

		public Func<GameLocation, string[], Farmer, Point, bool>? DoAction { get; }

		public Action<GameLocation, string[], Farmer, Vector2>? DoTileAction { get; }

		public static void RegisterAll(Logger Log)
		{
			for (int i = 0; i < ActionCount; i++)
			{
				var action = Actions[i];

				if (action.DoAction is Func<GameLocation, string[], Farmer, Point, bool> use)
					GameLocation.RegisterTileAction(action.ID, use);

				if (action.DoTileAction is Action<GameLocation, string[], Farmer, Vector2> touch)
					GameLocation.RegisterTouchAction(action.ID, touch);
			}
		}
	}
}
