using EMU.Framework;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace EMU.Features
{
	internal class ActionMulti : ITileAction
	{
		public string ID => "MultiAction";

		public Func<GameLocation, string[], Farmer, Point, bool>? DoAction => DoTile;

		public Action<GameLocation, string[], Farmer, Vector2>? DoTileAction => DoTouch;

		public void Init(IFeature.Logger log, IModHelper helper) { }

		private static bool DoTile(GameLocation where, string[] args, Farmer who, Point tile)
		{
			bool ret = false;
			xTile.Dimensions.Location tileLoc = new(tile.X, tile.Y);
			foreach (var action in ParseActions(args))
			{
				ret = ret || where.performAction(action, who, tileLoc);
			}
			return ret;
		}

		private static void DoTouch(GameLocation where, string[] args, Farmer who, Vector2 tile)
		{
			foreach (var action in ParseActions(args))
			{
				where.performTouchAction(action, tile);
			}
		}

		private static IEnumerable<string[]> ParseActions(string[] source)
		{
			int lastIndex = 0;
			for (int i = 0; i < source.Length; i++)
			{
				if (source[i] == "|")
				{
					if (lastIndex < i)
						yield return source[lastIndex..i];
					lastIndex = i + 1;
				}

				if (source[i] is "\\|")
					source[i] = "|";
			}
		}
	}
}
