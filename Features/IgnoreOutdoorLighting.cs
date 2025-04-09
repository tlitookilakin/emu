using EMU.Framework;
using StardewModdingAPI;
using StardewValley;

namespace EMU.Features
{
	internal class IgnoreOutdoorLighting : IFeature
	{
		public void Init(IFeature.Logger log, IModHelper helper)
		{
			ModEntry.OnLocationChanged += Update;
		}

		private void Update(GameLocation where, Farmer who)
		{
			where.ignoreOutdoorLighting.Value |= where.getMapProperty("EMU_IgnoreOutdoorLighting") is not null;
		}
	}
}
