using HarmonyLib;
using MUMPs.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Mods;
using xTile.Layers;

namespace MUMPs.Features
{
	internal class LightingLayer : IPatch
	{
		private static readonly PerScreen<List<Layer>> lightingLayers = new(() => new());
		private const int tileScale = Game1.tileSize / 16;

		public string Name => "Lighting Layer";

		public void Init(IFeature.Logger log, IModHelper helper)
		{
			helper.Events.Display.RenderingStep += OnRendering;

			ModEntry.OnLocationChanged += ChangeLocation;
		}

		private void ChangeLocation(GameLocation where, Farmer who)
		{
			var layers = lightingLayers.Value;
			layers.Clear();

			var sort = new List<(Layer layer, int priority)>();
			foreach(var layer in where.map.Layers)
			{
				if (layer.Id.StartsWith("Lighting"))
				{
					int sortIndex = 0;
					string sortString = layer.Id[8..];
					if (sortString.Length <= 0 || int.TryParse(sortString, out sortIndex))
					{
						sort.Add((layer, sortIndex));
						break;
					}
				}
			}
			sort.Sort((a, b) => a.priority.CompareTo(b.priority));
			layers.AddRange(sort.Select(i => i.layer));
		}

		private void OnRendering(object? sender, RenderingStepEventArgs e)
		{
			if (e.Step is RenderSteps.World_DrawLightmapOnScreen)
			{
				int quality = Game1.options.lightingQuality / 2;

				if (quality <= tileScale)
				{
					int scale = tileScale / quality;
					var bounds = Game1.lightmap.Bounds;
					var lightingPort = new xTile.Dimensions.Rectangle(Game1.viewport.Location, new(bounds.Width, bounds.Height));
					var layers = lightingLayers.Value;

					for (int i = 0; i < layers.Count; i++)
						layers[i].Draw(Game1.mapDisplayDevice, lightingPort, xTile.Dimensions.Location.Origin, false, scale, i * .0001f + .05f);
				}
			}
		}

		public void Patch(Harmony harmony, out string? Error)
		{
			Error = null;
			// TODO: patch updateOther
		}
	}
}
