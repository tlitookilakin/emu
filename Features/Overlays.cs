using EMU.Data;
using EMU.Framework;
using EMU.Framework.Attributes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Mods;
using System.ComponentModel;
using xTile.Display;

namespace EMU.Features;

[Feature("Overlays")]
internal class Overlays
{
	private readonly Assets Assets;
	private static IMonitor? Monitor;
	private readonly PerScreen<List<Overlay>> Layers;

	public Overlays(Assets assets, IModHelper helper, IMonitor monitor)
	{
		Assets = assets;
		Monitor = monitor;

		Layers = new(SupplyData);
		Assets.PropertyChanged += AssetsChanged;
		ModEntry.OnLocationChanged += ChangeLocation;
		helper.Events.Display.RenderedStep += Render;
	}

	private void Render(object? sender, RenderedStepEventArgs e)
	{
		if (e.Step != RenderSteps.World_AlwaysFront)
			return;

		if (Game1.player is not Farmer who)
			return;

		var pos = who.TilePoint;
		foreach (var layer in Layers.Value)
			layer.Update(e.SpriteBatch, pos);
	}

	private void ChangeLocation(GameLocation where, Farmer who)
	{
		Layers.Value = SupplyData(where);
	}

	private void AssetsChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is not nameof(Assets.ExtendedData))
			return;

		Layers.ResetAllScreens();
	}

	private List<Overlay> SupplyData()
		=> SupplyData(Game1.currentLocation);

	private List<Overlay> SupplyData(GameLocation where)
	{
		if (where is null)
			return [];

		if (!Assets.ExtendedData.TryGetValue(where.Name, out var data))
			return [];

		if (data.Overlays is not List<OverlayData> overlays)
			return [];

		var val = new List<Overlay>(overlays.Count);
		foreach (var item in overlays)
			val.Add(new(item));
		return val;
	}

	private class Overlay
	{
		public const int MAX_TICKS = 30;

		public OverlayData Data
		{
			get => data;
			set
			{
				data = value;
				if (data.UseLayer is not null && data.Texture is not null)
				{
					try
					{
						texture = Game1.content.Load<Texture2D>(data.Texture);
					}
					catch (Exception ex)
					{
						Monitor!.Log($"Error loading texture:\n{ex}", LogLevel.Error);
						texture = null;
					}
				}
			}
		}
		private OverlayData data = null!;
		public Texture2D? texture;
		public int ticks;

		public Overlay(OverlayData Data)
		{
			this.Data = Data;
		}

		public void Reset(Point position)
		{
			ticks = data.ActiveRegion.Contains(position) ? MAX_TICKS : 0;
		}

		public void Update(SpriteBatch b, Point position)
		{
			if (data.ActiveRegion.Contains(position))
				ticks = Math.Min(ticks + 1, MAX_TICKS);
			else
				ticks = Math.Max(ticks - 1, 0);

			if (ticks is MAX_TICKS && data.Opacity is 0f)
				return;

			if (texture is null && data.UseLayer is null)
				return;

			float opacity = (float)ModUtilities.SmoothStep(0, MAX_TICKS, 1, data.Opacity, ticks);

			if (data.UseLayer is string name)
			{
				var layer = Game1.currentLocation.Map.GetLayer(name);
				if (layer is null)
					return;

				if (Game1.mapDisplayDevice is XnaDisplayDevice device)
				{
					var col = device.ModulationColour;
					device.ModulationColour = new(device.ModulationColour, opacity);
					layer.Draw(Game1.mapDisplayDevice, Game1.viewport, new(), false, 4);
					device.ModulationColour = col;
				}
			}
			else
			{
				var dest = data.Destination;
				b.Draw(
					texture,
					new Rectangle(dest.X * Game1.tileSize, dest.Y * Game1.tileSize, dest.Width * Game1.tileSize, dest.Height * Game1.tileSize),
					data.Source, 
					Color.White * opacity
				);
			}
		}
	}
}
