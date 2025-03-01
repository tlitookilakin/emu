using EMU.Framework;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using System.Reflection;
using System.Reflection.Emit;
using static EMU.ModEntry;

namespace EMU.Features
{
	internal class CustomSteps : IPatch
	{
		private const string PROP_SOUND = MOD_ID + "_StepSound";
		private const string PROP_SPRITE = MOD_ID + "_StepSprite";

		private static IFeature.Logger Log = ModUtilities.LogDefault;
		private static readonly Color splashColor = new(141, 181, 216, 91);

		public string Name => "Custom Footsteps";

		public void Init(IFeature.Logger log, IModHelper helper)
		{
			Log = log;
		}

		public void Patch(Harmony harmony, out string? error)
		{
			error = null;
			harmony.Patch(
				typeof(FarmerSprite).GetMethod("checkForFootstep", IPatch.AnyDeclared | BindingFlags.Instance),
				transpiler: new(typeof(CustomSteps), nameof(FootstepPatch))
			);
		}

		private static IEnumerable<CodeInstruction>? FootstepPatch(IEnumerable<CodeInstruction> source, ILGenerator gen)
		{
			var il = new CodeMatcher(source, gen);

			// string a = this.currentStep;
			il.MatchEndForward(
				new(OpCodes.Ldarg_0),
				new(OpCodes.Ldfld, typeof(FarmerSprite).GetField(nameof(FarmerSprite.currentStep))),
				new(OpCodes.Stloc_2)
			)
			.Advance(1);

			if (il.IsInvalid)
			{
				Log("Custom steps failed: Could not find first anchor.", LogLevel.Error);
				return null;
			}

			// a = SetStepSound(this.Owner, this);
			il.InsertAndAdvance(
				new(OpCodes.Ldarg_0),
				new(OpCodes.Callvirt, typeof(FarmerSprite).GetProperty(nameof(FarmerSprite.Owner))!.GetMethod),
				new(OpCodes.Ldarg_0),
				new(OpCodes.Call, typeof(CustomSteps).GetMethod(nameof(SetStepSound))),
				new(OpCodes.Stloc_2)
			)
			// if (a is not null)
			.MatchStartForward(
				new(OpCodes.Ldloc_2),
				new(OpCodes.Brfalse)
			);

			if (il.IsInvalid)
			{
				Log("Custom steps failed: Could not find second anchor.", LogLevel.Error);
				return null;
			}

			// AddStepFX(a, this.Owner);
			il.Insert(
				new CodeInstruction(OpCodes.Ldloc_2).MoveLabelsFrom(il.Instruction),
				new(OpCodes.Ldarg_0),
				new(OpCodes.Callvirt, typeof(FarmerSprite).GetProperty(nameof(FarmerSprite.Owner))!.GetMethod),
				new(OpCodes.Call, typeof(CustomSteps).GetMethod(nameof(AddStepFX)))
			);

			return il.InstructionEnumeration();
		}

		public static string SetStepSound(Farmer who, FarmerSprite sprite)
		{
			var pos = who.TilePoint;
			var loc = who.currentLocation;

			return
				loc.doesTileHaveProperty(pos.X, pos.Y, PROP_SOUND, "Back") ??
				(UseWaterStep(loc, pos) ? "quickSlosh" : sprite.currentStep);
		}

		public static void AddStepFX(string what, Farmer who)
		{
			var pos = who.Position;
			var where = who.currentLocation;

			if (where is null)
				return;

			string? str =
				where.doesTileHaveProperty((int)pos.X / 64, (int)pos.Y / 64, PROP_SPRITE, "Back") ??
				(what is "quickSlosh" ? "water_ripple water_splash" : null);

			if (str is null)
				return;

			var split = ArgUtility.SplitBySpaceQuoteAware(str);
			foreach (var id in split)
				if (Assets.TempSprites.TryGetValue(id, out var sprite))
					sprite.Emit(where, pos);
		}

		public static bool UseWaterStep(GameLocation where, Point tile)
		{
			return
				where is not BoatTunnel &&
				where.getMapProperty("NoSplashSteps") is null &&
				where.doesTileHaveProperty(tile.X, tile.Y, "Water", "Back") is not null &&
				where.getTileIndexAt(tile, "Buildings") is -1;
		}
	}
}
