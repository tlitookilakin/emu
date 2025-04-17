using Microsoft.Xna.Framework;
using StardewValley.GameData.Locations;

namespace EMU.Data;

public class ForageRegionData
{
	public string? Id { get; set; }
	public List<SpawnForageData>? Forage { get; set; }
	public Rectangle Region { get; set; }
	public string? Condition { get; set; }
	public List<string>? RequiredTerrainType { get; set; }
	public int Min { get; set; }
	public int Max { get; set; }
}
