using EMU.Framework;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using System.Reflection;
using System.Reflection.Emit;

namespace EMU.Features
{
	// TODO cue persistence
	// TODO crossfade
	internal class MusicRegion : IPatch
	{
		private static readonly PropertyCache<List<KeyValuePair<Rectangle, string>>> Regions =
			new("EMU_MusicRegions", ParseRegions);

		private static readonly PerScreen<string?> PrevTrack = new();

		private static IFeature.Logger Log;

		public string Name => "Music Regions (New)";

		public void Init(IFeature.Logger log, IModHelper helper)
		{
			Log = log;
		}

		public void Patch(Harmony harmony, out string? Error)
		{
			Error = null;

			harmony.Patch(
				typeof(GameLocation).GetMethod(nameof(GameLocation.HandleMusicChange)),
				transpiler: new(typeof(MusicRegion), nameof(InsertLocationMusicShift))
			);
		}

		public static void Update()
		{
			var track = GetTrackAtPosition();
			var prev = PrevTrack.Value;

			if (track == prev)
				return;

			PrevTrack.Value = track;
			Game1.changeMusicTrack(track);
		}

		public static List<KeyValuePair<Rectangle, string>> ParseRegions(GameLocation _, string? prop)
		{
			if (prop is null)
				return [];

			var split = ArgUtility.SplitBySpaceQuoteAware(prop);
			List<KeyValuePair<Rectangle, string>> regions = [];

			for (int i = 0; i < split.Length; i += 5)
			{
				if (!ArgUtility.TryGetRectangle(split, i, out var rect, out var err))
				{
					Log($"Error parsing Music Region property:\n{err}", LogLevel.Warn);
					return [];
				}

				if (!ArgUtility.TryGetRemainder(split, i, out var track, out err))
				{
					Log($"Error parsing Music Region property:\n{err}", LogLevel.Warn);
					return [];
				}

				if (!Game1.soundBank.Exists(track))
				{
					Log($"Error parsing Music Region property:\nSound with name '{track}' does not exist.", LogLevel.Warn);
					return [];
				}

				regions.Add(new(rect, track));
			}

			return regions;
		}

		public static string ReplaceTrack(string original, GameLocation where)
		{
			return GetTrackAtPosition(where) ?? original;
		}

		private static IEnumerable<CodeInstruction> InsertLocationMusicShift(IEnumerable<CodeInstruction> instructions)
		{
			var method = typeof(GameLocation).GetMethod(nameof(GameLocation.GetLocationSpecificMusic));
			foreach (var code in instructions)
			{
				yield return code;
				if (code.operand is MethodInfo mi && mi == method)
				{
					yield return new(OpCodes.Ldarg_1);
					yield return new(OpCodes.Call, typeof(MusicRegion).GetMethod(nameof(ReplaceTrack)));
				}
			}
		}

		private static string? GetTrackAtPosition(GameLocation? where = null, Farmer? who = null)
		{
			who ??= Game1.player;
			where ??= who.currentLocation;
			var regions = Regions.Get(where);
			var pos = who.TilePoint;

			for (int i = 0; i < regions.Count; i++)
				if (regions[i].Key.Contains(pos))
					return regions[i].Value;

			return null;
		}
	}
}
