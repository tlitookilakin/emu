﻿using EMU.Framework.Attributes;
using StardewValley;

namespace EMU.Features;

[Feature("Ignore Outdoor Lighting")]
internal class IgnoreOutdoorLighting
{
	public IgnoreOutdoorLighting()
	{
		ModEntry.OnLocationChanged += Update;
	}

	private void Update(GameLocation where, Farmer who)
	{
		where.ignoreOutdoorLighting.Value |= where.getMapProperty("EMU_IgnoreOutdoorLighting") is not null;
	}
}
