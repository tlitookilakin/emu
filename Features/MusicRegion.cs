using EMU.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.GameData;
using System.Reflection;

namespace EMU.Features
{
	// TODO double-check overrides
	// TODO add bgm injection
	internal class MusicRegion : IFeature
	{
		private static readonly PerScreen<MusicState> State = new(() => new());
		private static int ActiveScreen;
		private static float MainGameVolume = 1f;
		private static Func<Game1, MusicContext>? ContextGetter;
		private static Func<Game1, Dictionary<MusicContext, KeyValuePair<string, bool>>>? TrackGetter;
		private static IFeature.Logger Log = ModUtilities.LogDefault;

		public void Init(IFeature.Logger log, IModHelper helper)
		{
			Log = log;

			string? err = null;

			try
			{
				ContextGetter =
					typeof(Game1)
					.GetField("_instanceActiveMusicContext", IPatch.AnyDeclared | BindingFlags.Instance)
					?.CreateFieldGetter<Game1, MusicContext>();

				TrackGetter =
					typeof(Game1)
					.GetField("_instanceRequestedMusicTracks", IPatch.AnyDeclared | BindingFlags.Instance)
					?.CreateFieldGetter<Game1, Dictionary<MusicContext, KeyValuePair<string, bool>>>();
			}
			catch (Exception ex)
			{
				err = ex.Message;
			}

			if (err is null && (ContextGetter is null || TrackGetter is null))
				err = "Failed to find field";

			if (err is not null)
			{
				Log($"MusicRegion failed to create getter and will be disabled: {err}.", LogLevel.Error);
				return;
			}

			ModEntry.OnLocationChanged += Update;
			ModEntry.OnCleanup += Cleanup;
			helper.Events.GameLoop.UpdateTicked += Tick;
		}

		private void Tick(object? sender, UpdateTickedEventArgs e)
		{
			UpdateCore();
		}

		private void Update(GameLocation where, Farmer who)
		{
			UpdateRegions(where);
			UpdateCore(true);
		}

		private class MusicState
		{
			public List<(Rectangle region, string cue)> Regions = new();
			public Point lastTile;
			public string? lastCue;
			public Dictionary<ICue, bool> cueIsFade = new();
			public List<ICue> oldCues = new();
			public bool channelFade;
			public string? baseMusic;
		}

		#region legacy

