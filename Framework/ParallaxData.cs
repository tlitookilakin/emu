using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Delegates;

namespace EMU.Framework
{
	public class ParallaxData
	{
		public List<ParallaxLayer>? Layers { get; set; }
		public bool UseDynamicSky { get; set; }
	}

	public class ParallaxLayer
	{
		public enum RepeatStyle { None, TileH, TileV, TileBoth };

		public float Depth { get; set; }
		public string Texture { get; set; } = "";
		public Rectangle SourceRegion { get; set; }
		public Rectangle TargetRegion { get; set; }
		public RepeatStyle Repeat { get; set; } = RepeatStyle.None;
		public string? Condition { get; set; }
	}

	internal class ParallaxState
	{
		public List<ParallaxLayer> Layers;
		public List<Texture2D> Textures;
		public bool UseDynamicSky;
		public Point Offset;

		public ParallaxState(ParallaxData source, GameStateQueryContext ctx, Point offset)
		{
			UseDynamicSky = source.UseDynamicSky;
			Offset = offset;

			if (source.Layers is List<ParallaxLayer> layers)
			{
				Layers = new(layers.Count);
				Textures = new(layers.Count);

				foreach (var layer in layers)
				{
					if (layer.Condition is string con && !GameStateQuery.CheckConditions(con, ctx))
						continue;

					if (!Assets.TryLoad<Texture2D>(layer.Texture, out var texture))
						continue;

					Layers.Add(layer);
					Textures.Add(texture);
				}
			}
			else
			{
				Layers = [];
				Textures = [];
			}
		}
	}
}
