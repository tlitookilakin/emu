using MUMPs.Framework;
using StardewModdingAPI;
using StardewValley;

namespace MUMPs.Features
{
	internal class IgnoreOutdoorLighting : IFeature
	{
		public void Init(IFeature.Logger log, IModHelper helper)
		{
			ModEntry.OnLocationChanged += Update;
		}

		private void Update(GameLocation where, Farmer who)
		{
			where.ignoreOutdoorLighting.Value = 
				where.ignoreOutdoorLighting.Value || 
				where.getMapProperty("IgnoreOutdoorLighting") is not null;
		}
	}
}