		private static void UpdateRegions(GameLocation where)
		{
			var state = State.Value;
			state.Regions.Clear();

			state.baseMusic = where.GetLocationSpecificMusic();

			if (!where.TryGetMapProperty("MusicRegions", out var prop))
				return;

			var split = ArgUtility.SplitBySpaceQuoteAware(prop);
			string? error = null;
			var knownCues = new List<string>();

			for (int i = 0; i < split.Length; i += 5)
			{
				if (ArgUtility.TryGetRectangle(split, i, out var rect, out error))
				{
					error = null;
					if (i + 4 >= split.Length)
					{
						error = "Missing cue name";
						break;
					}
					state.Regions.Add((rect, split[i + 4]));
					knownCues.Add(split[i + 4]);
				}
			}

			if (error is not null)
				Log($"Failed to parse MusicRegions '{prop}' for location '{where.NameOrUniqueName}': {error}." +
					" Expected format is 'X, Y, Width, Height, cue name'.", LogLevel.Warn);

			List<ICue> toRemove = new();
			foreach (var cue in state.cueIsFade.Keys)
			{
				if (!knownCues.Contains(cue.Name))
				{
					if (cue.Volume > 0f)
					{
						state.cueIsFade[cue] = false;
						state.oldCues.Add(cue);
					}
					else
					{
						toRemove.Add(cue);
					}
				}
			}
			for (int i = 0; i < toRemove.Count; i++)
			{
				var cue = toRemove[i];
				cue.Stop(AudioStopOptions.Immediate);
				state.cueIsFade.Remove(cue);
				cue.Dispose();
			}
		}
		private static void Cleanup(Farmer who)
		{
			State.Value = new();
		}
		private static void UpdateScreenVolume()
		{
			if (Game1.game1.IsMainInstance)
			{
				ActiveScreen = Game1.game1.instanceId;
				bool OverrideMain = false;
				foreach (var game in GameRunner.instance.gameInstances)
				{
					var tracks = TrackGetter!(game);
					var ctx = ContextGetter!(game);
					var state = State.GetValueForScreen(game.instanceId);

					if (tracks.TryGetValue(ctx, out var item) && item.Key == "mermaidSong")
						ctx = MusicContext.MAX;

					if (ctx > MusicContext.SubLocation)
					{
						OverrideMain = false;
						ActiveScreen = Game1.game1.instanceId;
						break;
					}
					if (state.lastCue != "" && !OverrideMain)
					{
						if (game.IsMainInstance)
							OverrideMain = true;
						ActiveScreen = game.instanceId;
					}
				}
				State.Value.channelFade = ActiveScreen == Game1.game1.instanceId && OverrideMain;
			}
			else
			{
				State.Value.channelFade = ActiveScreen == Game1.game1.instanceId;
			}
		}
		private static void UpdateTrackVolume()
		{
			var state = State.Value;
			foreach ((var cue, var fade) in state.cueIsFade)
			{
				if (fade && state.channelFade)
				{
					if (cue.IsPaused)
						cue.Resume();
					else if (!cue.IsPlaying)
						cue.Play();
					cue.Volume = MathF.Min(1f, cue.Volume + .01f);
				}
				else
				{
					cue.Volume = MathF.Max(0f, cue.Volume - .01f);
					if (cue.Volume == 0 && !cue.IsPaused)
						cue.Pause();
				}
			}
			for (int i = state.oldCues.Count - 1; i >= 0; i--)
			{
				var n = state.oldCues[i];
				if (!n.IsPlaying || n.Volume == 0)
				{
					n.Stop(AudioStopOptions.Immediate);
					state.cueIsFade.Remove(n);
					n.Dispose();
					state.oldCues.RemoveAt(i);
				}
			}
			if (Game1.game1.IsMainInstance)
			{
				if (state.channelFade)
					MainGameVolume = MathF.Max(0f, MainGameVolume - .01f);
				else
					MainGameVolume = MathF.Min(1f, MainGameVolume + .01f);
				if (Game1.currentSong is not null)
					Game1.currentSong.Volume = MainGameVolume;
			}
		}
		private static void UpdateCore(bool location_changed = false)
		{
			var pos = Game1.player.TilePoint;
			var state = State.Value;

			bool changed = location_changed || state.lastTile != pos;
			state.lastTile = pos;

			if (!Context.IsWorldReady)
				return;

			UpdateScreenVolume();
			UpdateTrackVolume();

			if (Game1.currentLocation is null || !changed)
				return;

			var oldCue = state.lastCue;

			state.lastCue = state.baseMusic;
			foreach ((var region, string song) in state.Regions)
			{
				if (region.Contains(pos))
				{
					state.lastCue = song;
					break;
				}
			}

			if (oldCue == state.lastCue)
				return;

			bool fade = ActiveScreen == Context.ScreenId;
			bool foundCue = false;
			var id = state.lastCue;
			foreach (var cue in state.cueIsFade.Keys)
			{
				if (cue.Name == id)
				{
					foundCue = true;
					state.cueIsFade[cue] = fade;
				}
				else
				{
					state.cueIsFade[cue] = false;
				}
			}
			if (!foundCue && id != "")
			{
				if (Game1.soundBank.TryGetCue(id, out var cue))
					state.cueIsFade.Add(cue, fade);
				else
					Log($"Could not find music track named '{id}' @ {Game1.currentLocation?.NameOrUniqueName}", LogLevel.Warn);
			}
		}

		#endregion legacy
	}
}
