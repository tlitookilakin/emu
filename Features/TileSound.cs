using StardewModdingAPI.Events;
using StardewModdingAPI;
using StardewValley;
using EMU.Framework.Attributes;
using EMU.Framework;
using StardewModdingAPI.Utilities;
using HarmonyLib;
using StardewValley.BellsAndWhistles;

namespace EMU.Features;

[Feature("Tile Sound")]
internal class TileSound
{
	public const string PROPERTY_NAME = "EMU_TileSound";

	private static readonly Dictionary<string, ICue> cues = [];
	private readonly IModHelper Helper;
	private readonly TileCache<string> SoundPoints;
	private static readonly PerScreen<float> LocationFade = new();
	private float updateTimer;

	public TileSound(IModHelper helper, ICacheProvider tileCache, Harmony harmony)
	{
		Helper = helper;
		SoundPoints = tileCache.CreateTileCache(PROPERTY_NAME, "Paths", static (g, p, s) => s);

		Helper.Events.GameLoop.UpdateTicking += Tick;

		harmony.Patch(
			typeof(AmbientLocationSounds).GetMethod(nameof(AmbientLocationSounds.onLocationLeave)),
			postfix: new(typeof(TileSound), nameof(StartFade))
		);
	}

	private static void StartFade()
	{
		LocationFade.Value = -.5f;
	}

	private void Tick(object? sender, UpdateTickingEventArgs ev)
	{
		if (ev.Ticks is 0)
			return;

		var elapsed = Game1.currentGameTime.ElapsedGameTime.Milliseconds;
		updateTimer -= elapsed;

		var fade = LocationFade.Value = Math.Min(1f, LocationFade.Value + elapsed * .0003f);

		if (updateTimer > 0)
			return;

		//var vol = Math.Min(Game1.ambientPlayerVolume, Game1.options.ambientVolumeLevel);
		var pos = Game1.player.TilePoint;
		var volumes = new Dictionary<string, int>();
		
		foreach ((var tile, var name) in SoundPoints.GetAll(Game1.currentLocation))
		{
			int d = (int)(pos.Distance(tile) * 64.0);

			if (!volumes.TryGetValue(name, out int dist) || d > dist)
				volumes[name] = d;
		}

		foreach ((var name, var dist) in volumes)
		{
			if (dist > 1536)
				continue;

			if (!cues.TryGetValue(name, out var cue))
			{
				if (!Game1.soundBank.Exists(name))
					continue;

				cues[name] = cue = Game1.soundBank.GetCue(name);
			}

			cue.Volume = MathF.Min(fade, 1f - dist / 1536f);
		}

		List<string> toRemove = [];

		foreach (var name in cues.Keys)
			if (!volumes.ContainsKey(name))
				toRemove.Add(name);

		foreach (var remove in toRemove)
		{
			cues[remove].Dispose();
			cues.Remove(remove);
		}
	}
}
