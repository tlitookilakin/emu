using StardewValley.GameData;

namespace EMU.Framework
{
	public class ExtendedLocationData
	{
		public Dictionary<string, Renovation>? Renovations { get; set; }
		public List<ForageRegionData>? ForageRegions { get; set; }
		public List<GenericSpawnItemDataWithCondition>? DailySpawns { get; set; }
	}
}
