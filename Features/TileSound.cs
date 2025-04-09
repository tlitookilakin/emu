using Microsoft.Xna.Framework.Audio;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewModdingAPI;
using StardewValley;
using Microsoft.Xna.Framework;
using EMU.Framework;

namespace EMU.Features
{
	internal class TileSound : IFeature
	{
		public const string PROPERTY_NAME = "EMU_TileSounds";

		private static readonly PerScreen<Dictionary<ICue, List<Vector2>>> soundSources = new(() => new());
		private static readonly PerScreen<float> fadeVolume = new(() => -.5f);
		private static IFeature.Logger Logger = null!;
		private static IModHelper Helper = null!;

		public void Init(IFeature.Logger log, IModHelper helper)
		{
			Helper = helper;
			Logger = log;

			ModEntry.OnLocationChanged += PopulateSounds;
			ModEntry.OnCleanup += Cleanup;
			Helper.Events.Multiplayer.PeerDisconnected += CheckSplitScreenStop;
			Helper.Events.GameLoop.UpdateTicking += Tick;
		}

		private static void PopulateSounds(GameLocation where, Farmer who)
		{
			var data = new Dictionary<ICue, List<Vector2>>();
			var soundCache = new Dictionary<string, ICue>();
			soundSources.Value = data;

			if (!where.TryGetMapProperty(PROPERTY_NAME, out var prop))
				return;

			var split = ArgUtility.SplitBySpaceQuoteAware(prop);
			for (int i = 0; i < split.Length; i += 3)
			{
				if (
					!ArgUtility.TryGetPoint(split, i, out var tile, out var error) ||
					!ArgUtility.TryGet(split, i, out var s, out error, false)
				)
				{
					Logger($"Could not read {PROPERTY_NAME} for location {where.NameOrUniqueName}:\n{error}", LogLevel.Warn);
					return;
				}

				s = s.Trim();

				if (!soundCache.TryGetValue(s, out var cue))
				{
					if (Game1.playSound(s, out cue))
						soundCache[s] = cue;
					else
						continue;
				}

				var points = data.TryGetValue(cue, out var p) ? p : data[cue] = [];
				points.Add(new(tile.X * 64f, tile.Y * 64f));
				cue.Volume = 0f;
			}
		}
		private static void CheckSplitScreenStop(object? _, PeerDisconnectedEventArgs ev)
		{
			if (ev.Peer.IsSplitScreen)
				StopAll(soundSources.GetValueForScreen(ev.Peer.ScreenID ?? 0));
		}
		private static void StopAll(Dictionary<ICue, List<Vector2>>? which = null)
		{
			which ??= soundSources.Value;
			foreach (var cue in which.Keys)
			{
				cue.Stop(AudioStopOptions.Immediate);
			}
			which.Clear();
		}
		private static void Cleanup(Farmer who)
		{
			fadeVolume.Value = -.5f;
			StopAll();
		}
		private static void Tick(object? sender, UpdateTickingEventArgs ev)
		{

			//fadeVolume.Value = Math.Min(1f, fadeVolume.Value + (float)Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds * .0003f);

			var vol = Math.Min(Game1.ambientPlayerVolume, Game1.options.ambientVolumeLevel);
			var pos = Game1.player.Position;

			foreach ((var cue, var points) in soundSources.Value)
			{
				float nearest = float.PositiveInfinity;
				foreach (var point in points)
					nearest = MathF.Min(nearest, Vector2.Distance(point, pos));

				if (nearest > 1536)
				{
					cue.Pause();
					continue;
				}

				nearest = MathF.Min(1f - nearest / 1536, fadeVolume.Value);
				cue.Volume = MathF.Pow(nearest, 5f) * vol * 100f;
				if (cue.IsPaused)
					cue.Resume();
				else if (!cue.IsPlaying)
					cue.Play();
			}
		}
	}
}
