using StardewValley;

namespace EMU.Framework
{
	public static class Extensions
	{
		public static string GetId(this GameLocation where)
			=> where is Farm ? "Farm_" + Game1.GetFarmTypeKey() : where.Name;
	}
}
